using BepInEx;
using HarmonyLib;
using Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;
using System;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch;

/// <summary>
/// Patch for "Death Tweaks", protecting items and skills during the death grace period.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("muindor.DeathTweaks", "1.1.0")]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.DeathTweaks.GracePeriodPatch";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "DeathTweaks Grace Period Patch";

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
			GracePeriod.Initialize();
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			Logger.LogInfo($"{PluginName} loaded.");
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"Patch failed to apply: DeathTweaks or Valheim methods don't match expected layout.\r\n{ex}");
		}
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		_harmony = null;
		GracePeriod.Reset();
	}
}
