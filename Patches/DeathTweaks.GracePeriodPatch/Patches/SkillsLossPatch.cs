using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Patches;

/// <summary>
/// Guards skill reduction and full resets only within a protected death operation.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SkillsLossPatch
{
	/// <summary>
	/// Gets both skill-loss paths, including the world's full-reset modifier.
	/// </summary>
	/// <returns> Patched methods. </returns>
	[HarmonyTargetMethods]
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Skills), nameof(Skills.LowerAllSkills), [typeof(float)]);
		yield return AccessTools.Method(typeof(Skills), nameof(Skills.Clear), System.Type.EmptyTypes);
	}

	/// <summary>
	/// Prevents skill loss without interfering with DeathTweaks' temporary factor restoration.
	/// </summary>
	/// <param name="__instance"> Skills being changed. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Skills __instance)
	{
		return !GracePeriod.Protects(__instance);
	}
}
