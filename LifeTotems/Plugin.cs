using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Mugnum.ValheimMods.LifeTotems.Services;
using Mugnum.ValheimMods.LifeTotems.UI;
using Mugnum.ValheimMods.LifeTotems.World;
using System;

namespace Mugnum.ValheimMods.LifeTotems;

/// <summary> Life Totems: world-bound resurrection reserves and personal offering rituals. </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("com.jotunn.jotunn")]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "Mugnum.LifeTotems";
	public const string PluginName = "Life Totems";
	public const string PluginVersion = "0.3.1";
	internal const string ReliquaryPrefab = "LifeTotems_Reliquary";
	internal static ManualLogSource Log;
	internal static ConfigEntry<bool> KeepEquipment;
	internal static ConfigEntry<bool> GracePeriod;
	internal static bool IsActive;
	private ConfigEntry<bool> _resetSavedRecipes;
	private Harmony _harmony;

	private void Awake()
	{
		Log = Logger;
		KeepEquipment = Config.Bind("Death", "Keep equipment", true,
			"On ordinary deaths retain equipment, food, ammunition and quest items in their original inventory cells. " +
			"Materials, trophies, ordinary cargo and every non-teleportable item go through vanilla tombstone handling. " +
			"Grace deaths always use vanilla inventory handling.");
		GracePeriod = Config.Bind("Death", "Grace period", true,
			"Deaths during No skill drain do not consume another totem, but drop inventory using vanilla rules.");
		_resetSavedRecipes = Config.Bind("Debug", "Reset saved recipes", false,
			"One-shot: back up and regenerate the current character's random recipes for the current single-player world. " +
			"Lives, paid totem costs and recovered offerings are unchanged. Waits until a player is loaded, then returns to false. Save normally afterwards.");
		PrefabManager.OnVanillaPrefabsAvailable += ReliquaryPrefabs.Register;
		try
		{
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Plugin).Assembly);
			IsActive = true;
			Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Single-player prototype; world life limit: 3.");
		}
		catch (Exception ex)
		{
			IsActive = false;
			_harmony?.UnpatchSelf();
			_harmony = null;
			Logger.LogError($"LifeTotems could not initialize its game hooks and has been disabled.\n{ex}");
		}
	}

	private void Update()
	{
		if (IsActive && _resetSavedRecipes.Value && ZNet.IsSinglePlayer && ZNet.instance && Player.m_localPlayer
			&& !Player.m_localPlayer.IsDead() && ObjectDB.instance && ZoneSystem.instance)
		{
			_resetSavedRecipes.Value = false;
			try
			{
				RitualService.ResetSavedRecipes(Player.m_localPlayer);
				ReliquaryWindow.Close();
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Random offering recipes reset. Lives and recovered offerings are unchanged.");
				Logger.LogInfo($"Reset random recipes for world {ZNet.instance.GetWorldUID()}; the previous character entry was backed up.");
			}
			catch (Exception ex) { Logger.LogError($"Recipe reset failed; retry through the debug option after checking the log.\n{ex}"); }
		}
		ReliquaryWindow.Tick();
		PinnedRecipeHud.Tick();
		DeathService.Tick();
		DismantleService.Tick();
		if (!ZNet.instance) LedgerStore.Reset();
	}

	private void OnDestroy()
	{
		IsActive = false;
		PrefabManager.OnVanillaPrefabsAvailable -= ReliquaryPrefabs.Register;
		ReliquaryWindow.Close();
		PinnedRecipe.Clear();
		PinnedRecipeHud.Destroy();
		_harmony?.UnpatchSelf();
		LedgerStore.Reset();
	}
}
