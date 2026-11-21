using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.BonusSkillsUncap.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Patches;

/// <summary>
/// Patch for <see cref="Skills.GetRandomSkillRange"/> method.
/// </summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillRange))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SkillsGetRandomSkillRangePatch
{
	/// <summary>
	/// Keeps weapon damage tooltips consistent with the extended damage rolls.
	/// </summary>
	/// <param name="__instance"> Player skills. </param>
	/// <param name="skillType"> Skill being evaluated. </param>
	/// <param name="min"> Minimum damage factor. </param>
	/// <param name="max"> Maximum damage factor. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float min, ref float max)
	{
		var bonus = SkillBonusMath.GetRandomOverflowBonus(__instance.GetSkillFactor(skillType));
		min += bonus;
		max += bonus;
	}
}
