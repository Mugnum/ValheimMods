using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace Mugnum.ValheimMods.ShieldSledding.BounceLimits;

/// <summary>
/// Patch for "Shield Sledding", limiting extreme velocity and direct position corrections.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("ModdedWolf.ShieldSledding", "1.1.1")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ShieldSledding.BounceLimits";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Shield Sledding Bounce Limits";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Absolute horizontal speed cap.
	/// </summary>
	internal static ConfigEntry<float> MaximumHorizontalSpeed;

	/// <summary>
	/// Absolute upward speed cap.
	/// </summary>
	internal static ConfigEntry<float> MaximumUpwardSpeed;

	/// <summary>
	/// Absolute downward speed cap.
	/// </summary>
	internal static ConfigEntry<float> MaximumFallSpeed;

	/// <summary>
	/// Additional position correction permitted across an entire sled physics tick.
	/// </summary>
	internal static ConfigEntry<float> MaximumCorrection;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		MaximumHorizontalSpeed = Config.Bind("Bounce limits", "Maximum horizontal speed", 60f,
			new ConfigDescription("Hard horizontal speed cap in m/s. Limits all sled speed boosts, including pin escape and landing carry.", new AcceptableValueRange<float>(5f, 100f)));
		MaximumUpwardSpeed = Config.Bind("Bounce limits", "Maximum upward speed", 8f,
			new ConfigDescription("Hard upward velocity cap in m/s, including sled jumps and slope deflections.", new AcceptableValueRange<float>(1f, 20f)));
		MaximumFallSpeed = Config.Bind("Bounce limits", "Maximum fall speed", 60f,
			new ConfigDescription("Hard downward velocity cap in m/s. Does not disable normal fall damage.", new AcceptableValueRange<float>(5f, 100f)));
		MaximumCorrection = Config.Bind("Bounce limits", "Maximum position correction", 0.5f,
			new ConfigDescription("Maximum extra displacement in meters beyond speed-based movement during one physics tick. " +
				"Shared by all ground, water and penetration corrections in that tick, rather than reapplied per substep.", new AcceptableValueRange<float>(0.05f, 2f)));

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			Logger.LogError($"Patch failed to apply: ShieldSledding's physics methods don't match expected layout.\r\n{ex}");
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
