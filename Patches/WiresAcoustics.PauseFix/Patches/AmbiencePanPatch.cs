using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace Mugnum.ValheimMods.WiresAcoustics.PauseFix.Patches;

/// <summary>
/// Patch for "WiresAcoustics.Ambience.Pan" method.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AmbiencePanPatch
{
	/// <summary>
	/// Retrieves the render-frame ambience adjustment method.
	/// </summary>
	/// <returns> Method. </returns>
	/// <exception cref="TypeLoadException"> Type not found. </exception>
	/// <exception cref="MissingMethodException"> Expected method not found. </exception>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		var type = AccessTools.TypeByName("WiresAcoustics.Ambience")
			?? throw new TypeLoadException("Could not find WiresAcoustics.Ambience.");
		var method = AccessTools.Method(type, "Pan", [typeof(AudioMan), typeof(Transform), typeof(float)]);

		if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
		{
			throw new MissingMethodException("Could not find static void WiresAcoustics.Ambience.Pan(AudioMan, Transform, float).");
		}

		return method;
	}

	/// <summary>
	/// Prevents repeated wind multiplication when vanilla's physics-driven wind refresh is stopped.
	/// </summary>
	/// <param name="__2"> Scaled elapsed time supplied to the ambience update. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	internal static bool Prefix(float __2)
	{
		// AudioMan.Update keeps running while paused, but FixedUpdate no longer resets wind volume.
		// A visible multiplayer menu alone is not a pause and must not disable acoustics.
		return __2 > 0f && !Game.IsPaused();
	}
}
