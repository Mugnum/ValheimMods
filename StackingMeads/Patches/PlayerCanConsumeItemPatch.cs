using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.StackingMeads.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.StackingMeads.Patches;

/// <summary>
/// Replaces only the two cooldown checks, preserving vanilla consumption validation and messages.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
internal static class PlayerCanConsumeItemPatch
{
	/// <summary>
	/// Supplies the consumed item to the supported-mead cooldown checks.
	/// </summary>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var haveEffect = AccessTools.Method(typeof(SEMan), nameof(SEMan.HaveStatusEffect), [typeof(int)]);
		var haveCategory = AccessTools.Method(typeof(SEMan), nameof(SEMan.HaveStatusEffectCategory));
		if (codes.Count(code => code.Calls(haveEffect)) != 1 || codes.Count(code => code.Calls(haveCategory)) != 1)
		{
			throw new InvalidOperationException("Expected one duplicate-effect check and one category cooldown check in Player.CanConsumeItem.");
		}

		foreach (var code in codes)
		{
			var replacement = code.Calls(haveEffect) ? nameof(MeadController.HasBlockingEffect)
				: code.Calls(haveCategory) ? nameof(MeadController.HasBlockingCategory) : null;
			if (replacement != null)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
				code.opcode = OpCodes.Call;
				code.operand = AccessTools.Method(typeof(MeadController), replacement);
			}

			yield return code;
		}
	}
}
