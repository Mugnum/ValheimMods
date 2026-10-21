using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Mugnum.ValheimMods.ValheimRayTracing.MenuShortcut.Patches;

/// <summary>
/// Patch for "ValheimRayTracing.Plugin.DrawWaitingForDlssWarning" method.
/// </summary>
[HarmonyPatch]
internal static class DrawWaitingForDlssWarningPatch
{
	/// <summary>
	/// Original shortcut hint in the DLSS warning.
	/// </summary>
	private const string OriginalHint = "F1 for ray tracing settings.";

	/// <summary>
	/// Retrieves patched method.
	/// </summary>
	/// <returns> Method. </returns>
	/// <exception cref="TypeLoadException"> Type not found. </exception>
	/// <exception cref="MissingMethodException"> Method not found. </exception>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		var type = AccessTools.TypeByName("ValheimRayTracing.Plugin")
			?? throw new TypeLoadException("Could not find ValheimRayTracing.Plugin.");

		return AccessTools.Method(type, "DrawWaitingForDlssWarning", Type.EmptyTypes)
			?? throw new MissingMethodException("Could not find ValheimRayTracing.Plugin.DrawWaitingForDlssWarning.");
	}

	/// <summary>
	/// Updates the visible shortcut hint when the warning is drawn.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched method instructions. </returns>
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var formatHintMethod = AccessTools.Method(typeof(DrawWaitingForDlssWarningPatch), nameof(FormatShortcutHint));

		foreach (var instruction in instructions)
		{
			yield return instruction;

			if (instruction.opcode == OpCodes.Ldstr
				&& instruction.operand is string text
				&& text.Contains(OriginalHint))
			{
				yield return new CodeInstruction(OpCodes.Call, formatHintMethod);
			}
		}
	}

	/// <summary>
	/// Replaces the warning's shortcut hint with the current configuration.
	/// </summary>
	/// <param name="text"> Original warning text. </param>
	/// <returns> Warning text containing the configured shortcut. </returns>
	private static string FormatShortcutHint(string text)
	{
		var shortcut = Plugin.MenuShortcut.Value;
		var hint = shortcut.MainKey == UnityEngine.KeyCode.None
			? "Ray tracing settings shortcut is disabled."
			: $"{shortcut} for ray tracing settings.";

		return text.Replace(OriginalHint, hint);
	}
}
