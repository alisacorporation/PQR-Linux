//! Build the `PQR_AddAbility(...)` Lua fragment from an `Ability`. Port of
//! `clsLua.AddAbility` — the exact same pattern of three function definitions
//! (pqrFunc / pqrFuncBefore / pqrFuncAfter) followed by the registration call.

use pqr_profile::{Ability, TargetKind};

pub fn add_ability_lua(rotation_number: u32, index: usize, a: &Ability) -> String {
    let test_fn = format!("function pqrFunc{index}() {} end", a.lua);
    let before_fn = format!(" function pqrFuncBefore{index}() {} end", a.lua_before);
    let after_fn  = format!(" function pqrFuncAfter{index}() {} end",  a.lua_after);

    // Ability name escapes: `\` -> `\\`, `"` -> `\"`. Matches C#.
    let escaped_name = a.name.replace('\\', "\\\\").replace('"', "\\\"");
    let target = target_str(&a.target);
    let cancel = if a.cancel_channel { "true" } else { "false" };
    let actions = a.actions.replace('\\', "\\\\").replace('"', "\\\"");
    let call = format!(
        "PQR_AddAbility({rot}, {idx}, \"{name}\", {spid}, \"{actn}\", pqrFunc{idx}, {recast}, \"{tgt}\", {cancel}, pqrFuncBefore{idx}, pqrFuncAfter{idx})",
        rot = rotation_number, idx = index, name = escaped_name,
        spid = a.spell_id, actn = actions, recast = a.recast_delay,
        tgt = target, cancel = cancel,
    );
    format!("{test_fn} {before_fn} {after_fn} {call}")
}

fn target_str(t: &TargetKind) -> &'static str {
    // clsLua lowercases the target string; matches PQR_ExecuteBot's dispatch.
    match t {
        TargetKind::Target => "target",
        TargetKind::Mouseover => "mouseover",
        TargetKind::Click => "click",
        TargetKind::Player => "player",
        TargetKind::Focus => "focus",
        TargetKind::Custom => "custom",
        TargetKind::Other(_) => "target",
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn escapes_quotes_and_backslashes_in_name() {
        let a = Ability {
            name: r#"F:Say "hi" \o/"#.into(),
            default: false, spell_id: 42,
            actions: String::new(),
            lua: "return true".into(),
            recast_delay: 0, target: TargetKind::Target, cancel_channel: false,
            lua_before: String::new(), lua_after: String::new(),
        };
        let out = add_ability_lua(0, 3, &a);
        assert!(out.contains(r#""F:Say \"hi\" \\o/""#), "actual:\n{out}");
        assert!(out.contains("pqrFunc3"));
        assert!(out.contains("PQR_AddAbility(0, 3,"));
    }
}
