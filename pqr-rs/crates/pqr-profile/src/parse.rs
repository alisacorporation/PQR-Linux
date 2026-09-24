//! Event-driven parser for the two PQR XML formats.
//!
//! Standard XML escaping is undone by quick-xml. On top of that we apply
//! `unescape_legacy` to strip the residual layer legacy PQR files left in
//! the text (see `clsXML.cs:567`).

use quick_xml::events::Event;
use quick_xml::Reader;

use crate::escape::unescape_legacy;
use crate::model::{Ability, Profile, Rotation, TargetKind};
use std::str::FromStr;

#[derive(Debug, thiserror::Error)]
pub enum ProfileError {
    #[error("xml parse: {0}")]
    Xml(#[from] quick_xml::Error),
    #[error("xml attribute: {0}")]
    Attr(#[from] quick_xml::events::attributes::AttrError),
    #[error("utf8: {0}")]
    Utf8(#[from] std::str::Utf8Error),
    #[error("invalid integer for <{field}>: {value}")]
    BadInt { field: &'static str, value: String },
    #[error("unexpected end of document")]
    UnexpectedEof,
    #[error("root element missing")]
    NoRoot,
}

fn parse_bool(s: &str) -> bool {
    matches!(s.trim().to_ascii_lowercase().as_str(), "true" | "1" | "yes")
}

fn parse_u32(field: &'static str, s: &str) -> Result<u32, ProfileError> {
    s.trim().parse::<u32>().map_err(|_| ProfileError::BadInt {
        field,
        value: s.to_string(),
    })
}

/// Parse `<CLASS><Ability>...</Ability>...</CLASS>` bytes.
pub fn parse_abilities(bytes: &[u8]) -> Result<(String, Vec<Ability>), ProfileError> {
    let mut reader = Reader::from_reader(bytes);
    reader.config_mut().trim_text(false);
    let mut buf = Vec::new();
    let mut root: Option<String> = None;
    let mut abilities = Vec::new();

    loop {
        match reader.read_event_into(&mut buf)? {
            Event::Start(e) => {
                let name = std::str::from_utf8(e.name().as_ref())?.to_string();
                if root.is_none() {
                    root = Some(name);
                } else if name == "Ability" {
                    abilities.push(read_ability(&mut reader)?);
                }
            }
            Event::Eof => break,
            _ => {}
        }
        buf.clear();
    }
    Ok((root.ok_or(ProfileError::NoRoot)?, abilities))
}

fn read_ability<R: std::io::BufRead>(reader: &mut Reader<R>) -> Result<Ability, ProfileError> {
    let mut a = Ability {
        name: String::new(),
        default: false,
        spell_id: 0,
        actions: String::new(),
        lua: String::new(),
        recast_delay: 0,
        target: TargetKind::Target,
        cancel_channel: false,
        lua_before: String::new(),
        lua_after: String::new(),
    };
    let mut buf = Vec::new();
    loop {
        match reader.read_event_into(&mut buf)? {
            Event::Start(e) => {
                let tag = std::str::from_utf8(e.name().as_ref())?.to_string();
                let text = read_text_until_close(reader, &tag)?;
                let decoded = unescape_legacy(&text);
                match tag.as_str() {
                    "Name" => a.name = decoded,
                    "Default" => a.default = parse_bool(&decoded),
                    "SpellID" => a.spell_id = parse_u32("SpellID", &decoded)?,
                    "Actions" => a.actions = decoded,
                    "Lua" => a.lua = decoded,
                    "RecastDelay" => a.recast_delay = parse_u32("RecastDelay", &decoded)?,
                    "Target" => a.target = TargetKind::from_str(&decoded).unwrap(),
                    "CancelChannel" => a.cancel_channel = parse_bool(&decoded),
                    "LuaBefore" => a.lua_before = decoded,
                    "LuaAfter" => a.lua_after = decoded,
                    _ => {}
                }
            }
            Event::Empty(e) => {
                // Self-closing element like <Actions/> means empty value; nothing to record.
                let _ = std::str::from_utf8(e.name().as_ref())?;
            }
            Event::End(e) => {
                if e.name().as_ref() == b"Ability" {
                    return Ok(a);
                }
            }
            Event::Eof => return Err(ProfileError::UnexpectedEof),
            _ => {}
        }
        buf.clear();
    }
}

/// Read all text content until the matching close tag. XML entities are
/// already unescaped by quick-xml; caller applies `unescape_legacy` for the
/// PQR-specific residual pass.
fn read_text_until_close<R: std::io::BufRead>(
    reader: &mut Reader<R>,
    tag: &str,
) -> Result<String, ProfileError> {
    let mut buf = Vec::new();
    let mut out = String::new();
    loop {
        match reader.read_event_into(&mut buf)? {
            Event::Text(t) => {
                out.push_str(&t.unescape()?);
            }
            Event::CData(c) => {
                out.push_str(std::str::from_utf8(&c)?);
            }
            Event::End(e) => {
                if e.name().as_ref() == tag.as_bytes() {
                    return Ok(out);
                }
            }
            Event::Eof => return Err(ProfileError::UnexpectedEof),
            _ => {}
        }
        buf.clear();
    }
}

/// Parse `<CLASS><Rotation>...</Rotation>...</CLASS>` bytes.
pub fn parse_rotations(bytes: &[u8]) -> Result<(String, Vec<Rotation>), ProfileError> {
    let mut reader = Reader::from_reader(bytes);
    reader.config_mut().trim_text(false);
    let mut buf = Vec::new();
    let mut root: Option<String> = None;
    let mut rotations = Vec::new();

    loop {
        match reader.read_event_into(&mut buf)? {
            Event::Start(e) => {
                let name = std::str::from_utf8(e.name().as_ref())?.to_string();
                if root.is_none() {
                    root = Some(name);
                } else if name == "Rotation" {
                    rotations.push(read_rotation(&mut reader)?);
                }
            }
            Event::Eof => break,
            _ => {}
        }
        buf.clear();
    }
    Ok((root.ok_or(ProfileError::NoRoot)?, rotations))
}

fn read_rotation<R: std::io::BufRead>(reader: &mut Reader<R>) -> Result<Rotation, ProfileError> {
    let mut r = Rotation {
        name: String::new(),
        default: false,
        priority: Vec::new(),
        require_combat: false,
        notes: String::new(),
    };
    let mut buf = Vec::new();
    loop {
        match reader.read_event_into(&mut buf)? {
            Event::Start(e) => {
                let tag = std::str::from_utf8(e.name().as_ref())?.to_string();
                let text = read_text_until_close(reader, &tag)?;
                let decoded = unescape_legacy(&text);
                match tag.as_str() {
                    "RotationName" => r.name = decoded,
                    "RotationDefault" => r.default = parse_bool(&decoded),
                    "RotationList" => {
                        r.priority = decoded
                            .split('|')
                            .map(|s| s.to_string())
                            .filter(|s| !s.is_empty())
                            .collect();
                    }
                    "RequireCombat" => r.require_combat = parse_bool(&decoded),
                    "RotationNotes" => r.notes = decoded,
                    _ => {}
                }
            }
            Event::End(e) => {
                if e.name().as_ref() == b"Rotation" {
                    return Ok(r);
                }
            }
            Event::Eof => return Err(ProfileError::UnexpectedEof),
            _ => {}
        }
        buf.clear();
    }
}

/// Convenience: load both files for a class from a Profiles directory.
pub fn load_profile(
    profiles_dir: &std::path::Path,
    prefix: &str,
    class: &str,
) -> Result<Profile, Box<dyn std::error::Error>> {
    let abilities_path = profiles_dir.join(format!("{prefix}_{class}_Abilities.xml"));
    let rotations_path = profiles_dir.join(format!("{prefix}_{class}_Rotations.xml"));
    let (root_a, abilities) = parse_abilities(&std::fs::read(&abilities_path)?)?;
    let (root_r, rotations) = parse_rotations(&std::fs::read(&rotations_path)?)?;
    if root_a != class || root_r != class {
        tracing_style_warn(&format!(
            "root mismatch: abilities=<{root_a}>, rotations=<{root_r}>, expected <{class}>"
        ));
    }
    Ok(Profile { class: class.to_string(), abilities, rotations })
}

fn tracing_style_warn(_s: &str) {}
