using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.BonusSkillsUncap.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Patches;

/// <summary>
/// Patch for <see cref="Skills.GetSkillFactor"/> method.
/// </summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
internal static class SkillsGetSkillFactorPatch
{
	/// <summary>
	/// Preserves scaled overflow when vanilla converts a level into a skill factor.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched instructions. </returns>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		return SkillIlReplacements.ReplaceCalls(instructions,
			AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp01), [typeof(float)]),
			AccessTools.Method(typeof(SkillBonusMath), nameof(SkillBonusMath.GetSkillFactor)), 1);
	}
}
