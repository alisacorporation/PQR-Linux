/* Align two rotation lists (GitHub split-diff style) by base ability name,
 * i.e. ignoring mode prefixes (R:, F:, PvP:, PvP_BG:, ...) per user request.
 * Unified display name = base name without prefix; PvP shown as a badge. */
"use strict";

/* Whitelist of real mode prefixes — prevents mangling names like
 * "Power Word: Shield" (prefix "Power Word" would contain a space). */
const PREFIX_RE = /^(PvP_BG|PvP|Use|All|Enc|Dest|Dem|Af|Fr|BM|Pet|[FBRHSPDEAUMC]):(.+)$/;

function isPvpPrefix(prefix) {
  return prefix === "PvP:" || prefix === "PvP_BG:";
}

/** Split "R:Tree of Life" -> { prefix: "R:", base: "Tree of Life", section } */
function splitAbilityName(full) {
  const name = (full || "").trim();
  const m = name.match(PREFIX_RE);
  if (m) {
    return { prefix: m[1] + ":", base: m[2].trim(), section: m[2].trim().startsWith("--") };
  }
  return { prefix: null, base: name, section: name.startsWith("--") };
}

function seqKey(split) {
  return (split.section ? "s:" : "a:") + split.base;
}

/**
 * Ordered unique sequence of a rotation list.
 * Dedupe by key (first occurrence wins for rank); same-base repeats
 * in one side are kept in `extras` so no data is lost.
 * idx = raw position, used for interleaving the other side's items.
 */
function buildSequence(entries) {
  const seq = [];
  const extras = {};
  const seen = new Set();
  let rank = 0;
  let idx = 0;
  for (const full of entries) {
    const split = splitAbilityName(full);
    const key = seqKey(split);
    if (seen.has(key)) {
      (extras[key] = extras[key] || []).push({ full });
      idx += 1;
      continue;
    }
    seen.add(key);
    if (!split.section) rank += 1;
    seq.push({ key, full, base: split.base, prefix: split.prefix, section: split.section, rank: split.section ? null : rank, idx });
    idx += 1;
  }
  return { seq, extras };
}

/**
 * Ordered merge-join on shared keys (NOT LCS): every base name present on
 * both sides becomes ONE pair, even with order conflicts — the rank delta
 * column communicates the move. Backbone follows seqA's order; seqB-only
 * items slot in by their own position relative to paired anchors.
 */
function joinOps(seqA, seqB) {
  const mapB = new Map(seqB.map((e) => [e.key, e]));
  const consumed = new Set();
  const backbone = seqA.map((a) => {
    const b = mapB.has(a.key) && !consumed.has(a.key) ? mapB.get(a.key) : null;
    if (b) consumed.add(a.key);
    return { a, b };
  });
  const pending = seqB.filter((e) => !consumed.has(e.key)); // B-only, B order
  const ops = [];
  let pi = 0;
  for (const p of backbone) {
    if (p.b) {
      while (pi < pending.length && pending[pi].idx < p.b.idx) {
        ops.push({ t: "onlyB", b: pending[pi] });
        pi++;
      }
      ops.push({ t: "both", a: p.a, b: p.b });
    } else {
      ops.push({ t: "onlyA", a: p.a });
    }
  }
  while (pi < pending.length) {
    ops.push({ t: "onlyB", b: pending[pi] });
    pi++;
  }
  return ops;
}

/**
 * Align two rotations. options: { sort, onlyDiff, showSections }
 * pair: { key, base, section, kind: 'both'|'onlyA'|'onlyB',
 *         nameA, rankA, nameB, rankB, prefixA, prefixB,
 *         delta, abilityA, abilityB, ghost, extraA, extraB }
 */
function alignRotations(rotA, rotB, abilities, options) {
  const opts = options || {};
  const sort = opts.sort || "order-a";

  // order-b: align from B's perspective, then swap sides
  let flip = false;
  let left = rotA, right = rotB;
  if (sort === "order-b") { left = rotB; right = rotA; flip = true; }

  const sA = buildSequence(left.entries);
  const sB = buildSequence(right.entries);
  const ops = joinOps(sA.seq, sB.seq);

  const seqForA = flip ? sB : sA; // buildSequence result of rotA
  const seqForB = flip ? sA : sB; // buildSequence result of rotB

  const pairs = ops.map((op) => {
    // op.a = entry of `left`, op.b = entry of `right`.
    // Display side A is always rotA, side B is always rotB.
    const A = flip
      ? (op.t === "both" ? op.b : (op.t === "onlyB" ? op.b : null))
      : (op.a || null);
    const B = flip
      ? (op.t === "both" ? op.a : (op.t === "onlyA" ? op.a : null))
      : (op.b || null);
    const k = op.t === "both" ? "both"
      : ((op.t === "onlyA") !== flip ? "onlyA" : "onlyB");
    const base = (A || B).base;
    const section = (A || B).section;
    const rankA = A ? A.rank : null;
    const rankB = B ? B.rank : null;
    const nameA = A ? A.full : null;
    const nameB = B ? B.full : null;
    const abilityA = nameA && !section ? (abilities.get(nameA) || null) : null;
    const abilityB = nameB && !section ? (abilities.get(nameB) || null) : null;
    const ghostA = !!nameA && !section && !abilityA;
    const ghostB = !!nameB && !section && !abilityB;
    const exKey = seqKey({ section, base });
    return {
      key: exKey, base, section, kind: k,
      nameA, rankA, prefixA: A ? A.prefix : null,
      nameB, rankB, prefixB: B ? B.prefix : null,
      delta: rankA != null && rankB != null ? rankA - rankB : null,
      abilityA, abilityB,
      ghost: ghostA || ghostB, ghostA, ghostB,
      extraA: seqForA.extras[exKey] || [],
      extraB: seqForB.extras[exKey] || [],
    };
  });

  // filters
  let filtered = pairs;
  if (!opts.showSections) {
    filtered = filtered.filter((p) => !p.section);
  }
  if (opts.onlyDiff) {
    filtered = filtered.filter((p) => {
      if (p.section) return p.kind !== "both";
      return p.kind !== "both" || p.delta !== 0;
    });
  }

  // sorts
  const byAlpha = (x, y) => x.base.localeCompare(y.base);
  if (sort === "alpha") {
    filtered = [...filtered].sort(byAlpha);
  } else if (sort === "delta") {
    filtered = [...filtered].sort((x, y) => {
      const score = (p) => {
        if (p.section) return -1;
        if (p.kind !== "both") return 1e8; // one-sided => maximal divergence
        return Math.abs(p.delta || 0);
      };
      const d = score(y) - score(x);
      if (d !== 0) return d;
      return (x.rankA ?? x.rankB ?? 1e9) - (y.rankA ?? y.rankB ?? 1e9);
    });
  }

  const real = pairs.filter((p) => !p.section);
  return {
    pairs: filtered,
    stats: {
      onlyA: real.filter((p) => p.kind === "onlyA").length,
      onlyB: real.filter((p) => p.kind === "onlyB").length,
      both: real.filter((p) => p.kind === "both").length,
      same: real.filter((p) => p.kind === "both" && p.delta === 0).length,
      ghost: real.filter((p) => p.ghost).length,
      totalA: sA.seq.filter((e) => !e.section).length,
      totalB: sB.seq.filter((e) => !e.section).length,
    },
  };
}

/** Plain ordered list for the raw "Списки" view. */
function columnList(rot, abilities, showSections) {
  const out = [];
  const seen = new Set();
  let rank = 0;
  for (const full of rot.entries) {
    const split = splitAbilityName(full);
    if (split.section) {
      if (showSections) out.push({ kind: "section", name: split.base });
      continue;
    }
    const key = seqKey(split);
    if (seen.has(key)) continue;
    seen.add(key);
    rank += 1;
    out.push({
      kind: "ability",
      rank,
      name: full,
      base: split.base,
      prefix: split.prefix,
      ability: abilities.get(full.trim()) || null,
    });
  }
  return out;
}
