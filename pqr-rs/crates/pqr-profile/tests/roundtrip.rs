//! Integration tests against all real profiles in `PQR_fixed/Profiles/`.
//!
//! Semantic round-trip: parse -> serialize -> parse -> equal to first parse.
//! Byte-exact round-trip is intentionally NOT asserted: legacy profiles mix
//! double-escaped and single-escaped entities (see escape.rs docs), and the
//! writer always emits the double-escaped canonical form PQR itself produces.

use std::path::PathBuf;

fn profiles_dir() -> PathBuf {
    // pqr-rs/crates/pqr-profile/tests/ -> ../../../ = pqr-rs/, then ../ = repo root.
    PathBuf::from(env!("CARGO_MANIFEST_DIR"))
        .join("../../..")
        .join("PQR_fixed/Profiles")
        .canonicalize()
        .expect("PQR_fixed/Profiles must exist relative to crate")
}

fn all_files_matching(suffix: &str) -> Vec<PathBuf> {
    std::fs::read_dir(profiles_dir())
        .expect("read Profiles dir")
        .filter_map(Result::ok)
        .map(|e| e.path())
        .filter(|p| p.file_name().and_then(|n| n.to_str()).map_or(false, |n| n.ends_with(suffix)))
        .collect()
}

#[test]
fn parse_all_abilities_files() {
    let files = all_files_matching("_Abilities.xml");
    assert!(!files.is_empty(), "expected some abilities XML files");
    for path in files {
        let bytes = std::fs::read(&path).unwrap();
        let (root, abilities) = pqr_profile::parse_abilities(&bytes)
            .unwrap_or_else(|e| panic!("parse {} failed: {e}", path.display()));
        assert!(!root.is_empty(), "empty root in {}", path.display());
        assert!(!abilities.is_empty(), "no abilities in {}", path.display());
    }
}

#[test]
fn parse_all_rotations_files() {
    let files = all_files_matching("_Rotations.xml");
    assert!(!files.is_empty(), "expected some rotations XML files");
    for path in files {
        let bytes = std::fs::read(&path).unwrap();
        let (root, rotations) = pqr_profile::parse_rotations(&bytes)
            .unwrap_or_else(|e| panic!("parse {} failed: {e}", path.display()));
        assert!(!root.is_empty(), "empty root in {}", path.display());
        assert!(!rotations.is_empty(), "no rotations in {}", path.display());
    }
}

#[test]
fn semantic_roundtrip_abilities() {
    for path in all_files_matching("_Abilities.xml") {
        let bytes = std::fs::read(&path).unwrap();
        let (root, first) = pqr_profile::parse_abilities(&bytes).unwrap();
        let written = pqr_profile::write::write_abilities(&root, &first);
        let (root2, second) = pqr_profile::parse_abilities(written.as_bytes())
            .unwrap_or_else(|e| panic!("reparse failed for {}: {e}", path.display()));
        assert_eq!(root, root2);
        assert_eq!(first, second, "semantic round-trip drift in {}", path.display());
    }
}

#[test]
fn semantic_roundtrip_rotations() {
    for path in all_files_matching("_Rotations.xml") {
        let bytes = std::fs::read(&path).unwrap();
        let (root, first) = pqr_profile::parse_rotations(&bytes).unwrap();
        let written = pqr_profile::write::write_rotations(&root, &first);
        let (root2, second) = pqr_profile::parse_rotations(written.as_bytes())
            .unwrap_or_else(|e| panic!("reparse failed for {}: {e}", path.display()));
        assert_eq!(root, root2);
        assert_eq!(first, second, "semantic round-trip drift in {}", path.display());
    }
}

#[test]
fn resolve_reports_missing_names() {
    use pqr_profile::model::{Ability, Rotation, TargetKind};
    let abilities = vec![Ability {
        name: "F:Slam".into(),
        default: false,
        spell_id: 47475,
        actions: String::new(),
        lua: String::new(),
        recast_delay: 0,
        target: TargetKind::Target,
        cancel_channel: false,
        lua_before: String::new(),
        lua_after: String::new(),
    }];
    let rotation = Rotation {
        name: "Test".into(),
        default: false,
        priority: vec!["F:Slam".into(), "F:Missing".into(), "AlsoMissing".into()],
        require_combat: false,
        notes: String::new(),
    };
    let err = rotation.resolve(&abilities).unwrap_err();
    assert_eq!(err.len(), 2);
    assert_eq!(err[0].missing, "F:Missing");
    assert_eq!(err[1].missing, "AlsoMissing");
}

#[test]
fn all_real_rotations_resolve() {
    // Currently expected to have SOME unresolved names (that's the whole point
    // of the AddAbilityToCurrent silent-drop trap the plan aims to eliminate).
    // This test enumerates them so we can see the baseline.
    for path in all_files_matching("_Rotations.xml") {
        let class = path.file_name().unwrap().to_str().unwrap()
            .split('_').nth(1).unwrap().to_string();
        let ab_path = path.with_file_name(format!(
            "{}_{}_Abilities.xml",
            path.file_name().unwrap().to_str().unwrap().split('_').next().unwrap(),
            class,
        ));
        let (_, abilities) = pqr_profile::parse_abilities(&std::fs::read(&ab_path).unwrap()).unwrap();
        let (_, rotations) = pqr_profile::parse_rotations(&std::fs::read(&path).unwrap()).unwrap();
        for r in &rotations {
            if let Err(missing) = r.resolve(&abilities) {
                eprintln!("{} :: {}: {} missing", path.file_name().unwrap().to_string_lossy(), r.name, missing.len());
                for m in missing.iter().take(5) {
                    eprintln!("    - {:?}", m.missing);
                }
            }
        }
    }
}
