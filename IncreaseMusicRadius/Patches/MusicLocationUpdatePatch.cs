using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.IncreaseMusicRadius.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace Mugnum.ValheimMods.IncreaseMusicRadius.Patches;

/// <summary>
/// Patch for MusicLocation.Update method.
/// </summary>
[HarmonyPatch(typeof(MusicLocation), "Update")]
internal static class MusicLocationUpdatePatch
{
	/// <summary>
	/// Preserves the original enemy check radius while expanding the audible music range.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched method instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected enemy check not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var radiusField = AccessTools.Field(typeof(MusicLocation), nameof(MusicLocation.m_radius));
		var enemyCheckMethod = AccessTools.Method(typeof(BaseAI), nameof(BaseAI.HaveEnemyInRange),
			[typeof(Character), typeof(Vector3), typeof(float)]);
		var originalRadiusMethod = AccessTools.Method(typeof(MusicRangeState), nameof(MusicRangeState.GetEnemyCheckRadius));
		var matches = new List<int>();

		for (var i = 0; i < codes.Count - 1; i++)
		{
			if (codes[i].LoadsField(radiusField) && codes[i + 1].Calls(enemyCheckMethod))
			{
				matches.Add(i);
			}
		}

		if (matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one legacy location music enemy-radius check, but found {matches.Count}.");
		}

		// Both operations consume the trigger instance and return one radius; labels remain on the same instruction.
		codes[matches[0]].opcode = OpCodes.Call;
		codes[matches[0]].operand = originalRadiusMethod;
		return codes;
	}
}
