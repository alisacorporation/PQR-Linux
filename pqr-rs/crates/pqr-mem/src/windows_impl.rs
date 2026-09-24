//! Windows backend. Port of the load-bearing pieces of BlackMagic:
//! `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`,
//! `VirtualAllocEx`/`VirtualFreeEx`, plus Toolhelp process enumeration and
//! `EnumProcessModules`+`GetModuleInformation` for the image base.

use std::io;
use std::mem::size_of;

use windows::Win32::Foundation::{CloseHandle, HANDLE, HMODULE};
use windows::Win32::System::Diagnostics::Debug::{ReadProcessMemory, WriteProcessMemory};
use windows::Win32::System::Diagnostics::ToolHelp::{
    CreateToolhelp32Snapshot, Process32FirstW, Process32NextW, PROCESSENTRY32W, TH32CS_SNAPPROCESS,
};
use windows::Win32::System::Memory::{
    VirtualAllocEx, VirtualFreeEx, MEM_COMMIT, MEM_RELEASE, MEM_RESERVE,
    PAGE_EXECUTE_READWRITE, PAGE_READWRITE,
};
use windows::Win32::System::ProcessStatus::{EnumProcessModules, GetModuleInformation, MODULEINFO};
use windows::Win32::System::Threading::{OpenProcess, PROCESS_ALL_ACCESS};

use crate::{ProcessInfo, ProcessMemory};

fn last_error(context: &str) -> io::Error {
    io::Error::new(io::ErrorKind::Other, format!("{context}: {}", io::Error::last_os_error()))
}

pub fn find_by_name(name: &str) -> io::Result<Vec<ProcessInfo>> {
    let mut out = Vec::new();
    unsafe {
        let snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0)
            .map_err(|e| io::Error::other(format!("CreateToolhelp32Snapshot: {e}")))?;
        let _guard = HandleGuard(snap);

        let mut entry = PROCESSENTRY32W {
            dwSize: size_of::<PROCESSENTRY32W>() as u32,
            ..Default::default()
        };

        if Process32FirstW(snap, &mut entry).is_err() {
            return Ok(out);
        }
        loop {
            let end = entry.szExeFile.iter().position(|&c| c == 0).unwrap_or(entry.szExeFile.len());
            let exe = String::from_utf16_lossy(&entry.szExeFile[..end]);
            // BlackMagic used exact `"Wow"` (Process.GetProcessesByName strips `.exe`).
            // On disk the entry is `Wow.exe`, so we compare against both.
            let stem = exe.strip_suffix(".exe").unwrap_or(&exe);
            if stem.eq_ignore_ascii_case(name) {
                out.push(ProcessInfo { pid: entry.th32ProcessID, name: exe });
            }
            if Process32NextW(snap, &mut entry).is_err() { break; }
        }
    }
    Ok(out)
}

pub fn open(pid: u32) -> io::Result<WinProcess> {
    unsafe {
        let handle = OpenProcess(PROCESS_ALL_ACCESS, false, pid)
            .map_err(|e| io::Error::other(format!("OpenProcess({pid}): {e}")))?;
        let image_base = enum_first_module_base(handle)?;
        Ok(WinProcess { pid, handle, image_base })
    }
}

unsafe fn enum_first_module_base(handle: HANDLE) -> io::Result<u32> {
    let mut hmod = HMODULE::default();
    let mut needed = 0u32;
    EnumProcessModules(handle, &mut hmod, size_of::<HMODULE>() as u32, &mut needed)
        .map_err(|e| io::Error::other(format!("EnumProcessModules: {e}")))?;

    let mut info = MODULEINFO::default();
    GetModuleInformation(handle, hmod, &mut info, size_of::<MODULEINFO>() as u32)
        .map_err(|e| io::Error::other(format!("GetModuleInformation: {e}")))?;
    Ok(info.lpBaseOfDll as usize as u32)
}

pub struct WinProcess {
    pid: u32,
    handle: HANDLE,
    image_base: u32,
}

impl Drop for WinProcess {
    fn drop(&mut self) {
        unsafe { let _ = CloseHandle(self.handle); }
    }
}

/// RAII for Toolhelp snapshot / other short-lived HANDLEs.
struct HandleGuard(HANDLE);
impl Drop for HandleGuard {
    fn drop(&mut self) { unsafe { let _ = CloseHandle(self.0); } }
}

// WinProcess owns the HANDLE; we never share it across threads without care.
// The Win32 handles themselves are thread-safe for these operations.
unsafe impl Send for WinProcess {}
unsafe impl Sync for WinProcess {}

impl ProcessMemory for WinProcess {
    fn pid(&self) -> u32 { self.pid }
    fn image_base(&self) -> u32 { self.image_base }

    fn read_bytes(&self, addr: u32, len: usize) -> io::Result<Vec<u8>> {
        let mut buf = vec![0u8; len];
        let mut read = 0usize;
        unsafe {
            ReadProcessMemory(
                self.handle,
                addr as *const _,
                buf.as_mut_ptr() as *mut _,
                len,
                Some(&mut read),
            ).map_err(|_| last_error("ReadProcessMemory"))?;
        }
        buf.truncate(read);
        Ok(buf)
    }

    fn write_bytes(&self, addr: u32, data: &[u8]) -> io::Result<()> {
        let mut written = 0usize;
        unsafe {
            WriteProcessMemory(
                self.handle,
                addr as *mut _,
                data.as_ptr() as *const _,
                data.len(),
                Some(&mut written),
            ).map_err(|_| last_error("WriteProcessMemory"))?;
        }
        if written != data.len() {
            return Err(io::Error::other(format!(
                "WriteProcessMemory: short write ({written}/{})", data.len()
            )));
        }
        Ok(())
    }

    fn alloc(&self, size: usize, exec: bool) -> io::Result<u32> {
        let protect = if exec { PAGE_EXECUTE_READWRITE } else { PAGE_READWRITE };
        let ptr = unsafe {
            VirtualAllocEx(self.handle, None, size, MEM_COMMIT | MEM_RESERVE, protect)
        };
        if ptr.is_null() {
            return Err(last_error("VirtualAllocEx"));
        }
        let addr = ptr as usize;
        if addr > u32::MAX as usize {
            // Should never happen against a 32-bit target under WOW64, but be
            // explicit — the injected asm can't reach 64-bit addresses.
            unsafe { let _ = VirtualFreeEx(self.handle, ptr, 0, MEM_RELEASE); }
            return Err(io::Error::other(format!(
                "VirtualAllocEx returned address above 4GB (0x{addr:x})"
            )));
        }
        Ok(addr as u32)
    }

    fn free(&self, addr: u32) -> io::Result<()> {
        unsafe {
            VirtualFreeEx(self.handle, addr as *mut _, 0, MEM_RELEASE)
                .map_err(|_| last_error("VirtualFreeEx"))?;
        }
        Ok(())
    }
}
