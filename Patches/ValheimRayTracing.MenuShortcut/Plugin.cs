using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

namespace Mugnum.ValheimMods.ValheimRayTracing.MenuShortcut;

/// <summary>
/// Patch for "Valheim Ray Tracing", replacing the hardcoded menu shortcut.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MainPluginGuid, MainPluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ValheimRayTracing.MenuShortcut";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Valheim Ray Tracing Menu Shortcut";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Main mod plugin Id.
	/// </summary>
	private const string MainPluginGuid = "org.kewb.valheim.raytracing";

	/// <summary>
	/// Main mod plugin version.
	/// </summary>
	private const string MainPluginVersion = "1.0.3";

	/// <summary>
	/// Shortcut for opening and closing the ray tracing menu.
	/// </summary>
	internal static ConfigEntry<KeyboardShortcut> MenuShortcut;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		MenuShortcut = Config.Bind("General", "Menu shortcut", new KeyboardShortcut(KeyCode.F2),
			"Keyboard shortcut for opening and closing the Valheim Ray Tracing menu. " +
			"Supports modifier keys, for example F2 + LeftControl. Set to None to disable the shortcut.");

		_harmony = new Harmony(PluginGuid);

		try
		{
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded. Menu shortcut: {MenuShortcut.Value}.");
		}
		catch (Exception ex)
		{
			_harmony.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: ValheimRayTracing's methods don't match expected layout.\r\n{ex}");
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
