//! Terminal UI for `pqr tui`.
//!
//! Layout (top to bottom):
//! ┌──────────────────────────────────────────────────────────────────────┐
//! │  ● RUNNING                              MM_PvP_BG_DarhangeR           │  BANNER (bold, big)
//! │  Sonarahunts · WoW 12340 · pid 280      elapsed 00:42  ·  ticks 210  │
//! ├────────────────────────────┬─────────────────────────────────────────┤
//! │  session stats             │  events                                 │  STATS + EVENTS
//! │  ticks/sec  4.9            │  00:00.02  attaching                    │
//! │  in world   YES            │  00:00.05  bootstrap ok                 │
//! │  uptime     00:42          │  00:00.09  rotation loaded (41 abs)     │
//! │  status     Running        │  00:00.10  running                      │
//! ├────────────────────────────┴─────────────────────────────────────────┤
//! │  debug log (press L to toggle)                                       │  optional
//! │  … dimmed low-level tracing …                                        │
//! └──────────────────────────────────────────────────────────────────────┘
//!   q/Esc quit   L toggle debug   R restart rotation (todo)
//!
//! Engine work runs on a background thread; the UI thread paints from
//! shared state at ~30fps and cooperatively stops on `q`.

use std::collections::VecDeque;
use std::io::{self, Write};
use std::path::Path;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::{Duration, Instant};

use anyhow::{anyhow, Context, Result};
use crossterm::event::{self, Event, KeyCode, KeyEventKind, KeyModifiers};
use crossterm::{execute, queue};
use crossterm::terminal::{
    disable_raw_mode, enable_raw_mode, BeginSynchronizedUpdate, EndSynchronizedUpdate,
    EnterAlternateScreen, LeaveAlternateScreen,
};
use pqr_engine::{Engine, IdentReplacer};
use pqr_profile::parse::load_profile;
use pqr_wow::{discover, Offsets};
use ratatui::backend::CrosstermBackend;
use ratatui::layout::{Alignment, Constraint, Direction, Layout, Rect};
use ratatui::style::{Color, Modifier, Style};
use ratatui::text::{Line, Span};
use ratatui::widgets::{Block, Borders, Clear, Paragraph, Wrap};
use ratatui::Terminal;
use tracing_subscriber::fmt::MakeWriter;

const DEBUG_LOG_CAPACITY: usize = 500;
const EVENT_LOG_CAPACITY: usize = 100;
const FRAME_INTERVAL: Duration = Duration::from_millis(33);

pub fn run(
    profiles: &Path,
    offsets_dir: &Path,
    prefix: &str,
    class: &str,
    rotation_name: &str,
    no_rename: bool,
    demo: bool,
    offsets_loader: fn(&Path) -> Result<Vec<Offsets>>,
) -> Result<()> {
    let debug_ring: Arc<Mutex<VecDeque<String>>> = Arc::new(Mutex::new(VecDeque::with_capacity(DEBUG_LOG_CAPACITY)));
    let state = Arc::new(Mutex::new(AppState::default()));
    let stop = Arc::new(AtomicBool::new(false));

    // Route tracing to the debug ring only; the "events" panel is fed by the
    // engine thread through push_event() so the two panes stay independent.
    let _ = tracing_subscriber::fmt()
        .with_writer(RingMakeWriter { ring: debug_ring.clone() })
        .with_ansi(false)
        .with_target(false)
        .with_env_filter(
            tracing_subscriber::EnvFilter::try_from_default_env()
                // Default: quiet — INFO from all, no per-payload DEBUG spam.
                .unwrap_or_else(|_| "info".into()),
        )
        .try_init();

    let engine_handle = if demo {
        // Synthetic state — no offsets, no Wow process, no profile files.
        {
            let mut s = state.lock().unwrap();
            s.player = "DemoPlayer".into();
            s.build = "3.3.5a".into();
            s.pid = 4242;
            s.rotation = rotation_name.to_string();
            s.abilities_count = 41;
            s.priority_count = 12;
            s.rotations_count = 3;
            s.status = Status::Attaching;
            s.session_start = Instant::now();
            s.push_event(EventKind::Info, "demo mode: synthetic state (no WoW attach)".into());
        }
        let stop_engine = stop.clone();
        let state_for_engine = state.clone();
        let rotation_owned = rotation_name.to_string();
        thread::spawn(move || -> Result<()> {
            demo_engine(state_for_engine, stop_engine, &rotation_owned);
            Ok(())
        })
    } else {
        let candidates = offsets_loader(offsets_dir)?;
        let mut found = discover(&candidates)?;
        let discovery = found.pop().ok_or_else(|| anyhow!(
            "no matching Wow process (must be logged into the world)"))?;
        let profile = load_profile(profiles, prefix, class)
            .map_err(|e| anyhow!("load profile {prefix}_{class}: {e}"))?;

        let priority_count = profile.rotations.iter()
            .find(|r| r.name == rotation_name)
            .map(|r| r.priority.len())
            .unwrap_or(0);
        {
            let mut s = state.lock().unwrap();
            s.player = discovery.player_name.clone();
            s.build = discovery.version_string.clone();
            s.pid = discovery.process.pid;
            s.rotation = rotation_name.to_string();
            s.abilities_count = profile.abilities.len();
            s.priority_count = priority_count;
            s.rotations_count = profile.rotations.len();
            s.status = Status::Attaching;
            s.session_start = Instant::now();
            s.push_event(EventKind::Info, format!("discovered pid={} player={}", discovery.process.pid, discovery.player_name));
            s.push_event(EventKind::Info, format!("profile loaded: {} abilities, {} rotations",
                                                  profile.abilities.len(), profile.rotations.len()));
        }

        let stop_engine = stop.clone();
        let state_for_engine = state.clone();
        let profile_for_engine = profile.clone();
        let rotation_owned = rotation_name.to_string();
        thread::spawn(move || -> Result<()> {
            let client = discovery.into_client()?;
            let ident = if no_rename { IdentReplacer::identity() } else { IdentReplacer::random() };
            let mut engine = Engine::attach_with(client, ident)?;
            {
                let mut s = state_for_engine.lock().unwrap();
                s.status = Status::Bootstrapping;
                s.push_event(EventKind::Ok, "detour applied".into());
            }
            engine.bootstrap()?;
            {
                let mut s = state_for_engine.lock().unwrap();
                s.status = Status::LoadingRotation;
                s.push_event(EventKind::Ok, "bootstrap complete".into());
            }
            engine.load_rotation(&profile_for_engine, &rotation_owned)?;
            {
                let mut s = state_for_engine.lock().unwrap();
                s.status = Status::Running;
                s.running_start = Some(Instant::now());
                s.push_event(EventKind::Ok, format!("rotation loaded: {}", rotation_owned));
            }

            let mut last_tick = Instant::now();
            while !stop_engine.load(Ordering::SeqCst) {
                match engine.tick() {
                    Ok(alive) => {
                        let mut s = state_for_engine.lock().unwrap();
                        s.ticks = s.ticks.wrapping_add(1);
                        s.in_world = alive;
                        let now = Instant::now();
                        let dt = now.duration_since(last_tick).as_secs_f32().max(1e-3);
                        // EWMA on tick rate
                        let inst = 1.0 / dt;
                        s.tick_rate = if s.tick_rate == 0.0 { inst } else { s.tick_rate * 0.85 + inst * 0.15 };
                        last_tick = now;
                    }
                    Err(e) => {
                        state_for_engine.lock().unwrap().push_event(EventKind::Warn, format!("tick: {e}"));
                    }
                }
                thread::sleep(pqr_engine::engine::DEFAULT_TICK_INTERVAL);
            }

            {
                let mut s = state_for_engine.lock().unwrap();
                s.status = Status::ShuttingDown;
                s.push_event(EventKind::Info, "shutting down".into());
            }
            engine.shutdown()?;
            {
                let mut s = state_for_engine.lock().unwrap();
                s.status = Status::Done;
                s.push_event(EventKind::Ok, "detour restored".into());
            }
            Ok(())
        })
    };

    // Restore the terminal even if we panic inside the UI loop, otherwise
    // the shell is left in raw mode with the alternate screen active.
    let previous_hook = std::panic::take_hook();
    std::panic::set_hook(Box::new(move |info| {
        let _ = disable_raw_mode();
        let _ = execute!(io::stdout(), LeaveAlternateScreen);
        previous_hook(info);
    }));

    enable_raw_mode().context("enable raw mode")?;
    let mut stdout = io::stdout();
    execute!(stdout, EnterAlternateScreen).context("enter alt screen")?;
    let backend = CrosstermBackend::new(stdout);
    let mut terminal = Terminal::new(backend).context("create terminal")?;

    let ui_result = ui_loop(&mut terminal, &debug_ring, &state, &stop);

    stop.store(true, Ordering::SeqCst);
    let join_result = engine_handle.join();
    disable_raw_mode().ok();
    execute!(terminal.backend_mut(), LeaveAlternateScreen).ok();
    terminal.show_cursor().ok();

    ui_result?;
    match join_result {
        Ok(Ok(())) => Ok(()),
        Ok(Err(e)) => Err(e).context("engine thread failed"),
        Err(_) => Err(anyhow!("engine thread panicked")),
    }
}

fn ui_loop(
    terminal: &mut Terminal<CrosstermBackend<io::Stdout>>,
    debug_ring: &Arc<Mutex<VecDeque<String>>>,
    state: &Arc<Mutex<AppState>>,
    stop: &Arc<AtomicBool>,
) -> Result<()> {
    // Full initial clear. Afterwards ratatui's per-cell diff keeps repaints
    // minimal — blanking + repainting the whole screen every frame (the old
    // behavior) is visible as flicker/tearing under Wine's console.
    terminal.clear()?;
    let mut last_frame = Instant::now();
    let mut show_debug = false;
    loop {
        let now = Instant::now();
        let remaining = FRAME_INTERVAL.saturating_sub(now.duration_since(last_frame));
        if event::poll(remaining)? {
            match event::read()? {
                // Windows consoles emit a Release event after every Press;
                // without this filter each keystroke is handled twice and
                // toggles (e.g. the debug pane) cancel themselves out.
                Event::Key(k) if k.kind != KeyEventKind::Release => match k.code {
                    KeyCode::Char('q') | KeyCode::Esc => { stop.store(true, Ordering::SeqCst); break; }
                    KeyCode::Char('c') if k.modifiers.contains(KeyModifiers::CONTROL) => {
                        stop.store(true, Ordering::SeqCst); break;
                    }
                    KeyCode::Char('l') | KeyCode::Char('L') => { show_debug = !show_debug; }
                    _ => {}
                },
                // Resizes are handled by ratatui's autoresize on the next draw.
                _ => {}
            }
        }

        let snapshot = state.lock().unwrap().clone();
        let debug_lines: Vec<String> = if show_debug {
            let g = debug_ring.lock().unwrap();
            g.iter().rev().take(200).rev().cloned().collect()
        } else { Vec::new() };
        // Synchronized update: the terminal applies the frame atomically on
        // End, so a screenshot can never catch a half-painted frame.
        queue!(terminal.backend_mut(), BeginSynchronizedUpdate)?;
        terminal.draw(|f| draw(f, &snapshot, &debug_lines, show_debug))?;
        queue!(terminal.backend_mut(), EndSynchronizedUpdate)?;
        terminal.backend_mut().flush()?;
        last_frame = Instant::now();

        if matches!(snapshot.status, Status::Done) { break; }
    }
    Ok(())
}

/// Synthetic "engine" for `--demo`: walks the real status sequence, ticks a
/// fake counter, and pushes events so every pane updates like a live session.
fn demo_engine(state: Arc<Mutex<AppState>>, stop: Arc<AtomicBool>, rotation: &str) {
    const STAGES: [(Status, &str, EventKind); 4] = [
        (Status::Attaching,       "discovered pid=4242 player=DemoPlayer", EventKind::Info),
        (Status::Bootstrapping,   "detour applied",                       EventKind::Ok),
        (Status::LoadingRotation, "bootstrap complete",                    EventKind::Ok),
        (Status::Running,         "rotation loaded",                       EventKind::Ok),
    ];
    let mut stage = 0usize;
    let mut next_event = Instant::now() + Duration::from_millis(700);
    let mut next_trace = Instant::now() + Duration::from_millis(1000);
    let mut last_tick = Instant::now();
    let mut event_no = 0u32;
    let rotation = rotation.to_string();

    while !stop.load(Ordering::SeqCst) {
        thread::sleep(Duration::from_millis(50));
        let now = Instant::now();
        let mut s = state.lock().unwrap();

        if stage < STAGES.len() && now >= next_event {
            let (status, text, kind) = STAGES[stage];
            s.status = status;
            if matches!(status, Status::Running) {
                s.running_start = Some(now);
            }
            s.push_event(kind, format!("{text}: {rotation}"));
            stage += 1;
            next_event = now + Duration::from_millis(900);
        }

        if matches!(s.status, Status::Running | Status::ShuttingDown) {
            s.ticks = s.ticks.wrapping_add(1);
            s.in_world = true;
            let dt = now.duration_since(last_tick).as_secs_f32().max(1e-3);
            let inst = 1.0 / dt;
            s.tick_rate = if s.tick_rate == 0.0 { inst } else { s.tick_rate * 0.85 + inst * 0.15 };
            last_tick = now;
        }

        if now >= next_trace {
            event_no += 1;
            tracing::info!(event_no, "demo trace line for the debug pane");
            if event_no % 7 == 0 {
                s.push_event(EventKind::Warn, format!("simulated warning #{event_no}"));
            }
            next_trace = now + Duration::from_millis(600);
        }
    }

    let mut s = state.lock().unwrap();
    s.status = Status::Done;
    s.push_event(EventKind::Info, "demo stopped".into());
}

fn draw(f: &mut ratatui::Frame, s: &AppState, debug_lines: &[String], show_debug: bool) {
    // Blank every cell before painting — avoids ghosting when status labels or
    // stat values shorten between frames, and when the layout has a variable
    // debug pane.
    f.render_widget(Clear, f.area());

    let root = Layout::default()
        .direction(Direction::Vertical)
        .constraints([
            Constraint::Length(5),           // banner
            Constraint::Length(11),          // stats + events row
            Constraint::Min(0),              // debug pane (fills what's left, 0 when hidden)
            Constraint::Length(1),           // footer
        ])
        .split(f.area());

    draw_banner(f, root[0], s);
    draw_body(f, root[1], s);
    if show_debug {
        draw_debug(f, root[2], debug_lines);
    }
    draw_footer(f, root[3], show_debug);
}

fn draw_banner(f: &mut ratatui::Frame, area: Rect, s: &AppState) {
    let (dot, dot_color, label) = match s.status {
        Status::Idle           => ("*", Color::DarkGray, "IDLE"),
        Status::Attaching      => ("*", Color::Yellow,   "ATTACHING"),
        Status::Bootstrapping  => ("*", Color::Yellow,   "BOOTSTRAP"),
        Status::LoadingRotation=> ("*", Color::Yellow,   "LOADING ROTATION"),
        Status::Running        => ("*", Color::Green,    "RUNNING"),
        Status::ShuttingDown   => ("*", Color::Yellow,   "SHUTTING DOWN"),
        Status::Done           => ("*", Color::Gray,     "DONE"),
    };
    let elapsed = fmt_elapsed(s.running_start.map(|t| t.elapsed()).unwrap_or_default());

    // Fixed-width status label so subsequent Spans on the same line always
    // land at the same column — this removes trailing garbage when the label
    // shortens (BOOTSTRAP -> RUNNING).
    let label_fixed = format!("{label:<18}");
    let rotation_fixed = format!("{:<40}", s.rotation);

    let title_line = Line::from(vec![
        Span::raw(" "),
        Span::styled(dot, Style::default().fg(dot_color)),
        Span::raw("  "),
        Span::styled(label_fixed, Style::default().fg(dot_color).add_modifier(Modifier::BOLD)),
        Span::styled(rotation_fixed, Style::default().fg(Color::Magenta).add_modifier(Modifier::BOLD)),
    ]);

    let dim = Style::default().add_modifier(Modifier::DIM);
    let info_line = Line::from(vec![
        Span::raw("  "),
        Span::styled(format!("{:<20}", s.player), Style::default().fg(Color::Yellow)),
        Span::styled("WoW ",  dim),
        Span::raw(format!("{:<8}", s.build)),
        Span::styled("pid ",  dim),
        Span::raw(format!("{:<10}", s.pid)),
        Span::styled("elapsed ", dim),
        Span::styled(format!("{elapsed:<8}"), Style::default().fg(Color::Cyan).add_modifier(Modifier::BOLD)),
        Span::styled("ticks ",   dim),
        Span::styled(format!("{:<10}", s.ticks), Style::default().fg(Color::Cyan)),
    ]);

    let p = Paragraph::new(vec![
        Line::from(""),
        title_line,
        info_line,
        Line::from(""),
    ])
    .block(Block::default().borders(Borders::BOTTOM).border_style(Style::default().fg(dot_color)));
    f.render_widget(p, area);
}

fn draw_body(f: &mut ratatui::Frame, area: Rect, s: &AppState) {
    let cols = Layout::default()
        .direction(Direction::Horizontal)
        .constraints([Constraint::Length(34), Constraint::Min(20)])
        .split(area);
    draw_stats(f, cols[0], s);
    draw_events(f, cols[1], s);
}

fn draw_stats(f: &mut ratatui::Frame, area: Rect, s: &AppState) {
    let world_txt = if s.in_world { "IN WORLD" } else { "OUT" };
    let world_color = if s.in_world { Color::Green } else { Color::Red };
    let rows = vec![
        stat_row("ticks/sec",  format!("{:>8.1}", s.tick_rate), Color::Cyan),
        stat_row("uptime",     format!("{:>8}", fmt_elapsed(s.running_start.map(|t| t.elapsed()).unwrap_or_default())), Color::Cyan),
        stat_row("session",    format!("{:>8}", fmt_elapsed(s.session_start.elapsed())), Color::Gray),
        Line::from(vec![
            Span::styled("  world      ", Style::default().add_modifier(Modifier::DIM)),
            Span::styled(format!("{world_txt:<10}"), Style::default().fg(world_color).add_modifier(Modifier::BOLD)),
        ]),
        stat_row("priority",   format!("{:>8}", s.priority_count),  Color::Cyan),
        stat_row("abilities",  format!("{:>8}", s.abilities_count), Color::Gray),
        stat_row("rotations",  format!("{:>8}", s.rotations_count), Color::Gray),
    ];
    let p = Paragraph::new(rows)
        .block(Block::default().borders(Borders::ALL).title(" session ").title_alignment(Alignment::Left));
    f.render_widget(p, area);
}

fn stat_row(label: &'static str, value: String, val_color: Color) -> Line<'static> {
    Line::from(vec![
        Span::styled(format!("  {label:<10} "), Style::default().add_modifier(Modifier::DIM)),
        Span::styled(value, Style::default().fg(val_color).add_modifier(Modifier::BOLD)),
    ])
}

fn draw_events(f: &mut ratatui::Frame, area: Rect, s: &AppState) {
    let inner_h = area.height.saturating_sub(2) as usize;
    let start = s.events.len().saturating_sub(inner_h);
    let lines: Vec<Line> = s.events.iter().skip(start).map(|e| {
        let (glyph, glyph_color) = match e.kind {
            EventKind::Ok   => ("+", Color::Green),
            EventKind::Info => ("-", Color::Cyan),
            EventKind::Warn => ("!", Color::Yellow),
        };
        let stamp = fmt_short(e.at.duration_since(s.session_start));
        Line::from(vec![
            Span::styled(format!(" {stamp:>8}  "), Style::default().add_modifier(Modifier::DIM)),
            Span::styled(format!("{glyph} "), Style::default().fg(glyph_color).add_modifier(Modifier::BOLD)),
            Span::raw(e.text.clone()),
        ])
    }).collect();
    let block = Block::default().borders(Borders::ALL).title(" events ").title_alignment(Alignment::Left);
    let p = Paragraph::new(lines).block(block).wrap(Wrap { trim: false });
    f.render_widget(p, area);
}

fn draw_debug(f: &mut ratatui::Frame, area: Rect, log_lines: &[String]) {
    let inner_h = area.height.saturating_sub(2) as usize;
    let start = log_lines.len().saturating_sub(inner_h);
    let lines: Vec<Line> = log_lines[start..].iter().map(|l| {
        let color = if l.contains("WARN") { Color::Yellow }
                    else if l.contains("ERROR") { Color::Red }
                    else { Color::DarkGray };
        Line::from(Span::styled(l.clone(), Style::default().fg(color)))
    }).collect();
    let block = Block::default().borders(Borders::ALL).title(" debug log ").title_alignment(Alignment::Left);
    let p = Paragraph::new(lines).block(block);
    f.render_widget(p, area);
}

fn draw_footer(f: &mut ratatui::Frame, area: Rect, show_debug: bool) {
    let debug_label = if show_debug { "hide debug log" } else { "show debug log" };
    let p = Paragraph::new(Line::from(vec![
        Span::raw("  "),
        key("q/Esc"), Span::styled(" quit  ", Style::default().add_modifier(Modifier::DIM)),
        key("L"),     Span::styled(format!(" {debug_label}  "), Style::default().add_modifier(Modifier::DIM)),
        key("Ctrl-C"), Span::styled(" force quit", Style::default().add_modifier(Modifier::DIM)),
    ]));
    f.render_widget(p, area);
}

fn key(s: &'static str) -> Span<'static> {
    Span::styled(s, Style::default().fg(Color::Cyan).add_modifier(Modifier::BOLD))
}

fn fmt_elapsed(d: Duration) -> String {
    let total = d.as_secs();
    format!("{:02}:{:02}", total / 60, total % 60)
}

fn fmt_short(d: Duration) -> String {
    let s = d.as_secs_f32();
    if s < 60.0 { format!("{s:>6.2}s") } else {
        let m = (s / 60.0) as u64;
        let r = s - (m as f32) * 60.0;
        format!("{m}m{r:04.1}s")
    }
}

// -------- shared state --------

#[derive(Debug, Clone)]
struct AppState {
    pid: u32,
    player: String,
    build: String,
    rotation: String,
    abilities_count: usize,     // total abilities defined in the profile file
    priority_count: usize,      // abilities in the *active* rotation's priority list
    rotations_count: usize,
    status: Status,
    session_start: Instant,
    running_start: Option<Instant>,
    ticks: u64,
    tick_rate: f32,
    in_world: bool,
    events: VecDeque<AppEvent>,
}

impl Default for AppState {
    fn default() -> Self {
        Self {
            pid: 0, player: "-".into(), build: "-".into(), rotation: "-".into(),
            abilities_count: 0, priority_count: 0, rotations_count: 0,
            status: Status::Idle,
            session_start: Instant::now(),
            running_start: None,
            ticks: 0, tick_rate: 0.0, in_world: false,
            events: VecDeque::with_capacity(EVENT_LOG_CAPACITY),
        }
    }
}

impl AppState {
    fn push_event(&mut self, kind: EventKind, text: String) {
        if self.events.len() >= EVENT_LOG_CAPACITY { self.events.pop_front(); }
        self.events.push_back(AppEvent { kind, text, at: Instant::now() });
    }
}

#[derive(Debug, Clone)]
struct AppEvent { kind: EventKind, text: String, at: Instant }

#[derive(Debug, Clone, Copy)]
enum EventKind { Ok, Info, Warn }

#[derive(Debug, Clone, Copy)]
enum Status { Idle, Attaching, Bootstrapping, LoadingRotation, Running, ShuttingDown, Done }

// -------- tracing capture (debug pane only) --------

#[derive(Clone)]
struct RingMakeWriter { ring: Arc<Mutex<VecDeque<String>>> }

impl<'a> MakeWriter<'a> for RingMakeWriter {
    type Writer = RingWriter;
    fn make_writer(&'a self) -> RingWriter { RingWriter { ring: self.ring.clone(), buf: Vec::new() } }
}

struct RingWriter { ring: Arc<Mutex<VecDeque<String>>>, buf: Vec<u8> }

impl io::Write for RingWriter {
    fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
        self.buf.extend_from_slice(buf);
        while let Some(nl) = self.buf.iter().position(|&b| b == b'\n') {
            let line: Vec<u8> = self.buf.drain(..=nl).collect();
            let text = String::from_utf8_lossy(&line[..line.len().saturating_sub(1)]).into_owned();
            let mut g = self.ring.lock().unwrap();
            if g.len() >= DEBUG_LOG_CAPACITY { g.pop_front(); }
            g.push_back(text);
        }
        Ok(buf.len())
    }
    fn flush(&mut self) -> io::Result<()> { Ok(()) }
}
