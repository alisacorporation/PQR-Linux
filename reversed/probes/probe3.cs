using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class Probe3
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint a, bool b, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);

    static void Main()
    {
        Process wow = null;
        foreach (var p in Process.GetProcesses())
            try { if (p.ProcessName.ToLower().Contains("wow")) { wow = p; break; } } catch {}
        IntPtr h = OpenProcess(0x1F0FFF, false, wow.Id);

        Check(h, 0x419210, "Lua_DoStringAddress (expect 55 8B EC 81 EC F8 00 00 00)");
        Check(h, 0x3225E0, "Lua_GetLocalizedTextAddress");
        Check(h, 0x7D078A, "GameState (expect small int 0..12)");
        Console.WriteLine("DONE");
    }

    static void Check(IntPtr h, long addrL, string label)
    {
        byte[] buf = new byte[16]; int r;
        var hex = new StringBuilder();
        if (!ReadProcessMemory(h, new IntPtr(addrL), buf, 16, out r)) { Console.WriteLine(label + ": FAIL"); return; }
        foreach (byte b in buf) hex.Append(b.ToString("X2") + " ");
        Console.WriteLine(label);
        Console.WriteLine("  " + hex);
    }
}
