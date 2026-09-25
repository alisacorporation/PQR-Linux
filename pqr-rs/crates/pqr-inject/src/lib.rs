//! Detour executor — 1:1 port of `reversed/pqr-app/PriorityQueueRotation/Executor.cs`.
//!
//! Setup once:
//! - allocate `code_cave_ptr` (4 bytes, initialised to 0) — the "pending payload"
//!   handshake slot.
//! - allocate `detour_ptr` (~598 bytes) — the trampoline region.
//! - write the original prologue bytes (`overwritten`) at `detour_ptr`, followed
//!   by a stub that checks `code_cave_ptr`, calls the pending payload, clears
//!   the slot, and jumps back to the original function.
//! - patch the first bytes of the target function with `jmp detour_ptr`.
//!
//! Executing anything:
//! - assemble the caller's asm into a fresh allocation.
//! - write that allocation's address into `code_cave_ptr`.
//! - busy-wait (10 ms sleep) until the game thread clears the slot, or 3 s
//!   passes. Then free the payload memory.

use std::io;
use std::sync::Arc;
use std::time::{Duration, Instant};

use iced_x86::code_asm::*;
use iced_x86::IcedError;
use pqr_asm::assemble_randomized;
use pqr_mem::ProcessMemory;
use tracing::debug;

#[derive(Debug, thiserror::Error)]
pub enum ExecError {
    #[error("io: {0}")]
    Io(#[from] io::Error),
    #[error("asm: {0}")]
    Asm(#[from] IcedError),
    #[error("payload timed out after {0:?}")]
    Timeout(Duration),
}

pub struct Executor {
    mem: Arc<dyn ProcessMemory>,
    detour_va: u32,              // absolute address of the target function
    overwritten: Vec<u8>,        // original prologue bytes (usually 9 bytes)
    code_cave_ptr: u32,
    detour_ptr: u32,
    applied: bool,
    rng_seed: u64,
}

const TRAMPOLINE_SIZE: usize = 598;
const HANDSHAKE_TIMEOUT: Duration = Duration::from_secs(3);
// 1ms polling: payload consume normally takes 10-30ms (bounded by game
// thread hitting the detour), so shaving off up to 9ms of the tail wait cuts
// average injection latency by ~5ms. Cost is a few extra RPM syscalls per
// payload — negligible under Wine.
const POLL_INTERVAL: Duration = Duration::from_millis(1);

impl Executor {
    pub fn new(mem: Arc<dyn ProcessMemory>, detour_va: u32, overwritten: Vec<u8>) -> io::Result<Self> {
        let code_cave_ptr = mem.alloc(4, false)?;
        mem.write_u32(code_cave_ptr, 0)?;
        let detour_ptr = mem.alloc(TRAMPOLINE_SIZE, true)?;
        let rng_seed = seed_from_time();
        Ok(Self {
            mem, detour_va, overwritten,
            code_cave_ptr, detour_ptr, applied: false, rng_seed,
        })
    }

    pub fn is_applied(&self) -> bool { self.applied }
    pub fn code_cave_ptr(&self) -> u32 { self.code_cave_ptr }
    pub fn detour_ptr(&self) -> u32 { self.detour_ptr }
    pub fn mem_arc(&self) -> Arc<dyn ProcessMemory> { self.mem.clone() }

    pub fn apply(&mut self) -> Result<(), ExecError> {
        if self.applied { self.restore()?; }

        // Trampoline layout: [overwritten bytes][stub][... unused]
        // Stub jumps back to detour_va + overwritten.len().
        let stub_va = self.detour_ptr.wrapping_add(self.overwritten.len() as u32);
        let stub_bytes = self.build_stub(stub_va)?;

        // Write prologue then stub.
        self.mem.write_bytes(self.detour_ptr, &self.overwritten)?;
        self.mem.write_bytes(stub_va, &stub_bytes)?;

        // Replace the first bytes of the target function with `jmp detour_ptr`.
        // We reserve exactly overwritten.len() bytes; a near jmp is 5 bytes,
        // the extra bytes are padded with nop so the layout remains sane if
        // any tool disassembles it.
        let hook = self.build_hook_to_detour()?;
        self.mem.write_bytes(self.detour_va, &hook)?;
        self.applied = true;
        Ok(())
    }

    pub fn restore(&mut self) -> Result<(), ExecError> {
        if self.applied {
            self.mem.write_bytes(self.detour_va, &self.overwritten)?;
            self.applied = false;
        }
        Ok(())
    }

    /// Assemble `build`, allocate memory in the target for it, publish the
    /// address in `code_cave_ptr`, and wait for the game thread to consume it.
    pub fn inject_and_execute<F>(&mut self, build: F) -> Result<(), ExecError>
    where
        F: FnOnce(&mut CodeAssembler) -> Result<(), IcedError>,
    {
        // Allocate first so we know the target address before assembling.
        // The payload is position-dependent (any absolute addresses baked in
        // by the caller are computed against this base), so we allocate an
        // upper-bound size, assemble, and only write the exact bytes.
        const PAYLOAD_MAX: usize = 16 * 1024;
        let payload_va = self.mem.alloc(PAYLOAD_MAX, true)?;

        let seed = self.next_seed();
        let bytes = assemble_randomized(payload_va, seed, |rec| {
            // Adapter: build's argument is a CodeAssembler, but our Recorder
            // API takes closures per-instruction to allow junk splicing. So
            // we bypass randomization here by running build directly on an
            // assembler and pushing the resulting bytes as a single opaque
            // step — junk is still inserted around it, just not between the
            // caller's own instructions.
            let mut inner = CodeAssembler::new(32)?;
            build(&mut inner)?;
            let inner_bytes = inner.assemble(payload_va as u64)?;
            rec.push(move |a| a.db(&inner_bytes));
            Ok(())
        })?;

        if bytes.len() > PAYLOAD_MAX {
            self.mem.free(payload_va).ok();
            return Err(ExecError::Io(io::Error::other(format!(
                "payload {} bytes exceeds PAYLOAD_MAX {PAYLOAD_MAX}", bytes.len()
            ))));
        }

        self.mem.write_bytes(payload_va, &bytes)?;
        self.mem.write_u32(self.code_cave_ptr, payload_va)?;
        debug!(payload_va = format!("0x{payload_va:x}"), size = bytes.len(), "payload published, awaiting handshake");

        let start = Instant::now();
        while self.mem.read_u32(self.code_cave_ptr)? != 0 {
            if start.elapsed() > HANDSHAKE_TIMEOUT {
                // The game hasn't taken the payload — clear the handshake and
                // give any in-flight stub invocation one frame to finish
                // before releasing the memory. Skipping this leaves a dangling
                // pointer in the cave: the next detour hit executes freed
                // memory (crashes the target), and every later injection sees
                // a stuck slot (permanent timeouts).
                let _ = self.mem.write_u32(self.code_cave_ptr, 0);
                std::thread::sleep(Duration::from_millis(35));
                self.mem.free(payload_va).ok();
                return Err(ExecError::Timeout(HANDSHAKE_TIMEOUT));
            }
            std::thread::sleep(POLL_INTERVAL);
        }
        self.mem.free(payload_va)?;
        debug!(payload_va = format!("0x{payload_va:x}"), elapsed = ?start.elapsed(), "payload consumed");
        Ok(())
    }

    // ------------------------------------------------------------------

    fn build_stub(&self, stub_va: u32) -> Result<Vec<u8>, IcedError> {
        let code_cave = self.code_cave_ptr as u64;
        let back_va = self.detour_va.wrapping_add(self.overwritten.len() as u32);
        let seed = self.rng_seed;

        assemble_randomized(stub_va, seed, |rec| {
            rec.push(|a| a.pushfd());
            rec.push(|a| a.pushad());
            rec.push(move |a| a.mov(eax, dword_ptr(code_cave)));
            // Use conditional forward jump via a label to skip the call when
            // eax is zero.
            rec.push(move |a| {
                let mut skip = a.create_label();
                a.cmp(eax, 0)?;
                a.je(skip)?;
                a.call(eax)?;
                a.mov(eax, code_cave as u32)?;
                a.xor(edx, edx)?;
                a.mov(dword_ptr(eax), edx)?;
                a.set_label(&mut skip)?;
                a.nop()
            });
            rec.push(|a| a.popad());
            rec.push(|a| a.popfd());
            rec.push(move |a| a.jmp(back_va as u64));
            Ok(())
        })
    }

    fn build_hook_to_detour(&self) -> Result<Vec<u8>, IcedError> {
        let target = self.detour_ptr as u64;
        let hook_va = self.detour_va;
        let padding = self.overwritten.len();
        let mut a = CodeAssembler::new(32)?;
        a.jmp(target)?;
        let mut bytes = a.assemble(hook_va as u64)?;
        // Pad remainder with NOP (0x90) so the byte range we own is fully
        // occupied — matches BlackMagic behavior of writing exactly N bytes.
        while bytes.len() < padding {
            bytes.push(0x90);
        }
        assert!(bytes.len() <= padding,
            "hook jmp ({} bytes) exceeds overwritten window ({padding})", bytes.len());
        Ok(bytes)
    }

    fn next_seed(&mut self) -> u64 {
        self.rng_seed = self.rng_seed.wrapping_mul(6364136223846793005).wrapping_add(1442695040888963407);
        self.rng_seed
    }
}

fn seed_from_time() -> u64 {
    use std::time::SystemTime;
    SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_nanos() as u64)
        .unwrap_or(0xC0FFEE_DEAD_BEEF)
}
