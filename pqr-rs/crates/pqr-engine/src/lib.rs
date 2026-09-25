//! Lua bootstrap injection + rotation ticker.
//!
//! The heavy lifting sits inside WoW's Lua VM — this crate's job is to
//! (1) inject the PQR framework at startup, (2) register the abilities of
//! whichever rotation we're running, (3) tell the bot which rotation to run,
//! and (4) shut down cleanly.

pub mod ident;
pub mod lua_snippets;
pub mod lua_call;
pub mod ability;
pub mod engine;

pub use engine::{ChainWork, Engine, EngineError, LuaEvent};
pub use ident::IdentReplacer;
