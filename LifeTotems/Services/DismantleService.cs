using Mugnum.ValheimMods.LifeTotems.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Removes lives before destruction, then delivers durable refunds only after actual destruction. </summary>
internal static class DismantleService
{
	internal sealed class Removal
	{
		internal List<RecoveredOffering> Recoveries;
	}
	private static float NextCheck;

	internal static Removal Begin(string reliquary, Vector3 position)
	{
		if (!LedgerStore.Ready()) throw new InvalidOperationException(LedgerStore.Error ?? "LifeTotems world state is not ready.");
		var ledger = LedgerStore.Current;
		var charges = ledger.Charges.Where(c => c.Reliquary == reliquary).OrderBy(c => c.Slot).ToList();
		if (charges.Count == 0) return null;
		foreach (var charge in charges)
		{
			if (charge.Recipe == null || charge.Crafter == 0)
				throw new InvalidOperationException("A totem has no saved offering information; dismantling has been cancelled.");
			RefundInventoryCodec.Decode(charge.PaidInventory, charge.InventoryWidth, charge.InventoryHeight);
		}
		var removal = new Removal { Recoveries = charges.Select(charge => new RecoveredOffering
		{
			Id = Guid.NewGuid().ToString("N"), Charge = charge, X = position.x, Y = position.y, Z = position.z
		}).ToList() };
		foreach (var charge in charges) ledger.Charges.Remove(charge);
		ledger.Recoveries.AddRange(removal.Recoveries);
		try { LedgerStore.Save(); }
		catch
		{
			foreach (var recovery in removal.Recoveries) ledger.Recoveries.Remove(recovery);
			ledger.Charges.AddRange(charges);
			throw;
		}
		return removal;
	}

	internal static void Complete(Removal removal)
	{
		if (removal == null) return;
		foreach (var recovery in removal.Recoveries) recovery.Confirmed = true;
		try { LedgerStore.Save(); ProcessRefunds(); }
		catch (Exception ex) { Plugin.Log.LogError($"Totem refunds remain pending and will be retried.\n{ex}"); }
	}

	internal static void Cancel(Removal removal)
	{
		if (removal == null || removal.Recoveries.Any(r => r.Confirmed)) return;
		foreach (var recovery in removal.Recoveries)
		{
			LedgerStore.Current.Recoveries.Remove(recovery);
			LedgerStore.Current.Charges.Add(recovery.Charge);
		}
		LedgerStore.Save();
	}

	internal static void Tick()
	{
		if (!Plugin.IsActive || !ZNet.IsSinglePlayer || !Player.m_localPlayer || LedgerStore.Current == null || Time.unscaledTime < NextCheck) return;
		NextCheck = Time.unscaledTime + 1f;
		try { ProcessRefunds(); }
		catch (Exception ex) { NextCheck = Time.unscaledTime + 15f; Plugin.Log.LogError($"Life Totem refunds are still pending.\n{ex}"); }
	}

	/// <summary> Refund beside the removed building; inventory capacity cannot discard a refund. </summary>
	internal static void ProcessRefunds()
	{
		var ledger = LedgerStore.Current;
		var player = Player.m_localPlayer;
		if (ledger == null || !player) return;
		var pending = ledger.Recoveries.Where(r => r.Confirmed && !r.RefundComplete
			&& Vector3.Distance(player.transform.position, new Vector3(r.X, r.Y, r.Z)) < 32f).ToArray();
		var spent = ledger.Recoveries.Where(r => r.Rebound && r.RefundComplete).ToArray();
		if (pending.Length == 0 && spent.Length == 0) return;
		// Flush any earlier in-memory progress before attempting another physical drop.
		LedgerStore.Save();
		foreach (var recovery in pending)
		{
			var items = RefundInventoryCodec.Decode(recovery.Charge.PaidInventory, recovery.Charge.InventoryWidth, recovery.Charge.InventoryHeight);
			for (; recovery.RefundedStacks < items.Count;)
			{
				var index = recovery.RefundedStacks;
				var item = items[index];
				var position = new Vector3(recovery.X, recovery.Y + 0.8f, recovery.Z)
					+ new Vector3((index % 3 - 1) * 0.35f, 0, (index / 3) * 0.25f);
				if (!ItemDrop.DropItem(item, item.m_stack, position, Quaternion.identity))
					throw new InvalidOperationException("A refunded item could not be spawned.");
				recovery.RefundedStacks++;
				LedgerStore.Save();
			}
			recovery.RefundComplete = true;
			LedgerStore.Save();
		}
		if (spent.Length > 0)
		{
			foreach (var recovery in spent) ledger.Recoveries.Remove(recovery);
			try { LedgerStore.Save(); }
			catch { ledger.Recoveries.AddRange(spent); throw; }
		}
	}
}
