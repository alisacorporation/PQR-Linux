//! Offsets XML — same schema PQR uses (`PQR_fixed/Offsets_12340.xml`).
//! All addresses are RVAs; absolute VA = `image_base + rva`.

use quick_xml::events::Event;
use quick_xml::Reader;

#[derive(Debug, thiserror::Error)]
pub enum OffsetsError {
    #[error("xml: {0}")]
    Xml(#[from] quick_xml::Error),
    #[error("utf8: {0}")]
    Utf8(#[from] std::str::Utf8Error),
    #[error("missing field <{0}> in offsets XML")]
    Missing(&'static str),
    #[error("invalid hex value for <{field}>: {value}")]
    BadHex { field: &'static str, value: String },
    #[error("invalid version <CurrentWoWVersion>: {0}")]
    BadVersion(String),
    #[error("invalid Overwritten byte sequence: {0}")]
    BadOverwritten(String),
}

/// Everything the executor and the discovery loop need to attach to WoW.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct Offsets {
    pub version: u32,               // build number, also matched as ASCII at wow_version_offset
    pub wow_version_offset: u32,
    pub player_name: u32,
    pub player_class: u32,
    pub keyboard_focus: u32,
    pub game_state: u32,
    pub lua_do_string: u32,
    pub lua_get_localized_text: u32,
    pub clnt_obj_mgr_get_active_player: u32,
    pub detour: u32,
    pub overwritten: Vec<u8>,       // typically 9 bytes; restored on unhook
}

pub fn parse(bytes: &[u8]) -> Result<Offsets, OffsetsError> {
    let mut r = Reader::from_reader(bytes);
    r.config_mut().trim_text(true);

    let mut buf = Vec::new();
    let mut current: Option<String> = None;
    let mut fields: std::collections::HashMap<String, String> = Default::default();

    loop {
        match r.read_event_into(&mut buf)? {
            Event::Start(e) => {
                current = Some(std::str::from_utf8(e.name().as_ref())?.to_string());
            }
            Event::Text(t) => {
                if let Some(tag) = &current {
                    let txt = t.unescape()?.to_string();
                    fields.entry(tag.clone()).or_insert(txt);
                }
            }
            Event::End(_) => current = None,
            Event::Eof => break,
            _ => {}
        }
        buf.clear();
    }

    let get = |k: &'static str| fields.get(k).cloned().ok_or(OffsetsError::Missing(k));

    let version = get("CurrentWoWVersion")?
        .trim()
        .parse::<u32>()
        .map_err(|_| OffsetsError::BadVersion(get("CurrentWoWVersion").unwrap_or_default()))?;

    Ok(Offsets {
        version,
        wow_version_offset: hex("WoWVersionOffset", &get("WoWVersionOffset")?)?,
        player_name: hex("PlayerName", &get("PlayerName")?)?,
        player_class: hex("PlayerClass", &get("PlayerClass")?)?,
        keyboard_focus: hex("GetCurrentKeyBoardFocus", &get("GetCurrentKeyBoardFocus")?)?,
        game_state: hex("GameState", &get("GameState")?)?,
        lua_do_string: hex("Lua_DoStringAddress", &get("Lua_DoStringAddress")?)?,
        lua_get_localized_text: hex("Lua_GetLocalizedTextAddress", &get("Lua_GetLocalizedTextAddress")?)?,
        clnt_obj_mgr_get_active_player: fields
            .get("ClntObjMgrGetActivePlayerObjAddress")
            .map(|v| hex("ClntObjMgrGetActivePlayerObjAddress", v))
            .transpose()?
            .unwrap_or(0),
        detour: hex("Detour", &get("Detour")?)?,
        overwritten: parse_byte_sequence(&get("Overwritten")?)?,
    })
}

fn hex(field: &'static str, value: &str) -> Result<u32, OffsetsError> {
    let stripped = value.trim().trim_start_matches("0x").trim_start_matches("0X");
    u32::from_str_radix(stripped, 16)
        .map_err(|_| OffsetsError::BadHex { field, value: value.to_string() })
}

fn parse_byte_sequence(s: &str) -> Result<Vec<u8>, OffsetsError> {
    s.split_ascii_whitespace()
        .map(|tok| {
            let stripped = tok.trim_start_matches("0x").trim_start_matches("0X");
            u8::from_str_radix(stripped, 16)
        })
        .collect::<Result<Vec<_>, _>>()
        .map_err(|_| OffsetsError::BadOverwritten(s.to_string()))
}

#[cfg(test)]
mod tests {
    use super::*;

    const SAMPLE: &[u8] = include_bytes!("../../../../PQR_fixed/Offsets_12340.xml");

    #[test]
    fn parse_shipped_offsets() {
        let o = parse(SAMPLE).unwrap();
        assert_eq!(o.version, 12340);
        assert_eq!(o.wow_version_offset, 0x8AD851);
        assert_eq!(o.player_name, 0x879D18);
        assert_eq!(o.detour, 0xBF0F0);
        assert_eq!(o.overwritten, vec![0x55, 0x8B, 0xEC, 0x81, 0xEC, 0xF8, 0x00, 0x00, 0x00]);
    }
}
