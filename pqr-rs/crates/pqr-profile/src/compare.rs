//! Side-by-side rotation alignment — port of `viewer/js/compare.js`.
//!
//! GitHub split-diff style: two rotation lists are aligned by **base ability
//! name** (mode prefixes `R:`/`F:`/`PvP:`/`PvP_BG:` stripped per whitelist),
//! so `R:Tree of Life` and `Tree of Life` pair up. Order conflicts are
//! communicated by a rank-delta column, not by the alignment itself.

use std::collections::{HashMap, HashSet};

/// Whitelist of real mode prefixes — prevents mangling names like
/// `Power Word: Shield` (prefix "Power Word" would contain a space).
/// Mirrors `PREFIX_RE = /^(PvP_BG|PvP|Use|All|Enc|Dest|Dem|Af|Fr|BM|Pet|[FBRHSPDEAUMC]):(.+)$/`.
fn prefix_allowed(pre: &str) -> bool {
    matches!(
        pre,
        "PvP_BG" | "PvP" | "Use" | "All" | "Enc" | "Dest" | "Dem" | "Af" | "Fr" | "BM" | "Pet"
    ) || (pre.len() == 1 && "FBRHSPDEAUMC".contains(pre.chars().next().unwrap()))
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SplitName {
    pub prefix: Option<String>,
    pub base: String,
    pub section: bool,
}

/// `"R:Tree of Life"` -> prefix `R:`, base `Tree of Life`.
/// Sections (`-- Functions --`) are flag-markers, never casts.
pub fn split_ability_name(full: &str) -> SplitName {
    let name = full.trim();
    if let Some((pre, rest)) = name.split_once(':') {
        if !rest.trim().is_empty() && prefix_allowed(pre) {
            let base = rest.trim().to_string();
            return SplitName {
                prefix: Some(format!("{pre}:")),
                section: base.starts_with("--"),
                base,
            };
        }
    }
    SplitName { prefix: None, section: name.starts_with("--"), base: name.to_string() }
}

/// Dedup key: sections and abilities are separate namespaces.
pub fn seq_key(section: bool, base: &str) -> String {
    format!("{}{}", if section { "s:" } else { "a:" }, base)
}

#[derive(Debug, Clone)]
pub struct SeqEntry {
    pub key: String,
    pub full: String,
    pub base: String,
    pub prefix: Option<String>,
    pub section: bool,
    pub rank: Option<u32>,
    pub idx: usize,
}

/// Ordered unique sequence of a rotation list. Dedupe by key (first occurrence
/// wins for rank); same-base repeats in one side are kept in `extras` so no
/// data is lost. `idx` = raw position, used for interleaving the other side.
#[derive(Debug, Default)]
pub struct Sequence {
    pub seq: Vec<SeqEntry>,
    pub extras: HashMap<String, Vec<String>>,
}

pub fn build_sequence(entries: &[String]) -> Sequence {
    let mut out = Sequence::default();
    let mut seen: HashSet<String> = HashSet::new();
    let mut rank = 0u32;
    for (idx, full) in entries.iter().enumerate() {
        let split = split_ability_name(full);
        let key = seq_key(split.section, &split.base);
        if !seen.insert(key.clone()) {
            out.extras.entry(key).or_default().push(full.clone());
            continue;
        }
        let rank = if split.section { None } else { rank += 1; Some(rank) };
        out.seq.push(SeqEntry {
            key,
            full: full.clone(),
            base: split.base,
            prefix: split.prefix,
            section: split.section,
            rank,
            idx,
        });
    }
    out
}

enum Op<'a> {
    Both(&'a SeqEntry, &'a SeqEntry),
    OnlyA(&'a SeqEntry),
    OnlyB(&'a SeqEntry),
}

/// Ordered merge-join on shared keys (NOT LCS): every base name present on both
/// sides becomes ONE pair, even with order conflicts. Backbone follows seqA's
/// order; seqB-only items slot in by their own position relative to anchors.
fn join_ops<'a>(a: &'a [SeqEntry], b: &'a [SeqEntry]) -> Vec<Op<'a>> {
    let map_b: HashMap<&str, &SeqEntry> =
        b.iter().map(|e| (e.key.as_str(), e)).collect();
    let mut consumed: HashSet<&str> = HashSet::new();
    // Backbone: every A entry with its paired B entry (if any), A order.
    let mut backbone: Vec<(&'a SeqEntry, Option<&'a SeqEntry>)> = Vec::new();
    for pa in a {
        let paired = map_b.get(pa.key.as_str()).copied().filter(|_| consumed.insert(pa.key.as_str()));
        backbone.push((pa, paired));
    }
    // B-only entries in B order, interleaved relative to paired anchors.
    let pending: Vec<&SeqEntry> =
        b.iter().filter(|e| !consumed.contains(e.key.as_str())).collect();
    let mut pi = 0;
    let mut ops = Vec::with_capacity(backbone.len() + pending.len());
    for (pa, pb) in backbone {
        if let Some(pb) = pb {
            while pi < pending.len() && pending[pi].idx < pb.idx {
                ops.push(Op::OnlyB(pending[pi]));
                pi += 1;
            }
            ops.push(Op::Both(pa, pb));
        } else {
            ops.push(Op::OnlyA(pa));
        }
    }
    while pi < pending.len() {
        ops.push(Op::OnlyB(pending[pi]));
        pi += 1;
    }
    ops
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum PairKind {
    Both,
    OnlyA,
    OnlyB,
}

#[derive(Debug, Clone)]
pub struct Pair {
    pub key: String,
    pub base: String,
    pub section: bool,
    pub kind: PairKind,
    pub name_a: Option<String>,
    pub rank_a: Option<u32>,
    pub prefix_a: Option<String>,
    pub name_b: Option<String>,
    pub rank_b: Option<u32>,
    pub prefix_b: Option<String>,
    /// `rank_a - rank_b`, None when either side lacks a real rank.
    pub delta: Option<i64>,
    /// Name present on side A but absent from the profile's ability table
    /// (a RotationList entry silently dropped by the old editor — "ghost").
    pub ghost_a: bool,
    pub ghost_b: bool,
    pub ghost: bool,
    pub extra_a: Vec<String>,
    pub extra_b: Vec<String>,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SortMode {
    /// Backbone order (seqA).
    OrderA,
    Alpha,
    /// Divergence first (one-sided > big delta > ... > sections last).
    Delta,
}

#[derive(Debug, Clone)]
pub struct DiffOptions {
    pub sort: SortMode,
    pub only_diff: bool,
    pub show_sections: bool,
}

impl Default for DiffOptions {
    fn default() -> Self {
        Self { sort: SortMode::OrderA, only_diff: false, show_sections: true }
    }
}

#[derive(Debug, Clone, Copy, Default)]
pub struct DiffStats {
    pub only_a: usize,
    pub only_b: usize,
    pub both: usize,
    pub same: usize,
    pub ghost: usize,
    pub total_a: usize,
    pub total_b: usize,
}

#[derive(Debug, Clone)]
pub struct DiffResult {
    pub pairs: Vec<Pair>,
    pub stats: DiffStats,
}

/// Align two rotation lists. `ability_names` is the profile's ability table
/// (used only to flag ghosts); lookup is trimmed on both sides.
///
/// Not ported from the viewer: the `order-b` flip sort (marginal value,
/// doubles the side bookkeeping) — backbone is always side A's order.
pub fn align_rotations(
    entries_a: &[String],
    entries_b: &[String],
    ability_names: &HashSet<String>,
    opts: &DiffOptions,
) -> DiffResult {
    let s_a = build_sequence(entries_a);
    let s_b = build_sequence(entries_b);
    let ops = join_ops(&s_a.seq, &s_b.seq);

    let ghost_of = |name: &Option<String>, section: bool| -> bool {
        match name {
            Some(n) if !section => !ability_names.contains(n.trim()),
            _ => false,
        }
    };

    let mut pairs: Vec<Pair> = ops
        .iter()
        .map(|op| {
            let (a, b) = match op {
                Op::Both(a, b) => (Some(*a), Some(*b)),
                Op::OnlyA(a) => (Some(*a), None),
                Op::OnlyB(b) => (None, Some(*b)),
            };
            let anchor = a.or(b).expect("op always has one side");
            let kind = match op {
                Op::Both(..) => PairKind::Both,
                Op::OnlyA(_) => PairKind::OnlyA,
                Op::OnlyB(_) => PairKind::OnlyB,
            };
            let name_a = a.map(|e| e.full.clone());
            let name_b = b.map(|e| e.full.clone());
            let rank_a = a.and_then(|e| e.rank);
            let rank_b = b.and_then(|e| e.rank);
            let section = anchor.section;
            let key = seq_key(section, &anchor.base);
            let extra_a = s_a.extras.get(&key).cloned().unwrap_or_default();
            let extra_b = s_b.extras.get(&key).cloned().unwrap_or_default();
            let ghost_a = ghost_of(&name_a, section);
            let ghost_b = ghost_of(&name_b, section);
            Pair {
                ghost: ghost_a || ghost_b,
                ghost_a,
                ghost_b,
                key,
                base: anchor.base.clone(),
                section,
                kind,
                delta: match (rank_a, rank_b) {
                    (Some(x), Some(y)) => Some(x as i64 - y as i64),
                    _ => None,
                },
                name_a,
                rank_a,
                prefix_a: a.and_then(|e| e.prefix.clone()),
                name_b,
                rank_b,
                prefix_b: b.and_then(|e| e.prefix.clone()),
                extra_a,
                extra_b,
            }
        })
        .collect();

    // Stats always describe the full (non-section) alignment, filters aside.
    let stats = {
        let real = pairs.iter().filter(|p| !p.section);
        DiffStats {
            only_a: real.clone().filter(|p| p.kind == PairKind::OnlyA).count(),
            only_b: real.clone().filter(|p| p.kind == PairKind::OnlyB).count(),
            both: real.clone().filter(|p| p.kind == PairKind::Both).count(),
            same: real.clone().filter(|p| p.kind == PairKind::Both && p.delta == Some(0)).count(),
            ghost: real.filter(|p| p.ghost).count(),
            total_a: s_a.seq.iter().filter(|e| !e.section).count(),
            total_b: s_b.seq.iter().filter(|e| !e.section).count(),
        }
    };

    if !opts.show_sections {
        pairs.retain(|p| !p.section);
    }
    if opts.only_diff {
        pairs.retain(|p| {
            if p.section {
                p.kind != PairKind::Both
            } else {
                p.kind != PairKind::Both || p.delta != Some(0)
            }
        });
    }
    match opts.sort {
        SortMode::OrderA => {}
        SortMode::Alpha => {
            pairs.sort_by_key(|p| p.base.to_lowercase());
        }
        SortMode::Delta => {
            let score = |p: &Pair| -> i64 {
                if p.section {
                    -1
                } else if p.kind != PairKind::Both {
                    1_000_000_000
                } else {
                    p.delta.unwrap_or(0).abs()
                }
            };
            pairs.sort_by(|x, y| {
                score(y)
                    .cmp(&score(x))
                    .then_with(|| {
                        let rx = x.rank_a.or(x.rank_b).unwrap_or(u32::MAX);
                        let ry = y.rank_a.or(y.rank_b).unwrap_or(u32::MAX);
                        rx.cmp(&ry)
                    })
            });
        }
    }

    DiffResult { pairs, stats }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn e(names: &[&str]) -> Vec<String> {
        names.iter().map(|s| s.to_string()).collect()
    }
    fn names(list: &[&str]) -> HashSet<String> {
        list.iter().map(|s| s.to_string()).collect()
    }
    fn opts(sort: SortMode, only_diff: bool, sections: bool) -> DiffOptions {
        DiffOptions { sort, only_diff, show_sections: sections }
    }

    #[test]
    fn split_prefix_whitelist() {
        let s = split_ability_name("R:Tree of Life");
        assert_eq!(s.prefix.as_deref(), Some("R:"));
        assert_eq!(s.base, "Tree of Life");
        assert!(!s.section);

        let s = split_ability_name("PvP_BG:Hammer of Justice");
        assert_eq!(s.prefix.as_deref(), Some("PvP_BG:"));
        assert_eq!(s.base, "Hammer of Justice");

        // Spaces in the token => not a mode prefix (whitelist guard).
        let s = split_ability_name("Power Word: Shield");
        assert_eq!(s.prefix, None);
        assert_eq!(s.base, "Power Word: Shield");

        // Empty remainder => not a prefix either.
        assert_eq!(split_ability_name("R:").prefix, None);

        // Single letters outside the whitelist don't match.
        assert_eq!(split_ability_name("Z:foo").prefix, None);
        let s = split_ability_name("F:Frost Strike");
        assert_eq!(s.prefix.as_deref(), Some("F:"));
    }

    #[test]
    fn split_sections() {
        assert!(split_ability_name("-- Functions --").section);
        assert!(split_ability_name("R:-- Hotkeys --").section);
        assert!(!split_ability_name("  Chain Heal  ").section);
        assert_eq!(split_ability_name("  Chain Heal  ").base, "Chain Heal");
    }

    #[test]
    fn sequence_ranks_and_extras() {
        let s = build_sequence(&e(&[
            "-- Functions --",
            "R:Wrath",
            "Moonfire",
            "Wrath",   // same base as R:Wrath => duplicate, goes to extras
            "R:Wrath", // duplicate again
        ]));
        assert_eq!(s.seq.len(), 3);
        assert_eq!(s.seq[0].rank, None);
        assert_eq!(s.seq[1].rank, Some(1)); // sections consume no rank
        assert_eq!(s.seq[2].rank, Some(2));
        let key = seq_key(false, "Wrath");
        assert_eq!(s.extras.get(&key), Some(&e(&["Wrath", "R:Wrath"])));
        // idx counts raw positions (duplicates included).
        assert_eq!(s.seq[1].idx, 1);
        assert_eq!(s.seq[2].idx, 2);
    }

    #[test]
    fn align_basic_kinds_and_delta() {
        let a = e(&["One", "Two", "Three"]);
        let b = e(&["Two", "One", "Four"]);
        let r = align_rotations(&a, &b, &names(&["One", "Two", "Three", "Four"]), &Default::default());
        // Backbone = A order: One(only in A? no — paired), ...
        let by_base = |r: &DiffResult, base: &str| {
            r.pairs.iter().find(|p| p.base == base).unwrap().clone()
        };
        let one = by_base(&r, "One");
        assert_eq!(one.kind, PairKind::Both);
        assert_eq!(one.rank_a, Some(1));
        assert_eq!(one.rank_b, Some(2));
        assert_eq!(one.delta, Some(-1));
        let two = by_base(&r, "Two");
        assert_eq!(two.delta, Some(1));
        let four = by_base(&r, "Four");
        assert_eq!(four.kind, PairKind::OnlyB);
        assert_eq!(r.stats.both, 2); // One, Two
        assert_eq!(r.stats.only_a, 1); // Three
        assert_eq!(r.stats.only_b, 1); // Four
        assert_eq!(r.stats.total_a, 3);
        assert_eq!(r.stats.total_b, 3);
        assert_eq!(r.stats.same, 0);
    }

    #[test]
    fn align_backbone_order_with_interleave() {
        // B-only item sits between paired anchors by its raw B position.
        let a = e(&["X", "Z"]);
        let b = e(&["X", "Mid", "Z"]);
        let r = align_rotations(&a, &b, &names(&["X", "Z", "Mid"]), &Default::default());
        let order: Vec<(&str, PairKind)> =
            r.pairs.iter().map(|p| (p.base.as_str(), p.kind)).collect();
        assert_eq!(
            order,
            vec![("X", PairKind::Both), ("Mid", PairKind::OnlyB), ("Z", PairKind::Both)]
        );
        // Trailing B-only lands at the end.
        let a = e(&["X"]);
        let b = e(&["X", "Tail"]);
        let r = align_rotations(&a, &b, &names(&["X", "Tail"]), &Default::default());
        assert_eq!(r.pairs.last().unwrap().base, "Tail");
        assert_eq!(r.pairs.last().unwrap().kind, PairKind::OnlyB);
    }

    #[test]
    fn align_pairs_across_prefixes() {
        let a = e(&["R:Tree of Life"]);
        let b = e(&["Tree of Life"]);
        let r = align_rotations(&a, &b, &names(&["Tree of Life"]), &Default::default());
        assert_eq!(r.pairs.len(), 1);
        assert_eq!(r.pairs[0].kind, PairKind::Both);
        assert_eq!(r.pairs[0].prefix_a.as_deref(), Some("R:"));
        assert_eq!(r.pairs[0].prefix_b, None);
        assert_eq!(r.stats.same, 1);
    }

    #[test]
    fn ghosts_flagged_from_ability_table() {
        let a = e(&["Real", "Removed"]);
        let b = e(&["Real", "Added"]);
        let r = align_rotations(&a, &b, &names(&["Real"]), &Default::default());
        let removed = r.pairs.iter().find(|p| p.base == "Removed").unwrap();
        assert!(removed.ghost_a && removed.ghost && !removed.ghost_b);
        let added = r.pairs.iter().find(|p| p.base == "Added").unwrap();
        assert!(added.ghost_b && added.ghost && !added.ghost_a);
        assert_eq!(r.stats.ghost, 2);
    }

    #[test]
    fn filters_only_diff_and_sections() {
        let a = e(&["-- Functions --", "Same", "Old"]);
        let b = e(&["-- Functions --", "Same", "New"]);
        let table = names(&["Same", "Old", "New"]);
        let r = align_rotations(&a, &b, &table, &opts(SortMode::OrderA, true, true));
        let bases: Vec<&str> = r.pairs.iter().map(|p| p.base.as_str()).collect();
        // section unchanged => hidden by only_diff; Same unchanged => hidden.
        assert_eq!(bases, vec!["Old", "New"]);

        let r = align_rotations(&a, &b, &table, &opts(SortMode::OrderA, false, false));
        let bases: Vec<&str> = r.pairs.iter().map(|p| p.base.as_str()).collect();
        assert_eq!(bases, vec!["Same", "Old", "New"]);

        // only_diff keeps changed section rows (both sides of the rename).
        let a = e(&["-- Functions --", "Same"]);
        let b = e(&["-- Hotkeys --", "Same"]);
        let r = align_rotations(&a, &b, &table, &opts(SortMode::OrderA, true, true));
        assert_eq!(r.pairs.len(), 2);
        assert!(r.pairs.iter().all(|p| p.section));
    }

    #[test]
    fn sort_alpha_and_delta() {
        let a = e(&["Same", "MovedFar", "OnlyA"]);
        let b = e(&["OnlyB", "Same", "MovedFar"]); // MovedFar: 2 -> 3? no: ranks 1..; see below
        let table = names(&["Same", "MovedFar", "OnlyA", "OnlyB"]);
        // ranks: A: Same=1 MovedFar=2 OnlyA=3 ; B: OnlyB=1 Same=2 MovedFar=3
        let r = align_rotations(&a, &b, &table, &opts(SortMode::Alpha, false, false));
        let bases: Vec<&str> = r.pairs.iter().map(|p| p.base.as_str()).collect();
        assert_eq!(bases, vec!["MovedFar", "OnlyA", "OnlyB", "Same"]);

        let r = align_rotations(&a, &b, &table, &opts(SortMode::Delta, false, false));
        // one-sided first (score 1e8), tie broken by rank: OnlyA(r3) vs OnlyB(r1)
        let bases: Vec<&str> = r.pairs.iter().map(|p| p.base.as_str()).collect();
        assert_eq!(bases[0], "OnlyB");
        assert_eq!(bases[1], "OnlyA");
        // then biggest |delta|: MovedFar |2-3|=1 == Same |1-2|=1, tie by rank.
        assert_eq!(bases[2], "Same");
        assert_eq!(bases[3], "MovedFar");
    }

    #[test]
    fn duplicate_on_both_sides_lands_in_extras() {
        let a = e(&["Fireball", "Fireball"]);
        let b = e(&["Fireball"]);
        let r = align_rotations(&a, &b, &names(&["Fireball"]), &Default::default());
        assert_eq!(r.pairs.len(), 1);
        assert_eq!(r.pairs[0].extra_a, e(&["Fireball"]));
        assert!(r.pairs[0].extra_b.is_empty());
        assert_eq!(r.stats.total_a, 1); // dedup: first occurrence only
    }
}
