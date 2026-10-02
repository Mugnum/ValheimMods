using BepInEx;
using HarmonyLib;
using Mugnum.ValheimMods.NomapPrinter.BiomePatch.Common;
using Mugnum.ValheimMods.NomapPrinter.BiomePatch.Patches;
using System;
using UnityEngine;

namespace Mugnum.ValheimMods.NomapPrinter.BiomePatch;

/// <summary>
/// Patch for "NomapPrinter", displaying the player's biome on the baked map.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MainPluginGuid, MainPluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.NomapPrinterBiomePatch";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "NomapPrinter Biome Patch";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Main mod plugin Id.
	/// </summary>
	private const string MainPluginGuid = "shudnal.NomapPrinter";

	/// <summary>
	/// Main mod plugin version.
	/// </summary>
	private const string MainPluginVersion = "1.5.6";

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		var mapViewerType = AccessTools.TypeByName("NomapPrinter.MapViewer");

		if (mapViewerType == null)
		{
			Logger.LogError("Patch failed to apply: Couldn't find NomapPrinter.MapViewer.");
			return;
		}

		var updateMethod = AccessTools.Method(mapViewerType, "Update", Type.EmptyTypes);
		var parentObjectField = AccessTools.Field(mapViewerType, "parentObject");

		if (updateMethod == null
			|| !updateMethod.IsStatic
			|| updateMethod.ReturnType != typeof(void)
			|| parentObjectField == null
			|| !parentObjectField.IsStatic
			|| parentObjectField.FieldType != typeof(GameObject))
		{
			Logger.LogError("Patch failed to apply: NomapPrinter's types don't match expected layout.");
			return;
		}

		_harmony = new Harmony(PluginGuid);
		_harmony.Patch(updateMethod, postfix: new HarmonyMethod(
			AccessTools.Method(typeof(MapViewerUpdatePatch), nameof(MapViewerUpdatePatch.Postfix))));
		Logger.LogInfo($"{PluginName} loaded.");
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
		BiomeLabelUi.Reset();
	}
}
