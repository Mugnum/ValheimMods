using UnityEngine;

namespace Mugnum.ValheimMods.ComfortHints.Common;

/// <summary>
/// Provides hypothetical vanilla comfort calculations.
/// </summary>
internal static class ComfortProbe
{
	/// <summary>
	/// Piece temporarily injected into vanilla comfort lookup.
	/// </summary>
	internal static Piece Candidate { get; private set; }

	/// <summary>
	/// Number of lookups that confirmed inclusion of a hypothetical candidate.
	/// </summary>
	internal static int CandidateInclusionCount { get; private set; }

	/// <summary>
	/// Records that the lookup patch included the current candidate.
	/// </summary>
	internal static void ConfirmCandidateIncluded()
	{
		CandidateInclusionCount++;
	}

	/// <summary>
	/// Calculates vanilla comfort with an optional hypothetical piece.
	/// </summary>
	/// <param name="inShelter"> Whether player is sheltered. </param>
	/// <param name="position"> Position used for comfort calculation. </param>
	/// <param name="candidate"> Hypothetical comfort piece. </param>
	/// <returns> Calculated comfort level. </returns>
	internal static int Calculate(bool inShelter, Vector3 position, Piece candidate)
	{
		var previousCandidate = Candidate;

		try
		{
			Candidate = candidate;

			return SE_Rested.CalculateComfortLevel(inShelter, position);
		}
		finally
		{
			Candidate = previousCandidate;
		}
	}
}
