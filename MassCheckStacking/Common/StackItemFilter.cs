using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.MassCheckStacking.Common;

/// <summary>
/// Filters only transfers belonging to a granted mass stacking request.
/// </summary>
internal static class StackItemFilter
{
	/// <summary>
	/// Cached name fragments, rebuilt on configuration changes.
	/// </summary>
	private static string[] Exclusions = [];

	/// <summary>
	/// Destination currently executing vanilla StackAll.
	/// </summary>
	private static Inventory Target;

	/// <summary>
	/// Player inventory currently supplying items.
	/// </summary>
	private static Inventory Source;

	/// <summary>
	/// Player whose equipment must be preserved.
	/// </summary>
	private static Player TransferPlayer;

	/// <summary>
	/// Parses exclusions while preserving spaces within each fragment.
	/// </summary>
	/// <param name="text"> Comma-separated name fragments, or null to clear exclusions. </param>
	internal static void SetExclusions(string text)
	{
		Exclusions = (text ?? string.Empty).Split(',')
			.Select(fragment => fragment.Trim())
			.Where(fragment => fragment.Length > 0)
			.ToArray();
	}

	/// <summary>
	/// Starts filtering the exact source and destination used by this response.
	/// </summary>
	/// <param name="target"> Chest inventory receiving items. </param>
	/// <param name="source"> Player inventory supplying items. </param>
	/// <param name="player"> Player whose equipped items must be preserved. </param>
	internal static void BeginTransfer(Inventory target, Inventory source, Player player)
	{
		Target = target;
		Source = source;
		TransferPlayer = player;
	}

	/// <summary>
	/// Checks whether an AddItem call belongs to the active filtered transfer.
	/// </summary>
	/// <param name="inventory"> Inventory receiving the item. </param>
	/// <param name="item"> Item offered to that inventory. </param>
	/// <returns> Does this item addition belong to the active mass stacking transfer. </returns>
	internal static bool AppliesTo(Inventory inventory, ItemDrop.ItemData item)
	{
		return ReferenceEquals(inventory, Target) && Source != null && Source.ContainsItem(item);
	}

	/// <summary>
	/// Applies permanent protections, exclusions and the registered category setting.
	/// </summary>
	/// <param name="item"> Item to check. </param>
	/// <param name="player"> Player whose equipment references are checked. </param>
	/// <returns> Is the item allowed to leave the player's inventory during mass stacking. </returns>
	internal static bool CanStack(ItemDrop.ItemData item, Player player)
	{
		if (item?.m_shared == null
			|| item.m_stack <= 0
			|| item.m_shared.m_maxStackSize <= 1
			|| item.m_shared.m_questItem
			|| item.m_equipped
			|| !player
			|| player.IsItemEquiped(item))
		{
			return false;
		}

		var name = item.m_shared.m_name;
		var localizedName = Localization.instance.Localize(name);
		var prefabName = item.m_dropPrefab ? item.m_dropPrefab.name : string.Empty;

		if (Exclusions.Any(fragment => Contains(name, fragment)
			|| Contains(localizedName, fragment)
			|| Contains(prefabName, fragment)))
		{
			return false;
		}

		var category = ItemCatalog.Classify(item);
		return !Plugin.CategorySettings.TryGetValue(category, out var setting) || setting.Value;
	}

	/// <summary>
	/// Checks an item offered by vanilla StackAll.
	/// </summary>
	/// <param name="item"> Item offered by the active transfer. </param>
	/// <returns> Is the item allowed to leave the current transfer player's inventory. </returns>
	internal static bool CanStackCurrentItem(ItemDrop.ItemData item)
	{
		return CanStack(item, TransferPlayer);
	}

	/// <summary>
	/// Performs culture-independent, case-insensitive substring matching.
	/// </summary>
	/// <param name="text"> Item name to search. </param>
	/// <param name="fragment"> Excluded name fragment to find. </param>
	/// <returns> Does the nonempty item name contain the fragment, ignoring case. </returns>
	private static bool Contains(string text, string fragment)
	{
		return !string.IsNullOrEmpty(text) && text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	/// <summary>
	/// Restores ordinary inventory behavior after the response, including on exceptions.
	/// </summary>
	internal static void EndTransfer()
	{
		Target = null;
		Source = null;
		TransferPlayer = null;
	}
}
