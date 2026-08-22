namespace PriorityQueueRotation;

internal class clsOffsets
{
	public enum WoWClass : uint
	{
		None = 0u,
		Warrior = 1u,
		Paladin = 2u,
		Hunter = 3u,
		Rogue = 4u,
		Priest = 5u,
		DeathKnight = 6u,
		Shaman = 7u,
		Mage = 8u,
		Warlock = 9u,
		Druid = 11u
	}

	public static uint PlayerName = 0u;

	public static uint PlayerClass = 0u;

	public static uint GameState = 0u;

	public static uint wowVersion = 0u;

	public static uint CurrentWoWVersion = 0u;

	public static uint KeyboardFocus = 0u;

	public static uint Detour = 0u;

	public static byte[] OverWritten = new byte[9] { 85, 139, 236, 129, 236, 148, 0, 0, 0 };

	public static byte[] OverWrittenPattern = new byte[16]
	{
		85, 139, 236, 129, 236, 148, 0, 0, 0, 131,
		125, 20, 0, 139, 69, 8
	};

	public static uint ClntObjMgrGetActivePlayerObjAddress = 0u;

	public static byte[] ClntObjMgrSearch = new byte[8] { 232, 0, 0, 0, 0, 104, 0, 0 };

	public static string ClntObjMgrMask = "x???xx?x";

	public static uint Lua_DoStringAddress = 0u;

	public static uint Lua_GetLocalizedTextAddress = 0u;

	public static uint ObjectField_GUID = 0u;
}
