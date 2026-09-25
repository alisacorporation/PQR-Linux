//! PQR profile GUI — edit `DarhangeR_<CLASS>_{Abilities,Rotations}.xml`
//! profiles with live lint, rotation diff and disk hot-reload.

mod abilities;
mod diff_tab;
mod rotations;
mod state;

use std::path::PathBuf;
use std::time::Duration;

use clap::Parser;
use eframe::egui;

use state::{Gui as Data, Tab};

#[derive(Parser, Debug)]
#[command(name = "pqr-gui", about = "PQR profile editor (egui)")]
struct Args {
    /// Profiles directory (default: first existing of ./Profiles,
    /// <exe_dir>/Profiles, <repo>/PQR_fixed/Profiles).
    #[arg(long)]
    profiles: Option<PathBuf>,
    /// Open the given tab on startup: abilities, rotations or diff.
    #[arg(long, default_value = "abilities")]
    tab: String,
    /// Profile to select on startup (label, e.g. DarhangeR_DEATHKNIGHT).
    #[arg(long)]
    profile: Option<String>,
}

fn resolve_profiles_dir(arg: Option<PathBuf>) -> PathBuf {
    if let Some(p) = arg {
        return p;
    }
    let mut candidates: Vec<PathBuf> = vec![PathBuf::from("Profiles")];
    if let Ok(exe) = std::env::current_exe() {
        if let Some(dir) = exe.parent() {
            candidates.push(dir.join("Profiles"));
            // pqr-rs/target/{debug,release} -> repo root -> PQR_fixed/Profiles
            candidates.push(dir.join("../../../PQR_fixed/Profiles"));
        }
    }
    candidates
        .into_iter()
        .find(|c| c.is_dir())
        .unwrap_or_else(|| PathBuf::from("Profiles"))
}

fn main() -> eframe::Result {
    let args = Args::parse();
    let dir = resolve_profiles_dir(args.profiles);
    let mut data = Data::new(dir);
    data.tab = match args.tab.to_ascii_lowercase().as_str() {
        "rotations" => Tab::Rotations,
        "diff" => Tab::Diff,
        _ => Tab::Abilities,
    };
    eprintln!("startup: tab={} profile={:?}", data.tab.label(), args.profile);
    if let Some(label) = &args.profile {
        if let Some(i) = data.available.iter().position(|p| p.label() == *label) {
            data.select_profile(i);
        }
    }

    let options = eframe::NativeOptions {
        viewport: egui::ViewportBuilder::default()
            .with_title("PQR - profiles")
            // Right monitor (HDMI-A-2 at +2560,+0): never covers the game.
            .with_position([3020.0, 190.0])
            .with_inner_size([1180.0, 800.0]),
        ..Default::default()
    };
    eframe::run_native(
        "pqr-gui",
        options,
        Box::new(move |_cc| Ok(Box::new(App { data }))),
    )
}

struct App {
    data: Data,
}

impl eframe::App for App {
    fn logic(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        let now = ctx.input(|i| i.time);
        self.data.check_hot_reload(now);
        // The window may sit idle — poll the files even so.
        ctx.request_repaint_after(Duration::from_millis(400));
    }

    fn ui(&mut self, ui: &mut egui::Ui, _frame: &mut eframe::Frame) {
        let g = &mut self.data;

        egui::Panel::top("header").show(ui, |ui| header(g, ui));
        if g.conflict.iter().any(|c| *c) {
            egui::Panel::top("conflict").show(ui, |ui| conflict_banner(g, ui));
        }
        if g.show_lint && !g.lint.is_empty() {
            egui::Panel::top("lint").show(ui, |ui| lint_banner(g, ui));
        }
        egui::Panel::bottom("status_bar").show(ui, |ui| status_bar(g, ui));

        if g.profile.is_none() {
            egui::CentralPanel::default().show(ui, |ui| {
                ui.centered_and_justified(|ui| {
                    ui.heading("No profile loaded");
                    ui.label(&g.status);
                });
            });
            return;
        }

        match g.tab {
            Tab::Abilities => abilities::show(g, ui),
            Tab::Rotations => rotations::show(g, ui),
            Tab::Diff => diff_tab::show(g, ui),
        }
    }
}

fn header(g: &mut Data, ui: &mut egui::Ui) {
    ui.horizontal(|ui| {
        ui.add(egui::Label::new(
            egui::RichText::new("PQR").strong().size(16.0),
        ));
        ui.separator();

        // Profile picker (auto-saves pending edits; refuses on save error).
        let labels: Vec<String> =
            g.available.iter().map(|p| p.label()).collect();
        let current = g.current().map(|c| c.label()).unwrap_or_default();
        let mut pick: Option<usize> = None;
        egui::ComboBox::from_id_salt("profile_pick")
            .selected_text(if current.is_empty() { "no profiles" } else { current.as_str() })
            .width(240.0)
            .show_ui(ui, |ui| {
                for (i, label) in labels.iter().enumerate() {
                    if ui
                        .selectable_label(g.sel_profile == Some(i), label)
                        .clicked()
                    {
                        pick = Some(i);
                    }
                }
            });
        if let Some(i) = pick {
            g.select_profile(i);
        }

        ui.separator();

        // Tabs with dirty markers.
        for tab in Tab::ALL {
            let dirty = match tab {
                Tab::Abilities => g.dirty[0],
                Tab::Rotations => g.dirty[1],
                Tab::Diff => false,
            };
            let label = if dirty {
                format!("{} \u{2022}", tab.label())
            } else {
                tab.label().to_string()
            };
            let selected = g.tab == tab;
            if ui.selectable_label(selected, label).clicked() {
                g.tab = tab;
            }
        }

        ui.separator();
        if ui.button("Lint").clicked() {
            g.lint();
        }
        let unresolved = g.unresolved_count();
        if unresolved > 0 {
            ui.colored_label(
                egui::Color32::from_rgb(255, 90, 90),
                format!("{unresolved} unresolved"),
            );
        }

        ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
            let dirty_n = g.dirty.iter().filter(|d| **d).count();
            let save = ui.add_enabled(
                dirty_n > 0,
                egui::Button::new(if dirty_n > 0 {
                    format!("Save ({dirty_n})")
                } else {
                    "Saved".to_string()
                }),
            );
            if save.clicked() {
                g.save_dirty();
            }
        });
    });
}

fn conflict_banner(g: &mut Data, ui: &mut egui::Ui) {
    let id = g.current().map(|c| c.label()).unwrap_or_default();
    egui::Frame::popup(ui.style()).show(ui, |ui| {
        ui.horizontal(|ui| {
            ui.colored_label(
                ui.visuals().warn_fg_color,
                "\u{26a0} file changed on disk while you edit",
            );
            ui.weak(&id);
            ui.separator();
            let mut reload: Option<usize> = None;
            let mut keep: Option<usize> = None;
            for which in 0..2 {
                if !g.conflict[which] {
                    continue;
                }
                let name = if which == 0 {
                    g.current().map(|c| c.abilities_file()).unwrap_or_default()
                } else {
                    g.current().map(|c| c.rotations_file()).unwrap_or_default()
                };
                ui.weak(format!("[{name}]"));
                if ui.button("reload (discard mine)").clicked() {
                    reload = Some(which);
                }
                if ui.button("keep mine").clicked() {
                    keep = Some(which);
                }
            }
            if let Some(w) = reload {
                g.conflict_reload(w);
            }
            if let Some(w) = keep {
                g.conflict_keep(w);
            }
        });
    });
}

fn lint_banner(g: &mut Data, ui: &mut egui::Ui) {
    egui::Frame::popup(ui.style()).show(ui, |ui| {
        ui.horizontal(|ui| {
            ui.colored_label(
                egui::Color32::from_rgb(255, 90, 90),
                format!("lint \u{2014} {} issue(s)", g.lint.len()),
            );
            ui.separator();
            if ui.button("hide").clicked() {
                g.show_lint = false;
            }
            if ui.button("re-run").clicked() {
                g.lint();
            }
        });
        egui::ScrollArea::vertical()
            .id_salt("lint_list")
            .max_height(140.0)
            .show(ui, |ui| {
                for issue in &g.lint {
                    ui.colored_label(egui::Color32::from_rgb(255, 140, 140), issue);
                }
            });
    });
}

fn status_bar(g: &mut Data, ui: &mut egui::Ui) {
    ui.horizontal(|ui| {
        if let Some(id) = g.current() {
            ui.weak(id.label());
            ui.separator();
        }
        if g.dirty[0] {
            ui.colored_label(ui.visuals().warn_fg_color, "Abilities unsaved");
        }
        if g.dirty[1] {
            ui.colored_label(ui.visuals().warn_fg_color, "Rotations unsaved");
        }
        ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
            ui.weak(&g.status);
        });
    });
}
