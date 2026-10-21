using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.Stride.DeathFix.Common;
using System.Reflection;

namespace Mugnum.ValheimMods.Stride.DeathFix.Patches;

/// <summary>
/// Patch for "StrideMod.StaminaDebt.ResetTracking" method.
/// </summary>
[HarmonyPatch]
internal static class StaminaDebtResetTrackingPatch
{
	/// <summary>
	/// Retrieves Stride's validated debt tracking reset method.
	/// </summary>
	/// <returns> Method. </returns>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		return StrideCleanup.ResetTrackingMethod;
	}

	/// <summary>
	/// Restores modified bars before ResetTracking clears the restoration guard.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix()
	{
		StrideCleanup.RestoreHud();
	}
}
