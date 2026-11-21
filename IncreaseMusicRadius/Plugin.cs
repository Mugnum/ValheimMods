using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mugnum.ValheimMods.IncreaseMusicRadius.Common;
using System;

namespace Mugnum.ValheimMods.IncreaseMusicRadius;

/// <summary>
/// Increase Music Radius plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.IncreaseMusicRadius";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Increase Music Radius";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Whether location music ranges are adjusted.
	/// </summary>
	internal static ConfigEntry<bool> Enabled;

	/// <summary>
	/// Additional full-volume range in meters.
	/// </summary>
	internal static ConfigEntry<float> ExtraRadius;

	/// <summary>
	/// Minimum fade band outside a location music volume.
	/// </summary>
	internal static ConfigEntry<float> MinimumFadeDistance;

	/// <summary>
	/// Whether per-trigger diagnostics are enabled.
	/// </summary>
	internal static ConfigEntry<bool> VerboseLogging;

	/// <summary>
	/// Whether patches were applied successfully.
	/// </summary>
	internal static bool IsActive;

	/// <summary>
	/// Logger.
	/// </summary>
	private static ManualLogSource Log;

	/// <summary>
	/// Whether configuration changes need to be applied on the main thread.
	/// </summary>
	private volatile bool _refreshPending;

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
		Enabled = Config.Bind("General", "Enabled", true,
			"Expand outdoor location music triggers. Biome music and dungeon-room triggers are unchanged.");
		ExtraRadius = Config.Bind("General", "Extra radius", 30f,
			new ConfigDescription("Additional full-volume range in meters. Added to spherical radii and to each side of box triggers.",
				new AcceptableValueRange<float>(0f, 200f)));
		MinimumFadeDistance = Config.Bind("General", "Minimum fade distance", 15f,
			new ConfigDescription("Minimum distance in meters between the inner and outer boundaries. " +
				"Proximity music fades through this band. Existing wider bands are preserved; 0 disables widening.",
				new AcceptableValueRange<float>(0f, 200f)));
		VerboseLogging = Config.Bind("Logging", "Verbose logging", false,
			"Log each location music trigger, its original and adjusted range, and skipped dungeon-room triggers.");
		Enabled.SettingChanged += OnSettingsChanged;
		ExtraRadius.SettingChanged += OnSettingsChanged;
		MinimumFadeDistance.SettingChanged += OnSettingsChanged;
		VerboseLogging.SettingChanged += OnSettingsChanged;

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			IsActive = true;
			MusicRangeState.TrackExisting();
			Logger.LogInfo($"{PluginName} initialized. Extra radius: {ExtraRadius.Value}m; minimum fade distance: {MinimumFadeDistance.Value}m.");
		}
		catch (Exception ex)
		{
			IsActive = false;
			_harmony?.UnpatchSelf();
			_harmony = null;
			MusicRangeState.RestoreAll();
			Logger.LogError($"Patch failed to apply: Location music methods don't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Schedules a refresh without touching Unity objects from a configuration callback.
	/// </summary>
	private void OnSettingsChanged(object sender, EventArgs args)
	{
		_refreshPending = true;
	}

	/// <summary>
	/// Applies pending configuration changes without scanning the scene every frame.
	/// </summary>
	private void LateUpdate()
	{
		if (!_refreshPending || !IsActive)
		{
			return;
		}

		_refreshPending = false;
		MusicRangeState.RefreshAll();
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
	/// Restores trigger geometry and removes patches on plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		IsActive = false;
		Enabled.SettingChanged -= OnSettingsChanged;
		ExtraRadius.SettingChanged -= OnSettingsChanged;
		MinimumFadeDistance.SettingChanged -= OnSettingsChanged;
		VerboseLogging.SettingChanged -= OnSettingsChanged;
		_harmony?.UnpatchSelf();
		_harmony = null;
		MusicRangeState.RestoreAll();
	}
}
