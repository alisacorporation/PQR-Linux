using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace PriorityQueueRotation;

internal class clsXML
{
	public static void SaveXML_Rotations(string sClass, string sProfile, string[,] rotationArray)
	{
		string text = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
		string text2 = "<" + sClass + ">";
		string text3 = "</" + sClass + ">";
		List<string> list = new List<string>();
		string text4 = "<Rotation><RotationName>%RNAME%</RotationName><RotationDefault>%RDEFAULT%</RotationDefault><RotationList>%RLIST%</RotationList><RequireCombat>%RCOMBAT%</RequireCombat><RotationNotes>%RNOTES%</RotationNotes></Rotation>";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] != null)
			{
				string newValue = XMLEncode(rotationArray[i, 0]);
				string newValue2 = XMLEncode(rotationArray[i, 1]);
				string newValue3 = XMLEncode(rotationArray[i, 2]);
				string newValue4 = XMLEncode(rotationArray[i, 3]);
				string newValue5 = XMLEncode(rotationArray[i, 4]);
				string text5 = text4.Replace("%RNAME%", newValue);
				text5 = text5.Replace("%RDEFAULT%", newValue2);
				text5 = text5.Replace("%RLIST%", newValue3);
				text5 = text5.Replace("%RCOMBAT%", newValue4);
				text5 = text5.Replace("%RNOTES%", newValue5);
				list.Add(text5);
			}
		}
		string text6 = "";
		text6 = text + text2;
		foreach (string item in list)
		{
			text6 += item;
		}
		text6 += text3;
		File.WriteAllText(Application.StartupPath + "\\Profiles\\" + sProfile + "_" + sClass + "_Rotations.xml", text6);
	}

	public static string[,] LoadXML_Rotations(string strClass)
	{
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected I4, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Invalid comparison between Unknown and I4
		if (strClass == null || strClass == "")
		{
			return new string[1024, 5];
		}
		FileStream fileStream = null;
		string text = "Profiles\\" + strClass + "_Rotations.xml";
		new XmlDocument();
		string[,] array = new string[1024, 5];
		if (File.Exists(text))
		{
			try
			{
				fileStream = new FileStream(text, FileMode.Open, FileAccess.Read);
				XmlTextReader val = new XmlTextReader((Stream)fileStream);
				string text2 = "";
				string text3 = "";
				string text4 = "";
				string input = "";
				string input2 = "true";
				string input3 = "";
				do
				{
					XmlNodeType nodeType = ((XmlReader)val).NodeType;
					switch ((int)nodeType - 1)
					{
					case 0:
						text2 = ((XmlReader)val).Name;
						continue;
					case 2:
						switch (text2)
						{
						case "RotationName":
							text3 = ((XmlReader)val).Value;
							break;
						case "RotationDefault":
							text4 = ((XmlReader)val).Value;
							break;
						case "RotationList":
							input = ((XmlReader)val).Value;
							break;
						case "RequireCombat":
							input2 = ((XmlReader)val).Value;
							break;
						case "RotationNotes":
							input3 = ((XmlReader)val).Value;
							break;
						}
						continue;
					case 1:
						continue;
					}
					if ((int)nodeType != 15 || !(((XmlReader)val).Name == "Rotation") || !(text3 != "") || !(text4 != ""))
					{
						continue;
					}
					for (int i = 0; i < 1024; i++)
					{
						if (array[i, 0] == null)
						{
							array[i, 0] = XMLDecode(text3);
							array[i, 1] = XMLDecode(text4);
							array[i, 2] = XMLDecode(input);
							array[i, 3] = XMLDecode(input2);
							array[i, 4] = XMLDecode(input3);
							text3 = "";
							input = "";
							text4 = "";
							input2 = "true";
							input3 = "";
							break;
						}
					}
				}
				while (((XmlReader)val).Read());
				fileStream.Close();
			}
			catch
			{
				MessageBox.Show("Unable to load a rotation/ability list. The XML is not well-formed.\r\n   " + text);
			}
		}
		return array;
	}

	public static string[,] LoadXML_AllRotations(string sClass)
	{
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected I4, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Invalid comparison between Unknown and I4
		if (sClass == null || sClass == "")
		{
			return new string[1024, 5];
		}
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		string[] files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Rotations.xml");
		FileStream fileStream = null;
		new XmlDocument();
		string[,] array = new string[1024, 5];
		string[] array2 = files;
		foreach (string text in array2)
		{
			string text2 = text.Substring(text.LastIndexOf("\\") + 1, text.Length - text.LastIndexOf("\\") - 1);
			text2 = text2.Substring(0, text2.IndexOf("_" + sClass.ToUpper()));
			if (!File.Exists(text))
			{
				continue;
			}
			try
			{
				fileStream = new FileStream(text, FileMode.Open, FileAccess.Read);
				XmlTextReader val = new XmlTextReader((Stream)fileStream);
				string text3 = "";
				string text4 = "";
				string text5 = "";
				string input = "";
				string input2 = "true";
				string input3 = "";
				do
				{
					XmlNodeType nodeType = ((XmlReader)val).NodeType;
					switch ((int)nodeType - 1)
					{
					case 0:
						text3 = ((XmlReader)val).Name;
						continue;
					case 2:
						switch (text3)
						{
						case "RotationName":
							text4 = ((XmlReader)val).Value;
							break;
						case "RotationDefault":
							text5 = ((XmlReader)val).Value;
							break;
						case "RotationList":
							input = ((XmlReader)val).Value;
							break;
						case "RequireCombat":
							input2 = ((XmlReader)val).Value;
							break;
						case "RotationNotes":
							input3 = ((XmlReader)val).Value;
							break;
						}
						continue;
					case 1:
						continue;
					}
					if ((int)nodeType != 15 || !(((XmlReader)val).Name == "Rotation") || !(text4 != "") || !(text5 != ""))
					{
						continue;
					}
					for (int j = 0; j < 1024; j++)
					{
						if (array[j, 0] == null)
						{
							array[j, 0] = XMLDecode(text4) + " (" + text2 + ")";
							array[j, 1] = XMLDecode(text5);
							array[j, 2] = XMLDecode(input);
							array[j, 2] = array[j, 2].Replace("|", " (" + text2 + ")|");
							array[j, 2] = array[j, 2] + " (" + text2 + ")";
							array[j, 3] = XMLDecode(input2);
							array[j, 4] = XMLDecode(input3);
							input2 = "true";
							input3 = "";
							text4 = "";
							input = "";
							text5 = "";
							break;
						}
					}
				}
				while (((XmlReader)val).Read());
				fileStream.Close();
			}
			catch
			{
				MessageBox.Show("Unable to load a rotation/ability list. The XML is not well-formed.\r\n   " + text);
			}
		}
		return array;
	}

	public static string[,] LoadXML_AllAbilities(string sClass)
	{
		//IL_047b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected I4, but got Unknown
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Invalid comparison between Unknown and I4
		if (sClass == null || sClass == "")
		{
			return new string[1024, 10];
		}
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		string[] files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Abilities.xml");
		FileStream fileStream = null;
		new XmlDocument();
		string[,] array = new string[1024, 10];
		string[] array2 = files;
		foreach (string text in array2)
		{
			string text2 = text.Substring(text.LastIndexOf("\\") + 1, text.Length - text.LastIndexOf("\\") - 1);
			text2 = text2.Substring(0, text2.IndexOf("_" + sClass.ToUpper()));
			if (!File.Exists(text))
			{
				continue;
			}
			try
			{
				fileStream = new FileStream(text, FileMode.Open, FileAccess.Read);
				XmlTextReader val = new XmlTextReader((Stream)fileStream);
				string text3 = "";
				string text4 = "";
				string text5 = "";
				string input = "";
				string text6 = "";
				string text7 = "";
				string input2 = "0";
				string input3 = "Target";
				string input4 = "False";
				string input5 = "";
				string input6 = "";
				do
				{
					XmlNodeType nodeType = ((XmlReader)val).NodeType;
					switch ((int)nodeType - 1)
					{
					case 0:
						text3 = ((XmlReader)val).Name;
						continue;
					case 2:
						switch (text3)
						{
						case "Name":
							text4 = ((XmlReader)val).Value;
							break;
						case "Default":
							text6 = ((XmlReader)val).Value;
							break;
						case "SpellID":
							text5 = ((XmlReader)val).Value;
							break;
						case "Actions":
							input = ((XmlReader)val).Value;
							break;
						case "Lua":
							text7 = ((XmlReader)val).Value;
							break;
						case "LuaBefore":
							input5 = ((XmlReader)val).Value;
							break;
						case "LuaAfter":
							input6 = ((XmlReader)val).Value;
							break;
						case "RecastDelay":
							input2 = ((XmlReader)val).Value;
							break;
						case "SelfCast":
							input3 = ((XmlReader)val).Value;
							input3 = ((!(input3 == "True")) ? "Target" : "Player");
							break;
						case "Target":
							input3 = ((XmlReader)val).Value;
							break;
						case "CancelChannel":
							input4 = ((XmlReader)val).Value;
							break;
						}
						continue;
					case 1:
						continue;
					}
					if ((int)nodeType != 15 || !(((XmlReader)val).Name == "Ability") || !(text4 != "") || !(text6 != "") || !(text5 != "") || !(text7 != ""))
					{
						continue;
					}
					for (int j = 0; j < 1024; j++)
					{
						if (array[j, 0] == null)
						{
							array[j, 0] = XMLDecode(text4) + " (" + text2 + ")";
							array[j, 1] = XMLDecode(text6);
							array[j, 2] = XMLDecode(text5);
							array[j, 3] = XMLDecode(input);
							array[j, 4] = XMLDecode(text7);
							array[j, 5] = XMLDecode(input2);
							array[j, 6] = XMLDecode(input3);
							array[j, 7] = XMLDecode(input4);
							array[j, 8] = XMLDecode(input5);
							array[j, 9] = XMLDecode(input6);
							text4 = "";
							text6 = "";
							text5 = "";
							input = "";
							text7 = "";
							input2 = "0";
							input3 = "Target";
							input4 = "False";
							input5 = "";
							input6 = "";
							break;
						}
					}
				}
				while (((XmlReader)val).Read());
				fileStream.Close();
			}
			catch
			{
				MessageBox.Show("Unable to load a rotation/ability list. The XML is not well-formed.\r\n   " + text);
			}
		}
		return array;
	}

	public static string[,] LoadXML_Abilities(string strClass)
	{
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected I4, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Invalid comparison between Unknown and I4
		if (strClass == null || strClass == "")
		{
			return new string[1024, 10];
		}
		FileStream fileStream = null;
		string text = "Profiles\\" + strClass + "_Abilities.xml";
		new XmlDocument();
		string[,] array = new string[1024, 10];
		if (File.Exists(text))
		{
			try
			{
				fileStream = new FileStream(text, FileMode.Open, FileAccess.Read);
				XmlTextReader val = new XmlTextReader((Stream)fileStream);
				string text2 = "";
				string text3 = "";
				string text4 = "";
				string input = "";
				string text5 = "";
				string text6 = "";
				string input2 = "0";
				string input3 = "Target";
				string input4 = "False";
				string input5 = "";
				string input6 = "";
				do
				{
					XmlNodeType nodeType = ((XmlReader)val).NodeType;
					switch ((int)nodeType - 1)
					{
					case 0:
						text2 = ((XmlReader)val).Name;
						continue;
					case 2:
						switch (text2)
						{
						case "Name":
							text3 = ((XmlReader)val).Value;
							break;
						case "Default":
							text5 = ((XmlReader)val).Value;
							break;
						case "SpellID":
							text4 = ((XmlReader)val).Value;
							break;
						case "Actions":
							input = ((XmlReader)val).Value;
							break;
						case "Lua":
							text6 = ((XmlReader)val).Value;
							break;
						case "LuaBefore":
							input5 = ((XmlReader)val).Value;
							break;
						case "LuaAfter":
							input6 = ((XmlReader)val).Value;
							break;
						case "RecastDelay":
							input2 = ((XmlReader)val).Value;
							break;
						case "SelfCast":
							input3 = ((XmlReader)val).Value;
							input3 = ((!(input3 == "True")) ? "Target" : "Player");
							break;
						case "Target":
							input3 = ((XmlReader)val).Value;
							break;
						case "CancelChannel":
							input4 = ((XmlReader)val).Value;
							break;
						}
						continue;
					case 1:
						continue;
					}
					if ((int)nodeType != 15 || !(((XmlReader)val).Name == "Ability") || !(text3 != "") || !(text5 != "") || !(text4 != "") || !(text6 != ""))
					{
						continue;
					}
					for (int i = 0; i < 1024; i++)
					{
						if (array[i, 0] == null)
						{
							array[i, 0] = XMLDecode(text3);
							array[i, 1] = XMLDecode(text5);
							array[i, 2] = XMLDecode(text4);
							array[i, 3] = XMLDecode(input);
							array[i, 4] = XMLDecode(text6);
							array[i, 5] = XMLDecode(input2);
							array[i, 6] = XMLDecode(input3);
							array[i, 7] = XMLDecode(input4);
							array[i, 8] = XMLDecode(input5);
							array[i, 9] = XMLDecode(input6);
							text3 = "";
							text5 = "";
							text4 = "";
							input = "";
							text6 = "";
							input2 = "0";
							input3 = "Target";
							input4 = "False";
							break;
						}
					}
				}
				while (((XmlReader)val).Read());
				fileStream.Close();
			}
			catch
			{
				MessageBox.Show("Unable to load a rotation/ability list. The XML is not well-formed.\r\n   " + text);
			}
		}
		return array;
	}

	public static void SaveXML_Abilities(string sClass, string sProfile, string[,] abilityArray)
	{
		string text = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
		string text2 = "<" + sClass + ">";
		string text3 = "</" + sClass + ">";
		List<string> list = new List<string>();
		string text4 = "<Ability><Name>%SPELLNAME%</Name><Default>%DEFAULT%</Default><SpellID>%SPELLID%</SpellID><Actions>%ACTIONS%</Actions><Lua>%LUA%</Lua><RecastDelay>%RECAST%</RecastDelay><Target>%TARGET%</Target><CancelChannel>%CANCELCHANNEL%</CancelChannel><LuaBefore>%LBEFORE%</LuaBefore><LuaAfter>%LAFTER%</LuaAfter></Ability>";
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] != null)
			{
				string newValue = XMLEncode(abilityArray[i, 0]);
				string newValue2 = XMLEncode(abilityArray[i, 1]);
				string newValue3 = XMLEncode(abilityArray[i, 2]);
				string newValue4 = XMLEncode(abilityArray[i, 3]);
				string newValue5 = XMLEncode(abilityArray[i, 4]);
				string newValue6 = XMLEncode(abilityArray[i, 5]);
				string newValue7 = XMLEncode(abilityArray[i, 6]);
				string newValue8 = XMLEncode(abilityArray[i, 7]);
				string newValue9 = XMLEncode(abilityArray[i, 8]);
				string newValue10 = XMLEncode(abilityArray[i, 9]);
				string text5 = text4.Replace("%SPELLNAME%", newValue);
				text5 = text5.Replace("%DEFAULT%", newValue2);
				text5 = text5.Replace("%SPELLID%", newValue3);
				text5 = text5.Replace("%ACTIONS%", newValue4);
				text5 = text5.Replace("%LUA%", newValue5);
				text5 = text5.Replace("%RECAST%", newValue6);
				text5 = text5.Replace("%TARGET%", newValue7);
				text5 = text5.Replace("%CANCELCHANNEL%", newValue8);
				text5 = text5.Replace("%LBEFORE%", newValue9);
				text5 = text5.Replace("%LAFTER%", newValue10);
				list.Add(text5);
			}
		}
		string text6 = "";
		text6 = text + text2;
		foreach (string item in list)
		{
			text6 += item;
		}
		text6 += text3;
		File.WriteAllText(Application.StartupPath + "\\Profiles\\" + sProfile + "_" + sClass + "_Abilities.xml", text6);
	}

	public static string XMLEncode(string input)
	{
		if (input == null)
		{
			return string.Empty;
		}
		string text = input.Replace("<", "&lt;");
		text = text.Replace(">", "&gt;");
		text = text.Replace("\"", "&quot;");
		text = text.Replace("'", "&apos;");
		return text.Replace("&", "&amp;");
	}

	public static string XMLDecode(string input)
	{
		if (input == null)
		{
			return string.Empty;
		}
		string text = input.Replace("&lt;", "<");
		text = text.Replace("&gt;", ">");
		text = text.Replace("&quot;", "\"");
		text = text.Replace("&apos;", "'");
		return text.Replace("&amp;", "&");
	}

	public static bool CreateProfile(string sClass, string sProfile)
	{
		if (sClass == null || sProfile == null || sClass == "" || sProfile == "")
		{
			return false;
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		char[] array = invalidFileNameChars;
		foreach (char value in array)
		{
			if (Enumerable.Contains(sProfile, value))
			{
				return false;
			}
		}
		if (sProfile.Contains("(") || sProfile.Contains(")"))
		{
			return false;
		}
		bool flag = true;
		bool flag2 = true;
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		string[] files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Rotations.xml");
		string[] array2 = files;
		foreach (string text in array2)
		{
			string text2 = text.Substring(text.LastIndexOf("\\") + 1, text.Length - text.LastIndexOf("\\") - 1);
			text2 = text2.Substring(0, text2.IndexOf("_" + sClass.ToUpper()));
			if (text2.ToUpper() == sProfile.ToUpper())
			{
				flag = false;
			}
		}
		files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Abilities.xml");
		string[] array3 = files;
		foreach (string text3 in array3)
		{
			string text4 = text3.Substring(text3.LastIndexOf("\\") + 1, text3.Length - text3.LastIndexOf("\\") - 1);
			text4 = text4.Substring(0, text4.IndexOf("_" + sClass.ToUpper()));
			if (text4.ToUpper() == sProfile.ToUpper())
			{
				flag2 = false;
			}
		}
		if (!flag && !flag2)
		{
			return false;
		}
		CreateEmptyXMLs(sClass, sProfile, flag, flag2);
		return true;
	}

	public static void CreateEmptyXMLs(string sClass, string sProfile, bool bRotation, bool bAbilities)
	{
		if (bAbilities)
		{
			string text = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
			string text2 = "<" + sClass + ">";
			string text3 = "</" + sClass + ">";
			string text4 = "";
			text4 = text + text2;
			text4 += text3;
			File.WriteAllText(Application.StartupPath + "\\Profiles\\" + sProfile + "_" + sClass + "_Abilities.xml", text4);
		}
		if (bRotation)
		{
			string text5 = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
			string text6 = "<" + sClass + ">";
			string text7 = "</" + sClass + ">";
			string text8 = "";
			text8 = text5 + text6;
			text8 += text7;
			File.WriteAllText(Application.StartupPath + "\\Profiles\\" + sProfile + "_" + sClass + "_Rotations.xml", text8);
		}
	}

	public static bool CopyProfile(string sClass, string sProfile, string sCopyTo)
	{
		bool flag = true;
		bool flag2 = true;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		char[] array = invalidFileNameChars;
		foreach (char value in array)
		{
			if (Enumerable.Contains(sCopyTo, value))
			{
				return false;
			}
		}
		if (sCopyTo.Contains("(") || sCopyTo.Contains(")"))
		{
			return false;
		}
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		string[] files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Rotations.xml");
		string[] array2 = files;
		foreach (string text in array2)
		{
			string text2 = text.Substring(text.LastIndexOf("\\") + 1, text.Length - text.LastIndexOf("\\") - 1);
			text2 = text2.Substring(0, text2.IndexOf("_" + sClass.ToUpper()));
			if (text2.ToUpper() == sCopyTo.ToUpper())
			{
				flag = false;
			}
		}
		files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Abilities.xml");
		string[] array3 = files;
		foreach (string text3 in array3)
		{
			string text4 = text3.Substring(text3.LastIndexOf("\\") + 1, text3.Length - text3.LastIndexOf("\\") - 1);
			text4 = text4.Substring(0, text4.IndexOf("_" + sClass.ToUpper()));
			if (text4.ToUpper() == sCopyTo.ToUpper())
			{
				flag2 = false;
			}
		}
		if (flag2)
		{
			try
			{
				if (File.Exists(executablePath + sProfile + "_" + sClass + "_Abilities.xml"))
				{
					File.Delete(executablePath + sCopyTo + "_" + sClass + "_Abilities.xml");
					File.Copy(executablePath + sProfile + "_" + sClass + "_Abilities.xml", executablePath + sCopyTo + "_" + sClass + "_Abilities.xml");
				}
			}
			catch
			{
				return false;
			}
		}
		if (flag)
		{
			try
			{
				if (File.Exists(executablePath + sProfile + "_" + sClass + "_Rotations.xml"))
				{
					File.Delete(executablePath + sCopyTo + "_" + sClass + "_Rotations.xml");
					File.Copy(executablePath + sProfile + "_" + sClass + "_Rotations.xml", executablePath + sCopyTo + "_" + sClass + "_Rotations.xml");
				}
			}
			catch
			{
				return false;
			}
		}
		return true;
	}

	public static void DeleteXMLs(string sClass, string sProfile)
	{
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		File.Delete(executablePath + sProfile + "_" + sClass + "_Rotations.xml");
		File.Delete(executablePath + sProfile + "_" + sClass + "_Abilities.xml");
	}

	public static string[,] RotationEditor_Profiles(string sClass)
	{
		string[,] array = new string[1024, 2];
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		executablePath += "\\Profiles\\";
		string[] files = Directory.GetFiles(executablePath, "*_" + sClass.ToUpper() + "_Rotations.xml");
		string[] array2 = files;
		foreach (string text in array2)
		{
			for (int j = 0; j < 1024; j++)
			{
				if (array[j, 0] == null)
				{
					string text2 = text.Substring(text.LastIndexOf("\\") + 1, text.Length - text.LastIndexOf("\\") - 1);
					text2 = text2.Substring(0, text2.IndexOf("_" + sClass.ToUpper()));
					array[j, 0] = text2;
					array[j, 1] = text;
					break;
				}
			}
		}
		return array;
	}

	public static string[,] LoadXML_SelectFormOffsets()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected I4, but got Unknown
		string executablePath = Application.ExecutablePath;
		executablePath = executablePath.Substring(0, executablePath.Length - (executablePath.Length - executablePath.LastIndexOf("\\")));
		string[] files = Directory.GetFiles(executablePath, "Offsets_*.xml");
		string[,] array = new string[1024, 4];
		string[] array2 = files;
		foreach (string text in array2)
		{
			for (int j = 0; j < 1024; j++)
			{
				if (array[j, 0] != null)
				{
					continue;
				}
				try
				{
					FileStream fileStream = null;
					string path = text;
					new XmlDocument();
					if (!File.Exists(path))
					{
						break;
					}
					fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
					XmlTextReader val = new XmlTextReader((Stream)fileStream);
					string text2 = "";
					do
					{
						XmlNodeType nodeType = ((XmlReader)val).NodeType;
						switch ((int)nodeType - 1)
						{
						case 0:
							text2 = ((XmlReader)val).Name;
							break;
						case 2:
							switch (text2)
							{
							case "CurrentWoWVersion":
								array[j, 0] = text;
								array[j, 1] = ((XmlReader)val).Value.Trim();
								break;
							case "WoWVersionOffset":
								array[j, 2] = ((XmlReader)val).Value;
								break;
							case "PlayerName":
								array[j, 3] = ((XmlReader)val).Value;
								break;
							}
							break;
						}
					}
					while (((XmlReader)val).Read());
					fileStream.Close();
					if (array[j, 0] == null || array[j, 1] == null || array[j, 2] == null || array[j, 3] == null)
					{
						array[j, 0] = null;
						array[j, 1] = null;
						array[j, 2] = null;
						array[j, 3] = null;
					}
				}
				catch
				{
				}
				break;
			}
		}
		return array;
	}

	public static void LoadXML_Offsets(string sVersionNumber)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected I4, but got Unknown
		string path = "";
		string[,] offsetsArray = frmSelect.OffsetsArray;
		for (int i = 0; i < 1024; i++)
		{
			if (offsetsArray[i, 1] == sVersionNumber)
			{
				path = offsetsArray[i, 0];
				break;
			}
			if (offsetsArray[i, 1] == null)
			{
				break;
			}
		}
		FileStream fileStream = null;
		new XmlDocument();
		if (!File.Exists(path))
		{
			return;
		}
		fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
		XmlTextReader val = new XmlTextReader((Stream)fileStream);
		string text = "";
		do
		{
			XmlNodeType nodeType = ((XmlReader)val).NodeType;
			switch ((int)nodeType - 1)
			{
			case 0:
				text = ((XmlReader)val).Name;
				break;
			case 2:
				switch (text)
				{
				case "CurrentWoWVersion":
					clsOffsets.CurrentWoWVersion = Convert.ToUInt32(((XmlReader)val).Value);
					break;
				case "WoWVersionOffset":
					clsOffsets.wowVersion = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "PlayerName":
					clsOffsets.PlayerName = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "GameState":
					clsOffsets.GameState = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "GetCurrentKeyBoardFocus":
					clsOffsets.KeyboardFocus = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "ClntObjMgrGetActivePlayerObjAddress":
					clsOffsets.ClntObjMgrGetActivePlayerObjAddress = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "Lua_DoStringAddress":
					clsOffsets.Lua_DoStringAddress = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "Lua_GetLocalizedTextAddress":
					clsOffsets.Lua_GetLocalizedTextAddress = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "PlayerClass":
					clsOffsets.PlayerClass = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "Detour":
					clsOffsets.Detour = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				case "Overwritten":
				{
					byte[] array = new byte[9];
					string value = ((XmlReader)val).Value;
					string[] array2 = value.Split(new char[1] { ' ' });
					for (int j = 0; j < 9; j++)
					{
						array[j] = byte.Parse(array2[j], NumberStyles.HexNumber);
					}
					clsOffsets.OverWritten = array;
					break;
				}
				case "ObjectFieldGUID":
					clsOffsets.ObjectField_GUID = uint.Parse(((XmlReader)val).Value.Replace("0x", "").Trim(), NumberStyles.HexNumber);
					break;
				}
				break;
			}
		}
		while (((XmlReader)val).Read());
		fileStream.Close();
	}
}
