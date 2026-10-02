using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.ComfortHints.Common;

/// <summary>
/// Provides contextual comfort checks around the player.
/// </summary>
internal static class ComfortContext
{
	/// <summary>
	/// Vanilla comfort radius.
	/// </summary>
	private const float ComfortRadius = 10f;

	/// <summary>
	/// Reusable comfort piece buffer.
	/// </summary>
	private static readonly List<Piece> NearbyPieces = new(64);

	/// <summary>
	/// Checks whether a bed exists within comfort range.
	/// </summary>
	/// <param name="position"> Player position. </param>
	/// <returns> Flag indicating that a bed is nearby. </returns>
	internal static bool HasNearbyBed(Vector3 position)
	{
		NearbyPieces.Clear();
		Piece.GetAllComfortPiecesInRadius(position, ComfortRadius, NearbyPieces);

		var bedCount = NearbyPieces.Count(piece => piece != null && piece.GetComponent<Bed>() != null);
		Plugin.Diagnostic($"Bed lookup: radius={ComfortRadius}, nearbyComfortPieces={NearbyPieces.Count}, beds={bedCount}.");

		if (Plugin.EnableDiagnostics.Value)
		{
			var sample = string.Join(", ", NearbyPieces.Where(piece => piece != null).Take(10)
				.Select(piece => $"{piece.gameObject.name}={piece.GetComfort()}"));
			Plugin.Diagnostic($"Nearby comfort sample (up to 10): [{sample}].");
		}

		return bedCount > 0;
	}
}
