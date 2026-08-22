using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PriorityQueueRotation.Properties;

namespace PriorityQueueRotation;

internal class GlobalSettings
{
	public static int CurrentProcessID = 0;

	public static string CurrentPlayerName = "";

	public static string CurrentPlayerClass = "";

	public static string CurrentLoadedClass = "";

	public static bool AppliedExecutor = false;

	public static bool EnableButtons = false;

	public static bool DisableButtons = false;

	public static string PQR_SmartHotkeyCommand = "PQR_SmartHotkey";

	public static bool SmartHotkeyMode = false;

	public static bool AllowRotationDesignation = true;

	public static bool RequireCombat = true;

	public static bool StartingBot = false;

	public static bool NotifyUser = false;

	public static bool BotRunning = false;

	public static bool InterruptMode = false;

	public static int RestoreDelay = 0;

	public static float RefreshRate = 100f;

	public static List<string> spellList = new List<string>();

	public static List<bool> spellEnabled = new List<bool>();

	public static bool AbilityEditorLoaded = false;

	public static bool RotationEditorLoaded = false;

	public static bool HotkeyEditorLoaded = false;

	public static int EditingHotkey = -1;

	public static string CurrentRunningProfile = "";

	public static bool ChangeRotation = false;

	public static bool ChangeInterrupt = false;

	public static bool ChangeFrequency = true;

	public static bool ShowMessages = true;

	public static bool LoadBot = false;

	public static double InterruptDelay = 0.0;

	public static string Sound_StartRotation = "";

	public static string Sound_ChangeRotation = "";

	public static string Sound_StopRotation = "";

	public static string Sound_StartInterrupt = "";

	public static string Sound_StopInterrupt = "";

	public static bool DebugMode = false;

	public static string[] DebugArray = new string[1024];

	public static bool InterruptAll = false;

	public static Dictionary<Keys, string> myKeys = new Dictionary<Keys, string>();

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32", SetLastError = true)]
	public static extern int GetWindowThreadProcessId(int hwnd, ref int lProcessId);

	public static string Rotation1(string strClass, string Set)
	{
		switch (strClass)
		{
		case "DEATHKNIGHT":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DEATHKNIGHTRotation1 = Set;
			}
			return Settings.Default.DEATHKNIGHTRotation1;
		case "DRUID":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DRUIDRotation1 = Set;
			}
			return Settings.Default.DRUIDRotation1;
		case "HUNTER":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.HUNTERRotation1 = Set;
			}
			return Settings.Default.HUNTERRotation1;
		case "MAGE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.MAGERotation1 = Set;
			}
			return Settings.Default.MAGERotation1;
		case "PALADIN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PALADINRotation1 = Set;
			}
			return Settings.Default.PALADINRotation1;
		case "PRIEST":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PRIESTRotation1 = Set;
			}
			return Settings.Default.PRIESTRotation1;
		case "ROGUE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.ROGUERotation1 = Set;
			}
			return Settings.Default.ROGUERotation1;
		case "SHAMAN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.SHAMANRotation1 = Set;
			}
			return Settings.Default.SHAMANRotation1;
		case "WARLOCK":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation1 = Set;
			}
			return Settings.Default.WARLOCKRotation1;
		case "WARRIOR":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation1 = Set;
			}
			return Settings.Default.WARLOCKRotation1;
		default:
			return "";
		}
	}

	public static string Rotation2(string strClass, string Set)
	{
		switch (strClass)
		{
		case "DEATHKNIGHT":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DEATHKNIGHTRotation2 = Set;
			}
			return Settings.Default.DEATHKNIGHTRotation2;
		case "DRUID":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DRUIDRotation2 = Set;
			}
			return Settings.Default.DRUIDRotation2;
		case "HUNTER":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.HUNTERRotation2 = Set;
			}
			return Settings.Default.HUNTERRotation2;
		case "MAGE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.MAGERotation2 = Set;
			}
			return Settings.Default.MAGERotation2;
		case "PALADIN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PALADINRotation2 = Set;
			}
			return Settings.Default.PALADINRotation2;
		case "PRIEST":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PRIESTRotation2 = Set;
			}
			return Settings.Default.PRIESTRotation2;
		case "ROGUE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.ROGUERotation2 = Set;
			}
			return Settings.Default.ROGUERotation2;
		case "SHAMAN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.SHAMANRotation2 = Set;
			}
			return Settings.Default.SHAMANRotation2;
		case "WARLOCK":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation2 = Set;
			}
			return Settings.Default.WARLOCKRotation2;
		case "WARRIOR":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation2 = Set;
			}
			return Settings.Default.WARLOCKRotation2;
		default:
			return "";
		}
	}

	public static string Rotation3(string strClass, string Set)
	{
		switch (strClass)
		{
		case "DEATHKNIGHT":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DEATHKNIGHTRotation3 = Set;
			}
			return Settings.Default.DEATHKNIGHTRotation3;
		case "DRUID":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DRUIDRotation3 = Set;
			}
			return Settings.Default.DRUIDRotation3;
		case "HUNTER":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.HUNTERRotation3 = Set;
			}
			return Settings.Default.HUNTERRotation3;
		case "MAGE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.MAGERotation3 = Set;
			}
			return Settings.Default.MAGERotation3;
		case "PALADIN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PALADINRotation3 = Set;
			}
			return Settings.Default.PALADINRotation3;
		case "PRIEST":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PRIESTRotation3 = Set;
			}
			return Settings.Default.PRIESTRotation3;
		case "ROGUE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.ROGUERotation3 = Set;
			}
			return Settings.Default.ROGUERotation3;
		case "SHAMAN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.SHAMANRotation3 = Set;
			}
			return Settings.Default.SHAMANRotation3;
		case "WARLOCK":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation3 = Set;
			}
			return Settings.Default.WARLOCKRotation3;
		case "WARRIOR":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation3 = Set;
			}
			return Settings.Default.WARLOCKRotation3;
		default:
			return "";
		}
	}

	public static string Rotation4(string strClass, string Set)
	{
		switch (strClass)
		{
		case "DEATHKNIGHT":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DEATHKNIGHTRotation4 = Set;
			}
			return Settings.Default.DEATHKNIGHTRotation4;
		case "DRUID":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.DRUIDRotation4 = Set;
			}
			return Settings.Default.DRUIDRotation4;
		case "HUNTER":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.HUNTERRotation4 = Set;
			}
			return Settings.Default.HUNTERRotation4;
		case "MAGE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.MAGERotation4 = Set;
			}
			return Settings.Default.MAGERotation4;
		case "PALADIN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PALADINRotation4 = Set;
			}
			return Settings.Default.PALADINRotation4;
		case "PRIEST":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.PRIESTRotation4 = Set;
			}
			return Settings.Default.PRIESTRotation4;
		case "ROGUE":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.ROGUERotation4 = Set;
			}
			return Settings.Default.ROGUERotation4;
		case "SHAMAN":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.SHAMANRotation4 = Set;
			}
			return Settings.Default.SHAMANRotation4;
		case "WARLOCK":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation4 = Set;
			}
			return Settings.Default.WARLOCKRotation4;
		case "WARRIOR":
			if (Set.Replace(" ", "") != "")
			{
				Settings.Default.WARLOCKRotation4 = Set;
			}
			return Settings.Default.WARLOCKRotation4;
		default:
			return "";
		}
	}

	public static Keys GetKeyFromString(string strKey)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		SetupDictionary();
		if (myKeys.ContainsValue(strKey))
		{
			foreach (KeyValuePair<Keys, string> myKey in myKeys)
			{
				if (myKey.Value == strKey)
				{
					return myKey.Key;
				}
			}
			return (Keys)226;
		}
		return (Keys)226;
	}

	public static bool WoWHasFocus(int ProcessID)
	{
		try
		{
			IntPtr foregroundWindow = GetForegroundWindow();
			int lProcessId = 0;
			GetWindowThreadProcessId((int)foregroundWindow, ref lProcessId);
			if (ProcessID == lProcessId)
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

	public static string GetStringFromKey(Keys key)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		SetupDictionary();
		return myKeys[key];
	}

	public static void SetupDictionary()
	{
		myKeys.Clear();
		myKeys.Add((Keys)65, "A");
		myKeys.Add((Keys)66, "B");
		myKeys.Add((Keys)67, "C");
		myKeys.Add((Keys)68, "D");
		myKeys.Add((Keys)69, "E");
		myKeys.Add((Keys)70, "F");
		myKeys.Add((Keys)71, "G");
		myKeys.Add((Keys)72, "H");
		myKeys.Add((Keys)73, "I");
		myKeys.Add((Keys)74, "J");
		myKeys.Add((Keys)75, "K");
		myKeys.Add((Keys)76, "L");
		myKeys.Add((Keys)77, "M");
		myKeys.Add((Keys)78, "N");
		myKeys.Add((Keys)79, "O");
		myKeys.Add((Keys)80, "P");
		myKeys.Add((Keys)81, "Q");
		myKeys.Add((Keys)82, "R");
		myKeys.Add((Keys)83, "S");
		myKeys.Add((Keys)84, "T");
		myKeys.Add((Keys)85, "U");
		myKeys.Add((Keys)86, "V");
		myKeys.Add((Keys)87, "W");
		myKeys.Add((Keys)88, "X");
		myKeys.Add((Keys)89, "Y");
		myKeys.Add((Keys)90, "Z");
		myKeys.Add((Keys)192, "Tilde");
		myKeys.Add((Keys)48, "0");
		myKeys.Add((Keys)49, "1");
		myKeys.Add((Keys)50, "2");
		myKeys.Add((Keys)51, "3");
		myKeys.Add((Keys)52, "4");
		myKeys.Add((Keys)53, "5");
		myKeys.Add((Keys)54, "6");
		myKeys.Add((Keys)55, "7");
		myKeys.Add((Keys)56, "8");
		myKeys.Add((Keys)57, "9");
		myKeys.Add((Keys)112, "F1");
		myKeys.Add((Keys)113, "F2");
		myKeys.Add((Keys)114, "F3");
		myKeys.Add((Keys)115, "F4");
		myKeys.Add((Keys)116, "F5");
		myKeys.Add((Keys)117, "F6");
		myKeys.Add((Keys)118, "F7");
		myKeys.Add((Keys)119, "F8");
		myKeys.Add((Keys)120, "F9");
		myKeys.Add((Keys)121, "F10");
		myKeys.Add((Keys)122, "F11");
		myKeys.Add((Keys)123, "F12");
		myKeys.Add((Keys)189, "Minus (-)");
		myKeys.Add((Keys)187, "Equals (=)");
		myKeys.Add((Keys)219, "Open Bracket ([)");
		myKeys.Add((Keys)221, "Close Bracket (])");
		myKeys.Add((Keys)226, "Back Slash (\\)");
		myKeys.Add((Keys)186, "Semi-Colon (;)");
		myKeys.Add((Keys)222, "Apostrophe (')");
		myKeys.Add((Keys)188, "Comma (,)");
		myKeys.Add((Keys)190, "Period (.)");
		myKeys.Add((Keys)191, "Forward Slash (/)");
		myKeys.Add((Keys)96, "NumPad0");
		myKeys.Add((Keys)97, "NumPad1");
		myKeys.Add((Keys)98, "NumPad2");
		myKeys.Add((Keys)99, "NumPad3");
		myKeys.Add((Keys)100, "NumPad4");
		myKeys.Add((Keys)101, "NumPad5");
		myKeys.Add((Keys)102, "NumPad6");
		myKeys.Add((Keys)103, "NumPad7");
		myKeys.Add((Keys)104, "NumPad8");
		myKeys.Add((Keys)105, "NumPad9");
	}
}
