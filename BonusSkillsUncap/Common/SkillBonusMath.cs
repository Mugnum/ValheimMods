using UnityEngine;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Common;

/// <summary>
/// Applies reduced bonus skill overflow while preserving calculations below the cap.
/// </summary>
internal static class SkillBonusMath
{
	/// <summary>
	/// Applies vanilla rounding below the cap and scales only excess effective levels.
	/// </summary>
	/// <param name="level"> Base level after the game's status-effect modifiers. </param>
	/// <returns> Effective skill level. </returns>
	internal static float GetEffectiveLevel(float level)
	{
		return level <= Skills.c_MaxSkillLevel
			? Mathf.Floor(level)
			: Skills.c_MaxSkillLevel + (level - Skills.c_MaxSkillLevel) * Plugin.OverflowMultiplier.Value;
	}

	/// <summary>
	/// Keeps vanilla's lower bound without discarding already scaled overflow.
	/// </summary>
	/// <param name="factor"> Effective skill level divided by 100. </param>
	/// <returns> Skill factor including overflow. </returns>
	internal static float GetSkillFactor(float factor)
	{
		return factor > 1f ? factor : Mathf.Clamp01(factor);
	}

	/// <summary>
	/// Extends a skill-based interpolation above maximum skill without negative results.
	/// </summary>
	/// <param name="minimum"> Value at zero skill. </param>
	/// <param name="maximum"> Value at maximum vanilla skill. </param>
	/// <param name="factor"> Effective skill factor. </param>
	/// <returns> Vanilla or extrapolated skill benefit. </returns>
	internal static float Lerp(float minimum, float maximum, float factor)
	{
		return factor > 1f
			? Mathf.Max(0f, Mathf.LerpUnclamped(minimum, maximum, factor))
			: Mathf.Lerp(minimum, maximum, factor);
	}

	/// <summary>
	/// Extends vanilla's random damage range smoothly beyond maximum skill.
	/// </summary>
	/// <param name="factor"> Effective skill factor. </param>
	/// <returns> Additive shift of both vanilla damage range endpoints. </returns>
	internal static float GetRandomOverflowBonus(float factor)
	{
		// Vanilla maps skill factor 0..1 to a damage center of 0.4..1 before clamping its range.
		// Shift the capped range by the excess center, avoiding a jump from 1.0 to 1.15 at the cap.
		return Mathf.Max(0f, factor - 1f) * 0.6f;
	}
}
