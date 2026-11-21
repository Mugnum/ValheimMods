using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.StackingMeads.Common;
using Mugnum.ValheimMods.StackingMeads.Meads;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.StackingMeads.Patches;

/// <summary>
/// Limits stack creation to actual local drink consumption, including nested-call cleanup.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class PlayerConsumeItemPatch
{
	/// <summary>
	/// Current synchronous consumption scope, independent for each thread.
	/// </summary>
	[ThreadStatic]
	private static MeadDefinition CurrentDrink;

	/// <summary>
	/// Identifies a prefix that actually ran, preserving an outer scope if another patch skips it.
	/// </summary>
	private sealed class DrinkScope
	{
		internal MeadDefinition Previous;
	}

	/// <summary>
	/// Begins a local supported-mead scope without changing inventory or vanilla checks.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player __instance, ItemDrop.ItemData item, out DrinkScope __state)
	{
		__state = new DrinkScope { Previous = CurrentDrink };
		CurrentDrink = __instance == Player.m_localPlayer
			? MeadRegistry.Find(item?.m_shared?.m_consumeStatusEffect)
			: null;
	}

	/// <summary>
	/// Restores the previous scope even if another patch or vanilla throws.
	/// </summary>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer(DrinkScope __state)
	{
		if (__state != null)
		{
			CurrentDrink = __state.Previous;
		}
	}

	/// <summary>
	/// Checks that a matching local drink caused the status addition.
	/// </summary>
	internal static bool IsDrinking(SEMan manager, MeadDefinition definition)
	{
		return ReferenceEquals(CurrentDrink, definition) && MeadController.IsLocal(manager);
	}
}
