using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ProjectilePenetration.Modules.KillRewards;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.ProjectilePenetration.Patches;

/// <summary>
/// Patch for Character.RPC_Damage method.
/// </summary>
[HarmonyPatch(typeof(Character), "RPC_Damage")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class CharacterRpcDamagePatch
{
	/// <summary>
	/// Prefix before "RPC_Damage" method execution.
	/// </summary>
	/// <param name="__instance"> Character instance. </param>
	/// <param name="hit"> Hit information. </param>
	/// <param name="__state"> Ranged kill state. </param>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Character __instance, HitData hit, out RangedKillDamageState __state)
	{
		__state = RangedKillStaminaHandler.HandleBeforeDamage(__instance, hit);
	}

	/// <summary>
	/// Postfix after "RPC_Damage" method execution.
	/// </summary>
	/// <param name="__instance"> Character instance. </param>
	/// <param name="__state"> Ranged kill state. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Character __instance, RangedKillDamageState __state)
	{
		RangedKillStaminaHandler.HandleAfterDamage(__instance, __state);
	}
}
