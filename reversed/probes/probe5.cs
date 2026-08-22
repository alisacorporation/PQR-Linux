using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Probe5
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint a,bool b,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenThread(uint a,bool b,int tid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out int r);

    static void Step(string name, Action f)
    {
        try { f(); }
        catch (Exception e) { Console.WriteLine("[FAIL] " + name + " -> " + e.GetType().Name + ": " + e.Message); }
    }

    static void Main()
    {
        var ps = Process.GetProcessesByName("Wow");
        Console.WriteLine("GetProcessesByName(\"Wow\"): " + ps.Length);
        if (ps.Length == 0) return;
        var wow = ps[0];
        Console.WriteLine("PID=" + wow.Id);

        Step("EnterDebugMode", () => Process.EnterDebugMode());
        Console.WriteLine("[ok] EnterDebugMode");

        Step("Modules", () => {
            var mods = Process.GetProcessById(wow.Id).Modules;
            Console.WriteLine("[ok] Modules count=" + mods.Count + " main=" + mods[0].BaseAddress.ToString("X"));
        });

        int tid = 0;
        Step("Threads[0]", () => { tid = Process.GetProcessById(wow.Id).Threads[0].Id; Console.WriteLine("[ok] main TID=" + tid); });

        Step("OpenThread", () => {
            var h = OpenThread(0x1F03FF, false, tid);
            Console.WriteLine(h == IntPtr.Zero ? "[FAIL] OpenThread err=" + Marshal.GetLastWin32Error() : "[ok] OpenThread h=" + h.ToString("X"));
        });

        Step("Open+Read chain", () => {
            var h = OpenProcess(0x1F0FFF, false, wow.Id);
            var b = new byte[5]; int r;
            ReadProcessMemory(h, (IntPtr)0x4CAD851, b, 5, out r);
            Console.WriteLine("[ok] version=" + System.Text.Encoding.UTF8.GetString(b));
        });
        Console.WriteLine("DONE");
    }
}
