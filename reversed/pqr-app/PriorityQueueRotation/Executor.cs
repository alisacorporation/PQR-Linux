using System;
using System.Collections.Generic;
using System.Threading;
using Fasm;
using Magic;

namespace PriorityQueueRotation;

public class Executor
{
	private BlackMagic BlackMagic = new BlackMagic();

	private uint codeCavePtr;

	private uint DetourPtr;

	private uint dwAddress;

	private byte[] OverwrittenBytes;

	private Random random = new Random();

	private static string[] RandomAsmCode = new string[31]
	{
		"mov eax, eax", "mov ecx, ecx", "mov ebp, ebp", "mov edx, edx", "mov ebx, ebx", "mov esp, esp", "mov esi, esi", "mov edi, edi", "nop", "push ebp|pop ebp",
		"push eax|pop eax", "push ecx|pop ecx", "push edx|pop edx", "push ebx|pop ebx", "push esp|pop esp", "push edi|pop edi", "xchg eax, eax", "xchg ebp, ebp", "xchg ecx, ecx", "xchg edx, edx",
		"xchg ebx, ebx", "xchg esp, esp", "xchg edi, edi", "xchg eax, ebp|xchg ebp, eax", "xchg ecx, ebp|xchg ebp, ecx", "xchg eax, edx|xchg edx, eax", "xchg eax, ebx|xchg ebx, eax", "xchg eax, edi|xchg edi, eax", "xchg edi, edx|xchg edx, edi", "xchg ecx, ebx|xchg ebx, ecx",
		"xchg ebp, edi|xchg edi, ebp"
	};

	public bool IsApplied { get; private set; }

	public Executor(int ProcessId, uint dwAddress, byte[] OverwrittenBytes)
	{
		BlackMagic.OpenProcessAndThread(ProcessId);
		BlackMagic.SetDebugPrivileges = true;
		codeCavePtr = BlackMagic.AllocateMemory(4);
		BlackMagic.WriteUInt(codeCavePtr, 0u);
		DetourPtr = BlackMagic.AllocateMemory(598);
		this.dwAddress = dwAddress;
		this.OverwrittenBytes = OverwrittenBytes;
	}

	public void Apply()
	{
		if (IsApplied)
		{
			Restore();
		}
		new ManagedFasm(BlackMagic.ProcessHandle);
		string[] aSM = new string[13]
		{
			"pushfd",
			"pushad",
			"mov eax, [" + codeCavePtr + "]",
			"cmp eax, 0",
			"je @out",
			"call eax",
			"mov eax, " + codeCavePtr,
			"xor edx, edx",
			"mov [eax], edx",
			"@out:",
			"popad",
			"popfd",
			"jmp " + (dwAddress + (uint)OverwrittenBytes.Length)
		};
		aSM = RandomizeASM(aSM);
		BlackMagic.WriteBytes(DetourPtr, OverwrittenBytes);
		Inject(aSM, DetourPtr + (uint)OverwrittenBytes.Length);
		string[] aSM2 = new string[1] { "jmp " + DetourPtr };
		Inject(aSM2, dwAddress);
		IsApplied = true;
	}

	private void Inject(string[] ASM, uint Address)
	{
		ManagedFasm managedFasm = new ManagedFasm(BlackMagic.ProcessHandle);
		managedFasm.SetMemorySize(16534);
		foreach (string szLine in ASM)
		{
			managedFasm.AddLine(szLine);
		}
		try
		{
			managedFasm.Inject(Address);
		}
		catch
		{
		}
	}

	public bool InjectAndExecute(string[] ASM, string Details)
	{
		lock (this)
		{
			try
			{
				ASM = RandomizeASM(ASM);
				uint num = BlackMagic.AllocateMemory(16534);
				Inject(ASM, num);
				BlackMagic.WriteUInt(codeCavePtr, num);
				int tickCount = Environment.TickCount;
				while (BlackMagic.ReadInt(codeCavePtr) != 0)
				{
					if (tickCount + 3000 < Environment.TickCount)
					{
						return false;
					}
					Thread.Sleep(10);
				}
				BlackMagic.FreeMemory(num);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public void Restore()
	{
		if (IsApplied)
		{
			BlackMagic.WriteBytes(dwAddress, OverwrittenBytes);
			IsApplied = false;
		}
	}

	internal static string[] RandomizeASM(string[] ASM)
	{
		Random random = new Random();
		List<string> list = new List<string>();
		foreach (string item in ASM)
		{
			for (int j = 0; j < random.Next(1, 5); j++)
			{
				string text = RandomAsmCode[random.Next(0, RandomAsmCode.Length - 1)];
				if (text.Contains("|"))
				{
					string[] array = text.Split(new char[1] { '|' });
					foreach (string text2 in array)
					{
						if (text2.Length > 0)
						{
							list.Add(text2);
						}
					}
				}
				else
				{
					list.Add(text);
				}
			}
			list.Add(item);
		}
		return list.ToArray();
	}
}
