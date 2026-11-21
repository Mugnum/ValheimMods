using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.BonusSkillsUncap.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Patches;

/// <summary>
/// Extends only the interpolations driven by vanilla skill factors.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class SkillInterpolationPatch
{
	/// <summary>
	/// Known skill interpolation sites and expected numbers of float Lerp calls.
	/// </summary>
	private static readonly (Type Type, string Name, int Count, int[] Calls)[] Targets =
	[
		(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage), 1, []),
		(typeof(Player), "CheckRun", 1, []),
		(typeof(Player), "OnSwimming", 1, []),
		(typeof(Player), "GetDodgeStaminaUse", 1, []),
		(typeof(Player), "OnSneaking", 1, []),
		(typeof(Player), "UpdateStealth", 1, []),
		(typeof(Sadle), "UpdateRiding", 1, []),
		(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime), 1, []),
		(typeof(FishingFloat), "FixedUpdate", 3, []),
		(typeof(Piece), "Awake", 1, []),
		(typeof(Piece), nameof(Piece.OnPlaced), 1, []),
		(typeof(Attack), "DoMeleeAttack", 1, []),
		// The first two projectile interpolations use bow draw, not skill. Keep those unchanged.
		(typeof(Attack), "FireProjectileBurst", 3, [2])
	];

	/// <summary>
	/// Retrieves methods containing skill-based interpolations.
	/// </summary>
	/// <returns> Methods to patch. </returns>
	/// <exception cref="MissingMethodException"> Expected method not found. </exception>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return Targets.Select(target => AccessTools.DeclaredMethod(target.Type, target.Name)
			?? throw new MissingMethodException($"Could not find {target.Type.FullName}.{target.Name}."));
	}

	/// <summary>
	/// Replaces selected skill interpolation calls, preserving other gameplay calculations.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <param name="__originalMethod"> Method currently being patched. </param>
	/// <returns> Patched instructions. </returns>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		var target = Targets.Single(target => target.Type == __originalMethod.DeclaringType && target.Name == __originalMethod.Name);
		return SkillIlReplacements.ReplaceCalls(instructions,
			AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), [typeof(float), typeof(float), typeof(float)]),
			AccessTools.Method(typeof(SkillBonusMath), nameof(SkillBonusMath.Lerp)), target.Count, target.Calls);
	}
}
