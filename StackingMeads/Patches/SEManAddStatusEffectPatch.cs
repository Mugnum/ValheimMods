using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.StackingMeads.Common;
using Mugnum.ValheimMods.StackingMeads.Meads;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.StackingMeads.Patches;

/// <summary>
/// Replaces the supported drink's vanilla timer reset with an independent stack.
/// </summary>
[HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect),
	[typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short)])]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SEManAddStatusEffectPatch
{
	/// <summary>
	/// Leaves other effects, remote characters and non-consumption restores alone.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(SEMan __instance, StatusEffect statusEffect, bool resetTime, ref StatusEffect __result)
	{
		var definition = MeadRegistry.Find(statusEffect);
		if (!resetTime || definition == null || !PlayerConsumeItemPatch.IsDrinking(__instance, definition))
		{
			return true;
		}

		__result = MeadController.AddDrink(statusEffect, definition);
		return false;
	}
}
