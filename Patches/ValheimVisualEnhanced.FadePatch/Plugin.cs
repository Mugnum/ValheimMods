using BepInEx;
using HarmonyLib;

namespace Mugnum.ValheimMods.ValheimVisualEnhanced.FadePatch;

/// <summary>
/// Patch for "Valheim Visual Enhanced" with fixed morning/evening lighting fade.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MainPluginGuid)]
public sealed class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ValheimVisualEnhanced.FadePatch";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Valheim Visual Enhanced Fade Patch";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Main mod plugin Id.
	/// </summary>
	private const string MainPluginGuid = "dev.local.valheimvisualenhanced";

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll();
		Logger.LogInfo($"{PluginName} loaded.");
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
