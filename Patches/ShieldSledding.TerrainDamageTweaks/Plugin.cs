using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;
using System;

namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks;

/// <summary>
/// Patch for "Shield Sledding", controlling vegetation damage and collision penalties.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("ModdedWolf.ShieldSledding", "1.1.1")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ShieldSledding.TerrainDamageTweaks";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Shield Sledding Terrain Damage Tweaks";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Vegetation interaction mode.
	/// </summary>
	internal static ConfigEntry<VegetationMode> Mode;

	/// <summary>
	/// Stamina cost for each meter per second of impact speed.
	/// </summary>
	internal static ConfigEntry<float> StaminaPerSpeed;

	/// <summary>
	/// Maximum stamina cost per physics tick.
	/// </summary>
	internal static ConfigEntry<float> MaximumStaminaCost;

	/// <summary>
	/// Percentage of horizontal speed lost on impact.
	/// </summary>
	internal static ConfigEntry<float> SpeedLossPercent;

	/// <summary>
	/// Maximum horizontal speed loss per physics tick.
	/// </summary>
	internal static ConfigEntry<float> MaximumSpeedLoss;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		Mode = Config.Bind("Vegetation", "Mode", VegetationMode.DisableDamage,
			"DisableDamage prevents sled vegetation damage while retaining the mod's collision behavior. " +
			"IgnoreSmallVegetationCollisions also passes through small destructible bushes and saplings. " +
			"OriginalDamage preserves the original destructive behavior. Terrain, buildings, rocks and large trees stay solid.");
		StaminaPerSpeed = Config.Bind("Collision penalties", "Stamina cost per speed", 0f,
			new ConfigDescription("Stamina spent per m/s of collision speed. Zero disables this penalty.", new AcceptableValueRange<float>(0f, 5f)));
		MaximumStaminaCost = Config.Bind("Collision penalties", "Maximum stamina cost", 25f,
			new ConfigDescription("Maximum total stamina spent on collision penalties in one physics tick.", new AcceptableValueRange<float>(0f, 100f)));
		SpeedLossPercent = Config.Bind("Collision penalties", "Speed loss percent", 0f,
			new ConfigDescription("Percentage of horizontal speed lost on impact. Zero disables this penalty.", new AcceptableValueRange<float>(0f, 100f)));
		MaximumSpeedLoss = Config.Bind("Collision penalties", "Maximum speed loss", 10f,
			new ConfigDescription("Maximum total horizontal speed lost in one physics tick, in m/s.", new AcceptableValueRange<float>(0f, 30f)));
		Mode.SettingChanged += OnModeChanged;

		try
		{
			SledContext.Initialize();
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			SledContext.Reset();
			Logger.LogError($"Patch failed to apply: ShieldSledding's collision methods don't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Restores collision pairs when the vegetation policy changes.
	/// </summary>
	private static void OnModeChanged(object sender, EventArgs args)
	{
		SledContext.Reset();
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		Mode.SettingChanged -= OnModeChanged;
		SledContext.Reset();
		_harmony?.UnpatchSelf();
		_harmony = null;
	}
}

/// <summary>
/// Sled interactions with bushes and small trees.
/// </summary>
public enum VegetationMode
{
	/// <summary>
	/// Preserve vegetation without changing collision handling.
	/// </summary>
	DisableDamage,

	/// <summary>
	/// Preserve vegetation and pass through eligible small plants.
	/// </summary>
	IgnoreSmallVegetationCollisions,

	/// <summary>
	/// Retain Shield Sledding's original damage.
	/// </summary>
	OriginalDamage
}
