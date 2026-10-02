using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ComfortHints.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mugnum.ValheimMods.ComfortHints.Patches;

/// <summary>
/// Refreshes hints after build buttons or search results change.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class BuildUiRefreshPatch
{
	/// <summary>
	/// Gets the build UI methods that change displayed pieces.
	/// </summary>
	[HarmonyTargetMethods]
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(BuildUi), "UpdatePieceButtons");
		yield return AccessTools.Method(typeof(BuildUi), "UpdateSearch");
	}

	/// <summary>
	/// Requests one refresh after all UI changes in this frame.
	/// </summary>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(BuildUi __instance, MethodBase __originalMethod)
	{
		ComfortHintController.Refresh(__instance, Player.m_localPlayer, $"BuildUi.{__originalMethod.Name}");
	}
}
