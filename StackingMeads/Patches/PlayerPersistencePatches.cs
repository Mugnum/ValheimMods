using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.StackingMeads.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.StackingMeads.Patches;

/// <summary>
/// Updates our character data before vanilla serializes it.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.Save))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerSavePatch
{
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player __instance) => MeadPersistence.Save(__instance);
}

/// <summary>
/// Restores independent records after any legacy LogoutTweaks effect restoration.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.Load))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerLoadPatch
{
	[HarmonyPostfix]
	[HarmonyAfter(Plugin.LogoutTweaksGuid)]
	[UsedImplicitly]
	private static void Postfix(Player __instance) => MeadPersistence.Load(__instance);
}

/// <summary>
/// Clears stale saved stack data after normal death removes status effects.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerOnDeathPatch
{
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Player __instance) => MeadPersistence.Save(__instance);
}
