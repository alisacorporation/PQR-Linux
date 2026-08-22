using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class Probe6
{
    [StructLayout(LayoutKind.Sequential)]
    struct THREADENTRY32 { public uint dwSize; public uint cntUsage; public int th32ThreadID;
        public int th32OwnerProcessID; public int tpBasePri; public int tpDeltaPri; public uint dwFlags; }

    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr CreateToolhelp32Snapshot(uint flags, int pid);
    [DllImport("kernel32.dll")] static extern bool Thread32First(IntPtr h, ref THREADENTRY32 e);
    [DllImport("kernel32.dll")] static extern bool Thread32Next(IntPtr h, ref THREADENTRY32 e);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenThread(uint a, bool b, int tid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    static void Main()
    {
        var wow = Process.GetProcessesByName("Wow")[0];
        Console.WriteLine("Wow PID=" + wow.Id);

        var snap = CreateToolhelp32Snapshot(0x4 /*TH32CS_SNAPTHREAD*/, 0);
        if (snap == IntPtr.Zero || snap == new IntPtr(-1)) { Console.WriteLine("snapshot FAIL " + Marshal.GetLastWin32Error()); return; }

        int best = int.MaxValue, count = 0;
        var e = new THREADENTRY32(); e.dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32));
        if (Thread32First(snap, ref e))
        {
            do {
                if (e.th32OwnerProcessID == wow.Id) { count++; if (e.th32ThreadID < best) best = e.th32ThreadID; }
            } while (Thread32Next(snap, ref e));
        }
        CloseHandle(snap);
        Console.WriteLine("threads found=" + count + ", lowest TID=" + best);

        if (best != int.MaxValue)
        {
            var ht = OpenThread(0x1F03FF, false, best);
            Console.WriteLine(ht == IntPtr.Zero ? "OpenThread FAIL err=" + Marshal.GetLastWin32Error()
                                                : "OpenThread OK handle=0x" + ht.ToString("X"));
            if (ht != IntPtr.Zero) CloseHandle(ht);
        }
        Console.WriteLine("DONE");
    }
}
