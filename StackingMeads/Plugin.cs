using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mugnum.ValheimMods.StackingMeads.Common;
using System;

namespace Mugnum.ValheimMods.StackingMeads;

/// <summary>
/// Stacking Meads plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(LogoutTweaksGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.StackingMeads";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Stacking Meads";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Optional logout persistence plugin Id.
	/// </summary>
	internal const string LogoutTweaksGuid = "com.orianaventure.mod.LogoutTweaks";

	/// <summary>
	/// Maximum active stacks in one vanilla cooldown category.
	/// </summary>
	internal static ConfigEntry<int> MaxStacks;

	/// <summary>
	/// Duration factor for each already active stack in the category.
	/// </summary>
	internal static ConfigEntry<float> DurationMultiplier;

	/// <summary>
	/// Bonus strength factor for each already active stack in the category.
	/// </summary>
	internal static ConfigEntry<float> EffectsMultiplier;

	/// <summary>
	/// Whether independent stack data is saved with the character.
	/// </summary>
	internal static ConfigEntry<bool> SaveOnExit;

	/// <summary>
	/// Whether technical diagnostics are enabled.
	/// </summary>
	internal static ConfigEntry<bool> VerboseLogging;

	/// <summary>
	/// Logger.
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
		MaxStacks = Config.Bind("Stacking", "Max stacks", 3,
			new ConfigDescription("Maximum simultaneous drinks per vanilla cooldown category, among supported meads.",
				new AcceptableValueRange<int>(1, 50)));
		DurationMultiplier = Config.Bind("Stacking", "Duration multiplier", 1f,
			new ConfigDescription("New duration = base duration * multiplier ^ currently active stacks in the category. Existing timers do not change.",
				new AcceptableValueRange<float>(0.01f, 1f)));
		EffectsMultiplier = Config.Bind("Stacking", "Effects multiplier", 1f,
			new ConfigDescription("New bonus strength = multiplier ^ currently active stacks in the category. Bonuses add before applying to the baseline.",
				new AcceptableValueRange<float>(0.01f, 1f)));
		SaveOnExit = Config.Bind("Persistence", "Save on exit", true,
			"Save individual remaining durations and strengths in the character file. Timers pause while logged out. Independent of LogoutTweaks.");
		VerboseLogging = Config.Bind("Logging", "Verbose logging", false,
			"Log stack durations, strengths, persistence and taming rate calculations.");

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} initialized. Supported mead: Brew of animal whispers.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: Mead or taming methods don't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Writes technical details when requested.
	/// </summary>
	internal static void Diagnostic(string message)
	{
		if (VerboseLogging?.Value == true)
		{
			Log.LogInfo(message);
		}
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
		MeadController.DetachLocalPlayer();
	}
}
