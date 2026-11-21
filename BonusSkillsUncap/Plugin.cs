using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace Mugnum.ValheimMods.BonusSkillsUncap;

/// <summary>
/// Bonus Skills Uncap plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.BonusSkillsUncap";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Bonus Skills Uncap";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Strength of effective skill levels above the vanilla maximum.
	/// </summary>
	internal static ConfigEntry<float> OverflowMultiplier;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		OverflowMultiplier = Config.Bind("General", "Overflow multiplier", 0.5f,
			new ConfigDescription("Multiplier for skill levels above 100 (obtained via buffs). " +
				"Effective level = 100 + (total level - 100) * multiplier. " +
				"Base skill progression remains capped at 100.",
				new AcceptableValueRange<float>(0f, 1f)));

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} initialized. Overflow multiplier: {OverflowMultiplier.Value}.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: Skill calculations don't match expected layout.\r\n{ex}");
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
