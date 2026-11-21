using HarmonyLib;
using Mugnum.ValheimMods.LifeTotems.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Removes exact unequipped item instances, with rollback if the ritual cannot be committed. </summary>
internal sealed class InventoryPayment : IDisposable
{
	private readonly Inventory Inventory;
	private readonly List<(ItemDrop.ItemData Item, int Stack)> Before = [];
	private bool Committed;
	internal string RefundData { get; private set; }
	internal static readonly Action<Inventory, bool, bool> NotifyChanged =
		AccessTools.MethodDelegate<Action<Inventory, bool, bool>>(AccessTools.Method(typeof(Inventory), "Changed"));

	internal static bool Matches(ItemDrop.ItemData item, Offering cost) => !item.m_equipped
		&& item.m_dropPrefab && item.m_dropPrefab.name == cost.Prefab && item.m_quality >= cost.Quality;

	internal static int Count(Inventory inventory, Offering cost) => inventory.GetAllItems().Where(item => Matches(item, cost)).Sum(item => item.m_stack);
	internal static bool CanPay(Inventory inventory, RitualRecipe recipe) => recipe.Items.All(cost => Count(inventory, cost) >= cost.Amount);

	internal InventoryPayment(Inventory inventory, RitualRecipe recipe)
	{
		Inventory = inventory;
		if (!CanPay(inventory, recipe)) throw new InvalidOperationException("You do not have all the required unequipped items.");
		try
		{
			var paid = new List<ItemDrop.ItemData>();
			foreach (var cost in recipe.Items)
			{
				var remaining = cost.Amount;
				foreach (var item in inventory.GetAllItems().Where(i => Matches(i, cost)).OrderBy(i => i.m_quality).ToArray())
				{
					Before.Add((item, item.m_stack));
					var amount = Math.Min(item.m_stack, remaining);
					var snapshot = item.Clone();
					snapshot.m_stack = amount;
					snapshot.m_equipped = false;
					paid.Add(snapshot);
					if (!inventory.RemoveItem(item, amount)) throw new InvalidOperationException("An offering item could not be removed.");
					remaining -= amount;
					if (remaining == 0) break;
				}
				if (remaining != 0) throw new InvalidOperationException("Inventory changed during the offering.");
			}
			RefundData = RefundInventoryCodec.Encode(paid, inventory.GetWidth(), inventory.GetHeight());
		}
		catch { Dispose(); throw; }
	}

	internal void Commit() => Committed = true;
	public void Dispose()
	{
		if (Committed || Before.Count == 0) return;
		foreach (var entry in Before)
		{
			entry.Item.m_stack = entry.Stack;
			if (!Inventory.GetAllItems().Contains(entry.Item)) Inventory.GetAllItems().Add(entry.Item);
		}
		Before.Clear();
		NotifyChanged(Inventory, false, false);
	}
}
