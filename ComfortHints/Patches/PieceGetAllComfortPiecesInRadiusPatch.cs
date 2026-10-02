using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ComfortHints.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.ComfortHints.Patches;

/// <summary>
/// Patch for <see cref="Piece.GetAllComfortPiecesInRadius"/> method.
/// </summary>
[HarmonyPatch(typeof(Piece), nameof(Piece.GetAllComfortPiecesInRadius))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PieceGetAllComfortPiecesInRadiusPatch
{
	/// <summary>
	/// Postfix after <see cref="Piece.GetAllComfortPiecesInRadius"/> method execution.
	/// </summary>
	/// <param name="__2"> List populated by vanilla comfort lookup. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(List<Piece> __2)
	{
		var pieces = __2;
		var candidate = ComfortProbe.Candidate;

		if (candidate == null)
		{
			return;
		}

		if (pieces == null)
		{
			Plugin.Diagnostic("Hypothetical comfort lookup received a null piece list.");
			return;
		}

		if (!pieces.Contains(candidate))
		{
			// Adds hypothetical piece to vanilla comfort lookup.
			pieces.Add(candidate);
		}

		ComfortProbe.ConfirmCandidateIncluded();
	}
}
