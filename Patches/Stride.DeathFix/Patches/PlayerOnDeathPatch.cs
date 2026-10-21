using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.Stride.DeathFix.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.Stride.DeathFix.Patches;

/// <summary>
/// Patch for <see cref="Player.OnDeath"/> method.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerOnDeathPatch
{
	/// <summary>
	/// Ends stamina debt and restores the HUD once the local player has actually died.
	/// </summary>
	/// <param name="__instance"> Dying player. </param>
	/// <param name="___m_stamina"> Current stamina, including Stride debt. </param>
	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	[UsedImplicitly]
	private static void Postfix(Player __instance, ref float ___m_stamina)
	{
		if (!__instance || __instance != Player.m_localPlayer || !__instance.IsDead())
		{
			return;
		}

		// A dead player can still receive HUD updates before respawn. Ending debt prevents
		// Stride from disabling the restored bars again during that interval.
		if (___m_stamina < 0f)
		{
			___m_stamina = 0f;
		}

		StrideCleanup.ResetAfterDeath();
	}
}
