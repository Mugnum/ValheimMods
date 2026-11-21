using BepInEx;
using HarmonyLib;
using Mugnum.ValheimMods.MassFarming.PlantingFix.Common;
using System;

namespace Mugnum.ValheimMods.MassFarming.PlantingFix;

/// <summary>
/// Patch for "MassFarming", excluding invalid planting positions from preview budgets.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("xeio.MassFarming", "1.13")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.MassFarming.PlantingFix";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "MassFarming Planting Fix";

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
			PreviewBudget.Initialize();
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} initialized.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: MassFarming's placement preview doesn't match expected layout.\r\n{ex}");
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
