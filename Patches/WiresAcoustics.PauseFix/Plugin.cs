using BepInEx;
using HarmonyLib;
using System;

namespace Mugnum.ValheimMods.WiresAcoustics.PauseFix;

/// <summary>
/// Patch for "Wire's Acoustics", preventing wind gain from accumulating while paused.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("wire.valheim.acoustics", "0.6.0")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.WiresAcoustics.PauseFix";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Wire's Acoustics Pause Fix";

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
		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: WiresAcoustics' ambience method doesn't match expected layout.\r\n{ex}");
		}
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
