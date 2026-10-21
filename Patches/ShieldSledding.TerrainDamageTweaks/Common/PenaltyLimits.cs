using System;

namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;

/// <summary>
/// Finite, nonnegative collision penalty arithmetic shared by stamina and speed loss.
/// </summary>
internal static class PenaltyLimits
{
	/// <summary>
	/// Accumulates speed-scaled cost without overflowing or exceeding the configured cap.
	/// </summary>
	internal static float Accumulate(float current, float speed, float rate, float maximum)
	{
		if (!IsFinite(current) || !IsFinite(speed) || !IsFinite(rate) || !IsFinite(maximum))
		{
			return 0f;
		}
		var cost = Math.Max(0d, current) + Math.Min(100d, Math.Max(0d, speed)) * Math.Max(0d, rate);
		return (float)Math.Min(Math.Max(0d, maximum), cost);
	}

	/// <summary>
	/// Rejects invalid input before it reaches inventory or movement state.
	/// </summary>
	private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
