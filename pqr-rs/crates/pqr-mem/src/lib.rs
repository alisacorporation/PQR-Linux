//! Cross-platform process memory access.
//!
//! v0.1: Windows backend only. Target platform is 32-bit WoW 3.3.5a, so
//! addresses are `u32` throughout. The exe is cross-compiled to Windows and
//! run under Wine alongside WoW — this is the model current PQR uses, so
//! offsets and injection strategy are known-compatible.
//!
//! v0.2+ will add a native Linux backend using `process_vm_readv` and a
//! ptrace-based `mmap` injector for allocation.

use std::io;

#[derive(Debug, Clone)]
pub struct ProcessInfo {
    pub pid: u32,
    pub name: String,
}

/// Common interface. All addresses and sizes are 32-bit — the target process
/// is 32-bit WoW, and every offset in the ecosystem is a `u32` RVA.
pub trait ProcessMemory: Send + Sync {
    fn pid(&self) -> u32;
    fn image_base(&self) -> u32;

    fn read_bytes(&self, addr: u32, len: usize) -> io::Result<Vec<u8>>;
    fn write_bytes(&self, addr: u32, data: &[u8]) -> io::Result<()>;

    fn alloc(&self, size: usize, exec: bool) -> io::Result<u32>;
    fn free(&self, addr: u32) -> io::Result<()>;

    // Typed helpers -----------------------------------------------------

    fn read_u32(&self, addr: u32) -> io::Result<u32> {
        let b = self.read_bytes(addr, 4)?;
        Ok(u32::from_le_bytes(b.try_into().unwrap()))
    }

    fn read_i32(&self, addr: u32) -> io::Result<i32> {
        let b = self.read_bytes(addr, 4)?;
        Ok(i32::from_le_bytes(b.try_into().unwrap()))
    }

    fn write_u32(&self, addr: u32, value: u32) -> io::Result<()> {
        self.write_bytes(addr, &value.to_le_bytes())
    }

    /// Read UTF-8 bytes, truncating at the first NUL. Mirrors
    /// `clsMemory.ReadUTF8String` (see `reversed/.../clsMemory.cs:27-46`).
    fn read_utf8_string(&self, addr: u32, max_len: usize) -> io::Result<String> {
        let bytes = self.read_bytes(addr, max_len)?;
        let end = bytes.iter().position(|&b| b == 0).unwrap_or(bytes.len());
        Ok(String::from_utf8_lossy(&bytes[..end]).into_owned())
    }
}

pub fn find_by_name(_name: &str) -> io::Result<Vec<ProcessInfo>> {
    #[cfg(windows)]
    { windows_impl::find_by_name(_name) }
    #[cfg(not(windows))]
    { unsupported("find_by_name") }
}

pub fn open(_pid: u32) -> io::Result<Box<dyn ProcessMemory>> {
    #[cfg(windows)]
    { windows_impl::open(_pid).map(|p| Box::new(p) as Box<dyn ProcessMemory>) }
    #[cfg(not(windows))]
    { unsupported("open") }
}

#[cfg(not(windows))]
fn unsupported<T>(what: &str) -> io::Result<T> {
    Err(io::Error::new(
        io::ErrorKind::Unsupported,
        format!("pqr-mem::{what}: only Windows backend available in v0.1; \
                 build for --target x86_64-pc-windows-msvc and run under Wine"),
    ))
}

#[cfg(windows)]
mod windows_impl;
