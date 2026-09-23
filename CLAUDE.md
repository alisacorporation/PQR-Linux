# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`AGENTS.md` at the repo root is the primary spec for this project and stays authoritative; `reversed/ARCHITECTURE.md` is the deeper reference for the decompiled internals. Read `AGENTS.md` before non-trivial work — the notes below are the parts most likely to bite.

## What this repo is

Continuation of PQR 1.8.5, an external WoW WotLK 3.3.5a rotation tool (closed-source since 2020). Contents: ILSpy-decompiled C# rebuilt for Wine/Linux (`reversed/`), the deployable runtime bundle (`PQR_fixed/`), and edited community XML rotation profiles. There is no CI, no test suite, no lint, and no `.sln`.

## Build

```sh
export PATH="$HOME/.dotnet:$PATH"              # dotnet SDK lives in ~/.dotnet, not on PATH
dotnet build reversed/blackmagic -c Release    # must build first
dotnet build reversed/pqr-app -c Release       # -> reversed/pqr-app/bin/Release/net40/
```

- Build order is load-bearing: `pqr-app` HintPaths `../blackmagic/bin/Release/net20/BlackMagic.dll`.
- The build also hard-depends on the pristine upstream `PQR_DarhangeR_3.3.5a/` directory (its own gitignored `.git`) for `fasmdll_managed.dll` and `SyntaxHighlighter.dll` HintPaths. Don't delete or move it.
- `reversed/fasm/` and `reversed/probes/` are reference-only, not in the build.
- CS0649 warnings from decompiled WinForms code are expected; success = 0 errors.
- Deploy C# changes by copying `PriorityQueueRotation.exe` (and `BlackMagic.dll` if touched) from bin output into `PQR_fixed/`.

## Run

```sh
cd PQR_fixed && wine PriorityQueueRotation.exe
```

- Needs real .NET 4 (`winetricks dotnet40`); Mono runs but enumeration is buggier.
- PQR and WoW must share one wineserver/prefix — Lutris/Proton WoW is invisible to plain-wine PQR (empty process dropdown). Process name must be exactly `Wow`, character must be in the world.
- Ability/rotation editors resolve `Profiles\` relative to CWD, so always launch from inside `PQR_fixed/`.
- Offsets in `Offsets_12340.xml` are RVAs: absolute = `0x400000` + offset for 3.3.5a (build 12340).

## Profiles (main editing surface)

Files: `PQR_fixed/Profiles/DarhangeR_<CLASS>_{Abilities,Rotations}.xml`, root `<CLASS>`. App discovery glob is `*_<CLASS>_Rotations.xml` — keep the `Prefix_CLASS_` filename pattern.

- **Silent failure trap:** `frmMain.AddAbilityToCurrent` does exact (trimmed) string match of pipe-separated `RotationList` entries against Ability `<Name>` values. Mismatches are dropped with no error. Re-check every name added to a `RotationList`, including prefixes like `R:`, `B:`, `PvP:`.
- Abilities with `SpellID 0` (e.g. `-- Functions --`, `-- Hotkeys --`) are section markers / shared Lua, not casts. Shared helpers (`getHp`, `rangeCheck`, `CanAttack`, …) live inside `-- Functions --`'s Lua guarded by `FuncLoaded`; every ability test closure runs each tick in priority order, which is what loads them. Keep `-- Functions --` early in every `RotationList`; new shared helpers follow the same pattern.
- Entity escaping quirk: legacy lines are double-escaped (`&amp;quot;`) because `clsXML.XMLEncode` escapes `&` last and `XMLDecode` runs a second pass after the XML parser (see `clsXML.cs:575-590`). Both `&quot;` and `&amp;quot;` load to `"`. Use standard single XML escaping in new edits; never leave raw `<` or `&`.
- Profile XMLs generally have no trailing newline — preserve that.
- Only automated check available: `xmllint --noout <file>` per touched XML. Run it after every profile edit.

## In-game test hook

`/run PQR_TestMode = true` (disable with `= nil`) bypasses `UnitIsPlayer`/class gates so PvP offensive abilities fire on training dummies. Defensive HP/mana triggers are not bypassed — exercise those with real damage. Documented in Resto_PvP `RotationNotes`.

## Wine-specific code to not regress

`SThread.GetMainThreadId` in `reversed/blackmagic/` uses a Toolhelp thread snapshot, **not** `Process.Threads[0]` (which NREs under Wine). Keep it that way.

## Conventions

- Commit style: `<Class or area>: <imperative summary>` with a body explaining the *why* (see `git log`). Single-author, direct commits to `main`.
- Offsets and profile XML formats must stay byte-compatible with the upstream community ecosystem — those formats are the project's value. See `reversed/ARCHITECTURE.md` §10.
- Tracked content is limited to `.gitignore`, `PQR_fixed/`, and `reversed/`. `mempalace.yaml` and `entities.json` are local tool files — gitignored, never commit or edit.
