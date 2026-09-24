//! Anti-signature identifier renamer. Port of `clsMemory.ReplacePQR` /
//! `GetName` / `GetUBID` / `GetUDBID`.
//!
//! At construction we pick random alpha names for `PQR`, `UnitBuffID`,
//! `UnitDebuffID`. Every Lua fragment we inject passes through `apply()` so
//! the on-the-wire text differs each session — same anti-detection trick as
//! the original.

use rand::Rng;
use regex::Regex;

const ALPHABET: &[u8] = b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

pub struct IdentReplacer {
    pub pqr: String,
    pub unit_buff_id: String,
    pub unit_debuff_id: String,
    pqr_re: Regex,
    ubi_re: Regex,
    udbi_re: Regex,
}

impl IdentReplacer {
    pub fn random() -> Self {
        Self::with_rng(&mut rand::thread_rng())
    }

    /// Identity — leaves `pqr`/`UnitBuffID`/`UnitDebuffID` untouched. Used
    /// for debugging so probes from later invocations can find globals by
    /// their original names.
    pub fn identity() -> Self {
        Self::from_names("PQR".into(), "UnitBuffID".into(), "UnitDebuffID".into())
    }

    pub fn with_rng<R: Rng>(rng: &mut R) -> Self {
        Self::from_names(
            random_name(rng, 8),
            random_name(rng, 7),
            random_name(rng, 6),
        )
    }

    /// Explicit names — mainly for tests.
    pub fn from_names(pqr: String, ubi: String, udbi: String) -> Self {
        // `pqr` regex is case-insensitive in the original; the other two are
        // exact identifier matches.
        let pqr_re = Regex::new(r"(?i)pqr").expect("pqr regex compiles");
        let ubi_re = Regex::new(r"UnitBuffID").expect("UnitBuffID regex compiles");
        let udbi_re = Regex::new(r"UnitDebuffID").expect("UnitDebuffID regex compiles");
        Self { pqr, unit_buff_id: ubi, unit_debuff_id: udbi, pqr_re, ubi_re, udbi_re }
    }

    pub fn apply(&self, lua: &str) -> String {
        // Order matches the C# original: UnitDebuffID first (more specific),
        // then UnitBuffID, then pqr (case-insensitive).
        let s = self.udbi_re.replace_all(lua, self.unit_debuff_id.as_str());
        let s = self.ubi_re.replace_all(&s, self.unit_buff_id.as_str());
        self.pqr_re.replace_all(&s, self.pqr.as_str()).into_owned()
    }
}

fn random_name<R: Rng>(rng: &mut R, len: usize) -> String {
    // Letters only — original uses `ABCDEFGHIJKLMNOPQRSTUVWXYZabcdef...` (no digits).
    (0..len).map(|_| ALPHABET[rng.gen_range(0..ALPHABET.len())] as char).collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn replaces_all_three_identifiers() {
        let r = IdentReplacer::from_names("ZZZ".into(), "YYY".into(), "WWW".into());
        let out = r.apply("if UnitBuffID('player', 1) and UnitDebuffID('target', 2) then PQR_Debug('x') end");
        // UnitDebuffID -> WWW, UnitBuffID -> YYY, pqr -> ZZZ (case-insensitive; PQR_Debug's PQR gets renamed too)
        assert!(out.contains("WWW('target', 2)"));
        assert!(out.contains("YYY('player', 1)"));
        assert!(out.contains("ZZZ_Debug"));
    }

    #[test]
    fn random_names_are_letters_only() {
        let r = IdentReplacer::random();
        assert_eq!(r.pqr.len(), 8);
        assert_eq!(r.unit_buff_id.len(), 7);
        assert_eq!(r.unit_debuff_id.len(), 6);
        for name in [&r.pqr, &r.unit_buff_id, &r.unit_debuff_id] {
            assert!(name.chars().all(|c| c.is_ascii_alphabetic()), "bad name: {name}");
        }
    }
}
