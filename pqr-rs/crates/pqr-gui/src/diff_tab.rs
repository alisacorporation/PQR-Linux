//! Diff tab: side-by-side aligned rotation comparison (ports the viewer's
//! compare.js alignment — see `pqr_profile::compare`).

use eframe::egui;

use pqr_profile::compare::{split_ability_name, Pair, PairKind, SortMode};

use crate::state::Gui;

const COLOR_ONLY_A: egui::Color32 = egui::Color32::from_rgb(255, 165, 70);
const COLOR_ONLY_B: egui::Color32 = egui::Color32::from_rgb(110, 175, 255);
const COLOR_GHOST: egui::Color32 = egui::Color32::from_rgb(255, 90, 90);
const COLOR_DELTA: egui::Color32 = egui::Color32::from_rgb(255, 230, 120);
const COLOR_DIM: egui::Color32 = egui::Color32::from_rgb(130, 130, 140);

pub fn show(g: &mut Gui, ui: &mut egui::Ui) {
    egui::CentralPanel::default().show(ui, |ui| {
        let Some(p) = g.profile.as_ref() else {
            ui.centered_and_justified(|ui| ui.weak("no profile loaded"));
            return;
        };
        if p.rotations.is_empty() {
            ui.centered_and_justified(|ui| ui.weak("profile has no rotations"));
            return;
        }

        // ---- controls ----
        ui.horizontal_wrapped(|ui| {
            ui.label("Left:");
            egui::ComboBox::from_id_salt("diff_left")
                .selected_text(&p.rotations[g.diff_a.min(p.rotations.len() - 1)].name)
                .width(240.0)
                .show_ui(ui, |ui| {
                    for (i, r) in p.rotations.iter().enumerate() {
                        ui.selectable_value(&mut g.diff_a, i, r.name.clone());
                    }
                });
            ui.label("Right:");
            egui::ComboBox::from_id_salt("diff_right")
                .selected_text(&p.rotations[g.diff_b.min(p.rotations.len() - 1)].name)
                .width(240.0)
                .show_ui(ui, |ui| {
                    for (i, r) in p.rotations.iter().enumerate() {
                        ui.selectable_value(&mut g.diff_b, i, r.name.clone());
                    }
                });
            ui.separator();
            ui.label("Sort:");
            egui::ComboBox::from_id_salt("diff_sort")
                .selected_text(match g.diff_opts.sort {
                    SortMode::OrderA => "order (left)",
                    SortMode::Alpha => "A-Z",
                    SortMode::Delta => "divergence",
                })
                .width(120.0)
                .show_ui(ui, |ui| {
                    ui.selectable_value(&mut g.diff_opts.sort, SortMode::OrderA, "order (left)");
                    ui.selectable_value(&mut g.diff_opts.sort, SortMode::Alpha, "A-Z");
                    ui.selectable_value(&mut g.diff_opts.sort, SortMode::Delta, "divergence");
                });
            ui.checkbox(&mut g.diff_opts.only_diff, "only diff");
            ui.checkbox(&mut g.diff_opts.show_sections, "sections");
        });

        let Some(result) = g.diff_result() else {
            ui.weak("select two rotations");
            return;
        };

        // ---- stats ----
        let s = result.stats;
        ui.horizontal(|ui| {
            ui.weak(format!(
                "both {both} \u{00b7} same {same} \u{00b7} only left {only_a} \u{00b7} \
                 only right {only_b} \u{00b7} ghosts {ghost} \u{00b7} \
                 left {total_a} / right {total_b} entries",
                both = s.both,
                same = s.same,
                only_a = s.only_a,
                only_b = s.only_b,
                ghost = s.ghost,
                total_a = s.total_a,
                total_b = s.total_b,
            ));
        });
        ui.separator();

        // ---- table ----
        egui::ScrollArea::vertical().id_salt("diff_table").show(ui, |ui| {
            egui::Grid::new("diff_grid")
                .num_columns(5)
                .striped(true)
                .min_col_width(40.0)
                .show(ui, |ui| {
                    // header
                    ui.weak("L#");
                    ui.weak("left ability");
                    ui.weak("\u{0394}");
                    ui.weak("right ability");
                    ui.weak("R#");
                    ui.end_row();

                    for pair in &result.pairs {
                        row(ui, pair);
                    }
                });
            if result.pairs.is_empty() {
                ui.weak("nothing matches the current filters");
            }
        });
    });
}

fn row(ui: &mut egui::Ui, pair: &Pair) {
    let rank_style = |ui: &mut egui::Ui, txt: String| {
        if txt == "\u{2014}" {
            ui.weak(txt);
        } else {
            ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                ui.weak(txt);
            });
        }
    };

    rank_style(ui, pair.rank_a.map(|r| r.to_string()).unwrap_or("\u{2014}".into()));
    name_cell(ui, pair, true);
    // delta (right-aligned, weak so columns line up with the ranks)
    ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| match pair.delta {
        None if pair.section => {
            ui.weak("");
        }
        None => {
            ui.weak("\u{2014}");
        }
        Some(0) => {
            ui.weak("=");
        }
        Some(d) => {
            ui.colored_label(COLOR_DELTA, format!("{d:+}"));
        }
    });
    name_cell(ui, pair, false);
    rank_style(ui, pair.rank_b.map(|r| r.to_string()).unwrap_or("\u{2014}".into()));
    ui.end_row();
}

fn name_cell(ui: &mut egui::Ui, pair: &Pair, left: bool) {
    let name = if left { pair.name_a.as_deref() } else { pair.name_b.as_deref() };
    let prefix = if left { pair.prefix_a.as_deref() } else { pair.prefix_b.as_deref() };
    let ghost = if left { pair.ghost_a } else { pair.ghost_b };

    let Some(name) = name else {
        ui.weak("\u{00b7}");
        return;
    };

    let color = if pair.section {
        COLOR_DIM
    } else if ghost {
        COLOR_GHOST
    } else {
        match pair.kind {
            PairKind::OnlyA if left => COLOR_ONLY_A,
            PairKind::OnlyB if !left => COLOR_ONLY_B,
            _ => ui.visuals().text_color(),
        }
    };

    let base = split_ability_name(name).base;
    let mut shown = base;
    if ghost {
        shown = format!("\u{2020} {shown}");
    }
    match prefix {
        Some(pre) => {
            ui.horizontal(|ui| {
                ui.colored_label(color, egui::RichText::new(pre).monospace());
                ui.colored_label(color, shown);
            });
        }
        None => {
            ui.colored_label(color, shown);
        }
    }
}
