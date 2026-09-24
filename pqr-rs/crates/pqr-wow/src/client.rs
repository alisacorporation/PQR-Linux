//! Live view of the attached WoW process. Wraps a `ProcessMemory` with
//! offsets-aware accessors.

use std::io;
use std::sync::Arc;

use pqr_mem::ProcessMemory;

use crate::offsets::Offsets;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum WowClass {
    Warrior, Paladin, Hunter, Rogue, Priest, DeathKnight,
    Shaman, Mage, Warlock, Druid, Unknown(u8),
}

impl WowClass {
    fn from_byte(b: u8) -> Self {
        match b {
            1 => Self::Warrior, 2 => Self::Paladin, 3 => Self::Hunter,
            4 => Self::Rogue, 5 => Self::Priest, 6 => Self::DeathKnight,
            7 => Self::Shaman, 8 => Self::Mage, 9 => Self::Warlock,
            11 => Self::Druid, other => Self::Unknown(other),
        }
    }

    pub fn as_str(&self) -> &'static str {
        match self {
            Self::Warrior => "WARRIOR", Self::Paladin => "PALADIN",
            Self::Hunter => "HUNTER", Self::Rogue => "ROGUE",
            Self::Priest => "PRIEST", Self::DeathKnight => "DEATHKNIGHT",
            Self::Shaman => "SHAMAN", Self::Mage => "MAGE",
            Self::Warlock => "WARLOCK", Self::Druid => "DRUID",
            Self::Unknown(_) => "UNKNOWN",
        }
    }
}

pub struct WowClient {
    pub mem: Arc<dyn ProcessMemory>,
    pub offsets: Offsets,
}

impl WowClient {
    pub fn new(mem: Arc<dyn ProcessMemory>, offsets: Offsets) -> Self {
        Self { mem, offsets }
    }

    fn addr(&self, rva: u32) -> u32 { self.mem.image_base().wrapping_add(rva) }

    pub fn player_name(&self) -> io::Result<String> {
        self.mem.read_utf8_string(self.addr(self.offsets.player_name), 30)
    }

    pub fn player_class(&self) -> io::Result<WowClass> {
        let bytes = self.mem.read_bytes(self.addr(self.offsets.player_class), 1)?;
        Ok(WowClass::from_byte(*bytes.first().unwrap_or(&0)))
    }

    /// True while the player is fully in the world (as opposed to login /
    /// character-select). Mirrors `clsMemory.IsWoWReady` (with the 12340
    /// nonzero-normalize quirk).
    pub fn is_in_world(&self) -> io::Result<bool> {
        if self.offsets.game_state == 0 { return Ok(true); }
        let mut n = self.mem.read_i32(self.addr(self.offsets.game_state))?;
        if self.offsets.version <= 12340 && n != 0 { n = 1; }
        Ok(n == 1)
    }

    /// True when the world (not the chat/editbox) has keyboard focus, i.e.
    /// safe to inject typing-sensitive Lua. Mirrors `clsMemory.WorldHasFocus`.
    pub fn world_has_focus(&self) -> io::Result<bool> {
        Ok(self.mem.read_u32(self.addr(self.offsets.keyboard_focus))? == 0)
    }

    /// Read the 5-byte build string at `wow_version_offset` (e.g. `"12340"`).
    pub fn version_string(&self) -> io::Result<String> {
        self.mem.read_utf8_string(self.addr(self.offsets.wow_version_offset), 5)
    }
}
