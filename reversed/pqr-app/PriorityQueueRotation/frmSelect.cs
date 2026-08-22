using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PriorityQueueRotation;

public class frmSelect : Form
{
	private IContainer components;

	private Button btnRefresh;

	private Button btnSelect;

	private ComboBox cmbProcesses;

	private Label label1;

	private Label lblVersion;

	public static List<string> CurrentProcessList;

	public static string[,] OffsetsArray;

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
		btnRefresh = new Button();
		btnSelect = new Button();
		cmbProcesses = new ComboBox();
		label1 = new Label();
		lblVersion = new Label();
		((Control)this).SuspendLayout();
		((Control)btnRefresh).Location = new Point(130, 33);
		((Control)btnRefresh).Name = "btnRefresh";
		((Control)btnRefresh).Size = new Size(78, 22);
		((Control)btnRefresh).TabIndex = 15;
		((Control)btnRefresh).Text = "Refresh";
		((ButtonBase)btnRefresh).UseVisualStyleBackColor = true;
		((Control)btnRefresh).Click += btnRefresh_Click;
		((Control)btnSelect).Location = new Point(214, 33);
		((Control)btnSelect).Name = "btnSelect";
		((Control)btnSelect).Size = new Size(78, 22);
		((Control)btnSelect).TabIndex = 14;
		((Control)btnSelect).Text = "Select";
		((ButtonBase)btnSelect).UseVisualStyleBackColor = true;
		((Control)btnSelect).Click += btnSelect_Click;
		cmbProcesses.DropDownStyle = (ComboBoxStyle)2;
		((ListControl)cmbProcesses).FormattingEnabled = true;
		((Control)cmbProcesses).Location = new Point(63, 6);
		((Control)cmbProcesses).Name = "cmbProcesses";
		((Control)cmbProcesses).Size = new Size(229, 21);
		((Control)cmbProcesses).TabIndex = 13;
		((Control)label1).AutoSize = true;
		((Control)label1).Location = new Point(9, 9);
		((Control)label1).Name = "label1";
		((Control)label1).Size = new Size(48, 13);
		((Control)label1).TabIndex = 12;
		((Control)label1).Text = "Process:";
		((Control)lblVersion).AutoSize = true;
		((Control)lblVersion).Location = new Point(9, 38);
		((Control)lblVersion).Name = "lblVersion";
		((Control)lblVersion).Size = new Size(45, 13);
		((Control)lblVersion).TabIndex = 16;
		((Control)lblVersion).Text = "Version:";
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Form)this).ClientSize = new Size(304, 59);
		((Control)this).Controls.Add((Control)(object)lblVersion);
		((Control)this).Controls.Add((Control)(object)btnRefresh);
		((Control)this).Controls.Add((Control)(object)btnSelect);
		((Control)this).Controls.Add((Control)(object)cmbProcesses);
		((Control)this).Controls.Add((Control)(object)label1);
		((Form)this).MaximizeBox = false;
		((Control)this).MaximumSize = new Size(320, 97);
		((Control)this).MinimumSize = new Size(320, 97);
		((Control)this).Name = "frmSelect";
		((Control)this).Text = "Rotation - Select Process...";
		((Form)this).Load += frmSelect_Load;
		((Control)this).ResumeLayout(false);
		((Control)this).PerformLayout();
	}

	public frmSelect()
	{
		InitializeComponent();
	}

	private void UpdateProcesses()
	{
		CurrentProcessList = clsMemory.WoWProcesses();
		cmbProcesses.Items.Clear();
		int num = 0;
		foreach (string currentProcess in CurrentProcessList)
		{
			num++;
			cmbProcesses.Items.Add((object)currentProcess);
		}
		if (num > 0)
		{
			((ListControl)cmbProcesses).SelectedIndex = 0;
		}
		cmbProcesses.Items.Add((object)"Edit Mode (No Bot Functionality)");
		if (num == 0)
		{
			((ListControl)cmbProcesses).SelectedIndex = 0;
		}
	}

	private void frmSelect_Load(object sender, EventArgs e)
	{
		((Form)Program.selectForm).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		OffsetsArray = clsXML.LoadXML_SelectFormOffsets();
		UpdateProcesses();
		int num = 0;
		Process[] processesByName = Process.GetProcessesByName("PriorityQueueRotation");
		Process[] array = processesByName;
		for (int i = 0; i < array.Length; i++)
		{
			_ = array[i];
			num++;
			_ = 30;
		}
		((Control)lblVersion).Text = "Version: " + Application.ProductVersion;
	}

	private void btnRefresh_Click(object sender, EventArgs e)
	{
		UpdateProcesses();
	}

	private void btnSelect_Click(object sender, EventArgs e)
	{
		if (Convert.ToString(cmbProcesses.SelectedItem) != "")
		{
			string text = cmbProcesses.SelectedItem.ToString();
			if (text == "Edit Mode (No Bot Functionality)")
			{
				GlobalSettings.CurrentProcessID = 0;
			}
			else
			{
				text = text.Replace("(", "|");
				text = text.Replace(")", "");
				text = text.Replace(" ", "");
				string[] array = text.Split(new char[1] { '|' });
				GlobalSettings.CurrentProcessID = Convert.ToInt32(array[1]);
				clsXML.LoadXML_Offsets(array[2]);
			}
			Program.mainForm = new frmMain();
			((Control)Program.mainForm).Show();
		}
	}

	static frmSelect()
	{
		CurrentProcessList = new List<string>();
		OffsetsArray = new string[1024, 4];
	}
}
