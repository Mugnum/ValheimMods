using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.IncreaseMusicRadius.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.IncreaseMusicRadius.Patches;

/// <summary>
/// Patch for MusicVolume.Awake method.
/// </summary>
[HarmonyPatch(typeof(MusicVolume), "Awake")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MusicVolumeAwakePatch
{
	/// <summary>
	/// Tracks the trigger after vanilla initialization without changing its playback rules.
	/// </summary>
	/// <param name="__instance"> Initialized location music volume. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(MusicVolume __instance)
	{
		MusicRangeState.Track(__instance);
	}
}
