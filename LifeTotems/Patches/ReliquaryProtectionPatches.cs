using HarmonyLib;
using Mugnum.ValheimMods.LifeTotems.World;
using Mugnum.ValheimMods.LifeTotems.Services;
using System;

namespace Mugnum.ValheimMods.LifeTotems.Patches;

/// <summary> Permit deliberate dismantling once the ledger is available; incidental damage stays protected. </summary>
[HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
internal static class ReliquaryRemovePatch
{
	private static bool Prefix(Piece __instance, ref bool __result)
	{
		var reliquary = __instance.GetComponent<Reliquary>();
		if (!reliquary || reliquary.Id == null || !Plugin.IsActive || !ZNet.IsSinglePlayer || LedgerStore.Ready()) return true;
		__result = false;
		return false;
	}
}

/// <summary> Runs only on actual building destruction, never on scene unloading or visual destruction. </summary>
[HarmonyPatch(typeof(WearNTear), "Destroy")]
internal static class ReliquaryDestroyPatch
{
	private static bool Prefix(WearNTear __instance, out DismantleService.Removal __state)
	{
		__state = null;
		var reliquary = __instance.GetComponent<Reliquary>();
		if (!reliquary || reliquary.Id == null || !Plugin.IsActive || !ZNet.IsSinglePlayer) return true;
		try { __state = DismantleService.Begin(reliquary.Id, reliquary.transform.position); return true; }
		catch (Exception ex)
		{
			Plugin.Log.LogError($"Reliquary dismantling was cancelled to preserve its totems.\n{ex}");
			if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center, ex.Message);
			return false;
		}
	}
	private static void Postfix(DismantleService.Removal __state) => DismantleService.Complete(__state);
	private static Exception Finalizer(Exception __exception, DismantleService.Removal __state)
	{
		if (__exception != null && __state != null)
		{
			try { DismantleService.Cancel(__state); }
			catch (Exception ex) { Plugin.Log.LogError($"Reliquary removal rollback could not be saved.\n{ex}"); }
		}
		return __exception;
	}
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
internal static class ReliquaryDamagePatch
{
	private static bool Prefix(WearNTear __instance, ref bool __result)
	{
		var reliquary = __instance.GetComponent<Reliquary>();
		if (!reliquary || !reliquary.HasCharges) return true;
		__result = false;
		return false;
	}
}
