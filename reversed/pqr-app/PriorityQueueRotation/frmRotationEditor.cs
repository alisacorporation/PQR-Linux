using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace PriorityQueueRotation;

public class frmRotationEditor : Form
{
	private IContainer components;

	private Button btnClear;

	private Button btnAddNew;

	private Button btnDeleteSelected;

	private ListBox lstRotations;

	private Label label2;

	private Label label1;

	private Button btnReload;

	private Button btnLeftToRight;

	private Button btnRightToLeft;

	private ListBox lstCurrent;

	private ListBox lstAvailable;

	private Label label6;

	private ComboBox cmbClass;

	private Button btnUp;

	private Button btnDown;

	private Label label4;

	private ComboBox cmbProfile;

	private Button btnNewProfile;

	private Button btnCopyProfile;

	private Button btnDeleteProfile;

	private Button btnCopyRotation;

	private CheckBox chkRequireCombat;

	private Label label3;

	private TextBox txtRotationNotes;

	private Button btnSaveNotes;

	public static bool isLoading;

	public static string[,] profileArray;

	public static string[,] abilityArray;

	public static string[,] rotationArray;

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
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0f4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f56: Expected O, but got Unknown
		btnClear = new Button();
		btnAddNew = new Button();
		btnDeleteSelected = new Button();
		lstRotations = new ListBox();
		label2 = new Label();
		label1 = new Label();
		btnReload = new Button();
		btnLeftToRight = new Button();
		btnRightToLeft = new Button();
		lstCurrent = new ListBox();
		lstAvailable = new ListBox();
		label6 = new Label();
		cmbClass = new ComboBox();
		btnUp = new Button();
		btnDown = new Button();
		label4 = new Label();
		cmbProfile = new ComboBox();
		btnNewProfile = new Button();
		btnCopyProfile = new Button();
		btnDeleteProfile = new Button();
		btnCopyRotation = new Button();
		chkRequireCombat = new CheckBox();
		label3 = new Label();
		txtRotationNotes = new TextBox();
		btnSaveNotes = new Button();
		((Control)this).SuspendLayout();
		((Control)btnClear).Location = new Point(205, 294);
		((Control)btnClear).Name = "btnClear";
		((Control)btnClear).Size = new Size(86, 32);
		((Control)btnClear).TabIndex = 48;
		((Control)btnClear).Text = "Clear All";
		((ButtonBase)btnClear).UseVisualStyleBackColor = true;
		((Control)btnClear).Click += btnClear_Click;
		((Control)btnAddNew).Location = new Point(12, 275);
		((Control)btnAddNew).Name = "btnAddNew";
		((Control)btnAddNew).Size = new Size(182, 23);
		((Control)btnAddNew).TabIndex = 47;
		((Control)btnAddNew).Text = "New Rotation";
		((ButtonBase)btnAddNew).UseVisualStyleBackColor = true;
		((Control)btnAddNew).Click += btnAddNew_Click;
		((Control)btnDeleteSelected).Location = new Point(102, 304);
		((Control)btnDeleteSelected).Name = "btnDeleteSelected";
		((Control)btnDeleteSelected).Size = new Size(92, 23);
		((Control)btnDeleteSelected).TabIndex = 46;
		((Control)btnDeleteSelected).Text = "Delete Rotation";
		((ButtonBase)btnDeleteSelected).UseVisualStyleBackColor = true;
		((Control)btnDeleteSelected).Click += btnDeleteSelected_Click;
		((ListControl)lstRotations).FormattingEnabled = true;
		((Control)lstRotations).Location = new Point(15, 174);
		((Control)lstRotations).Name = "lstRotations";
		((Control)lstRotations).Size = new Size(177, 95);
		((Control)lstRotations).TabIndex = 45;
		lstRotations.SelectedIndexChanged += lstRotations_SelectedIndexChanged;
		((Control)label2).AutoSize = true;
		((Control)label2).Location = new Point(361, 8);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(79, 13);
		((Control)label2).TabIndex = 44;
		((Control)label2).Text = "Current Abilities";
		((Control)label1).AutoSize = true;
		((Control)label1).Location = new Point(228, 8);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(88, 13);
		((Control)label1).TabIndex = 43;
		((Control)label1).Text = "Available Abilities";
		((Control)btnReload).Location = new Point(110, 76);
		((Control)btnReload).Name = "btnReload";
		((Control)btnReload).Size = new Size(87, 23);
		((Control)btnReload).TabIndex = 42;
		((Control)btnReload).Text = "Reload";
		((ButtonBase)btnReload).UseVisualStyleBackColor = true;
		((Control)btnReload).Click += btnReload_Click;
		((Control)btnLeftToRight).Location = new Point(298, 293);
		((Control)btnLeftToRight).Name = "btnLeftToRight";
		((Control)btnLeftToRight).Size = new Size(39, 33);
		((Control)btnLeftToRight).TabIndex = 41;
		((Control)btnLeftToRight).Text = "->";
		((ButtonBase)btnLeftToRight).UseVisualStyleBackColor = true;
		((Control)btnLeftToRight).Click += btnLeftToRight_Click;
		((Control)btnRightToLeft).Location = new Point(343, 293);
		((Control)btnRightToLeft).Name = "btnRightToLeft";
		((Control)btnRightToLeft).Size = new Size(39, 33);
		((Control)btnRightToLeft).TabIndex = 40;
		((Control)btnRightToLeft).Text = "<-";
		((ButtonBase)btnRightToLeft).UseVisualStyleBackColor = true;
		((Control)btnRightToLeft).Click += btnRightToLeft_Click;
		((ListControl)lstCurrent).FormattingEnabled = true;
		((Control)lstCurrent).Location = new Point(343, 24);
		((Control)lstCurrent).Name = "lstCurrent";
		((Control)lstCurrent).Size = new Size(132, 264);
		((Control)lstCurrent).TabIndex = 39;
		lstCurrent.SelectedIndexChanged += lstCurrent_SelectedIndexChanged;
		((ListControl)lstAvailable).FormattingEnabled = true;
		((Control)lstAvailable).Location = new Point(205, 24);
		((Control)lstAvailable).Name = "lstAvailable";
		((Control)lstAvailable).Size = new Size(132, 264);
		lstAvailable.Sorted = true;
		((Control)lstAvailable).TabIndex = 38;
		lstAvailable.SelectedIndexChanged += lstAvailable_SelectedIndexChanged;
		((Control)label6).AutoSize = true;
		((Control)label6).Location = new Point(12, 27);
		((Control)label6).Name = "label6";
		((Control)label6).Size = new Size(35, 13);
		((Control)label6).TabIndex = 37;
		((Control)label6).Text = "Class:";
		cmbClass.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbClass).FormattingEnabled = true;
		cmbClass.Items.AddRange(new object[10] { "DEATHKNIGHT", "DRUID", "HUNTER", "MAGE", "PALADIN", "PRIEST", "ROGUE", "SHAMAN", "WARLOCK", "WARRIOR" });
		((Control)cmbClass).Location = new Point(53, 24);
		((Control)cmbClass).Name = "cmbClass";
		((Control)cmbClass).Size = new Size(144, 21);
		((Control)cmbClass).TabIndex = 36;
		cmbClass.SelectedIndexChanged += cmbClass_SelectedIndexChanged;
		((Control)btnUp).Location = new Point(388, 293);
		((Control)btnUp).Name = "btnUp";
		((Control)btnUp).Size = new Size(39, 33);
		((Control)btnUp).TabIndex = 52;
		((Control)btnUp).Text = "Up";
		((ButtonBase)btnUp).UseVisualStyleBackColor = true;
		((Control)btnUp).Click += btnUp_Click;
		((Control)btnDown).Location = new Point(430, 293);
		((Control)btnDown).Name = "btnDown";
		((Control)btnDown).Size = new Size(45, 33);
		((Control)btnDown).TabIndex = 51;
		((Control)btnDown).Text = "Down";
		((ButtonBase)btnDown).UseVisualStyleBackColor = true;
		((Control)btnDown).Click += btnDown_Click;
		((Control)label4).AutoSize = true;
		((Control)label4).Location = new Point(12, 52);
		((Control)label4).Name = "label4";
		((Control)label4).Size = new Size(39, 13);
		((Control)label4).TabIndex = 53;
		((Control)label4).Text = "Profile:";
		cmbProfile.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbProfile).FormattingEnabled = true;
		((Control)cmbProfile).Location = new Point(53, 49);
		((Control)cmbProfile).Name = "cmbProfile";
		((Control)cmbProfile).Size = new Size(144, 21);
		((Control)cmbProfile).TabIndex = 54;
		cmbProfile.SelectedIndexChanged += cmbProfile_SelectedIndexChanged;
		((Control)btnNewProfile).Location = new Point(17, 76);
		((Control)btnNewProfile).Name = "btnNewProfile";
		((Control)btnNewProfile).Size = new Size(87, 23);
		((Control)btnNewProfile).TabIndex = 55;
		((Control)btnNewProfile).Text = "New Profile";
		((ButtonBase)btnNewProfile).UseVisualStyleBackColor = true;
		((Control)btnNewProfile).Click += btnNew_Click;
		((Control)btnCopyProfile).Location = new Point(17, 105);
		((Control)btnCopyProfile).Name = "btnCopyProfile";
		((Control)btnCopyProfile).Size = new Size(87, 23);
		((Control)btnCopyProfile).TabIndex = 56;
		((Control)btnCopyProfile).Text = "Copy Profile";
		((ButtonBase)btnCopyProfile).UseVisualStyleBackColor = true;
		((Control)btnCopyProfile).Click += btnCopy_Click;
		((Control)btnDeleteProfile).Location = new Point(110, 105);
		((Control)btnDeleteProfile).Name = "btnDeleteProfile";
		((Control)btnDeleteProfile).Size = new Size(87, 23);
		((Control)btnDeleteProfile).TabIndex = 57;
		((Control)btnDeleteProfile).Text = "Delete Profile";
		((ButtonBase)btnDeleteProfile).UseVisualStyleBackColor = true;
		((Control)btnDeleteProfile).Click += btnDeleteProfile_Click;
		((Control)btnCopyRotation).Location = new Point(12, 304);
		((Control)btnCopyRotation).Name = "btnCopyRotation";
		((Control)btnCopyRotation).Size = new Size(87, 23);
		((Control)btnCopyRotation).TabIndex = 59;
		((Control)btnCopyRotation).Text = "Copy Rotation";
		((ButtonBase)btnCopyRotation).UseVisualStyleBackColor = true;
		((Control)btnCopyRotation).Click += btnCopyRotation_Click;
		((Control)chkRequireCombat).AutoSize = true;
		((Control)chkRequireCombat).Location = new Point(484, 194);
		((Control)chkRequireCombat).Name = "chkRequireCombat";
		((Control)chkRequireCombat).Size = new Size(199, 17);
		((Control)chkRequireCombat).TabIndex = 60;
		((Control)chkRequireCombat).Text = "Require Combat to Execute Rotation";
		((ButtonBase)chkRequireCombat).UseVisualStyleBackColor = true;
		chkRequireCombat.CheckedChanged += chkRequireCombat_CheckedChanged;
		((Control)label3).AutoSize = true;
		((Control)label3).Location = new Point(481, 8);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(38, 13);
		((Control)label3).TabIndex = 62;
		((Control)label3).Text = "Notes:";
		txtRotationNotes.AcceptsReturn = true;
		((TextBoxBase)txtRotationNotes).AcceptsTab = true;
		((Control)txtRotationNotes).Location = new Point(484, 24);
		((TextBoxBase)txtRotationNotes).Multiline = true;
		((Control)txtRotationNotes).Name = "txtRotationNotes";
		txtRotationNotes.ScrollBars = (ScrollBars)2;
		((Control)txtRotationNotes).Size = new Size(330, 133);
		((Control)txtRotationNotes).TabIndex = 63;
		((Control)txtRotationNotes).TextChanged += txtRotationNotes_TextChanged;
		((Control)btnSaveNotes).Location = new Point(484, 163);
		((Control)btnSaveNotes).Name = "btnSaveNotes";
		((Control)btnSaveNotes).Size = new Size(330, 23);
		((Control)btnSaveNotes).TabIndex = 64;
		((Control)btnSaveNotes).Text = "Save Notes";
		((ButtonBase)btnSaveNotes).UseVisualStyleBackColor = true;
		((Control)btnSaveNotes).Click += btnSaveNotes_Click;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(818, 334);
		((Control)this).Controls.Add((Control)(object)btnSaveNotes);
		((Control)this).Controls.Add((Control)(object)txtRotationNotes);
		((Control)this).Controls.Add((Control)(object)label3);
		((Control)this).Controls.Add((Control)(object)chkRequireCombat);
		((Control)this).Controls.Add((Control)(object)btnCopyRotation);
		((Control)this).Controls.Add((Control)(object)btnDeleteProfile);
		((Control)this).Controls.Add((Control)(object)btnCopyProfile);
		((Control)this).Controls.Add((Control)(object)btnNewProfile);
		((Control)this).Controls.Add((Control)(object)cmbProfile);
		((Control)this).Controls.Add((Control)(object)label4);
		((Control)this).Controls.Add((Control)(object)btnUp);
		((Control)this).Controls.Add((Control)(object)btnDown);
		((Control)this).Controls.Add((Control)(object)btnClear);
		((Control)this).Controls.Add((Control)(object)btnAddNew);
		((Control)this).Controls.Add((Control)(object)btnDeleteSelected);
		((Control)this).Controls.Add((Control)(object)lstRotations);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)label1);
		((Control)this).Controls.Add((Control)(object)btnReload);
		((Control)this).Controls.Add((Control)(object)btnLeftToRight);
		((Control)this).Controls.Add((Control)(object)btnRightToLeft);
		((Control)this).Controls.Add((Control)(object)lstCurrent);
		((Control)this).Controls.Add((Control)(object)lstAvailable);
		((Control)this).Controls.Add((Control)(object)label6);
		((Control)this).Controls.Add((Control)(object)cmbClass);
		((Control)this).MaximumSize = new Size(834, 372);
		((Control)this).MinimumSize = new Size(834, 372);
		((Control)this).Name = "frmRotationEditor";
		((Form)this).SizeGripStyle = (SizeGripStyle)2;
		((Control)this).Text = "Rotation - Profile & Rotation Editor";
		((Form)this).FormClosing += new FormClosingEventHandler(frmRotationEditor_FormClosing);
		((Form)this).Load += frmRotationEditor_Load;
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}

	public frmRotationEditor()
	{
		InitializeComponent();
	}

	private void frmRotationEditor_Load(object sender, EventArgs e)
	{
		((Form)Program.rotationForm).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		frmMain.LockdownMain(trueFalse: true);
		GlobalSettings.RotationEditorLoaded = true;
		((Control)btnAddNew).Enabled = false;
		((Control)btnDeleteSelected).Enabled = false;
		((Control)btnNewProfile).Enabled = false;
		((Control)btnCopyProfile).Enabled = false;
		((Control)cmbProfile).Enabled = false;
		((Control)btnCopyRotation).Enabled = false;
		((Control)btnDeleteProfile).Enabled = false;
		((Control)lstCurrent).Enabled = false;
		((Control)lstAvailable).Enabled = false;
		((Control)txtRotationNotes).Enabled = false;
		((Control)btnLeftToRight).Enabled = false;
		((Control)btnRightToLeft).Enabled = false;
		((Control)btnUp).Enabled = false;
		((Control)btnDown).Enabled = false;
		((Control)btnClear).Enabled = false;
		((Control)btnSaveNotes).Enabled = false;
		((Control)chkRequireCombat).Enabled = false;
	}

	private void cmbClass_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbClass.SelectedItem != null)
		{
			LoadProfileList();
			((Control)btnCopyProfile).Enabled = true;
			((Control)btnDeleteProfile).Enabled = true;
			((Control)btnNewProfile).Enabled = true;
			((Control)cmbProfile).Enabled = true;
		}
	}

	private void LoadProfileList()
	{
		profileArray = clsXML.RotationEditor_Profiles(cmbClass.SelectedItem.ToString());
		cmbProfile.Items.Clear();
		for (int i = 0; i < 1024; i++)
		{
			if (profileArray[i, 0] != null)
			{
				cmbProfile.Items.Add((object)profileArray[i, 0]);
			}
			if (profileArray[i, 0] == null)
			{
				break;
			}
			lstAvailable.Items.Clear();
			lstCurrent.Items.Clear();
			((Control)txtRotationNotes).Text = "";
			lstRotations.Items.Clear();
			((Control)btnAddNew).Enabled = false;
			((Control)btnDeleteSelected).Enabled = false;
			((Control)btnCopyRotation).Enabled = false;
		}
	}

	private void lstRotations_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!isLoading)
		{
			if (lstRotations.SelectedItem != null)
			{
				PopulateAbilities();
				PopulateCurrent(lstRotations.SelectedItem.ToString());
				PopulateNotes(lstRotations.SelectedItem.ToString());
				PopulateCombatRequirement(lstRotations.SelectedItem.ToString());
				((Control)btnSaveNotes).Enabled = false;
				((Control)btnLeftToRight).Enabled = true;
				((Control)btnRightToLeft).Enabled = true;
				((Control)btnUp).Enabled = true;
				((Control)btnDown).Enabled = true;
				((Control)btnClear).Enabled = true;
				((Control)txtRotationNotes).Enabled = true;
				((Control)chkRequireCombat).Enabled = true;
				((Control)lstCurrent).Enabled = true;
				((Control)lstAvailable).Enabled = true;
			}
			else
			{
				((Control)btnLeftToRight).Enabled = false;
				((Control)btnRightToLeft).Enabled = false;
				((Control)btnUp).Enabled = false;
				((Control)txtRotationNotes).Enabled = false;
				((Control)btnSaveNotes).Enabled = false;
				((Control)chkRequireCombat).Enabled = false;
				((Control)btnDown).Enabled = false;
				((Control)btnClear).Enabled = false;
				((Control)lstCurrent).Enabled = false;
				((Control)lstAvailable).Enabled = false;
			}
		}
	}

	private void btnReload_Click(object sender, EventArgs e)
	{
		LoadClass("", "");
		cmbProfile.Items.Clear();
	}

	private void btnDeleteSelected_Click(object sender, EventArgs e)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		if (lstRotations.SelectedItem == null)
		{
			return;
		}
		DialogResult val = MessageBox.Show("Are you sure that you would like to delete the selected rotation? This cannot be undone.", "Delete Rotation?", (MessageBoxButtons)4, (MessageBoxIcon)32);
		if ((int)val == 7)
		{
			return;
		}
		string text = lstRotations.SelectedItem.ToString();
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == text)
			{
				rotationArray[i, 0] = null;
				rotationArray[i, 1] = null;
				rotationArray[i, 2] = null;
				rotationArray[i, 3] = null;
				rotationArray[i, 4] = null;
			}
		}
		lstRotations.Items.Remove((object)text);
		SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		isLoading = true;
		PopulateRotations();
		PopulateAbilities();
		lstCurrent.Items.Clear();
		((Control)txtRotationNotes).Text = "";
		chkRequireCombat.Checked = true;
		isLoading = false;
	}

	private void btnAddNew_Click(object sender, EventArgs e)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		string text = Interaction.InputBox("Enter the name of the new rotation. Note that this cannot match an already existing rotation from this profile.", "Rotation Name", "", -1, -1);
		if (text.Contains("(") || text.Contains(")"))
		{
			text.Replace("(", "");
			text.Replace(")", "");
		}
		if (text == null || text == "")
		{
			return;
		}
		string text2 = text.Trim();
		if (!(text2.Replace(" ", "") != ""))
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == text2)
			{
				MessageBox.Show("The name you have selected is already in use. Please select a new name for this rotation.");
				return;
			}
		}
		for (int j = 0; j < 1024; j++)
		{
			if (rotationArray[j, 0] == null)
			{
				rotationArray[j, 0] = text2;
				rotationArray[j, 1] = "false";
				rotationArray[j, 2] = "";
				rotationArray[j, 3] = "true";
				rotationArray[j, 4] = "";
				break;
			}
		}
		isLoading = true;
		PopulateRotations();
		PopulateAbilities();
		lstCurrent.Items.Clear();
		((Control)txtRotationNotes).Text = "";
		chkRequireCombat.Checked = true;
		((Control)btnLeftToRight).Enabled = false;
		((Control)btnRightToLeft).Enabled = false;
		((Control)btnUp).Enabled = false;
		((Control)btnDown).Enabled = false;
		((Control)txtRotationNotes).Enabled = false;
		((Control)btnSaveNotes).Enabled = false;
		((Control)chkRequireCombat).Enabled = false;
		((Control)btnClear).Enabled = false;
		((Control)lstCurrent).Enabled = false;
		((Control)lstAvailable).Enabled = false;
		isLoading = false;
		lstRotations.SelectedItem = text2;
		SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
	}

	private void btnLeftToRight_Click(object sender, EventArgs e)
	{
		if (lstAvailable.SelectedItem != null)
		{
			AddToCurrent(lstAvailable.SelectedItem.ToString());
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnRightToLeft_Click(object sender, EventArgs e)
	{
		if (lstCurrent.SelectedItem != null)
		{
			RemoveFromCurrent(lstCurrent.SelectedItem.ToString());
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnClear_Click(object sender, EventArgs e)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		DialogResult val = MessageBox.Show("Are you sure that you would like to clear the current rotation? This cannot be undone.", "Clear Rotation?", (MessageBoxButtons)4, (MessageBoxIcon)32);
		if ((int)val != 7)
		{
			lstCurrent.Items.Clear();
			((Control)txtRotationNotes).Text = "";
			chkRequireCombat.Checked = true;
			PopulateAbilities();
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void LoadClass(string sClass, string sProfile)
	{
		if (sClass != null && sProfile != null)
		{
			if (sClass == "" || sProfile == "")
			{
				abilityArray = new string[1024, 10];
				rotationArray = new string[1024, 5];
				isLoading = true;
				((ListControl)cmbClass).SelectedIndex = -1;
				PopulateRotations();
				PopulateAbilities();
				lstCurrent.Items.Clear();
				((Control)txtRotationNotes).Text = "";
				chkRequireCombat.Checked = true;
				((Control)btnAddNew).Enabled = false;
				((Control)btnLeftToRight).Enabled = false;
				((Control)btnRightToLeft).Enabled = false;
				((Control)btnUp).Enabled = false;
				((Control)btnDown).Enabled = false;
				((Control)btnClear).Enabled = false;
				((Control)btnCopyProfile).Enabled = false;
				((Control)btnNewProfile).Enabled = false;
				((Control)btnDeleteSelected).Enabled = false;
				((Control)btnCopyRotation).Enabled = false;
				((Control)btnDeleteProfile).Enabled = false;
				((Control)cmbProfile).Enabled = false;
				((Control)lstCurrent).Enabled = false;
				((Control)lstAvailable).Enabled = false;
				((Control)txtRotationNotes).Enabled = false;
				((Control)btnSaveNotes).Enabled = false;
				((Control)chkRequireCombat).Enabled = false;
				isLoading = false;
			}
			else
			{
				isLoading = true;
				abilityArray = clsXML.LoadXML_Abilities(sProfile + "_" + sClass);
				rotationArray = clsXML.LoadXML_Rotations(sProfile + "_" + sClass);
				PopulateRotations();
				lstCurrent.Items.Clear();
				PopulateAbilities();
				isLoading = true;
				((Control)txtRotationNotes).Text = "";
				chkRequireCombat.Checked = true;
				((Control)txtRotationNotes).Enabled = false;
				((Control)btnLeftToRight).Enabled = false;
				((Control)btnRightToLeft).Enabled = false;
				((Control)btnUp).Enabled = false;
				((Control)btnDown).Enabled = false;
				((Control)btnClear).Enabled = false;
				((Control)btnSaveNotes).Enabled = false;
				((Control)chkRequireCombat).Enabled = false;
				((Control)btnAddNew).Enabled = true;
				((Control)btnDeleteSelected).Enabled = true;
				((Control)btnCopyRotation).Enabled = true;
				((Control)lstCurrent).Enabled = false;
				((Control)lstAvailable).Enabled = false;
				isLoading = false;
			}
		}
	}

	private void PopulateRotations()
	{
		lstRotations.Items.Clear();
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] != null)
			{
				lstRotations.Items.Add((object)rotationArray[i, 0]);
			}
		}
	}

	private void PopulateAbilities()
	{
		lstAvailable.Items.Clear();
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] != null)
			{
				lstAvailable.Items.Add((object)abilityArray[i, 0]);
			}
		}
	}

	private void PopulateCurrent(string strRotation)
	{
		lstCurrent.Items.Clear();
		string abilityString = "";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == strRotation)
			{
				abilityString = rotationArray[i, 2];
			}
		}
		List<string> abilityList = GetAbilityList(abilityString);
		foreach (string item in abilityList)
		{
			AddToCurrent(item.Trim());
		}
	}

	private void PopulateNotes(string strRotation)
	{
		isLoading = true;
		string text = "";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == strRotation)
			{
				text = rotationArray[i, 4];
			}
		}
		((Control)txtRotationNotes).Text = text;
		isLoading = false;
	}

	private void PopulateCombatRequirement(string strRotation)
	{
		isLoading = true;
		string text = "";
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == strRotation)
			{
				text = rotationArray[i, 3];
			}
		}
		if (text == "false")
		{
			chkRequireCombat.Checked = false;
		}
		else
		{
			chkRequireCombat.Checked = true;
		}
		isLoading = false;
	}

	private List<string> GetAbilityList(string abilityString)
	{
		List<string> list = new List<string>();
		string[] array = abilityString.Split(new char[1] { '|' });
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.Replace(" ", "") != "")
			{
				list.Add(text.Trim());
			}
		}
		return list;
	}

	private void AddToCurrent(string strAddAbility)
	{
		if (strAddAbility.Replace(" ", "") != "")
		{
			try
			{
				lstAvailable.Items.Remove((object)strAddAbility.Trim());
			}
			catch
			{
			}
			lstCurrent.Items.Add((object)strAddAbility.Trim());
		}
	}

	private void RemoveFromCurrent(string strRemoveAbility)
	{
		if (strRemoveAbility.Replace(" ", "") != "")
		{
			try
			{
				lstCurrent.Items.Remove((object)strRemoveAbility.Trim());
			}
			catch
			{
			}
			lstAvailable.Items.Add((object)strRemoveAbility);
		}
	}

	private void SaveRotation(string strRotation, List<string> myRotation, bool requireCombat, string rotationNotes)
	{
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == strRotation.Trim())
			{
				rotationArray[i, 0] = strRotation.Trim();
				rotationArray[i, 1] = "false";
				rotationArray[i, 2] = AbilityString(myRotation);
				rotationArray[i, 3] = requireCombat.ToString().ToLower();
				if (rotationNotes != null)
				{
					rotationArray[i, 4] = rotationNotes;
				}
			}
		}
	}

	private string AbilityString(List<string> myActions)
	{
		string text = "";
		foreach (string myAction in myActions)
		{
			if (myAction.Replace(" ", "") != "")
			{
				text = ((!(text == "")) ? (text + "|" + myAction.Trim()) : myAction.Trim());
			}
		}
		return text;
	}

	private void SaveClass(string sClass, string sProfile)
	{
		if (sClass != "" && sClass != null)
		{
			clsXML.SaveXML_Rotations(sClass, sProfile, rotationArray);
		}
	}

	private void lstCurrent_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!isLoading)
		{
			isLoading = true;
			lstAvailable.ClearSelected();
			isLoading = false;
		}
	}

	private void lstAvailable_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!isLoading)
		{
			isLoading = true;
			lstCurrent.ClearSelected();
			isLoading = false;
		}
	}

	private void frmRotationEditor_FormClosing(object sender, FormClosingEventArgs e)
	{
		frmMain.LockdownMain(trueFalse: false);
		GlobalSettings.RotationEditorLoaded = false;
	}

	private void btnUp_Click(object sender, EventArgs e)
	{
		if (lstCurrent.SelectedItems.Count > 0)
		{
			object selectedItem = lstCurrent.SelectedItem;
			int num = lstCurrent.Items.IndexOf(selectedItem);
			int count = lstCurrent.Items.Count;
			if (num == 0)
			{
				lstCurrent.Items.Remove(selectedItem);
				lstCurrent.Items.Insert(count - 1, selectedItem);
				lstCurrent.SetSelected(count - 1, true);
			}
			else
			{
				lstCurrent.Items.Remove(selectedItem);
				lstCurrent.Items.Insert(num - 1, selectedItem);
				lstCurrent.SetSelected(num - 1, true);
			}
		}
		SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
		SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
	}

	private void btnDown_Click(object sender, EventArgs e)
	{
		if (lstCurrent.SelectedItems.Count > 0)
		{
			object selectedItem = lstCurrent.SelectedItem;
			int num = lstCurrent.Items.IndexOf(selectedItem);
			int count = lstCurrent.Items.Count;
			if (num == count - 1)
			{
				lstCurrent.Items.Remove(selectedItem);
				lstCurrent.Items.Insert(0, selectedItem);
				lstCurrent.SetSelected(0, true);
			}
			else
			{
				lstCurrent.Items.Remove(selectedItem);
				lstCurrent.Items.Insert(num + 1, selectedItem);
				lstCurrent.SetSelected(num + 1, true);
			}
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnNew_Click(object sender, EventArgs e)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		string text = Interaction.InputBox("Enter the name of the new profile. Note that this cannot match an already existing profile.", "Profile Name", "", -1, -1);
		if (text != null && !(text == "") && cmbClass.SelectedItem != null)
		{
			if (clsXML.CreateProfile(cmbClass.SelectedItem.ToString(), text))
			{
				LoadProfileList();
				LoadClass(cmbClass.SelectedItem.ToString(), text);
				cmbProfile.SelectedItem = text;
			}
			else
			{
				MessageBox.Show("Failed to create the specified profile name. The profile may already exist or you may have used invalid characters in the file name.");
			}
		}
	}

	private void cmbProfile_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbClass.SelectedItem != null && cmbProfile.SelectedItem != null)
		{
			string sClass = cmbClass.SelectedItem.ToString();
			string sProfile = cmbProfile.SelectedItem.ToString();
			LoadClass(sClass, sProfile);
		}
	}

	private void btnCopy_Click(object sender, EventArgs e)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (cmbClass.SelectedItem == null || cmbProfile.SelectedItem == null)
		{
			return;
		}
		string text = Interaction.InputBox("Enter the name of the new profile. Note that this cannot match an already existing profile.", "Profile Name", "", -1, -1);
		if (text != null && !(text == ""))
		{
			if (clsXML.CopyProfile(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString(), text))
			{
				LoadProfileList();
				cmbProfile.SelectedItem = text;
			}
			else
			{
				MessageBox.Show("An error occurred while trying to copy this profile. The profile name you specified may already exist or you may have used invalid characters in the file name.");
			}
		}
	}

	private void btnDeleteProfile_Click(object sender, EventArgs e)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		if (cmbClass.SelectedItem != null && cmbProfile.SelectedItem != null)
		{
			DialogResult val = MessageBox.Show("WARNING: You are about to delete an entire profile, this will delete BOTH rotations AND abilities stored in these files, as well as the files themselves. This cannot be undone. Proceed?", "Delete Profile?", (MessageBoxButtons)4, (MessageBoxIcon)48);
			if ((int)val != 7)
			{
				clsXML.DeleteXMLs(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
				LoadProfileList();
				cmbProfile.SelectedItem = "";
			}
		}
	}

	private void btnCopyRotation_Click(object sender, EventArgs e)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		if (lstRotations.SelectedItem == null)
		{
			return;
		}
		string text = lstRotations.SelectedItem.ToString().Trim();
		int num = -1;
		int num2 = -1;
		if (!(text != ""))
		{
			return;
		}
		string text2 = Interaction.InputBox("Enter the name of the new rotation. Note that this cannot match an already existing rotation from this profile.", "Rotation Name", "", -1, -1);
		if (text2.Contains("(") || text2.Contains(")"))
		{
			text2.Replace("(", "");
			text2.Replace(")", "");
		}
		if (text2 == null || text2 == "")
		{
			return;
		}
		string selectedItem = cmbProfile.SelectedItem.ToString();
		for (int i = 0; i < 1024; i++)
		{
			if (rotationArray[i, 0] == text2)
			{
				MessageBox.Show("The name you have selected is already in use. Please select a new name for this rotation.");
				return;
			}
			if (rotationArray[i, 0] == text)
			{
				num = i;
			}
			if (rotationArray[i, 0] == null && num2 == -1)
			{
				num2 = i;
			}
		}
		if (num != -1 && num2 != -1)
		{
			rotationArray[num2, 0] = text2;
			rotationArray[num2, 1] = "false";
			rotationArray[num2, 2] = rotationArray[num, 2];
			rotationArray[num2, 3] = rotationArray[num, 3];
			rotationArray[num2, 4] = rotationArray[num, 4];
		}
		SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		LoadProfileList();
		cmbProfile.SelectedItem = selectedItem;
		lstRotations.SelectedItem = text2;
	}

	private void txtRotationNotes_TextChanged(object sender, EventArgs e)
	{
		if (((Control)txtRotationNotes).Text.Trim() == "")
		{
			((Control)btnSaveNotes).Enabled = false;
		}
		else
		{
			((Control)btnSaveNotes).Enabled = true;
		}
		if (cmbProfile.SelectedItem != null && !(cmbProfile.SelectedItem.ToString() == "") && cmbClass.SelectedItem != null && !(cmbClass.SelectedItem.ToString() == "") && lstRotations.SelectedItem != null)
		{
			_ = lstRotations.SelectedItem.ToString() == "";
		}
	}

	private void chkRequireCombat_CheckedChanged(object sender, EventArgs e)
	{
		if (!isLoading && cmbProfile.SelectedItem != null && !(cmbProfile.SelectedItem.ToString() == "") && cmbClass.SelectedItem != null && !(cmbClass.SelectedItem.ToString() == "") && lstRotations.SelectedItem != null && !(lstRotations.SelectedItem.ToString() == ""))
		{
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, null);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnSaveNotes_Click(object sender, EventArgs e)
	{
		((Control)btnSaveNotes).Enabled = false;
		if (cmbProfile.SelectedItem != null && !(cmbProfile.SelectedItem.ToString() == "") && cmbClass.SelectedItem != null && !(cmbClass.SelectedItem.ToString() == "") && lstRotations.SelectedItem != null && !(lstRotations.SelectedItem.ToString() == ""))
		{
			SaveRotation(lstRotations.SelectedItem.ToString(), ((IEnumerable)lstCurrent.Items).OfType<string>().ToList(), chkRequireCombat.Checked, ((Control)txtRotationNotes).Text);
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	static frmRotationEditor()
	{
		isLoading = false;
		profileArray = new string[1024, 2];
		abilityArray = new string[1024, 10];
		rotationArray = new string[1024, 5];
	}
}
