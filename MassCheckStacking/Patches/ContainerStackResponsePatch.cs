using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassCheckStacking.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassCheckStacking.Patches;

/// <summary>
/// Patch for Container.RPC_StackResponse, preserving vanilla's transfer and effects.
/// </summary>
[HarmonyPatch(typeof(Container), "RPC_StackResponse")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ContainerStackResponsePatch
{
	/// <summary>
	/// Enables filtering only for a valid pending batch request.
	/// </summary>
	/// <param name="__instance"> Chest receiving the stacking response. </param>
	/// <param name="granted"> Was stacking access granted by the chest owner. </param>
	/// <param name="__state"> Matching mass stacking request, or null for an unrelated response. </param>
	/// <returns> Flag indicating need to execute the original response method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Container __instance, bool granted, out StackRequest __state)
	{
		return ChestStackController.PrepareResponse(__instance, granted, out __state);
	}

	/// <summary>
	/// Releases the filter and allows the next chest after the complete response call.
	/// </summary>
	/// <param name="__state"> Request captured by the prefix, or null for an unrelated response. </param>
	/// <param name="__exception"> Exception raised during the response, or null if processing succeeded. </param>
	/// <returns> Original exception to propagate, or null when no exception occurred. </returns>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static Exception Finalizer(StackRequest __state, Exception __exception)
	{
		ChestStackController.CompleteResponse(__state, __exception);
		return __exception;
	}
}
