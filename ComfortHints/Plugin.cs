using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mugnum.ValheimMods.ComfortHints.Common;
using System.Linq;

namespace Mugnum.ValheimMods.ComfortHints;

/// <summary>
/// Comfort Hints plugin.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.ComfortHints";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Comfort Hints";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Plugin instance.
	/// </summary>
	internal static Plugin Instance;

	/// <summary>
	/// Logger.
	/// </summary>
	internal static ManualLogSource Log;

	/// <summary>
	/// Whether comfort hints require a nearby bed.
	/// </summary>
	internal static ConfigEntry<bool> RequireNearbyBed;

	/// <summary>
	/// Whether diagnostic messages are written at Info level.
	/// </summary>
	internal static ConfigEntry<bool> EnableDiagnostics;

	/// <summary>
	/// Whether the first LateUpdate has been observed.
	/// </summary>
	private bool _lateUpdateObserved;

	/// <summary>
	/// Harmony hook.
	/// </summary>
	private Harmony _harmony;

	/// <summary>
	/// Initializes plugin.
	/// </summary>
	private void Awake()
	{
		Instance = this;
		Log = Logger;
		RequireNearbyBed = Config.Bind("General", "Require nearby bed", true,
			"Only evaluate comfort improvements when a bed is within comfort range.");
		EnableDiagnostics = Config.Bind("Diagnostics", "Enable diagnostics", true,
			"Log patch registration, refresh requests, early exits, candidate gains and marker counts at Info level. Disable after troubleshooting.");

		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(Plugin).Assembly);
		VerifyPatches();
		Diagnostic($"Loaded assembly: {typeof(Plugin).Assembly.Location}; MVID={typeof(Plugin).Module.ModuleVersionId}; requireNearbyBed={RequireNearbyBed.Value}.");
		Logger.LogInfo($"{PluginName} initialized.");
	}

	/// <summary>
	/// Processes a pending refresh after build UI updates.
	/// </summary>
	private void LateUpdate()
	{
		if (!_lateUpdateObserved && EnableDiagnostics.Value)
		{
			_lateUpdateObserved = true;
			Diagnostic($"First LateUpdate reached; enabled={enabled}, active={gameObject.activeInHierarchy}.");
		}

		ComfortHintController.Flush();
	}

	/// <summary>
	/// Writes a diagnostic message when troubleshooting is enabled.
	/// </summary>
	internal static void Diagnostic(string message)
	{
		if (EnableDiagnostics != null && EnableDiagnostics.Value)
		{
			Log.LogInfo($"[Diagnostics] {message}");
		}
	}

	/// <summary>
	/// Verifies that this plugin registered each expected postfix.
	/// </summary>
	private void VerifyPatches()
	{
		var targets = new[]
		{
			AccessTools.Method(typeof(BuildUi), "OpenBuildMenu"),
			AccessTools.Method(typeof(BuildUi), "Close"),
			AccessTools.Method(typeof(BuildUi), "UpdatePieceButtons"),
			AccessTools.Method(typeof(BuildUi), "UpdateSearch"),
			AccessTools.Method(typeof(Piece), nameof(Piece.GetAllComfortPiecesInRadius))
		};

		foreach (var target in targets)
		{
			if (target == null)
			{
				Logger.LogWarning("An expected Harmony target was not found. Check the game assembly version.");
				continue;
			}

			var patches = Harmony.GetPatchInfo(target);
			var postfixCount = patches?.Postfixes.Count(patch => patch.owner == PluginGuid) ?? 0;
			var targetName = $"{target.DeclaringType?.Name}.{target.Name}";

			if (postfixCount == 0)
			{
				Logger.LogWarning($"Expected postfix missing: {targetName}.");
			}
			else
			{
				Diagnostic($"Patch verified: {targetName}; ownPostfixes={postfixCount}, allPostfixes={patches.Postfixes.Count}.");
			}
		}
	}

	/// <summary>
	/// Handles plugin unloading.
	/// </summary>
	private void OnDestroy()
	{
		ComfortHintController.Reset();
		_harmony?.UnpatchSelf();
		Instance = null;
	}
}
