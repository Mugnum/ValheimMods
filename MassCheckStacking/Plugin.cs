using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mugnum.ValheimMods.MassCheckStacking.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mugnum.ValheimMods.MassCheckStacking;

/// <summary>
/// Mass Chest Stacking plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.MassCheckStacking";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Mass Chest Stacking";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Logger.
	/// </summary>
	internal static ManualLogSource Log;

	/// <summary>
	/// Whether the plugin can start new batches.
	/// </summary>
	internal static bool IsActive;

	/// <summary>
	/// Whether mass stacking is enabled.
	/// </summary>
	internal static ConfigEntry<bool> Enabled;

	/// <summary>
	/// Radius measured from the selected chest.
	/// </summary>
	internal static ConfigEntry<float> StackRadius;

	/// <summary>
	/// Modifier held while using vanilla's hold-to-stack interaction.
	/// </summary>
	internal static ConfigEntry<KeyCode> ModifierKey;

	/// <summary>
	/// Comma-separated item name fragments excluded from mass stacking.
	/// </summary>
	internal static ConfigEntry<string> ExcludedItemNames;

	/// <summary>
	/// Stacking permissions for optional item categories.
	/// </summary>
	internal static readonly Dictionary<ItemCategory, ConfigEntry<bool>> CategorySettings = [];

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
		Enabled = Config.Bind("General", "Enabled", true, "Enable mass chest stacking.");
		StackRadius = Config.Bind("General", "Stack radius", 10f,
			new ConfigDescription("Search radius in meters, measured from the selected chest.",
				new AcceptableValueRange<float>(1f, 100f)));
		ModifierKey = Config.Bind("General", "Modifier key", KeyCode.LeftAlt,
			"Hold this key while briefly holding interact on a chest. Only this exact key is accepted. " +
			"None disables mass stacking and its hover hint.");
		ExcludedItemNames = Config.Bind("General", "Excluded item names", string.Empty,
			"Comma-separated name fragments to exclude. Matches localized names, internal names and prefab names, " +
			"ignoring case. Spaces around entries and empty entries are ignored; spaces inside names are preserved.");

		BindCategory(ItemCategory.Arrows, "Arrows", false, "Include unequipped arrows and bolts.");
		BindCategory(ItemCategory.Meads, "Meads", false, "Include meads, potion-like consumables and mead bases.");
		BindCategory(ItemCategory.CookedFood, "Cooked food", false, "Include prepared food made by crafting or cooking, including unbaked food.");
		BindCategory(ItemCategory.RawFood, "Raw food", true, "Include raw food and food ingredients, including berries, mushrooms, meat and fish.");
		BindCategory(ItemCategory.UnprocessedOre, "Unprocessed ore", false, "Include unprocessed ores and metal scraps used in smelting.");
		StackItemFilter.SetExclusions(ExcludedItemNames.Value);
		ExcludedItemNames.SettingChanged += OnExclusionsChanged;

		_harmony = new Harmony(PluginGuid);

		try
		{
			_harmony.PatchAll(typeof(Plugin).Assembly);
			IsActive = true;
			Logger.LogInfo($"{PluginName} initialized.");
		}
		catch (Exception ex)
		{
			_harmony.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply.\r\n{ex}");
		}
	}

	/// <summary>
	/// Registers a category without coupling category switches to chest transfer logic.
	/// </summary>
	/// <param name="category"> Item category controlled by the setting. </param>
	/// <param name="categoryName"> Configuration setting name. </param>
	/// <param name="defaultValue"> Default stacking permission. </param>
	/// <param name="description"> Configuration setting description. </param>
	private void BindCategory(ItemCategory category, string categoryName, bool defaultValue, string description)
	{
		CategorySettings[category] = Config.Bind("Item categories", categoryName, defaultValue, description);
	}

	/// <summary>
	/// Checks only the configured modifier key.
	/// </summary>
	/// <returns> Is the configured modifier set and held. </returns>
	internal static bool IsModifierHeld()
	{
		var key = ModifierKey?.Value ?? KeyCode.None;
		return key != KeyCode.None && ZInput.GetKey(key);
	}

	/// <summary>
	/// Advances at most one chest request after the preceding response finishes.
	/// </summary>
	private void LateUpdate()
	{
		ChestStackController.Update();
	}

	/// <summary>
	/// Rebuilds exclusions only when the setting changes.
	/// </summary>
	/// <param name="sender"> Configuration entry that raised the event. </param>
	/// <param name="args"> Configuration change event arguments. </param>
	private static void OnExclusionsChanged(object sender, EventArgs args)
	{
		StackItemFilter.SetExclusions(ExcludedItemNames.Value);
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		IsActive = false;
		if (ExcludedItemNames != null)
		{
			ExcludedItemNames.SettingChanged -= OnExclusionsChanged;
		}

		ChestStackController.Cancel();
		StackItemFilter.EndTransfer();

		// Keep the response guard until outstanding vanilla RPCs arrive or their chests are destroyed.
		// Removing it early would let a delayed reply perform an unfiltered vanilla stack.
		if (!ChestStackController.HasOutstandingRequests)
		{
			_harmony?.UnpatchSelf();
		}
	}
}
