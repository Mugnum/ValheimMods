using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Mugnum.ValheimMods.ValheimRayTracing.MenuShortcut.Patches;

/// <summary>
/// Patch for "ValheimRayTracing.Plugin.HandleMenuToggle" method.
/// </summary>
[HarmonyPatch]
internal static class HandleMenuTogglePatch
{
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

		return AccessTools.Method(type, "HandleMenuToggle", Type.EmptyTypes)
			?? throw new MissingMethodException("Could not find ValheimRayTracing.Plugin.HandleMenuToggle.");
	}

	/// <summary>
	/// Replaces the F1 key check with the configured shortcut check.
	/// </summary>
	/// <param name="instructions"> Original method instructions. </param>
	/// <returns> Patched method instructions. </returns>
	/// <exception cref="InvalidOperationException"> Expected key check not found. </exception>
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var codes = instructions.ToList();
		var getKeyDownMethod = AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), [typeof(KeyCode)]);
		var shortcutMethod = AccessTools.Method(typeof(HandleMenuTogglePatch), nameof(IsMenuShortcutDown));
		var matches = new List<int>();

		for (var i = 0; i < codes.Count - 1; i++)
		{
			if (codes[i].LoadsConstant((int)KeyCode.F1) && codes[i + 1].Calls(getKeyDownMethod))
			{
				matches.Add(i);
			}
		}

		if (matches.Count != 1)
		{
			throw new InvalidOperationException($"Expected one F1 menu key check, but found {matches.Count}.");
		}

		var index = matches[0];

		// Keep the original instruction objects to preserve branch labels and exception blocks.
		codes[index].opcode = OpCodes.Nop;
		codes[index].operand = null;
		codes[index + 1].opcode = OpCodes.Call;
		codes[index + 1].operand = shortcutMethod;

		return codes;
	}

	/// <summary>
	/// Checks the current shortcut, including configured modifier keys.
	/// </summary>
	/// <returns> Was the shortcut pressed this frame. </returns>
	private static bool IsMenuShortcutDown()
	{
		return Plugin.MenuShortcut.Value.IsDown();
	}
}
