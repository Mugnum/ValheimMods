using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassTorchRefuel.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassTorchRefuel.Patches;

/// <summary>
/// Patch for <see cref="Fireplace.Interact"/> method.
/// </summary>
[HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class FireplaceInteractPatch
{
	/// <summary>
	/// Handles a modified interaction before vanilla adds a single fuel item.
	/// </summary>
	/// <param name="__instance"> Interacted fireplace. </param>
	/// <param name="user"> Interacting player. </param>
	/// <param name="hold"> Is this a held interaction repeat. </param>
	/// <param name="__result"> Interaction result, set to whether the batch consumed fuel; false for held repeats. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Fireplace __instance, Humanoid user, bool hold, ref bool __result)
	{
		if (!Plugin.Enabled.Value
			|| user != Player.m_localPlayer
			|| !Player.m_localPlayer
			|| !Plugin.IsModifierHeld()
			|| !TorchRefuelHandler.IsEligible(__instance))
		{
			return true;
		}

		// Only the initial key press starts a batch; held repeats must not add more fuel.
		__result = !hold && TorchRefuelHandler.RefuelNearby(__instance, Player.m_localPlayer);
		return false;
	}
}
