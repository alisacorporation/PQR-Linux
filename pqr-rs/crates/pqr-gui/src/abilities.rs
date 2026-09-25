//! Abilities tab: filtered list on the left, full ability editor on the right.

use eframe::egui;

use pqr_profile::{Ability, TargetKind};

use crate::state::Gui;

const TARGET_OPTIONS: [(&str, &str); 7] = [
    ("Target", "Target"),
    ("Mouseover", "Mouseover"),
    ("Click", "Click"),
    ("Player", "Player"),
    ("Focus", "Focus"),
    ("Custom", "Custom"),
    ("Other…", "Other"),
];

pub fn show(g: &mut Gui, ui: &mut egui::Ui) {
    egui::Panel::left("abilities_list")
        .resizable(true)
        .default_size(280.0)
        .show(ui, |ui| list(g, ui));
    egui::CentralPanel::default().show(ui, |ui| editor(g, ui));
}

fn list(g: &mut Gui, ui: &mut egui::Ui) {
    ui.horizontal(|ui| {
        ui.label("Filter:");
        ui.add(
            egui::TextEdit::singleline(&mut g.ability_filter)
                .hint_text("name / spellid")
                .desired_width(f32::INFINITY),
        );
    });
    ui.separator();

    let filter = g.ability_filter.trim().to_lowercase();
    let mut clicked: Option<usize> = None;
    egui::ScrollArea::vertical().id_salt("ability_list").show(ui, |ui| {
        if let Some(p) = g.profile.as_ref() {
            for (i, a) in p.abilities.iter().enumerate() {
                if !filter.is_empty()
                    && !a.name.to_lowercase().contains(&filter)
                    && !a.spell_id.to_string().contains(&filter)
                {
                    continue;
                }
                let selected = g.sel_ability == Some(i);
                let text = if a.spell_id != 0 {
                    format!("{}  [{}]", a.name, a.spell_id)
                } else {
                    a.name.clone()
                };
                if ui.selectable_label(selected, text).clicked() {
                    clicked = Some(i);
                }
            }
        } else {
            ui.weak("no profile loaded");
        }
    });
    if let Some(i) = clicked {
        g.select_ability(Some(i));
    }

    ui.separator();
    if ui.button("+ New ability").clicked() {
        if let Some(p) = g.profile.as_mut() {
            let mut n = p.abilities.len() + 1;
            let mut name = format!("New Ability {n}");
            while p.abilities.iter().any(|a| a.name == name) {
                n += 1;
                name = format!("New Ability {n}");
            }
            p.abilities.push(Ability {
                name: name.clone(),
                default: false,
                spell_id: 0,
                actions: String::new(),
                lua: String::new(),
                recast_delay: 0,
                target: TargetKind::Target,
                cancel_channel: false,
                lua_before: String::new(),
                lua_after: String::new(),
            });
            g.dirty[0] = true;
            let idx = p.abilities.len() - 1;
            g.select_ability(Some(idx));
            g.status = format!("added \u{201c}{name}\u{201d} (unsaved)");
        }
    }
}

fn editor(g: &mut Gui, ui: &mut egui::Ui) {
    let valid = g
        .sel_ability
        .and_then(|i| g.profile.as_ref().and_then(|p| p.abilities.get(i).map(|_| i)))
        .is_some();
    if !valid {
        ui.centered_and_justified(|ui| ui.weak("select an ability"));
        return;
    }
    let i = g.sel_ability.expect("checked above");

    let selected_name = g
        .profile
        .as_ref()
        .and_then(|p| p.abilities.get(i))
        .map(|a| a.name.clone());
    let rename_pending = selected_name
        .as_deref()
        .is_some_and(|n| n != g.name_buf.trim() && !g.name_buf.trim().is_empty());

    let mut changed = false;
    let mut delete = false;
    let mut commit_enter = false;

    egui::ScrollArea::vertical().id_salt("ability_editor").show(ui, |ui| {
        ui.horizontal(|ui| {
            ui.heading("Ability");
            if let Some(p) = g.profile.as_ref() {
                ui.weak(format!("[{}/{}]", i + 1, p.abilities.len()));
            }
            ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                if ui.button("Delete").clicked() {
                    delete = true;
                }
            });
        });

        ui.horizontal(|ui| {
            ui.label("Name");
            let resp = ui.add(
                egui::TextEdit::singleline(&mut g.name_buf)
                    .desired_width(320.0)
                    .code_editor(),
            );
            changed |= resp.changed();
            if rename_pending {
                ui.colored_label(
                    ui.visuals().warn_fg_color,
                    egui::RichText::new(
                        "\u{2192} rename updates rotation references on save",
                    )
                    .monospace(),
                );
            }
            if resp.lost_focus() && ui.input(|i| i.key_pressed(egui::Key::Enter)) {
                commit_enter = true;
            }
        });
        ui.add_space(6.0);

        let p = g.profile.as_mut().expect("loaded");
        let a = &mut p.abilities[i];

        egui::Grid::new("ability_fields").num_columns(2).show(ui, |ui| {
            ui.label("SpellID");
            changed |= ui
                .add(egui::DragValue::new(&mut a.spell_id).speed(1).range(0..=999_999))
                .changed();
            ui.end_row();

            ui.label("Recast delay (ms)");
            changed |= ui
                .add(egui::DragValue::new(&mut a.recast_delay).speed(1).range(0..=60_000))
                .changed();
            ui.end_row();

            ui.label("Target");
            egui::ComboBox::from_id_salt("ability_target")
                .selected_text(a.target.as_str())
                .width(180.0)
                .show_ui(ui, |ui| {
                    for (label, value) in TARGET_OPTIONS {
                        let selected = match (&a.target, value) {
                            (TargetKind::Other(_), "Other") => true,
                            (t, v) if v != "Other" => t.as_str() == v,
                            _ => false,
                        };
                        if ui.selectable_label(selected, label).clicked() {
                            a.target = match value {
                                "Target" => TargetKind::Target,
                                "Mouseover" => TargetKind::Mouseover,
                                "Click" => TargetKind::Click,
                                "Player" => TargetKind::Player,
                                "Focus" => TargetKind::Focus,
                                "Custom" => TargetKind::Custom,
                                "Other" => TargetKind::Other(String::new()),
                                _ => unreachable!(),
                            };
                            changed = true;
                        }
                    }
                });
            ui.end_row();

            if let TargetKind::Other(ref mut raw) = a.target {
                ui.label("Target (raw)");
                changed |= ui
                    .add(egui::TextEdit::singleline(raw).desired_width(180.0).code_editor())
                    .changed();
                ui.end_row();
            }

            ui.label("Actions");
            changed |= ui
                .add(
                    egui::TextEdit::singleline(&mut a.actions)
                        .desired_width(f32::INFINITY)
                        .code_editor(),
                )
                .changed();
            ui.end_row();

            ui.label("Default");
            changed |= ui.checkbox(&mut a.default, "checked on by default").changed();
            ui.end_row();

            ui.label("Cancel channel");
            changed |= ui.checkbox(&mut a.cancel_channel, "").changed();
            ui.end_row();
        });

        ui.separator();
        ui.label(
            egui::RichText::new("Lua test (return true \u{2192} cast)")
                .strong()
                .monospace(),
        );
        changed |= ui
            .add(
                egui::TextEdit::multiline(&mut a.lua)
                    .code_editor()
                    .desired_rows(10)
                    .desired_width(f32::INFINITY),
            )
            .changed();
        ui.add_space(6.0);

        ui.label(egui::RichText::new("LuaBefore").strong());
        changed |= ui
            .add(
                egui::TextEdit::multiline(&mut a.lua_before)
                    .code_editor()
                    .desired_rows(3)
                    .desired_width(f32::INFINITY),
            )
            .changed();
        ui.add_space(6.0);

        ui.label(egui::RichText::new("LuaAfter").strong());
        changed |= ui
            .add(
                egui::TextEdit::multiline(&mut a.lua_after)
                    .code_editor()
                    .desired_rows(3)
                    .desired_width(f32::INFINITY),
            )
            .changed();
    });

    g.dirty[0] |= changed;
    if commit_enter {
        g.commit_rename();
    }
    if delete {
        g.commit_rename();
        let (removed, empty, len) = {
            let p = g.profile.as_mut().expect("loaded");
            let removed = if i < p.abilities.len() {
                Some(p.abilities.remove(i))
            } else {
                None
            };
            let len = p.abilities.len();
            (removed, len == 0, len)
        };
        if let Some(removed) = removed {
            g.dirty[0] = true;
            g.sel_ability = if empty {
                None
            } else if i >= len {
                Some(len - 1)
            } else {
                Some(i)
            };
            g.sync_name_buf();
            g.status = format!("deleted \u{201c}{}\u{201d} (unsaved)", removed.name);
        }
    }
}
