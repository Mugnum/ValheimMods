using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.BonusSkillsUncap.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Patches;

/// <summary>
/// Patch for <see cref="Skills.GetRandomSkillFactor"/> method.
/// </summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillFactor))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SkillsGetRandomSkillFactorPatch
{
	/// <summary>
	/// Adds only the overflow contribution to the game's original damage roll.
	/// </summary>
	/// <param name="__instance"> Player skills. </param>
	/// <param name="skillType"> Skill being evaluated. </param>
	/// <param name="__result"> Random skill damage factor. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
	{
		__result += SkillBonusMath.GetRandomOverflowBonus(__instance.GetSkillFactor(skillType));
	}
}
