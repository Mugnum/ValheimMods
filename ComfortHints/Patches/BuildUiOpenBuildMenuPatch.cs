using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ComfortHints.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.ComfortHints.Patches;

/// <summary>
/// Patch for BuildUi.OpenBuildMenu method.
/// </summary>
[HarmonyPatch(typeof(BuildUi), "OpenBuildMenu")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class BuildUiOpenBuildMenuPatch
{
	/// <summary>
	/// Refreshes comfort hints after opening build menu.
	/// </summary>
	/// <param name="__instance"> Build UI instance. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(BuildUi __instance)
	{
		ComfortHintController.Refresh(__instance, Player.m_localPlayer, "BuildUi.OpenBuildMenu");
	}
}
