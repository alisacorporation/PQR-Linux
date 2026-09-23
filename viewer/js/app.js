/* App state, rendering, and interactions. */
"use strict";

const state = {
  cls: null,
  rotA: null,
  rotB: null,
  sort: "order-a",
  onlyDiff: false,
  showSections: false,
  view: "merge",
  lang: "ru",
};

const expandedRows = new Set(); // pair keys with open detail row
let currentData = null; // { abilities, rotations }
let lastDiff = null;
let baseIndex = null; // base name -> [ability, ...] for "other variants"

/* ---------- hash state ---------- */

function readHash() {
  const h = new URLSearchParams(location.hash.replace(/^#/, ""));
  if (h.get("c")) state.cls = h.get("c").toUpperCase();
  if (h.get("a")) state.rotA = h.get("a");
  if (h.get("b")) state.rotB = h.get("b");
  if (h.get("sort")) state.sort = h.get("sort");
  if (h.get("diff") === "1") state.onlyDiff = true;
  if (h.get("sec") === "1") state.showSections = true;
  if (h.get("view") === "cols") state.view = "cols";
  if (h.get("lang") === "en" || h.get("lang") === "ru") state.lang = h.get("lang");
}

function writeHash() {
  const h = new URLSearchParams();
  h.set("c", state.cls || "");
  if (state.rotA) h.set("a", state.rotA);
  if (state.rotB) h.set("b", state.rotB);
  if (state.sort !== "order-a") h.set("sort", state.sort);
  if (state.onlyDiff) h.set("diff", "1");
  if (state.showSections) h.set("sec", "1");
  if (state.view !== "merge") h.set("view", state.view);
  if (state.lang !== "ru") h.set("lang", state.lang);
  const next = "#" + h.toString();
  if (location.hash !== next) {
    history.replaceState(null, "", next);
  }
}

/* ---------- helpers ---------- */

const $ = (id) => document.getElementById(id);

function esc(s) {
  return String(s == null ? "" : s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

/* Unified display: base name without any prefix (no "PvP:" in front);
 * spec prefix shown as chip, PvP shown as a badge instead of prefix text. */
function prefixChips(prefix) {
  if (!prefix) return "";
  if (isPvpPrefix(prefix)) {
    const bg = prefix === "PvP_BG:" ? '<span class="prefix-chip pvp-badge">BG</span>' : "";
    return `<span class="prefix-chip pvp-badge">PvP</span>${bg}`;
  }
  return `<span class="prefix-chip">${esc(prefix)}</span>`;
}

function wowheadUrl(spellId) {
  return `https://www.wowhead.com/wotlk/spell=${spellId}`;
}

/* ---------- Lua condition extraction (conservative) ---------- */

function extractConditions(lua) {
  if (!lua) return [];
  const out = [];
  const seen = new Set();
  const add = (key, text) => {
    if (seen.has(key)) return;
    seen.add(key);
    out.push(text);
  };

  const unitName = (u) => (u === "player" ? "player" : u);
  for (const m of lua.matchAll(/get(HP|Hp|Mana)\(\s*["'](\w+)["']\s*\)\s*(<|<=|>|>=)\s*(\d+)/g)) {
    const kind = m[1].toLowerCase() === "hp" ? "hp" : "mana";
    add(kind + m[2] + m[3] + m[4],
      tf("cond." + kind, { unit: unitName(m[2]), op: m[3], n: m[4] }));
  }
  if (/UnitAffectingCombat/.test(lua)) add("combat", t("cond.combat"));
  if (/UnitExists\(\s*["']target["']/.test(lua)) add("tgt", t("cond.needsTarget"));
  if (/UnitExists\(\s*["']focus["']/.test(lua)) add("foc", t("cond.needsFocus"));
  for (const m of lua.matchAll(/UnitExists\(\s*["'](\w+)["']\s*\)/g)) {
    if (m[1] !== "target" && m[1] !== "focus") {
      add("u" + m[1], tf("cond.needsUnit", { u: m[1] }));
    }
  }
  if (/rangeCheck\(/.test(lua)) add("range", t("cond.range"));
  if (/CooldownRemains\(/.test(lua)) add("cd", t("cond.cd"));
  if (/IsSpellKnown\(/.test(lua)) add("known", t("cond.known"));
  if (/UnitIsPlayer\(/.test(lua)) add("player", t("cond.playerOnly"));
  if (/UnitBuffID\(/.test(lua)) add("buff", t("cond.buff"));
  if (/UnitDebuffID\(/.test(lua)) add("debuff", t("cond.debuff"));
  if (/UnitCastingInfo\(|UnitChannelInfo\(/.test(lua)) add("cast", t("cond.cast"));
  if (/PQR_TestMode/.test(lua)) add("test", t("cond.testMode"));
  if (/PQR_IsMoving|IsMoving/.test(lua)) add("move", t("cond.moving"));
  if (/IsBoss\(/.test(lua)) add("boss", t("cond.boss"));
  if (/UnitIsDeadOrGhost/.test(lua)) add("dead", t("cond.dead"));
  if (/UnitClass\(|isMelee|isHealer/.test(lua)) add("cls", t("cond.classGate"));

  return out;
}

/* ---------- Lua syntax highlight (light) ---------- */

function highlightLua(lua) {
  const kws = /\b(function|end|if|then|else|elseif|return|local|and|or|not|for|in|do|while|true|false|nil)\b/g;
  let s = esc(lua);
  // comments, then strings, then numbers, then keywords, then calls
  s = s.replace(/(--[^\n]*)/g, '<span class="cm">$1</span>');
  s = s.replace(/(&quot;[^\n]*?&quot;|'[^'\n]*')/g, '<span class="str">$1</span>');
  s = s.replace(/\b(\d+\.?\d*)\b/g, '<span class="num">$1</span>');
  s = s.replace(kws, '<span class="kw">$1</span>');
  s = s.replace(/\b([A-Za-z_]\w*)\s*(?=\()/g, '<span class="fn">$1</span>');
  return s;
}

/* ---------- renderers ---------- */

function renderStats(stats) {
  const el = $("stats");
  if (!stats) { el.hidden = true; return; }
  el.hidden = false;
  const pct = stats.both
    ? Math.round((stats.same / stats.both) * 100)
    : 0;
  const items = [
    [t("statCountA"), stats.totalA],
    [t("statCountB"), stats.totalB],
    [t("statCommon"), stats.both],
    [t("statOnlyA"), stats.onlyA],
    [t("statOnlyB"), stats.onlyB],
    [t("statSamePos"), stats.both ? pct + "%" : "—"],
  ];
  if (stats.ghost) items.push([t("statGhost"), stats.ghost, true]);
  el.innerHTML = items.map(([label, val, warn]) =>
    `<span class="stat${warn ? " warn" : ""}">${esc(label)}: <b>${esc(val)}</b></span>`
  ).join("");
}

function renderNotes(rotA, rotB) {
  const el = $("notes");
  const card = (rot, side) => {
    if (!rot) return "";
    const notes = rot.notes || t("notesEmpty");
    const long = notes.length > 420;
    return `<div class="note-card">
      <h2><span class="badge badge-${side}">${side.toUpperCase()}</span> ${esc(rot.name)}</h2>
      <pre class="note-body" id="note-${side}">${esc(notes)}</pre>
      ${long ? `<button type="button" class="note-toggle" data-note="${side}">${esc(t("showMore"))}</button>` : ""}
    </div>`;
  };
  el.innerHTML = card(rotA, "a") + card(rotB, "b");
  el.querySelectorAll(".note-toggle").forEach((btn) => {
    btn.addEventListener("click", () => {
      const body = $("note-" + btn.dataset.note);
      const open = body.classList.toggle("expanded");
      btn.textContent = open ? t("showLess") : t("showMore");
    });
  });
}

/** One side cell of a split pair: unified base name + side's prefix chips. */
function sideCellHtml(pair, side) {
  const name = side === "A" ? pair.nameA : pair.nameB;
  if (!name) {
    return `<div class="missing">${esc(t("missingSide"))}</div>`;
  }
  const prefix = side === "A" ? pair.prefixA : pair.prefixB;
  const ab = side === "A" ? pair.abilityA : pair.abilityB;
  const ghost = side === "A" ? pair.ghostA : pair.ghostB;
  const extras = side === "A" ? pair.extraA : pair.extraB;

  const meta = [];
  if (ab && ab.spellId) {
    meta.push(`<span class="chip">${ab.spellId}</span>`);
    meta.push(`<a class="chip wowhead" href="${wowheadUrl(ab.spellId)}" target="_blank" rel="noopener noreferrer" title="${esc(t("wowhead"))}" data-stop="1">W</a>`);
  }
  if (ab) {
    if (ab.target && ab.target !== "Target") meta.push(`<span class="chip">${esc(t("target"))}:${esc(ab.target)}</span>`);
    if (ab.recastDelay && ab.recastDelay !== "0") meta.push(`<span class="chip">${esc(t("recast"))}:${esc(ab.recastDelay)}ms</span>`);
    if (ab.cancelChannel === "True") meta.push(`<span class="chip">${esc(t("cancelChan"))}</span>`);
    if (ab.actions) meta.push(`<span class="chip">macro</span>`);
  }
  if (ghost) meta.push(`<span class="chip warn">${esc(t("ghostMissing"))}</span>`);
  if (extras.length) {
    meta.push(`<span class="chip" title="${esc(extras.map((e) => e.full).join(", "))}">${esc(t("alsoIn"))} ${extras.length}</span>`);
  }

  return `<div class="ability-cell">
    ${prefixChips(prefix)}
    <span class="ability-name">${esc(pair.base)}</span>
    <span class="meta">${meta.join("")}</span>
  </div>`;
}

function deltaHtml(row) {
  if (row.rankA == null || row.rankB == null) {
    return `<span class="delta na">—</span>`;
  }
  if (row.delta === 0) {
    return `<span class="delta same">=</span>`;
  }
  // delta = rankA - rankB; >0 means smaller rankB → earlier in B → ↑
  const abs = Math.abs(row.delta);
  if (row.delta > 0) {
    return `<span class="delta up" title="${esc(t("deltaUp"))} ${abs}">↑${abs}</span>`;
  }
  return `<span class="delta down" title="${esc(t("deltaDown"))} ${abs}">↓${abs}</span>`;
}

/** Algorithm panel for one side: base name + side prefix + conditions + Lua + macro.
 * fullName null => side missing from this rotation. */
function algoPanelHtml(base, fullName, prefix, ability) {
  if (!fullName) {
    return `<div class="algo missing-algo">${esc(t("missingSide"))}</div>`;
  }
  if (!ability) {
    return `<div class="algo"><div class="algo-head">${prefixChips(prefix)}<b>${esc(base)}</b></div>
      <div class="expand-grid"><span class="chip warn">${esc(t("ghostMissing"))}</span></div></div>`;
  }
  const conds = extractConditions(ability.lua);
  const condsHtml = conds.length
    ? `<div class="cond-chips">${conds.map((c) => `<span class="cond">${esc(c)}</span>`).join("")}</div>`
    : "";
  const luaHtml = ability.lua && ability.lua.trim()
    ? `<div><div class="expand-title">${esc(t("luaTitle"))}</div><pre class="lua">${highlightLua(ability.lua)}</pre></div>`
    : "";
  const actionsHtml = ability.actions
    ? `<div><div class="expand-title">${esc(t("actionsTitle"))}</div><div class="actions">${esc(ability.actions)}</div></div>`
    : "";
  const metaBits = [];
  metaBits.push(`${t("target")}: ${esc(ability.target)}`);
  metaBits.push(`${t("recast")}: ${esc(ability.recastDelay)}`);
  if (ability.spellId) {
    metaBits.push(`<a class="chip wowhead" href="${wowheadUrl(ability.spellId)}" target="_blank" rel="noopener noreferrer" data-stop="1">${esc(t("wowhead"))} #${ability.spellId}</a>`);
  }
  return `<div class="algo"><div class="algo-head">${prefixChips(prefix)}<b>${esc(base)}</b></div>
    <div class="expand-grid">
      <div class="meta">${metaBits.join(" ")}</div>
      ${condsHtml ? `<div><div class="expand-title">${esc(t("condTitle"))}</div>${condsHtml}</div>` : ""}
      ${luaHtml}
      ${actionsHtml}
    </div></div>`;
}

/** Other class variants of the same base name (not in A/B) as <details>. */
function otherVariantsHtml(pair) {
  if (!baseIndex) return "";
  const others = (baseIndex.get(pair.base) || [])
    .filter((ab) => ab.name !== pair.nameA && ab.name !== pair.nameB);
  if (!others.length) return "";
  const items = others.map((ab) => {
    const sp = splitAbilityName(ab.name);
    const body = ab.lua && ab.lua.trim()
      ? `<pre class="lua">${highlightLua(ab.lua)}</pre>` : `<i>${esc(t("notesEmpty"))}</i>`;
    const wow = ab.spellId
      ? ` <a class="chip wowhead" href="${wowheadUrl(ab.spellId)}" target="_blank" rel="noopener noreferrer" data-stop="1">W</a>` : "";
    return `<details class="variant"><summary>${prefixChips(sp.prefix)} ${esc(ab.name)}${ab.spellId ? ` <span class="chip">${ab.spellId}</span>` : ""}${wow}</summary>${body}</details>`;
  }).join("");
  return `<div class="variants"><div class="expand-title">${esc(t("otherVariants"))}</div>${items}</div>`;
}

function expandPairHtml(pair) {
  return `<tr class="expand" data-key="${esc(pair.key)}"><td colspan="5">
    <div class="expand-grid">
      <div class="algo-cols">
        <div class="algo-side"><div class="expand-title">A · ${esc(state.rotA || "")}</div>
          ${algoPanelHtml(pair.base, pair.nameA, pair.prefixA, pair.abilityA)}</div>
        <div class="algo-side"><div class="expand-title">B · ${esc(state.rotB || "")}</div>
          ${algoPanelHtml(pair.base, pair.nameB, pair.prefixB, pair.abilityB)}</div>
      </div>
      ${otherVariantsHtml(pair)}
    </div>
  </td></tr>`;
}

function rankCell(rank) {
  return `<span class="rank${rank == null ? " dim" : ""}">${rank == null ? "·" : rank}</span>`;
}

/** GitHub split-diff: two full aligned columns; click opens both algorithms. */
function renderSplit(align) {
  const rowsHtml = [];
  for (const pair of align.pairs) {
    if (pair.section) {
      rowsHtml.push(`<tr class="section-row"><td colspan="5">${esc(pair.base)}</td></tr>`);
      continue;
    }
    const open = expandedRows.has(pair.key);
    let kindCls = pair.kind;
    if (pair.kind === "both" && pair.delta !== 0) kindCls = "moved";
    if (pair.ghost) kindCls += " ghost";
    rowsHtml.push(`<tr class="row ${kindCls}${open ? " open" : ""}" data-key="${esc(pair.key)}">
      <td class="num">${rankCell(pair.rankA)}</td>
      <td class="side-a">${sideCellHtml(pair, "A")}</td>
      <td class="delta-col">${deltaHtml(pair)}</td>
      <td class="side-b">${sideCellHtml(pair, "B")}</td>
      <td class="num">${rankCell(pair.rankB)}</td>
    </tr>`);
    if (open) rowsHtml.push(expandPairHtml(pair));
  }

  return `<table class="merge split">
    <thead><tr>
      <th class="num" title="${esc(t("profileA"))}">A</th>
      <th>${esc(state.rotA || "")}</th>
      <th class="delta-col">${esc(t("colDelta"))}</th>
      <th>${esc(state.rotB || "")}</th>
      <th class="num" title="${esc(t("profileB"))}">B</th>
    </tr></thead>
    <tbody>${rowsHtml.join("")}</tbody>
  </table>
  <div class="status" style="text-align:left;padding:8px 0 0">${esc(t("expandHint"))}</div>`;
}

function renderCols(rotA, rotB) {
  const listA = columnList(rotA, currentData.abilities, state.showSections);
  const listB = columnList(rotB, currentData.abilities, state.showSections);

  const basesOf = (rot) => new Set(
    rot.entries.map((n) => splitAbilityName(n)).filter((s) => !s.section).map((s) => s.base));
  const inA = basesOf(rotA);
  const inB = basesOf(rotB);

  const panel = (rot, list, side, otherSet) => {
    const items = list.map((it) => {
      if (it.kind === "section") {
        return `<li class="is-section">${esc(it.name)}</li>`;
      }
      const otherHas = otherSet.has(it.base);
      const mark = !otherSet.size ? "" : (otherHas ? "" : (side === "a" ? " mark-a" : " mark-b"));
      const ghost = !it.ability;
      const spell = it.ability && it.ability.spellId
        ? `<a class="chip wowhead" href="${wowheadUrl(it.ability.spellId)}" target="_blank" rel="noopener noreferrer" data-stop="1">W</a>`
        : "";
      return `<li class="${mark}${ghost ? " ghost" : ""}">
        <span class="c-rank">${it.rank}</span>
        <span class="c-name">${prefixChips(it.prefix)}${esc(it.base)} ${spell}${ghost ? ` <span class="chip warn">${esc(t("ghostMissing"))}</span>` : ""}</span>
      </li>`;
    }).join("");
    return `<div class="col-panel">
      <h3><span class="badge badge-${side}">${side.toUpperCase()}</span> ${esc(rot.name)}</h3>
      <ul class="col-list">${items}</ul>
    </div>`;
  };

  return `<div class="cols">${panel(rotA, listA, "a", inB)}${panel(rotB, listB, "b", inA)}</div>`;
}

/* ---------- main render ---------- */

function render() {
  if (!currentData) return;
  const { rotations, abilities } = currentData;

  const rotA = rotations.find((r) => r.name === state.rotA) || rotations[0];
  const rotB = rotations.find((r) => r.name === state.rotB) || rotations[1] || rotations[0];
  state.rotA = rotA ? rotA.name : null;
  state.rotB = rotB ? rotB.name : null;

  renderNotes(rotA, rotB);

  const diffEl = $("diff");
  const statusEl = $("status");

  if (!rotA || !rotB) {
    statusEl.textContent = t("noRotations");
    statusEl.hidden = false;
    diffEl.hidden = true;
    renderStats(null);
    return;
  }
  if (rotA.name === rotB.name) {
    statusEl.textContent = t("sameProfile");
    statusEl.hidden = false;
  } else {
    statusEl.hidden = true;
  }

  const align = alignRotations(rotA, rotB, abilities, {
    sort: state.sort,
    onlyDiff: state.onlyDiff,
    showSections: state.showSections,
  });
  lastDiff = align;
  renderStats(align.stats);

  diffEl.hidden = false;
  if (state.view === "cols") {
    diffEl.innerHTML = renderCols(rotA, rotB);
  } else {
    diffEl.innerHTML = renderSplit(align);
    bindSplitRows();
  }
  writeHash();
}

function bindSplitRows() {
  $("diff").querySelectorAll("tr.row").forEach((tr) => {
    tr.addEventListener("click", (ev) => {
      // wowhead links open in new tab without toggling the row
      if (ev.target.closest("[data-stop]")) return;
      const key = tr.dataset.key;
      if (expandedRows.has(key)) expandedRows.delete(key);
      else expandedRows.add(key);
      render();
      const again = $("diff").querySelector(`tr.row[data-key="${cssEscape(key)}"]`);
      if (again) again.scrollIntoView({ block: "nearest" });
    });
  });
}

function buildBaseIndex() {
  baseIndex = new Map();
  if (!currentData) return;
  for (const ab of currentData.abilities.values()) {
    const sp = splitAbilityName(ab.name);
    if (sp.section) continue;
    if (!baseIndex.has(sp.base)) baseIndex.set(sp.base, []);
    baseIndex.get(sp.base).push(ab);
  }
}

function cssEscape(s) {
  if (window.CSS && CSS.escape) return CSS.escape(s);
  return s.replace(/["\\]/g, "\\$&");
}

/* ---------- selects & events ---------- */

function fillClassSelect(classes) {
  const sel = $("sel-class");
  sel.innerHTML = classes.map((c) =>
    `<option value="${esc(c)}"${c === state.cls ? " selected" : ""}>${esc(c)}</option>`
  ).join("");
  if (!state.cls || !classes.includes(state.cls)) {
    state.cls = classes[0];
    sel.value = state.cls;
  }
}

function fillRotationSelects() {
  const rots = currentData.rotations;
  const makeOpts = (selected) => rots.map((r) =>
    `<option value="${esc(r.name)}"${r.name === selected ? " selected" : ""}>${esc(r.name)}</option>`
  ).join("");
  $("sel-a").innerHTML = makeOpts(state.rotA);
  $("sel-b").innerHTML = makeOpts(state.rotB);
}

async function selectClass(cls) {
  state.cls = cls;
  expandedRows.clear();
  setStatus(t("loading"));
  try {
    currentData = await loadClass(cls);
    buildBaseIndex();
    const names = currentData.rotations.map((r) => r.name);
    if (!names.includes(state.rotA)) state.rotA = names[0];
    if (!names.includes(state.rotB)) state.rotB = names[1] || names[0];
    fillRotationSelects();
    $("status").hidden = true;
    render();
  } catch (e) {
    console.error(e);
    setStatus(`${t("loadError")}: ${e.message}`, true);
  }
}

function setStatus(msg, isError) {
  const el = $("status");
  el.textContent = msg;
  el.classList.toggle("error", !!isError);
  el.hidden = !msg;
  $("diff").hidden = !!msg;
}

function syncControls() {
  $("sel-sort").value = state.sort;
  $("chk-diff").checked = state.onlyDiff;
  $("chk-sections").checked = state.showSections;
  $("view-merge").classList.toggle("active", state.view === "merge");
  $("view-cols").classList.toggle("active", state.view === "cols");
}

function measureTopbar() {
  const tb = document.querySelector(".topbar");
  if (tb) document.documentElement.style.setProperty("--topbar-h", tb.offsetHeight + "px");
}

/* ---------- init ---------- */

async function init() {
  readHash();
  setLang(state.lang);
  syncControls();

  $("lang-ru").addEventListener("click", () => switchLang("ru"));
  $("lang-en").addEventListener("click", () => switchLang("en"));
  $("btn-swap").addEventListener("click", () => {
    const t0 = state.rotA;
    state.rotA = state.rotB;
    state.rotB = t0;
    fillRotationSelects();
    expandedRows.clear();
    render();
  });
  $("sel-class").addEventListener("change", (e) => selectClass(e.target.value));
  $("sel-a").addEventListener("change", (e) => {
    state.rotA = e.target.value;
    expandedRows.clear();
    render();
  });
  $("sel-b").addEventListener("change", (e) => {
    state.rotB = e.target.value;
    expandedRows.clear();
    render();
  });
  $("sel-sort").addEventListener("change", (e) => {
    state.sort = e.target.value;
    render();
  });
  $("chk-diff").addEventListener("change", (e) => {
    state.onlyDiff = e.target.checked;
    render();
  });
  $("chk-sections").addEventListener("change", (e) => {
    state.showSections = e.target.checked;
    render();
  });
  document.querySelectorAll(".view-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      state.view = btn.dataset.view;
      syncControls();
      render();
    });
  });

  window.addEventListener("resize", measureTopbar);
  measureTopbar();

  setStatus(t("loading"));
  try {
    const classes = await getClasses();
    fillClassSelect(classes);
    await selectClass(state.cls);
  } catch (e) {
    console.error(e);
    setStatus(`${t("loadError")}: ${e.message}. ${location.port ? "" : "— " + t("footer")}`, true);
  }
}

function switchLang(lang) {
  state.lang = lang;
  setLang(lang);
  writeHash();
  if (currentData) render();
}

document.addEventListener("DOMContentLoaded", init);
