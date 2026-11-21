using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.BonusSkillsUncap.Common;

/// <summary>
/// Replaces selected math calls without changing instruction labels or exception blocks.
/// </summary>
internal static class SkillIlReplacements
{
	/// <summary>
	/// Replaces known call sites after validating the expected method layout.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <param name="original"> Math method to replace. </param>
	/// <param name="replacement"> Replacement with the same signature. </param>
	/// <param name="expectedCount"> Expected number of original calls. </param>
	/// <param name="selectedCalls"> Zero-based call ordinals; empty replaces all calls. </param>
	/// <returns> Patched instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected calls not found. </exception>
	internal static IEnumerable<CodeInstruction> ReplaceCalls(IEnumerable<CodeInstruction> instructions,
		MethodInfo original, MethodInfo replacement, int expectedCount, params int[] selectedCalls)
	{
		var codes = instructions.ToList();
		var calls = codes.Where(code => code.Calls(original)).ToArray();

		if (calls.Length != expectedCount)
		{
			throw new InvalidOperationException($"Expected {expectedCount} calls to {original.Name}, but found {calls.Length}.");
		}

		for (var i = 0; i < calls.Length; i++)
		{
			if (selectedCalls.Length != 0 && !selectedCalls.Contains(i))
			{
				continue;
			}

			calls[i].opcode = OpCodes.Call;
			calls[i].operand = replacement;
		}

		return codes;
	}
}
