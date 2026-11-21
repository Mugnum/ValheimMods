using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for Attack.Update method.
/// </summary>
[HarmonyPatch(typeof(Attack), nameof(Attack.Update))]
internal static class AttackUpdatePatch
{
	/// <summary>
	/// Wraps only the attack cost expenditure, leaving other stamina drains alone.
	/// </summary>
	/// <param name="instructions"> Original attack update instructions. </param>
	/// <returns> Patched attack update instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected attack expenditure not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var getCost = AccessTools.Method(typeof(Attack), "GetAttackStamina");
		var useStamina = AccessTools.Method(typeof(Character), nameof(Character.UseStamina));
		var matches = new List<int>();

		for (var i = 1; i < codes.Count; i++)
		{
			if (codes[i].Calls(useStamina) && codes[i - 1].Calls(getCost))
			{
				matches.Add(i);
			}
		}

		if (matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one attack stamina expenditure, but found {matches.Count}.");
		}

		var index = matches[0];
		var call = codes[index];
		call.opcode = OpCodes.Call;
		call.operand = AccessTools.Method(typeof(ScytheStamina), nameof(ScytheStamina.UseAttackStamina));
		codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(call));
		return codes;
	}
}
