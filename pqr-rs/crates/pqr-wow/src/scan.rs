//! Pattern scanning over the target's mapped image — port of
//! `SPattern.FindPattern` (`reversed/blackmagic/Magic/SPattern.cs`).
//!
//! The shipped offsets XML predates `<ClntObjMgrGetActivePlayerObjAddress>`,
//! so — exactly like `frmMain` does when the field is zero — we resolve that
//! thunk by scanning the module for its prologue pattern at attach time.

use std::io;

use pqr_mem::ProcessMemory;

/// `E8 ?? ?? ?? 00 68 ?? 00` — a `call rel32` whose high byte is 0 followed
/// by a `push imm32` whose high byte is 0. First match in RVA order is the
/// `ClntObjMgrGetActivePlayerObj` wrapper. Port of
/// `clsOffsets.ClntObjMgrSearch` / `ClntObjMgrMask`.
pub const CLNT_OBJ_MGR_PATTERN: [u8; 8] = [0xE8, 0, 0, 0, 0, 0x68, 0, 0];
pub const CLNT_OBJ_MGR_MASK: &str = "x???xx?x";

/// Mask semantics of `SPattern`: `x` = byte must equal the pattern,
/// `!` = byte must differ, any other character = position ignored.
/// Returns the offset of the first match (as SPattern does).
pub fn find_pattern(data: &[u8], pattern: &[u8], mask: &str) -> Option<u32> {
    if pattern.is_empty() || pattern.len() != mask.len() || data.len() < pattern.len() {
        return None;
    }
    let mask = mask.as_bytes();
    for i in 0..=(data.len() - pattern.len()) {
        let mut matched = true;
        for j in 0..pattern.len() {
            let hit = pattern[j] == data[i + j];
            match mask[j] {
                b'x' if !hit => { matched = false; break; }
                b'!' if hit => { matched = false; break; }
                _ => {}
            }
        }
        if matched {
            return Some(i as u32);
        }
    }
    None
}

/// Resolve the ClntObjMgr thunk RVA by scanning the mapped image, or `0`
/// when the pattern isn't found (callers then treat the health probe as
/// unavailable instead of guessing at an address).
pub fn resolve_clnt_obj_mgr(mem: &dyn ProcessMemory) -> io::Result<u32> {
    let base = mem.image_base();
    let size = size_of_image(mem)? as usize;
    let data = mem.read_bytes(base, size)?;
    Ok(find_pattern(&data, &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK).unwrap_or(0))
}

/// `SizeOfImage` from the remote PE header — the same value SPattern gets
/// from `ProcessModule.ModuleMemorySize`.
fn size_of_image(mem: &dyn ProcessMemory) -> io::Result<u32> {
    let hdr = mem.read_bytes(mem.image_base(), 0x400)?;
    pe_size_of_image(&hdr)
        .ok_or_else(|| io::Error::other("PE header not readable (SizeOfImage missing)"))
}

fn pe_size_of_image(hdr: &[u8]) -> Option<u32> {
    if hdr.len() < 0x40 || &hdr[0..2] != b"MZ" {
        return None;
    }
    let lfanew = u32::from_le_bytes(hdr[0x3C..0x40].try_into().ok()?) as usize;
    let opt = lfanew.checked_add(24)?;
    let size_at = opt.checked_add(56)?; // OptionalHeader.SizeOfImage (PE32 and PE32+)
    let end = size_at.checked_add(4)?;
    if hdr.len() < end || &hdr[lfanew..lfanew.checked_add(4)?] != b"PE\0\0" {
        return None;
    }
    Some(u32::from_le_bytes(hdr[size_at..end].try_into().ok()?))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn finds_first_match_with_mask_semantics() {
        let data = [0x90, 0xE8, 0x11, 0x22, 0x33, 0x00, 0x68, 0x05, 0x00, 0x90];
        assert_eq!(
            find_pattern(&data, &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK),
            Some(1),
        );
    }

    #[test]
    fn mask_question_marks_ignore_position() {
        // Wildcards differ from the pattern everywhere `?` is set.
        let data = [0xE8, 0xFF, 0xEE, 0xDD, 0x00, 0x68, 0xAA, 0x00];
        assert_eq!(find_pattern(&data, &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK), Some(0));
    }

    #[test]
    fn mask_bang_requires_mismatch() {
        let pat = [0x01, 0x02];
        // '!' at pos 0: bytes must DIFFER; 'x' at pos 1: must match.
        assert_eq!(find_pattern(&[0x09, 0x02], &pat, "!x"), Some(0));
        assert_eq!(find_pattern(&[0x01, 0x02], &pat, "!x"), None); // pos 0 equal
        assert_eq!(find_pattern(&[0x09, 0x03], &pat, "!x"), None); // pos 1 differs
    }

    #[test]
    fn no_match_returns_none() {
        let data = [0u8; 64];
        assert_eq!(find_pattern(&data, &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK), None);
        assert_eq!(find_pattern(&data, &[], ""), None);
        assert_eq!(find_pattern(&[0xE8], &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK), None);
    }

    #[test]
    fn match_at_the_very_end_is_found() {
        let mut data = vec![0u8; 8];
        data.extend_from_slice(&CLNT_OBJ_MGR_PATTERN);
        assert_eq!(find_pattern(&data, &CLNT_OBJ_MGR_PATTERN, CLNT_OBJ_MGR_MASK), Some(8));
    }

    #[test]
    fn parses_size_of_image_from_synthetic_pe() {
        let mut hdr = vec![0u8; 0x400];
        hdr[0..2].copy_from_slice(b"MZ");
        hdr[0x3C..0x40].copy_from_slice(&0x80u32.to_le_bytes());
        hdr[0x80..0x84].copy_from_slice(b"PE\0\0");
        let size_at = 0x80 + 24 + 56;
        hdr[size_at..size_at + 4].copy_from_slice(&0x007A_1200u32.to_le_bytes());
        assert_eq!(pe_size_of_image(&hdr), Some(0x007A_1200));
        assert_eq!(pe_size_of_image(&[0u8; 16]), None);
    }
}
