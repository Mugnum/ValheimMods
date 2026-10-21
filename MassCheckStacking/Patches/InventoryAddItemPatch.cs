using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.MassCheckStacking.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mugnum.ValheimMods.MassCheckStacking.Patches;

/// <summary>
/// Filters the exact AddItem overload used by vanilla Inventory.StackAll.
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class InventoryAddItemPatch
{
	/// <summary>
	/// Leaves protected source stacks untouched by reporting that they were not added.
	/// </summary>
	/// <param name="__instance"> Inventory receiving the item. </param>
	/// <param name="item"> Source item offered by vanilla stacking. </param>
	/// <param name="__result"> Addition result, set to false when the item is protected. </param>
	/// <returns> Flag indicating need to execute the original item addition method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
	{
		if (!StackItemFilter.AppliesTo(__instance, item) || StackItemFilter.CanStackCurrentItem(item))
		{
			return true;
		}

		__result = false;
		return false;
	}
}
