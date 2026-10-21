using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mugnum.ValheimMods.ShieldSledding.RemapShortcuts.Patches;

/// <summary>
/// Patch for "ShieldSledding.Utility.WasDismountPressed" method.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class WasDismountPressedPatch
{
	/// <summary>
	/// Retrieves the original shortcut check.
	/// </summary>
	/// <returns> Patched method. </returns>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		var type = AccessTools.TypeByName("ShieldSledding.Utility")
			?? throw new TypeLoadException("Could not find ShieldSledding.Utility.");
		return AccessTools.Method(type, "WasDismountPressed", Type.EmptyTypes)
			?? throw new MissingMethodException("Could not find ShieldSledding.Utility.WasDismountPressed.");
	}

	/// <summary>
	/// Replaces both hardcoded Escape checks with the configurable shortcut.
	/// </summary>
	/// <param name="__result"> Was cancel pressed. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(ref bool __result)
	{
		__result = Plugin.WasCancelPressed();
		return false;
	}
}
