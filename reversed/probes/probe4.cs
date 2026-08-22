using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

class Probe4
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint a,bool b,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out int read);

    static void Main()
    {
        Process wow=null;
        foreach(var p in Process.GetProcesses()) try{ if(p.ProcessName.ToLower().Contains("wow")){wow=p;break;} }catch{}
        if(wow==null){Console.WriteLine("no wow running");return;}
        IntPtr h=OpenProcess(0x1F0FFF,false,wow.Id);
        uint BASE=0x400000;
        Dump(h,BASE+0x8AD851,5,"VersionStr @base+0x8AD851 (expect \"12340\")");
        Dump(h,BASE+0x879D18,30,"PlayerName @base+0x879D18");
        Dump(h,BASE+0x7D078A,4,"GameState @base+0x7D078A");
        Dump(h,BASE+0x419210,9,"DoString prologue @base+0x419210 (expect 55 8B EC 81 EC F8 00 00 00)");
        Console.WriteLine("DONE");
    }
    static void Dump(IntPtr h,long addr,int len,string label)
    {
        byte[] b=new byte[len];int r;
        var hex=new StringBuilder();var asc=new StringBuilder();
        if(!ReadProcessMemory(h,new IntPtr(addr),b,len,out r)){Console.WriteLine(label+" READFAIL "+Marshal.GetLastWin32Error());return;}
        foreach(byte x in b){hex.Append(x.ToString("X2")+" ");asc.Append(x>=0x20&&x<0x7F?(char)x:'.');}
        Console.WriteLine(label+"\n  HEX: "+hex+"\n  ASC: "+asc);
    }
}
