using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Mugnum.ValheimMods.NomapPrinterSavePatch;

/// <summary>
/// Patch for <see cref="NomapPrinterGuid"/> mod, replacing map saving logic.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(NomapPrinterGuid, NomapPrinterVersion)]
public class Plugin : BaseUnityPlugin
{
	#region Constants

	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.NomapPrinterSavePatch";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "NomapPrinter Save Patch";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// Main mod plugin Id.
	/// </summary>
	private const string NomapPrinterGuid = "shudnal.NomapPrinter";

	/// <summary>
	/// Main mod plugin version.
	/// </summary>
	private const string NomapPrinterVersion = "1.5.6";

	#endregion

	#region Fields

	/// <summary>
	/// Logger.
	/// </summary>
	private static ManualLogSource Log;

	/// <summary>
	/// Method info for "NomapPrinter.MapViewer.SaveMapToLocalFile".
	/// </summary>
	private static MethodInfo SaveMapToLocalFileMethod;

	/// <summary>
	/// Field info for "NomapPrinter.MapMaker.isWorking".
	/// </summary>
	private static FieldInfo IsWorkingField;

	/// <summary>
	/// Flag indicating map generation in progress.
	/// </summary>
	private static bool PersistPending;

	/// <summary>
	/// Player executing map generation.
	/// </summary>
	private static Player GenerationPlayer;

	/// <summary>
	/// World UId.
	/// </summary>
	private static long GenerationWorldUid;

	/// <summary>
	/// Thread for saving generated map.
	/// </summary>
	private static Thread SaveThread;

	/// <summary>
	/// Harmony instance.
	/// </summary>
	private Harmony _harmony;

	#endregion

	#region Methods: Private

	/// <summary>
	/// Initializes mod.
	/// </summary>
	private void Awake()
	{
		Log = Logger;
		var mapViewerType = AccessTools.TypeByName("NomapPrinter.MapViewer");
		var mapMakerType = AccessTools.TypeByName("NomapPrinter.MapMaker");

		if (mapViewerType == null || mapMakerType == null)
		{
			Logger.LogError("Patch failed to apply: Couldn't find types to override.");
			return;
		}

		var generateMapMethod = AccessTools.Method(mapMakerType, "GenerateMap");
		var setMapIsReadyMethod = AccessTools.Method(mapViewerType, "SetMapIsReady", [typeof(bool)]);
		SaveMapToLocalFileMethod = AccessTools.Method(mapViewerType, "SaveMapToLocalFile", [typeof(Player)]);
		IsWorkingField = AccessTools.Field(mapMakerType, "isWorking");

		if (generateMapMethod == null
			|| setMapIsReadyMethod == null
			|| SaveMapToLocalFileMethod == null
			|| IsWorkingField == null)
		{
			Logger.LogError("Patch failed to apply: NomapPrinter's types do not match expected layout.");
			return;
		}

		_harmony = new Harmony(PluginGuid);

		if (!RemoveNomapPrinterPlayerSaveHook())
		{
			Logger.LogError("Patch failed to apply: Couldn't remove NomapPrinter's Player.Save hook.");
			return;
		}

		_harmony.Patch(generateMapMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(GenerateMapPostfix))));
		_harmony.Patch(setMapIsReadyMethod, postfix: new HarmonyMethod(AccessTools.Method(typeof(Plugin), nameof(SetMapIsReadyPostfix))));
		Logger.LogInfo("NomapPrinter LocalFolder persistence patched: " +
			"maps will be written immediately after generation instead of on Player.Save.");
	}

	/// <summary>
	/// Removes NomapPrinter's <see cref="Player.Save"/> prefix hook.
	/// </summary>
	/// <returns> Is unhooking successful. </returns>
	private bool RemoveNomapPrinterPlayerSaveHook()
	{
		const string SaveMapDataMethodType = "NomapPrinter.MapViewer+Player_Save_SaveMapData";
		var playerSaveMethod = AccessTools.Method(typeof(Player), nameof(Player.Save));

		if (playerSaveMethod == null)
		{
			Logger.LogError("Patch failed to apply: Couldn't find method Player.Save.");
			return false;
		}

		var patchInfo = Harmony.GetPatchInfo(playerSaveMethod);

		if (patchInfo == null)
		{
			Logger.LogError("Patch failed to apply: Player.Save has no Harmony patch information.");
			return false;
		}

		var ownedPrefixes = patchInfo.Prefixes
			.Where(p => NomapPrinterGuid.Equals(p.owner, StringComparison.OrdinalIgnoreCase))
			.ToArray();

		var savePatch = ownedPrefixes.FirstOrDefault(p =>
			SaveMapDataMethodType.Equals(p.PatchMethod?.DeclaringType?.FullName, StringComparison.OrdinalIgnoreCase)
				&& p.PatchMethod.Name.Equals("Prefix", StringComparison.Ordinal));

		if (savePatch == null)
		{
			Logger.LogError($"Patch failed to apply: Expected 1 Player.Save prefix, but found {ownedPrefixes.Length}.");
			return false;
		}

		_harmony.Unpatch(playerSaveMethod, savePatch.PatchMethod);
		Logger.LogInfo("Removed NomapPrinter's Player.Save prefix: " +
			$"{savePatch.PatchMethod.DeclaringType?.FullName}.{savePatch.PatchMethod.Name}");

		return true;
	}

	/// <summary>
	/// Postfix for "NomapPrinter.MapMaker.GenerateMap" method.
	/// </summary>
	private static void GenerateMapPostfix()
	{
		if (!GetIsMapMakerWorking())
		{
			ClearPendingGeneration();
			return;
		}

		var player = Player.m_localPlayer;
		var znet = ZNet.instance;

		if (!player || !znet)
		{
			ClearPendingGeneration();
			return;
		}

		GenerationPlayer = player;
		GenerationWorldUid = znet.GetWorldUID();
		PersistPending = true;
	}

	/// <summary>
	/// Postfix for "NomapPrinter.MapViewer.SetMapIsReady" method.
	/// </summary>
	/// <param name="__0"> Is map ready flag. </param>
	[SuppressMessage("ReSharper", "InlineTemporaryVariable")]
	private static void SetMapIsReadyPostfix(bool __0)
	{
		var isReady = __0;

		if (!isReady || !PersistPending)
		{
			return;
		}
		if (!GetIsMapMakerWorking())
		{
			ClearPendingGeneration();
			return;
		}

		var player = Player.m_localPlayer;
		var znet = ZNet.instance;

		if (!player
			|| !znet
			|| player != GenerationPlayer
			|| znet.GetWorldUID() != GenerationWorldUid)
		{
			// Generation invocation is declared after comparison check because
			// generation mutates the fields used in this check.
			ClearPendingGeneration();
			return;
		}

		ClearPendingGeneration();

		if (SaveThread?.IsAlive == true)
		{
			Log.LogInfo("Previous NomapPrinter map save is still running. Skipping duplicate save.");
			return;
		}

		SaveThread = new Thread(() =>
		{
			try
			{
				SaveMapToLocalFileMethod.Invoke(null, [player]);
			}
			catch (TargetInvocationException ex)
			{
				var innerEx = ex.InnerException ?? ex;
				Log.LogError($"NomapPrinter map persistence failed:\r\n{innerEx}");
			}
			catch (Exception ex)
			{
				Log.LogError($"NomapPrinter map persistence failed:\r\n{ex}");
			}
		})
		{
			IsBackground = true,
			Name = "NomapPointer LocalFolder Save"
		};

		SaveThread.Start();
	}

	/// <summary>
	/// Checks if map maker is working.
	/// </summary>
	/// <returns> Is map maker working. </returns>
	private static bool GetIsMapMakerWorking()
	{
		return IsWorkingField?.GetValue(null) is true;
	}

	/// <summary>
	/// Clears pending map generation.
	/// </summary>
	private static void ClearPendingGeneration()
	{
		PersistPending = false;
		GenerationPlayer = null;
		GenerationWorldUid = 0L;
	}

	#endregion
}
