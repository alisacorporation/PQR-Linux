using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using PriorityQueueRotation.Properties;

namespace PriorityQueueRotation;

public class frmHotkeyEditor : Form
{
	private IContainer components;

	private Button btnCancel;

	private Button btnSave;

	private GroupBox groupBox1;

	private RadioButton rbShiftNo;

	private RadioButton rbShiftYes;

	private GroupBox groupBox2;

	private RadioButton rbCtrlNo;

	private RadioButton rbCtrlYes;

	private GroupBox groupBox3;

	private RadioButton rbAltNo;

	private RadioButton rbAltYes;

	private GroupBox groupBox4;

	private ComboBox cmbKey;

	public frmHotkeyEditor()
	{
		InitializeComponent();
	}

	private void SetupKeyList()
	{
		GlobalSettings.SetupDictionary();
		foreach (KeyValuePair<Keys, string> myKey in GlobalSettings.myKeys)
		{
			cmbKey.Items.Add((object)myKey.Value);
		}
	}

	private void frmHotkeyEditor_Load(object sender, EventArgs e)
	{
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_045e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Unknown result type (might be due to invalid IL or missing references)
		GlobalSettings.HotkeyEditorLoaded = true;
		((Form)Program.hotkeyForm).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		SetupKeyList();
		switch (GlobalSettings.EditingHotkey)
		{
		case 0:
			if (Settings.Default.Hotkey0ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey0CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey0SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey0Key);
			break;
		case 1:
			if (Settings.Default.Hotkey1ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey1CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey1SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey1Key);
			break;
		case 2:
			if (Settings.Default.Hotkey2ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey2CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey2SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey2Key);
			break;
		case 3:
			if (Settings.Default.Hotkey3ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey3CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey3SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey3Key);
			break;
		case 4:
			if (Settings.Default.Hotkey4ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey4CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey4SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey4Key);
			break;
		case 5:
			if (Settings.Default.Hotkey5ALT)
			{
				rbAltYes.Checked = true;
				rbAltNo.Checked = false;
			}
			else
			{
				rbAltYes.Checked = false;
				rbAltNo.Checked = true;
			}
			if (Settings.Default.Hotkey5CTRL)
			{
				rbCtrlYes.Checked = true;
				rbCtrlNo.Checked = false;
			}
			else
			{
				rbCtrlYes.Checked = false;
				rbCtrlNo.Checked = true;
			}
			if (Settings.Default.Hotkey5SHIFT)
			{
				rbShiftYes.Checked = true;
				rbShiftNo.Checked = false;
			}
			else
			{
				rbShiftYes.Checked = false;
				rbShiftNo.Checked = true;
			}
			cmbKey.SelectedItem = GlobalSettings.GetStringFromKey(Settings.Default.Hotkey5Key);
			break;
		}
	}

	private void frmHotkeyEditor_FormClosing(object sender, FormClosingEventArgs e)
	{
		GlobalSettings.HotkeyEditorLoaded = false;
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		if (GlobalSettings.EditingHotkey != 0 && Settings.Default.Hotkey0ALT == rbAltYes.Checked && Settings.Default.Hotkey0CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey0SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey0Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (GlobalSettings.EditingHotkey != 1 && Settings.Default.Hotkey1ALT == rbAltYes.Checked && Settings.Default.Hotkey1CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey1SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey1Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (GlobalSettings.EditingHotkey != 2 && Settings.Default.Hotkey2ALT == rbAltYes.Checked && Settings.Default.Hotkey2CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey2SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey2Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (GlobalSettings.EditingHotkey != 3 && Settings.Default.Hotkey3ALT == rbAltYes.Checked && Settings.Default.Hotkey3CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey3SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey3Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (GlobalSettings.EditingHotkey != 4 && Settings.Default.Hotkey4ALT == rbAltYes.Checked && Settings.Default.Hotkey4CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey4SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey4Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (GlobalSettings.EditingHotkey != 5 && Settings.Default.Hotkey5ALT == rbAltYes.Checked && Settings.Default.Hotkey5CTRL == rbCtrlYes.Checked && Settings.Default.Hotkey5SHIFT == rbShiftYes.Checked && Settings.Default.Hotkey5Key == GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString()))
		{
			flag = true;
		}
		if (flag)
		{
			MessageBox.Show("You cannot assign a hotkey to more than one rotation or mode. Please select a new key combination.", "DUPLICATE HOTKEY");
			return;
		}
		switch (GlobalSettings.EditingHotkey)
		{
		case 0:
			Settings.Default.Hotkey0ALT = rbAltYes.Checked;
			Settings.Default.Hotkey0CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey0SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey0Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		case 1:
			Settings.Default.Hotkey1ALT = rbAltYes.Checked;
			Settings.Default.Hotkey1CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey1SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey1Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		case 2:
			Settings.Default.Hotkey2ALT = rbAltYes.Checked;
			Settings.Default.Hotkey2CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey2SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey2Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		case 3:
			Settings.Default.Hotkey3ALT = rbAltYes.Checked;
			Settings.Default.Hotkey3CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey3SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey3Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		case 4:
			Settings.Default.Hotkey4ALT = rbAltYes.Checked;
			Settings.Default.Hotkey4CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey4SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey4Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		case 5:
			Settings.Default.Hotkey5ALT = rbAltYes.Checked;
			Settings.Default.Hotkey5CTRL = rbCtrlYes.Checked;
			Settings.Default.Hotkey5SHIFT = rbShiftYes.Checked;
			Settings.Default.Hotkey5Key = GlobalSettings.GetKeyFromString(cmbKey.SelectedItem.ToString());
			break;
		}
		((SettingsBase)Settings.Default).Save();
		frmMain.ConfigureHotkeys();
		((Form)Program.hotkeyForm).Close();
	}

	private void btnCancel_Click(object sender, EventArgs e)
	{
		((Form)Program.hotkeyForm).Close();
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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_085e: Expected O, but got Unknown
		btnCancel = new Button();
		btnSave = new Button();
		groupBox1 = new GroupBox();
		rbShiftNo = new RadioButton();
		rbShiftYes = new RadioButton();
		groupBox2 = new GroupBox();
		rbCtrlNo = new RadioButton();
		rbCtrlYes = new RadioButton();
		groupBox3 = new GroupBox();
		rbAltNo = new RadioButton();
		rbAltYes = new RadioButton();
		groupBox4 = new GroupBox();
		cmbKey = new ComboBox();
		((Control)groupBox1).SuspendLayout();
		((Control)groupBox2).SuspendLayout();
		((Control)groupBox3).SuspendLayout();
		((Control)groupBox4).SuspendLayout();
		((Control)this).SuspendLayout();
		((Control)btnCancel).Location = new Point(107, 86);
		((Control)btnCancel).Name = "btnCancel";
		((Control)btnCancel).Size = new Size(99, 27);
		((Control)btnCancel).TabIndex = 29;
		((Control)btnCancel).Text = "Cancel";
		((ButtonBase)btnCancel).UseVisualStyleBackColor = true;
		((Control)btnCancel).Click += btnCancel_Click;
		((Control)btnSave).Location = new Point(217, 86);
		((Control)btnSave).Name = "btnSave";
		((Control)btnSave).Size = new Size(99, 27);
		((Control)btnSave).TabIndex = 28;
		((Control)btnSave).Text = "Save";
		((ButtonBase)btnSave).UseVisualStyleBackColor = true;
		((Control)btnSave).Click += btnSave_Click;
		((Control)groupBox1).Controls.Add((Control)(object)rbShiftNo);
		((Control)groupBox1).Controls.Add((Control)(object)rbShiftYes);
		((Control)groupBox1).Location = new Point(12, 9);
		((Control)groupBox1).Name = "groupBox1";
		((Control)groupBox1).Size = new Size(77, 71);
		((Control)groupBox1).TabIndex = 30;
		groupBox1.TabStop = false;
		((Control)groupBox1).Text = "SHIFT";
		((Control)rbShiftNo).AutoSize = true;
		((Control)rbShiftNo).Location = new Point(11, 42);
		((Control)rbShiftNo).Name = "rbShiftNo";
		((Control)rbShiftNo).Size = new Size(39, 17);
		((Control)rbShiftNo).TabIndex = 22;
		rbShiftNo.TabStop = true;
		((Control)rbShiftNo).Tag = "SHIFT";
		((Control)rbShiftNo).Text = "No";
		((ButtonBase)rbShiftNo).UseVisualStyleBackColor = true;
		((Control)rbShiftYes).AutoSize = true;
		((Control)rbShiftYes).Location = new Point(11, 19);
		((Control)rbShiftYes).Name = "rbShiftYes";
		((Control)rbShiftYes).Size = new Size(43, 17);
		((Control)rbShiftYes).TabIndex = 21;
		rbShiftYes.TabStop = true;
		((Control)rbShiftYes).Tag = "SHIFT";
		((Control)rbShiftYes).Text = "Yes";
		((ButtonBase)rbShiftYes).UseVisualStyleBackColor = true;
		((Control)groupBox2).Controls.Add((Control)(object)rbCtrlNo);
		((Control)groupBox2).Controls.Add((Control)(object)rbCtrlYes);
		((Control)groupBox2).Location = new Point(98, 9);
		((Control)groupBox2).Name = "groupBox2";
		((Control)groupBox2).Size = new Size(77, 71);
		((Control)groupBox2).TabIndex = 31;
		groupBox2.TabStop = false;
		((Control)groupBox2).Text = "CTRL";
		((Control)rbCtrlNo).AutoSize = true;
		((Control)rbCtrlNo).Location = new Point(11, 42);
		((Control)rbCtrlNo).Name = "rbCtrlNo";
		((Control)rbCtrlNo).Size = new Size(39, 17);
		((Control)rbCtrlNo).TabIndex = 22;
		rbCtrlNo.TabStop = true;
		((Control)rbCtrlNo).Tag = "SHIFT";
		((Control)rbCtrlNo).Text = "No";
		((ButtonBase)rbCtrlNo).UseVisualStyleBackColor = true;
		((Control)rbCtrlYes).AutoSize = true;
		((Control)rbCtrlYes).Location = new Point(11, 19);
		((Control)rbCtrlYes).Name = "rbCtrlYes";
		((Control)rbCtrlYes).Size = new Size(43, 17);
		((Control)rbCtrlYes).TabIndex = 21;
		rbCtrlYes.TabStop = true;
		((Control)rbCtrlYes).Tag = "SHIFT";
		((Control)rbCtrlYes).Text = "Yes";
		((ButtonBase)rbCtrlYes).UseVisualStyleBackColor = true;
		((Control)groupBox3).Controls.Add((Control)(object)rbAltNo);
		((Control)groupBox3).Controls.Add((Control)(object)rbAltYes);
		((Control)groupBox3).Location = new Point(181, 9);
		((Control)groupBox3).Name = "groupBox3";
		((Control)groupBox3).Size = new Size(77, 71);
		((Control)groupBox3).TabIndex = 32;
		groupBox3.TabStop = false;
		((Control)groupBox3).Text = "ALT";
		((Control)rbAltNo).AutoSize = true;
		((Control)rbAltNo).Location = new Point(11, 42);
		((Control)rbAltNo).Name = "rbAltNo";
		((Control)rbAltNo).Size = new Size(39, 17);
		((Control)rbAltNo).TabIndex = 22;
		rbAltNo.TabStop = true;
		((Control)rbAltNo).Tag = "SHIFT";
		((Control)rbAltNo).Text = "No";
		((ButtonBase)rbAltNo).UseVisualStyleBackColor = true;
		((Control)rbAltYes).AutoSize = true;
		((Control)rbAltYes).Location = new Point(11, 19);
		((Control)rbAltYes).Name = "rbAltYes";
		((Control)rbAltYes).Size = new Size(43, 17);
		((Control)rbAltYes).TabIndex = 21;
		rbAltYes.TabStop = true;
		((Control)rbAltYes).Tag = "SHIFT";
		((Control)rbAltYes).Text = "Yes";
		((ButtonBase)rbAltYes).UseVisualStyleBackColor = true;
		((Control)groupBox4).Controls.Add((Control)(object)cmbKey);
		((Control)groupBox4).Location = new Point(264, 9);
		((Control)groupBox4).Name = "groupBox4";
		((Control)groupBox4).Size = new Size(152, 71);
		((Control)groupBox4).TabIndex = 33;
		groupBox4.TabStop = false;
		((Control)groupBox4).Text = "KEY";
		cmbKey.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbKey).FormattingEnabled = true;
		((Control)cmbKey).Location = new Point(16, 28);
		((Control)cmbKey).Name = "cmbKey";
		((Control)cmbKey).Size = new Size(130, 21);
		((Control)cmbKey).TabIndex = 28;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(421, 119);
		((Control)this).Controls.Add((Control)(object)groupBox4);
		((Control)this).Controls.Add((Control)(object)groupBox3);
		((Control)this).Controls.Add((Control)(object)groupBox2);
		((Control)this).Controls.Add((Control)(object)groupBox1);
		((Control)this).Controls.Add((Control)(object)btnCancel);
		((Control)this).Controls.Add((Control)(object)btnSave);
		((Form)this).MaximizeBox = false;
		((Control)this).MaximumSize = new Size(437, 157);
		((Control)this).MinimumSize = new Size(437, 157);
		((Control)this).Name = "frmHotkeyEditor";
		((Control)this).Text = "frmHotkeyEditor";
		((Form)this).FormClosing += new FormClosingEventHandler(frmHotkeyEditor_FormClosing);
		((Form)this).Load += frmHotkeyEditor_Load;
		((Control)groupBox1).ResumeLayout(false);
		((Control)groupBox1).PerformLayout();
		((Control)groupBox2).ResumeLayout(false);
		((Control)groupBox2).PerformLayout();
		((Control)groupBox3).ResumeLayout(false);
		((Control)groupBox3).PerformLayout();
		((Control)groupBox4).ResumeLayout(false);
		((Control)this).ResumeLayout(false);
	}
}
