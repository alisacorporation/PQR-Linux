#!/usr/bin/env bash
# Run the pqr TUI under Wine with a stdout pipe.
#
# Two Wine bugs make the plain `wine pqr.exe tui ...` command unusable:
#
# 1. Corruption: Wine's conhost does not parse VT sequences — it stores ESC
#    sequences as literal cells and re-renders them through its own
#    \r / \x1b[?25h / \x1b[K writer, injecting CRs mid-sequence at row wraps.
#    Piping stdout bypasses conhost entirely (app writes to a pipe, not the
#    console), and crossterm still gets its real size/keys from CONOUT$/CONIN$.
# 2. Size: Wine only probes terminal size from fd 1/2 (ntdll env.c TIOCGWINSZ).
#    That is why stderr must stay on the tty — do NOT add `2>&1`, otherwise
#    conhost spawns with `--width 0 --height 0` and falls back to 80x150,
#    which puts the footer at row 150 of a 40-row screen.
set -euo pipefail
cd "$(dirname "$0")"

EXE="target/x86_64-pc-windows-msvc/release/pqr.exe"
if [[ ! -f "$EXE" ]]; then
    echo "error: $EXE not found" >&2
    echo "build first: export PATH=\"\$HOME/.cargo/bin:\$HOME/.dotnet:\$PATH\" && cargo xwin build --release --target x86_64-pc-windows-msvc" >&2
    exit 1
fi

# pqr's --profiles/--offsets-dir default to CWD ("Profiles" and "."), but the
# real files live in PQR_fixed/. Inject the paths unless the caller passed
# their own; without them a non---demo run dies with "load profile ...".
args=("$@")
have_profiles=0 have_offsets=0
for a in "${args[@]}"; do
    case "$a" in
        --profiles|--profiles=*)    have_profiles=1 ;;
        --offsets-dir|--offsets-dir=*) have_offsets=1 ;;
    esac
done
if (( ! have_profiles )); then
    args=(--profiles ../PQR_fixed/Profiles "${args[@]}")
fi
if (( ! have_offsets )); then
    args=(--offsets-dir ../PQR_fixed "${args[@]}")
fi

wine "$EXE" "${args[@]}" | cat
