/* Load PQR profile XMLs from /PQR_fixed/Profiles/ and build an in-memory model.
 * Entity decoding mirrors clsXML.XMLDecode (second pass after the XML parser). */
"use strict";

const PROFILES_BASE = "/PQR_fixed/Profiles/";

const WOW_CLASSES = [
  "DEATHKNIGHT", "DRUID", "HUNTER", "MAGE", "PALADIN",
  "PRIEST", "ROGUE", "SHAMAN", "WARLOCK", "WARRIOR",
];

/** Second decode pass — same order as reversed/pqr-app/.../clsXML.cs XMLDecode. */
function xmlDecode(s) {
  if (s == null) return "";
  return s
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, "&");
}

function textOf(root, tag) {
  const el = root.querySelector(tag);
  return el ? xmlDecode(el.textContent) : "";
}

function isSectionName(name) {
  return name.startsWith("--");
}

/** Parse DarhangeR_<CLASS>_Abilities.xml */
function parseAbilities(xmlText) {
  const doc = new DOMParser().parseFromString(xmlText, "application/xml");
  if (doc.querySelector("parsererror")) {
    throw new Error("Abilities XML: parse error");
  }
  const map = new Map();
  for (const el of doc.querySelectorAll("Ability")) {
    const name = textOf(el, "Name").trim();
    if (!name) continue;
    const ability = {
      name,
      spellId: parseInt(textOf(el, "SpellID"), 10) || 0,
      actions: textOf(el, "Actions").trim(),
      lua: textOf(el, "Lua"),
      luaBefore: textOf(el, "LuaBefore"),
      luaAfter: textOf(el, "LuaAfter"),
      recastDelay: textOf(el, "RecastDelay").trim() || "0",
      target: textOf(el, "Target").trim() || "Target",
      cancelChannel: textOf(el, "CancelChannel").trim(),
      isSection: isSectionName(name),
    };
    if (!map.has(name)) map.set(name, ability);
  }
  return map;
}

/** Parse DarhangeR_<CLASS>_Rotations.xml */
function parseRotations(xmlText) {
  const doc = new DOMParser().parseFromString(xmlText, "application/xml");
  if (doc.querySelector("parsererror")) {
    throw new Error("Rotations XML: parse error");
  }
  const rotations = [];
  for (const el of doc.querySelectorAll("Rotation")) {
    const name = textOf(el, "RotationName").trim();
    if (!name) continue;
    const rawList = textOf(el, "RotationList");
    const entries = [];
    const seen = new Set();
    for (const part of rawList.split("|")) {
      const n = part.trim();
      if (!n || seen.has(n)) continue;
      seen.add(n);
      entries.push(n);
    }
    rotations.push({
      name,
      isDefault: textOf(el, "RotationDefault").trim() === "true",
      requireCombat: textOf(el, "RequireCombat").trim(),
      notes: textOf(el, "RotationNotes").trim(),
      entries,
    });
  }
  return rotations;
}

/** Fetch the Profiles/ directory listing; returns { cls: {abilities, rotations} } filenames. */
async function discoverClassFiles() {
  const res = await fetch(PROFILES_BASE);
  if (!res.ok) {
    throw new Error(`Profiles dir: HTTP ${res.status}`);
  }
  const html = await res.text();
  const doc = new DOMParser().parseFromString(html, "text/html");
  const files = [...doc.querySelectorAll("a")].map((a) => a.getAttribute("href") || "")
    .map((h) => decodeURIComponent(h.split("/").pop() || ""))
    .filter(Boolean);

  const found = {};
  for (const cls of WOW_CLASSES) {
    const ab = files.find((f) => f.endsWith(`_${cls}_Abilities.xml`));
    const ro = files.find((f) => f.endsWith(`_${cls}_Rotations.xml`));
    if (ab && ro) found[cls] = { abilities: ab, rotations: ro };
  }
  if (Object.keys(found).length === 0) {
    throw new Error("No class profiles found in " + PROFILES_BASE);
  }
  // keep canonical 3.3.5a class order
  const ordered = {};
  for (const cls of WOW_CLASSES) {
    if (found[cls]) ordered[cls] = found[cls];
  }
  return ordered;
}

const classCache = new Map();
let classFiles = null;

async function getClasses() {
  if (!classFiles) classFiles = await discoverClassFiles();
  return Object.keys(classFiles);
}

/** Lazily fetch + parse Abilities and Rotations for one class. */
async function loadClass(cls) {
  if (classCache.has(cls)) return classCache.get(cls);
  if (!classFiles) classFiles = await discoverClassFiles();
  const files = classFiles[cls];
  if (!files) throw new Error("Unknown class: " + cls);

  const promise = (async () => {
    const [abRes, roRes] = await Promise.all([
      fetch(PROFILES_BASE + files.abilities),
      fetch(PROFILES_BASE + files.rotations),
    ]);
    if (!abRes.ok) throw new Error(`Abilities ${cls}: HTTP ${abRes.status}`);
    if (!roRes.ok) throw new Error(`Rotations ${cls}: HTTP ${roRes.status}`);
    const abilities = parseAbilities(await abRes.text());
    const rotations = parseRotations(await roRes.text());
    if (rotations.length === 0) throw new Error(`No rotations in ${cls}`);
    return { cls, abilities, rotations };
  })();

  classCache.set(cls, promise);
  try {
    return await promise;
  } catch (e) {
    classCache.delete(cls);
    throw e;
  }
}
