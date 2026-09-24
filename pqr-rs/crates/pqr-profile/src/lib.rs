//! Legacy PQR XML profile parser/writer.
//!
//! Byte-exact compatibility with `DarhangeR_<CLASS>_{Abilities,Rotations}.xml`.
//! Ports `reversed/pqr-app/PriorityQueueRotation/clsXML.cs` including its
//! double XMLEncode/XMLDecode quirk (see clsXML.cs:575-590).

pub mod escape;
pub mod model;
pub mod parse;
pub mod write;

pub use model::{Ability, Rotation, Profile, TargetKind, UnresolvedName};
pub use parse::{parse_abilities, parse_rotations, ProfileError};
