using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Patches;

/// <summary>
/// Patch for <see cref="Player.OnDeath"/> method.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerOnDeathPatch
{
	/// <summary>
	/// Captures the buff and timer before any death processing changes them.
	/// </summary>
	/// <param name="__instance"> Dying player. </param>
	/// <param name="__state"> Previous protection context. </param>
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	[HarmonyBefore("muindor.DeathTweaks")]
	[UsedImplicitly]
	private static void Prefix(Player __instance, out Player __state)
	{
		__state = GracePeriod.Enter(__instance);
	}

	/// <summary>
	/// Clears or restores protection even if death processing throws.
	/// </summary>
	/// <param name="__state"> Previous protection context. </param>
	/// <param name="__exception"> Original exception. </param>
	/// <returns> Original exception. </returns>
	[HarmonyFinalizer]
	[HarmonyPriority(Priority.Last)]
	[UsedImplicitly]
	private static Exception Finalizer(Player __state, Exception __exception)
	{
		GracePeriod.Exit(__state);
		return __exception;
	}
}
