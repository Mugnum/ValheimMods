using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System;

namespace Mugnum.ValheimMods.ScytheBuff;

/// <summary>
/// Scythe Buff plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ScytheBuff";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Scythe Buff";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.3";

	/// <summary>
	/// Whether scythe improvements are enabled.
	/// </summary>
	internal static ConfigEntry<bool> Enabled;

	/// <summary>
	/// Full horizontal angle of the scythe's crop harvest sweep.
	/// </summary>
	internal static ConfigEntry<float> SwingAngle;

	/// <summary>
	/// Percentage increase to forward reach and both skill-scaled harvest radii.
	/// </summary>
	internal static ConfigEntry<float> HarvestDistanceIncrease;

	/// <summary>
	/// Percentage increase to vanilla's Farming bonus yield chance.
	/// </summary>
	internal static ConfigEntry<float> BonusYieldChanceIncrease;

	/// <summary>
	/// Percentage of vanilla's calculated scythe attack stamina cost.
	/// </summary>
	internal static ConfigEntry<int> StaminaUsage;

	/// <summary>
	/// Whether scythe attacks leave stamina regeneration uninterrupted.
	/// </summary>
	internal static ConfigEntry<bool> RemoveStaminaRegenerationDelay;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		Enabled = Config.Bind("General", "Enabled", true, "Enable scythe improvements.");
		SwingAngle = Config.Bind("Harvesting", "Swing angle", 110f,
			new ConfigDescription("Crop harvest angle in degrees, centered on facing direction. 360 disables angle filtering.",
				new AcceptableValueRange<float>(1f, 360f)));
		HarvestDistanceIncrease = Config.Bind("Harvesting", "Harvest distance increase (%)", 30f,
			new ConfigDescription("Multiplies vanilla scythe reach and skill-scaled harvest radius. 30 means 1.3x vanilla.",
				new AcceptableValueRange<float>(0f, 200f)));
		BonusYieldChanceIncrease = Config.Bind("Yield", "Bonus yield chance increase (%)", 20f,
			new ConfigDescription("Multiplies vanilla's skill-scaled bonus yield chance during scythe swings. 20 means 1.2x vanilla.",
				new AcceptableValueRange<float>(0f, 400f)));
		StaminaUsage = Config.Bind("Stamina", "Stamina usage (%)", 100,
			new ConfigDescription("Multiplies vanilla scythe attack stamina cost. 100 keeps vanilla cost; 0 disables consumption and its regeneration delay.",
				new AcceptableValueRange<int>(0, 100)));
		RemoveStaminaRegenerationDelay = Config.Bind("Stamina", "Remove regeneration delay", false,
			"Scythe attacks do not delay stamina regeneration, including during the swing animation. Existing delays from other actions still apply.");

		try
		{
			ScytheStamina.Initialize();
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} initialized.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: Scythe methods don't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
		ScytheRules.SwingHarvester = null;
	}
}
