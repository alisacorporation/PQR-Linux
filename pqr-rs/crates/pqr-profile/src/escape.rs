//! Legacy PQR double-escape workaround.
//!
//! Standard XML escaping is handled by `quick-xml` on both sides (Writer
//! escapes on write, `Text::unescape` on read) — we don't reimplement it.
//!
//! What we still need is one extra unescape pass on read to undo legacy
//! PQR's `clsXML.XMLEncode` bug (`reversed/.../clsXML.cs:567`), which
//! escaped `&` *after* the other four entities and thus wrote sequences
//! like `&amp;quot;` to disk. quick-xml strips the outer `&amp;`, leaving
//! `&quot;` still in the text — this pass strips that residual layer.
//!
//! Files we write ourselves are single-escaped and pass through this
//! function unchanged (no `&name;` sequences left after quick-xml's parse).

/// Idempotent second-pass unescape. Recognises the five entities PQR uses;
/// leaves everything else untouched.
pub fn unescape_legacy(input: &str) -> String {
    let mut out = String::with_capacity(input.len());
    let mut rest = input;
    while let Some(amp) = rest.find('&') {
        out.push_str(&rest[..amp]);
        let after = &rest[amp + 1..];
        let (ch, tail) = match after.find(';').map(|end| (&after[..end], &after[end + 1..])) {
            Some(("amp", t)) => ('&', t),
            Some(("lt", t)) => ('<', t),
            Some(("gt", t)) => ('>', t),
            Some(("quot", t)) => ('"', t),
            Some(("apos", t)) => ('\'', t),
            _ => { out.push('&'); rest = after; continue; }
        };
        out.push(ch);
        rest = tail;
    }
    out.push_str(rest);
    out
}

#[cfg(test)]
mod tests {
    use super::unescape_legacy;

    #[test]
    fn strips_residual_layer_from_legacy_double_escape() {
        assert_eq!(unescape_legacy("&quot;foo&quot;"), r#""foo""#);
        assert_eq!(unescape_legacy("a &lt;= b"), "a <= b");
    }

    #[test]
    fn passes_clean_text_through() {
        assert_eq!(unescape_legacy(r#"if x == "y" then"#), r#"if x == "y" then"#);
        assert_eq!(unescape_legacy("plain"), "plain");
    }

    #[test]
    fn leaves_unknown_entities_alone() {
        assert_eq!(unescape_legacy("&nbsp;"), "&nbsp;");
        assert_eq!(unescape_legacy("cash & carry"), "cash & carry");
    }
}
