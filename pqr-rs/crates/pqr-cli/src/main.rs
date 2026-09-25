use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;

use anyhow::{anyhow, Context, Result};
use clap::{Parser, Subcommand};
use pqr_engine::Engine;
use pqr_profile::parse::load_profile;
use pqr_wow::{discover, offsets, Offsets};
use tracing::{info, warn};

mod tui;

/// PQR-rs — Rust rewrite of Priority Queue Rotation.
#[derive(Parser)]
#[command(version, about, long_about = None)]
struct Cli {
    /// Path to the Profiles directory (holds `<prefix>_<CLASS>_{Abilities,Rotations}.xml`).
    #[arg(long, global = true, default_value = "Profiles")]
    profiles: PathBuf,

    /// Path (or glob-parent dir) of Offsets_*.xml files.
    #[arg(long, global = true, default_value = ".")]
    offsets_dir: PathBuf,

    #[command(subcommand)]
    cmd: Cmd,
}

#[derive(Subcommand)]
enum Cmd {
    /// List Wow processes matching a loaded offsets file, showing which
    /// player+build was detected.
    Attach,

    /// Attach, inject the PQR framework, load a rotation, run until Ctrl-C.
    Run {
        /// Profile filename prefix (e.g. `DarhangeR` for `DarhangeR_WARRIOR_*.xml`).
        #[arg(long, default_value = "DarhangeR")]
        prefix: String,
        /// Class name in caps (e.g. `WARRIOR`).
        class: String,
        /// Rotation name (matches `<RotationName>` in the rotations XML).
        rotation: String,
        /// Debug: keep the original `PQR`/`UnitBuffID`/`UnitDebuffID` names
        /// instead of randomizing them, so later `probe` calls can inspect the
        /// framework by known name.
        #[arg(long)]
        no_rename: bool,
    },

    /// Static profile lint: parse abilities+rotations, verify every
    /// `RotationList` entry resolves to a real ability.
    Lint {
        prefix: String,
        class: String,
    },

    /// Diff two rotations from the same profile — reports abilities present
    /// in one and not the other.
    Diff {
        prefix: String,
        class: String,
        left: String,
        right: String,
    },

    /// Attach, apply detour, run one Lua chunk (raw, no identifier rename),
    /// restore detour. Used to smoke-test the injection pipeline in isolation
    /// from the PQR framework.
    Probe {
        /// Lua code to run inside WoW. Example: `DEFAULT_CHAT_FRAME:AddMessage("hi")`.
        lua: String,
    },

    /// Terminal UI: live-updating view of engine state, log tail, and stop
    /// gracefully on `q`.
    Tui {
        #[arg(long, default_value = "DarhangeR")]
        prefix: String,
        class: String,
        rotation: String,
        #[arg(long)]
        no_rename: bool,
        /// Render with synthetic state (no WoW attach) — for debugging the UI.
        #[arg(long)]
        demo: bool,
    },
}

fn main() -> Result<()> {
    let cli = Cli::parse();

    // TUI installs its own subscriber that captures into a ring buffer; the
    // stderr writer here would paint over the alternate screen.
    if !matches!(cli.cmd, Cmd::Tui { .. }) {
        tracing_subscriber::fmt()
            .with_writer(std::io::stderr)
            .with_env_filter(
                tracing_subscriber::EnvFilter::try_from_default_env()
                    // Binary crate is `pqr` (from [[bin]] name), not `pqr_cli` (package name).
                    .unwrap_or_else(|_| "info,pqr=debug,pqr_engine=debug,pqr_inject=debug".into()),
            )
            .init();
    }

    let profiles = cli.profiles.clone();
    let offsets_dir = cli.offsets_dir.clone();
    match &cli.cmd {
        Cmd::Attach => cmd_attach(&offsets_dir),
        Cmd::Run { prefix, class, rotation, no_rename } => cmd_run(&profiles, &offsets_dir, prefix, class, rotation, *no_rename),
        Cmd::Lint { prefix, class } => cmd_lint(&profiles, prefix, class),
        Cmd::Diff { prefix, class, left, right } => cmd_diff(&profiles, prefix, class, left, right),
        Cmd::Probe { lua } => cmd_probe(&offsets_dir, lua),
        Cmd::Tui { prefix, class, rotation, no_rename, demo } =>
            tui::run(&profiles, &offsets_dir, prefix, class, rotation, *no_rename, *demo, load_all_offsets),
    }
}

fn cmd_probe(offsets_dir: &std::path::Path, lua: &str) -> Result<()> {
    let candidates = load_all_offsets(offsets_dir)?;
    let mut found = pqr_wow::discover(&candidates)?;
    let discovery = found.pop().ok_or_else(|| anyhow!("no matching Wow process"))?;
    let client = discovery.into_client()?;
    let mem = client.mem.clone();
    let detour_va = mem.image_base().wrapping_add(client.offsets.detour);
    let mut executor = pqr_inject::Executor::new(mem, detour_va, client.offsets.overwritten.clone())?;
    executor.apply()?;
    info!("detour applied — sending probe");
    let res = pqr_engine::lua_call::do_string(&mut executor, &client.offsets, lua);
    executor.restore()?;
    info!("detour restored");
    res.map_err(|e| anyhow!("probe failed: {e}"))?;
    Ok(())
}

fn load_all_offsets(dir: &std::path::Path) -> Result<Vec<Offsets>> {
    let mut out = Vec::new();
    // Support both "give me a single file" and "give me a directory" for --offsets-dir.
    let iter: Box<dyn Iterator<Item = PathBuf>> = if dir.is_file() {
        Box::new(std::iter::once(dir.to_path_buf()))
    } else {
        Box::new(std::fs::read_dir(dir)?
            .filter_map(Result::ok)
            .map(|e| e.path())
            .filter(|p| p.file_name().and_then(|n| n.to_str())
                .is_some_and(|n| n.starts_with("Offsets_") && n.ends_with(".xml"))))
    };
    for path in iter {
        let bytes = std::fs::read(&path)
            .with_context(|| format!("read {}", path.display()))?;
        match offsets::parse(&bytes) {
            Ok(o) => {
                info!(file = %path.display(), version = o.version, "loaded offsets");
                out.push(o);
            }
            Err(e) => {
                // Post-3.3.5a offsets (Cata+) lack <Detour>/<Overwritten>; the
                // v0.1 injector can't use them anyway.
                warn!(file = %path.display(), error = %e, "skipping incompatible offsets file");
            }
        }
    }
    if out.is_empty() {
        return Err(anyhow!("no Offsets_*.xml files found in {}", dir.display()));
    }
    Ok(out)
}

fn cmd_attach(offsets_dir: &std::path::Path) -> Result<()> {
    let candidates = load_all_offsets(offsets_dir)?;
    let found = discover(&candidates)?;
    if found.is_empty() {
        println!("no matching Wow process (must be logged into the world)");
        return Ok(());
    }
    for d in &found {
        println!(
            "pid={:>6}  build={}  player={}  detour_rva=0x{:x}",
            d.process.pid, d.version_string, d.player_name, d.matched_offsets.detour,
        );
    }
    Ok(())
}

fn cmd_run(profiles: &std::path::Path, offsets_dir: &std::path::Path, prefix: &str, class: &str, rotation_name: &str, no_rename: bool) -> Result<()> {
    let candidates = load_all_offsets(offsets_dir)?;
    let mut found = discover(&candidates)?;
    let discovery = found.pop().ok_or_else(|| anyhow!(
        "no matching Wow process (must be logged into the world)"))?;

    let profile = load_profile(profiles, prefix, class)
        .map_err(|e| anyhow!("load profile {prefix}_{class}: {e}"))?;
    info!(abilities = profile.abilities.len(), rotations = profile.rotations.len(), "profile loaded");

    let client = discovery.into_client()?;
    let ident = if no_rename {
        info!("using identity ident replacer (no rename)");
        pqr_engine::IdentReplacer::identity()
    } else {
        pqr_engine::IdentReplacer::random()
    };
    let mut engine = Engine::attach_with(client, ident)?;
    info!("bootstrap...");
    engine.bootstrap()?;
    info!(rotation = rotation_name, "loading rotation");
    engine.load_rotation(&profile, rotation_name)?;

    let stopping = Arc::new(AtomicBool::new(false));
    let s2 = stopping.clone();
    ctrlc::set_handler(move || s2.store(true, Ordering::SeqCst))
        .context("install Ctrl-C handler")?;

    info!("running (Ctrl-C to stop)");
    while !stopping.load(Ordering::SeqCst) {
        match engine.tick() {
            Ok(true) => {}
            Ok(false) => { warn!("no longer in world; stopping"); break; }
            Err(e) => { warn!(error = %e, "tick failed"); }
        }
        std::thread::sleep(pqr_engine::engine::DEFAULT_TICK_INTERVAL);
    }
    info!("shutting down");
    engine.shutdown()?;
    Ok(())
}

fn cmd_lint(profiles: &std::path::Path, prefix: &str, class: &str) -> Result<()> {
    let profile = load_profile(profiles, prefix, class)
        .map_err(|e| anyhow!("load profile: {e}"))?;
    let mut had_error = false;
    for r in &profile.rotations {
        match r.resolve(&profile.abilities) {
            Ok(_) => println!("{}: OK ({} abilities)", r.name, r.priority.len()),
            Err(missing) => {
                had_error = true;
                println!("{}: {} missing", r.name, missing.len());
                for m in missing { println!("    - {:?}", m.missing); }
            }
        }
    }
    if had_error { std::process::exit(1); }
    Ok(())
}

fn cmd_diff(profiles: &std::path::Path, prefix: &str, class: &str, left: &str, right: &str) -> Result<()> {
    let profile = load_profile(profiles, prefix, class)
        .map_err(|e| anyhow!("load profile: {e}"))?;
    let find = |name: &str| profile.rotations.iter().find(|r| r.name == name)
        .ok_or_else(|| anyhow!("rotation {name} not found"));
    let a = find(left)?;
    let b = find(right)?;
    let sa: std::collections::HashSet<_> = a.priority.iter().collect();
    let sb: std::collections::HashSet<_> = b.priority.iter().collect();
    println!("only in {left}:");
    for x in sa.difference(&sb) { println!("  - {x}"); }
    println!("only in {right}:");
    for x in sb.difference(&sa) { println!("  - {x}"); }
    Ok(())
}

