using DeathRules = global::DeathTweaks.Rules.DeathRules;
using ItemFate = global::DeathTweaks.Rules.ItemFate;
using WorldDeathModifiers = global::DeathTweaks.Rules.WorldDeathModifiers;
using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Patches;

/// <summary>
/// Patch for DeathTweaks.Rules.DeathRules.Resolve method.
/// </summary>
[HarmonyPatch(typeof(DeathRules), nameof(DeathRules.Resolve))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class DeathRulesResolvePatch
{
	/// <summary>
	/// Turns destructive decisions into recoverable drops during the protected death.
	/// </summary>
	/// <param name="world"> World death modifiers. </param>
	/// <param name="__result"> Item disposition. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(WorldDeathModifiers world, ref ItemFate __result)
	{
		if (!GracePeriod.IsProtectedDeath || __result != ItemFate.Destroy)
		{
			return;
		}

		// Vanilla's tombstone path would delete dropped items again in destructive worlds.
		// Keep those items through DeathTweaks' existing held-item path instead.
		__result = world.DeleteItems || world.DeleteUnequipped
			? ItemFate.Keep
			: ItemFate.Drop;
	}
}
