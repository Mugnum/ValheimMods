using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mugnum.ValheimMods.ProjectilePenetration.Common;
using System;

namespace Mugnum.ValheimMods.ProjectilePenetration;

/// <summary>
/// Projectile penetration mod.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	#region Constants

	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ProjectilePenetration";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Projectile Penetration";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "0.2.0";

	#endregion

	#region Fields

	/// <summary>
	/// Harmony.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Logger.
	/// </summary>
	internal static ManualLogSource Log { get; private set; }

	/// <summary>
	/// Mod toggle.
	/// </summary>
	internal static ConfigEntry<bool> IsModEnabled { get; private set; }

	/// <summary>
	/// Verbose debug logging toggle.
	/// </summary>
	internal static ConfigEntry<bool> IsDebugLogging { get; private set; }

	/// <summary>
	/// Penetration module toggle.
	/// </summary>
	internal static ConfigEntry<bool> IsPenetrationEnabled { get; private set; }

	/// <summary>
	/// Damage retention ratio (multiplier after each hit).
	/// </summary>
	internal static ConfigEntry<float> DamageRetentionRatio { get; private set; }

	/// <summary>
	/// Toggle for earning Bows skill only on first hit.
	/// </summary>
	internal static ConfigEntry<bool> IsSkillGainLimitedToFirstHit { get; private set; }

	/// <summary>
	/// Penetration - Limiting module toggle.
	/// </summary>
	internal static ConfigEntry<bool> IsChargeLimitedPenetrationEnabled { get; private set; }

	/// <summary>
	/// Bow charge level for max penetrations.
	/// </summary>
	internal static ConfigEntry<float> UnlimitedPenetrationStrength { get; private set; }

	/// <summary>
	/// Penetration factor at low charge.
	/// </summary>
	internal static ConfigEntry<float> LowChargePenetrationFactor { get; private set; }

	/// <summary>
	/// Kill rewards modules toggle.
	/// </summary>
	internal static ConfigEntry<bool> IsKillRewardsEnabled { get; private set; }

	/// <summary>
	/// Amount of stamina restored on kill.
	/// </summary>
	internal static ConfigEntry<int> RestoredStaminaOnRangedKill { get; private set; }

	#endregion Fields

	#region Methods: Internal

	/// <summary>
	/// Writes debug log.
	/// </summary>
	/// <param name="message"> Message. </param>
	internal static void LogDebug(string message)
	{
		if (IsDebugLogging.Value)
		{
			Log.LogInfo($"[Debug] [{DateTime.Now.TimeOfDay}] {message}");
		}
	}

	#endregion

	#region Methods: Private

	/// <summary>
	/// Initializes mod.
	/// </summary>
	private void Awake()
	{
		Log = Logger;
		RegisterGeneralConfig();
		RegisterPenetrationConfig();
		RegisterLimitingConfig();
		RegisterKillRewardsConfig();

		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Log.LogInfo($"{PluginName} {PluginVersion} initialized.");
	}

	/// <summary>
	/// Unloads mod.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
	}

	/// <summary>
	/// Creates config description.
	/// </summary>
	/// <param name="order"> Configuration option order in section. </param>
	/// <param name="description"> Text description. </param>
	/// <param name="acceptableValues"> Acceptable values. </param>
	/// <returns> Config description. </returns>
	private static ConfigDescription CreateDescription(int order, string description,
		AcceptableValueBase acceptableValues = null)
	{
		var attributes = new ConfigurationManagerAttributes
		{
			Order = order
		};
		return new ConfigDescription(description, acceptableValues, attributes);
	}

	/// <summary>
	/// Registers config for "General" section.
	/// </summary>
	private void RegisterGeneralConfig()
	{
		const string SectionName = "General";
		var order = 100;
		IsModEnabled = Config.Bind(SectionName, "Enable mod", true,
			CreateDescription(order--, "Enables mod."));

		IsDebugLogging = Config.Bind(SectionName, "Debug logging", false,
			CreateDescription(order, "Enables verbose debug logging."));
	}

	/// <summary>
	/// Registers config for "Penetration" section.
	/// </summary>
	private void RegisterPenetrationConfig()
	{
		const string SectionName = "Penetration - Main";
		var order = 100;
		IsPenetrationEnabled = Config.Bind(SectionName, "Enable", true,
			CreateDescription(order--, "Enables penetrations module. Allows player-fired arrows to penetrate enemies."));

		DamageRetentionRatio = Config.Bind(SectionName, "Damage retention ratio", 0.8f,
			CreateDescription(order--, "Damage multiplier on each penetration. At 1.0 the damage is unchanged.",
				new AcceptableValueRange<float>(0f, 3f)));

		IsSkillGainLimitedToFirstHit = Config.Bind(SectionName, "Only first hit gives skill exp", false,
			CreateDescription(order, "Only the first hit gives skill experience gain."));
	}

	/// <summary>
	/// Registers config for "Penetration - Limiting" section.
	/// </summary>
	private void RegisterLimitingConfig()
	{
		const string SectionName = "Penetration - Limiting";
		var order = 100;
		IsChargeLimitedPenetrationEnabled = Config.Bind(SectionName, "Enable", true,
			CreateDescription(order--, "Enables limiting the number of penetrations based on bow charge level."));

		UnlimitedPenetrationStrength = Config.Bind(SectionName, "Charge level for max penetrations", 0.35f,
			CreateDescription(order--, "Draw charge percentage threshold, past which arrows have unlimited penetrations.",
				new AcceptableValueRange<float>(0f, 1f)));

		LowChargePenetrationFactor = Config.Bind(SectionName, "Low charge penetration factor", 2f,
			CreateDescription(order, "Contributes to how many targets a poorly charged arrow may penetrate.",
				new AcceptableValueRange<float>(0f, 10f)));
	}

	/// <summary>
	/// Registers config for "Kill Rewards" section.
	/// </summary>
	private void RegisterKillRewardsConfig()
	{
		const string SectionName = "Kill Rewards";
		var order = 100;
		IsKillRewardsEnabled = Config.Bind(SectionName, "Enable", true,
			CreateDescription(order--, "Enables ranged kill rewards module."));

		RestoredStaminaOnRangedKill = Config.Bind(SectionName, "Stamina restored on kill", 20,
			CreateDescription(order, "Amount of stamina restored on a ranged kill.",
				new AcceptableValueRange<int>(-50, 250)));
	}

	#endregion
}
