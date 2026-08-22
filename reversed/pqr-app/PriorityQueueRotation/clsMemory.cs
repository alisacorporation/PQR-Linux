using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Magic;

namespace PriorityQueueRotation;

internal class clsMemory
{
	public static BlackMagic Memory;

	public static Executor executor;

	public static bool attachedWoW;

	public static IntPtr hWnd;

	private static string Name;

	private static string UnitBuffID;

	private static string UnitDebuffID;

	public static string ReadUTF8String(uint dwAddress, int Size)
	{
		byte[] array = Memory.ReadBytes(dwAddress, Size);
		if (array == null)
		{
			return string.Empty;
		}
		return StringFromBytes(array);
	}

	public static string StringFromBytes(byte[] myBuffer)
	{
		Encoding uTF = Encoding.UTF8;
		string text = uTF.GetString(myBuffer, 0, myBuffer.Length);
		if (text.IndexOf("\0") != -1)
		{
			text = text.Remove(text.IndexOf("\0"), text.Length - text.IndexOf("\0"));
		}
		return text;
	}

	public static List<string> WoWProcessesOld()
	{
		List<string> list = new List<string>();
		Process[] processesByName = Process.GetProcessesByName("Wow");
		if (processesByName.Length < 0)
		{
			list.Clear();
		}
		else
		{
			int num = processesByName.Length - 1;
			for (int i = 0; i <= num; i++)
			{
				int id = processesByName[i].Id;
				try
				{
					Memory.OpenProcessAndThread(id);
					string value = ReadUTF8String(BaseAddress() + clsOffsets.wowVersion, 5);
					if (Convert.ToUInt32(value) == clsOffsets.CurrentWoWVersion)
					{
						string text = ReadUTF8String(BaseAddress() + clsOffsets.PlayerName, 30);
						if (text != "")
						{
							list.Add(text + " (" + id + ")");
						}
					}
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public static List<string> WoWProcesses()
	{
		List<string> list = new List<string>();
		Process[] processesByName = Process.GetProcessesByName("Wow");
		string[,] offsetsArray = frmSelect.OffsetsArray;
		if (processesByName.Length < 0)
		{
			list.Clear();
		}
		else
		{
			int num = processesByName.Length - 1;
			for (int i = 0; i <= num; i++)
			{
				int id = processesByName[i].Id;
				try
				{
					Memory.OpenProcessAndThread(id);
					for (int j = 0; j < 1024; j++)
					{
						if (offsetsArray[j, 0] != null)
						{
							string text = ReadUTF8String(BaseAddress() + uint.Parse(offsetsArray[j, 2].Replace("0x", "").Trim(), NumberStyles.HexNumber), 5);
							if (text == offsetsArray[j, 1])
							{
								string text2 = ReadUTF8String(BaseAddress() + uint.Parse(offsetsArray[j, 3].Replace("0x", "").Trim(), NumberStyles.HexNumber), 30);
								if (text2 != "")
								{
									list.Add(text2 + " (" + id + ") (" + text + ")");
									break;
								}
							}
						}
						if (offsetsArray[j, 0] == null)
						{
							break;
						}
					}
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public static uint BaseAddress()
	{
		return (uint)(int)Memory.MainModule.BaseAddress;
	}

	public static string GetPlayerName()
	{
		return ReadUTF8String(BaseAddress() + clsOffsets.PlayerName, 30);
	}

	public static bool Initialize(int ProcessID)
	{
		try
		{
			Memory.OpenProcessAndThread(ProcessID);
			Memory.SetDebugPrivileges = true;
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool ApplyExecutor()
	{
		try
		{
			Memory.SuspendThread();
			executor = new Executor(Memory.ProcessId, (uint)((int)BaseAddress() + clsOffsets.Detour), clsOffsets.OverWritten);
			executor.Apply();
			Memory.ResumeThread();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool RestoreExecutor()
	{
		if (executor.IsApplied)
		{
			try
			{
				executor.Restore();
				return true;
			}
			catch
			{
				return false;
			}
		}
		return true;
	}

	public static string Lua_GetReturnValue(string Command)
	{
		return Lua_GetReturnValue(ReplacePQR(Command), string.Empty);
	}

	public static string Lua_GetReturnValue(string Command, string Argument)
	{
		return Lua_GetReturnValue(Command, Argument, 255);
	}

	public static string Lua_GetReturnValue(string Command, string Argument, int ReturnLength)
	{
		if (Argument.Length == 0)
		{
			Argument = "nil";
		}
		byte[] bytes = Encoding.UTF8.GetBytes(Command);
		byte[] bytes2 = Encoding.UTF8.GetBytes(Argument);
		uint num = Memory.AllocateMemory(bytes.Length + 1);
		uint num2 = Memory.AllocateMemory(bytes2.Length + 1);
		uint num3 = Memory.AllocateMemory(4);
		Memory.WriteBytes(num, bytes);
		Memory.WriteBytes(num2, bytes2);
		string[] aSM = new string[18]
		{
			"mov eax, " + num,
			"push 0",
			"push eax",
			"push eax",
			"mov eax, " + (uint)((int)BaseAddress() + clsOffsets.Lua_DoStringAddress),
			"call eax",
			"add esp, 0xC",
			"call " + (uint)((int)BaseAddress() + clsOffsets.ClntObjMgrGetActivePlayerObjAddress),
			"test eax, eax",
			"je @out",
			"mov ecx, eax",
			"push -1",
			"mov edx, " + num2,
			"push edx",
			"call " + (uint)((int)BaseAddress() + clsOffsets.Lua_GetLocalizedTextAddress),
			"mov [" + num3 + "], eax",
			"@out:",
			"retn"
		};
		executor.InjectAndExecute(aSM, "Lua_GetReturnValue");
		uint num4 = Memory.ReadUInt(num3);
		Memory.FreeMemory(num);
		Memory.FreeMemory(num2);
		Memory.FreeMemory(num3);
		if (num4 != 0)
		{
			return ReadUTF8String(num4, 2000);
		}
		return string.Empty;
	}

	public static void WriteToChat(string strChat)
	{
		if (IsWoWReady())
		{
			string text = string.Concat(new string[3] { "DEFAULT_CHAT_FRAME:AddMessage(\"", strChat, "\")" });
			Lua_GetReturnValue("if UnitName ~= nil then " + text + " end");
		}
	}

	public static string GetPlayerClass()
	{
		byte[] array = Memory.ReadBytes(BaseAddress() + clsOffsets.PlayerClass, 2);
		return (uint)array[0] switch
		{
			1u => "WARRIOR", 
			2u => "PALADIN", 
			3u => "HUNTER", 
			4u => "ROGUE", 
			5u => "PRIEST", 
			6u => "DEATHKNIGHT", 
			7u => "SHAMAN", 
			8u => "MAGE", 
			9u => "WARLOCK", 
			11u => "DRUID", 
			_ => "", 
		};
	}

	public static bool WorldHasFocus()
	{
		uint num;
		try
		{
			num = Memory.ReadUInt(BaseAddress() + clsOffsets.KeyboardFocus);
		}
		catch
		{
			return false;
		}
		if (num == 0)
		{
			return true;
		}
		return false;
	}

	public static bool IsAtCharacterSelect()
	{
		if (clsOffsets.GameState == 0)
		{
			return false;
		}
		try
		{
			int num = Memory.ReadInt(BaseAddress() + clsOffsets.GameState);
			if (clsOffsets.CurrentWoWVersion <= 12340 && num != 0)
			{
				num = 1;
			}
			if (num == 0)
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsWoWReady()
	{
		if (clsOffsets.GameState == 0)
		{
			return true;
		}
		try
		{
			int num = Memory.ReadInt(BaseAddress() + clsOffsets.GameState);
			if (clsOffsets.CurrentWoWVersion <= 12340 && num != 0)
			{
				num = 1;
			}
			if (num == 1)
			{
				if (GlobalSettings.RestoreDelay > 0)
				{
					return false;
				}
				return true;
			}
			GlobalSettings.RestoreDelay = 1;
			return false;
		}
		catch
		{
			GlobalSettings.RestoreDelay = 1;
			frmMain.AddToDebug("Checking GameState... State Bad, Set 5second restore delay.");
			return false;
		}
	}

	static clsMemory()
	{
		Memory = new BlackMagic();
		attachedWoW = false;
		hWnd = IntPtr.Zero;
	}

	public static string ReplacePQR(string str)
	{
		return Regex.Replace(Regex.Replace(Regex.Replace(str, "UnitDebuffID", GetUDBID()), "UnitBuffID", GetUBID()), "pqr", GetName(), RegexOptions.IgnoreCase);
	}

	public static string GetName()
	{
		if (string.IsNullOrEmpty(Name))
		{
			string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] array = new char[8];
			Random random = new Random();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = text[random.Next(text.Length)];
			}
			Name = new string(array);
		}
		return Name;
	}

	public static string GetUBID()
	{
		if (string.IsNullOrEmpty(UnitBuffID))
		{
			string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] array = new char[7];
			Random random = new Random();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = text[random.Next(text.Length)];
			}
			UnitBuffID = new string(array);
		}
		return UnitBuffID;
	}

	public static string GetUDBID()
	{
		if (string.IsNullOrEmpty(UnitDebuffID))
		{
			string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			char[] array = new char[6];
			Random random = new Random();
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = text[random.Next(text.Length)];
			}
			UnitDebuffID = new string(array);
		}
		return UnitDebuffID;
	}
}
