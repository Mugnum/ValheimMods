using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Mugnum.ValheimMods.RoadsideTorches.Patches;

/// <summary>
/// Patch for <see cref="PieceTable.UpdateAvailable"/> method.
/// </summary>
[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PieceTableUpdateAvailablePatch
{
	/// <summary>
	/// Prefix before <see cref="PieceTable.UpdateAvailable"/> method execution.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(PieceTable __instance)
	{
		if (__instance == null || __instance.m_pieces == null)
		{
			return;
		}

		foreach (var piece in
			from prefab in __instance.m_pieces
			where prefab != null
				&& CheckIsStandingTorch(prefab.name)
			select prefab.GetComponent<Piece>()
			into piece
			where piece != null
			select piece)
		{
			piece.m_craftingStation = null;
		}
	}

	/// <summary>
	/// Checks if constructible piece is a standing torch.
	/// </summary>
	/// <param name="name"> Piece name. </param>
	/// <returns> Flag indicating that piece is a constructible torch. </returns>
	private static bool CheckIsStandingTorch(string name)
	{
		const string TorchPrefabPrefix = "piece_groundtorch";
		return !string.IsNullOrEmpty(name)
			&& name.StartsWith(TorchPrefabPrefix, StringComparison.OrdinalIgnoreCase);
	}
}
