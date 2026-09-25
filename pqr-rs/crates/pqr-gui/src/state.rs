//! Non-UI application state: profile discovery, load/save, hot-reload
//! watching, rename-with-reference-update, lint.

use std::collections::{HashMap, HashSet};
use std::path::{Path, PathBuf};
use std::time::SystemTime;

use pqr_profile::compare::{align_rotations, DiffOptions, DiffResult};
use pqr_profile::{
    load_profile, parse_abilities, parse_rotations, write_abilities, write_rotations, Profile,
};

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ProfileId {
    pub prefix: String,
    pub class: String,
}

impl ProfileId {
    pub fn label(&self) -> String {
        format!("{}_{}", self.prefix, self.class)
    }
    pub fn abilities_file(&self) -> String {
        format!("{}_{}_Abilities.xml", self.prefix, self.class)
    }
    pub fn rotations_file(&self) -> String {
        format!("{}_{}_Rotations.xml", self.prefix, self.class)
    }
}

/// Discover `{prefix}_{CLASS}_Abilities.xml` files that have a rotations
/// counterpart. Class = last `_` segment of the stem, prefix = the rest.
pub fn discover_profiles(dir: &Path) -> Vec<ProfileId> {
    let mut out = Vec::new();
    let Ok(rd) = std::fs::read_dir(dir) else { return out };
    for entry in rd.flatten() {
        let name = entry.file_name().to_string_lossy().into_owned();
        let Some(stem) = name.strip_suffix("_Abilities.xml") else { continue };
        let Some((prefix, class)) = stem.rsplit_once('_') else { continue };
        if prefix.is_empty() || class.is_empty() {
            continue;
        }
        let id = ProfileId { prefix: prefix.to_string(), class: class.to_string() };
        if dir.join(id.rotations_file()).exists() {
            out.push(id);
        }
    }
    out.sort_by_key(|a| a.label());
    out
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Tab {
    Abilities,
    Rotations,
    Diff,
}

impl Tab {
    pub const ALL: [Tab; 3] = [Tab::Abilities, Tab::Rotations, Tab::Diff];
    pub fn label(self) -> &'static str {
        match self {
            Tab::Abilities => "Abilities",
            Tab::Rotations => "Rotations",
            Tab::Diff => "Diff",
        }
    }
}

/// Which side of the profile the diff tab compares.
pub struct Gui {
    pub dir: PathBuf,
    pub available: Vec<ProfileId>,
    pub sel_profile: Option<usize>,
    pub profile: Option<Profile>,
    /// `[abilities, rotations]` — unsaved local edits pending.
    pub dirty: [bool; 2],
    /// `[abilities, rotations]` — file changed on disk while dirty.
    pub conflict: [bool; 2],
    pub tab: Tab,

    // Abilities tab
    pub sel_ability: Option<usize>,
    pub ability_filter: String,
    /// Pending rename buffer for the selected ability (committed on
    /// save/selection change so live lint is never noisy while typing).
    pub name_buf: String,

    // Rotations tab
    pub sel_rotation: Option<usize>,

    // Diff tab
    pub diff_a: usize,
    pub diff_b: usize,
    pub diff_opts: DiffOptions,

    // Diagnostics
    pub status: String,
    pub lint: Vec<String>,
    pub show_lint: bool,

    mtimes: [Option<SystemTime>; 2],
    last_check: f64,
}

impl Gui {
    pub fn new(dir: PathBuf) -> Self {
        let available = discover_profiles(&dir);
        let mut g = Self {
            dir,
            available,
            sel_profile: None,
            profile: None,
            dirty: [false; 2],
            conflict: [false; 2],
            tab: Tab::Abilities,
            sel_ability: None,
            ability_filter: String::new(),
            name_buf: String::new(),
            sel_rotation: Some(0),
            diff_a: 0,
            diff_b: 0,
            diff_opts: DiffOptions::default(),
            status: String::new(),
            lint: Vec::new(),
            show_lint: false,
            mtimes: [None; 2],
            last_check: 0.0,
        };
        if g.available.is_empty() {
            g.status = format!("no profiles found in {}", g.dir.display());
        } else {
            g.sel_profile = Some(0);
            g.load_selected();
        }
        g
    }

    pub fn current(&self) -> Option<&ProfileId> {
        self.sel_profile.and_then(|i| self.available.get(i))
    }

    fn paths(&self) -> Option<(PathBuf, PathBuf)> {
        let id = self.current()?;
        Some((
            self.dir.join(id.abilities_file()),
            self.dir.join(id.rotations_file()),
        ))
    }

    pub fn load_selected(&mut self) {
        self.commit_rename();
        let Some(id) = self.current().cloned() else {
            self.profile = None;
            return;
        };
        match load_profile(&self.dir, &id.prefix, &id.class) {
            Ok(profile) => {
                // Default diff: first two rotations side by side.
                self.diff_a = 0;
                self.diff_b = if profile.rotations.len() > 1 { 1 } else { 0 };
                self.profile = Some(profile);
                self.dirty = [false; 2];
                self.conflict = [false; 2];
                self.sel_ability = Some(0);
                self.sel_rotation = Some(0);
                self.sync_name_buf();
                self.status = format!("loaded {}", id.label());
            }
            Err(e) => {
                self.profile = None;
                self.status = format!("load failed: {e}");
            }
        }
        self.baseline_mtimes();
    }

    /// Profile switch: pending edits are saved first (files are byte-exact
    /// round-trips, and silent loss would be worse than an extra write).
    /// Refuses to switch if a save failed.
    pub fn select_profile(&mut self, idx: usize) {
        if self.sel_profile == Some(idx) {
            return;
        }
        if self.profile.is_some() {
            self.save_dirty();
            if self.dirty.iter().any(|d| *d) {
                self.status =
                    "save failed — fix the error before switching profiles".into();
                return;
            }
        }
        self.sel_profile = Some(idx);
        self.load_selected();
    }

    fn baseline_mtimes(&mut self) {
        self.mtimes = [self.mtime(0), self.mtime(1)];
    }

    fn mtime(&self, which: usize) -> Option<SystemTime> {
        let (a, r) = self.paths()?;
        let p = if which == 0 { a } else { r };
        std::fs::metadata(p).and_then(|m| m.modified()).ok()
    }

    /// Commit the pending rename into the ability name and every rotation
    /// priority entry that references the old name (trimmed equality — the
    /// same rule `Rotation::resolve` uses, so nothing gets silently dropped).
    pub fn commit_rename(&mut self) {
        let Some(profile) = self.profile.as_mut() else { return };
        let Some(i) = self.sel_ability else { return };
        let new = self.name_buf.trim().to_string();
        if new.is_empty() || i >= profile.abilities.len() {
            self.sync_name_buf();
            return;
        }
        let old = profile.abilities[i].name.clone();
        if old == new {
            return;
        }
        profile.abilities[i].name = new.clone();
        let mut refs = 0;
        for rot in &mut profile.rotations {
            for entry in &mut rot.priority {
                if entry.trim() == old.trim() {
                    *entry = new.clone();
                    refs += 1;
                }
            }
        }
        self.dirty[0] = true;
        if refs > 0 {
            self.dirty[1] = true;
        }
        self.status = if refs > 0 {
            format!("renamed \u{201c}{old}\u{201d} -> \u{201c}{new}\u{201d}, updated {refs} rotation reference(s)")
        } else {
            format!("renamed \u{201c}{old}\u{201d} -> \u{201c}{new}\u{201d}")
        };
        self.sync_name_buf();
    }

    pub fn sync_name_buf(&mut self) {
        self.name_buf = self
            .profile
            .as_ref()
            .and_then(|p| self.sel_ability.and_then(|i| p.abilities.get(i)))
            .map(|a| a.name.clone())
            .unwrap_or_default();
    }

    pub fn select_ability(&mut self, idx: Option<usize>) {
        if self.sel_ability == idx {
            return;
        }
        self.commit_rename();
        self.sel_ability = idx;
        self.sync_name_buf();
    }

    /// Save dirty files (after committing a pending rename). Returns the
    /// number of files written.
    pub fn save_dirty(&mut self) -> usize {
        self.commit_rename();
        let mut written = 0;
        for which in 0..2 {
            if self.dirty[which] && self.save_which(which) {
                written += 1;
            }
        }
        written
    }

    fn save_which(&mut self, which: usize) -> bool {
        let Some(profile) = self.profile.as_ref() else { return false };
        let Some((a, r)) = self.paths() else { return false };
        let (path, content) = if which == 0 {
            (a, write_abilities(&profile.class, &profile.abilities))
        } else {
            (r, write_rotations(&profile.class, &profile.rotations))
        };
        match std::fs::write(&path, &content) {
            Ok(()) => {
                self.dirty[which] = false;
                self.conflict[which] = false;
                self.mtimes[which] = self.mtime(which);
                let name = path.file_name().unwrap_or_default().to_string_lossy().into_owned();
                self.status = format!("saved {name}");
                true
            }
            Err(e) => {
                self.status = format!("save failed: {e}");
                false
            }
        }
    }

    /// Throttled hot-reload: called from `App::logic` with the egui clock.
    pub fn check_hot_reload(&mut self, now: f64) {
        if now - self.last_check < 0.5 {
            return;
        }
        self.last_check = now;
        for which in 0..2 {
            let Some(m) = self.mtime(which) else { continue };
            let changed = self.mtimes[which].is_some_and(|old| old != m);
            self.mtimes[which] = Some(m);
            if !changed {
                continue;
            }
            if self.dirty[which] {
                self.conflict[which] = true;
                self.status = "file changed on disk while editing — see banner".into();
            } else if self.profile.is_some() {
                self.reload_which(which);
            }
        }
    }

    fn reload_which(&mut self, which: usize) {
        let Some((a, r)) = self.paths() else { return };
        let (path, class) = match (which, self.profile.as_ref()) {
            (0, Some(p)) => (a, p.class.clone()),
            (1, Some(p)) => (r, p.class.clone()),
            _ => return,
        };
        let bytes = match std::fs::read(&path) {
            Ok(b) => b,
            Err(e) => {
                self.status = format!("reload failed: {e}");
                return;
            }
        };
        let res = if which == 0 {
            parse_abilities(&bytes).map(|(_, v)| Profile { class: class.clone(), abilities: v, rotations: vec![] })
        } else {
            parse_rotations(&bytes).map(|(_, v)| Profile { class: class.clone(), abilities: vec![], rotations: v })
        };
        match res {
            Ok(chunk) => {
                let profile = self.profile.as_mut().expect("checked above");
                if which == 0 {
                    profile.abilities = chunk.abilities;
                    if self.sel_ability.is_some_and(|i| i >= profile.abilities.len()) {
                        self.sel_ability =
                            (!profile.abilities.is_empty()).then_some(profile.abilities.len() - 1);
                        self.sync_name_buf();
                    }
                } else {
                    profile.rotations = chunk.rotations;
                    if self.sel_rotation.is_some_and(|i| i >= profile.rotations.len()) {
                        self.sel_rotation =
                            (!profile.rotations.is_empty()).then_some(profile.rotations.len() - 1);
                    }
                }
                let name = path.file_name().unwrap_or_default().to_string_lossy().into_owned();
                self.status = format!("reloaded {name} from disk");
            }
            Err(e) => self.status = format!("reload parse error: {e}"),
        }
    }

    /// Conflict banner actions.
    pub fn conflict_reload(&mut self, which: usize) {
        self.dirty[which] = false;
        self.conflict[which] = false;
        self.reload_which(which);
    }

    pub fn conflict_keep(&mut self, which: usize) {
        // mtime baseline was already advanced in check_hot_reload; a later
        // save overwrites disk with the local buffer.
        self.conflict[which] = false;
        self.status = "keeping local edits — save will overwrite the disk copy".into();
    }

    pub fn ability_name_set(&self) -> HashSet<String> {
        self.profile
            .as_ref()
            .map(|p| p.abilities.iter().map(|a| a.name.trim().to_string()).collect())
            .unwrap_or_default()
    }

    pub fn diff_result(&self) -> Option<DiffResult> {
        let p = self.profile.as_ref()?;
        let a = p.rotations.get(self.diff_a)?;
        let b = p.rotations.get(self.diff_b)?;
        Some(align_rotations(&a.priority, &b.priority, &self.ability_name_set(), &self.diff_opts))
    }

    /// Full-profile lint: unresolved RotationList references (the silent
    /// failure of `frmMain.AddAbilityToCurrent`), duplicate ability names,
    /// empty priority lists.
    pub fn lint(&mut self) {
        self.commit_rename();
        let mut out = Vec::new();
        if let Some(p) = self.profile.as_ref() {
            let mut counts: HashMap<&str, usize> = HashMap::new();
            for a in &p.abilities {
                *counts.entry(a.name.trim()).or_default() += 1;
            }
            let mut dups: Vec<&str> =
                counts.iter().filter(|(_, &n)| n > 1).map(|(k, _)| *k).collect();
            dups.sort_unstable();
            for name in dups {
                out.push(format!("duplicate ability name \u{201c}{name}\u{201d}"));
            }
            for rot in &p.rotations {
                if rot.priority.is_empty() {
                    out.push(format!("rotation \u{201c}{}\u{201d}: empty priority list", rot.name));
                    continue;
                }
                if let Err(missing) = rot.resolve(&p.abilities) {
                    for m in missing {
                        out.push(format!(
                            "rotation \u{201c}{}\u{201d}: \u{201c}{}\u{201d} is not an ability name",
                            rot.name, m.missing
                        ));
                    }
                }
            }
        }
        self.lint = out;
        self.show_lint = true;
        if self.lint.is_empty() {
            self.status = "lint: clean".into();
        } else {
            self.status = format!("lint: {} issue(s)", self.lint.len());
        }
    }

    /// Unresolved references across all rotations (badge counter).
    pub fn unresolved_count(&self) -> usize {
        let Some(p) = self.profile.as_ref() else { return 0 };
        p.rotations
            .iter()
            .map(|r| match r.resolve(&p.abilities) {
                Ok(_) => 0,
                Err(missing) => missing.len(),
            })
            .sum()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use pqr_profile::{Ability, Rotation, TargetKind};
    use std::sync::atomic::{AtomicUsize, Ordering};

    static N: AtomicUsize = AtomicUsize::new(0);

    fn temp_dir(tag: &str) -> PathBuf {
        let n = N.fetch_add(1, Ordering::SeqCst);
        let d = std::env::temp_dir().join(format!(
            "pqr-gui-{tag}-{}-{n}",
            std::process::id()
        ));
        std::fs::create_dir_all(&d).unwrap();
        d
    }

    fn ability(name: &str, spell_id: u32) -> Ability {
        Ability {
            name: name.into(),
            default: false,
            spell_id,
            actions: String::new(),
            lua: String::new(),
            recast_delay: 0,
            target: TargetKind::Target,
            cancel_channel: false,
            lua_before: String::new(),
            lua_after: String::new(),
        }
    }

    fn rotation(name: &str, priority: &[&str]) -> Rotation {
        Rotation {
            name: name.into(),
            default: true,
            priority: priority.iter().map(|s| s.to_string()).collect(),
            require_combat: true,
            notes: format!("note for {name}"),
        }
    }

    fn write_pair(dir: &Path, prefix: &str, class: &str) {
        let abilities = vec![ability("Wrath", 51701), ability("Moonfire", 8921)];
        let rotations = vec![
            rotation("Default", &["Wrath", "Moonfire"]),
            rotation("AoE", &["Moonfire"]),
        ];
        std::fs::write(
            dir.join(format!("{prefix}_{class}_Abilities.xml")),
            write_abilities(class, &abilities),
        )
        .unwrap();
        std::fs::write(
            dir.join(format!("{prefix}_{class}_Rotations.xml")),
            write_rotations(class, &rotations),
        )
        .unwrap();
    }

    #[test]
    fn discover_pairs_and_skips_unpaired() {
        let dir = temp_dir("discover");
        write_pair(&dir, "DarhangeR", "DRUID");
        write_pair(&dir, "Solo", "PAL");
        std::fs::write(dir.join("Lonely_DRUID_Abilities.xml"), "<DRUID></DRUID>").unwrap();
        std::fs::write(dir.join("NoClass_Abilities.xml"), "<X></X>").unwrap();
        std::fs::write(dir.join("NoUnderscore_Abilities.xml"), "<X></X>").unwrap();

        let found = discover_profiles(&dir);
        let labels: Vec<String> = found.iter().map(|f| f.label()).collect();
        assert_eq!(labels, vec!["DarhangeR_DRUID", "Solo_PAL"]);

        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn rename_updates_rotation_references() {
        let dir = temp_dir("rename");
        write_pair(&dir, "T", "DRUID");
        let mut g = Gui::new(dir.clone());
        assert_eq!(g.profile.as_ref().unwrap().abilities.len(), 2);

        g.select_ability(Some(0));
        assert_eq!(g.name_buf, "Wrath");
        g.name_buf = "Fury".into();
        g.commit_rename();

        let p = g.profile.as_ref().unwrap();
        assert_eq!(p.abilities[0].name, "Fury");
        assert_eq!(p.rotations[0].priority, vec!["Fury", "Moonfire"]);
        assert!(g.dirty[0] && g.dirty[1]);

        // Unresolved warning disappears after the committed rename.
        assert_eq!(g.unresolved_count(), 0);

        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn save_then_external_change_hot_reloads() {
        let dir = temp_dir("reload");
        write_pair(&dir, "T", "DRUID");
        let mut g = Gui::new(dir.clone());

        g.select_ability(Some(0));
        g.name_buf = "Fury".into();
        assert_eq!(g.save_dirty(), 2);
        assert!(!g.dirty[0] && !g.dirty[1]);
        let on_disk = std::fs::read_to_string(dir.join("T_DRUID_Abilities.xml")).unwrap();
        assert!(on_disk.contains("Fury"));

        // External edit while clean => silent reload.
        std::thread::sleep(std::time::Duration::from_millis(60));
        std::fs::write(
            dir.join("T_DRUID_Abilities.xml"),
            write_abilities("DRUID", &[ability("Rejuvenation", 774)]),
        )
        .unwrap();
        g.check_hot_reload(10.0);
        assert!(!g.conflict[0]);
        assert_eq!(g.profile.as_ref().unwrap().abilities[0].name, "Rejuvenation");

        // External edit while dirty => conflict banner, keep preserves edits.
        g.select_ability(Some(0));
        g.name_buf = "Rejuv2".into();
        g.commit_rename();
        assert!(g.dirty[0]);
        std::thread::sleep(std::time::Duration::from_millis(60));
        std::fs::write(
            dir.join("T_DRUID_Abilities.xml"),
            write_abilities("DRUID", &[ability("Wild", 33763)]),
        )
        .unwrap();
        g.check_hot_reload(20.0);
        assert!(g.conflict[0]);
        g.conflict_keep(0);
        assert!(!g.conflict[0]);
        assert!(g.dirty[0]);
        assert_eq!(g.profile.as_ref().unwrap().abilities[0].name, "Rejuv2");

        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn conflict_reload_discards_local_edits() {
        let dir = temp_dir("conflict");
        write_pair(&dir, "T", "DRUID");
        let mut g = Gui::new(dir.clone());

        g.select_ability(Some(0));
        g.name_buf = "LocalEdit".into();
        g.commit_rename();
        assert!(g.dirty[0]);
        std::thread::sleep(std::time::Duration::from_millis(60));
        std::fs::write(
            dir.join("T_DRUID_Abilities.xml"),
            write_abilities("DRUID", &[ability("DiskWins", 1)]),
        )
        .unwrap();
        g.check_hot_reload(10.0);
        assert!(g.conflict[0]);
        g.conflict_reload(0);
        assert!(!g.conflict[0]);
        assert!(!g.dirty[0]);
        assert_eq!(g.profile.as_ref().unwrap().abilities[0].name, "DiskWins");

        std::fs::remove_dir_all(&dir).ok();
    }

    #[test]
    fn lint_reports_missing_and_duplicates() {
        let dir = temp_dir("lint");
        write_pair(&dir, "T", "DRUID");
        let mut g = Gui::new(dir.clone());
        g.lint();
        assert!(g.lint.is_empty());

        let p = g.profile.as_mut().unwrap();
        p.rotations[0].priority.push("Ghost Spell".into());
        p.abilities.push(ability("Wrath", 51701)); // duplicate name
        g.lint();
        let text = g.lint.join("\n");
        assert!(text.contains("Ghost Spell"), "{text}");
        assert!(text.contains("duplicate"), "{text}");

        std::fs::remove_dir_all(&dir).ok();
    }
}
