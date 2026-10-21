using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.RemapShortcuts;

/// <summary>
/// Patch for "Shield Sledding", replacing the hardcoded dismount shortcut.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("ModdedWolf.ShieldSledding", "1.1.1")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ShieldSledding.RemapShortcuts";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Shield Sledding Remap Shortcuts";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Shortcut for canceling an armed or active sled.
	/// </summary>
	internal static ConfigEntry<KeyboardShortcut> CancelShortcut;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		CancelShortcut = Config.Bind("Controls", "Cancel sledding", new KeyboardShortcut(KeyCode.Escape),
			"Shortcut for canceling sledding or disarming a sled. Use Clear in Configuration Manager, " +
			"or set None, to disable this shortcut. The original activation shortcut remains unchanged.");

		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			Logger.LogError($"Patch failed to apply: ShieldSledding's input method doesn't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Checks the shortcut without querying input for an unset key.
	/// </summary>
	/// <returns> Was the configured shortcut pressed this frame. </returns>
	internal static bool WasCancelPressed()
	{
		var shortcut = CancelShortcut?.Value ?? KeyboardShortcut.Empty;
		return shortcut.MainKey != KeyCode.None && shortcut.IsDown();
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
