using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.BonusSkillsUncap.Common;
using System.Collections.Generic;
using UnityEngine;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Patches;

/// <summary>
/// Patch for <see cref="Skills.GetSkillLevel"/> method.
/// </summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
internal static class SkillsGetSkillLevelPatch
{
	/// <summary>
	/// Adjusts only the final level returned after vanilla collects skill bonuses.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched instructions. </returns>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		return SkillIlReplacements.ReplaceCalls(instructions,
			AccessTools.Method(typeof(Mathf), nameof(Mathf.Floor), [typeof(float)]),
			AccessTools.Method(typeof(SkillBonusMath), nameof(SkillBonusMath.GetEffectiveLevel)), 1);
	}
}
