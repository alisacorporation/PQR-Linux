//! Rotations tab: rotation list on the left, priority-order editor on the
//! right. Every RotationList entry is validated against the ability table —
//! the mismatch the old editor dropped silently is shown in red here.

use eframe::egui;

use pqr_profile::Rotation;

use crate::state::Gui;

pub fn show(g: &mut Gui, ui: &mut egui::Ui) {
    egui::Panel::left("rotations_list")
        .resizable(true)
        .default_size(280.0)
        .show(ui, |ui| list(g, ui));
    egui::CentralPanel::default().show(ui, |ui| editor(g, ui));
}

fn list(g: &mut Gui, ui: &mut egui::Ui) {
    let mut clicked: Option<usize> = None;
    egui::ScrollArea::vertical().id_salt("rotation_list").show(ui, |ui| {
        if let Some(p) = g.profile.as_ref() {
            for (i, r) in p.rotations.iter().enumerate() {
                let unresolved = r.resolve(&p.abilities).is_err();
                let selected = g.sel_rotation == Some(i);
                ui.horizontal(|ui| {
                    if ui.selectable_label(selected, r.name.clone()).clicked() {
                        clicked = Some(i);
                    }
                    if r.default {
                        ui.colored_label(ui.visuals().strong_text_color(), "\u{2605}");
                    }
                    if unresolved {
                        ui.colored_label(egui::Color32::from_rgb(255, 90, 90), "!");
                    }
                });
            }
            if p.rotations.is_empty() {
                ui.weak("no rotations");
            }
        } else {
            ui.weak("no profile loaded");
        }
    });
    if let Some(i) = clicked {
        g.sel_rotation = Some(i);
    }

    ui.separator();
    if ui.button("+ New rotation").clicked() {
        if let Some(p) = g.profile.as_mut() {
            let mut n = p.rotations.len() + 1;
            let mut name = format!("New Rotation {n}");
            while p.rotations.iter().any(|r| r.name == name) {
                n += 1;
                name = format!("New Rotation {n}");
            }
            p.rotations.push(Rotation {
                name: name.clone(),
                default: false,
                priority: Vec::new(),
                require_combat: false,
                notes: String::new(),
            });
            g.dirty[1] = true;
            let idx = p.rotations.len() - 1;
            g.sel_rotation = Some(idx);
            g.status = format!("added \u{201c}{name}\u{201d} (unsaved)");
        }
    }
}

fn editor(g: &mut Gui, ui: &mut egui::Ui) {
    let valid = g
        .sel_rotation
        .and_then(|i| g.profile.as_ref().and_then(|p| p.rotations.get(i).map(|_| i)))
        .is_some();
    if !valid {
        ui.centered_and_justified(|ui| ui.weak("select a rotation"));
        return;
    }
    let i = g.sel_rotation.expect("checked above");

    let mut changed = false;
    let mut delete = false;
    let mut add_name: Option<String> = None;
    let mut default_promote = false;
    let mut moves: Vec<(usize, isize)> = Vec::new();
    let rot_count = g.profile.as_ref().map(|p| p.rotations.len()).unwrap_or(0);

    egui::ScrollArea::vertical().id_salt("rotation_editor").show(ui, |ui| {
        let p = g.profile.as_mut().expect("loaded");
        let r = &mut p.rotations[i];

        ui.horizontal(|ui| {
            ui.heading("Rotation");
            ui.weak(format!("[{}/{}]", i + 1, rot_count));
            ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                if ui.button("Delete").clicked() {
                    delete = true;
                }
            });
        });

        egui::Grid::new("rotation_fields").num_columns(2).show(ui, |ui| {
            ui.label("Name");
            changed |= ui
                .add(egui::TextEdit::singleline(&mut r.name).desired_width(320.0).code_editor())
                .changed();
            ui.end_row();

            ui.label("Default");
            let was = r.default;
            changed |= ui.checkbox(&mut r.default, "checked on profile load").changed();
            if r.default && !was {
                default_promote = true;
            }
            ui.end_row();

            ui.label("Require combat");
            changed |= ui.checkbox(&mut r.require_combat, "").changed();
            ui.end_row();
        });

        ui.add_space(4.0);
        ui.label("Notes");
        changed |= ui
            .add(
                egui::TextEdit::multiline(&mut r.notes)
                    .desired_rows(2)
                    .desired_width(f32::INFINITY),
            )
            .changed();

        // ---- priority list ----
        ui.separator();
        ui.horizontal(|ui| {
            ui.label(egui::RichText::new("Priority list").strong());
            ui.weak(format!("{} entries", r.priority.len()));
        });

        let names: Vec<String> =
            p.abilities.iter().map(|a| a.name.clone()).collect();
        let name_set: std::collections::HashSet<&str> =
            p.abilities.iter().map(|a| a.name.trim()).collect();

        egui::Grid::new("priority_grid")
            .num_columns(4)
            .striped(true)
            .show(ui, |ui| {
                for (rank, entry) in r.priority.iter().enumerate() {
                    ui.weak(format!("{}", rank + 1));
                    let missing = !name_set.contains(entry.trim());
                    if missing {
                        ui.colored_label(
                            egui::Color32::from_rgb(255, 90, 90),
                            format!("\u{201c}{entry}\u{201d} \u{2014} not an ability"),
                        );
                    } else {
                        ui.label(entry);
                    }
                    // Arrows exist only in Hack — force the monospace font
                    // (proportional Ubuntu-Light has no U+25B2/U+25BC).
                    if rank > 0 {
                        if ui
                            .button(egui::RichText::new("\u{25b2}").monospace())
                            .on_hover_text("move up")
                            .clicked()
                        {
                            moves.push((rank, -1));
                        }
                    } else {
                        // Empty cell keeps the column grid-aligned; the column
                        // width comes from the other rows' buttons.
                        ui.weak("");
                    }
                    if ui
                        .add_enabled(
                            rank + 1 < r.priority.len(),
                            egui::Button::new(egui::RichText::new("\u{25bc}").monospace()),
                        )
                        .on_hover_text("move down")
                        .clicked()
                    {
                        moves.push((rank, 1));
                    }
                    ui.end_row();
                }
                if r.priority.is_empty() {
                    ui.weak("");
                    ui.weak("empty \u{2014} nothing will cast");
                    ui.weak("");
                    ui.weak("");
                    ui.end_row();
                }
            });
        // Remove: pick from a compact combo (one-shot picker per frame).
        ui.horizontal(|ui| {
            ui.label("Remove:");
            egui::ComboBox::from_id_salt("priority_remove")
                .selected_text("select entry\u{2026}")
                .width(320.0)
                .show_ui(ui, |ui| {
                    for (rank, entry) in r.priority.iter().enumerate() {
                        if ui.selectable_label(false, format!("{:>2}. {entry}", rank + 1)).clicked()
                        {
                            moves.push((rank, isize::MIN)); // sentinel: remove
                        }
                    }
                });
        });

        ui.horizontal(|ui| {
            ui.label("Add:");
            egui::ComboBox::from_id_salt("priority_add")
                .selected_text("select ability\u{2026}")
                .width(360.0)
                .show_ui(ui, |ui| {
                    egui::ScrollArea::vertical().max_height(240.0).show(ui, |ui| {
                        for name in &names {
                            if ui.selectable_label(false, name.clone()).clicked() {
                                add_name = Some(name.clone());
                            }
                        }
                    });
                });
            if let Some(name) = add_name.clone() {
                r.priority.push(name);
                changed = true;
            }
        });
    });

    if !moves.is_empty() {
        let p = g.profile.as_mut().expect("loaded");
        let list = &mut p.rotations[i].priority;
        for (rank, dir) in moves {
            if dir == isize::MIN {
                if rank < list.len() {
                    list.remove(rank);
                }
                g.dirty[1] = true;
                g.status = format!("removed priority entry #{} (unsaved)", rank + 1);
            } else if dir < 0 && rank > 0 {
                list.swap(rank - 1, rank);
                g.dirty[1] = true;
            } else if dir > 0 && rank + 1 < list.len() {
                list.swap(rank, rank + 1);
                g.dirty[1] = true;
            }
        }
    }

    g.dirty[1] |= changed;
    if default_promote {
        let p = g.profile.as_mut().expect("loaded");
        for (j, other) in p.rotations.iter_mut().enumerate() {
            if j != i {
                other.default = false;
            }
        }
        g.dirty[1] = true;
        g.status = "default rotation switched (unsaved)".into();
    }
    if delete {
        let (removed, len) = {
            let p = g.profile.as_mut().expect("loaded");
            let removed = if i < p.rotations.len() {
                Some(p.rotations.remove(i))
            } else {
                None
            };
            let len = p.rotations.len();
            (removed, len)
        };
        if let Some(removed) = removed {
            g.dirty[1] = true;
            g.sel_rotation = if len == 0 {
                None
            } else if i >= len {
                Some(len - 1)
            } else {
                Some(i)
            };
            g.status = format!("deleted \u{201c}{}\u{201d} (unsaved)", removed.name);
        }
    }
}
