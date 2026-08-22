using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class Probe2
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    static void Main()
    {
        Process wow = null;
        foreach (var p in Process.GetProcesses())
            try { if (p.ProcessName.ToLower().Contains("wow")) { wow = p; break; } } catch {}
        if (wow == null) { Console.WriteLine("no wow"); return; }

        IntPtr h = OpenProcess(0x1F0FFF, false, wow.Id);
        if (h == IntPtr.Zero) { Console.WriteLine("open fail " + Marshal.GetLastWin32Error()); return; }

        Dump(h, 0x8AD851 - 8, 40, "around WoWVersionOffset 0x8AD851");
        Dump(h, 0x879D18, 32, "PlayerName 0x879D18");

        CloseHandle(h);
        Console.WriteLine("DONE");
    }

    static void Dump(IntPtr h, long addrL, int len, string label)
    {
        IntPtr addr = new IntPtr(addrL);
        byte[] buf = new byte[len]; int r;
        if (!ReadProcessMemory(h, addr, buf, len, out r)) { Console.WriteLine(label + ": READ FAIL err=" + Marshal.GetLastWin32Error()); return; }
        var hex = new StringBuilder(); var asc = new StringBuilder();
        foreach (byte b in buf)
        {
            hex.Append(b.ToString("X2") + " ");
            asc.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
        }
        Console.WriteLine(label);
        Console.WriteLine("  HEX: " + hex);
        Console.WriteLine("  ASC: " + asc);
    }
}
