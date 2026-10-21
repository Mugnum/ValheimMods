using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassCheckStacking.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassCheckStacking.Patches;

/// <summary>
/// Patch for <see cref="Container.StackAll"/> method, called by vanilla's hold-to-stack action.
/// </summary>
[HarmonyPatch(typeof(Container), nameof(Container.StackAll))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ContainerStackAllPatch
{
	/// <summary>
	/// Converts a modified hold action into a sequential batch.
	/// </summary>
	/// <param name="__instance"> Chest receiving the stacking interaction. </param>
	/// <returns> Flag indicating need to execute the original single-chest stacking method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Container __instance)
	{
		if (ChestStackController.IsSendingRequest)
		{
			return true;
		}
		if (ChestStackController.IsPending(__instance))
		{
			return false;
		}
		if (!Plugin.IsActive || !Plugin.Enabled.Value || !Plugin.IsModifierHeld()
			|| !ChestStackController.IsSupported(__instance))
		{
			return true;
		}

		ChestStackController.Start(__instance);
		return false;
	}
}
