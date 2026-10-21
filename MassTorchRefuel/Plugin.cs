using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mugnum.ValheimMods.MassTorchRefuel.Common;
using UnityEngine;

namespace Mugnum.ValheimMods.MassTorchRefuel;

/// <summary>
/// Mass Torch Refuel plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.MassTorchRefuel";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Mass Torch Refuel";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Whether mass refueling is enabled.
	/// </summary>
	internal static ConfigEntry<bool> Enabled;

	/// <summary>
	/// Radius measured from the interacted torch.
	/// </summary>
	internal static ConfigEntry<float> RefuelRadius;

	/// <summary>
	/// Modifier held while interacting.
	/// </summary>
	internal static ConfigEntry<KeyCode> ModifierKey;

	/// <summary>
	/// Whether mass refueling can be started on campfires, hearths and braziers.
	/// </summary>
	internal static ConfigEntry<bool> IncludeCampfires;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		Enabled = Config.Bind("General", "Enabled", true, "Enable mass torch refueling.");
		RefuelRadius = Config.Bind("General", "Refuel radius", 60f,
			new ConfigDescription("Refuel player-built torches within this radius in meters, measured from the interacted torch.",
				new AcceptableValueRange<float>(1f, 200f)));
		ModifierKey = Config.Bind("General", "Modifier key", KeyCode.LeftAlt,
			"Hold this key while pressing interact to refuel nearby torches. " +
			"Only the configured key is accepted. None disables mass refueling.");
		IncludeCampfires = Config.Bind("General", "IncludeCampfires", false,
			"Enable a separate mass refueling action for campfires, hearths and braziers. " +
			"Torch interactions only refuel torches; campfire interactions only refuel campfires and similar fires.");

		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(Plugin).Assembly);
		Logger.LogInfo($"{PluginName} initialized.");
	}

	/// <summary>
	/// Checks whether the mass refueling modifier is held.
	/// </summary>
	/// <returns> Is the modifier held. </returns>
	internal static bool IsModifierHeld()
	{
		var key = ModifierKey?.Value ?? KeyCode.None;
		return key != KeyCode.None && ZInput.GetKey(key);
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
		TorchRefuelHandler.Reset();
	}
}
