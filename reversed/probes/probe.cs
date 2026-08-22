using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Probe
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out int read);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x1F0FFF;
    const uint VERSION_OFFSET = 0x8AD851; // Offsets_12340.xml WoWVersionOffset

    static void Main()
    {
        Console.WriteLine("CLR: " + Environment.Version);
        var procs = Process.GetProcesses();
        Console.WriteLine("Total processes visible: " + procs.Length);
        foreach (var p in procs)
        {
            string name = "<err>";
            try { name = p.ProcessName; } catch (Exception e) { name = "<throw:" + e.GetType().Name + ">"; }
            Console.WriteLine("  PID=" + p.Id + " Name=[" + name + "]");
        }

        Process wow = null;
        foreach (var p in Process.GetProcesses())
        {
            try { if (p.ProcessName.ToLower().Contains("wow")) { wow = p; break; } }
            catch {}
        }
        if (wow == null)
        {
            Console.WriteLine("RESULT: no process with 'wow' in name found");
            return;
        }
        Console.WriteLine("Candidate: PID " + wow.Id);

        IntPtr h = OpenProcess(ACCESS, false, wow.Id);
        if (h == IntPtr.Zero)
        {
            Console.WriteLine("OpenProcess FAILED err=" + Marshal.GetLastWin32Error());
            return;
        }
        Console.WriteLine("OpenProcess OK");

        // read version dword at 0x400000 + VERSION_OFFSET - 0x400000? No:
        // offsets are absolute VAs. 3.3.5a imagebase = 0x00400000.
        IntPtr va = new IntPtr(0x8AD851);
        byte[] buf = new byte[4];
        int read;
        if (ReadProcessMemory(h, va, buf, 4, out read) && read == 4)
        {
            uint ver = BitConverter.ToUInt32(buf, 0);
            Console.WriteLine("Version @0x8AD851 = " + ver + (ver == 12340 ? "  (MATCHES 12340)" : "  (unexpected)"));
        }
        else
        {
            Console.WriteLine("ReadProcessMemory FAILED err=" + Marshal.GetLastWin32Error());
        }

        // also try MainModule like PQR might
        try
        {
            Console.WriteLine("MainModule.FileName = " + wow.MainModule.FileName);
        }
        catch (Exception e)
        {
            Console.WriteLine("MainModule THREW: " + e.GetType().Name + ": " + e.Message);
        }
        CloseHandle(h);
        Console.WriteLine("DONE");
    }
}
