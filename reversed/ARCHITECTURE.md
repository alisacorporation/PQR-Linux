# PQR Architecture — Reverse Engineering Notes

> Recovered from `PriorityQueueRotation.exe` 1.8.5 (closed-source since 2020).
> Decompiled C# source lives next to this file (`pqr-app/`, `blackmagic/`, `fasm/`), generated with ILSpy.
> Purpose: let people continue / reimplement the project without the original source.

## 1. The Big Picture

PQR is NOT a bot brain running in C#. It has two halves:

```
┌─────────────────────────────┐        ┌──────────────────────────────────┐
│  PQR (C#, external)         │        │  WoW.exe (target process)        │
│                             │        │                                  │
│  frmSelect ── find & pick   │        │  [detoured function]             │
│  frmMain   ── UI/hotkeys    │        │       │ jmp→ trampoline          │
│  clsXML    ── profiles      │  ASM   │       ▼                          │
│  clsOffsets── offsets       │────────▶  code cave executor            │
│  clsMemory ── attach/read   │ writes │       │ calls Lua_DoString()     │
│  Executor  ── inject ASM    │        │       ▼                          │
│                             │        │  LUA VM = THE ACTUAL BOT BRAIN   │
│                             │        │  OnUpdate(100ms): walk priority  │
│                             │        │  table → CastSpellByID           │
└─────────────────────────────┘        └──────────────────────────────────┘
```

The C# half only **attaches, patches a detour, and injects a Lua framework**.
After bootstrapping, the entire rotation logic executes *inside* WoW's own Lua VM,
ticking on an invisible frame's `OnUpdate`. Spells are cast with real Lua API
(`CastSpellByID`) — no simulated keypresses anywhere.

## 2. Process Discovery & Validation (why your list can be empty)

`clsMemory.WoWProcesses()`:

1. `Process.GetProcessesByName("Wow")` — **exact name**, case-insensitive.
2. For each candidate:
   - `BlackMagic.OpenProcessAndThread(pid)` — opens process + first thread
     (`Process.GetProcessById(pid).Threads[0]`). Any failure → silently skipped.
   - Reads **5 UTF8 bytes** at `ImageBase + <WoWVersionOffset>` and compares as a
     **string** against each loaded offsets file's `<CurrentWoWVersion>` (e.g. `"12340"`).
   - If version matches, reads 30 bytes at `ImageBase + <PlayerName>`.
     **If empty → process is skipped.** PlayerName static is zero until a character
     is logged into the world.

**Consequences:**
- Sitting at login / character-select screen ⇒ dropdown is empty *by design*. Log into the world first, hit Refresh.
- Offsets are **relative to ImageBase (0x400000 for 3.3.5a)** — absolute VA = `0x400000 + offset`. Verified under Wine: version string `"12340"` reads correctly at `base+0x8AD851`.

## 3. Attachment (BlackMagic.dll)

- `OpenProcess` with `PROCESS_ALL_ACCESS`; optional debug privileges.
- Main thread handle from .NET `process.Threads[0]`.
- Module base from `Process.Modules[0].BaseAddress`.
- All reads/writes via classic `ReadProcessMemory`/`WriteProcessMemory`.

## 4. The Executor (the clever bit)

`Executor.cs` — a self-clearing code-cave detour on a frequently-called game function:

Setup (once):
- Allocate `codeCavePtr` (4 bytes, holds "pending payload" pointer, initially 0)
- Allocate `DetourPtr` (598-byte trampoline region)
- Write original prologue bytes (`<Overwritten>` from XML, e.g. `55 8B EC 81 EC F8 00 00 00`)
  at `DetourPtr`, followed by the stub:
  ```asm
  pushfd / pushad
  mov eax, [codeCavePtr]
  cmp eax, 0
  je @out
  call eax              ; run queued payload ON THE GAME'S MAIN THREAD
  mov eax, codeCavePtr
  xor edx, edx / mov [eax], edx   ; self-clear => handshake done
  @out:
  popad / popfd
  jmp <detour_addr + len(prologue)>   ; back to original function
  ```
- Patch original function start with `jmp DetourPtr`.

Executing anything (incl. Lua):
- Assemble payload with **ManagedFasm** (fasmdll_managed.dll) into allocated memory.
- Store its address in `codeCavePtr`.
- Spin-wait until the game thread clears it (3 s timeout). No CreateRemoteThread, no races.

Anti-signature trick: `RandomizeASM()` sprinkles random no-op instructions
(`mov eax,eax`, `push/pop`, `xchg` pairs) between every real instruction, so the
injected stub bytes differ every session.

## 5. Calling Lua & getting return values

`clsMemory.Lua_GetReturnValue(cmd)` builds one x86 payload that:

```asm
push 0 / push <cmd_ptr> / push <cmd_ptr>
mov eax, <base+Lua_DoStringAddress> ; call Lua_DoString
call eax / add esp, 0xC
call <base+ClntObjMgrGetActivePlayerObjAddress>
test eax, eax / je @out
mov ecx, eax                        ; player object
push -1 / push <arg_ptr>
call <base+Lua_GetLocalizedTextAddress>  ; fetch return value
mov [<result_buf>], eax
@out: retn
```
Result buffer read back with `ReadUTF8String`. `WriteToChat` is just
`DEFAULT_CHAT_FRAME:AddMessage(...)` through the same path.

## 6. The injected Lua framework (the real bot)

`clsLua.cs` contains big embedded Lua strings injected at startup:

- `PQR_SetupTables()` — creates `PQR[i].priorityTable` arrays per rotation slot:
  index, spell id, action string, **test closure**, recast delay, target type,
  cancelChannel, luaBefore/luaAfter closures.
- Each profile ability compiles to `function pqrFunc<N>() <lua test> end` plus
  Before/After variants, registered via `PQR_AddAbility(...)`.
- An invisible `PQR_EventFrame` with `OnUpdate` handler ticks every
  `PQR_UpdateInterval` ms (configurable 20–1000, default 100):
  movement tracking → recast-delay management → interrupt engine → `PQR_ExecuteBot()`.
- `PQR_ExecuteBot()` walks the priority table top-down; first ability whose test
  returns true gets cast (`CastSpellByID` / action string) honoring target rules
  (Target/Mouseover/Click/Custom), recast delays and channel-cancel flags.
- Helper library: `PQR_IsOutOfSight` (line-of-sight cache table),
  `PQR_IsMoving`, `PQR_IsCastingSpell`, `UnitBuffID`/`UnitDebuffID` wrappers,
  `PQR_WriteToChat`, debug facilities.
- Anti-detection: all `pqr`/`UnitBuffID`/`UnitDebuffID` identifiers are renamed to
  per-session random names before injection (`ReplacePQR`).

## 7. Profile format

`Profiles/<CLASS>_Abilities.xml` — list of `<Ability>` entries:
`Name` (with mode prefixes like `F:`/`B:`/`U:` shown in UI), `SpellID`,
`Actions` (fallback macro text), `Lua` (the test — return true to cast),
`RecastDelay`, `Target` (Target/Mouseover/Click/Custom), `CancelChannel`,
optional `LuaBefore`/`LuaAfter`.

`Profiles/<CLASS>_Rotations.xml` — named rotations whose `<RotationList>` is a
pipe-separated ordered list of ability names (the priority order). Prefix letters
map abilities to sub-modes of the same class spec set.

## 8. Offsets XML — full schema

Loaded lazily after process selection (`LoadXML_Offsets(version)`). **All addresses
are RVA-style, add ImageBase yourself.**

| Field | Meaning |
|---|---|
| `CurrentWoWVersion` | build number, also matched as ASCII string during discovery |
| `WoWVersionOffset` | where build string lives (e.g. `12340`) |
| `PlayerName` | static ptr-to-name slot used for discovery & status |
| `PlayerClass` | byte class id (1=Warrior … 11=Druid) |
| `GameState` | 0=login/charselect, 1=in world |
| `GetCurrentKeyBoardFocus` | nonzero while chat/editbox focused (pause typing safety) |
| `ClntObjMgrGetActivePlayerObjAddress` | returns active player obj ptr (for GetLocalizedText) |
| `Lua_DoStringAddress` | `FrameScript::Execute`-style DoString entry |
| `Lua_GetLocalizedTextAddress` | fetches Lua return value into buffer |
| `Detour` | frequently-called fn to hijack for the executor |
| `Overwritten` | exactly 9 bytes currently at Detour (restored on unhook) |
| `ObjectFieldGUID` | unit GUID field offset |

For 3.3.5a (build 12340) shipped values work against stock clients — verified live
under Wine: version string ✓, DoString prologue ✓, GameState ✓.

## 9. Linux/Wine findings (validated experimentally)

- Works under plain Wine ≥11 with **real .NET 4** (winetricks dotnet40); Mono also
  launches the app but real .NET removes a class of enumeration bugs.
- `GetProcessesByName("Wow")`, `OpenProcess`, `ReadProcessMemory`, `MainModule`,
  thread enumeration: all functional in same wineserver session.
- **Critical:** PQR and WoW must share ONE wineserver/prefix. Lutris+Proton runs
  its own wineserver ⇒ processes mutually invisible. Run both via plain `wine`,
  or run PQR inside the same Proton/umu env as the game.
- Empty-dropdown checklist: (1) not logged into world, (2) different wine sessions.

## 10. Continuation roadmap ideas

1. **Drop-in fix pack:** keep binary, ship corrected `Offsets_*.xml` for custom
   server clients (all fields documented above).
2. **Clean-room rewrite:** the design above is small (~3k LOC core). A modern
   cross-platform port needs: memory access (Windows API / `process_vm_readv` +
   ptrace on Linux), a main-thread execution primitive (same detour technique
   works — write trampoline via `/proc/pid/mem`), FASM JIT (flat assembler exists
   natively for Linux), input simulation (uinput/XTEST) only for Click-to-Cast
   targeting, GUI in Avalonia/GTK.
3. **Profile compatibility:** keep the XML formats verbatim — the community's value
   (hundreds of tuned ability Lua snippets) lives there, not in the C#.
