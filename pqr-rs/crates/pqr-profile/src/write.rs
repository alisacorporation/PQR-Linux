//! Serializer for the two PQR XML formats.
//!
//! Delegates all XML text escaping to `quick-xml::Writer` — we don't touch
//! `&`/`<`/`>`/`"`/`'` by hand. Output matches PQR's expectations: single-line
//! document, `<?xml version="1.0" encoding="utf-8" ?>` prolog, no trailing
//! newline. The prolog space before `?>` is preserved to match the original
//! writer's output.

use quick_xml::events::{BytesDecl, BytesEnd, BytesStart, BytesText, Event};
use quick_xml::Writer;

use crate::model::{Ability, Rotation};

const PROLOG: &[u8] = b"<?xml version=\"1.0\" encoding=\"utf-8\" ?>";

fn open<W: std::io::Write>(w: &mut Writer<W>, tag: &str) -> quick_xml::Result<()> {
    w.write_event(Event::Start(BytesStart::new(tag)))
}

fn close<W: std::io::Write>(w: &mut Writer<W>, tag: &str) -> quick_xml::Result<()> {
    w.write_event(Event::End(BytesEnd::new(tag)))
}

fn field<W: std::io::Write>(w: &mut Writer<W>, tag: &str, value: &str) -> quick_xml::Result<()> {
    open(w, tag)?;
    // BytesText::new escapes reserved characters automatically.
    w.write_event(Event::Text(BytesText::new(value)))?;
    close(w, tag)?;
    Ok(())
}

fn write_document<W, F>(class: &str, body: F) -> quick_xml::Result<Vec<u8>>
where
    W: std::io::Write,
    F: FnOnce(&mut Writer<Vec<u8>>) -> quick_xml::Result<()>,
{
    let mut buf = Vec::new();
    // Emit the exact prolog PQR writes (BytesDecl formats without the trailing
    // space, so we hand-roll it).
    buf.extend_from_slice(PROLOG);
    let mut w = Writer::new(buf);
    open(&mut w, class)?;
    body(&mut w)?;
    close(&mut w, class)?;
    Ok(w.into_inner())
}

pub fn write_abilities(class: &str, abilities: &[Ability]) -> String {
    let bytes = write_document::<Vec<u8>, _>(class, |w| {
        for a in abilities {
            open(w, "Ability")?;
            field(w, "Name", &a.name)?;
            field(w, "Default", bool_str(a.default))?;
            field(w, "SpellID", &a.spell_id.to_string())?;
            field(w, "Actions", &a.actions)?;
            field(w, "Lua", &a.lua)?;
            field(w, "RecastDelay", &a.recast_delay.to_string())?;
            field(w, "Target", a.target.as_str())?;
            field(w, "CancelChannel", bool_str_cap(a.cancel_channel))?;
            field(w, "LuaBefore", &a.lua_before)?;
            field(w, "LuaAfter", &a.lua_after)?;
            close(w, "Ability")?;
        }
        Ok(())
    })
    .expect("in-memory writer never fails");
    String::from_utf8(bytes).expect("utf-8 in, utf-8 out")
}

pub fn write_rotations(class: &str, rotations: &[Rotation]) -> String {
    let bytes = write_document::<Vec<u8>, _>(class, |w| {
        for r in rotations {
            open(w, "Rotation")?;
            field(w, "RotationName", &r.name)?;
            field(w, "RotationDefault", bool_str(r.default))?;
            field(w, "RotationList", &r.priority.join("|"))?;
            field(w, "RequireCombat", bool_str(r.require_combat))?;
            field(w, "RotationNotes", &r.notes)?;
            close(w, "Rotation")?;
        }
        Ok(())
    })
    .expect("in-memory writer never fails");
    String::from_utf8(bytes).expect("utf-8 in, utf-8 out")
}

/// Legacy PQR writes ability booleans lowercase but `CancelChannel` capitalised.
fn bool_str(b: bool) -> &'static str { if b { "true" } else { "false" } }
fn bool_str_cap(b: bool) -> &'static str { if b { "True" } else { "False" } }

// Silence unused-imports until we wire the trait-object variant of write_document.
#[allow(dead_code)]
fn _decl_reference(_: BytesDecl<'_>) {}
