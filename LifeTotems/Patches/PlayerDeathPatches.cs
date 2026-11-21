using HarmonyLib;
using Mugnum.ValheimMods.LifeTotems.Services;
using System;

namespace Mugnum.ValheimMods.LifeTotems.Patches;

/// <summary> Preserves one death operation's inventory context through all normal and exceptional exits. </summary>
[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
internal static class PlayerDeathPatch
{
	[HarmonyPrefix, HarmonyPriority(Priority.First)]
	private static void Prefix(Player __instance, out DeathService.Context __state) => __state = DeathService.Begin(__instance);

	[HarmonyFinalizer]
	private static Exception Finalizer(Exception __exception, DeathService.Context __state)
	{
		try { DeathService.End(__state); }
		catch (Exception ex) { Plugin.Log.LogError($"LifeTotems inventory restoration failed.\n{ex}"); }
		return __exception;
	}
}

