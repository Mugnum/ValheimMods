using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.StackingMeads.Meads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.StackingMeads.Patches;

/// <summary>
/// Applies additive stacks per actual nearby player while preserving vanilla range and ownership rules.
/// </summary>
[HarmonyPatch(typeof(Tameable), "DecreaseRemainingTime")]
internal static class TameableDecreaseRemainingTimePatch
{
	/// <summary>
	/// Captures each nearby player and adjusts only that player's vanilla boost multiplier.
	/// </summary>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		var codes = instructions.ToList();
		var getSEMan = AccessTools.Method(typeof(Character), nameof(Character.GetSEMan));
		var boostField = AccessTools.Field(typeof(Tameable), nameof(Tameable.m_tamingBoostMultiplier));
		if (codes.Count(code => code.Calls(getSEMan)) != 1 || codes.Count(code => code.LoadsField(boostField)) != 1)
		{
			throw new InvalidOperationException("Expected one nearby-player status lookup and one taming boost multiplier.");
		}

		var player = generator.DeclareLocal(typeof(Character));
		var getMultiplier = AccessTools.Method(typeof(AnimalWhispersMead), nameof(AnimalWhispersMead.GetTamingMultiplier));
		foreach (var code in codes)
		{
			if (code.Calls(getSEMan))
			{
				yield return new CodeInstruction(OpCodes.Dup).MoveLabelsFrom(code);
				yield return new CodeInstruction(OpCodes.Stloc, player);
			}

			yield return code;
			if (code.LoadsField(boostField))
			{
				yield return new CodeInstruction(OpCodes.Ldloc, player);
				yield return new CodeInstruction(OpCodes.Call, getMultiplier);
			}
		}
	}
}
