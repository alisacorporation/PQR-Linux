//! Payloads that talk to WoW's Lua VM over the detour.
//!
//! Three shapes:
//! - `do_string` — fire-and-forget `FrameScript::Execute` (Lua_DoString);
//!   most bootstrap payloads don't need a return value, so we skip the
//!   readback the C# original does (`clsMemory.Lua_GetReturnValue`).
//! - `read_global` — chain link: executes `kick` (a Lua chunk) through
//!   `FrameScript::Execute`, then reads a name out of `_G`
//!   (`ClntObjMgrGetActivePlayerObj` + `Lua_GetLocalizedText`; see
//!   `clsMemory.cs:197-241` for the original — the upstream combined
//!   "run `return X`, then read back" form is broken there, it probes the
//!   global `"nil"`, so we never copy it).
//! - the raw pieces (`build_*` helpers) for tests.
//!
//! **Why every read carries a `kick` chunk:** the detour is only ever
//! executed when the game calls the hooked function — payload consumption is
//! demand-driven. Empirically that call is armed by *our own* executed Lua
//! (bootstrap chains payloads back-to-back because each chunk's execution
//! triggers the next call a frame later), and by in-game activity; a payload
//! that only reads leaves no trace and the very next publish starves. So
//! each health-probe link re-arms the trigger itself: `kick` mutates a frame
//! (Hide/Show toggle, invisible inside one Lua call), then the marker is
//! read back. That makes the chain self-sustaining — probes, `ReloadUI()`
//! and recovery all ride the window the previous link opened.
//!
//! The read path returns `GlobalRead::Missing` for a nil/non-string global,
//! which is how the engine's health probe notices that a `/reload` wiped the
//! injected framework.

use std::io;

use iced_x86::code_asm::*;
use pqr_inject::{ExecError, Executor};
use pqr_wow::Offsets;

/// Outcome of reading one string global out of the target's Lua state.
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum GlobalRead {
    /// No active player object (loading screen / not in world) — the read
    /// wasn't attempted, health is unknown.
    NoPlayer,
    /// Player present but the global is nil (or not string/number/bool).
    Missing,
    /// Player present and the global held a string.
    Found(String),
}

/// Send `command` to WoW's Lua VM, don't read a return value. The command
/// text is copied into WoW's address space; freed after the call completes.
pub fn do_string(exec: &mut Executor, offsets: &Offsets, command: &str) -> Result<(), ExecError> {
    let mem = exec.mem_arc();
    let mut bytes = command.as_bytes().to_vec();
    bytes.push(0);
    let cmd_addr = mem.alloc(bytes.len(), false)?;
    mem.write_bytes(cmd_addr, &bytes)?;

    let lua_do_string = mem.image_base().wrapping_add(offsets.lua_do_string);

    let result = exec.inject_and_execute(|a| {
        emit_kick(a, cmd_addr, lua_do_string)?;
        a.ret()
    });
    // free cmd regardless of success
    let _ = mem.free(cmd_addr);
    result
}

/// Read the global `name` from the target's Lua state.
///
/// First executes `kick` through `Lua_DoString` (arms the next detour hit —
/// see the module docs), then:
///
/// ```nasm
/// mov  eax, clnt_va            ; ClntObjMgrGetActivePlayerObj, cdecl, no args
/// call eax
/// test eax, eax
/// je   @noplayer               ; sentinel 1 — health unknown, skip
/// mov  ecx, eax                ; thiscall: player object
/// push -1                      ; Lua stack index (ignored by the name ladder)
/// push name_addr               ; global name, NUL-terminated
/// mov  eax, getloc_va          ; Lua_GetLocalizedText: callee pops 8 bytes
/// call eax                     ;   (`ret $8`), stack stays balanced
/// mov  [ret_addr], eax         ; char* | 0 (name not found)
/// jmp  @done
/// @noplayer: mov [ret_addr], 1
/// @done: ret
/// ```
///
/// The buffer read back after injection: `0` = global missing, `1` = no
/// player, anything else = pointer to the value string in the target.
pub fn read_global(
    exec: &mut Executor,
    offsets: &Offsets,
    clnt_rva: u32,
    name: &str,
    kick: &str,
) -> Result<GlobalRead, ExecError> {
    if clnt_rva == 0 {
        return Ok(GlobalRead::NoPlayer);
    }
    if offsets.lua_get_localized_text == 0 {
        return Err(ExecError::Io(io::Error::other(
            "Lua_GetLocalizedTextAddress missing from offsets",
        )));
    }

    let mem = exec.mem_arc();
    let mut name_bytes = name.as_bytes().to_vec();
    name_bytes.push(0);
    let name_addr = mem.alloc(name_bytes.len(), false)?;
    mem.write_bytes(name_addr, &name_bytes)?;
    let mut kick_bytes = kick.as_bytes().to_vec();
    kick_bytes.push(0);
    let kick_addr = mem.alloc(kick_bytes.len(), false)?;
    mem.write_bytes(kick_addr, &kick_bytes)?;
    let ret_addr = mem.alloc(4, false)?;
    mem.write_u32(ret_addr, 0)?;

    let clnt_va = mem.image_base().wrapping_add(clnt_rva);
    let getloc_va = mem.image_base().wrapping_add(offsets.lua_get_localized_text);
    let do_string_va = mem.image_base().wrapping_add(offsets.lua_do_string);
    let executed = exec.inject_and_execute(|a| {
        emit_kick(a, kick_addr, do_string_va)?;
        emit_read_global(a, name_addr, ret_addr, clnt_va, getloc_va)
    });

    let _ = mem.free(name_addr);
    let _ = mem.free(kick_addr);
    let result = match executed {
        Ok(()) => {
            let slot = mem.read_u32(ret_addr)?;
            match slot {
                0 => Ok(GlobalRead::Missing),
                1 => Ok(GlobalRead::NoPlayer),
                // Lua may free or move the string between the read and this
                // copy; a failed/stale read is reported as Missing — the
                // conservative direction (triggers a safe re-bootstrap).
                addr => Ok(mem.read_utf8_string(addr, 2000)
                    .map(GlobalRead::Found)
                    .unwrap_or(GlobalRead::Missing)),
            }
        }
        Err(e) => Err(e),
    };
    let _ = mem.free(ret_addr);
    result
}

/// Convenience for wire-in helpers: build the `do_string` payload without
/// executing (useful for tests). Returns the assembled bytes at `base`.
pub fn build_do_string_payload(
    base: u32,
    cmd_addr: u32,
    lua_do_string_va: u32,
) -> io::Result<Vec<u8>> {
    let mut a = CodeAssembler::new(32).map_err(io_asm)?;
    a.push(0i32).map_err(io_asm)?;
    a.push(cmd_addr as i32).map_err(io_asm)?;
    a.push(cmd_addr as i32).map_err(io_asm)?;
    a.mov(eax, lua_do_string_va).map_err(io_asm)?;
    a.call(eax).map_err(io_asm)?;
    a.add(esp, 0x0Ci32).map_err(io_asm)?;
    a.ret().map_err(io_asm)?;
    a.assemble(base as u64).map_err(io_asm)
}

/// Assemble a chain link (`kick` chunk + `read_global`) at `base` without
/// executing it (tests / wire-in helpers). Mirrors `build_do_string_payload`.
pub fn build_read_global_payload(
    base: u32,
    kick_addr: u32,
    do_string_va: u32,
    name_addr: u32,
    ret_addr: u32,
    clnt_va: u32,
    getloc_va: u32,
) -> io::Result<Vec<u8>> {
    let mut a = CodeAssembler::new(32).map_err(io_asm)?;
    emit_kick(&mut a, kick_addr, do_string_va).map_err(io_asm)?;
    emit_read_global(&mut a, name_addr, ret_addr, clnt_va, getloc_va).map_err(io_asm)?;
    a.assemble(base as u64).map_err(io_asm)
}

/// `FrameScript::Execute(kick_addr)` prelude: push 0, push cmd, push cmd,
/// call `Lua_DoString`, pop the 3 args. No trailing ret — the payload
/// continues with whatever else the link needs (the marker read).
fn emit_kick(a: &mut CodeAssembler, kick_addr: u32, do_string_va: u32) -> Result<(), iced_x86::IcedError> {
    a.push(0i32)?;
    a.push(kick_addr as i32)?;
    a.push(kick_addr as i32)?;
    a.mov(eax, do_string_va)?;
    a.call(eax)?;
    a.add(esp, 0x0Ci32)?;
    Ok(())
}

fn emit_read_global(
    a: &mut CodeAssembler,
    name_addr: u32,
    ret_addr: u32,
    clnt_va: u32,
    getloc_va: u32,
) -> Result<(), iced_x86::IcedError> {
    let mut noplayer = a.create_label();
    let mut done = a.create_label();

    a.mov(eax, clnt_va)?;
    a.call(eax)?;
    a.test(eax, eax)?;
    a.je(noplayer)?;
    a.mov(ecx, eax)?;
    a.push(-1i32)?;
    a.push(name_addr as i32)?;
    a.mov(eax, getloc_va)?;
    a.call(eax)?;
    a.mov(dword_ptr(ret_addr), eax)?;
    a.jmp(done)?;
    a.set_label(&mut noplayer)?;
    a.mov(dword_ptr(ret_addr), 1i32)?;
    a.set_label(&mut done)?;
    a.ret()
}

fn io_asm(e: iced_x86::IcedError) -> io::Error {
    io::Error::other(format!("asm: {e}"))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn payload_ends_with_ret() {
        let b = build_do_string_payload(0x1000_0000, 0x2000_0000, 0x0041_9210).unwrap();
        assert_eq!(*b.last().unwrap(), 0xC3); // near ret
    }

    #[test]
    fn read_global_payload_assembles() {
        let b = build_read_global_payload(
            0x1000_0000,
            0x2100_0000, // kick chunk
            0x0040_1210, // Lua_DoString
            0x2000_0000,
            0x3000_0000,
            0x0040_38F0,
            0x0072_25E0,
        )
        .unwrap();
        assert_eq!(*b.last().unwrap(), 0xC3); // near ret
        assert!(b.len() > 20, "suspiciously small payload: {} bytes", b.len());
        // both target addresses must appear in the immediate operands
        assert!(b.windows(4).any(|w| w == 0x0040_38F0u32.to_le_bytes()));
        assert!(b.windows(4).any(|w| w == 0x0072_25E0u32.to_le_bytes()));
        // kick prelude: Lua_DoString address must be present too …
        assert!(b.windows(4).any(|w| w == 0x0040_1210u32.to_le_bytes()));
        // … and execute before the marker read (it arms the next detour hit).
        let kick_pos = b.windows(4).position(|w| w == 0x0040_1210u32.to_le_bytes()).unwrap();
        let clnt_pos = b.windows(4).position(|w| w == 0x0040_38F0u32.to_le_bytes()).unwrap();
        assert!(kick_pos < clnt_pos, "kick must run before the marker read");
        // sentinel 1 for the no-player path: `mov dword [ret], 1` -> C7 /0
        assert!(b.windows(4).any(|w| w[0] == 0xC7 && w[1] == 0x05),
                "no mov-dword-imm store in payload: {b:02x?}");
    }
}
