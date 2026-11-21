using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.IncreaseMusicRadius.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.IncreaseMusicRadius.Patches;

/// <summary>
/// Patch for MusicLocation.Awake method.
/// </summary>
[HarmonyPatch(typeof(MusicLocation), "Awake")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MusicLocationAwakePatch
{
	/// <summary>
	/// Tracks legacy location audio triggers after vanilla initialization.
	/// </summary>
	/// <param name="__instance"> Initialized location music trigger. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(MusicLocation __instance)
	{
		MusicRangeState.Track(__instance);
	}
}
