# AGENTS.md

Continuation of PQR 1.8.5 (external WoW WotLK 3.3.5a rotation tool, closed-source since 2020): ILSpy-decompiled C# rebuilt for Wine/Linux, plus edited community profiles. `reversed/ARCHITECTURE.md` is the real spec for how injection, offsets, and the Lua bootstrap work — read it before touching that code.

## Layout

- `reversed/` — decompiled source (tracked):
  - `pqr-app/` — main WinForms exe (net40, x86). App logic in `PriorityQueueRotation/*.cs`.
  - `blackmagic/` — process attach/read/write lib (net20). Contains the Wine fix: `SThread.GetMainThreadId` uses a Toolhelp thread snapshot, **not** `Process.Threads[0]` (NREs under Wine). Do not regress this.
  - `fasm/` — decompiled C++/CLI FASM wrapper. Reference only, **not in the build** (machine-specific broken HintPaths). The build uses `fasmdll_managed.dll` from the upstream dir below.
  - `probes/` — standalone diagnostic `.cs`, not compiled by any project.
- `PQR_fixed/` — deployable runtime: committed exe + `.exe.config` + DLLs + `Offsets_*.xml` + `Fonts/` + `Profiles/`. What users actually run.
- `PQR_DarhangeR_3.3.5a/` — pristine upstream clone (own `.git`, gitignored, read-only). **Builds hard-depend on it**: csproj HintPaths point at its `fasmdll_managed.dll` and `SyntaxHighlighter.dll`.
- `viewer/` — static profile-compare site (plain HTML/CSS/JS, no build, no npm). Reads `PQR_fixed/Profiles/*.xml` at runtime.
- `mempalace.yaml`, `entities.json` — local MemPalace tool files; gitignored, never commit or edit.
- Tracked content is only `.gitignore`, `PQR_fixed/`, `reversed/`, `viewer/`. No CI, no tests, no lint, no `.sln`.

## Build (verified on Linux)

```sh
export PATH="$HOME/.dotnet:$PATH"   # dotnet is NOT on PATH; SDK lives in ~/.dotnet
dotnet build reversed/blackmagic -c Release   # must build first
dotnet build reversed/pqr-app -c Release      # -> reversed/pqr-app/bin/Release/net40/
```

- Build order matters: `pqr-app` references `../blackmagic/bin/Release/net20/BlackMagic.dll`.
- Success criterion: 0 errors. CS0649 warnings from decompiled WinForms code are expected.
- After C# changes, deploy by copying `PriorityQueueRotation.exe` and `PriorityQueueRotation.exe.config` (and `BlackMagic.dll` if changed) from the bin output into `PQR_fixed/`. The `.exe.config` is mandatory: without it, Mono cannot resolve the `<userSettings>` section when reading the Wine `user.config` and dies with `ConfigurationErrorsException: Unrecognized configuration section <userSettings>` on character select.

## Profiles (main editing surface)

- Files: `PQR_fixed/Profiles/DarhangeR_<CLASS>_{Abilities,Rotations}.xml`, root element `<CLASS>` (e.g. `<DRUID>`). The app discovers profiles by glob `*_<CLASS>_Rotations.xml` — keep the `Prefix_CLASS_` filename pattern.
- Abilities: `<Ability>` with `Name`, `SpellID`, `Actions`, `Lua` (test — return true → cast), `RecastDelay`, `Target` (Target/Mouseover/Click/Player/Focus/Custom), `CancelChannel`, `LuaBefore`/`LuaAfter`. Entries with `SpellID 0` like `-- Functions --` or `-- Hotkeys --` are section markers / shared Lua, not casts.
- Rotations: `<Rotation>` with `RotationName`, `RotationDefault`, `RotationList` (pipe-separated priority order of exact Ability `<Name>` strings, including prefixes like `R:`, `B:`, `PvP:`), `RequireCombat`, `RotationNotes`.
- **Silent failure:** `frmMain.AddAbilityToCurrent` does exact string match (after trim) of RotationList entries against Ability names; a mismatched name is dropped with no error. Double-check every name you add to a `RotationList`.
- Shared helper functions (`getHp`, `rangeCheck`, `CanAttack`, …) are defined inside the `-- Functions --` ability's Lua, guarded by `FuncLoaded`, and run because every ability test closure executes each tick in priority order. Keep `-- Functions --` early in every RotationList; new shared helpers follow the same pattern.
- Verify after every profile edit: `xmllint --noout <file>` per touched XML (the only automated check that exists).
- Entity escaping: legacy lines are double-escaped (`&amp;quot;`) because the app's `XMLEncode` escapes `&` last and `XMLDecode` runs a second pass after the XML parser (see `clsXML.cs:575-590`). Both `&quot;` (standard) and `&amp;quot;` load to `"` — use standard single XML escaping in new edits; never leave raw `<` or `&` (breaks parsing).
- Profile XML files generally have no trailing newline; keep that.

## Viewer (profile compare site)

```sh
python3 -m http.server 8000    # from the repo ROOT, not from viewer/
# open http://localhost:8000/viewer/
```

- Must serve from repo root: the site fetches `/PQR_fixed/Profiles/` (directory listing + XML). `file://` and serving inside `viewer/` will not work.
- No bundler/deps — keep it that way; edit `viewer/js/*.js` directly (`i18n` RU/EN, `parse` XML+double-decode, `compare` rank-diff, `app` UI).
- Entity decode in `parse.js` must stay in sync with `clsXML.XMLDecode` (second pass after the XML parser) or legacy `&amp;quot;` profiles will show garbage.
- `compare.js` aligns rotations by **base ability name** (mode prefixes `R:`/`F:`/`PvP:`/`PvP_BG:` stripped via whitelist regex — never split on bare `:` or `Power Word: Shield` breaks). XML files are never renamed by the viewer; ghost rows flag RotationList entries missing from Abilities.

## Running under Wine

```sh
cd PQR_fixed && wine PriorityQueueRotation.exe
```

- Real .NET 4 required (`winetricks dotnet40`); Mono launches but enumeration is buggier.
- PQR and WoW must share **one** wineserver/prefix. Lutris/Proton WoW is invisible to plain-wine PQR → empty process dropdown.
- Empty dropdown checklist: (1) character not logged into the world (PlayerName static still zero), (2) different wine session, (3) process name must be exactly `Wow`.
- Offsets are RVAs: absolute address = ImageBase (`0x400000` for 3.3.5a) + offset. `Offsets_12340.xml` = build 12340.
- Main loads resolve relative to the exe directory, but the ability/rotation **editors** resolve `Profiles\` relative to CWD — always run from inside `PQR_fixed/`.

## In-game profile testing

- `/run PQR_TestMode = true` (disable: `= nil`) bypasses `UnitIsPlayer`/class gates so PvP offensive abilities fire on training dummies. Defensive HP/mana triggers are not bypassed — test those with real damage. Documented in Resto_PvP `RotationNotes`.

## Conventions

- Commit messages: `<Class or area>: <imperative summary>` plus a body explaining the why (see `git log`). Single-author, direct commits to `main`.
- Offsets/profile formats must stay byte-compatible with the community ecosystem — the XML formats are the project's value; keep them verbatim (see roadmap in `reversed/ARCHITECTURE.md` §10).
