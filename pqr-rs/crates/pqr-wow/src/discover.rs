//! 3-step process discovery, matching `clsMemory.WoWProcesses` (see
//! `reversed/.../clsMemory.cs:83-128` and `ARCHITECTURE.md §2`):
//!   1. Enumerate all processes named `Wow`.
//!   2. For each, for each loaded offsets file, read 5 ASCII bytes at
//!      `image_base + wow_version_offset` and compare to `CurrentWoWVersion`.
//!   3. If matched, read up to 30 UTF-8 bytes at `image_base + player_name`.
//!      Skip if empty (character not yet in the world).

use std::io;
use std::sync::Arc;

use pqr_mem::{find_by_name, open, ProcessInfo};
use tracing::debug;

use crate::client::WowClient;
use crate::offsets::Offsets;

#[derive(Debug)]
pub struct Discovery {
    pub process: ProcessInfo,
    pub matched_offsets: Offsets,
    pub player_name: String,
    pub version_string: String,
}

impl Discovery {
    pub fn into_client(self) -> io::Result<WowClient> {
        let mem = open(self.process.pid)?;
        Ok(WowClient::new(Arc::from(mem), self.matched_offsets))
    }
}

pub fn discover(candidates: &[Offsets]) -> io::Result<Vec<Discovery>> {
    let mut out = Vec::new();
    for proc in find_by_name("Wow")? {
        let mem = match open(proc.pid) {
            Ok(m) => m,
            Err(e) => { debug!(pid = proc.pid, error = %e, "open failed"); continue; }
        };
        let base = mem.image_base();
        for offs in candidates {
            let version_string = match mem.read_utf8_string(base.wrapping_add(offs.wow_version_offset), 5) {
                Ok(s) => s,
                Err(_) => continue,
            };
            if version_string != offs.version.to_string() {
                continue;
            }
            let player_name = mem.read_utf8_string(base.wrapping_add(offs.player_name), 30)
                .unwrap_or_default();
            if player_name.is_empty() {
                debug!(pid = proc.pid, version = %version_string, "PlayerName empty — not in world");
                continue;
            }
            out.push(Discovery {
                process: proc.clone(),
                matched_offsets: offs.clone(),
                player_name,
                version_string,
            });
            break;
        }
    }
    Ok(out)
}
