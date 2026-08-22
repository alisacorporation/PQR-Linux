using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using PriorityQueueRotation.Properties;

namespace PriorityQueueRotation;

public class frmMain : Form
{
	public static string[,] abilityArray;

	public static string[,] rotationArray;

	public static string[,] currentRotationArray;

	public static bool isLoading;

	public static bool SettingsShown;

	public static int intCounter;

	public static int intCountRestore;

	public static string[] chatQueue;

	public static Hotkey hk0;

	public static Hotkey hk1;

	public static Hotkey hk2;

	public static Hotkey hk3;

	public static Hotkey hk4;

	public static Hotkey hk5;

	private IContainer components;

	private System.Windows.Forms.Timer tmrMain;

	public BackgroundWorker bwExecute;

	private Button btnRotationEditor;

	private Label label4;

	private ComboBox cmbRotation2;

	private Label label2;

	private ComboBox cmbRotation1;

	private Label label1;

	private Button btnAbilityEditor;

	private TextBox txtAddSpell;

	private CheckBox chkInterruptAll;

	private Button btnAddSelected;

	private Button btnDelete;

	private CheckedListBox chkSpellList;

	private CheckBox chkDebug;

	internal Label lblRefreshRate;

	internal TrackBar tbRefreshRate;

	internal Label lblRefreshRateValue;

	private Button btnSettings;

	private Label label3;

	private Label label6;

	private ComboBox cmbRotation4;

	private ComboBox cmbRotation3;

	private Label lblHotkey1;

	private Label lblHotkey2;

	private Label lblHotkey3;

	private Label lblHotkey4;

	private Label lblHotkeyInterrupt;

	private Label label7;

	private Label label8;

	private Label label9;

	private ComboBox cmbStartRotation;

	private ComboBox cmbChangeRotation;

	private ComboBox cmbStopRotation;

	private ComboBox cmbStopInterrupt;

	private ComboBox cmbStartInterrupt;

	private Label lblChangeSound;

	private Label label11;

	private Label label10;

	private CheckBox chkShowMessages;

	private Label lblSmartHotkey;

	private Label label12;

	private TextBox txtSmartCommand;

	private Label label13;

	private Button btnSmartHelp;

	internal Label label5;

	internal TrackBar tbInterruptDelay;

	internal Label lblInterruptDelay;

	private PictureBox pbInfo2;

	private PictureBox pbInfo1;

	private PictureBox pbInfo3;

	private PictureBox pbInfo4;

	private Label label14;

	private ComboBox cmbRequireCombat;

	private PictureBox pbRequireCombat;

	public frmMain()
	{
		InitializeComponent();
	}

	private void frmMain_Load(object sender, EventArgs e)
	{
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PriorityQueueRotation.information.ico");
		pbInfo1.Image = Image.FromStream(manifestResourceStream);
		pbInfo1.SizeMode = (PictureBoxSizeMode)1;
		((Control)pbInfo1).Visible = false;
		pbInfo2.Image = Image.FromStream(manifestResourceStream);
		pbInfo2.SizeMode = (PictureBoxSizeMode)1;
		((Control)pbInfo2).Visible = false;
		pbInfo3.Image = Image.FromStream(manifestResourceStream);
		pbInfo3.SizeMode = (PictureBoxSizeMode)1;
		((Control)pbInfo3).Visible = false;
		pbInfo4.Image = Image.FromStream(manifestResourceStream);
		pbInfo4.SizeMode = (PictureBoxSizeMode)1;
		((Control)pbInfo4).Visible = false;
		pbRequireCombat.Image = Image.FromStream(manifestResourceStream);
		pbRequireCombat.SizeMode = (PictureBoxSizeMode)1;
		((Control)pbRequireCombat).Visible = true;
		((Form)Program.mainForm).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		try
		{
			if (GlobalSettings.CurrentProcessID != 0)
			{
				clsMemory.Initialize(GlobalSettings.CurrentProcessID);
				GlobalSettings.CurrentPlayerName = clsMemory.GetPlayerName();
				GlobalSettings.CurrentPlayerClass = clsMemory.GetPlayerClass();
				if (clsOffsets.Detour == 0)
				{
					clsOffsets.Detour = clsMemory.Memory.FindPattern(clsOffsets.OverWrittenPattern, "xxxxxxxxxxxxxxxx") - (uint)(int)clsMemory.Memory.MainModule.BaseAddress;
				}
				if (clsOffsets.ClntObjMgrGetActivePlayerObjAddress == 0)
				{
					clsOffsets.ClntObjMgrGetActivePlayerObjAddress = clsMemory.Memory.FindPattern(clsOffsets.ClntObjMgrSearch, clsOffsets.ClntObjMgrMask) - (uint)(int)clsMemory.Memory.MainModule.BaseAddress;
				}
			}
			else
			{
				((Control)cmbRotation1).Enabled = false;
				((Control)cmbRotation2).Enabled = false;
				((Control)cmbRotation3).Enabled = false;
				((Control)cmbRotation4).Enabled = false;
				GlobalSettings.CurrentPlayerName = "Edit Mode";
			}
			((Control)Program.mainForm).Text = "Rotation - " + GlobalSettings.CurrentPlayerName;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
		if (GlobalSettings.CurrentProcessID != 0)
		{
			LoadClass(GlobalSettings.CurrentPlayerClass);
			LoadRotations(GlobalSettings.CurrentPlayerClass);
			Program.mainForm.tmrMain.Start();
		}
		ConfigureHotkeys();
		LoadSettings();
		((Control)Program.selectForm).Hide();
	}

	private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
	{
		((Form)Program.selectForm).Close();
	}

	private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
	{
		if (GlobalSettings.AppliedExecutor)
		{
			if (bwExecute.IsBusy)
			{
				bwExecute.CancelAsync();
				Thread.Sleep(100);
			}
			try
			{
				GlobalSettings.CurrentRunningProfile = "";
				GlobalSettings.InterruptMode = false;
				StartStopRotation(-1);
				SetupBotWoW();
			}
			catch
			{
			}
		}
		if (GlobalSettings.AbilityEditorLoaded)
		{
			((Form)Program.abilityForm).Close();
		}
		if (GlobalSettings.RotationEditorLoaded)
		{
			((Form)Program.rotationForm).Close();
		}
		UnregisterHotkeys();
		SaveSettings();
		try
		{
			if (GlobalSettings.AppliedExecutor)
			{
				clsMemory.RestoreExecutor();
			}
		}
		catch
		{
		}
	}

	private void tmrMain_Tick(object sender, EventArgs e)
	{
		ProcessDebug();
		try
		{
			_ = Process.GetProcessById(GlobalSettings.CurrentProcessID).ProcessName.Length;
		}
		catch
		{
			try
			{
				UnregisterHotkeys();
			}
			catch
			{
			}
			SaveSettings();
			Process.GetCurrentProcess().Kill();
		}
		if (clsMemory.WorldHasFocus() && GlobalSettings.WoWHasFocus(GlobalSettings.CurrentProcessID))
		{
			if (!hk0.Registered)
			{
				RegisterHotkeys();
			}
		}
		else if (hk0.Registered)
		{
			UnregisterHotkeys();
		}
		if (GlobalSettings.RestoreDelay > 0)
		{
			intCountRestore++;
		}
		if (intCountRestore >= 10 && GlobalSettings.RestoreDelay > 0)
		{
			intCountRestore = 0;
			GlobalSettings.RestoreDelay--;
		}
		if (GlobalSettings.EnableButtons)
		{
			GlobalSettings.EnableButtons = false;
			((Control)btnAbilityEditor).Enabled = true;
			((Control)btnRotationEditor).Enabled = true;
		}
		if (GlobalSettings.DisableButtons)
		{
			GlobalSettings.DisableButtons = false;
			((Control)btnAbilityEditor).Enabled = false;
			((Control)btnRotationEditor).Enabled = false;
		}
		intCounter++;
		if (clsMemory.IsAtCharacterSelect())
		{
			GlobalSettings.CurrentRunningProfile = "";
			GlobalSettings.InterruptMode = false;
			GlobalSettings.BotRunning = false;
			GlobalSettings.EnableButtons = true;
		}
		try
		{
			if (clsMemory.IsWoWReady())
			{
				GlobalSettings.CurrentPlayerName = clsMemory.GetPlayerName();
			}
		}
		catch
		{
			GlobalSettings.CurrentPlayerName = "";
		}
		if (GlobalSettings.CurrentPlayerName == "")
		{
			((Control)Program.mainForm).Text = "Rotation - Not Logged In.";
		}
		else
		{
			((Control)Program.mainForm).Text = "Rotation - " + GlobalSettings.CurrentPlayerName;
		}
		bool flag = clsMemory.IsWoWReady();
		if (intCounter >= 50 && flag)
		{
			tmrMain.Stop();
			intCounter = 0;
			GlobalSettings.CurrentPlayerClass = clsMemory.GetPlayerClass();
			if (GlobalSettings.CurrentPlayerClass != GlobalSettings.CurrentLoadedClass && GlobalSettings.CurrentPlayerClass != "")
			{
				GlobalSettings.CurrentRunningProfile = "";
				GlobalSettings.InterruptMode = false;
				StartStopRotation(-1);
				LoadClass(GlobalSettings.CurrentPlayerClass);
				LoadRotations(GlobalSettings.CurrentPlayerClass);
				((Control)Program.mainForm.cmbRotation1).Enabled = true;
				((Control)Program.mainForm.cmbRotation2).Enabled = true;
				((Control)Program.mainForm.cmbRotation3).Enabled = true;
				((Control)Program.mainForm.cmbRotation4).Enabled = true;
			}
			else if (GlobalSettings.CurrentPlayerClass != "")
			{
				((Control)Program.mainForm.cmbRotation1).Enabled = true;
				((Control)Program.mainForm.cmbRotation2).Enabled = true;
				((Control)Program.mainForm.cmbRotation3).Enabled = true;
				((Control)Program.mainForm.cmbRotation4).Enabled = true;
			}
			else
			{
				((Control)Program.mainForm.cmbRotation1).Enabled = false;
				((Control)Program.mainForm.cmbRotation2).Enabled = false;
				((Control)Program.mainForm.cmbRotation3).Enabled = false;
				((Control)Program.mainForm.cmbRotation4).Enabled = false;
			}
			tmrMain.Start();
		}
		else if (!flag)
		{
			AddToDebug("Class Lookup: WoW Not Ready. Current Delay: " + GlobalSettings.RestoreDelay);
		}
	}

	public static void AddToDebug(string strAddText)
	{
		if (!GlobalSettings.DebugMode)
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (GlobalSettings.DebugArray[i] == null)
			{
				GlobalSettings.DebugArray[i] = strAddText;
				break;
			}
		}
	}

	public static void ProcessDebug()
	{
		if (!GlobalSettings.DebugMode)
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (GlobalSettings.DebugArray[i] != null)
			{
				GlobalSettings.DebugArray[i] = null;
			}
		}
	}

	private void btnAddSelected_Click(object sender, EventArgs e)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		((Control)btnAddSelected).Enabled = false;
		((Control)btnDelete).Enabled = false;
		string text = ((Control)txtAddSpell).Text.Trim();
		string text2 = ((Control)txtAddSpell).Text.Trim();
		if (Information.IsNumeric((object)text))
		{
			AddToInterruptList(text);
		}
		else if (text2 != "")
		{
			AddToInterruptList(text2);
		}
		else
		{
			MessageBox.Show("You must specify a Spell Name to add a spell to the list.");
		}
		((Control)txtAddSpell).Text = "";
		DisplayList();
		((Control)btnAddSelected).Enabled = true;
		((Control)btnDelete).Enabled = true;
	}

	private void btnDelete_Click(object sender, EventArgs e)
	{
		if (((ListControl)chkSpellList).SelectedIndex != -1)
		{
			chkSpellList.Items.RemoveAt(((ListControl)chkSpellList).SelectedIndex);
			SetListFromDisplay();
		}
	}

	private void chkInterruptAll_CheckedChanged(object sender, EventArgs e)
	{
		GlobalSettings.InterruptAll = chkInterruptAll.Checked;
		Settings.Default.InterruptAll = GlobalSettings.InterruptAll;
		((SettingsBase)Settings.Default).Save();
		GlobalSettings.LoadBot = true;
	}

	private void chkSpellList_SelectedValueChanged(object sender, EventArgs e)
	{
		SetListFromDisplay();
		SaveList();
		GlobalSettings.LoadBot = true;
	}

	private void SaveList()
	{
		string text = "";
		string text2 = "";
		for (int i = 0; i < GlobalSettings.spellList.Count; i++)
		{
			if (text == "")
			{
				text = GlobalSettings.spellList[i];
				text2 = ((!GlobalSettings.spellEnabled[i]) ? "false" : "true");
			}
			else
			{
				text = text + "|" + GlobalSettings.spellList[i];
				text2 = ((!GlobalSettings.spellEnabled[i]) ? (text2 + "|false") : (text2 + "|true"));
			}
		}
		Settings.Default.SpellList = text;
		Settings.Default.EnabledList = text2;
		((SettingsBase)Settings.Default).Save();
	}

	private void LoadList()
	{
		string[] array = Settings.Default.SpellList.Split(new char[1] { '|' });
		string[] array2 = Settings.Default.EnabledList.Split(new char[1] { '|' });
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != "")
			{
				GlobalSettings.spellList.Add(array[i]);
				if (array2[i] == "true")
				{
					GlobalSettings.spellEnabled.Add(item: true);
				}
				else
				{
					GlobalSettings.spellEnabled.Add(item: false);
				}
			}
		}
		DisplayList();
	}

	private void LoadSettings()
	{
		LoadList();
		chkInterruptAll.Checked = Settings.Default.InterruptAll;
		tbRefreshRate.Value = (int)Settings.Default.RefreshRate;
		UpdateRefresh();
		GlobalSettings.InterruptAll = chkInterruptAll.Checked;
		cmbStartRotation.SelectedItem = Settings.Default.StartRotation;
		cmbChangeRotation.SelectedItem = Settings.Default.ChangeRotation;
		cmbStopRotation.SelectedItem = Settings.Default.EndRotation;
		cmbStartInterrupt.SelectedItem = Settings.Default.StartInterrupt;
		cmbStopInterrupt.SelectedItem = Settings.Default.StopInterrupt;
		chkShowMessages.Checked = Settings.Default.ShowMessages;
		GlobalSettings.ShowMessages = chkShowMessages.Checked;
		((Control)txtSmartCommand).Text = Settings.Default.SmartHotkey;
		GlobalSettings.PQR_SmartHotkeyCommand = Settings.Default.SmartHotkey.Trim();
		cmbRequireCombat.SelectedItem = Settings.Default.CombatRequirement;
		switch ((!((cmbRequireCombat.SelectedItem == null) | (cmbRequireCombat.SelectedItem.ToString().Trim() == ""))) ? cmbRequireCombat.SelectedItem.ToString().Trim() : "")
		{
		case "":
		case "Rotation Designated":
			GlobalSettings.RequireCombat = true;
			GlobalSettings.AllowRotationDesignation = true;
			break;
		case "True":
			GlobalSettings.RequireCombat = true;
			GlobalSettings.AllowRotationDesignation = false;
			break;
		case "False":
			GlobalSettings.RequireCombat = false;
			GlobalSettings.AllowRotationDesignation = false;
			break;
		default:
			GlobalSettings.RequireCombat = true;
			GlobalSettings.AllowRotationDesignation = true;
			break;
		}
		tbInterruptDelay.Value = (int)Settings.Default.InterruptDelay;
		GlobalSettings.InterruptDelay = Settings.Default.InterruptDelay;
		((Control)lblInterruptDelay).Text = tbInterruptDelay.Value + "ms";
	}

	private void AddToInterruptList(string castingSpellIDorName)
	{
		bool flag = false;
		foreach (string spell in GlobalSettings.spellList)
		{
			if (spell.ToUpper() == castingSpellIDorName.ToUpper())
			{
				flag = true;
			}
		}
		if (!flag)
		{
			GlobalSettings.spellList.Add(castingSpellIDorName);
			GlobalSettings.spellEnabled.Add(item: true);
		}
		SaveList();
	}

	private void SetListFromDisplay()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		GlobalSettings.spellEnabled = new List<bool>();
		GlobalSettings.spellList = new List<string>();
		for (int i = 0; i < chkSpellList.Items.Count; i++)
		{
			GlobalSettings.spellList.Add(chkSpellList.Items[i].ToString());
			if ((int)chkSpellList.GetItemCheckState(i) == 0)
			{
				GlobalSettings.spellEnabled.Add(item: false);
			}
			else
			{
				GlobalSettings.spellEnabled.Add(item: true);
			}
		}
	}

	private void DisplayList()
	{
		Program.mainForm.chkSpellList.Items.Clear();
		for (int i = 0; i < GlobalSettings.spellList.Count; i++)
		{
			Program.mainForm.chkSpellList.Items.Add((object)GlobalSettings.spellList[i], GlobalSettings.spellEnabled[i]);
		}
	}

	private void SaveSettings()
	{
		((SettingsBase)Settings.Default).Save();
	}

	private void bwExecute_DoWork(object sender, DoWorkEventArgs e)
	{
		while (!bwExecute.CancellationPending)
		{
			ExecuteLogic();
			Thread.Sleep(30);
		}
	}

	private void ProcessChatQueue()
	{
		if (!clsMemory.IsWoWReady())
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (chatQueue[i] != null)
			{
				clsMemory.WriteToChat(chatQueue[i]);
			}
		}
		chatQueue = new string[1024];
	}

	public static void AddToChatQueue(string strAdd)
	{
		for (int i = 0; i < 1024; i++)
		{
			if (chatQueue[i] == null)
			{
				chatQueue[i] = strAdd;
				break;
			}
		}
	}

	public static void ConfigureHotkeys()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		UnregisterHotkeys();
		hk0 = new Hotkey();
		hk0.KeyCode = Settings.Default.Hotkey0Key;
		hk0.Shift = Settings.Default.Hotkey0SHIFT;
		hk0.Control = Settings.Default.Hotkey0CTRL;
		hk0.Alt = Settings.Default.Hotkey0ALT;
		hk0.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(0);
		};
		((Control)Program.mainForm.lblHotkeyInterrupt).Text = hk0.ToString();
		hk1 = new Hotkey();
		hk1.KeyCode = Settings.Default.Hotkey1Key;
		hk1.Shift = Settings.Default.Hotkey1SHIFT;
		hk1.Control = Settings.Default.Hotkey1CTRL;
		hk1.Alt = Settings.Default.Hotkey1ALT;
		hk1.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(1);
		};
		((Control)Program.mainForm.lblHotkey1).Text = hk1.ToString();
		hk2 = new Hotkey();
		hk2.KeyCode = Settings.Default.Hotkey2Key;
		hk2.Shift = Settings.Default.Hotkey2SHIFT;
		hk2.Control = Settings.Default.Hotkey2CTRL;
		hk2.Alt = Settings.Default.Hotkey2ALT;
		hk2.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(2);
		};
		((Control)Program.mainForm.lblHotkey2).Text = hk2.ToString();
		hk3 = new Hotkey();
		hk3.KeyCode = Settings.Default.Hotkey3Key;
		hk3.Shift = Settings.Default.Hotkey3SHIFT;
		hk3.Control = Settings.Default.Hotkey3CTRL;
		hk3.Alt = Settings.Default.Hotkey3ALT;
		hk3.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(3);
		};
		((Control)Program.mainForm.lblHotkey3).Text = hk3.ToString();
		hk4 = new Hotkey();
		hk4.KeyCode = Settings.Default.Hotkey4Key;
		hk4.Shift = Settings.Default.Hotkey4SHIFT;
		hk4.Control = Settings.Default.Hotkey4CTRL;
		hk4.Alt = Settings.Default.Hotkey4ALT;
		hk4.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(4);
		};
		((Control)Program.mainForm.lblHotkey4).Text = hk4.ToString();
		hk5 = new Hotkey();
		hk5.KeyCode = Settings.Default.Hotkey5Key;
		hk5.Shift = Settings.Default.Hotkey5SHIFT;
		hk5.Control = Settings.Default.Hotkey5CTRL;
		hk5.Alt = Settings.Default.Hotkey5ALT;
		hk5.Pressed += delegate
		{
			Program.mainForm.StartStopRotation(5);
		};
		((Control)Program.mainForm.lblSmartHotkey).Text = hk5.ToString();
	}

	public static void RegisterHotkeys()
	{
		ConfigureHotkeys();
		hk0.Register((Control)(object)Program.mainForm);
		hk1.Register((Control)(object)Program.mainForm);
		hk2.Register((Control)(object)Program.mainForm);
		hk3.Register((Control)(object)Program.mainForm);
		hk4.Register((Control)(object)Program.mainForm);
		hk5.Register((Control)(object)Program.mainForm);
	}

	public static void UnregisterHotkeys()
	{
		if (hk0.Registered)
		{
			hk0.Unregister();
		}
		if (hk1.Registered)
		{
			hk1.Unregister();
		}
		if (hk2.Registered)
		{
			hk2.Unregister();
		}
		if (hk3.Registered)
		{
			hk3.Unregister();
		}
		if (hk4.Registered)
		{
			hk4.Unregister();
		}
		if (hk5.Registered)
		{
			hk5.Unregister();
		}
	}

	private void StartStopRotation(int rotationNumber)
	{
		if (GlobalSettings.StartingBot || GlobalSettings.CurrentProcessID == 0)
		{
			return;
		}
		GlobalSettings.StartingBot = true;
		if (!bwExecute.IsBusy)
		{
			bwExecute.RunWorkerAsync();
		}
		switch (rotationNumber)
		{
		case 0:
			GlobalSettings.InterruptMode = true;
			GlobalSettings.ChangeInterrupt = true;
			break;
		case 1:
			if (cmbRotation1.SelectedItem != null)
			{
				GlobalSettings.CurrentRunningProfile = cmbRotation1.SelectedItem.ToString();
				GlobalSettings.ChangeRotation = true;
				LoadClass(GlobalSettings.CurrentPlayerClass);
				LoadRotations(GlobalSettings.CurrentPlayerClass);
				LoadCurrentArray(GlobalSettings.CurrentRunningProfile);
				GlobalSettings.SmartHotkeyMode = false;
			}
			break;
		case 2:
			if (cmbRotation2.SelectedItem != null)
			{
				GlobalSettings.CurrentRunningProfile = cmbRotation2.SelectedItem.ToString();
				GlobalSettings.ChangeRotation = true;
				LoadClass(GlobalSettings.CurrentPlayerClass);
				LoadRotations(GlobalSettings.CurrentPlayerClass);
				LoadCurrentArray(GlobalSettings.CurrentRunningProfile);
				GlobalSettings.SmartHotkeyMode = false;
			}
			break;
		case 3:
			if (cmbRotation3.SelectedItem != null)
			{
				GlobalSettings.CurrentRunningProfile = cmbRotation3.SelectedItem.ToString();
				GlobalSettings.ChangeRotation = true;
				LoadClass(GlobalSettings.CurrentPlayerClass);
				LoadRotations(GlobalSettings.CurrentPlayerClass);
				LoadCurrentArray(GlobalSettings.CurrentRunningProfile);
				GlobalSettings.SmartHotkeyMode = false;
			}
			break;
		case 4:
			if (cmbRotation4.SelectedItem != null)
			{
				GlobalSettings.CurrentRunningProfile = cmbRotation4.SelectedItem.ToString();
				GlobalSettings.ChangeRotation = true;
				LoadClass(GlobalSettings.CurrentPlayerClass);
				LoadRotations(GlobalSettings.CurrentPlayerClass);
				LoadCurrentArray(GlobalSettings.CurrentRunningProfile);
				GlobalSettings.SmartHotkeyMode = false;
			}
			break;
		case 5:
			GlobalSettings.CurrentRunningProfile = "* Manual";
			GlobalSettings.ChangeRotation = true;
			LoadClass(GlobalSettings.CurrentPlayerClass);
			LoadRotations(GlobalSettings.CurrentPlayerClass);
			GlobalSettings.SmartHotkeyMode = true;
			break;
		default:
			GlobalSettings.InterruptMode = false;
			GlobalSettings.CurrentRunningProfile = "";
			GlobalSettings.ChangeRotation = true;
			GlobalSettings.ChangeInterrupt = true;
			LoadClass(GlobalSettings.CurrentPlayerClass);
			LoadRotations(GlobalSettings.CurrentPlayerClass);
			LoadCurrentArray(GlobalSettings.CurrentRunningProfile);
			break;
		}
		GlobalSettings.LoadBot = true;
		GlobalSettings.StartingBot = false;
	}

	public static void ExecuteLogic()
	{
		if (clsMemory.IsWoWReady() && GlobalSettings.LoadBot)
		{
			SetupBotWoW();
			GlobalSettings.LoadBot = false;
		}
	}

	private static bool RotationRequiresCombat(string sRotation)
	{
		if (sRotation == null || sRotation.Trim() == "")
		{
			return true;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == sRotation)
			{
				if (rotationArray[i, 3] == "false")
				{
					return false;
				}
				return true;
			}
		}
		return true;
	}

	public static void SetupBotWoW()
	{
		GlobalSettings.AppliedExecutor = true;
		clsMemory.ApplyExecutor();
		clsMemory.Lua_GetReturnValue(clsLua.strFirstLoad);
		clsMemory.Lua_GetReturnValue(clsLua.ScriptingFunctions);
		clsMemory.Lua_GetReturnValue(clsLua.strTableSetup);
		clsMemory.Lua_GetReturnValue(clsLua.strClearTables);
		string text = "";
		string text2 = "";
		text2 = GlobalSettings.CurrentRunningProfile;
		text = "";
		if (text2 != "")
		{
			Program.mainForm.LoadCurrentArray(text2);
			for (int i = 0; i < 1024; i++)
			{
				if (currentRotationArray[i, 0] != null)
				{
					text = text + " " + clsLua.AddAbility("0", i.ToString(), currentRotationArray[i, 2], currentRotationArray[i, 3], currentRotationArray[i, 4], currentRotationArray[i, 5], currentRotationArray[i, 6], currentRotationArray[i, 7], currentRotationArray[i, 8], currentRotationArray[i, 9]);
				}
			}
			text = text + " PQR[0].priorityTable.requireCombat = " + RotationRequiresCombat(text2).ToString().ToLower();
			clsMemory.Lua_GetReturnValue(text);
		}
		text2 = GlobalSettings.Rotation1(GlobalSettings.CurrentPlayerClass.Trim(), "");
		text = "";
		if (text2 != "")
		{
			Program.mainForm.LoadCurrentArray(text2);
			for (int j = 0; j < 1024; j++)
			{
				if (currentRotationArray[j, 0] != null)
				{
					text = text + " " + clsLua.AddAbility("1", j.ToString(), currentRotationArray[j, 2], currentRotationArray[j, 3], currentRotationArray[j, 4], currentRotationArray[j, 5], currentRotationArray[j, 6], currentRotationArray[j, 7], currentRotationArray[j, 8], currentRotationArray[j, 9]);
				}
			}
			text = text + " PQR[1].priorityTable.requireCombat = " + RotationRequiresCombat(text2).ToString().ToLower();
			clsMemory.Lua_GetReturnValue(text);
		}
		text2 = GlobalSettings.Rotation2(GlobalSettings.CurrentPlayerClass.Trim(), "");
		text = "";
		if (text2 != "")
		{
			Program.mainForm.LoadCurrentArray(text2);
			for (int k = 0; k < 1024; k++)
			{
				if (currentRotationArray[k, 0] != null)
				{
					text = text + " " + clsLua.AddAbility("2", k.ToString(), currentRotationArray[k, 2], currentRotationArray[k, 3], currentRotationArray[k, 4], currentRotationArray[k, 5], currentRotationArray[k, 6], currentRotationArray[k, 7], currentRotationArray[k, 8], currentRotationArray[k, 9]);
				}
			}
			text = text + " PQR[2].priorityTable.requireCombat = " + RotationRequiresCombat(text2).ToString().ToLower();
			clsMemory.Lua_GetReturnValue(text);
		}
		text2 = GlobalSettings.Rotation3(GlobalSettings.CurrentPlayerClass.Trim(), "");
		text = "";
		if (text2 != "")
		{
			Program.mainForm.LoadCurrentArray(text2);
			for (int l = 0; l < 1024; l++)
			{
				if (currentRotationArray[l, 0] != null)
				{
					text = text + " " + clsLua.AddAbility("3", l.ToString(), currentRotationArray[l, 2], currentRotationArray[l, 3], currentRotationArray[l, 4], currentRotationArray[l, 5], currentRotationArray[l, 6], currentRotationArray[l, 7], currentRotationArray[l, 8], currentRotationArray[l, 9]);
				}
			}
			text = text + " PQR[3].priorityTable.requireCombat = " + RotationRequiresCombat(text2).ToString().ToLower();
			clsMemory.Lua_GetReturnValue(text);
		}
		text2 = GlobalSettings.Rotation4(GlobalSettings.CurrentPlayerClass.Trim(), "");
		text = "";
		if (text2 != "")
		{
			Program.mainForm.LoadCurrentArray(text2);
			for (int m = 0; m < 1024; m++)
			{
				if (currentRotationArray[m, 0] != null)
				{
					text = text + " " + clsLua.AddAbility("4", m.ToString(), currentRotationArray[m, 2], currentRotationArray[m, 3], currentRotationArray[m, 4], currentRotationArray[m, 5], currentRotationArray[m, 6], currentRotationArray[m, 7], currentRotationArray[m, 8], currentRotationArray[m, 9]);
				}
			}
			text = text + " PQR[4].priorityTable.requireCombat = " + RotationRequiresCombat(text2).ToString().ToLower();
			clsMemory.Lua_GetReturnValue(text);
		}
		string text3 = "";
		foreach (string spell in GlobalSettings.spellList)
		{
			if (spell.Replace(" ", "") != "")
			{
				text3 = text3 + " PQR_AddInterrupt(\"" + spell + "\")";
			}
		}
		clsMemory.Lua_GetReturnValue(text3);
		clsMemory.Lua_GetReturnValue(clsLua.PreStartupBot(GlobalSettings.ShowMessages, GlobalSettings.DebugMode, GlobalSettings.RequireCombat, GlobalSettings.AllowRotationDesignation, GlobalSettings.RefreshRate, GlobalSettings.InterruptDelay) + " " + clsLua.strCastNextFunction(GlobalSettings.PQR_SmartHotkeyCommand));
		clsMemory.Lua_GetReturnValue(clsLua.StartupBot(GlobalSettings.InterruptMode, GlobalSettings.ChangeInterrupt, GlobalSettings.InterruptAll, GlobalSettings.SmartHotkeyMode, GlobalSettings.ChangeRotation, GlobalSettings.CurrentRunningProfile));
		GlobalSettings.ChangeInterrupt = false;
		GlobalSettings.ChangeRotation = false;
		clsMemory.RestoreExecutor();
	}

	public static bool ChatToSend()
	{
		for (int i = 0; i < 1024; i++)
		{
			if (chatQueue[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	public static void NotifyUserDisabled()
	{
		if (clsMemory.GetPlayerClass() != "" && clsMemory.IsWoWReady())
		{
			GlobalSettings.NotifyUser = false;
			AddToChatQueue("Rotation Mode Disabled.");
		}
	}

	private void btnAbilityEditor_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.RotationEditorLoaded)
		{
			((Form)Program.rotationForm).Close();
		}
		if (!GlobalSettings.AbilityEditorLoaded)
		{
			Program.abilityForm = new frmAbilityEditor();
			((Control)Program.abilityForm).Show();
		}
		else
		{
			((Control)Program.abilityForm).Show();
		}
	}

	private void btnRotationEditor_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.AbilityEditorLoaded)
		{
			((Form)Program.abilityForm).Close();
		}
		if (!GlobalSettings.RotationEditorLoaded)
		{
			Program.rotationForm = new frmRotationEditor();
			((Control)Program.rotationForm).Show();
		}
		else
		{
			((Control)Program.rotationForm).Show();
		}
	}

	public static void LockdownMain(bool trueFalse)
	{
		if (trueFalse)
		{
			((Control)Program.mainForm.cmbRotation1).Enabled = false;
			((Control)Program.mainForm.cmbRotation2).Enabled = false;
			((Control)Program.mainForm.cmbRotation3).Enabled = false;
			((Control)Program.mainForm.cmbRotation4).Enabled = false;
		}
		else
		{
			((Control)Program.mainForm.cmbRotation1).Enabled = true;
			((Control)Program.mainForm.cmbRotation2).Enabled = true;
			((Control)Program.mainForm.cmbRotation3).Enabled = true;
			((Control)Program.mainForm.cmbRotation4).Enabled = true;
			LoadClass(GlobalSettings.CurrentPlayerClass);
			LoadRotations(GlobalSettings.CurrentPlayerClass);
		}
	}

	public static void LoadClass(string strClass)
	{
		if (strClass != "" && strClass != null)
		{
			GlobalSettings.CurrentLoadedClass = strClass;
			rotationArray = clsXML.LoadXML_AllRotations(strClass);
			abilityArray = clsXML.LoadXML_AllAbilities(strClass);
		}
		else
		{
			GlobalSettings.CurrentLoadedClass = "";
		}
	}

	public static void LoadRotations(string strClass)
	{
		isLoading = true;
		if (strClass == null || strClass == "")
		{
			Program.mainForm.cmbRotation1.Items.Clear();
			Program.mainForm.cmbRotation2.Items.Clear();
			Program.mainForm.cmbRotation3.Items.Clear();
			Program.mainForm.cmbRotation4.Items.Clear();
		}
		else
		{
			Program.mainForm.cmbRotation1.Items.Clear();
			Program.mainForm.cmbRotation2.Items.Clear();
			Program.mainForm.cmbRotation3.Items.Clear();
			Program.mainForm.cmbRotation4.Items.Clear();
			for (int i = 0; i < 1024; i++)
			{
				if (rotationArray[i, 0] != null)
				{
					Program.mainForm.cmbRotation1.Items.Add((object)rotationArray[i, 0]);
					Program.mainForm.cmbRotation2.Items.Add((object)rotationArray[i, 0]);
					Program.mainForm.cmbRotation3.Items.Add((object)rotationArray[i, 0]);
					Program.mainForm.cmbRotation4.Items.Add((object)rotationArray[i, 0]);
				}
			}
		}
		((Control)Program.mainForm.pbInfo1).Visible = false;
		((Control)Program.mainForm.pbInfo2).Visible = false;
		((Control)Program.mainForm.pbInfo3).Visible = false;
		((Control)Program.mainForm.pbInfo4).Visible = false;
		Program.mainForm.cmbRotation1.SelectedItem = GlobalSettings.Rotation1(strClass, "");
		Program.mainForm.cmbRotation2.SelectedItem = GlobalSettings.Rotation2(strClass, "");
		Program.mainForm.cmbRotation3.SelectedItem = GlobalSettings.Rotation3(strClass, "");
		Program.mainForm.cmbRotation4.SelectedItem = GlobalSettings.Rotation4(strClass, "");
		isLoading = false;
	}

	private void chkSpellList_SelectedIndexChanged(object sender, EventArgs e)
	{
	}

	private bool RotationHasNotes(string sRotation)
	{
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == null)
			{
				return false;
			}
			if (rotationArray[i, 0] == sRotation)
			{
				if (rotationArray[i, 4].Trim() == "")
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private void cmbRotation1_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbRotation1.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation1.SelectedItem.ToString()))
			{
				((Control)pbInfo1).Visible = true;
			}
			else
			{
				((Control)pbInfo1).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo1).Visible = false;
		}
		if (!isLoading)
		{
			GlobalSettings.Rotation1(GlobalSettings.CurrentLoadedClass, cmbRotation1.SelectedItem.ToString());
			((SettingsBase)Settings.Default).Save();
		}
	}

	private void cmbRotation2_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbRotation2.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation2.SelectedItem.ToString()))
			{
				((Control)pbInfo2).Visible = true;
			}
			else
			{
				((Control)pbInfo2).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo2).Visible = false;
		}
		if (!isLoading)
		{
			GlobalSettings.Rotation2(GlobalSettings.CurrentLoadedClass, cmbRotation2.SelectedItem.ToString());
			((SettingsBase)Settings.Default).Save();
		}
	}

	private void LoadCurrentArray(string strArray)
	{
		string text = "";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == strArray)
			{
				text = rotationArray[i, 2];
			}
		}
		if (text.Replace(" ", "") == "")
		{
			currentRotationArray = new string[1024, 10];
			return;
		}
		currentRotationArray = new string[1024, 10];
		string[] array = text.Split(new char[1] { '|' });
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.Replace(" ", "") != "")
			{
				AddAbilityToCurrent(text2);
			}
		}
	}

	private void AddAbilityToCurrent(string strName)
	{
		int num = -1;
		if (strName != "")
		{
			for (int i = 0; i < 1024; i++)
			{
				if (abilityArray[i, 0] == strName.Trim())
				{
					num = i;
				}
				else if (abilityArray[i, 0] == null)
				{
					break;
				}
			}
		}
		if (num == -1)
		{
			return;
		}
		for (int j = 0; j < 1024; j++)
		{
			if (currentRotationArray[j, 0] == null)
			{
				currentRotationArray[j, 0] = abilityArray[num, 0];
				currentRotationArray[j, 1] = abilityArray[num, 1];
				currentRotationArray[j, 2] = abilityArray[num, 2];
				currentRotationArray[j, 3] = abilityArray[num, 3];
				currentRotationArray[j, 4] = abilityArray[num, 4];
				currentRotationArray[j, 5] = abilityArray[num, 5];
				currentRotationArray[j, 6] = abilityArray[num, 6];
				currentRotationArray[j, 7] = abilityArray[num, 7];
				currentRotationArray[j, 8] = abilityArray[num, 8];
				currentRotationArray[j, 9] = abilityArray[num, 9];
				break;
			}
			if (currentRotationArray[j, 0] == strName)
			{
				break;
			}
		}
	}

	private void chkDebug_CheckedChanged(object sender, EventArgs e)
	{
		GlobalSettings.DebugMode = chkDebug.Checked;
		GlobalSettings.LoadBot = true;
	}

	private void tbRefreshRate_Scroll(object sender, EventArgs e)
	{
		UpdateRefresh();
	}

	private void UpdateRefresh()
	{
		if (Information.IsNumeric((object)tbRefreshRate.Value))
		{
			if (!isLoading)
			{
				Settings.Default.RefreshRate = tbRefreshRate.Value;
				((SettingsBase)Settings.Default).Save();
			}
			GlobalSettings.ChangeFrequency = true;
			GlobalSettings.RefreshRate = tbRefreshRate.Value;
			((Control)lblRefreshRateValue).Text = tbRefreshRate.Value + "ms";
		}
	}

	private void btnSettings_Click(object sender, EventArgs e)
	{
		Size size = default(Size);
		if (SettingsShown)
		{
			size.Height = 376;
			size.Width = 385;
			((Control)Program.mainForm).MaximumSize = size;
			((Form)Program.mainForm).Size = size;
			((Control)Program.mainForm).MinimumSize = size;
			SettingsShown = false;
			((Control)btnSettings).Text = "Show Settings";
		}
		else
		{
			size.Height = 376;
			size.Width = 700;
			((Control)Program.mainForm).MaximumSize = size;
			((Form)Program.mainForm).Size = size;
			((Control)Program.mainForm).MinimumSize = size;
			SettingsShown = true;
			((Control)btnSettings).Text = "Hide Settings";
		}
	}

	private void lblHotkey1_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 1;
		((Control)Program.hotkeyForm).Text = "Hotkey for Rotation #" + GlobalSettings.EditingHotkey;
		((Control)Program.hotkeyForm).Show();
	}

	private void lblHotkey2_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 2;
		((Control)Program.hotkeyForm).Text = "Hotkey for Rotation #" + GlobalSettings.EditingHotkey;
		((Control)Program.hotkeyForm).Show();
	}

	private void lblHotkey3_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 3;
		((Control)Program.hotkeyForm).Text = "Hotkey for Rotation #" + GlobalSettings.EditingHotkey;
		((Control)Program.hotkeyForm).Show();
	}

	private void lblHotkey4_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 4;
		((Control)Program.hotkeyForm).Text = "Hotkey for Rotation #" + GlobalSettings.EditingHotkey;
		((Control)Program.hotkeyForm).Show();
	}

	private void lblHotkeyInterrupt_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 0;
		((Control)Program.hotkeyForm).Text = "Hotkey for Interrupt Mode";
		((Control)Program.hotkeyForm).Show();
	}

	private void cmbRotation3_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbRotation3.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation3.SelectedItem.ToString()))
			{
				((Control)pbInfo3).Visible = true;
			}
			else
			{
				((Control)pbInfo3).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo3).Visible = false;
		}
		if (!isLoading)
		{
			GlobalSettings.Rotation3(GlobalSettings.CurrentLoadedClass, cmbRotation3.SelectedItem.ToString());
			((SettingsBase)Settings.Default).Save();
		}
	}

	private void cmbRotation4_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbRotation4.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation4.SelectedItem.ToString()))
			{
				((Control)pbInfo4).Visible = true;
			}
			else
			{
				((Control)pbInfo4).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo4).Visible = false;
		}
		if (!isLoading)
		{
			GlobalSettings.Rotation4(GlobalSettings.CurrentLoadedClass, cmbRotation4.SelectedItem.ToString());
			((SettingsBase)Settings.Default).Save();
		}
	}

	private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
	{
	}

	private void cmbStartRotation_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbStartRotation.SelectedItem != null)
		{
			GlobalSettings.Sound_StartRotation = cmbStartRotation.SelectedItem.ToString();
			Settings.Default.StartRotation = GlobalSettings.Sound_StartRotation;
			SaveSettings();
		}
	}

	private void cmbChangeRotation_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbChangeRotation.SelectedItem != null)
		{
			GlobalSettings.Sound_ChangeRotation = cmbChangeRotation.SelectedItem.ToString();
			Settings.Default.ChangeRotation = GlobalSettings.Sound_ChangeRotation;
			SaveSettings();
			GlobalSettings.LoadBot = true;
		}
	}

	private void cmbStopRotation_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbStopRotation.SelectedItem != null)
		{
			GlobalSettings.Sound_StopRotation = cmbStopRotation.SelectedItem.ToString();
			Settings.Default.EndRotation = GlobalSettings.Sound_StopRotation;
			SaveSettings();
			GlobalSettings.LoadBot = true;
		}
	}

	private void cmbStartInterrupt_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbStartInterrupt.SelectedItem != null)
		{
			GlobalSettings.Sound_StartInterrupt = cmbStartInterrupt.SelectedItem.ToString();
			Settings.Default.StartInterrupt = GlobalSettings.Sound_StartInterrupt;
			SaveSettings();
			GlobalSettings.LoadBot = true;
		}
	}

	private void cmbStopInterrupt_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbStopInterrupt.SelectedItem != null)
		{
			GlobalSettings.Sound_StopInterrupt = cmbStopInterrupt.SelectedItem.ToString();
			Settings.Default.StopInterrupt = GlobalSettings.Sound_StopInterrupt;
			SaveSettings();
			GlobalSettings.LoadBot = true;
		}
	}

	private void chkShowMessages_CheckedChanged(object sender, EventArgs e)
	{
		Settings.Default.ShowMessages = chkShowMessages.Checked;
		((SettingsBase)Settings.Default).Save();
		GlobalSettings.ShowMessages = chkShowMessages.Checked;
	}

	private void btnSmartHelp_Click(object sender, EventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		string text = "\r\nThis is the function you will call via script command to execute the next ability in your rotation. The default value is PQR_SmartHotkey.\r\n\r\nExample Usage:\r\n    /script PQR_SmartHotkey(1)\r\n    --This macro will cast the next ability in rotation 1.\r\n\r\n    /script PQR_SmartHotkey(3)\r\n    --This macro will cast the next ability in rotation 3.\r\n\r\n    /script MyCustomName(2)\r\n    --This macro will cast the next ability in rotation 2 if you have changed the value to MyCustomName.\r\n\r\nWARNING: It is highly recommended that you DO NOT SAVE MACROS with the default name value.";
		MessageBox.Show(text);
	}

	private void txtSmartCommand_TextChanged(object sender, EventArgs e)
	{
		GlobalSettings.PQR_SmartHotkeyCommand = ((Control)txtSmartCommand).Text.Trim();
		Settings.Default.SmartHotkey = ((Control)txtSmartCommand).Text;
		((SettingsBase)Settings.Default).Save();
	}

	private void lblSmartHotkey_Click(object sender, EventArgs e)
	{
		if (GlobalSettings.HotkeyEditorLoaded)
		{
			((Form)Program.hotkeyForm).Close();
		}
		Program.hotkeyForm = new frmHotkeyEditor();
		GlobalSettings.EditingHotkey = 5;
		((Control)Program.hotkeyForm).Text = "Hotkey for Smart Hotkey";
		((Control)Program.hotkeyForm).Show();
	}

	private void tbInterruptDelay_Scroll(object sender, EventArgs e)
	{
		GlobalSettings.InterruptDelay = tbInterruptDelay.Value;
		Settings.Default.InterruptDelay = GlobalSettings.InterruptDelay;
		((SettingsBase)Settings.Default).Save();
		((Control)lblInterruptDelay).Text = tbInterruptDelay.Value + "ms";
	}

	private void pbInfo1_Click(object sender, EventArgs e)
	{
		DisplayNotes(cmbRotation1.SelectedItem.ToString());
	}

	private void pbInfo2_Click(object sender, EventArgs e)
	{
		DisplayNotes(cmbRotation2.SelectedItem.ToString());
	}

	private void pbInfo3_Click(object sender, EventArgs e)
	{
		DisplayNotes(cmbRotation3.SelectedItem.ToString());
	}

	private void pbInfo4_Click(object sender, EventArgs e)
	{
		DisplayNotes(cmbRotation4.SelectedItem.ToString());
	}

	private void DisplayNotes(string sRotation)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		string text = "";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == null)
			{
				return;
			}
			if (rotationArray[i, 0] == sRotation)
			{
				if (rotationArray[i, 4].Trim() == "")
				{
					return;
				}
				text = rotationArray[i, 4];
				break;
			}
		}
		MessageBox.Show(text, sRotation + " Notes");
	}

	private void pbRequireCombat_Click(object sender, EventArgs e)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		string text = "It is recommended to allow the profile designer to designate the need for a combat requirement as some profiles execute functions while out of combat. A well designed profile will cast pre-buffs while out of combat and not pull mobs without a reason to.\r\n\r\nLeave this setting on Rotation Designated for the recommended setting.\r\n\r\nIf you are an advanced user, you may change this option to either True, forcing your character to be in combat to do anything, or False, allowing your character to be automated while out of combat.\r\n";
		MessageBox.Show(text, "Require Combat - Information");
	}

	private void cmbRequireCombat_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbRequireCombat.SelectedItem != null)
		{
			string text = cmbRequireCombat.SelectedItem.ToString();
			switch (text)
			{
			case "":
			case "Rotation Designated":
				GlobalSettings.RequireCombat = true;
				GlobalSettings.AllowRotationDesignation = true;
				break;
			case "True":
				GlobalSettings.RequireCombat = true;
				GlobalSettings.AllowRotationDesignation = false;
				break;
			case "False":
				GlobalSettings.RequireCombat = false;
				GlobalSettings.AllowRotationDesignation = false;
				break;
			default:
				GlobalSettings.RequireCombat = true;
				GlobalSettings.AllowRotationDesignation = true;
				break;
			}
			Settings.Default.CombatRequirement = text;
			((SettingsBase)Settings.Default).Save();
			GlobalSettings.LoadBot = true;
		}
	}

	private void cmbRotation1_SelectedValueChanged(object sender, EventArgs e)
	{
		if (cmbRotation1.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation1.SelectedItem.ToString()))
			{
				((Control)pbInfo1).Visible = true;
			}
			else
			{
				((Control)pbInfo1).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo1).Visible = false;
		}
	}

	private void cmbRotation2_SelectedValueChanged(object sender, EventArgs e)
	{
		if (cmbRotation2.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation2.SelectedItem.ToString()))
			{
				((Control)pbInfo2).Visible = true;
			}
			else
			{
				((Control)pbInfo2).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo2).Visible = false;
		}
	}

	private void cmbRotation3_SelectedValueChanged(object sender, EventArgs e)
	{
		if (cmbRotation3.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation3.SelectedItem.ToString()))
			{
				((Control)pbInfo3).Visible = true;
			}
			else
			{
				((Control)pbInfo3).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo3).Visible = false;
		}
	}

	private void cmbRotation4_SelectedValueChanged(object sender, EventArgs e)
	{
		if (cmbRotation4.SelectedItem != null)
		{
			if (RotationHasNotes(cmbRotation4.SelectedItem.ToString()))
			{
				((Control)pbInfo4).Visible = true;
			}
			else
			{
				((Control)pbInfo4).Visible = false;
			}
		}
		else
		{
			((Control)pbInfo4).Visible = false;
		}
	}

	private void label12_Click(object sender, EventArgs e)
	{
		UnregisterHotkeys();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Expected O, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Expected O, but got Unknown
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Expected O, but got Unknown
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Expected O, but got Unknown
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Expected O, but got Unknown
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Expected O, but got Unknown
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Expected O, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Expected O, but got Unknown
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Expected O, but got Unknown
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Expected O, but got Unknown
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Expected O, but got Unknown
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Expected O, but got Unknown
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Expected O, but got Unknown
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Expected O, but got Unknown
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Expected O, but got Unknown
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Expected O, but got Unknown
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Expected O, but got Unknown
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Expected O, but got Unknown
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Expected O, but got Unknown
		//IL_0d3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d45: Expected O, but got Unknown
		//IL_0de3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ded: Expected O, but got Unknown
		//IL_0e8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e95: Expected O, but got Unknown
		//IL_0f33: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f3d: Expected O, but got Unknown
		//IL_0fdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fe5: Expected O, but got Unknown
		//IL_4613: Unknown result type (might be due to invalid IL or missing references)
		//IL_461d: Expected O, but got Unknown
		//IL_50c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_50d0: Expected O, but got Unknown
		//IL_50d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_50e2: Expected O, but got Unknown
		components = new Container();
		tmrMain = new System.Windows.Forms.Timer(components);
		bwExecute = new BackgroundWorker();
		btnRotationEditor = new Button();
		label4 = new Label();
		cmbRotation2 = new ComboBox();
		label2 = new Label();
		cmbRotation1 = new ComboBox();
		label1 = new Label();
		btnAbilityEditor = new Button();
		txtAddSpell = new TextBox();
		chkInterruptAll = new CheckBox();
		btnAddSelected = new Button();
		btnDelete = new Button();
		chkSpellList = new CheckedListBox();
		chkDebug = new CheckBox();
		lblRefreshRate = new Label();
		tbRefreshRate = new TrackBar();
		lblRefreshRateValue = new Label();
		btnSettings = new Button();
		label3 = new Label();
		label6 = new Label();
		cmbRotation4 = new ComboBox();
		cmbRotation3 = new ComboBox();
		lblHotkey1 = new Label();
		lblHotkey2 = new Label();
		lblHotkey3 = new Label();
		lblHotkey4 = new Label();
		lblHotkeyInterrupt = new Label();
		label7 = new Label();
		label8 = new Label();
		label9 = new Label();
		cmbStartRotation = new ComboBox();
		cmbChangeRotation = new ComboBox();
		cmbStopRotation = new ComboBox();
		cmbStopInterrupt = new ComboBox();
		cmbStartInterrupt = new ComboBox();
		lblChangeSound = new Label();
		label11 = new Label();
		label10 = new Label();
		chkShowMessages = new CheckBox();
		lblSmartHotkey = new Label();
		label12 = new Label();
		txtSmartCommand = new TextBox();
		label13 = new Label();
		btnSmartHelp = new Button();
		label5 = new Label();
		tbInterruptDelay = new TrackBar();
		lblInterruptDelay = new Label();
		pbInfo2 = new PictureBox();
		pbInfo1 = new PictureBox();
		pbInfo3 = new PictureBox();
		pbInfo4 = new PictureBox();
		label14 = new Label();
		cmbRequireCombat = new ComboBox();
		pbRequireCombat = new PictureBox();
		((ISupportInitialize)tbRefreshRate).BeginInit();
		((ISupportInitialize)tbInterruptDelay).BeginInit();
		((ISupportInitialize)pbInfo2).BeginInit();
		((ISupportInitialize)pbInfo1).BeginInit();
		((ISupportInitialize)pbInfo3).BeginInit();
		((ISupportInitialize)pbInfo4).BeginInit();
		((ISupportInitialize)pbRequireCombat).BeginInit();
		((Control)this).SuspendLayout();
		tmrMain.Tick += tmrMain_Tick;
		bwExecute.WorkerReportsProgress = true;
		bwExecute.WorkerSupportsCancellation = true;
		bwExecute.DoWork += bwExecute_DoWork;
		((Control)btnRotationEditor).Location = new Point(203, 146);
		((Control)btnRotationEditor).Name = "btnRotationEditor";
		((Control)btnRotationEditor).Size = new Size(161, 26);
		((Control)btnRotationEditor).TabIndex = 62;
		((Control)btnRotationEditor).Text = "Profile and Rotation Editor";
		((ButtonBase)btnRotationEditor).UseVisualStyleBackColor = true;
		((Control)btnRotationEditor).Click += btnRotationEditor_Click;
		((Control)label4).AutoSize = true;
		((Control)label4).Location = new Point(12, 178);
		((Control)label4).Name = "label4";
		((Control)label4).Size = new Size(79, 13);
		((Control)label4).TabIndex = 60;
		((Control)label4).Text = "Interrupt Mode:";
		cmbRotation2.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbRotation2).FormattingEnabled = true;
		((Control)cmbRotation2).Location = new Point(77, 36);
		((Control)cmbRotation2).Name = "cmbRotation2";
		((Control)cmbRotation2).Size = new Size(145, 21);
		((Control)cmbRotation2).TabIndex = 59;
		cmbRotation2.SelectedIndexChanged += cmbRotation2_SelectedIndexChanged;
		((ListControl)cmbRotation2).SelectedValueChanged += cmbRotation2_SelectedValueChanged;
		((Control)label2).AutoSize = true;
		((Control)label2).Location = new Point(12, 39);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(59, 13);
		((Control)label2).TabIndex = 58;
		((Control)label2).Text = "Rotation 2:";
		cmbRotation1.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbRotation1).FormattingEnabled = true;
		((Control)cmbRotation1).Location = new Point(77, 9);
		((Control)cmbRotation1).Name = "cmbRotation1";
		((Control)cmbRotation1).Size = new Size(145, 21);
		((Control)cmbRotation1).TabIndex = 57;
		cmbRotation1.SelectedIndexChanged += cmbRotation1_SelectedIndexChanged;
		((ListControl)cmbRotation1).SelectedValueChanged += cmbRotation1_SelectedValueChanged;
		((Control)label1).AutoSize = true;
		((Control)label1).Location = new Point(12, 12);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(59, 13);
		((Control)label1).TabIndex = 56;
		((Control)label1).Text = "Rotation 1:";
		((Control)btnAbilityEditor).Location = new Point(203, 178);
		((Control)btnAbilityEditor).Name = "btnAbilityEditor";
		((Control)btnAbilityEditor).Size = new Size(161, 26);
		((Control)btnAbilityEditor).TabIndex = 61;
		((Control)btnAbilityEditor).Text = "Ability Editor";
		((ButtonBase)btnAbilityEditor).UseVisualStyleBackColor = true;
		((Control)btnAbilityEditor).Click += btnAbilityEditor_Click;
		((TextBoxBase)txtAddSpell).AcceptsTab = true;
		((Control)txtAddSpell).CausesValidation = false;
		((Control)txtAddSpell).Location = new Point(203, 222);
		((TextBoxBase)txtAddSpell).MaxLength = 40;
		((Control)txtAddSpell).Name = "txtAddSpell";
		((Control)txtAddSpell).Size = new Size(161, 20);
		((Control)txtAddSpell).TabIndex = 53;
		((Control)chkInterruptAll).AutoSize = true;
		((Control)chkInterruptAll).Location = new Point(203, 312);
		((Control)chkInterruptAll).Name = "chkInterruptAll";
		((Control)chkInterruptAll).Size = new Size(110, 17);
		((Control)chkInterruptAll).TabIndex = 55;
		((Control)chkInterruptAll).Text = "Interrupt All Spells";
		((ButtonBase)chkInterruptAll).UseVisualStyleBackColor = true;
		chkInterruptAll.CheckedChanged += chkInterruptAll_CheckedChanged;
		((Control)btnAddSelected).Location = new Point(203, 248);
		((Control)btnAddSelected).Name = "btnAddSelected";
		((Control)btnAddSelected).Size = new Size(161, 26);
		((Control)btnAddSelected).TabIndex = 52;
		((Control)btnAddSelected).Text = "Add Spell";
		((ButtonBase)btnAddSelected).UseVisualStyleBackColor = true;
		((Control)btnAddSelected).Click += btnAddSelected_Click;
		((Control)btnDelete).Location = new Point(203, 280);
		((Control)btnDelete).Name = "btnDelete";
		((Control)btnDelete).Size = new Size(161, 26);
		((Control)btnDelete).TabIndex = 51;
		((Control)btnDelete).Text = "Delete Selected";
		((ButtonBase)btnDelete).UseVisualStyleBackColor = true;
		((Control)btnDelete).Click += btnDelete_Click;
		((ListControl)chkSpellList).FormattingEnabled = true;
		((Control)chkSpellList).Location = new Point(15, 194);
		((Control)chkSpellList).Name = "chkSpellList";
		((Control)chkSpellList).Size = new Size(175, 139);
		((Control)chkSpellList).TabIndex = 50;
		((ListBox)chkSpellList).SelectedIndexChanged += chkSpellList_SelectedIndexChanged;
		((ListControl)chkSpellList).SelectedValueChanged += chkSpellList_SelectedValueChanged;
		((Control)chkDebug).AutoSize = true;
		((Control)chkDebug).Location = new Point(581, 304);
		((Control)chkDebug).Name = "chkDebug";
		((Control)chkDebug).Size = new Size(88, 17);
		((Control)chkDebug).TabIndex = 63;
		((Control)chkDebug).Text = "Debug Mode";
		((ButtonBase)chkDebug).UseVisualStyleBackColor = true;
		chkDebug.CheckedChanged += chkDebug_CheckedChanged;
		((Control)lblRefreshRate).AutoSize = true;
		((Control)lblRefreshRate).Location = new Point(379, 71);
		((Control)lblRefreshRate).Name = "lblRefreshRate";
		((Control)lblRefreshRate).Size = new Size(101, 13);
		((Control)lblRefreshRate).TabIndex = 64;
		((Control)lblRefreshRate).Text = "Ability Check Delay:";
		((Control)tbRefreshRate).BackColor = SystemColors.Control;
		((Control)tbRefreshRate).Location = new Point(513, 72);
		tbRefreshRate.Maximum = 1000;
		tbRefreshRate.Minimum = 20;
		((Control)tbRefreshRate).Name = "tbRefreshRate";
		((Control)tbRefreshRate).Size = new Size(163, 45);
		((Control)tbRefreshRate).TabIndex = 65;
		tbRefreshRate.Value = 100;
		tbRefreshRate.Scroll += tbRefreshRate_Scroll;
		((Control)lblRefreshRateValue).AutoSize = true;
		((Control)lblRefreshRateValue).Location = new Point(379, 84);
		((Control)lblRefreshRateValue).Name = "lblRefreshRateValue";
		((Control)lblRefreshRateValue).Size = new Size(38, 13);
		((Control)lblRefreshRateValue).TabIndex = 66;
		((Control)lblRefreshRateValue).Text = "100ms";
		((Control)btnSettings).Location = new Point(15, 146);
		((Control)btnSettings).Name = "btnSettings";
		((Control)btnSettings).Size = new Size(161, 26);
		((Control)btnSettings).TabIndex = 68;
		((Control)btnSettings).Text = "Show Settings";
		((ButtonBase)btnSettings).UseVisualStyleBackColor = true;
		((Control)btnSettings).Click += btnSettings_Click;
		((Control)label3).AutoSize = true;
		((Control)label3).Location = new Point(12, 93);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(59, 13);
		((Control)label3).TabIndex = 71;
		((Control)label3).Text = "Rotation 4:";
		((Control)label6).AutoSize = true;
		((Control)label6).Location = new Point(12, 66);
		((Control)label6).Name = "label6";
		((Control)label6).Size = new Size(59, 13);
		((Control)label6).TabIndex = 70;
		((Control)label6).Text = "Rotation 3:";
		cmbRotation4.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbRotation4).FormattingEnabled = true;
		((Control)cmbRotation4).Location = new Point(77, 90);
		((Control)cmbRotation4).Name = "cmbRotation4";
		((Control)cmbRotation4).Size = new Size(145, 21);
		((Control)cmbRotation4).TabIndex = 73;
		cmbRotation4.SelectedIndexChanged += cmbRotation4_SelectedIndexChanged;
		((ListControl)cmbRotation4).SelectedValueChanged += cmbRotation4_SelectedValueChanged;
		cmbRotation3.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbRotation3).FormattingEnabled = true;
		((Control)cmbRotation3).Location = new Point(77, 63);
		((Control)cmbRotation3).Name = "cmbRotation3";
		((Control)cmbRotation3).Size = new Size(145, 21);
		((Control)cmbRotation3).TabIndex = 72;
		cmbRotation3.SelectedIndexChanged += cmbRotation3_SelectedIndexChanged;
		((ListControl)cmbRotation3).SelectedValueChanged += cmbRotation3_SelectedValueChanged;
		((Control)lblHotkey1).AutoSize = true;
		((Control)lblHotkey1).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblHotkey1).ForeColor = SystemColors.Highlight;
		((Control)lblHotkey1).Location = new Point(255, 12);
		((Control)lblHotkey1).Name = "lblHotkey1";
		((Control)lblHotkey1).Size = new Size(77, 13);
		((Control)lblHotkey1).TabIndex = 74;
		((Control)lblHotkey1).Text = "Hotkey: (none)";
		((Control)lblHotkey1).Click += lblHotkey1_Click;
		((Control)lblHotkey2).AutoSize = true;
		((Control)lblHotkey2).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblHotkey2).ForeColor = SystemColors.Highlight;
		((Control)lblHotkey2).Location = new Point(255, 39);
		((Control)lblHotkey2).Name = "lblHotkey2";
		((Control)lblHotkey2).Size = new Size(77, 13);
		((Control)lblHotkey2).TabIndex = 75;
		((Control)lblHotkey2).Text = "Hotkey: (none)";
		((Control)lblHotkey2).Click += lblHotkey2_Click;
		((Control)lblHotkey3).AutoSize = true;
		((Control)lblHotkey3).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblHotkey3).ForeColor = SystemColors.Highlight;
		((Control)lblHotkey3).Location = new Point(255, 68);
		((Control)lblHotkey3).Name = "lblHotkey3";
		((Control)lblHotkey3).Size = new Size(77, 13);
		((Control)lblHotkey3).TabIndex = 76;
		((Control)lblHotkey3).Text = "Hotkey: (none)";
		((Control)lblHotkey3).Click += lblHotkey3_Click;
		((Control)lblHotkey4).AutoSize = true;
		((Control)lblHotkey4).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblHotkey4).ForeColor = SystemColors.Highlight;
		((Control)lblHotkey4).Location = new Point(255, 93);
		((Control)lblHotkey4).Name = "lblHotkey4";
		((Control)lblHotkey4).Size = new Size(77, 13);
		((Control)lblHotkey4).TabIndex = 77;
		((Control)lblHotkey4).Text = "Hotkey: (none)";
		((Control)lblHotkey4).Click += lblHotkey4_Click;
		((Control)lblHotkeyInterrupt).AutoSize = true;
		((Control)lblHotkeyInterrupt).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblHotkeyInterrupt).ForeColor = SystemColors.Highlight;
		((Control)lblHotkeyInterrupt).Location = new Point(90, 178);
		((Control)lblHotkeyInterrupt).Name = "lblHotkeyInterrupt";
		((Control)lblHotkeyInterrupt).Size = new Size(77, 13);
		((Control)lblHotkeyInterrupt).TabIndex = 78;
		((Control)lblHotkeyInterrupt).Text = "Hotkey: (none)";
		((Control)lblHotkeyInterrupt).Click += lblHotkeyInterrupt_Click;
		((Control)label7).AutoSize = true;
		((Control)label7).Location = new Point(379, 156);
		((Control)label7).Name = "label7";
		((Control)label7).Size = new Size(109, 13);
		((Control)label7).TabIndex = 79;
		((Control)label7).Text = "Sound Start Rotation:";
		((Control)label8).AutoSize = true;
		((Control)label8).Location = new Point(379, 205);
		((Control)label8).Name = "label8";
		((Control)label8).Size = new Size(103, 13);
		((Control)label8).TabIndex = 80;
		((Control)label8).Text = "Sound End Rotation";
		((Control)label9).AutoSize = true;
		((Control)label9).Location = new Point(379, 180);
		((Control)label9).Name = "label9";
		((Control)label9).Size = new Size(124, 13);
		((Control)label9).TabIndex = 81;
		((Control)label9).Text = "Sound Change Rotation:";
		cmbStartRotation.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbStartRotation).FormattingEnabled = true;
		cmbStartRotation.Items.AddRange(new object[236]
		{
			"NONE", "PVPENTERQUEUE", "PVPTHROUGHQUEUE", "GLUESCREENSMALLBUTTONMOUSEDOWN ", "GLUESCREENSMALLBUTTONMOUSEUP ", "GLUESCREENSMALLBUTTONMOUSEOVER ", "GLUESCREENMEDIUMBUTTONMOUSEDOWN ", "GLUESCREENMEDIUMBUTTONMOUSEUP ", "GLUESCREENMEDIUMBUTTONMOUSEOVER ", "GLUESCREENLARGEBUTTONMOUSEDOWN ",
			"GLUESCREENLARGEBUTTONMOUSEUP ", "GLUESCREENLARGEBUTTONMOUSEOVER ", "GLUESCREENEDITBOXKEYCLICK ", "GLUECHECKBOXMOUSEDOWN ", "GLUECHECKBOXMOUSEUP ", "GLUECHECKBOXMOUSEOVER ", "GLUECHARCUSTOMIZATIONMOUSEDOWN ", "GLUECHARCUSTOMIZATIONMOUSEUP ", "GLUECHARCUSTOMIZATIONMOUSEOVER ", "GLUESCROLLBUTTONMOUSEDOWN ",
			"GLUESCROLLBUTTONMOUSEUP ", "GLUESCROLLBUTTONMOUSEOVER ", "GAMEABILITYBUTTONMOUSEDOWN ", "GAMESPELLBUTTONMOUSEDOWN ", "GAMEWINDOWOPEN ", "GAMEWINDOWCLOSE ", "GAMEDIALOGOPEN ", "GAMEDIALOGCLOSE ", "GAMENEWWINDOWTAB ", "GAMESCREENSMALLBUTTONMOUSEDOWN ",
			"GAMESCREENSMALLBUTTONMOUSEUP ", "GAMESCREENSMALLBUTTONMOUSEOVER ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMESCREENMEDIUMBUTTONMOUSEUP ", "GAMESCREENMEDIUMBUTTONMOUSEOVER ", "GAMESCREENLARGEBUTTONMOUSEDOWN ", "GAMESCREENLARGEBUTTONMOUSEUP ", "GAMESCREENLARGEBUTTONMOUSEOVER ", "GAMETARGETFRIENDLYUNIT ", "GAMETARGETHOSTILEUNIT ",
			"GAMETARGETNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEINITIALATTACK ", "GAMEERROROUTOFRANGE ", "GAMEERROROUTOFMANA ", "GAMEERRORUNABLETOEQUIP ", "GAMEERRORINVALIDTARGET ", "ACTIONBARBUTTONDOWN ",
			"MAINBUTTONBARMENU ", "MINIMAPZOOMOUT ", "MINIMAPZOOMIN ", "MINIMAPOPEN ", "MINIMAPCLOSE ", "BAGMENUBUTTONPRESS ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "ITEMWEAPONSOUND ",
			"ITEMARMORSOUND ", "ITEMGENERICSOUND ", "LEVELUPSOUND ", "GLUECREATECHARACTERBUTTON ", "GLUEENTERWORLDBUTTON ", "SPELLBOOKOPEN ", "SPELLBOOKCLOSE ", "SPELLBOOKCHANGEPAGE ", "PAPERDOLLOPEN ", "PAPERDOLLCLOSE ",
			"QUESTADDED ", "QUESTCOMPLETED ", "QUESTLOGOPEN ", "QUESTLOGCLOSE ", "GLUEGENERICBUTTONPRESS ", "GAMEGENERICBUTTONPRESS ", "INTERFACESOUND_MONEYFRAMEOPEN ", "INTERFACESOUND_MONEYFRAMECLOSE ", "INTERFACESOUND_CHARWINDOWOPEN ", "INTERFACESOUND_CHARWINDOWCLOSE ",
			"INTERFACESOUND_CHARWINDOWTAB ", "INTERFACESOUND_GAMEMENUOPEN ", "INTERFACESOUND_GAMEMENUCLOSE ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_BACKPACKOPEN ", "INTERFACESOUND_BACKPACKCLOSE ", "INTERFACESOUND_GAMESCROLLBUTTON ", "INTERFACESOUND_CURSORGRABOBJECT ", "INTERFACESOUND_CURSORDROPOBJECT ", "SHEATHINGSHIELDSHEATHE ",
			"SHEATHINGWOODWEAPONSHEATHE ", "SHEATHINGMETALWEAPONSHEATHE ", "SHEATHINGWOODWEAPONUNSHEATHE ", "SHEATHINGMETALWEAPONUNSHEATHE ", "SHEATHINGSHIELDUNSHEATHE ", "igCreatureAggroDeselect ", "igQuestListOpen ", "igQuestListClose ", "igQuestListSelect ", "igQuestListComplete ",
			"igQuestCancel ", "igPlayerInvite ", "igPlayerInviteAccept ", "igPlayerInviteDecline ", "GAMEERRORUNABLETOEQUIP ", "ITEMGENERICSOUND ", "GAMEERRORINVALIDTARGET ", "LEVELUP ", "GAMEERROROUTOFRANGE ", "QUESTADDED ",
			"MONEYFRAMEOPEN ", "MONEYFRAMECLOSE ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_CURSORGRABOBJECT ",
			"INTERFACESOUND_CURSORDROPOBJECT ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMEABILITYACTIVATE ", "GAMESPELLACTIVATE ", "gsTitleEnterWorld ", "gsTitleOptions ", "gsTitleQuit ", "gsTitleCredits ", "gsTitleIntroMovie ", "gsTitleOptionScreenResolution ",
			"gsTitleOption16bit ", "gsTitleOption32bit ", "gsTitleOptionOpenGL ", "gsTitleOptionDirect3D ", "gsTitleOptionFullScreenMode ", "gsTitleOptionOK ", "gsTitleOptionExit ", "gsLogin ", "gsLoginNewAccount ", "gsLoginChangeRealm ",
			"gsLoginExit ", "gsLoginChangeRealmOK ", "gsLoginChangeRealmSelect ", "gsLoginChangeRealmCancel ", "gsCharacterSelection ", "gsCharacterSelectionEnterWorld ", "gsCharacterSelectionDelCharacter ", "gsCharacterSelectionAcctOptions ", "gsCharacterSelectionExit ", "gsCharacterSelectionCreateNew ",
			"gsCharacterCreationClass ", "gsCharacterCreationRace ", "gsCharacterCreationGender ", "gsCharacterCreationLook ", "gsCharacterCreationCreateChar ", "gsCharacterCreationCancel ", "igCurrentActiveSpell", "igMiniMapOpen ", "igMiniMapClose ", "igMiniMapZoomIn ",
			"igMiniMapZoomOut ", "igChatEmoteButton ", "igChatScrollUp ", "igChatScrollDown ", "igChatBottom ", "igSpellBookOpen ", "igSpellBookClose  ", "igSpellBokPageTurn ", "igSpellBookSpellIconPickup ", "igSpellBookSpellIconDrop ",
			"igAbilityOpen ", "igAbilityClose ", "igAbiliityPageTurn ", "igAbilityIconPickup ", "igAbilityIconDrop ", "TalentScreenOpen", "TalentScreenClose", "igCharacterInfoOpen ", "igCharacterInfoClose ", "igCharacterInfoTab ",
			"igCharacterInfoScrollUp ", "igCharacterInfoScrollDown ", "igQuestLogOpen ", "igQuestLogClose ", "igQuestLogAbandonQuest ", "igQuestFailed ", "igSocialOepn", "igSocialClose ", "igMainMenuOpen ", "igMainMenuClose ",
			"igMainMenuOption ", "igMainMenuLogout ", "igMainMenuQuit ", "igMainMenuContinue ", "igMainMenuOptionCheckBoxOn ", "igMainMenuOptionCheckBoxOff ", "igMainMenuOptionFaerTab ", "igInventoryOepn", "igInventoryClose ", "igInventoryRotateCharacter ",
			"igBackPackOpen ", "igBackPackClose ", "igBackPackCoinSelect ", "igBackPackCoinOK ", "igBackPackCoinCancel ", "igCharacterNPCSelect ", "igCharacterNPCDeselect ", "igCharacterSelect ", "igCharacterDeselect ", "igCreatureNeutralSelect ",
			"igCreatureNeutralDeselect ", "igCreatureAggroSelect ", "UChatScrollButton ", "Deathbind Sound ", "LOOTWINDOWOPENEMPTY ", "TaxiNodeDiscovered ", "UnwrapGift ", "TellMessage ", "WriteQuest ", "MapPing ",
			"igBonusBarOpen ", "FriendJoinGame ", "Fishing Reel in ", "HumanExploration ", "OrcExploration ", "UndeadExploration ", "TaurenExploration ", "TrollExploration ", "NightElfExploration ", "GnomeExploration ",
			"DwarfExploration ", "igPVPUpdate", "ReadyCheck", "RaidWarning", "AuctionWindowOpen", "AuctionWindowClose"
		});
		((Control)cmbStartRotation).Location = new Point(513, 153);
		((Control)cmbStartRotation).Name = "cmbStartRotation";
		((Control)cmbStartRotation).Size = new Size(157, 21);
		((Control)cmbStartRotation).TabIndex = 82;
		cmbStartRotation.SelectedIndexChanged += cmbStartRotation_SelectedIndexChanged;
		cmbChangeRotation.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbChangeRotation).FormattingEnabled = true;
		cmbChangeRotation.Items.AddRange(new object[236]
		{
			"NONE", "PVPENTERQUEUE", "PVPTHROUGHQUEUE", "GLUESCREENSMALLBUTTONMOUSEDOWN ", "GLUESCREENSMALLBUTTONMOUSEUP ", "GLUESCREENSMALLBUTTONMOUSEOVER ", "GLUESCREENMEDIUMBUTTONMOUSEDOWN ", "GLUESCREENMEDIUMBUTTONMOUSEUP ", "GLUESCREENMEDIUMBUTTONMOUSEOVER ", "GLUESCREENLARGEBUTTONMOUSEDOWN ",
			"GLUESCREENLARGEBUTTONMOUSEUP ", "GLUESCREENLARGEBUTTONMOUSEOVER ", "GLUESCREENEDITBOXKEYCLICK ", "GLUECHECKBOXMOUSEDOWN ", "GLUECHECKBOXMOUSEUP ", "GLUECHECKBOXMOUSEOVER ", "GLUECHARCUSTOMIZATIONMOUSEDOWN ", "GLUECHARCUSTOMIZATIONMOUSEUP ", "GLUECHARCUSTOMIZATIONMOUSEOVER ", "GLUESCROLLBUTTONMOUSEDOWN ",
			"GLUESCROLLBUTTONMOUSEUP ", "GLUESCROLLBUTTONMOUSEOVER ", "GAMEABILITYBUTTONMOUSEDOWN ", "GAMESPELLBUTTONMOUSEDOWN ", "GAMEWINDOWOPEN ", "GAMEWINDOWCLOSE ", "GAMEDIALOGOPEN ", "GAMEDIALOGCLOSE ", "GAMENEWWINDOWTAB ", "GAMESCREENSMALLBUTTONMOUSEDOWN ",
			"GAMESCREENSMALLBUTTONMOUSEUP ", "GAMESCREENSMALLBUTTONMOUSEOVER ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMESCREENMEDIUMBUTTONMOUSEUP ", "GAMESCREENMEDIUMBUTTONMOUSEOVER ", "GAMESCREENLARGEBUTTONMOUSEDOWN ", "GAMESCREENLARGEBUTTONMOUSEUP ", "GAMESCREENLARGEBUTTONMOUSEOVER ", "GAMETARGETFRIENDLYUNIT ", "GAMETARGETHOSTILEUNIT ",
			"GAMETARGETNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEINITIALATTACK ", "GAMEERROROUTOFRANGE ", "GAMEERROROUTOFMANA ", "GAMEERRORUNABLETOEQUIP ", "GAMEERRORINVALIDTARGET ", "ACTIONBARBUTTONDOWN ",
			"MAINBUTTONBARMENU ", "MINIMAPZOOMOUT ", "MINIMAPZOOMIN ", "MINIMAPOPEN ", "MINIMAPCLOSE ", "BAGMENUBUTTONPRESS ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "ITEMWEAPONSOUND ",
			"ITEMARMORSOUND ", "ITEMGENERICSOUND ", "LEVELUPSOUND ", "GLUECREATECHARACTERBUTTON ", "GLUEENTERWORLDBUTTON ", "SPELLBOOKOPEN ", "SPELLBOOKCLOSE ", "SPELLBOOKCHANGEPAGE ", "PAPERDOLLOPEN ", "PAPERDOLLCLOSE ",
			"QUESTADDED ", "QUESTCOMPLETED ", "QUESTLOGOPEN ", "QUESTLOGCLOSE ", "GLUEGENERICBUTTONPRESS ", "GAMEGENERICBUTTONPRESS ", "INTERFACESOUND_MONEYFRAMEOPEN ", "INTERFACESOUND_MONEYFRAMECLOSE ", "INTERFACESOUND_CHARWINDOWOPEN ", "INTERFACESOUND_CHARWINDOWCLOSE ",
			"INTERFACESOUND_CHARWINDOWTAB ", "INTERFACESOUND_GAMEMENUOPEN ", "INTERFACESOUND_GAMEMENUCLOSE ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_BACKPACKOPEN ", "INTERFACESOUND_BACKPACKCLOSE ", "INTERFACESOUND_GAMESCROLLBUTTON ", "INTERFACESOUND_CURSORGRABOBJECT ", "INTERFACESOUND_CURSORDROPOBJECT ", "SHEATHINGSHIELDSHEATHE ",
			"SHEATHINGWOODWEAPONSHEATHE ", "SHEATHINGMETALWEAPONSHEATHE ", "SHEATHINGWOODWEAPONUNSHEATHE ", "SHEATHINGMETALWEAPONUNSHEATHE ", "SHEATHINGSHIELDUNSHEATHE ", "igCreatureAggroDeselect ", "igQuestListOpen ", "igQuestListClose ", "igQuestListSelect ", "igQuestListComplete ",
			"igQuestCancel ", "igPlayerInvite ", "igPlayerInviteAccept ", "igPlayerInviteDecline ", "GAMEERRORUNABLETOEQUIP ", "ITEMGENERICSOUND ", "GAMEERRORINVALIDTARGET ", "LEVELUP ", "GAMEERROROUTOFRANGE ", "QUESTADDED ",
			"MONEYFRAMEOPEN ", "MONEYFRAMECLOSE ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_CURSORGRABOBJECT ",
			"INTERFACESOUND_CURSORDROPOBJECT ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMEABILITYACTIVATE ", "GAMESPELLACTIVATE ", "gsTitleEnterWorld ", "gsTitleOptions ", "gsTitleQuit ", "gsTitleCredits ", "gsTitleIntroMovie ", "gsTitleOptionScreenResolution ",
			"gsTitleOption16bit ", "gsTitleOption32bit ", "gsTitleOptionOpenGL ", "gsTitleOptionDirect3D ", "gsTitleOptionFullScreenMode ", "gsTitleOptionOK ", "gsTitleOptionExit ", "gsLogin ", "gsLoginNewAccount ", "gsLoginChangeRealm ",
			"gsLoginExit ", "gsLoginChangeRealmOK ", "gsLoginChangeRealmSelect ", "gsLoginChangeRealmCancel ", "gsCharacterSelection ", "gsCharacterSelectionEnterWorld ", "gsCharacterSelectionDelCharacter ", "gsCharacterSelectionAcctOptions ", "gsCharacterSelectionExit ", "gsCharacterSelectionCreateNew ",
			"gsCharacterCreationClass ", "gsCharacterCreationRace ", "gsCharacterCreationGender ", "gsCharacterCreationLook ", "gsCharacterCreationCreateChar ", "gsCharacterCreationCancel ", "igCurrentActiveSpell", "igMiniMapOpen ", "igMiniMapClose ", "igMiniMapZoomIn ",
			"igMiniMapZoomOut ", "igChatEmoteButton ", "igChatScrollUp ", "igChatScrollDown ", "igChatBottom ", "igSpellBookOpen ", "igSpellBookClose  ", "igSpellBokPageTurn ", "igSpellBookSpellIconPickup ", "igSpellBookSpellIconDrop ",
			"igAbilityOpen ", "igAbilityClose ", "igAbiliityPageTurn ", "igAbilityIconPickup ", "igAbilityIconDrop ", "TalentScreenOpen", "TalentScreenClose", "igCharacterInfoOpen ", "igCharacterInfoClose ", "igCharacterInfoTab ",
			"igCharacterInfoScrollUp ", "igCharacterInfoScrollDown ", "igQuestLogOpen ", "igQuestLogClose ", "igQuestLogAbandonQuest ", "igQuestFailed ", "igSocialOepn", "igSocialClose ", "igMainMenuOpen ", "igMainMenuClose ",
			"igMainMenuOption ", "igMainMenuLogout ", "igMainMenuQuit ", "igMainMenuContinue ", "igMainMenuOptionCheckBoxOn ", "igMainMenuOptionCheckBoxOff ", "igMainMenuOptionFaerTab ", "igInventoryOepn", "igInventoryClose ", "igInventoryRotateCharacter ",
			"igBackPackOpen ", "igBackPackClose ", "igBackPackCoinSelect ", "igBackPackCoinOK ", "igBackPackCoinCancel ", "igCharacterNPCSelect ", "igCharacterNPCDeselect ", "igCharacterSelect ", "igCharacterDeselect ", "igCreatureNeutralSelect ",
			"igCreatureNeutralDeselect ", "igCreatureAggroSelect ", "UChatScrollButton ", "Deathbind Sound ", "LOOTWINDOWOPENEMPTY ", "TaxiNodeDiscovered ", "UnwrapGift ", "TellMessage ", "WriteQuest ", "MapPing ",
			"igBonusBarOpen ", "FriendJoinGame ", "Fishing Reel in ", "HumanExploration ", "OrcExploration ", "UndeadExploration ", "TaurenExploration ", "TrollExploration ", "NightElfExploration ", "GnomeExploration ",
			"DwarfExploration ", "igPVPUpdate", "ReadyCheck", "RaidWarning", "AuctionWindowOpen", "AuctionWindowClose"
		});
		((Control)cmbChangeRotation).Location = new Point(513, 177);
		((Control)cmbChangeRotation).Name = "cmbChangeRotation";
		((Control)cmbChangeRotation).Size = new Size(157, 21);
		((Control)cmbChangeRotation).TabIndex = 83;
		cmbChangeRotation.SelectedIndexChanged += cmbChangeRotation_SelectedIndexChanged;
		cmbStopRotation.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbStopRotation).FormattingEnabled = true;
		cmbStopRotation.Items.AddRange(new object[236]
		{
			"NONE", "PVPENTERQUEUE", "PVPTHROUGHQUEUE", "GLUESCREENSMALLBUTTONMOUSEDOWN ", "GLUESCREENSMALLBUTTONMOUSEUP ", "GLUESCREENSMALLBUTTONMOUSEOVER ", "GLUESCREENMEDIUMBUTTONMOUSEDOWN ", "GLUESCREENMEDIUMBUTTONMOUSEUP ", "GLUESCREENMEDIUMBUTTONMOUSEOVER ", "GLUESCREENLARGEBUTTONMOUSEDOWN ",
			"GLUESCREENLARGEBUTTONMOUSEUP ", "GLUESCREENLARGEBUTTONMOUSEOVER ", "GLUESCREENEDITBOXKEYCLICK ", "GLUECHECKBOXMOUSEDOWN ", "GLUECHECKBOXMOUSEUP ", "GLUECHECKBOXMOUSEOVER ", "GLUECHARCUSTOMIZATIONMOUSEDOWN ", "GLUECHARCUSTOMIZATIONMOUSEUP ", "GLUECHARCUSTOMIZATIONMOUSEOVER ", "GLUESCROLLBUTTONMOUSEDOWN ",
			"GLUESCROLLBUTTONMOUSEUP ", "GLUESCROLLBUTTONMOUSEOVER ", "GAMEABILITYBUTTONMOUSEDOWN ", "GAMESPELLBUTTONMOUSEDOWN ", "GAMEWINDOWOPEN ", "GAMEWINDOWCLOSE ", "GAMEDIALOGOPEN ", "GAMEDIALOGCLOSE ", "GAMENEWWINDOWTAB ", "GAMESCREENSMALLBUTTONMOUSEDOWN ",
			"GAMESCREENSMALLBUTTONMOUSEUP ", "GAMESCREENSMALLBUTTONMOUSEOVER ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMESCREENMEDIUMBUTTONMOUSEUP ", "GAMESCREENMEDIUMBUTTONMOUSEOVER ", "GAMESCREENLARGEBUTTONMOUSEDOWN ", "GAMESCREENLARGEBUTTONMOUSEUP ", "GAMESCREENLARGEBUTTONMOUSEOVER ", "GAMETARGETFRIENDLYUNIT ", "GAMETARGETHOSTILEUNIT ",
			"GAMETARGETNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEINITIALATTACK ", "GAMEERROROUTOFRANGE ", "GAMEERROROUTOFMANA ", "GAMEERRORUNABLETOEQUIP ", "GAMEERRORINVALIDTARGET ", "ACTIONBARBUTTONDOWN ",
			"MAINBUTTONBARMENU ", "MINIMAPZOOMOUT ", "MINIMAPZOOMIN ", "MINIMAPOPEN ", "MINIMAPCLOSE ", "BAGMENUBUTTONPRESS ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "ITEMWEAPONSOUND ",
			"ITEMARMORSOUND ", "ITEMGENERICSOUND ", "LEVELUPSOUND ", "GLUECREATECHARACTERBUTTON ", "GLUEENTERWORLDBUTTON ", "SPELLBOOKOPEN ", "SPELLBOOKCLOSE ", "SPELLBOOKCHANGEPAGE ", "PAPERDOLLOPEN ", "PAPERDOLLCLOSE ",
			"QUESTADDED ", "QUESTCOMPLETED ", "QUESTLOGOPEN ", "QUESTLOGCLOSE ", "GLUEGENERICBUTTONPRESS ", "GAMEGENERICBUTTONPRESS ", "INTERFACESOUND_MONEYFRAMEOPEN ", "INTERFACESOUND_MONEYFRAMECLOSE ", "INTERFACESOUND_CHARWINDOWOPEN ", "INTERFACESOUND_CHARWINDOWCLOSE ",
			"INTERFACESOUND_CHARWINDOWTAB ", "INTERFACESOUND_GAMEMENUOPEN ", "INTERFACESOUND_GAMEMENUCLOSE ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_BACKPACKOPEN ", "INTERFACESOUND_BACKPACKCLOSE ", "INTERFACESOUND_GAMESCROLLBUTTON ", "INTERFACESOUND_CURSORGRABOBJECT ", "INTERFACESOUND_CURSORDROPOBJECT ", "SHEATHINGSHIELDSHEATHE ",
			"SHEATHINGWOODWEAPONSHEATHE ", "SHEATHINGMETALWEAPONSHEATHE ", "SHEATHINGWOODWEAPONUNSHEATHE ", "SHEATHINGMETALWEAPONUNSHEATHE ", "SHEATHINGSHIELDUNSHEATHE ", "igCreatureAggroDeselect ", "igQuestListOpen ", "igQuestListClose ", "igQuestListSelect ", "igQuestListComplete ",
			"igQuestCancel ", "igPlayerInvite ", "igPlayerInviteAccept ", "igPlayerInviteDecline ", "GAMEERRORUNABLETOEQUIP ", "ITEMGENERICSOUND ", "GAMEERRORINVALIDTARGET ", "LEVELUP ", "GAMEERROROUTOFRANGE ", "QUESTADDED ",
			"MONEYFRAMEOPEN ", "MONEYFRAMECLOSE ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_CURSORGRABOBJECT ",
			"INTERFACESOUND_CURSORDROPOBJECT ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMEABILITYACTIVATE ", "GAMESPELLACTIVATE ", "gsTitleEnterWorld ", "gsTitleOptions ", "gsTitleQuit ", "gsTitleCredits ", "gsTitleIntroMovie ", "gsTitleOptionScreenResolution ",
			"gsTitleOption16bit ", "gsTitleOption32bit ", "gsTitleOptionOpenGL ", "gsTitleOptionDirect3D ", "gsTitleOptionFullScreenMode ", "gsTitleOptionOK ", "gsTitleOptionExit ", "gsLogin ", "gsLoginNewAccount ", "gsLoginChangeRealm ",
			"gsLoginExit ", "gsLoginChangeRealmOK ", "gsLoginChangeRealmSelect ", "gsLoginChangeRealmCancel ", "gsCharacterSelection ", "gsCharacterSelectionEnterWorld ", "gsCharacterSelectionDelCharacter ", "gsCharacterSelectionAcctOptions ", "gsCharacterSelectionExit ", "gsCharacterSelectionCreateNew ",
			"gsCharacterCreationClass ", "gsCharacterCreationRace ", "gsCharacterCreationGender ", "gsCharacterCreationLook ", "gsCharacterCreationCreateChar ", "gsCharacterCreationCancel ", "igCurrentActiveSpell", "igMiniMapOpen ", "igMiniMapClose ", "igMiniMapZoomIn ",
			"igMiniMapZoomOut ", "igChatEmoteButton ", "igChatScrollUp ", "igChatScrollDown ", "igChatBottom ", "igSpellBookOpen ", "igSpellBookClose  ", "igSpellBokPageTurn ", "igSpellBookSpellIconPickup ", "igSpellBookSpellIconDrop ",
			"igAbilityOpen ", "igAbilityClose ", "igAbiliityPageTurn ", "igAbilityIconPickup ", "igAbilityIconDrop ", "TalentScreenOpen", "TalentScreenClose", "igCharacterInfoOpen ", "igCharacterInfoClose ", "igCharacterInfoTab ",
			"igCharacterInfoScrollUp ", "igCharacterInfoScrollDown ", "igQuestLogOpen ", "igQuestLogClose ", "igQuestLogAbandonQuest ", "igQuestFailed ", "igSocialOepn", "igSocialClose ", "igMainMenuOpen ", "igMainMenuClose ",
			"igMainMenuOption ", "igMainMenuLogout ", "igMainMenuQuit ", "igMainMenuContinue ", "igMainMenuOptionCheckBoxOn ", "igMainMenuOptionCheckBoxOff ", "igMainMenuOptionFaerTab ", "igInventoryOepn", "igInventoryClose ", "igInventoryRotateCharacter ",
			"igBackPackOpen ", "igBackPackClose ", "igBackPackCoinSelect ", "igBackPackCoinOK ", "igBackPackCoinCancel ", "igCharacterNPCSelect ", "igCharacterNPCDeselect ", "igCharacterSelect ", "igCharacterDeselect ", "igCreatureNeutralSelect ",
			"igCreatureNeutralDeselect ", "igCreatureAggroSelect ", "UChatScrollButton ", "Deathbind Sound ", "LOOTWINDOWOPENEMPTY ", "TaxiNodeDiscovered ", "UnwrapGift ", "TellMessage ", "WriteQuest ", "MapPing ",
			"igBonusBarOpen ", "FriendJoinGame ", "Fishing Reel in ", "HumanExploration ", "OrcExploration ", "UndeadExploration ", "TaurenExploration ", "TrollExploration ", "NightElfExploration ", "GnomeExploration ",
			"DwarfExploration ", "igPVPUpdate", "ReadyCheck", "RaidWarning", "AuctionWindowOpen", "AuctionWindowClose"
		});
		((Control)cmbStopRotation).Location = new Point(513, 202);
		((Control)cmbStopRotation).Name = "cmbStopRotation";
		((Control)cmbStopRotation).Size = new Size(157, 21);
		((Control)cmbStopRotation).TabIndex = 84;
		cmbStopRotation.SelectedIndexChanged += cmbStopRotation_SelectedIndexChanged;
		cmbStopInterrupt.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbStopInterrupt).FormattingEnabled = true;
		cmbStopInterrupt.Items.AddRange(new object[236]
		{
			"NONE", "PVPENTERQUEUE", "PVPTHROUGHQUEUE", "GLUESCREENSMALLBUTTONMOUSEDOWN ", "GLUESCREENSMALLBUTTONMOUSEUP ", "GLUESCREENSMALLBUTTONMOUSEOVER ", "GLUESCREENMEDIUMBUTTONMOUSEDOWN ", "GLUESCREENMEDIUMBUTTONMOUSEUP ", "GLUESCREENMEDIUMBUTTONMOUSEOVER ", "GLUESCREENLARGEBUTTONMOUSEDOWN ",
			"GLUESCREENLARGEBUTTONMOUSEUP ", "GLUESCREENLARGEBUTTONMOUSEOVER ", "GLUESCREENEDITBOXKEYCLICK ", "GLUECHECKBOXMOUSEDOWN ", "GLUECHECKBOXMOUSEUP ", "GLUECHECKBOXMOUSEOVER ", "GLUECHARCUSTOMIZATIONMOUSEDOWN ", "GLUECHARCUSTOMIZATIONMOUSEUP ", "GLUECHARCUSTOMIZATIONMOUSEOVER ", "GLUESCROLLBUTTONMOUSEDOWN ",
			"GLUESCROLLBUTTONMOUSEUP ", "GLUESCROLLBUTTONMOUSEOVER ", "GAMEABILITYBUTTONMOUSEDOWN ", "GAMESPELLBUTTONMOUSEDOWN ", "GAMEWINDOWOPEN ", "GAMEWINDOWCLOSE ", "GAMEDIALOGOPEN ", "GAMEDIALOGCLOSE ", "GAMENEWWINDOWTAB ", "GAMESCREENSMALLBUTTONMOUSEDOWN ",
			"GAMESCREENSMALLBUTTONMOUSEUP ", "GAMESCREENSMALLBUTTONMOUSEOVER ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMESCREENMEDIUMBUTTONMOUSEUP ", "GAMESCREENMEDIUMBUTTONMOUSEOVER ", "GAMESCREENLARGEBUTTONMOUSEDOWN ", "GAMESCREENLARGEBUTTONMOUSEUP ", "GAMESCREENLARGEBUTTONMOUSEOVER ", "GAMETARGETFRIENDLYUNIT ", "GAMETARGETHOSTILEUNIT ",
			"GAMETARGETNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEINITIALATTACK ", "GAMEERROROUTOFRANGE ", "GAMEERROROUTOFMANA ", "GAMEERRORUNABLETOEQUIP ", "GAMEERRORINVALIDTARGET ", "ACTIONBARBUTTONDOWN ",
			"MAINBUTTONBARMENU ", "MINIMAPZOOMOUT ", "MINIMAPZOOMIN ", "MINIMAPOPEN ", "MINIMAPCLOSE ", "BAGMENUBUTTONPRESS ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "ITEMWEAPONSOUND ",
			"ITEMARMORSOUND ", "ITEMGENERICSOUND ", "LEVELUPSOUND ", "GLUECREATECHARACTERBUTTON ", "GLUEENTERWORLDBUTTON ", "SPELLBOOKOPEN ", "SPELLBOOKCLOSE ", "SPELLBOOKCHANGEPAGE ", "PAPERDOLLOPEN ", "PAPERDOLLCLOSE ",
			"QUESTADDED ", "QUESTCOMPLETED ", "QUESTLOGOPEN ", "QUESTLOGCLOSE ", "GLUEGENERICBUTTONPRESS ", "GAMEGENERICBUTTONPRESS ", "INTERFACESOUND_MONEYFRAMEOPEN ", "INTERFACESOUND_MONEYFRAMECLOSE ", "INTERFACESOUND_CHARWINDOWOPEN ", "INTERFACESOUND_CHARWINDOWCLOSE ",
			"INTERFACESOUND_CHARWINDOWTAB ", "INTERFACESOUND_GAMEMENUOPEN ", "INTERFACESOUND_GAMEMENUCLOSE ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_BACKPACKOPEN ", "INTERFACESOUND_BACKPACKCLOSE ", "INTERFACESOUND_GAMESCROLLBUTTON ", "INTERFACESOUND_CURSORGRABOBJECT ", "INTERFACESOUND_CURSORDROPOBJECT ", "SHEATHINGSHIELDSHEATHE ",
			"SHEATHINGWOODWEAPONSHEATHE ", "SHEATHINGMETALWEAPONSHEATHE ", "SHEATHINGWOODWEAPONUNSHEATHE ", "SHEATHINGMETALWEAPONUNSHEATHE ", "SHEATHINGSHIELDUNSHEATHE ", "igCreatureAggroDeselect ", "igQuestListOpen ", "igQuestListClose ", "igQuestListSelect ", "igQuestListComplete ",
			"igQuestCancel ", "igPlayerInvite ", "igPlayerInviteAccept ", "igPlayerInviteDecline ", "GAMEERRORUNABLETOEQUIP ", "ITEMGENERICSOUND ", "GAMEERRORINVALIDTARGET ", "LEVELUP ", "GAMEERROROUTOFRANGE ", "QUESTADDED ",
			"MONEYFRAMEOPEN ", "MONEYFRAMECLOSE ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_CURSORGRABOBJECT ",
			"INTERFACESOUND_CURSORDROPOBJECT ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMEABILITYACTIVATE ", "GAMESPELLACTIVATE ", "gsTitleEnterWorld ", "gsTitleOptions ", "gsTitleQuit ", "gsTitleCredits ", "gsTitleIntroMovie ", "gsTitleOptionScreenResolution ",
			"gsTitleOption16bit ", "gsTitleOption32bit ", "gsTitleOptionOpenGL ", "gsTitleOptionDirect3D ", "gsTitleOptionFullScreenMode ", "gsTitleOptionOK ", "gsTitleOptionExit ", "gsLogin ", "gsLoginNewAccount ", "gsLoginChangeRealm ",
			"gsLoginExit ", "gsLoginChangeRealmOK ", "gsLoginChangeRealmSelect ", "gsLoginChangeRealmCancel ", "gsCharacterSelection ", "gsCharacterSelectionEnterWorld ", "gsCharacterSelectionDelCharacter ", "gsCharacterSelectionAcctOptions ", "gsCharacterSelectionExit ", "gsCharacterSelectionCreateNew ",
			"gsCharacterCreationClass ", "gsCharacterCreationRace ", "gsCharacterCreationGender ", "gsCharacterCreationLook ", "gsCharacterCreationCreateChar ", "gsCharacterCreationCancel ", "igCurrentActiveSpell", "igMiniMapOpen ", "igMiniMapClose ", "igMiniMapZoomIn ",
			"igMiniMapZoomOut ", "igChatEmoteButton ", "igChatScrollUp ", "igChatScrollDown ", "igChatBottom ", "igSpellBookOpen ", "igSpellBookClose  ", "igSpellBokPageTurn ", "igSpellBookSpellIconPickup ", "igSpellBookSpellIconDrop ",
			"igAbilityOpen ", "igAbilityClose ", "igAbiliityPageTurn ", "igAbilityIconPickup ", "igAbilityIconDrop ", "TalentScreenOpen", "TalentScreenClose", "igCharacterInfoOpen ", "igCharacterInfoClose ", "igCharacterInfoTab ",
			"igCharacterInfoScrollUp ", "igCharacterInfoScrollDown ", "igQuestLogOpen ", "igQuestLogClose ", "igQuestLogAbandonQuest ", "igQuestFailed ", "igSocialOepn", "igSocialClose ", "igMainMenuOpen ", "igMainMenuClose ",
			"igMainMenuOption ", "igMainMenuLogout ", "igMainMenuQuit ", "igMainMenuContinue ", "igMainMenuOptionCheckBoxOn ", "igMainMenuOptionCheckBoxOff ", "igMainMenuOptionFaerTab ", "igInventoryOepn", "igInventoryClose ", "igInventoryRotateCharacter ",
			"igBackPackOpen ", "igBackPackClose ", "igBackPackCoinSelect ", "igBackPackCoinOK ", "igBackPackCoinCancel ", "igCharacterNPCSelect ", "igCharacterNPCDeselect ", "igCharacterSelect ", "igCharacterDeselect ", "igCreatureNeutralSelect ",
			"igCreatureNeutralDeselect ", "igCreatureAggroSelect ", "UChatScrollButton ", "Deathbind Sound ", "LOOTWINDOWOPENEMPTY ", "TaxiNodeDiscovered ", "UnwrapGift ", "TellMessage ", "WriteQuest ", "MapPing ",
			"igBonusBarOpen ", "FriendJoinGame ", "Fishing Reel in ", "HumanExploration ", "OrcExploration ", "UndeadExploration ", "TaurenExploration ", "TrollExploration ", "NightElfExploration ", "GnomeExploration ",
			"DwarfExploration ", "igPVPUpdate", "ReadyCheck", "RaidWarning", "AuctionWindowOpen", "AuctionWindowClose"
		});
		((Control)cmbStopInterrupt).Location = new Point(512, 251);
		((Control)cmbStopInterrupt).Name = "cmbStopInterrupt";
		((Control)cmbStopInterrupt).Size = new Size(157, 21);
		((Control)cmbStopInterrupt).TabIndex = 88;
		cmbStopInterrupt.SelectedIndexChanged += cmbStopInterrupt_SelectedIndexChanged;
		cmbStartInterrupt.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbStartInterrupt).FormattingEnabled = true;
		cmbStartInterrupt.Items.AddRange(new object[236]
		{
			"NONE", "PVPENTERQUEUE", "PVPTHROUGHQUEUE", "GLUESCREENSMALLBUTTONMOUSEDOWN ", "GLUESCREENSMALLBUTTONMOUSEUP ", "GLUESCREENSMALLBUTTONMOUSEOVER ", "GLUESCREENMEDIUMBUTTONMOUSEDOWN ", "GLUESCREENMEDIUMBUTTONMOUSEUP ", "GLUESCREENMEDIUMBUTTONMOUSEOVER ", "GLUESCREENLARGEBUTTONMOUSEDOWN ",
			"GLUESCREENLARGEBUTTONMOUSEUP ", "GLUESCREENLARGEBUTTONMOUSEOVER ", "GLUESCREENEDITBOXKEYCLICK ", "GLUECHECKBOXMOUSEDOWN ", "GLUECHECKBOXMOUSEUP ", "GLUECHECKBOXMOUSEOVER ", "GLUECHARCUSTOMIZATIONMOUSEDOWN ", "GLUECHARCUSTOMIZATIONMOUSEUP ", "GLUECHARCUSTOMIZATIONMOUSEOVER ", "GLUESCROLLBUTTONMOUSEDOWN ",
			"GLUESCROLLBUTTONMOUSEUP ", "GLUESCROLLBUTTONMOUSEOVER ", "GAMEABILITYBUTTONMOUSEDOWN ", "GAMESPELLBUTTONMOUSEDOWN ", "GAMEWINDOWOPEN ", "GAMEWINDOWCLOSE ", "GAMEDIALOGOPEN ", "GAMEDIALOGCLOSE ", "GAMENEWWINDOWTAB ", "GAMESCREENSMALLBUTTONMOUSEDOWN ",
			"GAMESCREENSMALLBUTTONMOUSEUP ", "GAMESCREENSMALLBUTTONMOUSEOVER ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMESCREENMEDIUMBUTTONMOUSEUP ", "GAMESCREENMEDIUMBUTTONMOUSEOVER ", "GAMESCREENLARGEBUTTONMOUSEDOWN ", "GAMESCREENLARGEBUTTONMOUSEUP ", "GAMESCREENLARGEBUTTONMOUSEOVER ", "GAMETARGETFRIENDLYUNIT ", "GAMETARGETHOSTILEUNIT ",
			"GAMETARGETNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEINITIALATTACK ", "GAMEERROROUTOFRANGE ", "GAMEERROROUTOFMANA ", "GAMEERRORUNABLETOEQUIP ", "GAMEERRORINVALIDTARGET ", "ACTIONBARBUTTONDOWN ",
			"MAINBUTTONBARMENU ", "MINIMAPZOOMOUT ", "MINIMAPZOOMIN ", "MINIMAPOPEN ", "MINIMAPCLOSE ", "BAGMENUBUTTONPRESS ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "ITEMWEAPONSOUND ",
			"ITEMARMORSOUND ", "ITEMGENERICSOUND ", "LEVELUPSOUND ", "GLUECREATECHARACTERBUTTON ", "GLUEENTERWORLDBUTTON ", "SPELLBOOKOPEN ", "SPELLBOOKCLOSE ", "SPELLBOOKCHANGEPAGE ", "PAPERDOLLOPEN ", "PAPERDOLLCLOSE ",
			"QUESTADDED ", "QUESTCOMPLETED ", "QUESTLOGOPEN ", "QUESTLOGCLOSE ", "GLUEGENERICBUTTONPRESS ", "GAMEGENERICBUTTONPRESS ", "INTERFACESOUND_MONEYFRAMEOPEN ", "INTERFACESOUND_MONEYFRAMECLOSE ", "INTERFACESOUND_CHARWINDOWOPEN ", "INTERFACESOUND_CHARWINDOWCLOSE ",
			"INTERFACESOUND_CHARWINDOWTAB ", "INTERFACESOUND_GAMEMENUOPEN ", "INTERFACESOUND_GAMEMENUCLOSE ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_BACKPACKOPEN ", "INTERFACESOUND_BACKPACKCLOSE ", "INTERFACESOUND_GAMESCROLLBUTTON ", "INTERFACESOUND_CURSORGRABOBJECT ", "INTERFACESOUND_CURSORDROPOBJECT ", "SHEATHINGSHIELDSHEATHE ",
			"SHEATHINGWOODWEAPONSHEATHE ", "SHEATHINGMETALWEAPONSHEATHE ", "SHEATHINGWOODWEAPONUNSHEATHE ", "SHEATHINGMETALWEAPONUNSHEATHE ", "SHEATHINGSHIELDUNSHEATHE ", "igCreatureAggroDeselect ", "igQuestListOpen ", "igQuestListClose ", "igQuestListSelect ", "igQuestListComplete ",
			"igQuestCancel ", "igPlayerInvite ", "igPlayerInviteAccept ", "igPlayerInviteDecline ", "GAMEERRORUNABLETOEQUIP ", "ITEMGENERICSOUND ", "GAMEERRORINVALIDTARGET ", "LEVELUP ", "GAMEERROROUTOFRANGE ", "QUESTADDED ",
			"MONEYFRAMEOPEN ", "MONEYFRAMECLOSE ", "LOOTWINDOWOPEN ", "LOOTWINDOWCLOSE ", "LOOTWINDOWCOINSOUND ", "GAMEHIGHLIGHTHOSTILEUNIT ", "GAMEHIGHLIGHTNEUTRALUNIT ", "GAMEHIGHLIGHTFRIENDLYUNIT ", "INTERFACESOUND_LOSTTARGETUNIT ", "INTERFACESOUND_CURSORGRABOBJECT ",
			"INTERFACESOUND_CURSORDROPOBJECT ", "GAMESCREENMEDIUMBUTTONMOUSEDOWN ", "GAMEABILITYACTIVATE ", "GAMESPELLACTIVATE ", "gsTitleEnterWorld ", "gsTitleOptions ", "gsTitleQuit ", "gsTitleCredits ", "gsTitleIntroMovie ", "gsTitleOptionScreenResolution ",
			"gsTitleOption16bit ", "gsTitleOption32bit ", "gsTitleOptionOpenGL ", "gsTitleOptionDirect3D ", "gsTitleOptionFullScreenMode ", "gsTitleOptionOK ", "gsTitleOptionExit ", "gsLogin ", "gsLoginNewAccount ", "gsLoginChangeRealm ",
			"gsLoginExit ", "gsLoginChangeRealmOK ", "gsLoginChangeRealmSelect ", "gsLoginChangeRealmCancel ", "gsCharacterSelection ", "gsCharacterSelectionEnterWorld ", "gsCharacterSelectionDelCharacter ", "gsCharacterSelectionAcctOptions ", "gsCharacterSelectionExit ", "gsCharacterSelectionCreateNew ",
			"gsCharacterCreationClass ", "gsCharacterCreationRace ", "gsCharacterCreationGender ", "gsCharacterCreationLook ", "gsCharacterCreationCreateChar ", "gsCharacterCreationCancel ", "igCurrentActiveSpell", "igMiniMapOpen ", "igMiniMapClose ", "igMiniMapZoomIn ",
			"igMiniMapZoomOut ", "igChatEmoteButton ", "igChatScrollUp ", "igChatScrollDown ", "igChatBottom ", "igSpellBookOpen ", "igSpellBookClose  ", "igSpellBokPageTurn ", "igSpellBookSpellIconPickup ", "igSpellBookSpellIconDrop ",
			"igAbilityOpen ", "igAbilityClose ", "igAbiliityPageTurn ", "igAbilityIconPickup ", "igAbilityIconDrop ", "TalentScreenOpen", "TalentScreenClose", "igCharacterInfoOpen ", "igCharacterInfoClose ", "igCharacterInfoTab ",
			"igCharacterInfoScrollUp ", "igCharacterInfoScrollDown ", "igQuestLogOpen ", "igQuestLogClose ", "igQuestLogAbandonQuest ", "igQuestFailed ", "igSocialOepn", "igSocialClose ", "igMainMenuOpen ", "igMainMenuClose ",
			"igMainMenuOption ", "igMainMenuLogout ", "igMainMenuQuit ", "igMainMenuContinue ", "igMainMenuOptionCheckBoxOn ", "igMainMenuOptionCheckBoxOff ", "igMainMenuOptionFaerTab ", "igInventoryOepn", "igInventoryClose ", "igInventoryRotateCharacter ",
			"igBackPackOpen ", "igBackPackClose ", "igBackPackCoinSelect ", "igBackPackCoinOK ", "igBackPackCoinCancel ", "igCharacterNPCSelect ", "igCharacterNPCDeselect ", "igCharacterSelect ", "igCharacterDeselect ", "igCreatureNeutralSelect ",
			"igCreatureNeutralDeselect ", "igCreatureAggroSelect ", "UChatScrollButton ", "Deathbind Sound ", "LOOTWINDOWOPENEMPTY ", "TaxiNodeDiscovered ", "UnwrapGift ", "TellMessage ", "WriteQuest ", "MapPing ",
			"igBonusBarOpen ", "FriendJoinGame ", "Fishing Reel in ", "HumanExploration ", "OrcExploration ", "UndeadExploration ", "TaurenExploration ", "TrollExploration ", "NightElfExploration ", "GnomeExploration ",
			"DwarfExploration ", "igPVPUpdate", "ReadyCheck", "RaidWarning", "AuctionWindowOpen", "AuctionWindowClose"
		});
		((Control)cmbStartInterrupt).Location = new Point(512, 226);
		((Control)cmbStartInterrupt).Name = "cmbStartInterrupt";
		((Control)cmbStartInterrupt).Size = new Size(157, 21);
		((Control)cmbStartInterrupt).TabIndex = 87;
		cmbStartInterrupt.SelectedIndexChanged += cmbStartInterrupt_SelectedIndexChanged;
		((Control)lblChangeSound).AutoSize = true;
		((Control)lblChangeSound).Location = new Point(379, 229);
		((Control)lblChangeSound).Name = "lblChangeSound";
		((Control)lblChangeSound).Size = new Size(108, 13);
		((Control)lblChangeSound).TabIndex = 86;
		((Control)lblChangeSound).Text = "Sound Start Interrupt:";
		((Control)label11).AutoSize = true;
		((Control)label11).Location = new Point(379, 254);
		((Control)label11).Name = "label11";
		((Control)label11).Size = new Size(108, 13);
		((Control)label11).TabIndex = 85;
		((Control)label11).Text = "Sound Stop Interrupt:";
		((Control)label10).AutoSize = true;
		((Control)label10).Location = new Point(397, 283);
		((Control)label10).Name = "label10";
		((Control)label10).Size = new Size(260, 13);
		((Control)label10).TabIndex = 89;
		((Control)label10).Text = "To disable sound select \"NONE\" from the drop-down.";
		((Control)chkShowMessages).AutoSize = true;
		((Control)chkShowMessages).Location = new Point(382, 304);
		((Control)chkShowMessages).Name = "chkShowMessages";
		((Control)chkShowMessages).Size = new Size(140, 17);
		((Control)chkShowMessages).TabIndex = 90;
		((Control)chkShowMessages).Text = "Show Messages in Chat";
		((ButtonBase)chkShowMessages).UseVisualStyleBackColor = true;
		chkShowMessages.CheckedChanged += chkShowMessages_CheckedChanged;
		((Control)lblSmartHotkey).AutoSize = true;
		((Control)lblSmartHotkey).Font = new Font("Microsoft Sans Serif", 8.25f, (FontStyle)4, (GraphicsUnit)3, (byte)0);
		((Control)lblSmartHotkey).ForeColor = SystemColors.Highlight;
		((Control)lblSmartHotkey).Location = new Point(255, 121);
		((Control)lblSmartHotkey).Name = "lblSmartHotkey";
		((Control)lblSmartHotkey).Size = new Size(77, 13);
		((Control)lblSmartHotkey).TabIndex = 92;
		((Control)lblSmartHotkey).Text = "Hotkey: (none)";
		((Control)lblSmartHotkey).Click += lblSmartHotkey_Click;
		((Control)label12).AutoSize = true;
		((Control)label12).Location = new Point(79, 121);
		((Control)label12).Name = "label12";
		((Control)label12).Size = new Size(111, 13);
		((Control)label12).TabIndex = 93;
		((Control)label12).Text = "Enable Manual Mode:";
		((Control)label12).Click += label12_Click;
		((Control)txtSmartCommand).Location = new Point(513, 9);
		((Control)txtSmartCommand).Name = "txtSmartCommand";
		((Control)txtSmartCommand).Size = new Size(94, 20);
		((Control)txtSmartCommand).TabIndex = 94;
		((Control)txtSmartCommand).TextChanged += txtSmartCommand_TextChanged;
		((Control)label13).AutoSize = true;
		((Control)label13).Location = new Point(379, 12);
		((Control)label13).Name = "label13";
		((Control)label13).Size = new Size(131, 13);
		((Control)label13).TabIndex = 95;
		((Control)label13).Text = "Smart Hotkey Macro Cmd:";
		((Control)btnSmartHelp).Location = new Point(613, 7);
		((Control)btnSmartHelp).Name = "btnSmartHelp";
		((Control)btnSmartHelp).Size = new Size(57, 22);
		((Control)btnSmartHelp).TabIndex = 96;
		((Control)btnSmartHelp).Text = "Help";
		((ButtonBase)btnSmartHelp).UseVisualStyleBackColor = true;
		((Control)btnSmartHelp).Click += btnSmartHelp_Click;
		((Control)label5).AutoSize = true;
		((Control)label5).Location = new Point(379, 104);
		((Control)label5).Name = "label5";
		((Control)label5).Size = new Size(79, 13);
		((Control)label5).TabIndex = 98;
		((Control)label5).Text = "Interrupt Delay:";
		((Control)tbInterruptDelay).BackColor = SystemColors.Control;
		((Control)tbInterruptDelay).Location = new Point(513, 104);
		tbInterruptDelay.Maximum = 1000;
		((Control)tbInterruptDelay).Name = "tbInterruptDelay";
		((Control)tbInterruptDelay).Size = new Size(163, 45);
		((Control)tbInterruptDelay).TabIndex = 99;
		tbInterruptDelay.Scroll += tbInterruptDelay_Scroll;
		((Control)lblInterruptDelay).AutoSize = true;
		((Control)lblInterruptDelay).Location = new Point(379, 117);
		((Control)lblInterruptDelay).Name = "lblInterruptDelay";
		((Control)lblInterruptDelay).Size = new Size(26, 13);
		((Control)lblInterruptDelay).TabIndex = 100;
		((Control)lblInterruptDelay).Text = "0ms";
		((Control)pbInfo2).Location = new Point(228, 36);
		((Control)pbInfo2).Name = "pbInfo2";
		((Control)pbInfo2).Size = new Size(25, 25);
		pbInfo2.TabIndex = 103;
		pbInfo2.TabStop = false;
		((Control)pbInfo2).Click += pbInfo2_Click;
		((Control)pbInfo1).Location = new Point(228, 9);
		((Control)pbInfo1).Name = "pbInfo1";
		((Control)pbInfo1).Size = new Size(25, 25);
		pbInfo1.TabIndex = 104;
		pbInfo1.TabStop = false;
		((Control)pbInfo1).Click += pbInfo1_Click;
		((Control)pbInfo3).Location = new Point(228, 62);
		((Control)pbInfo3).Name = "pbInfo3";
		((Control)pbInfo3).Size = new Size(25, 25);
		pbInfo3.TabIndex = 105;
		pbInfo3.TabStop = false;
		((Control)pbInfo3).Click += pbInfo3_Click;
		((Control)pbInfo4).Location = new Point(228, 90);
		((Control)pbInfo4).Name = "pbInfo4";
		((Control)pbInfo4).Size = new Size(25, 25);
		pbInfo4.TabIndex = 106;
		pbInfo4.TabStop = false;
		((Control)pbInfo4).Click += pbInfo4_Click;
		((Control)label14).AutoSize = true;
		((Control)label14).Location = new Point(379, 39);
		((Control)label14).Name = "label14";
		((Control)label14).Size = new Size(86, 13);
		((Control)label14).TabIndex = 107;
		((Control)label14).Text = "Require Combat:";
		cmbRequireCombat.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbRequireCombat).FormattingEnabled = true;
		cmbRequireCombat.Items.AddRange(new object[3] { "Rotation Designated", "True", "False" });
		((Control)cmbRequireCombat).Location = new Point(513, 36);
		((Control)cmbRequireCombat).Name = "cmbRequireCombat";
		((Control)cmbRequireCombat).Size = new Size(157, 21);
		((Control)cmbRequireCombat).TabIndex = 108;
		cmbRequireCombat.SelectedIndexChanged += cmbRequireCombat_SelectedIndexChanged;
		((Control)pbRequireCombat).Location = new Point(471, 32);
		((Control)pbRequireCombat).Name = "pbRequireCombat";
		((Control)pbRequireCombat).Size = new Size(25, 25);
		pbRequireCombat.TabIndex = 109;
		pbRequireCombat.TabStop = false;
		((Control)pbRequireCombat).Click += pbRequireCombat_Click;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(369, 338);
		((Control)this).Controls.Add((Control)(object)pbRequireCombat);
		((Control)this).Controls.Add((Control)(object)cmbRequireCombat);
		((Control)this).Controls.Add((Control)(object)label14);
		((Control)this).Controls.Add((Control)(object)pbInfo4);
		((Control)this).Controls.Add((Control)(object)pbInfo3);
		((Control)this).Controls.Add((Control)(object)pbInfo1);
		((Control)this).Controls.Add((Control)(object)pbInfo2);
		((Control)this).Controls.Add((Control)(object)label5);
		((Control)this).Controls.Add((Control)(object)tbInterruptDelay);
		((Control)this).Controls.Add((Control)(object)lblInterruptDelay);
		((Control)this).Controls.Add((Control)(object)btnSmartHelp);
		((Control)this).Controls.Add((Control)(object)label13);
		((Control)this).Controls.Add((Control)(object)txtSmartCommand);
		((Control)this).Controls.Add((Control)(object)label12);
		((Control)this).Controls.Add((Control)(object)lblSmartHotkey);
		((Control)this).Controls.Add((Control)(object)chkShowMessages);
		((Control)this).Controls.Add((Control)(object)label10);
		((Control)this).Controls.Add((Control)(object)cmbStopInterrupt);
		((Control)this).Controls.Add((Control)(object)cmbStartInterrupt);
		((Control)this).Controls.Add((Control)(object)lblChangeSound);
		((Control)this).Controls.Add((Control)(object)label11);
		((Control)this).Controls.Add((Control)(object)cmbStopRotation);
		((Control)this).Controls.Add((Control)(object)cmbChangeRotation);
		((Control)this).Controls.Add((Control)(object)cmbStartRotation);
		((Control)this).Controls.Add((Control)(object)label9);
		((Control)this).Controls.Add((Control)(object)label8);
		((Control)this).Controls.Add((Control)(object)label7);
		((Control)this).Controls.Add((Control)(object)lblHotkeyInterrupt);
		((Control)this).Controls.Add((Control)(object)lblHotkey4);
		((Control)this).Controls.Add((Control)(object)lblHotkey3);
		((Control)this).Controls.Add((Control)(object)lblHotkey2);
		((Control)this).Controls.Add((Control)(object)lblHotkey1);
		((Control)this).Controls.Add((Control)(object)cmbRotation4);
		((Control)this).Controls.Add((Control)(object)cmbRotation3);
		((Control)this).Controls.Add((Control)(object)label3);
		((Control)this).Controls.Add((Control)(object)label6);
		((Control)this).Controls.Add((Control)(object)btnSettings);
		((Control)this).Controls.Add((Control)(object)lblRefreshRate);
		((Control)this).Controls.Add((Control)(object)tbRefreshRate);
		((Control)this).Controls.Add((Control)(object)lblRefreshRateValue);
		((Control)this).Controls.Add((Control)(object)chkDebug);
		((Control)this).Controls.Add((Control)(object)btnRotationEditor);
		((Control)this).Controls.Add((Control)(object)label4);
		((Control)this).Controls.Add((Control)(object)cmbRotation2);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)cmbRotation1);
		((Control)this).Controls.Add((Control)(object)label1);
		((Control)this).Controls.Add((Control)(object)btnAbilityEditor);
		((Control)this).Controls.Add((Control)(object)txtAddSpell);
		((Control)this).Controls.Add((Control)(object)chkInterruptAll);
		((Control)this).Controls.Add((Control)(object)btnAddSelected);
		((Control)this).Controls.Add((Control)(object)btnDelete);
		((Control)this).Controls.Add((Control)(object)chkSpellList);
		((Form)this).MaximizeBox = false;
		((Control)this).MaximumSize = new Size(385, 376);
		((Control)this).MinimumSize = new Size(385, 376);
		((Control)this).Name = "frmMain";
		((Control)this).Text = "Rotation -";
		((Form)this).FormClosing += new FormClosingEventHandler(frmMain_FormClosing);
		((Form)this).FormClosed += new FormClosedEventHandler(frmMain_FormClosed);
		((Form)this).Load += frmMain_Load;
		((ISupportInitialize)tbRefreshRate).EndInit();
		((ISupportInitialize)tbInterruptDelay).EndInit();
		((ISupportInitialize)pbInfo2).EndInit();
		((ISupportInitialize)pbInfo1).EndInit();
		((ISupportInitialize)pbInfo3).EndInit();
		((ISupportInitialize)pbInfo4).EndInit();
		((ISupportInitialize)pbRequireCombat).EndInit();
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}

	static frmMain()
	{
		abilityArray = new string[1024, 10];
		rotationArray = new string[1024, 5];
		currentRotationArray = new string[1024, 10];
		isLoading = false;
		SettingsShown = false;
		intCounter = 0;
		intCountRestore = 0;
		chatQueue = new string[1024];
		hk0 = new Hotkey();
		hk1 = new Hotkey();
		hk2 = new Hotkey();
		hk3 = new Hotkey();
		hk4 = new Hotkey();
		hk5 = new Hotkey();
	}
}
