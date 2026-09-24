//! 32-bit x86 codegen for the detour stub and Lua-call payloads.
//!
//! WoW 3.3.5a is a 32-bit PE, so every address baked into an injected payload
//! is a `u32`. Assembly is built with `iced-x86`'s `CodeAssembler` — no strings,
//! no external FASM binary.
//!
//! ## Anti-signature randomization
//!
//! Ports `Executor.RandomizeASM` (see `reversed/.../Executor.cs:130-158`):
//! before every real instruction, 1..=4 no-op-ish "junk" instructions are
//! inserted (register-preserving `mov r,r` / `push r;pop r` / `xchg r,r` /
//! `nop`), so the emitted byte sequence differs each session.

use iced_x86::code_asm::*;
use iced_x86::IcedError;
use rand::seq::SliceRandom;
use rand::{Rng, SeedableRng};
use rand::rngs::StdRng;

/// Assemble a 32-bit payload. `base` is the target address the code will be
/// loaded at (used for RIP-relative and absolute reference calculation).
pub fn assemble(base: u32, build: impl FnOnce(&mut CodeAssembler) -> Result<(), IcedError>)
    -> Result<Vec<u8>, IcedError>
{
    let mut a = CodeAssembler::new(32)?;
    build(&mut a)?;
    a.assemble(base as u64)
}

/// Same as `assemble`, but inserts 1..=4 junk instructions before every real
/// instruction emitted by `build`. To do this cleanly we let `build` populate
/// a `Recorder`, then replay it through a real `CodeAssembler` with junk
/// interleaved.
pub fn assemble_randomized(
    base: u32,
    seed: u64,
    build: impl FnOnce(&mut Recorder) -> Result<(), IcedError>,
) -> Result<Vec<u8>, IcedError> {
    let mut rec = Recorder::default();
    build(&mut rec)?;
    let mut rng = StdRng::seed_from_u64(seed);
    let mut a = CodeAssembler::new(32)?;
    for step in rec.steps {
        let n = rng.gen_range(1..=4);
        for _ in 0..n {
            emit_junk(&mut a, &mut rng)?;
        }
        step(&mut a)?;
    }
    a.assemble(base as u64)
}

type Step = Box<dyn FnOnce(&mut CodeAssembler) -> Result<(), IcedError>>;

/// Deferred instruction stream. Callers push closures rather than emitting to
/// a live assembler, so we can splice junk in between later.
#[derive(Default)]
pub struct Recorder {
    steps: Vec<Step>,
}

impl Recorder {
    pub fn push<F>(&mut self, f: F)
    where F: FnOnce(&mut CodeAssembler) -> Result<(), IcedError> + 'static
    {
        self.steps.push(Box::new(f));
    }
}

fn emit_junk(a: &mut CodeAssembler, rng: &mut StdRng) -> Result<(), IcedError> {
    // Register-preserving no-ops. Same spirit as RandomAsmCode in Executor.cs.
    const CHOICES: &[fn(&mut CodeAssembler) -> Result<(), IcedError>] = &[
        |a| a.nop(),
        |a| a.mov(eax, eax),
        |a| a.mov(ebx, ebx),
        |a| a.mov(ecx, ecx),
        |a| a.mov(edx, edx),
        |a| a.mov(esi, esi),
        |a| a.mov(edi, edi),
        |a| a.mov(ebp, ebp),
        |a| { a.push(eax)?; a.pop(eax) },
        |a| { a.push(ebx)?; a.pop(ebx) },
        |a| { a.push(ecx)?; a.pop(ecx) },
        |a| { a.push(edx)?; a.pop(edx) },
        |a| { a.push(esi)?; a.pop(esi) },
        |a| { a.push(edi)?; a.pop(edi) },
        |a| a.xchg(eax, eax),
        |a| a.xchg(ebx, ebx),
        |a| a.xchg(ecx, ecx),
        |a| a.xchg(edx, edx),
    ];
    let choice = CHOICES.choose(rng).unwrap();
    choice(a)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn plain_stub_bytes_are_deterministic() {
        let bytes = assemble(0x1000, |a| {
            a.pushfd()?;
            a.pushad()?;
            a.nop()?;
            a.popad()?;
            a.popfd()?;
            a.ret()
        }).unwrap();
        // pushfd=9C pushad=60 nop=90 popad=61 popfd=9D ret=C3
        assert_eq!(bytes, [0x9C, 0x60, 0x90, 0x61, 0x9D, 0xC3]);
    }

    #[test]
    fn randomized_is_larger_but_semantically_extends_original() {
        let plain = assemble(0x1000, |a| { a.nop() }).unwrap();
        let random = assemble_randomized(0x1000, 42, |r| {
            r.push(|a| a.nop());
            Ok(())
        }).unwrap();
        assert!(random.len() > plain.len());
        // Real nop is the last byte in the randomized stream (assuming junk
        // doesn't happen to end in 0x90 by coincidence — with seed 42 the tail
        // is our explicit nop).
        assert_eq!(*random.last().unwrap(), 0x90);
    }

    #[test]
    fn different_seeds_produce_different_bytes() {
        let a = assemble_randomized(0x1000, 1, |r| {
            r.push(|a| a.nop());
            r.push(|a| a.nop());
            Ok(())
        }).unwrap();
        let b = assemble_randomized(0x1000, 999, |r| {
            r.push(|a| a.nop());
            r.push(|a| a.nop());
            Ok(())
        }).unwrap();
        assert_ne!(a, b);
    }
}
