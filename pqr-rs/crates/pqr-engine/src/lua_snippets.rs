//! The four Lua strings originally embedded in `clsLua.cs`. Extracted to
//! standalone `.lua` files so they can be inspected, syntax-highlighted,
//! and evolved independently of the Rust source.

pub const FIRST_LOAD: &str = include_str!("../resources/lua/first_load.lua");
pub const SCRIPTING: &str = include_str!("../resources/lua/scripting.lua");
pub const TABLE_SETUP: &str = include_str!("../resources/lua/table_setup.lua");
pub const CLEAR_TABLES: &str = include_str!("../resources/lua/clear_tables.lua");
