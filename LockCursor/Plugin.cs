using BepInEx;
using HarmonyLib;

namespace Mugnum.ValheimMods.LockCursor;

/// <summary>
/// Lock Cursor plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.LockCursor";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Lock Cursor";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(Plugin).Assembly);
		Logger.LogInfo($"{PluginName} initialized.");
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
	}
}
