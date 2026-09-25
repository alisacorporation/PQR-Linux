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
        // scripting helpers. Also tighten PQR_UpdateInterval from the 100ms
        // upstream default to 30ms — 3x more rotation ticks per second within
        // the 20–1000ms range PQR_ChangeInterval allows.
        self.run_lua(lua_snippets::TABLE_SETUP)?;
        self.run_lua(lua_snippets::FIRST_LOAD)?;
        self.run_lua(lua_snippets::SCRIPTING)?;
        self.run_lua("PQR_ChangeInterval(30)")?;
        Ok(())
    }

    pub fn load_rotation(&mut self, profile: &Profile, rotation_name: &str) -> Result<(), EngineError> {
        let rotation: &Rotation = profile.rotations.iter()
            .find(|r| r.name == rotation_name)
            .ok_or_else(|| EngineError::RotationNotFound(rotation_name.into()))?;
        let resolved = rotation.resolve(&profile.abilities)
            .map_err(EngineError::Unresolved)?;

        // Build one Lua chunk that clears tables, registers every ability,
        // sets requireCombat, and flips the run flags. Upstream PQR issues one
        // Lua_DoString per ability which under our detour costs ~30ms per
        // round-trip — batching turns a ~1.2s rotation load (for 40 abilities)
        // into ~30ms.
        let display = rotation.name.replace('"', "\\\"");
        let combat = if rotation.require_combat { "true" } else { "false" };

        let mut chunk = String::with_capacity(4096 + resolved.iter().map(|a| a.lua.len()).sum::<usize>() * 2);
        chunk.push_str(lua_snippets::CLEAR_TABLES);
        chunk.push('\n');
        for (idx, ability) in resolved.iter().enumerate() {
            chunk.push_str(&add_ability_lua(0, idx, ability));
            chunk.push('\n');
        }
        chunk.push_str(&format!("PQR[0].priorityTable.requireCombat = {combat}\n"));
        // Best-effort call to PQR_EnableBot (fires chat toast). Its assignments
        // fail silently if PlaySound errors on nil sounds, so we set the flags
        // directly right after.
        chunk.push_str(&format!(
            "pcall(PQR_EnableBot, \"{display}\")\n\
             PQR_BotRotation = \"{display}\"\n\
             PQR_BotEnabled = true\n\
             PQR_ManualMode = false\n\
             PQR_ResetMovementTime = 1.0\n"
        ));
        self.run_lua(&chunk)?;
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
