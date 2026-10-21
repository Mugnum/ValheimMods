using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassTorchRefuel.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassTorchRefuel.Patches;

/// <summary>
/// Patch for <see cref="Fireplace.GetHoverText"/> method.
/// </summary>
[HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class FireplaceGetHoverTextPatch
{
	/// <summary>
	/// Shows the mass refueling hint only while the configured modifier is held.
	/// </summary>
	/// <param name="__instance"> Viewed fireplace. </param>
	/// <param name="__result"> Hover text updated with the mass refueling hint when applicable. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Fireplace __instance, ref string __result)
	{
		if (!Plugin.Enabled.Value
			|| !Plugin.IsModifierHeld()
			|| !TorchRefuelHandler.IsEligible(__instance))
		{
			return;
		}

		var modifier = Plugin.ModifierKey.Value.ToString();
		var group = TorchRefuelHandler.IsCampfire(__instance) ? "fires" : "torches";
		__result += Localization.instance.Localize(
			$"\n[<color=yellow><b>{modifier} + $KEY_Use</b></color>] Refuel nearby {group}");
	}
}
