using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassFarming.PlantingFix.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.MassFarming.PlantingFix.Patches;

/// <summary>
/// Patch for "MassFarming.MassPlant.UpdatePlacementGhostPostfix" method.
/// </summary>
[HarmonyPatch]
internal static class UpdatePlacementGhostPostfixPatch
{
	/// <summary>
	/// Retrieves the placement preview method without an assembly reference to MassFarming.
	/// </summary>
	/// <returns> Preview method. </returns>
	/// <exception cref="TypeLoadException"> MassFarming type not found. </exception>
	/// <exception cref="MissingMethodException"> Expected preview method not found. </exception>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		var type = AccessTools.TypeByName("MassFarming.MassPlant")
			?? throw new TypeLoadException("Could not find MassFarming.MassPlant.");
		var method = AccessTools.Method(type, "UpdatePlacementGhostPostfix", [typeof(Player), typeof(bool)]);

		return method != null && method.IsStatic && method.ReturnType == typeof(void)
			? method
			: throw new MissingMethodException("Could not find MassFarming.MassPlant.UpdatePlacementGhostPostfix.");
	}

	/// <summary>
	/// Changes only budget bookkeeping, retaining the existing geometry, resource and grid checks.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <param name="__originalMethod"> MassFarming's placement preview method. </param>
	/// <returns> Patched method instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected budget calculation not found. </exception>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		var codes = instructions.ToList();
		var getStamina = AccessTools.Method(typeof(Player), nameof(Player.GetStamina));
		var staminaCost = AccessTools.Field(typeof(Attack), nameof(Attack.m_attackStamina));
		var seedAmount = AccessTools.Field(typeof(Piece.Requirement), nameof(Piece.Requirement.m_amount));
		var highlight = AccessTools.Method(typeof(Piece), nameof(Piece.SetInvalidPlacementHeightlight));
		var resourcePiece = AccessTools.Field(__originalMethod.DeclaringType, "_fakeResourcePiece");
		var staminaReads = new List<int>();
		var staminaDebits = new List<int>();
		var staminaChecks = new List<int>();
		var requirementLoads = new List<int>();
		var invalidLoads = new List<int>();

		for (var i = 0; i < codes.Count; i++)
		{
			if (codes[i].Calls(getStamina))
			{
				staminaReads.Add(i);
			}
			if (i > 0 && i + 1 < codes.Count
				&& codes[i].opcode == OpCodes.Sub
				&& codes[i - 1].LoadsField(staminaCost)
				&& codes[i + 1].IsStloc())
			{
				staminaDebits.Add(i);
			}
			if (i > 0 && codes[i].opcode == OpCodes.Clt && codes[i - 1].LoadsField(staminaCost))
			{
				staminaChecks.Add(i);
			}
			if (i + 3 < codes.Count
				&& codes[i].IsLdloc()
				&& codes[i + 1].LoadsField(seedAmount)
				&& codes[i + 2].opcode == OpCodes.Add
				&& codes[i + 3].StoresField(seedAmount))
			{
				requirementLoads.Add(i);
			}
			if (i > 0 && codes[i].Calls(highlight) && codes[i - 1].IsLdloc())
			{
				invalidLoads.Add(i - 1);
			}
		}

		if (staminaReads.Count != 1
			|| staminaDebits.Count != 1
			|| staminaChecks.Count != 1
			|| requirementLoads.Count != 1
			|| invalidLoads.Count != 1
			|| resourcePiece == null
			|| !resourcePiece.IsStatic
			|| resourcePiece.FieldType != typeof(Piece))
		{
			throw new InvalidOperationException("Could not identify MassFarming's stamina, seed and invalid-position budget calculations.");
		}

		var requirementLoad = codes[requirementLoads[0]];
		var invalidLoad = codes[invalidLoads[0]];
		var getBudget = codes[staminaReads[0]];
		getBudget.opcode = OpCodes.Call;
		getBudget.operand = AccessTools.Method(typeof(PreviewBudget), nameof(PreviewBudget.GetAvailableStamina));
		var checkBudget = codes[staminaChecks[0]];
		checkBudget.opcode = OpCodes.Call;
		checkBudget.operand = AccessTools.Method(typeof(PreviewBudget), nameof(PreviewBudget.HasInsufficientStamina));

		// The original subtraction already has stamina and cost on the stack.
		// Reuse its instruction so branch labels still enter at the start of the replacement.
		var debitIndex = staminaDebits[0];
		codes[debitIndex].opcode = OpCodes.Ldsfld;
		codes[debitIndex].operand = resourcePiece;
		codes.InsertRange(debitIndex + 1,
		[
			new CodeInstruction(requirementLoad.opcode, requirementLoad.operand),
			new CodeInstruction(invalidLoad.opcode, invalidLoad.operand),
			new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PreviewBudget), nameof(PreviewBudget.CompletePosition)))
		]);

		return codes;
	}
}
