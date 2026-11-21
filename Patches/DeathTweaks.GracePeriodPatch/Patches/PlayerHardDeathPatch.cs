using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Patches;

/// <summary>
/// Patch for Player.HardDeath method.
/// </summary>
[HarmonyPatch(typeof(Player), "HardDeath")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerHardDeathPatch
{
	/// <summary>
	/// Restores vanilla grace protection even when DeathTweaks disables it.
	/// This also lets vanilla display the No skill drain buff during that window.
	/// </summary>
	/// <param name="__instance"> Player being checked. </param>
	/// <param name="__result"> Is this a hard death. </param>
	[HarmonyPostfix]
	[HarmonyAfter("muindor.DeathTweaks")]
	[UsedImplicitly]
	private static void Postfix(Player __instance, ref bool __result)
	{
		if (__result && GracePeriod.IsActive(__instance))
		{
			__result = false;
		}
	}
}
