using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.IncreaseMusicRadius.Common;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.IncreaseMusicRadius.Patches;

/// <summary>
/// Patch for MusicVolume.GetOuterBounds method.
/// </summary>
[HarmonyPatch(typeof(MusicVolume), "GetOuterBounds")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MusicVolumeGetOuterBoundsPatch
{
	/// <summary>
	/// Corrects the fade distance for adjusted box-shaped location triggers.
	/// </summary>
	/// <param name="__instance"> Evaluated location music volume. </param>
	/// <param name="__result"> Outer world-space bounds. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(MusicVolume __instance, ref Bounds __result)
	{
		MusicRangeState.AdjustOuterBounds(__instance, ref __result);
	}
}
