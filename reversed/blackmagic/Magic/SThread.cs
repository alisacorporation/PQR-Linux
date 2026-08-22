using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Magic;

public static class SThread
{
	public static IntPtr OpenThread(uint dwDesiredAccess, int dwThreadId)
	{
		return Imports.OpenThread(dwDesiredAccess, bInheritHandle: false, (uint)dwThreadId);
	}

	public static IntPtr OpenThread(int dwThreadId)
	{
		return Imports.OpenThread(2032639u, bInheritHandle: false, (uint)dwThreadId);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct THREADENTRY32
	{
		public uint dwSize;
		public uint cntUsage;
		public int th32ThreadID;
		public int th32OwnerProcessID;
		public int tpBasePri;
		public int tpDeltaPri;
		public uint dwFlags;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, int th32ProcessID);

	[DllImport("kernel32.dll")]
	private static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

	[DllImport("kernel32.dll")]
	private static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr hObject);

	public static int GetMainThreadId(int dwProcessId)
	{
		if (dwProcessId == 0)
		{
			return 0;
		}
		int num = GetFirstThreadIdViaToolhelp(dwProcessId);
		if (num != 0)
		{
			return num;
		}
		try
		{
			return Process.GetProcessById(dwProcessId).Threads[0].Id;
		}
		catch
		{
			return 0;
		}
	}

	private static int GetFirstThreadIdViaToolhelp(int dwProcessId)
	{
		IntPtr intPtr = CreateToolhelp32Snapshot(4u, 0);
		if (intPtr == IntPtr.Zero || intPtr == new IntPtr(-1))
		{
			return 0;
		}
		int result = 0;
		THREADENTRY32 tHREADENTRY = default(THREADENTRY32);
		tHREADENTRY.dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32));
		try
		{
			if (Thread32First(intPtr, ref tHREADENTRY))
			{
				do
				{
					if (tHREADENTRY.th32OwnerProcessID == dwProcessId && (result == 0 || tHREADENTRY.th32ThreadID < result))
					{
						result = tHREADENTRY.th32ThreadID;
					}
				}
				while (Thread32Next(intPtr, ref tHREADENTRY));
			}
			return result;
		}
		finally
		{
			CloseHandle(intPtr);
		}
	}

	public static int GetMainThreadId(IntPtr hWindowHandle)
	{
		if (hWindowHandle == IntPtr.Zero)
		{
			return 0;
		}
		return GetMainThreadId(SProcess.GetProcessFromWindow(hWindowHandle));
	}

	public static ProcessThread GetMainThread(int dwProcessId)
	{
		if (dwProcessId == 0)
		{
			return null;
		}
		Process processById = Process.GetProcessById(dwProcessId);
		return processById.Threads[0];
	}

	public static ProcessThread GetMainThread(IntPtr hWindowHandle)
	{
		if (hWindowHandle == IntPtr.Zero)
		{
			return null;
		}
		return GetMainThread(SProcess.GetProcessFromWindow(hWindowHandle));
	}

	public static CONTEXT GetThreadContext(IntPtr hThread, uint ContextFlags)
	{
		CONTEXT lpContext = new CONTEXT
		{
			ContextFlags = ContextFlags
		};
		if (!Imports.GetThreadContext(hThread, ref lpContext))
		{
			lpContext.ContextFlags = 0u;
		}
		return lpContext;
	}

	public static bool SetThreadContext(IntPtr hThread, CONTEXT ctx)
	{
		return Imports.SetThreadContext(hThread, ref ctx);
	}

	public static uint SuspendThread(IntPtr hThread)
	{
		return Imports.SuspendThread(hThread);
	}

	public static uint ResumeThread(IntPtr hThread)
	{
		return Imports.ResumeThread(hThread);
	}

	public static uint TerminateThread(IntPtr hThread, uint dwExitCode)
	{
		return Imports.TerminateThread(hThread, dwExitCode);
	}

	public static IntPtr CreateRemoteThread(IntPtr hProcess, uint dwStartAddress, uint dwParameter)
	{
		uint dwThreadId;
		return CreateRemoteThread(hProcess, dwStartAddress, dwParameter, 0u, out dwThreadId);
	}

	public static IntPtr CreateRemoteThread(IntPtr hProcess, uint dwStartAddress, uint dwParameter, out uint dwThreadId)
	{
		return CreateRemoteThread(hProcess, dwStartAddress, dwParameter, 0u, out dwThreadId);
	}

	public static IntPtr CreateRemoteThread(IntPtr hProcess, uint dwStartAddress, uint dwParameter, uint dwCreationFlags, out uint dwThreadId)
	{
		IntPtr dwThreadId2;
		IntPtr result = Imports.CreateRemoteThread(hProcess, IntPtr.Zero, 0u, (IntPtr)dwStartAddress, (IntPtr)dwParameter, dwCreationFlags, out dwThreadId2);
		dwThreadId = (uint)(int)dwThreadId2;
		return result;
	}

	public static uint GetExitCodeThread(IntPtr hThread)
	{
		if (!Imports.GetExitCodeThread(hThread, out var lpExitCode))
		{
			throw new Exception("GetExitCodeThread failed.");
		}
		return (uint)lpExitCode;
	}

	public static uint WaitForSingleObject(IntPtr hObject)
	{
		return Imports.WaitForSingleObject(hObject, uint.MaxValue);
	}

	public static uint WaitForSingleObject(IntPtr hObject, uint dwMilliseconds)
	{
		return Imports.WaitForSingleObject(hObject, dwMilliseconds);
	}
}
