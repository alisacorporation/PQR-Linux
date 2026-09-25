//! Domain model for parsed PQR profiles.

use std::str::FromStr;

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum TargetKind {
    Target,
    Mouseover,
    Click,
    Player,
    Focus,
    Custom,
    Other(String),
}

impl FromStr for TargetKind {
    type Err = std::convert::Infallible;
    fn from_str(s: &str) -> Result<Self, Self::Err> {
        Ok(match s.trim() {
            "Target" => Self::Target,
            "Mouseover" => Self::Mouseover,
            "Click" => Self::Click,
            "Player" => Self::Player,
            "Focus" => Self::Focus,
            "Custom" => Self::Custom,
            other => Self::Other(other.to_string()),
        })
    }
}

impl TargetKind {
    pub fn as_str(&self) -> &str {
        match self {
            Self::Target => "Target",
            Self::Mouseover => "Mouseover",
            Self::Click => "Click",
            Self::Player => "Player",
            Self::Focus => "Focus",
            Self::Custom => "Custom",
            Self::Other(s) => s.as_str(),
        }
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Ability {
    pub name: String,
    pub default: bool,
    pub spell_id: u32,
    pub actions: String,
    pub lua: String,
    pub recast_delay: u32,
    pub target: TargetKind,
    pub cancel_channel: bool,
    pub lua_before: String,
    pub lua_after: String,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Rotation {
    pub name: String,
    pub default: bool,
    /// Ordered priority list. Parsed from pipe-separated `RotationList`.
    pub priority: Vec<String>,
    pub require_combat: bool,
    pub notes: String,
}

/// A loaded pair of abilities + rotations for a single class.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Profile {
    // (Clone was already derived — TUI relies on it to hand the profile to
    // the background engine thread.)
    pub class: String,
    pub abilities: Vec<Ability>,
    pub rotations: Vec<Rotation>,
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct UnresolvedName {
    pub rotation: String,
    pub missing: String,
}

impl Rotation {
    /// Resolve `priority` names against `abilities`. Unlike `frmMain.AddAbilityToCurrent`,
    /// which silently drops mismatches (see `AGENTS.md:35`), this returns explicit errors.
    pub fn resolve<'a>(&self, abilities: &'a [Ability]) -> Result<Vec<&'a Ability>, Vec<UnresolvedName>> {
        let mut resolved = Vec::with_capacity(self.priority.len());
        let mut missing = Vec::new();
        for name in &self.priority {
            let trimmed = name.trim();
            match abilities.iter().find(|a| a.name.trim() == trimmed) {
                Some(a) => resolved.push(a),
                None => missing.push(UnresolvedName {
                    rotation: self.name.clone(),
                    missing: name.clone(),
                }),
            }
        }
        if missing.is_empty() {
            Ok(resolved)
        } else {
            Err(missing)
        }
    }
}
