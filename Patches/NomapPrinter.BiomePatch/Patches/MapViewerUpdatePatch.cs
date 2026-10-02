using Mugnum.ValheimMods.NomapPrinter.BiomePatch.Common;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.NomapPrinter.BiomePatch.Patches;

/// <summary>
/// Patch for "NomapPrinter.MapViewer.Update" method.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MapViewerUpdatePatch
{
	/// <summary>
	/// Refreshes the biome label after baked map input and visibility updates.
	/// </summary>
	/// <param name="___parentObject"> Baked map window root. </param>
	internal static void Postfix(GameObject ___parentObject)
	{
		BiomeLabelUi.Refresh(___parentObject);
	}
}
