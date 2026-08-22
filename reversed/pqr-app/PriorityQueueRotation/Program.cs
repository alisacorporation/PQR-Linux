using System;
using System.Windows.Forms;

namespace PriorityQueueRotation;

internal static class Program
{
	public static frmMain mainForm;

	public static frmSelect selectForm;

	public static frmAbilityEditor abilityForm;

	public static frmRotationEditor rotationForm;

	public static frmHotkeyEditor hotkeyForm;

	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		selectForm = new frmSelect();
		Application.Run((Form)(object)selectForm);
	}
}
