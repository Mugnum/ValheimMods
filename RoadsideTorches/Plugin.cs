using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Mugnum.ValheimMods.RoadsideTorches;

/// <summary>
/// Roadside Torches plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.RoadsideTorches";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Roadside Torches";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Logger;
	/// </summary>
	internal static ManualLogSource Log;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		Log = Logger;
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo($"{PluginName} initialized.");
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}
}
