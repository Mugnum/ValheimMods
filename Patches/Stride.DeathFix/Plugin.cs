using BepInEx;
using HarmonyLib;
using Mugnum.ValheimMods.Stride.DeathFix.Common;
using System;

namespace Mugnum.ValheimMods.Stride.DeathFix;

/// <summary>
/// Patch for "Stride", restoring stamina bars and clearing temporary state after death.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("com.b0n3.stride", "0.7.1")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.Stride.DeathFix";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Stride Death Fix";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		try
		{
			StrideCleanup.Initialize();
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: Stride's lifecycle methods don't match expected layout.\r\n{ex}");
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
