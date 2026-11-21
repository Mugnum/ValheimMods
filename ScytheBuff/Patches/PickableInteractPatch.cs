using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ScytheBuff.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.ScytheBuff.Patches;

/// <summary>
/// Patch for <see cref="Pickable.Interact"/> method.
/// </summary>
[HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
internal static class PickableInteractPatch
{
	/// <summary>
	/// Adjusts the chance value without changing crop fields, skill factors or the vanilla random roll.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched method instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected chance read not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var chanceField = AccessTools.Field(typeof(Pickable), nameof(Pickable.m_maxLevelBonusChance));
		var chanceMethod = AccessTools.Method(typeof(ScytheRules), nameof(ScytheRules.AdjustBonusChance));
		var matches = new List<int>();

		for (var i = 0; i < codes.Count; i++)
		{
			if (codes[i].LoadsField(chanceField))
			{
				matches.Add(i);
			}
		}

		if (matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one crop bonus chance read, but found {matches.Count}.");
		}

		codes.InsertRange(matches[0] + 1,
		[
			new CodeInstruction(OpCodes.Ldarg_0), // Crop whose chance was loaded.
			new CodeInstruction(OpCodes.Ldarg_1), // Character interacting with this crop.
			new CodeInstruction(OpCodes.Call, chanceMethod)
		]);

		return codes;
	}
}
