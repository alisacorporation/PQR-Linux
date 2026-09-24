# Rust rewrite of PQR — headless MVP (v0.1)

## Context

Текущий PQR — WinForms .NET 4 x86 приложение 2011 года постройки, ILSpy-декомпилировано в `reversed/`. Оно работает, но:

- Тянет за собой .NET Framework 4, `fasmdll_managed.dll`, C++/CLI FASM-обёртку и BlackMagic 2009 года.
- На Wine стреляет в ногу (`SThread.GetMainThreadId` пришлось патчить через Toolhelp — см. `reversed/blackmagic/Magic/SThread.cs`).
- Формат приоритетов — pipe-separated строка, `frmMain.AddAbilityToCurrent` тихо роняет опечатки (`AGENTS.md:35`). Основная ловушка проекта.
- Никаких тестов, линтеров, метрик, replay'а — единственная диагностика это `PQR_debug.log`.

Цель v0.1 — **доказать, что вся цепочка (attach → read player state → inject Lua через тот же 9-байтный детур → одна рабочая ротация из существующего XML) реализуема на Rust**, с сохранением байт-совместимости с профилями и `Offsets_12340.xml`. Без UI. Если MVP работает — дальше поверх ядра поднимаем TUI, потом GUI.

**Deployment для v0.1:** Windows-native exe, собранный через `cargo xwin`, запускается под Wine в одном wineserver с WoW. Это ровно модель работы текущего PQR — совместимость с offsets 3.3.5a гарантирована. Нативный Linux backend (через `process_vm_readv` + ptrace для аллокации в чужом процессе) — отдельная фича v0.2, не критерий MVP.

Не входит в v0.1: редакторы Abilities/Rotations, hotkeys, click-to-cast, новый формат профилей, GUI. Всё это отдельные milestones после того как ядро гоняет живого моба на training dummy.

## Целевая архитектура

```
pqr-rs/
├── crates/
│   ├── pqr-mem/       # process attach + read/write, кросс-платформ trait
│   ├── pqr-asm/       # x86 codegen для детура и Lua-payload'а (iced-x86)
│   ├── pqr-inject/    # Executor: детур, codeCavePtr, self-clear handshake
│   ├── pqr-wow/       # domain: offsets XML, GameState, PlayerName/Class, Lua_DoString wrapper
│   ├── pqr-profile/   # legacy XML parser/writer (Abilities + Rotations), двойное escape
│   ├── pqr-engine/    # инжектит clsLua-эквивалент, регистрирует ротации, тикает
│   └── pqr-cli/       # bin: attach → выбрать процесс → загрузить профиль → запустить
└── Cargo.toml         # workspace
```

Разделение по крейтам — чтобы `pqr-mem`, `pqr-asm`, `pqr-profile` можно было тестировать без живого WoW.

## Крейты по порядку реализации

### 1. `pqr-profile` (первый — чистая логика, никакого IO с процессами)

Ссылки на текущую реализацию: `reversed/pqr-app/PriorityQueueRotation/clsXML.cs:575-590` (двойное escape), `AGENTS.md:33-39` (формат).

- Парсит `DarhangeR_<CLASS>_Abilities.xml` и `_Rotations.xml`.
- Модель:
  ```rust
  struct Ability { name: String, spell_id: u32, actions: String, lua: String,
                   recast_delay: u32, target: TargetKind, cancel_channel: bool,
                   lua_before: Option<String>, lua_after: Option<String> }
  struct Rotation { name: String, default: bool, priority: Vec<String>,
                    require_combat: bool, notes: String }
  ```
- **Обязательно:** `RotationList` парсится в `Vec<String>` по `|`, при загрузке `resolve(&[Ability])` возвращает `Result<Vec<&Ability>, Vec<UnresolvedName>>` — никакого silent drop. Это главное улучшение UX над оригиналом.
- Двойное XMLDecode/XMLEncode: порт `clsXML.cs:575-590` один-в-один (иначе `&amp;quot;` в legacy профилях сломается).
- Writer сохраняет без trailing newline (см. `AGENTS.md:39`).
- **Тесты (первое место в проекте где вообще есть тесты):**
  - round-trip парс/сериализация всех файлов из `PQR_fixed/Profiles/*.xml` — байт-в-байт.
  - `xmllint --noout` на выходе.
  - fixture с mismatch в RotationList → возвращает `Err` с точным списком отсутствующих имён.

### 2. `pqr-mem` (memory access, кросс-платформ trait)

```rust
trait ProcessMemory {
    fn find_by_name(name: &str) -> Vec<ProcessHandle>;
    fn image_base(&self) -> u64;
    fn read_bytes(&self, addr: u64, len: usize) -> io::Result<Vec<u8>>;
    fn write_bytes(&self, addr: u64, data: &[u8]) -> io::Result<()>;
    fn alloc(&self, size: usize, exec: bool) -> io::Result<u64>;
    fn free(&self, addr: u64) -> io::Result<()>;
}
```

- **v0.1 — только Windows backend, запуск под Wine.** `windows-rs` крейт: `OpenProcess(PROCESS_ALL_ACCESS)`, `ReadProcessMemory`, `WriteProcessMemory`, `VirtualAllocEx`, `Process32First/Next`. Порт `reversed/blackmagic/Magic/BlackMagic.cs`, `SMemory.cs`, `SProcess.cs`. Собираем через `cargo xwin --target x86_64-pc-windows-msvc`, запускаем через `wine pqr.exe` в том же wineserver, что и WoW — это ровно та модель, что работает сегодня у оригинального PQR, значит offsets и детур совместимы гарантированно.
- **v0.2+ (отложено, вне scope MVP):** Linux-native backend через `process_vm_readv` / `process_vm_writev` для чтения Wine-процесса Wow напрямую + ptrace-инжектор для аллокации executable-памяти в чужом процессе (VirtualAllocEx-эквивалента в Linux нет, единственный путь — hijack потока и вызвать `mmap` syscall). Референс: `linux-inject`. Требует `ptrace_scope=0` или `cap_sys_ptrace`. Уберёт Wine-прослойку для самого pqr.
- **Тесты:** headless на Windows (можно и через Wine) — spawn'им фиктивный процесс, читаем/пишем/аллоцируем.

### 3. `pqr-asm`

- Обёртка над `iced-x86` (compile-time, чистый Rust, без FFI в отличие от keystone).
- API: `assemble(instructions: &[Instruction], base_addr: u64) -> Vec<u8>`.
- `randomize()`: порт `Executor.RandomizeASM` — вставка no-op между реальными инструкциями (список из `Executor.cs:23-29`). Нужен для сохранения anti-signature поведения оригинала.
- **Тесты:** ассемблируем известный детур из `Executor.cs:52-66`, сравниваем с ожидаемыми байтами.

### 4. `pqr-wow` (domain layer над memory)

- Загрузка `Offsets_12340.xml` (тот же формат, порт `clsOffsets.cs`).
- `WowClient::discover()` — реплика `clsMemory.WoWProcesses()` (`ARCHITECTURE.md §2`):
  1. Найти все процессы с именем `Wow`.
  2. Прочитать 5 UTF-8 байт по `base + WoWVersionOffset`, сравнить со всеми `<CurrentWoWVersion>` из загруженных offsets.
  3. Прочитать 30 байт по `base + PlayerName`, если пусто — пропустить (не в мире).
- Обёртки: `game_state()`, `player_name()`, `player_class()`, `is_typing()`.
- **Тесты:** мок `ProcessMemory` с известным layout'ом.

### 5. `pqr-inject` (Executor)

Порт `reversed/pqr-app/PriorityQueueRotation/Executor.cs` 1:1 (см. `ARCHITECTURE.md §4`):

- `apply()`: alloc `codeCavePtr` (4 байта, 0), alloc `DetourPtr` (598 байт), записать `<Overwritten>` префикс + стаб (`pushfd/pushad`/чтение `[codeCavePtr]`/`call eax`/self-clear/`popad/popfd/jmp back`), затем перезаписать первые 9 байт `<Detour>` на `jmp DetourPtr`.
- `inject_and_execute(asm)`: assemble payload → alloc → write → `codeCavePtr = payload_addr` → busy-wait `ReadInt(codeCavePtr) != 0` с 3-сек timeout → free.
- `restore()`: восстановить оригинальные 9 байт.
- **Тесты:** заасемблировать стаб, проверить что байты соответствуют декомпиляции текущей версии.

### 6. `pqr-engine`

- Встроенные Lua строки из `clsLua.cs` вынесены в `resources/lua/*.lua` (setup_tables, event_frame, execute_bot, helpers).
- `ReplacePQR`-эквивалент: генерация случайных имён идентификаторов для anti-detection (`ARCHITECTURE.md §6`).
- API: `engine.load_profile(class, profile_name)`, `engine.start()`, `engine.stop()`.
- **Не** порт `frmMain.AddAbilityToCurrent` — вместо него используется explicit `Rotation::resolve()` из `pqr-profile` который явно возвращает ошибки.
- Тик-луп: main thread приложения периодически (200мс) читает `game_state()`, если 1 — engine продолжает работать; если 0 — детур снимается.

### 7. `pqr-cli`

```
pqr attach                        # список процессов Wow с версиями
pqr run <class> <rotation_name>   # загрузить профиль, инжектнуть, тикать до Ctrl-C
pqr profile lint <path>           # xmllint + resolve RotationList
pqr profile diff <a> <b>          # то что viewer/ делает, но CLI
```

## Инструменты и зависимости

- **Rust edition 2024**, MSRV pinned.
- Крейты: `iced-x86` (assembler), `windows-rs` (Windows FFI), `nix` (Linux syscalls), `quick-xml` (XML), `serde`, `tokio` (для будущего TUI — в v0.1 не критично), `tracing` (structured logs — то чего нет в оригинале), `anyhow`/`thiserror`.
- Тесты: `cargo test` + `insta` для snapshot round-trip профилей.
- Cross-compile: `cargo xwin build --target x86_64-pc-windows-msvc` (Linux → Windows exe без VM).
- CI: GitHub Actions matrix (linux + windows), `cargo test`, `xmllint` на выходе `pqr-profile` round-trip.

## Совместимость (жёсткие инварианты)

- `PQR_fixed/Profiles/*.xml` читается и переписывается **байт-в-байт** (round-trip тест это гарантирует).
- `PQR_fixed/Offsets_12340.xml` используется как есть, тот же 9-байтный `<Overwritten>` префикс, тот же `<Detour>` RVA.
- Инжектированный Lua-фреймворк функционально эквивалентен `clsLua.cs` (те же имена таблиц/функций до `ReplacePQR`, тот же интервал `PQR_UpdateInterval` default 100мс).
- `PQR_TestMode` bypass (`AGENTS.md:67`) сохранён.

## Что явно НЕ делаем в v0.1

- GUI (ни egui, ни Tauri).
- Редакторы abilities / rotations / hotkeys.
- Новый формат профилей (TOML/Lua) — только после того как v0.1 работает на живой цели.
- Input simulation (click-to-cast через uinput/XTEST).
- Замена FASM-детура на альтернативные методы инжекта.
- Порт `frmSelect`/`frmMain` UI — их логика уже перенесена в `pqr-wow` и `pqr-cli`.

## Критические файлы для порта

| Оригинал | Куда в Rust | Что важно |
|---|---|---|
| `reversed/pqr-app/PriorityQueueRotation/Executor.cs` | `pqr-inject/src/lib.rs` | 1:1 порт, включая RandomizeASM |
| `reversed/pqr-app/PriorityQueueRotation/clsMemory.cs:WoWProcesses` | `pqr-wow/src/discover.rs` | Точная реплика 3-шагового discovery |
| `reversed/pqr-app/PriorityQueueRotation/clsXML.cs:575-590` | `pqr-profile/src/escape.rs` | Двойное escape, иначе legacy профили ломаются |
| `reversed/pqr-app/PriorityQueueRotation/clsLua.cs` | `pqr-engine/resources/lua/` | Вынести Lua-строки в файлы, не хардкодить в Rust |
| `reversed/pqr-app/PriorityQueueRotation/clsOffsets.cs` | `pqr-wow/src/offsets.rs` | Тот же XML формат |
| `reversed/blackmagic/Magic/BlackMagic.cs` + `SMemory.cs` | `pqr-mem/src/windows.rs` | Windows backend, порт RPM/WPM/VAE |
| `reversed/blackmagic/Magic/SThread.cs` | **не портируем** | На Linux нет `Process.Threads[0]` проблемы; на Windows пусть будет current thread |

## Верификация MVP

Порядок проверки (каждый шаг — gate для следующего):

1. **`pqr-profile`:** `cargo test` — round-trip всех файлов из `PQR_fixed/Profiles/` байт-в-байт, `resolve()` на реальных ротациях возвращает `Ok`.
2. **`pqr-mem` (Linux):** spawn'ить sleep-процесс, читать/писать/аллоцировать регионы. `cargo test`.
3. **`pqr-wow`:** запустить WoW 3.3.5a под Wine, залогиниться персонажем. `pqr attach` показывает процесс с версией `12340`, именем персонажа, GameState=1.
4. **`pqr-inject`:** применить детур на пустой Lua (`return 1`), убедиться что `codeCavePtr` очищается за <3с, snapshot байт стаба совпадает с `Executor.cs:52-66`.
5. **`pqr-engine`:** инжектнуть setup_tables Lua, из in-game чата ввести `/dump PQR` — должна появиться таблица. Загрузить одну простую ротацию (например Warrior `-- Functions --` + одна ability), встать на training dummy, `/run PQR_TestMode = true`, `pqr run WARRIOR Arms_PvE` — ability должна кастоваться.
6. **Кросс-компиляция:** `cargo xwin build --release --target x86_64-pc-windows-msvc` собирается без ошибок, полученный exe запускается на Windows (проверка на VM или у пользователя).
7. **Живой тест на dummy:** rotation крутится 5 минут без падений / desync с оригинальным PQR (тот же профиль на тех же условиях даёт ту же последовательность каста — через сравнение `tracing` логов).

## Дальнейшие milestones (после v0.1, вне scope этого плана)

- v0.2: TUI (ratatui) — process picker, live-лог, метрики частоты каста.
- v0.3: GUI (egui) — редакторы abilities/rotations, hot-reload профилей, дифф-виджет (порт `viewer/`).
- v0.4: Новый декларативный формат профилей (TOML) параллельно с legacy XML.
- v0.5: Structured replay — writer сохраняет каждый тик, replay-режим для отладки без игры.
