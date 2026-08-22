using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using SyntaxHighlighter;

namespace PriorityQueueRotation;

public class frmAbilityEditor : Form
{
	public static bool isLoading;

	public static string[,] abilityArray;

	public static string[,] profileArray;

	private IContainer components;

	private Label label5;

	private TextBox txtDefault;

	private Button btnDeleteSelected;

	private Button btnSave;

	private Label label7;

	private Label label6;

	private ComboBox cmbClass;

	private Button btnDeleteAction;

	private TextBox txtAddAction;

	private ListBox lstBoxActions;

	private Button btnAddAction;

	private Label label3;

	private Label label2;

	private Label label1;

	private TextBox txtSpellID;

	private TextBox txtSpellName;

	private ListBox lstAbilities;

	private Label label8;

	private Label label9;

	private TextBox txtRecastDelay;

	private ComboBox cmbTarget;

	private ComboBox cmbCancel;

	private Label label10;

	private Button btnCopyAbility;

	private Label lblProfile;

	private ComboBox cmbProfile;

	private ContextMenuStrip contextLua;

	private ToolStripMenuItem cutCtrlXToolStripMenuItem;

	private ToolStripMenuItem copyCtrlCToolStripMenuItem;

	private ToolStripMenuItem pasteCtrlVToolStripMenuItem;

	private ToolStripSeparator toolStripMenuItem1;

	private ToolStripMenuItem undoCtrlZToolStripMenuItem;

	private ToolStripMenuItem redoCTRLYToolStripMenuItem;

	private TabControl tabControl1;

	private TabPage tabLua;

	private TabPage tabBefore;

	private TabPage tabAfter;

	private SyntaxRichTextBox txtLua;

	private SyntaxRichTextBox txtLuaBefore;

	private SyntaxRichTextBox txtLuaAfter;

	private ContextMenuStrip contextLuaBefore;

	private ToolStripMenuItem toolStripMenuItem2;

	private ToolStripMenuItem toolStripMenuItem3;

	private ToolStripSeparator toolStripSeparator1;

	private ToolStripMenuItem toolStripMenuItem4;

	private ToolStripMenuItem toolStripMenuItem5;

	private ToolStripMenuItem toolStripMenuItem6;

	private ContextMenuStrip contextLuaAfter;

	private ToolStripMenuItem toolStripMenuItem7;

	private ToolStripMenuItem toolStripMenuItem8;

	private ToolStripSeparator toolStripSeparator2;

	private ToolStripMenuItem toolStripMenuItem9;

	private ToolStripMenuItem toolStripMenuItem10;

	private ToolStripMenuItem toolStripMenuItem11;

	public frmAbilityEditor()
	{
		InitializeComponent();
	}

	private void frmAbilityEditor_Load(object sender, EventArgs e)
	{
		((Form)Program.abilityForm).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		GlobalSettings.AbilityEditorLoaded = true;
		frmMain.LockdownMain(trueFalse: true);
		isLoading = true;
		isLoading = false;
		((Control)btnDeleteSelected).Enabled = false;
		((Control)btnCopyAbility).Enabled = false;
		((Control)lstAbilities).Enabled = false;
		lstAbilities.Items.Clear();
		((Control)cmbProfile).Enabled = false;
		((Control)btnSave).Enabled = false;
		txtLua.Settings.Keywords.Add("or");
		txtLua.Settings.Keywords.Add("local");
		txtLua.Settings.Keywords.Add("if");
		txtLua.Settings.Keywords.Add("then");
		txtLua.Settings.Keywords.Add("and");
		txtLua.Settings.Keywords.Add("else");
		txtLua.Settings.Keywords.Add("elseif");
		txtLua.Settings.Keywords.Add("end");
		txtLua.Settings.Keywords.Add("true");
		txtLua.Settings.Keywords.Add("false");
		txtLua.Settings.Keywords.Add("nil");
		txtLua.Settings.Keywords.Add("return");
		txtLua.Settings.Comment = "--";
		txtLua.Settings.KeywordColor = Color.Blue;
		txtLua.Settings.CommentColor = Color.Green;
		txtLua.Settings.StringColor = Color.Gray;
		txtLua.Settings.IntegerColor = Color.Red;
		txtLua.Settings.EnableStrings = false;
		txtLua.Settings.EnableIntegers = false;
		txtLua.CompileKeywords();
		txtLuaBefore.Settings.Keywords.Add("or");
		txtLuaBefore.Settings.Keywords.Add("local");
		txtLuaBefore.Settings.Keywords.Add("and");
		txtLuaBefore.Settings.Keywords.Add("if");
		txtLuaBefore.Settings.Keywords.Add("then");
		txtLuaBefore.Settings.Keywords.Add("else");
		txtLuaBefore.Settings.Keywords.Add("elseif");
		txtLuaBefore.Settings.Keywords.Add("end");
		txtLuaBefore.Settings.Keywords.Add("true");
		txtLuaBefore.Settings.Keywords.Add("false");
		txtLuaBefore.Settings.Keywords.Add("nil");
		txtLuaBefore.Settings.Keywords.Add("return");
		txtLuaBefore.Settings.Comment = "--";
		txtLuaBefore.Settings.KeywordColor = Color.Blue;
		txtLuaBefore.Settings.CommentColor = Color.Green;
		txtLuaBefore.Settings.StringColor = Color.Gray;
		txtLuaBefore.Settings.IntegerColor = Color.Red;
		txtLuaBefore.Settings.EnableStrings = false;
		txtLuaBefore.Settings.EnableIntegers = false;
		txtLuaBefore.CompileKeywords();
		txtLuaAfter.Settings.Keywords.Add("or");
		txtLuaAfter.Settings.Keywords.Add("local");
		txtLuaAfter.Settings.Keywords.Add("and");
		txtLuaAfter.Settings.Keywords.Add("if");
		txtLuaAfter.Settings.Keywords.Add("then");
		txtLuaAfter.Settings.Keywords.Add("else");
		txtLuaAfter.Settings.Keywords.Add("elseif");
		txtLuaAfter.Settings.Keywords.Add("end");
		txtLuaAfter.Settings.Keywords.Add("true");
		txtLuaAfter.Settings.Keywords.Add("false");
		txtLuaAfter.Settings.Keywords.Add("nil");
		txtLuaAfter.Settings.Keywords.Add("return");
		txtLuaAfter.Settings.Comment = "--";
		txtLuaAfter.Settings.KeywordColor = Color.Blue;
		txtLuaAfter.Settings.CommentColor = Color.Green;
		txtLuaAfter.Settings.StringColor = Color.Gray;
		txtLuaAfter.Settings.IntegerColor = Color.Red;
		txtLuaAfter.Settings.EnableStrings = false;
		txtLuaAfter.Settings.EnableIntegers = false;
		txtLuaAfter.CompileKeywords();
	}

	private void cmbClass_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!isLoading && cmbClass.SelectedItem != null)
		{
			LoadProfileList();
			((Control)cmbProfile).Enabled = true;
		}
	}

	private void lstAbilities_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (!isLoading && lstAbilities.SelectedItem != null)
		{
			LoadAbility(lstAbilities.SelectedItem.ToString());
		}
	}

	private void btnDeleteSelected_Click(object sender, EventArgs e)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		if (lstAbilities.SelectedItem == null)
		{
			return;
		}
		DialogResult val = MessageBox.Show("Are you sure that you would like to delete the selected ability? This cannot be undone.", "Delete Ability?", (MessageBoxButtons)4, (MessageBoxIcon)32);
		if ((int)val == 7)
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] == lstAbilities.SelectedItem.ToString())
			{
				abilityArray[i, 0] = null;
				abilityArray[i, 1] = null;
				abilityArray[i, 2] = null;
				abilityArray[i, 3] = null;
				abilityArray[i, 4] = null;
				abilityArray[i, 5] = null;
				abilityArray[i, 6] = null;
				abilityArray[i, 7] = null;
				abilityArray[i, 8] = null;
				abilityArray[i, 9] = null;
			}
		}
		PopulateAbilities();
		((Control)txtSpellName).Text = "";
		((Control)txtDefault).Text = "false";
		lstBoxActions.Items.Clear();
		((Control)txtSpellID).Text = "";
		((Control)txtLua).Text = "";
		((Control)txtLuaBefore).Text = "";
		((Control)txtLuaAfter).Text = "";
		((Control)txtRecastDelay).Text = "0";
		cmbTarget.SelectedItem = "Target";
		cmbCancel.SelectedItem = "False";
		if (cmbClass.SelectedItem != null)
		{
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnAddAction_Click(object sender, EventArgs e)
	{
		if (((Control)txtAddAction).Text.Replace(" ", "") != "")
		{
			lstBoxActions.Items.Add((object)((Control)txtAddAction).Text.Trim());
			((Control)txtAddAction).Text = "";
		}
	}

	private void btnDeleteAction_Click(object sender, EventArgs e)
	{
		if (lstBoxActions.SelectedItem != null)
		{
			lstBoxActions.Items.Remove(lstBoxActions.SelectedItem);
		}
	}

	private void btnSave_Click(object sender, EventArgs e)
	{
		SaveAbility(((Control)txtSpellName).Text, ((Control)txtDefault).Text, ((IEnumerable)lstBoxActions.Items).OfType<string>().ToList(), ((Control)txtSpellID).Text, ((Control)txtLua).Text, ((Control)txtRecastDelay).Text, cmbTarget.SelectedItem.ToString(), cmbCancel.SelectedItem.ToString(), ((Control)txtLuaBefore).Text, ((Control)txtLuaAfter).Text);
	}

	private void LoadClass(string sClass, string sProfile)
	{
		isLoading = true;
		lstAbilities.Items.Clear();
		((Control)txtSpellName).Text = "";
		((Control)txtDefault).Text = "false";
		lstBoxActions.Items.Clear();
		((Control)txtSpellID).Text = "";
		((Control)txtLua).Text = "";
		((Control)txtLuaBefore).Text = "";
		((Control)txtLuaAfter).Text = "";
		((Control)txtRecastDelay).Text = "0";
		cmbTarget.SelectedItem = "Target";
		cmbCancel.SelectedItem = "False";
		abilityArray = clsXML.LoadXML_Abilities(sProfile + "_" + sClass);
		PopulateAbilities();
		isLoading = false;
	}

	public static void SaveClass(string sClass, string sProfile)
	{
		clsXML.SaveXML_Abilities(sClass, sProfile, abilityArray);
	}

	private void LoadAbility(string strAbility)
	{
		isLoading = true;
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] == strAbility)
			{
				((Control)txtSpellName).Text = abilityArray[i, 0];
				((Control)txtDefault).Text = abilityArray[i, 1];
				((Control)txtSpellID).Text = abilityArray[i, 2];
				PopulateActions(abilityArray[i, 3]);
				((Control)txtLua).Text = abilityArray[i, 4];
				((Control)txtRecastDelay).Text = abilityArray[i, 5];
				((Control)cmbTarget).Text = abilityArray[i, 6];
				((Control)cmbCancel).Text = abilityArray[i, 7];
				((Control)txtLuaBefore).Text = abilityArray[i, 8];
				((Control)txtLuaAfter).Text = abilityArray[i, 9];
				txtLua.ProcessAllLines();
				txtLuaBefore.ProcessAllLines();
				txtLuaAfter.ProcessAllLines();
				tabControl1.SelectedIndex = 0;
			}
		}
		isLoading = false;
	}

	private void SaveAbility(string strAbility, string strDefault, List<string> ActionList, string strSpellID, string strLua, string recastDelay, string Target, string CancelChannel, string LuaBefore, string LuaAfter)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (strAbility.Replace(" ", "") == "" || strDefault.Replace(" ", "") == "" || !Information.IsNumeric((object)strSpellID.Trim()))
		{
			MessageBox.Show("You must specify an ability name and a spell ID (use 0 if no spell cast) to save an ability.");
			return;
		}
		if (!Information.IsNumeric((object)recastDelay.Trim()))
		{
			MessageBox.Show("You must specify a numeric recast delay value. A recast delay is the time in milliseconds that the bot will wait before recasting this ability after a successful cast.");
			return;
		}
		if (strLua.Replace(" ", "") == "")
		{
			strLua = "return true";
		}
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] == strAbility || abilityArray[i, 0] == null)
			{
				abilityArray[i, 0] = strAbility.Trim();
				abilityArray[i, 1] = strDefault.Trim();
				abilityArray[i, 2] = strSpellID.Trim();
				abilityArray[i, 3] = ActionString(ActionList);
				abilityArray[i, 4] = strLua.Trim();
				abilityArray[i, 5] = recastDelay.Trim();
				abilityArray[i, 6] = Target.Trim();
				abilityArray[i, 7] = CancelChannel.Trim();
				abilityArray[i, 8] = LuaBefore.Trim();
				abilityArray[i, 9] = LuaAfter.Trim();
				break;
			}
		}
		((Control)txtSpellName).Text = "";
		((Control)txtDefault).Text = "false";
		lstBoxActions.Items.Clear();
		((Control)txtAddAction).Text = "";
		((Control)txtSpellID).Text = "";
		((Control)txtLua).Text = "";
		((Control)txtLuaBefore).Text = "";
		((Control)txtLuaAfter).Text = "";
		((Control)txtRecastDelay).Text = "0";
		cmbTarget.SelectedItem = "Target";
		cmbCancel.SelectedItem = "False";
		PopulateAbilities();
		if (cmbClass.SelectedItem != null)
		{
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void PopulateAbilities()
	{
		isLoading = true;
		lstAbilities.Items.Clear();
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] != null)
			{
				lstAbilities.Items.Add((object)abilityArray[i, 0]);
			}
		}
		isLoading = false;
	}

	private void PopulateActions(string strActions)
	{
		lstBoxActions.Items.Clear();
		string[] array = strActions.Split(new char[1] { '|' });
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.Replace(" ", "") != "")
			{
				lstBoxActions.Items.Add((object)text);
			}
		}
	}

	private string ActionString(List<string> myActions)
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

	private void frmAbilityEditor_Shown(object sender, EventArgs e)
	{
	}

	private void frmAbilityEditor_FormClosing(object sender, FormClosingEventArgs e)
	{
		frmMain.LockdownMain(trueFalse: false);
		GlobalSettings.AbilityEditorLoaded = false;
	}

	private void cmbTarget_SelectedIndexChanged(object sender, EventArgs e)
	{
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
		}
		cmbProfile.SelectedItem = "";
		((Control)btnDeleteSelected).Enabled = false;
		((Control)btnCopyAbility).Enabled = false;
		((Control)lstAbilities).Enabled = false;
		lstAbilities.Items.Clear();
	}

	private void cmbProfile_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cmbProfile.SelectedItem == null || cmbProfile.SelectedItem.ToString().Trim() == "")
		{
			((Control)btnDeleteSelected).Enabled = false;
			((Control)btnCopyAbility).Enabled = false;
			((Control)lstAbilities).Enabled = false;
			lstAbilities.Items.Clear();
			((Control)btnSave).Enabled = false;
		}
		else
		{
			((Control)btnDeleteSelected).Enabled = true;
			((Control)btnCopyAbility).Enabled = true;
			((Control)lstAbilities).Enabled = true;
			((Control)btnSave).Enabled = true;
			LoadClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void btnCopyAbility_Click(object sender, EventArgs e)
	{
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		if (cmbClass.SelectedItem == null || cmbProfile.SelectedItem == null || cmbClass.SelectedItem.ToString() == "" || cmbProfile.SelectedItem.ToString() == "" || lstAbilities.SelectedItem == null)
		{
			return;
		}
		string text = lstAbilities.SelectedItem.ToString().Trim();
		int num = -1;
		int num2 = -1;
		if (!(text != ""))
		{
			return;
		}
		string text2 = Interaction.InputBox("Enter the name of the new ability. Note that this cannot match an already existing ability from this profile.", "Ability Name", "", -1, -1);
		if (text2.Contains("(") || text2.Contains(")"))
		{
			text2.Replace("(", "");
			text2.Replace(")", "");
		}
		if (text2 == null || text2 == "")
		{
			return;
		}
		for (int i = 0; i < 1024; i++)
		{
			if (abilityArray[i, 0] != null && abilityArray[i, 0].ToUpper() == text2.ToUpper())
			{
				MessageBox.Show("The name you have selected is already in use. Please select a new name for this rotation.");
				return;
			}
			if (abilityArray[i, 0] == text)
			{
				num = i;
			}
			if (abilityArray[i, 0] == null && num2 == -1)
			{
				num2 = i;
			}
		}
		if (num != -1 && num2 != -1)
		{
			abilityArray[num2, 0] = text2;
			abilityArray[num2, 1] = "false";
			abilityArray[num2, 2] = abilityArray[num, 2];
			abilityArray[num2, 3] = abilityArray[num, 3];
			abilityArray[num2, 4] = abilityArray[num, 4];
			abilityArray[num2, 5] = abilityArray[num, 5];
			abilityArray[num2, 6] = abilityArray[num, 6];
			abilityArray[num2, 7] = abilityArray[num, 7];
			abilityArray[num2, 8] = abilityArray[num, 8];
			abilityArray[num2, 9] = abilityArray[num, 9];
			SaveClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
			LoadClass(cmbClass.SelectedItem.ToString(), cmbProfile.SelectedItem.ToString());
		}
	}

	private void txtLua_TextChanged(object sender, EventArgs e)
	{
		txtLua.ProcessAllLines();
	}

	private void cutCtrlXToolStripMenuItem_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLua).Cut();
	}

	private void copyCtrlCToolStripMenuItem_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLua).Copy();
	}

	private void pasteCtrlVToolStripMenuItem_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLua).Paste();
	}

	private void undoCtrlZToolStripMenuItem_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLua).Undo();
	}

	private void redoCTRLYToolStripMenuItem_Click(object sender, EventArgs e)
	{
		((RichTextBox)txtLua).Redo();
	}

	private void txtLuaBefore_TextChanged(object sender, EventArgs e)
	{
		txtLuaBefore.ProcessAllLines();
	}

	private void toolStripMenuItem2_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaBefore).Undo();
	}

	private void toolStripMenuItem3_Click(object sender, EventArgs e)
	{
		((RichTextBox)txtLuaBefore).Redo();
	}

	private void toolStripMenuItem4_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaBefore).Cut();
	}

	private void toolStripMenuItem5_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaBefore).Copy();
	}

	private void toolStripMenuItem6_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaBefore).Paste();
	}

	private void toolStripMenuItem7_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaAfter).Undo();
	}

	private void toolStripMenuItem8_Click(object sender, EventArgs e)
	{
		((RichTextBox)txtLuaAfter).Redo();
	}

	private void toolStripMenuItem9_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaAfter).Cut();
	}

	private void toolStripMenuItem10_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaAfter).Copy();
	}

	private void toolStripMenuItem11_Click(object sender, EventArgs e)
	{
		((TextBoxBase)txtLuaAfter).Paste();
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
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
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
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Expected O, but got Unknown
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected O, but got Unknown
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Expected O, but got Unknown
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Expected O, but got Unknown
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Expected O, but got Unknown
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected O, but got Unknown
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Expected O, but got Unknown
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Expected O, but got Unknown
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Expected O, but got Unknown
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Expected O, but got Unknown
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Expected O, but got Unknown
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Expected O, but got Unknown
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Expected O, but got Unknown
		//IL_113c: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1abe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ac8: Expected O, but got Unknown
		components = new Container();
		label5 = new Label();
		txtDefault = new TextBox();
		btnDeleteSelected = new Button();
		btnSave = new Button();
		label7 = new Label();
		label6 = new Label();
		cmbClass = new ComboBox();
		btnDeleteAction = new Button();
		txtAddAction = new TextBox();
		lstBoxActions = new ListBox();
		btnAddAction = new Button();
		label3 = new Label();
		label2 = new Label();
		label1 = new Label();
		txtSpellID = new TextBox();
		txtSpellName = new TextBox();
		lstAbilities = new ListBox();
		label8 = new Label();
		label9 = new Label();
		txtRecastDelay = new TextBox();
		cmbTarget = new ComboBox();
		cmbCancel = new ComboBox();
		label10 = new Label();
		btnCopyAbility = new Button();
		lblProfile = new Label();
		cmbProfile = new ComboBox();
		contextLua = new ContextMenuStrip(components);
		undoCtrlZToolStripMenuItem = new ToolStripMenuItem();
		redoCTRLYToolStripMenuItem = new ToolStripMenuItem();
		toolStripMenuItem1 = new ToolStripSeparator();
		cutCtrlXToolStripMenuItem = new ToolStripMenuItem();
		copyCtrlCToolStripMenuItem = new ToolStripMenuItem();
		pasteCtrlVToolStripMenuItem = new ToolStripMenuItem();
		tabControl1 = new TabControl();
		tabLua = new TabPage();
		tabBefore = new TabPage();
		tabAfter = new TabPage();
		txtLuaBefore = new SyntaxRichTextBox();
		txtLuaAfter = new SyntaxRichTextBox();
		txtLua = new SyntaxRichTextBox();
		contextLuaBefore = new ContextMenuStrip(components);
		toolStripMenuItem2 = new ToolStripMenuItem();
		toolStripMenuItem3 = new ToolStripMenuItem();
		toolStripSeparator1 = new ToolStripSeparator();
		toolStripMenuItem4 = new ToolStripMenuItem();
		toolStripMenuItem5 = new ToolStripMenuItem();
		toolStripMenuItem6 = new ToolStripMenuItem();
		contextLuaAfter = new ContextMenuStrip(components);
		toolStripMenuItem7 = new ToolStripMenuItem();
		toolStripMenuItem8 = new ToolStripMenuItem();
		toolStripSeparator2 = new ToolStripSeparator();
		toolStripMenuItem9 = new ToolStripMenuItem();
		toolStripMenuItem10 = new ToolStripMenuItem();
		toolStripMenuItem11 = new ToolStripMenuItem();
		((Control)contextLua).SuspendLayout();
		((Control)tabControl1).SuspendLayout();
		((Control)tabLua).SuspendLayout();
		((Control)tabBefore).SuspendLayout();
		((Control)tabAfter).SuspendLayout();
		((Control)contextLuaBefore).SuspendLayout();
		((Control)contextLuaAfter).SuspendLayout();
		((Control)this).SuspendLayout();
		((Control)label5).AutoSize = true;
		((Control)label5).Location = new Point(379, 8);
		((Control)label5).Name = "label5";
		((Control)label5).Size = new Size(44, 13);
		((Control)label5).TabIndex = 46;
		((Control)label5).Text = "Default:";
		((Control)txtDefault).Enabled = false;
		((Control)txtDefault).Location = new Point(429, 5);
		((Control)txtDefault).Name = "txtDefault";
		((Control)txtDefault).Size = new Size(90, 20);
		((Control)txtDefault).TabIndex = 45;
		txtDefault.TextAlign = (HorizontalAlignment)1;
		((Control)btnDeleteSelected).Location = new Point(8, 376);
		((Control)btnDeleteSelected).Name = "btnDeleteSelected";
		((Control)btnDeleteSelected).Size = new Size(180, 23);
		((Control)btnDeleteSelected).TabIndex = 44;
		((Control)btnDeleteSelected).Text = "Delete Selected";
		((ButtonBase)btnDeleteSelected).UseVisualStyleBackColor = true;
		((Control)btnDeleteSelected).Click += btnDeleteSelected_Click;
		((Control)btnSave).Location = new Point(683, 69);
		((Control)btnSave).Name = "btnSave";
		((Control)btnSave).Size = new Size(137, 23);
		((Control)btnSave).TabIndex = 43;
		((Control)btnSave).Text = "Save";
		((ButtonBase)btnSave).UseVisualStyleBackColor = true;
		((Control)btnSave).Click += btnSave_Click;
		((Control)label7).AutoSize = true;
		((Control)label7).Location = new Point(80, 64);
		((Control)label7).Name = "label7";
		((Control)label7).Size = new Size(42, 13);
		((Control)label7).TabIndex = 42;
		((Control)label7).Text = "Abilities";
		((Control)label6).AutoSize = true;
		((Control)label6).Location = new Point(5, 8);
		((Control)label6).Name = "label6";
		((Control)label6).Size = new Size(35, 13);
		((Control)label6).TabIndex = 41;
		((Control)label6).Text = "Class:";
		cmbClass.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbClass).FormattingEnabled = true;
		cmbClass.Items.AddRange(new object[10] { "DEATHKNIGHT", "DRUID", "HUNTER", "MAGE", "PALADIN", "PRIEST", "ROGUE", "SHAMAN", "WARLOCK", "WARRIOR" });
		((Control)cmbClass).Location = new Point(44, 5);
		((Control)cmbClass).Name = "cmbClass";
		((Control)cmbClass).Size = new Size(144, 21);
		((Control)cmbClass).TabIndex = 40;
		cmbClass.SelectedIndexChanged += cmbClass_SelectedIndexChanged;
		((ButtonBase)btnDeleteAction).FlatStyle = (FlatStyle)3;
		((Control)btnDeleteAction).Location = new Point(382, 69);
		((Control)btnDeleteAction).Name = "btnDeleteAction";
		((Control)btnDeleteAction).Size = new Size(64, 23);
		((Control)btnDeleteAction).TabIndex = 39;
		((Control)btnDeleteAction).Text = "Delete";
		((ButtonBase)btnDeleteAction).UseVisualStyleBackColor = true;
		((Control)btnDeleteAction).Click += btnDeleteAction_Click;
		((Control)txtAddAction).Location = new Point(382, 37);
		((Control)txtAddAction).Name = "txtAddAction";
		((Control)txtAddAction).Size = new Size(137, 20);
		((Control)txtAddAction).TabIndex = 38;
		txtAddAction.TextAlign = (HorizontalAlignment)1;
		((ListControl)lstBoxActions).FormattingEnabled = true;
		((Control)lstBoxActions).Location = new Point(243, 36);
		((Control)lstBoxActions).Name = "lstBoxActions";
		((Control)lstBoxActions).Size = new Size(127, 56);
		((Control)lstBoxActions).TabIndex = 37;
		((Control)btnAddAction).Location = new Point(455, 69);
		((Control)btnAddAction).Name = "btnAddAction";
		((Control)btnAddAction).Size = new Size(64, 23);
		((Control)btnAddAction).TabIndex = 36;
		((Control)btnAddAction).Text = "Add";
		((ButtonBase)btnAddAction).UseVisualStyleBackColor = true;
		((Control)btnAddAction).Click += btnAddAction_Click;
		((Control)label3).AutoSize = true;
		((Control)label3).Location = new Point(192, 36);
		((Control)label3).Name = "label3";
		((Control)label3).Size = new Size(45, 13);
		((Control)label3).TabIndex = 34;
		((Control)label3).Text = "Actions:";
		((Control)label2).AutoSize = true;
		((Control)label2).Location = new Point(523, 7);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(47, 13);
		((Control)label2).TabIndex = 33;
		((Control)label2).Text = "Spell ID:";
		((Control)label1).AutoSize = true;
		((Control)label1).Location = new Point(199, 8);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(38, 13);
		((Control)label1).TabIndex = 32;
		((Control)label1).Text = "Name:";
		((Control)txtSpellID).Location = new Point(579, 5);
		((Control)txtSpellID).Name = "txtSpellID";
		((Control)txtSpellID).Size = new Size(90, 20);
		((Control)txtSpellID).TabIndex = 30;
		txtSpellID.TextAlign = (HorizontalAlignment)1;
		((Control)txtSpellName).Location = new Point(243, 5);
		((Control)txtSpellName).Name = "txtSpellName";
		((Control)txtSpellName).Size = new Size(127, 20);
		((Control)txtSpellName).TabIndex = 29;
		txtSpellName.TextAlign = (HorizontalAlignment)1;
		((ListControl)lstAbilities).FormattingEnabled = true;
		((Control)lstAbilities).Location = new Point(12, 80);
		((Control)lstAbilities).Name = "lstAbilities";
		((Control)lstAbilities).Size = new Size(176, 264);
		lstAbilities.Sorted = true;
		((Control)lstAbilities).TabIndex = 28;
		lstAbilities.SelectedIndexChanged += lstAbilities_SelectedIndexChanged;
		((Control)label8).AutoSize = true;
		((Control)label8).Location = new Point(529, 74);
		((Control)label8).Name = "label8";
		((Control)label8).Size = new Size(41, 13);
		((Control)label8).TabIndex = 50;
		((Control)label8).Text = "Target:";
		((Control)label9).AutoSize = true;
		((Control)label9).Location = new Point(533, 40);
		((Control)label9).Name = "label9";
		((Control)label9).Size = new Size(37, 13);
		((Control)label9).TabIndex = 48;
		((Control)label9).Text = "Delay:";
		((Control)txtRecastDelay).Location = new Point(580, 37);
		((Control)txtRecastDelay).Name = "txtRecastDelay";
		((Control)txtRecastDelay).Size = new Size(90, 20);
		((Control)txtRecastDelay).TabIndex = 47;
		((Control)txtRecastDelay).Text = "0";
		txtRecastDelay.TextAlign = (HorizontalAlignment)1;
		cmbTarget.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbTarget).FormattingEnabled = true;
		cmbTarget.Items.AddRange(new object[7] { "Target", "Focus", "Pet", "Player", "Mouseover", "Click", "Custom" });
		((Control)cmbTarget).Location = new Point(579, 71);
		((Control)cmbTarget).Name = "cmbTarget";
		((Control)cmbTarget).Size = new Size(90, 21);
		((Control)cmbTarget).TabIndex = 51;
		cmbTarget.SelectedIndexChanged += cmbTarget_SelectedIndexChanged;
		cmbCancel.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbCancel).FormattingEnabled = true;
		cmbCancel.Items.AddRange(new object[2] { "False", "True" });
		((Control)cmbCancel).Location = new Point(765, 5);
		((Control)cmbCancel).Name = "cmbCancel";
		((Control)cmbCancel).Size = new Size(49, 21);
		((Control)cmbCancel).TabIndex = 53;
		((Control)label10).AutoSize = true;
		((Control)label10).Location = new Point(674, 8);
		((Control)label10).Name = "label10";
		((Control)label10).Size = new Size(85, 13);
		((Control)label10).TabIndex = 52;
		((Control)label10).Text = "Cancel Channel:";
		((Control)btnCopyAbility).Location = new Point(8, 349);
		((Control)btnCopyAbility).Name = "btnCopyAbility";
		((Control)btnCopyAbility).Size = new Size(180, 23);
		((Control)btnCopyAbility).TabIndex = 54;
		((Control)btnCopyAbility).Text = "Copy Selected";
		((ButtonBase)btnCopyAbility).UseVisualStyleBackColor = true;
		((Control)btnCopyAbility).Click += btnCopyAbility_Click;
		((Control)lblProfile).AutoSize = true;
		((Control)lblProfile).Location = new Point(5, 32);
		((Control)lblProfile).Name = "lblProfile";
		((Control)lblProfile).Size = new Size(39, 13);
		((Control)lblProfile).TabIndex = 56;
		((Control)lblProfile).Text = "Profile:";
		cmbProfile.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbProfile).FormattingEnabled = true;
		((Control)cmbProfile).Location = new Point(44, 29);
		((Control)cmbProfile).Name = "cmbProfile";
		((Control)cmbProfile).Size = new Size(144, 21);
		((Control)cmbProfile).TabIndex = 55;
		cmbProfile.SelectedIndexChanged += cmbProfile_SelectedIndexChanged;
		((ToolStrip)contextLua).Items.AddRange((ToolStripItem[])(object)new ToolStripItem[6]
		{
			(ToolStripItem)undoCtrlZToolStripMenuItem,
			(ToolStripItem)redoCTRLYToolStripMenuItem,
			(ToolStripItem)toolStripMenuItem1,
			(ToolStripItem)cutCtrlXToolStripMenuItem,
			(ToolStripItem)copyCtrlCToolStripMenuItem,
			(ToolStripItem)pasteCtrlVToolStripMenuItem
		});
		((Control)contextLua).Name = "contextMenuStrip1";
		((Control)contextLua).Size = new Size(162, 120);
		((ToolStripItem)undoCtrlZToolStripMenuItem).Name = "undoCtrlZToolStripMenuItem";
		((ToolStripItem)undoCtrlZToolStripMenuItem).Size = new Size(161, 22);
		((ToolStripItem)undoCtrlZToolStripMenuItem).Text = "Undo (Ctrl + Z)";
		((ToolStripItem)undoCtrlZToolStripMenuItem).Click += undoCtrlZToolStripMenuItem_Click;
		((ToolStripItem)redoCTRLYToolStripMenuItem).Name = "redoCTRLYToolStripMenuItem";
		((ToolStripItem)redoCTRLYToolStripMenuItem).Size = new Size(161, 22);
		((ToolStripItem)redoCTRLYToolStripMenuItem).Text = "Redo (CTRL + Y)";
		((ToolStripItem)redoCTRLYToolStripMenuItem).Click += redoCTRLYToolStripMenuItem_Click;
		((ToolStripItem)toolStripMenuItem1).Name = "toolStripMenuItem1";
		((ToolStripItem)toolStripMenuItem1).Size = new Size(158, 6);
		((ToolStripItem)cutCtrlXToolStripMenuItem).Name = "cutCtrlXToolStripMenuItem";
		((ToolStripItem)cutCtrlXToolStripMenuItem).Size = new Size(161, 22);
		((ToolStripItem)cutCtrlXToolStripMenuItem).Text = "Cut (Ctrl + X)";
		((ToolStripItem)cutCtrlXToolStripMenuItem).Click += cutCtrlXToolStripMenuItem_Click;
		((ToolStripItem)copyCtrlCToolStripMenuItem).Name = "copyCtrlCToolStripMenuItem";
		((ToolStripItem)copyCtrlCToolStripMenuItem).Size = new Size(161, 22);
		((ToolStripItem)copyCtrlCToolStripMenuItem).Text = "Copy (Ctrl + C)";
		((ToolStripItem)copyCtrlCToolStripMenuItem).Click += copyCtrlCToolStripMenuItem_Click;
		((ToolStripItem)pasteCtrlVToolStripMenuItem).Name = "pasteCtrlVToolStripMenuItem";
		((ToolStripItem)pasteCtrlVToolStripMenuItem).Size = new Size(161, 22);
		((ToolStripItem)pasteCtrlVToolStripMenuItem).Text = "Paste (Ctrl + V)";
		((ToolStripItem)pasteCtrlVToolStripMenuItem).Click += pasteCtrlVToolStripMenuItem_Click;
		((Control)tabControl1).Controls.Add((Control)(object)tabLua);
		((Control)tabControl1).Controls.Add((Control)(object)tabBefore);
		((Control)tabControl1).Controls.Add((Control)(object)tabAfter);
		((Control)tabControl1).Location = new Point(202, 98);
		((Control)tabControl1).Name = "tabControl1";
		tabControl1.SelectedIndex = 0;
		((Control)tabControl1).Size = new Size(612, 305);
		((Control)tabControl1).TabIndex = 58;
		((Control)tabLua).Controls.Add((Control)(object)txtLua);
		tabLua.Location = new Point(4, 22);
		((Control)tabLua).Name = "tabLua";
		((Control)tabLua).Padding = new Padding(3);
		((Control)tabLua).Size = new Size(604, 279);
		tabLua.TabIndex = 0;
		((Control)tabLua).Text = "Test";
		tabLua.UseVisualStyleBackColor = true;
		((Control)tabBefore).Controls.Add((Control)(object)txtLuaBefore);
		tabBefore.Location = new Point(4, 22);
		((Control)tabBefore).Name = "tabBefore";
		((Control)tabBefore).Padding = new Padding(3);
		((Control)tabBefore).Size = new Size(604, 279);
		tabBefore.TabIndex = 1;
		((Control)tabBefore).Text = "Before Perform (Lua)";
		tabBefore.UseVisualStyleBackColor = true;
		((Control)tabAfter).Controls.Add((Control)(object)txtLuaAfter);
		tabAfter.Location = new Point(4, 22);
		((Control)tabAfter).Name = "tabAfter";
		((Control)tabAfter).Size = new Size(604, 279);
		tabAfter.TabIndex = 2;
		((Control)tabAfter).Text = "After Perform (Lua)";
		tabAfter.UseVisualStyleBackColor = true;
		((TextBoxBase)txtLuaBefore).AcceptsTab = true;
		((Control)txtLuaBefore).ContextMenuStrip = contextLuaBefore;
		((Control)txtLuaBefore).Location = new Point(-2, -2);
		((Control)txtLuaBefore).Name = "txtLuaBefore";
		((RichTextBox)txtLuaBefore).ScrollBars = (RichTextBoxScrollBars)18;
		((Control)txtLuaBefore).Size = new Size(608, 283);
		((Control)txtLuaBefore).TabIndex = 59;
		((Control)txtLuaBefore).Text = "";
		((Control)txtLuaBefore).TextChanged += txtLuaBefore_TextChanged;
		((TextBoxBase)txtLuaAfter).AcceptsTab = true;
		((Control)txtLuaAfter).ContextMenuStrip = contextLuaAfter;
		((Control)txtLuaAfter).Location = new Point(-2, -2);
		((Control)txtLuaAfter).Name = "txtLuaAfter";
		((RichTextBox)txtLuaAfter).ScrollBars = (RichTextBoxScrollBars)18;
		((Control)txtLuaAfter).Size = new Size(608, 283);
		((Control)txtLuaAfter).TabIndex = 59;
		((Control)txtLuaAfter).Text = "";
		((TextBoxBase)txtLua).AcceptsTab = true;
		((Control)txtLua).ContextMenuStrip = contextLua;
		((Control)txtLua).Location = new Point(-2, -2);
		((Control)txtLua).Name = "txtLua";
		((RichTextBox)txtLua).ScrollBars = (RichTextBoxScrollBars)18;
		((Control)txtLua).Size = new Size(608, 283);
		((Control)txtLua).TabIndex = 59;
		((Control)txtLua).Text = "";
		((ToolStrip)contextLuaBefore).Items.AddRange((ToolStripItem[])(object)new ToolStripItem[6]
		{
			(ToolStripItem)toolStripMenuItem2,
			(ToolStripItem)toolStripMenuItem3,
			(ToolStripItem)toolStripSeparator1,
			(ToolStripItem)toolStripMenuItem4,
			(ToolStripItem)toolStripMenuItem5,
			(ToolStripItem)toolStripMenuItem6
		});
		((Control)contextLuaBefore).Name = "contextMenuStrip1";
		((Control)contextLuaBefore).Size = new Size(162, 120);
		((ToolStripItem)toolStripMenuItem2).Name = "toolStripMenuItem2";
		((ToolStripItem)toolStripMenuItem2).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem2).Text = "Undo (Ctrl + Z)";
		((ToolStripItem)toolStripMenuItem2).Click += toolStripMenuItem2_Click;
		((ToolStripItem)toolStripMenuItem3).Name = "toolStripMenuItem3";
		((ToolStripItem)toolStripMenuItem3).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem3).Text = "Redo (CTRL + Y)";
		((ToolStripItem)toolStripMenuItem3).Click += toolStripMenuItem3_Click;
		((ToolStripItem)toolStripSeparator1).Name = "toolStripSeparator1";
		((ToolStripItem)toolStripSeparator1).Size = new Size(158, 6);
		((ToolStripItem)toolStripMenuItem4).Name = "toolStripMenuItem4";
		((ToolStripItem)toolStripMenuItem4).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem4).Text = "Cut (Ctrl + X)";
		((ToolStripItem)toolStripMenuItem4).Click += toolStripMenuItem4_Click;
		((ToolStripItem)toolStripMenuItem5).Name = "toolStripMenuItem5";
		((ToolStripItem)toolStripMenuItem5).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem5).Text = "Copy (Ctrl + C)";
		((ToolStripItem)toolStripMenuItem5).Click += toolStripMenuItem5_Click;
		((ToolStripItem)toolStripMenuItem6).Name = "toolStripMenuItem6";
		((ToolStripItem)toolStripMenuItem6).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem6).Text = "Paste (Ctrl + V)";
		((ToolStripItem)toolStripMenuItem6).Click += toolStripMenuItem6_Click;
		((ToolStrip)contextLuaAfter).Items.AddRange((ToolStripItem[])(object)new ToolStripItem[6]
		{
			(ToolStripItem)toolStripMenuItem7,
			(ToolStripItem)toolStripMenuItem8,
			(ToolStripItem)toolStripSeparator2,
			(ToolStripItem)toolStripMenuItem9,
			(ToolStripItem)toolStripMenuItem10,
			(ToolStripItem)toolStripMenuItem11
		});
		((Control)contextLuaAfter).Name = "contextMenuStrip1";
		((Control)contextLuaAfter).Size = new Size(162, 120);
		((ToolStripItem)toolStripMenuItem7).Name = "toolStripMenuItem7";
		((ToolStripItem)toolStripMenuItem7).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem7).Text = "Undo (Ctrl + Z)";
		((ToolStripItem)toolStripMenuItem7).Click += toolStripMenuItem7_Click;
		((ToolStripItem)toolStripMenuItem8).Name = "toolStripMenuItem8";
		((ToolStripItem)toolStripMenuItem8).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem8).Text = "Redo (CTRL + Y)";
		((ToolStripItem)toolStripMenuItem8).Click += toolStripMenuItem8_Click;
		((ToolStripItem)toolStripSeparator2).Name = "toolStripSeparator2";
		((ToolStripItem)toolStripSeparator2).Size = new Size(158, 6);
		((ToolStripItem)toolStripMenuItem9).Name = "toolStripMenuItem9";
		((ToolStripItem)toolStripMenuItem9).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem9).Text = "Cut (Ctrl + X)";
		((ToolStripItem)toolStripMenuItem9).Click += toolStripMenuItem9_Click;
		((ToolStripItem)toolStripMenuItem10).Name = "toolStripMenuItem10";
		((ToolStripItem)toolStripMenuItem10).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem10).Text = "Copy (Ctrl + C)";
		((ToolStripItem)toolStripMenuItem10).Click += toolStripMenuItem10_Click;
		((ToolStripItem)toolStripMenuItem11).Name = "toolStripMenuItem11";
		((ToolStripItem)toolStripMenuItem11).Size = new Size(161, 22);
		((ToolStripItem)toolStripMenuItem11).Text = "Paste (Ctrl + V)";
		((ToolStripItem)toolStripMenuItem11).Click += toolStripMenuItem11_Click;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(826, 406);
		((Control)this).Controls.Add((Control)(object)tabControl1);
		((Control)this).Controls.Add((Control)(object)lblProfile);
		((Control)this).Controls.Add((Control)(object)cmbProfile);
		((Control)this).Controls.Add((Control)(object)btnCopyAbility);
		((Control)this).Controls.Add((Control)(object)cmbCancel);
		((Control)this).Controls.Add((Control)(object)label10);
		((Control)this).Controls.Add((Control)(object)cmbTarget);
		((Control)this).Controls.Add((Control)(object)label8);
		((Control)this).Controls.Add((Control)(object)label9);
		((Control)this).Controls.Add((Control)(object)txtRecastDelay);
		((Control)this).Controls.Add((Control)(object)label5);
		((Control)this).Controls.Add((Control)(object)txtDefault);
		((Control)this).Controls.Add((Control)(object)btnDeleteSelected);
		((Control)this).Controls.Add((Control)(object)btnSave);
		((Control)this).Controls.Add((Control)(object)label7);
		((Control)this).Controls.Add((Control)(object)label6);
		((Control)this).Controls.Add((Control)(object)cmbClass);
		((Control)this).Controls.Add((Control)(object)btnDeleteAction);
		((Control)this).Controls.Add((Control)(object)txtAddAction);
		((Control)this).Controls.Add((Control)(object)lstBoxActions);
		((Control)this).Controls.Add((Control)(object)btnAddAction);
		((Control)this).Controls.Add((Control)(object)label3);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)label1);
		((Control)this).Controls.Add((Control)(object)txtSpellID);
		((Control)this).Controls.Add((Control)(object)txtSpellName);
		((Control)this).Controls.Add((Control)(object)lstAbilities);
		((Form)this).MaximizeBox = false;
		((Control)this).MaximumSize = new Size(842, 444);
		((Control)this).MinimumSize = new Size(842, 444);
		((Control)this).Name = "frmAbilityEditor";
		((Control)this).Text = "Rotation - Ability Editor";
		((Form)this).FormClosing += new FormClosingEventHandler(frmAbilityEditor_FormClosing);
		((Form)this).Load += frmAbilityEditor_Load;
		((Form)this).Shown += frmAbilityEditor_Shown;
		((Control)contextLua).ResumeLayout(false);
		((Control)tabControl1).ResumeLayout(false);
		((Control)tabLua).ResumeLayout(false);
		((Control)tabBefore).ResumeLayout(false);
		((Control)tabAfter).ResumeLayout(false);
		((Control)contextLuaBefore).ResumeLayout(false);
		((Control)contextLuaAfter).ResumeLayout(false);
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}

	static frmAbilityEditor()
	{
		isLoading = false;
		abilityArray = new string[1024, 10];
		profileArray = new string[1024, 2];
	}
}
