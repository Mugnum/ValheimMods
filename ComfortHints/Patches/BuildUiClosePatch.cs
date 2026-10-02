using HarmonyLib;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Mugnum.ValheimMods.ComfortHints.Common;

namespace Mugnum.ValheimMods.ComfortHints.Patches;

/// <summary>
/// Patch for BuildUi.Close method.
/// </summary>
[HarmonyPatch(typeof(BuildUi), "Close")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class BuildUiClosePatch
{
	/// <summary>
	/// Stops comfort evaluation after closing build menu.
	/// </summary>
	/// <param name="__instance"> Build UI instance. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(BuildUi __instance)
	{
		ComfortHintController.Invalidate("BuildUi.Close");
		ComfortHintUi.ClearAll(__instance);
	}
}
