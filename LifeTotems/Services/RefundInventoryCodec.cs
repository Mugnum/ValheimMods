using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Uses vanilla's versioned item format to preserve the actual stacks sacrificed in a ritual. </summary>
internal static class RefundInventoryCodec
{
	internal static string Encode(IEnumerable<ItemDrop.ItemData> items, int width, int height)
	{
		var snapshot = new Inventory("Life Totem offering", null, width, height);
		snapshot.GetAllItems().AddRange(items);
		var package = new ZPackage();
		snapshot.Save(package);
		return Convert.ToBase64String(package.GetArray());
	}

	internal static List<ItemDrop.ItemData> Decode(string data, int width, int height)
	{
		if (string.IsNullOrEmpty(data) || width < 1 || height < 1)
			throw new InvalidOperationException("This totem has no valid saved offering refund.");
		var package = new ZPackage(Convert.FromBase64String(data));
		// Verify native loading did not silently discard items whose prefabs are missing.
		package.ReadInt();
		var expected = package.ReadUShort();
		package = new ZPackage(Convert.FromBase64String(data));
		var inventory = new Inventory("Life Totem refund", null, width, height);
		inventory.Load(package);
		var items = inventory.GetAllItems();
		if (items.Count != expected || items.Any(i => !i.m_dropPrefab || i.m_stack < 1))
			throw new InvalidOperationException("An offering refund prefab is unavailable. The refund has been preserved for later.");
		return items;
	}
}
