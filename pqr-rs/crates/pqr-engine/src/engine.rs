//! High-level orchestrator.
//!
//! Lifecycle:
//! 1. `Engine::attach(discovery)` — take a matched `Discovery`, open the target,
//!    resolve the health-probe thunk RVA (XML field or pattern scan), install
//!    the detour, hand back an `Engine`.
//! 2. `bootstrap()` — inject the PQR Lua framework (setup tables, event frame,
//!    scripting helpers), one payload per snippet, then set the health marker.
//! 3. `load_rotation(profile, rotation_name)` — resolve rotation, `PQR_SetupTables()`,
//!    then `PQR_AddAbility(...)` for each ability in priority order, finally
//!    `PQR_EnableBot(...)`.
//! 4. `tick()` — periodic no-op that verifies the game is still in-world.
//! 5. `chain_step(on_event)` — ONE self-sustaining link of the payload chain.
//!    The detour only runs when the game calls the hooked function, and that
//!    call is armed by our own executed Lua — so every link both re-arms the
//!    trigger (`kick` chunk) and reads the health marker back. In world this
//!    returns `ChainWork::Injected` ~once per frame; the caller just loops.
//!    Marker gone (`/reload`, relog) → `Lost` + re-bootstrap inline.
//!    Out of world → `ChainWork::Skipped` (pauses the chain through loading
//!    screens, so a pending recovery rides along). `request_reload()` queues
//!    a `ReloadUI()` link; it rides the chain like any other payload.
//! 6. `shutdown()` — `PQR_EnableBot("")` then restore the detour.

use std::io;
use std::sync::Arc;
use std::time::{Duration, Instant};

use pqr_inject::{ExecError, Executor};
use pqr_mem::ProcessMemory;
use pqr_profile::{Profile, Rotation, UnresolvedName};
use pqr_wow::WowClient;
use tracing::{info, warn};

use crate::ability::add_ability_lua;
use crate::ident::IdentReplacer;
use crate::lua_call::{do_string, read_global, GlobalRead};
use crate::lua_snippets;

/// Lua global written by `bootstrap()` and polled by `chain_step()`. Set to
/// `"ALIVE"` only when the core framework globals are all present, so a
/// half-applied bootstrap reads as dead too. Passes through `IdentReplacer`
/// like every other injected fragment, so the wire name is per-session.
pub const MARKER_NAME: &str = "PQR_LuaAlive";

/// Marker assignment — last step of `bootstrap()`, after `PQR_ChangeInterval`.
/// Deliberately independent of `PQR[0]` (created later by `load_rotation`).
pub const MARKER_INIT: &str =
    r#"PQR_LuaAlive = (PQR and PQR_EventFrame and PQR_EnableBot and "ALIVE" or "DEAD")"#;

const MARKER_ALIVE: &str = "ALIVE";

/// Kick chunk executed at the head of every chain link — see `lua_call`.
///
/// Toggles the framework event frame's visibility: a same-call Hide/Show is
/// invisible (no frame boundary renders between them) but mutates the frame
/// tree, which is what re-arms the detoured function's next call. After a
/// `/reload` has wiped the framework the frame is gone — fall back to
/// toggling `UIParent`, which exists in a stock client; heavier (addons hook
/// its show/hide), but it only runs for the one or two detection links
/// before recovery re-creates `PQR_EventFrame`.
pub const CHAIN_KICK: &str = "if PQR_EventFrame then PQR_EventFrame:Hide() PQR_EventFrame:Show() \
                              else UIParent:Hide() UIParent:Show() end";

/// Outcome of one `chain_step` — tells the caller whether work happened, so
/// it can pace its loop (tight loop while the chain runs, sleep when paused).
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ChainWork {
    /// A payload was injected and consumed by the game.
    Injected,
    /// Nothing attempted (out of world / not in world yet) — the caller
    /// should sleep rather than spin; the next step re-checks.
    Skipped,
}

/// Cadence for the outer loop's stat polling (`tick`).
pub const DEFAULT_TICK_INTERVAL: Duration = Duration::from_millis(200);

/// Emitted by `Engine::chain_step` as framework health changes (and once when
/// the probe itself is unavailable). The chain's steady state is a stream of
/// `LuaEvent::Alive`, one per link.
#[derive(Debug, Clone)]
pub enum LuaEvent {
    /// Marker confirmed present — framework alive. Carries the link round-trip
    /// time, which is the main health diagnostic: while the chain is healthy
    /// each link costs one game frame (~16ms); 3000ms is the timeout.
    Alive(Duration),
    /// Marker gone — the Lua state was reset (`/reload`, relog).
    Lost,
    /// `bootstrap()` + `load_rotation()` are running again.
    Recovering,
    /// Recovery succeeded; the rotation is registered again.
    Recovered,
    /// Recovery attempt failed (retried on the next step).
    RecoverFailed(String),
    /// `ClntObjMgrGetActivePlayerObj` pattern unresolved — marker reads
    /// disabled (reported once per session); the chain still runs kick-only
    /// links so commands keep working.
    ProbeUnavailable,
}

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
    #[error("no rotation loaded yet")]
    NotLoaded,
}

pub struct Engine {
    pub client: WowClient,
    pub executor: Executor,
    pub ident: IdentReplacer,
    /// RVA of `ClntObjMgrGetActivePlayerObj` — from the offsets XML, or
    /// pattern-scanned at attach. 0 = probe disabled.
    clnt_rva: u32,
    /// Rotation loaded by `load_rotation`, remembered for `recover()`.
    loaded: Option<(Profile, String)>,
    /// Last confirmed framework health (`None` = never probed).
    lua_alive: Option<bool>,
    pending_recovery: bool,
    /// A `ReloadUI()` link was requested (`x` key) — sent as the next step,
    /// cleared only once it has been consumed by the game.
    pending_reload: bool,
    probe_unavailable_reported: bool,
}

impl Engine {
    pub fn attach(client: WowClient) -> Result<Self, EngineError> {
        Self::attach_with(client, IdentReplacer::random())
    }

    pub fn attach_with(client: WowClient, ident: IdentReplacer) -> Result<Self, EngineError> {
        let mem: Arc<dyn ProcessMemory> = client.mem.clone();

        // Health-probe thunk: usable offsets XMLs carry the RVA, the shipped
        // 12340 file doesn't — same fallback `frmMain` applies when the
        // element is zero: scan the mapped image for the wrapper prologue.
        let clnt_rva = if client.offsets.clnt_obj_mgr_get_active_player != 0 {
            client.offsets.clnt_obj_mgr_get_active_player
        } else {
            pqr_wow::scan::resolve_clnt_obj_mgr(&*mem)?
        };
        info!(clnt_rva = format!("0x{clnt_rva:x}"), "ClntObjMgrGetActivePlayerObj resolved");
        if clnt_rva == 0 {
            warn!("ClntObjMgr pattern not found — lua health probe disabled");
        }

        let detour_va = mem.image_base().wrapping_add(client.offsets.detour);
        warn_if_already_patched(&*mem, detour_va, &client.offsets.overwritten)?;
        let mut executor = Executor::new(mem, detour_va, client.offsets.overwritten.clone())?;
        executor.apply()?;
        info!(detour_va = format!("0x{detour_va:x}"), "detour applied");
        Ok(Self {
            client,
            executor,
            ident,
            clnt_rva,
            loaded: None,
            lua_alive: None,
            pending_recovery: false,
            pending_reload: false,
            probe_unavailable_reported: false,
        })
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
        // the 20–1000ms range PQR_ChangeInterval allows. The marker goes last
        // so `chain_step()` never sees a half-initialized framework as alive.
        self.run_lua(lua_snippets::TABLE_SETUP)?;
        self.run_lua(lua_snippets::FIRST_LOAD)?;
        self.run_lua(lua_snippets::SCRIPTING)?;
        self.run_lua("PQR_ChangeInterval(30)")?;
        self.run_lua(MARKER_INIT)?;
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
        self.loaded = Some((profile.clone(), rotation_name.to_string()));
        Ok(())
    }

    /// Poll cadence for the outer loop. Verifies the game is still in world
    /// and returns whether the engine should keep running.
    pub fn tick(&self) -> io::Result<bool> {
        self.client.is_in_world()
    }

    /// Queue a `ReloadUI()` — executed as the next chain link, so it only
    /// goes out while the trigger is armed, and is retried (the flag stays
    /// set) until the game actually consumes it.
    pub fn request_reload(&mut self) {
        self.pending_reload = true;
    }

    /// One self-sustaining link of the payload chain — call in a tight loop.
    ///
    /// Every link is `[Execute(kick chunk); marker read]`: the kick re-arms
    /// the detour trigger for the next link (payloads are only ever consumed
    /// when the game calls the hooked function, and that call comes from our
    /// own executed Lua — see `lua_call` module docs). That makes the chain
    /// ride at ~one link per frame while in world:
    ///
    /// - queued `ReloadUI()` (from `request_reload`) goes out first;
    /// - a pending recovery is retried before probing;
    /// - marker alive: `Alive(link latency)` — the UI's live health meter;
    /// - marker gone: `Lost` once, then `Recovering`, then `Recovered` or
    ///   `RecoverFailed` (stays pending, retried next step);
    /// - out of world: `ChainWork::Skipped` — the chain pauses through
    ///   loading screens instead of firing probes at a half-loaded frame.
    ///
    /// The marker read never runs Lua itself — it only reads
    /// `_G[PQR_LuaAlive]` through `Lua_GetLocalizedText`, so a wiped stack
    /// can't crash the probe.
    pub fn chain_step<F: FnMut(LuaEvent)>(&mut self, mut on_event: F) -> Result<ChainWork, EngineError> {
        if !self.client.is_in_world()? {
            return Ok(ChainWork::Skipped);
        }

        // Queued ReloadUI rides the chain like any other payload.
        if self.pending_reload {
            let chunk = format!("{}\nReloadUI()", self.ident.apply(CHAIN_KICK));
            do_string(&mut self.executor, &self.client.offsets, &chunk)?;
            self.pending_reload = false;
            return Ok(ChainWork::Injected);
        }

        if self.pending_recovery {
            self.finish_recovery(on_event)?;
            return Ok(ChainWork::Injected);
        }

        // No ClntObjMgr thunk — no marker read, but keep feeding the chain so
        // commands (ReloadUI, shutdown) still get consumed.
        if self.clnt_rva == 0 {
            if !self.probe_unavailable_reported {
                self.probe_unavailable_reported = true;
                on_event(LuaEvent::ProbeUnavailable);
            }
            let kick = self.ident.apply(CHAIN_KICK);
            do_string(&mut self.executor, &self.client.offsets, &kick)?;
            return Ok(ChainWork::Injected);
        }

        let marker = self.ident.apply(MARKER_NAME);
        let kick = self.ident.apply(CHAIN_KICK);
        let link_start = Instant::now();
        match read_global(&mut self.executor, &self.client.offsets, self.clnt_rva, &marker, &kick)? {
            // Player object transiently missing mid-transition — the kick
            // still executed, so the chain stays fed; probe again next step.
            GlobalRead::NoPlayer => Ok(ChainWork::Injected),
            GlobalRead::Found(v) if v.trim() == MARKER_ALIVE => {
                self.lua_alive = Some(true);
                on_event(LuaEvent::Alive(link_start.elapsed()));
                Ok(ChainWork::Injected)
            }
            _ => {
                if self.lua_alive != Some(false) {
                    on_event(LuaEvent::Lost);
                }
                self.lua_alive = Some(false);
                self.pending_recovery = true;
                self.finish_recovery(on_event)?;
                Ok(ChainWork::Injected)
            }
        }
    }

    fn finish_recovery<F: FnMut(LuaEvent)>(&mut self, mut on_event: F) -> Result<(), EngineError> {
        on_event(LuaEvent::Recovering);
        match self.recover() {
            Ok(()) => {
                self.pending_recovery = false;
                self.lua_alive = Some(true);
                on_event(LuaEvent::Recovered);
                Ok(())
            }
            Err(e) => {
                // Keep `pending_recovery` — retried on the next chain step.
                on_event(LuaEvent::RecoverFailed(e.to_string()));
                Ok(())
            }
        }
    }

    /// Re-run the full bootstrap + rotation load for the remembered rotation.
    /// Idempotent: every snippet either guards its globals or rebuilds them.
    pub fn recover(&mut self) -> Result<(), EngineError> {
        let (profile, rotation) = self.loaded.clone().ok_or(EngineError::NotLoaded)?;
        self.bootstrap()?;
        self.load_rotation(&profile, &rotation)?;
        Ok(())
    }

    pub fn shutdown(&mut self) -> Result<(), EngineError> {
        // Best-effort disable; even if Lua fails, still restore the detour.
        let _ = self.run_lua(r#"PQR_EnableBot("")"#);
        self.executor.restore()?;
        Ok(())
    }
}

/// The detour prologue is supposed to hold the XML's `Overwritten` bytes.
/// Anything else means a hook is already installed — a second live PQR
/// instance, or a stale one left by a crash. We still attach (the stale case
/// must self-heal), but say so loudly: our `restore()` will write the XML
/// bytes back and unhook the other instance's injections.
pub fn warn_if_already_patched(
    mem: &dyn ProcessMemory,
    detour_va: u32,
    overwritten: &[u8],
) -> io::Result<()> {
    let current = mem.read_bytes(detour_va, overwritten.len())?;
    if current != overwritten {
        let hooked = matches!(current.first(), Some(0xE8 | 0xE9 | 0xEB));
        if hooked {
            warn!(
                detour_va = format!("0x{detour_va:x}"),
                "detour already patched — another pqr instance or a stale hook; \
                 proceeding, restore will unhook it"
            );
        } else {
            warn!(
                detour_va = format!("0x{detour_va:x}"),
                current = format!("{current:02x?}"),
                "detour prologue differs from offsets Overwritten bytes"
            );
        }
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn marker_init_targets_real_framework_globals() {
        // The marker is the health probe's ground truth: every name it checks
        // must exist in the snippets `bootstrap()` actually injects.
        assert!(lua_snippets::FIRST_LOAD.contains("PQR_EventFrame = CreateFrame"));
        assert!(lua_snippets::FIRST_LOAD.contains("function PQR_EnableBot"));
        assert!(lua_snippets::TABLE_SETUP.contains("PQR = {}"));
        assert!(lua_snippets::FIRST_LOAD.contains("if PQR_EventFrame == nil"));
        for token in [MARKER_NAME, "PQR", "PQR_EventFrame", "PQR_EnableBot", MARKER_ALIVE, "DEAD"] {
            assert!(MARKER_INIT.contains(token), "MARKER_INIT misses {token}");
        }
        // …and is sent through the ident renamer by `bootstrap()` via
        // `run_lua`, like every other injected fragment.
        assert!(MARKER_INIT.starts_with(MARKER_NAME));
    }

    #[test]
    fn chain_kick_covers_framework_and_post_reload_states() {
        // The kick re-arms the trigger: with the framework up it toggles the
        // event frame; right after /reload (frame gone) it falls back to a
        // stock frame — both must survive the ident renamer.
        assert!(CHAIN_KICK.contains("PQR_EventFrame"));
        assert!(CHAIN_KICK.contains("UIParent"));
        assert!(CHAIN_KICK.contains("Hide()") && CHAIN_KICK.contains("Show()"));
        let id = IdentReplacer::from_names("ZZZ".into(), "YYY".into(), "WWW".into());
        let kicked = id.apply(CHAIN_KICK);
        assert!(kicked.contains("ZZZ_EventFrame"), "kick not renamed: {kicked}");
        assert!(kicked.contains("UIParent"), "UIParent must stay untouched: {kicked}");
        assert!(!kicked.contains("PQR"), "kick not fully renamed: {kicked}");
    }

    #[test]
    fn marker_follows_ident_renamer() {
        let id = IdentReplacer::from_names("ZZZ".into(), "YYY".into(), "WWW".into());
        assert_eq!(id.apply(MARKER_NAME), "ZZZ_LuaAlive");
        let init = id.apply(MARKER_INIT);
        assert!(init.starts_with("ZZZ_LuaAlive = (ZZZ and ZZZ_EventFrame and ZZZ_EnableBot"));
        assert!(!init.contains("PQR"), "marker not fully renamed: {init}");
        // … and the probe reads the same renamed name.
        assert_eq!(id.apply(MARKER_NAME), id.apply(MARKER_NAME));
    }
}
