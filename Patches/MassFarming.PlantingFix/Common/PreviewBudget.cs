using HarmonyLib;
using System;

namespace Mugnum.ValheimMods.MassFarming.PlantingFix.Common;

/// <summary>
/// Keeps MassFarming's preview budgets limited to positions that can actually be planted.
/// </summary>
internal static class PreviewBudget
{
	/// <summary>
	/// Cached native stamina calculation for the first plant placed by the player.
	/// </summary>
	private static Func<Player, float> GetBuildStamina;

	/// <summary>
	/// Resolves the native build stamina calculation once during initialization.
	/// </summary>
	/// <exception cref="MissingMethodException"> Native build stamina method not found. </exception>
	internal static void Initialize()
	{
		var method = AccessTools.Method(typeof(Player), "GetBuildStamina", Type.EmptyTypes);

		if (method == null || method.IsStatic || method.ReturnType != typeof(float))
		{
			throw new MissingMethodException("Could not find Player.GetBuildStamina.");
		}

		GetBuildStamina = AccessTools.MethodDelegate<Func<Player, float>>(method);
	}

	/// <summary>
	/// Reserves the initial plant's native stamina cost before considering additional grid positions.
	/// </summary>
	/// <param name="player"> Player viewing the planting grid. </param>
	/// <returns> Stamina available to MassFarming's additional plants. </returns>
	internal static float GetAvailableStamina(Player player)
	{
		return player.GetStamina() - GetBuildStamina(player);
	}

	/// <summary>
	/// Uses the same strict stamina comparison as the actual planting loop.
	/// </summary>
	/// <param name="stamina"> Remaining preview stamina. </param>
	/// <param name="cost"> Stamina required for the next plant. </param>
	/// <returns> Is there insufficient stamina for another plant. </returns>
	internal static bool HasInsufficientStamina(float stamina, float cost)
	{
		return !(stamina > cost);
	}

	/// <summary>
	/// Keeps the seed and stamina reservation only when this preview position passed all checks.
	/// </summary>
	/// <param name="stamina"> Stamina left for additional plants. </param>
	/// <param name="cost"> MassFarming's per-plant stamina cost. </param>
	/// <param name="resourcePiece"> Temporary piece containing the cumulative seed requirement. </param>
	/// <param name="requirement"> Seed requirement for one plant. </param>
	/// <param name="invalid"> Was this position rejected by the original preview checks. </param>
	/// <returns> Stamina budget for the next position. </returns>
	internal static float CompletePosition(float stamina, float cost, Piece resourcePiece, Piece.Requirement requirement, bool invalid)
	{
		if (invalid)
		{
			// MassFarming tentatively adds this requirement before checking cultivation and grow space.
			resourcePiece.m_resources[0].m_amount -= requirement.m_amount;
			return stamina;
		}

		return stamina - cost;
	}
}
