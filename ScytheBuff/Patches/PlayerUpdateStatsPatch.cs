using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for Player.UpdateStats(float) method.
/// </summary>
[HarmonyPatch(typeof(Player), "UpdateStats", new[] { typeof(float) })]
internal static class PlayerUpdateStatsPatch
{
	/// <summary>
	/// Replaces the stamina animation gate while preserving Eitr and other restrictions.
	/// </summary>
	/// <param name="instructions"> Original stats update instructions. </param>
	/// <returns> Patched stats update instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected stamina animation gate not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var inAttack = AccessTools.Method(typeof(Character), nameof(Character.InAttack));
		var staminaRegen = AccessTools.Field(typeof(Player), nameof(Player.m_staminaRegen));
		var staminaStart = codes.FindIndex(code => code.LoadsField(staminaRegen));
		var matches = new List<int>();

		for (var i = 0; i < staminaStart; i++)
		{
			if (codes[i].Calls(inAttack))
			{
				matches.Add(i);
			}
		}

		if (staminaStart < 0 || matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one stamina regeneration attack gate, but found {matches.Count}.");
		}

		// Keep the call's branch labels and leave the later Eitr attack gate untouched.
		codes[matches[0]].opcode = OpCodes.Call;
		codes[matches[0]].operand = AccessTools.Method(typeof(ScytheStamina), nameof(ScytheStamina.IsAttackBlockingRegeneration));
		return codes;
	}
}
