using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassCheckStacking.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassCheckStacking.Patches;

/// <summary>
/// Patch for <see cref="Container.GetHoverText"/> method.
/// </summary>
[HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ContainerGetHoverTextPatch
{
	/// <summary>
	/// Shows the extra hold action only while the configured modifier is held.
	/// </summary>
	/// <param name="__instance"> Viewed chest. </param>
	/// <param name="__result"> Hover text updated with the mass stacking hint when applicable. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Container __instance, ref string __result)
	{
		if (!Plugin.IsActive || !Plugin.Enabled.Value || !Plugin.IsModifierHeld()
			|| !ChestStackController.IsSupported(__instance))
		{
			return;
		}

		__result += Localization.instance.Localize(
			$"\n[<color=yellow><b>{Plugin.ModifierKey.Value} + $KEY_Use</b></color>] Hold to stack into nearby chests");
	}
}
