/* RU/EN strings for the viewer UI. */
"use strict";

const I18N = {
  ru: {
    brandSuffix: "Сравнение профилей",
    class: "Класс",
    profileA: "Профиль A",
    profileB: "Профиль B",
    sortBy: "Сортировка",
    sortOrderA: "Порядок A",
    sortOrderB: "Порядок B",
    sortAlpha: "Алфавит",
    sortDelta: "Расхождения первыми",
    onlyDiff: "Только различия",
    showSections: "Показать секции",
    viewMerge: "Дифф",
    viewCols: "Списки",
    legendBoth: "в обоих",
    legendOnlyA: "только в A",
    legendOnlyB: "только в B",
    legendDelta: "↑ раньше в B · ↓ позже в B",
    footer: "Данные читаются напрямую из PQR_fixed/Profiles/*.xml. Запуск: python3 -m http.server из корня репозитория.",
    loading: "Загрузка…",
    loadError: "Ошибка загрузки",
    statCommon: "общих",
    statOnlyA: "только A",
    statOnlyB: "только B",
    statSamePos: "совпадение позиций",
    statCountA: "скиллов в A",
    statCountB: "скиллов в B",
    statGhost: "без определения",
    colRank: "#",
    colAbility: "Способность",
    colDelta: "Δ",
    expandHint: "Клик по строке — алгоритмы обеих сторон",
    missingSide: "— нет в этом профиле —",
    alsoIn: "также",
    otherVariants: "Другие варианты этого скилла в классе",
    ghostMissing: "нет в Abilities",
    condTitle: "Условия срабатывания (из Lua)",
    luaTitle: "Lua-тест",
    actionsTitle: "Actions (макрос)",
    notesEmpty: "Заметок нет.",
    showMore: "показать полностью",
    showLess: "свернуть",
    target: "цель",
    recast: "перезапуск",
    cancelChan: "cancelChannel",
    wowhead: "Wowhead",
    noRotations: "Нет ротов для сравнения — выберите другой класс.",
    sameProfile: "Выбран один и тот же профиль — сравнение пустое.",
    deltaUp: "в B раньше на",
    deltaDown: "в B позже на",
    cond: {
      hp: "HP {unit} {op} {n}%",
      mana: "маны {unit} {op} {n}%",
      combat: "в бою",
      needsTarget: "нужна цель",
      needsFocus: "нужен фокус",
      needsUnit: "нужен юнит {u}",
      range: "в радиусе",
      cd: "кулдаун готов",
      known: "изучена",
      playerOnly: "только игрок",
      buff: "проверка баффа",
      debuff: "проверка дебаффа",
      cast: "каст/канал",
      testMode: "PQR_TestMode",
      moving: "движение",
      boss: "босс/элита",
      dead: "мёртв",
      classGate: "фильтр класса",
    },
  },
  en: {
    brandSuffix: "Profile compare",
    class: "Class",
    profileA: "Profile A",
    profileB: "Profile B",
    sortBy: "Sort by",
    sortOrderA: "Order A",
    sortOrderB: "Order B",
    sortAlpha: "Alphabetical",
    sortDelta: "Biggest deltas first",
    onlyDiff: "Differences only",
    showSections: "Show sections",
    viewMerge: "Diff",
    viewCols: "Lists",
    legendBoth: "in both",
    legendOnlyA: "only in A",
    legendOnlyB: "only in B",
    legendDelta: "↑ earlier in B · ↓ later in B",
    footer: "Data read directly from PQR_fixed/Profiles/*.xml. Run: python3 -m http.server from the repo root.",
    loading: "Loading…",
    loadError: "Load error",
    statCommon: "common",
    statOnlyA: "only A",
    statOnlyB: "only B",
    statSamePos: "same position",
    statCountA: "skills in A",
    statCountB: "skills in B",
    statGhost: "undefined",
    colRank: "#",
    colAbility: "Ability",
    colDelta: "Δ",
    expandHint: "Click a row for both sides' algorithms",
    missingSide: "— missing here —",
    alsoIn: "also",
    otherVariants: "Other variants of this skill in class",
    ghostMissing: "missing in Abilities",
    condTitle: "Cast conditions (from Lua)",
    luaTitle: "Lua test",
    actionsTitle: "Actions (macro)",
    notesEmpty: "No notes.",
    showMore: "show full",
    showLess: "collapse",
    target: "target",
    recast: "recast",
    cancelChan: "cancelChannel",
    wowhead: "Wowhead",
    noRotations: "No rotations to compare — pick another class.",
    sameProfile: "Same profile selected on both sides.",
    deltaUp: "earlier in B by",
    deltaDown: "later in B by",
    cond: {
      hp: "HP {unit} {op} {n}%",
      mana: "mana {unit} {op} {n}%",
      combat: "in combat",
      needsTarget: "needs target",
      needsFocus: "needs focus",
      needsUnit: "needs {u}",
      range: "in range",
      cd: "cooldown ready",
      known: "spell known",
      playerOnly: "players only",
      buff: "buff check",
      debuff: "debuff check",
      cast: "casting/channeling",
      testMode: "PQR_TestMode",
      moving: "movement",
      boss: "boss/elite",
      dead: "dead",
      classGate: "class filter",
    },
  },
};

let currentLang = "ru";

function t(key) {
  const dict = I18N[currentLang] || I18N.ru;
  const parts = key.split(".");
  let cur = dict;
  for (const p of parts) {
    cur = cur && cur[p];
    if (cur == null) return key;
  }
  return cur;
}

function tf(key, vars) {
  let s = t(key);
  for (const [k, v] of Object.entries(vars || {})) {
    s = s.replace(new RegExp("\\{" + k + "\\}", "g"), v);
  }
  return s;
}

function setLang(lang) {
  currentLang = I18N[lang] ? lang : "ru";
  document.documentElement.lang = currentLang;
  document.querySelectorAll("[data-i18n]").forEach((el) => {
    el.textContent = t(el.getAttribute("data-i18n"));
  });
  document.getElementById("lang-ru").classList.toggle("active", currentLang === "ru");
  document.getElementById("lang-en").classList.toggle("active", currentLang === "en");
}
