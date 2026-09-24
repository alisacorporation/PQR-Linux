//! Build the 32-bit x86 payload that calls `FrameScript::Execute`
//! (Lua_DoString) with a given command string, then reads back the return
//! value via `GetLocalizedText`.
//!
//! Ported from `clsMemory.Lua_GetReturnValue` (`reversed/.../clsMemory.cs:197-241`).
//! The layout differs from the original in that we skip the return-value read
//! for fire-and-forget calls (most bootstrap payloads don't need it).

use std::io;

use iced_x86::code_asm::*;
use pqr_inject::{ExecError, Executor};
use pqr_wow::Offsets;

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
        // push 0
        // push cmd_addr
        // push cmd_addr
        // mov eax, lua_do_string
        // call eax
        // add esp, 0xC
        // retn
        a.push(0i32)?;
        a.push(cmd_addr as i32)?;
        a.push(cmd_addr as i32)?;
        a.mov(eax, lua_do_string)?;
        a.call(eax)?;
        a.add(esp, 0x0Ci32)?;
        a.ret()
    });
    // free cmd regardless of success
    let _ = mem.free(cmd_addr);
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
}
