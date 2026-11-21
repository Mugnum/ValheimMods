using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for Attack.DoMeleeAttack method.
/// </summary>
[HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AttackDoMeleeAttackPatch
{
	/// <summary>
	/// Original settings for the current attack invocation.
	/// </summary>
	internal struct AttackState
	{
		/// <summary>
		/// Enclosing swing's harvester, restored after nested attack calls.
		/// </summary>
		internal Player PreviousHarvester;

		/// <summary>
		/// Were the attack settings overridden.
		/// </summary>
		internal bool Applied;

		/// <summary>
		/// Original swing angle.
		/// </summary>
		internal float Angle;

		/// <summary>
		/// Original forward reach.
		/// </summary>
		internal float Range;

		/// <summary>
		/// Original harvest radius at minimum Farming skill.
		/// </summary>
		internal float Radius;

		/// <summary>
		/// Original harvest radius at maximum Farming skill.
		/// </summary>
		internal float MaximumRadius;
	}

	/// <summary>
	/// Expands only the local player's scythe attack for this invocation.
	/// </summary>
	/// <param name="__instance"> Current attack. </param>
	/// <param name="___m_character"> Attacking character. </param>
	/// <param name="___m_weapon"> Weapon performing the attack. </param>
	/// <param name="__state"> Original attack settings. </param>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Attack __instance, Humanoid ___m_character, ItemDrop.ItemData ___m_weapon, out AttackState __state)
	{
		__state = new AttackState { PreviousHarvester = ScytheRules.SwingHarvester };
		ScytheRules.SwingHarvester = null;

		if (!Plugin.Enabled.Value
			|| !Player.m_localPlayer
			|| ___m_character != Player.m_localPlayer
			|| !__instance.m_harvest
			|| !ScytheRules.IsScythe(___m_weapon))
		{
			return;
		}

		__state = new AttackState
		{
			PreviousHarvester = __state.PreviousHarvester,
			Applied = true,
			Angle = __instance.m_attackAngle,
			Range = __instance.m_attackRange,
			Radius = __instance.m_harvestRadius,
			MaximumRadius = __instance.m_harvestRadiusMaxLevel
		};

		var multiplier = 1f + Plugin.HarvestDistanceIncrease.Value * 0.01f;
		__instance.m_attackAngle = Plugin.SwingAngle.Value;
		__instance.m_attackRange *= multiplier;
		__instance.m_harvestRadius *= multiplier;
		__instance.m_harvestRadiusMaxLevel *= multiplier;
		ScytheRules.SwingHarvester = Player.m_localPlayer;
	}

	/// <summary>
	/// Restores attack settings even when another patch or the original method throws.
	/// </summary>
	/// <param name="__instance"> Current attack. </param>
	/// <param name="__state"> Original attack settings. </param>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer(Attack __instance, AttackState __state)
	{
		ScytheRules.SwingHarvester = __state.PreviousHarvester;

		if (!__state.Applied)
		{
			return;
		}

		__instance.m_attackAngle = __state.Angle;
		__instance.m_attackRange = __state.Range;
		__instance.m_harvestRadius = __state.Radius;
		__instance.m_harvestRadiusMaxLevel = __state.MaximumRadius;
	}
}
