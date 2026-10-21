using HarmonyLib;
using System;
using System.Reflection;

namespace Mugnum.ValheimMods.Stride.DeathFix.Common;

/// <summary>
/// Uses Stride's own restore and reset methods at player lifecycle boundaries.
/// </summary>
internal static class StrideCleanup
{
	/// <summary>
	/// Restores Stride's modified stamina bars.
	/// </summary>
	private static Action<Hud> RestoreDebtHud;

	/// <summary>
	/// Resets Stride's debt tracking flags.
	/// </summary>
	private static Action ResetDebtTracking;

	/// <summary>
	/// Resets Stride's buffered jumps, wall contacts and fall rolls.
	/// </summary>
	private static Action ResetMovementTracking;

	/// <summary>
	/// Method patched to restore the HUD before its tracking flag is cleared.
	/// </summary>
	internal static MethodInfo ResetTrackingMethod { get; private set; }

	/// <summary>
	/// Resolves and validates Stride's internal cleanup methods.
	/// </summary>
	internal static void Initialize()
	{
		var debtType = AccessTools.TypeByName("StrideMod.StaminaDebt")
			?? throw new TypeLoadException("Could not find StrideMod.StaminaDebt.");
		var stateType = AccessTools.TypeByName("StrideMod.StrideState")
			?? throw new TypeLoadException("Could not find StrideMod.StrideState.");

		var restoreMethod = GetStaticMethod(debtType, "RestoreHudVisuals", [typeof(Hud)]);
		ResetTrackingMethod = GetStaticMethod(debtType, "ResetTracking", Type.EmptyTypes);
		var resetStateMethod = GetStaticMethod(stateType, "ResetAll", Type.EmptyTypes);

		RestoreDebtHud = AccessTools.MethodDelegate<Action<Hud>>(restoreMethod);
		ResetDebtTracking = AccessTools.MethodDelegate<Action>(ResetTrackingMethod);
		ResetMovementTracking = AccessTools.MethodDelegate<Action>(resetStateMethod);
	}

	/// <summary>
	/// Retrieves an expected static method returning void.
	/// </summary>
	/// <param name="type"> Declaring type. </param>
	/// <param name="name"> Method name. </param>
	/// <param name="parameters"> Parameter types. </param>
	/// <returns> Validated method. </returns>
	/// <exception cref="MissingMethodException"> Expected method not found. </exception>
	private static MethodInfo GetStaticMethod(Type type, string name, Type[] parameters)
	{
		var method = AccessTools.DeclaredMethod(type, name, parameters);

		if (method == null || !method.IsStatic || method.ReturnType != typeof(void))
		{
			throw new MissingMethodException($"Could not find expected static void method {type.FullName}.{name}.");
		}

		return method;
	}

	/// <summary>
	/// Restores color, scale and enabled state before Stride forgets that it modified the bars.
	/// </summary>
	internal static void RestoreHud()
	{
		var hud = Hud.instance;

		if (hud)
		{
			RestoreDebtHud(hud);
		}
	}

	/// <summary>
	/// Clears temporary debt and movement state after the local player's death.
	/// </summary>
	internal static void ResetAfterDeath()
	{
		RestoreHud();
		ResetDebtTracking();
		ResetMovementTracking();
	}
}
