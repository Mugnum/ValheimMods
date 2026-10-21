using HarmonyLib;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.LockCursor.Patches;

/// <summary>
/// Patch for <see cref="ZCursor.LockState"/> property setter.
/// </summary>
[HarmonyPatch(typeof(ZCursor), nameof(ZCursor.LockState), MethodType.Setter)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ZCursorLockStatePatch
{
	/// <summary>
	/// Confines unrestricted cursor requests while preserving gameplay locking.
	/// </summary>
	/// <param name="__0"> Requested cursor lock mode. </param>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(ref CursorLockMode __0)
	{
		if (__0 == CursorLockMode.None)
		{
			__0 = CursorLockMode.Confined;
		}
	}
}
