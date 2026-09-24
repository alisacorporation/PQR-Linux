//! High-level orchestrator.
//!
//! Lifecycle:
//! 1. `Engine::attach(discovery)` — take a matched `Discovery`, open the target,
//!    install the detour, hand back an `Engine`.
//! 2. `bootstrap()` — inject the PQR Lua framework (setup tables, event frame,
//!    scripting helpers), one payload per snippet.
//! 3. `load_rotation(profile, rotation_name)` — resolve rotation, `PQR_SetupTables()`,
//!    then `PQR_AddAbility(...)` for each ability in priority order, finally
//!    `PQR_EnableBot(...)`.
//! 4. `tick()` — periodic no-op that verifies the game is still in-world; on
//!    exit from world we tear down.
//! 5. `shutdown()` — `PQR_EnableBot("")` then restore the detour.

use std::io;
use std::sync::Arc;
use std::time::Duration;

use pqr_inject::{ExecError, Executor};
use pqr_mem::ProcessMemory;
use pqr_profile::{Profile, Rotation, UnresolvedName};
use pqr_wow::WowClient;
use tracing::info;

use crate::ability::add_ability_lua;
use crate::ident::IdentReplacer;
use crate::lua_call::do_string;
use crate::lua_snippets;

#[derive(Debug, thiserror::Error)]
pub enum EngineError {
    #[error("io: {0}")]
    Io(#[from] io::Error),
    #[error("executor: {0}")]
    Exec(#[from] ExecError),
    #[error("unresolved ability names in rotation: {0:?}")]
    Unresolved(Vec<UnresolvedName>),
    #[error("rotation not found: {0}")]
    RotationNotFound(String),
}

pub struct Engine {
    pub client: WowClient,
    pub executor: Executor,
    pub ident: IdentReplacer,
}

impl Engine {
    pub fn attach(client: WowClient) -> Result<Self, EngineError> {
        Self::attach_with(client, IdentReplacer::random())
    }

    pub fn attach_with(client: WowClient, ident: IdentReplacer) -> Result<Self, EngineError> {
        let mem: Arc<dyn ProcessMemory> = client.mem.clone();
        let detour_va = mem.image_base().wrapping_add(client.offsets.detour);
        let mut executor = Executor::new(mem, detour_va, client.offsets.overwritten.clone())?;
        executor.apply()?;
        info!(detour_va = format!("0x{detour_va:x}"), "detour applied");
        Ok(Self { client, executor, ident })
    }

    /// Send a chunk of Lua text through the detour. Applies identifier
    /// randomization first.
    pub fn run_lua(&mut self, lua: &str) -> Result<(), EngineError> {
        let rewritten = self.ident.apply(lua);
        do_string(&mut self.executor, &self.client.offsets, &rewritten)?;
        Ok(())
    }

    pub fn bootstrap(&mut self) -> Result<(), EngineError> {
        // Order matters: table setup first (defines PQR_AddAbility), then the
        // main framework (defines the event frame + PQR_ExecuteBot), then the
        // scripting helpers.
        self.run_lua(lua_snippets::TABLE_SETUP)?;
        self.run_lua(lua_snippets::FIRST_LOAD)?;
        self.run_lua(lua_snippets::SCRIPTING)?;
        Ok(())
    }

    pub fn load_rotation(&mut self, profile: &Profile, rotation_name: &str) -> Result<(), EngineError> {
        let rotation: &Rotation = profile.rotations.iter()
            .find(|r| r.name == rotation_name)
            .ok_or_else(|| EngineError::RotationNotFound(rotation_name.into()))?;
        let resolved = rotation.resolve(&profile.abilities)
            .map_err(EngineError::Unresolved)?;

        // Rebuild tables from scratch (equivalent to strClearTables call).
        self.run_lua(lua_snippets::CLEAR_TABLES)?;

        // Register abilities in priority order. Rotation index 0 = the active
        // rotation slot; the original PQR uses 0..=4 for multi-slot rotations,
        // v0.1 uses just slot 0.
        for (idx, ability) in resolved.iter().enumerate() {
            let lua = add_ability_lua(0, idx, ability);
            self.run_lua(&lua)?;
        }

        // Requirecombat flag on the slot.
        self.run_lua(&format!(
            "PQR[0].priorityTable.requireCombat = {}",
            if rotation.require_combat { "true" } else { "false" },
        ))?;

        // Enable the bot. The upstream `PQR_EnableBot` calls `PlaySound` with
        // globals PreStartupBot would have set (StartRotationSound etc.);
        // when those are nil, PlaySound errors and the assignments below it
        // never run. We best-effort call the function (for its side effect of
        // firing the chat toast), then set the state flags directly so the
        // tick loop reliably enters `PQR_CastNext(0)`.
        let display = rotation.name.replace('"', "\\\"");
        self.run_lua(&format!(
            r#"pcall(PQR_EnableBot, "{display}")
               PQR_BotRotation = "{display}"
               PQR_BotEnabled = true
               PQR_ManualMode = false
               PQR_ResetMovementTime = 1.0"#
        ))?;
        Ok(())
    }

    /// Poll cadence for the outer loop. Verifies the game is still in world
    /// and returns whether the engine should keep running.
    pub fn tick(&self) -> io::Result<bool> {
        Ok(self.client.is_in_world()?)
    }

    pub fn shutdown(&mut self) -> Result<(), EngineError> {
        // Best-effort disable; even if Lua fails, still restore the detour.
        let _ = self.run_lua(r#"PQR_EnableBot("")"#);
        self.executor.restore()?;
        Ok(())
    }
}

pub const DEFAULT_TICK_INTERVAL: Duration = Duration::from_millis(200);
