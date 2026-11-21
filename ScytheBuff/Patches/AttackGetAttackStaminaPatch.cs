using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for Attack.GetAttackStamina method.
/// </summary>
[HarmonyPatch(typeof(Attack), "GetAttackStamina")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AttackGetAttackStaminaPatch
{
	/// <summary>
	/// Scales the calculated cost for both attack availability and stamina consumption.
	/// </summary>
	/// <param name="___m_character"> Attacking character. </param>
	/// <param name="___m_weapon"> Weapon performing the attack. </param>
	/// <param name="__result"> Stamina cost after vanilla skill and equipment modifiers. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Humanoid ___m_character, ItemDrop.ItemData ___m_weapon, ref float __result)
	{
		if (!Plugin.Enabled.Value
			|| !Player.m_localPlayer
			|| ___m_character != Player.m_localPlayer
			|| !ScytheRules.IsScythe(___m_weapon))
		{
			return;
		}

		var percentage = Mathf.Clamp(Plugin.StaminaUsage.Value, 0, 100);

		// Vanilla UseStamina(0) returns before resetting the regeneration timer.
		// Keep an exact zero even if another cost modifier returns a non-finite value.
		__result = percentage <= 0 ? 0f : __result * (percentage * 0.01f);
	}
}
