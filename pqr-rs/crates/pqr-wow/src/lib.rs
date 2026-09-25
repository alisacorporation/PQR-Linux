//! WoW domain layer: offsets, process discovery, in-world state accessors.
//!
//! Ports:
//! - `clsOffsets.cs` -> `Offsets` struct + XML loader
//! - `clsMemory.WoWProcesses` -> `discover()`
//! - `clsMemory.GetPlayerName/GetPlayerClass/WorldHasFocus/IsWoWReady` ->
//!   trait methods on `WowClient`.

pub mod offsets;
pub mod discover;
pub mod client;
pub mod scan;

pub use offsets::{Offsets, OffsetsError};
pub use discover::{discover, Discovery};
pub use client::{WowClient, WowClass};
pub use scan::resolve_clnt_obj_mgr;
