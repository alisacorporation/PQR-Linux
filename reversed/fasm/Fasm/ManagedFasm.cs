using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Fasm;

public class ManagedFasm : IDisposable
{
	private StringBuilder m_AssemblyString;

	private List<IntPtr> m_ThreadHandles;

	private IntPtr m_hProcess;

	private int m_MemorySize;

	private int m_PassLimit;

	public ManagedFasm(IntPtr hProcess)
	{
		m_hProcess = hProcess;
		m_AssemblyString = new StringBuilder("use32\n");
		m_ThreadHandles = new List<IntPtr>();
		m_MemorySize = 4096;
		m_PassLimit = 100;
	}

	public ManagedFasm()
	{
		m_AssemblyString = new StringBuilder("use32\n");
		m_ThreadHandles = new List<IntPtr>();
		m_MemorySize = 4096;
		m_PassLimit = 100;
	}

	private unsafe void _007EManagedFasm()
	{
		for (int i = 0; i < m_ThreadHandles.Count; i++)
		{
			global::_003CModule_003E.CloseHandle((void*)m_ThreadHandles[i].ToInt32());
		}
		m_ThreadHandles.Clear();
	}

	public void AddLine(string szFormatString, params object[] args)
	{
		m_AssemblyString.AppendFormat(szFormatString + "\n", args);
	}

	public void AddLine(string szLine)
	{
		m_AssemblyString.Append(szLine + "\n");
	}

	public void Add(string szFormatString, params object[] args)
	{
		m_AssemblyString.AppendFormat(szFormatString, args);
	}

	public void Add(string szLine)
	{
		m_AssemblyString.Append(szLine);
	}

	public void InsertLine(string szLine, int nIndex)
	{
		m_AssemblyString.Insert(nIndex, szLine + "\n");
	}

	public void Insert(string szLine, int nIndex)
	{
		m_AssemblyString.Insert(nIndex, szLine);
	}

	public void Clear()
	{
		m_AssemblyString = new StringBuilder("use32\n");
	}

	public unsafe static byte[] Assemble(string szSource, int nMemorySize, int nPassLimit)
	{
		byte[] array = null;
		IntPtr intPtr = default(IntPtr);
		intPtr = Marshal.StringToHGlobalAnsi(szSource);
		uint num = global::_003CModule_003E._c_FasmAssemble((sbyte*)intPtr.ToPointer(), (uint)nMemorySize, (uint)nPassLimit);
		_c_FasmState* c_fasm_memorybuf = (_c_FasmState*)global::_003CModule_003E._c_fasm_memorybuf;
		Marshal.FreeHGlobal(intPtr);
		if (*(int*)c_fasm_memorybuf == 0)
		{
			array = new byte[((int*)c_fasm_memorybuf)[1]];
			Marshal.Copy((IntPtr)(void*)(int)((uint*)c_fasm_memorybuf)[2], array, 0, ((int*)c_fasm_memorybuf)[1]);
			return array;
		}
		throw new Exception($"Assembly failed!  Error code: {((int*)c_fasm_memorybuf)[1]};  Error Line: {*(uint*)(((int*)c_fasm_memorybuf)[2] + 4)}");
	}

	public static byte[] Assemble(string szSource, int nMemorySize)
	{
		return Assemble(szSource, nMemorySize, 100);
	}

	public static byte[] Assemble(string szSource)
	{
		return Assemble(szSource, 4096, 100);
	}

	public byte[] Assemble()
	{
		return Assemble(m_AssemblyString.ToString(), m_MemorySize, m_PassLimit);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public bool Inject(uint dwAddress)
	{
		return Inject(m_hProcess, dwAddress);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public unsafe bool Inject(IntPtr hProcess, uint dwAddress)
	{
		Exception ex = null;
		if (hProcess == IntPtr.Zero)
		{
			return false;
		}
		if (m_AssemblyString.ToString().Contains("use64") || m_AssemblyString.ToString().Contains("use16"))
		{
			m_AssemblyString.Replace("use32\n", "");
		}
		if (!m_AssemblyString.ToString().Contains("org "))
		{
			m_AssemblyString.Insert(0, $"org 0x{dwAddress:X08}\n");
		}
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			bool result;
			try
			{
				intPtr = Marshal.StringToHGlobalAnsi(m_AssemblyString.ToString());
				global::_003CModule_003E._c_FasmAssemble((sbyte*)intPtr.ToPointer(), (uint)m_MemorySize, (uint)m_PassLimit);
			}
			catch (Exception ex2)
			{
				Console.WriteLine(ex2.Message);
				result = false;
				goto IL_00d6;
			}
			goto end_IL_0095;
			IL_00d6:
			return result;
			end_IL_0095:;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
		_c_FasmState* c_fasm_memorybuf = (_c_FasmState*)global::_003CModule_003E._c_fasm_memorybuf;
		if (*(int*)c_fasm_memorybuf != 0)
		{
			throw new Exception($"Assembly failed!  Error code: {((int*)c_fasm_memorybuf)[1]};  Error Line: {*(uint*)(((int*)c_fasm_memorybuf)[2] + 4)}");
		}
		return (global::_003CModule_003E.WriteProcessMemory((void*)hProcess, (void*)(int)dwAddress, (void*)(int)((uint*)c_fasm_memorybuf)[2], ((uint*)c_fasm_memorybuf)[1], null) != 0) ? true : false;
	}

	public uint InjectAndExecute(uint dwAddress)
	{
		return InjectAndExecute(m_hProcess, dwAddress, 0u);
	}

	public uint InjectAndExecute(IntPtr hProcess, uint dwAddress)
	{
		return InjectAndExecute(hProcess, dwAddress, 0u);
	}

	public unsafe uint InjectAndExecute(IntPtr hProcess, uint dwAddress, uint dwParameter)
	{
		if (hProcess == IntPtr.Zero)
		{
			throw new ArgumentNullException("hProcess");
		}
		if (dwAddress == 0)
		{
			throw new ArgumentNullException("dwAddress");
		}
		uint result = 0u;
		if (!Inject(hProcess, dwAddress))
		{
			throw new Exception("Injection failed for some reason.");
		}
		void* ptr = global::_003CModule_003E.CreateRemoteThread((void*)hProcess.ToInt32(), null, 0u, (delegate* unmanaged[Stdcall, Stdcall]<void*, uint>)(int)dwAddress, (void*)(int)dwParameter, 0u, null);
		if (ptr == null)
		{
			throw new Exception("Remote thread failed.");
		}
		try
		{
			if (global::_003CModule_003E.WaitForSingleObject(ptr, 10000u) == 0 && global::_003CModule_003E.GetExitCodeThread(ptr, &result) == 0)
			{
				throw new Exception("Could not get thread exit code.");
			}
		}
		finally
		{
			global::_003CModule_003E.CloseHandle(ptr);
		}
		return result;
	}

	public IntPtr InjectAndExecuteEx(uint dwAddress)
	{
		return InjectAndExecuteEx(m_hProcess, dwAddress, 0u);
	}

	public IntPtr InjectAndExecuteEx(IntPtr hProcess, uint dwAddress)
	{
		return InjectAndExecuteEx(hProcess, dwAddress, 0u);
	}

	public unsafe IntPtr InjectAndExecuteEx(IntPtr hProcess, uint dwAddress, uint dwParameter)
	{
		Inject(hProcess, dwAddress);
		void* ptr = global::_003CModule_003E.CreateRemoteThread((void*)hProcess.ToInt32(), null, 0u, (delegate* unmanaged[Stdcall, Stdcall]<void*, uint>)(int)dwAddress, (void*)(int)dwParameter, 0u, null);
		IntPtr item = (IntPtr)ptr;
		m_ThreadHandles.Add(item);
		return (IntPtr)ptr;
	}

	public IntPtr GetProcessHandle()
	{
		return m_hProcess;
	}

	public void SetProcessHandle(IntPtr Value)
	{
		m_hProcess = Value;
	}

	public int GetMemorySize()
	{
		return m_MemorySize;
	}

	public void SetMemorySize(int Value)
	{
		m_MemorySize = Value;
	}

	public int GetPassLimit()
	{
		return m_PassLimit;
	}

	public void SetPassLimit(int Value)
	{
		m_PassLimit = Value;
	}

	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			_007EManagedFasm();
		}
		else
		{
			base.Finalize();
		}
	}

	public virtual sealed void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
