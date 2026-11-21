using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for the crop overlap query inside Attack.DoMeleeAttack.
/// </summary>
[HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
internal static class AttackHarvestAreaPatch
{
	/// <summary>
	/// Replaces only the harvest query, preserving unrelated overlap queries and attack logic.
	/// </summary>
	/// <param name="instructions"> Original melee attack instructions. </param>
	/// <returns> Patched melee attack instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected harvest query not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var harvestMask = AccessTools.Field(typeof(Attack), "m_harvestRayMask");
		var overlap = AccessTools.Method(typeof(Physics), nameof(Physics.OverlapSphereNonAlloc),
			[typeof(Vector3), typeof(float), typeof(Collider[]), typeof(int)]);
		var matches = new List<int>();

		for (var i = 1; i < codes.Count; i++)
		{
			if (codes[i].Calls(overlap) && codes[i - 1].LoadsField(harvestMask))
			{
				matches.Add(i);
			}
		}

		if (matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one scythe harvest overlap query, but found {matches.Count}.");
		}

		// Keep the call instruction and its branch labels; the replacement uses the same arguments.
		codes[matches[0]].opcode = OpCodes.Call;
		codes[matches[0]].operand = AccessTools.Method(typeof(ScytheHarvestArea), nameof(ScytheHarvestArea.OverlapSphere));
		return codes;
	}
}
