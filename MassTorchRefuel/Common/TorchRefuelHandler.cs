using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.MassTorchRefuel.Common;

/// <summary>
/// Refuels nearby torches in distance order using the player's inventory.
/// </summary>
internal static class TorchRefuelHandler
{
	/// <summary>
	/// Maximum time to reserve fuel while waiting for a remote owner's update.
	/// </summary>
	private const float PendingFuelTimeout = 10f;

	/// <summary>
	/// Fuel already requested but not yet reflected in local network data.
	/// </summary>
	private static readonly Dictionary<Fireplace, (int Fuel, float ExpiresAt)> PendingFuel = [];

	/// <summary>
	/// Checks whether a fireplace supports the mass refueling action.
	/// </summary>
	/// <param name="fireplace"> Fireplace to check. </param>
	/// <returns> Is this fireplace eligible. </returns>
	internal static bool IsEligible(Fireplace fireplace)
	{
		return IsRefuelable(fireplace)
			&& (!IsCampfire(fireplace) || Plugin.IncludeCampfires.Value);
	}

	/// <summary>
	/// Distinguishes campfires and braziers from torches using shared fire behavior.
	/// </summary>
	/// <param name="fireplace"> Refuelable fireplace. </param>
	/// <returns> Is this a campfire, hearth or similar fire. </returns>
	internal static bool IsCampfire(Fireplace fireplace)
	{
		// Torches do not use the smoke obstruction system; campfires, hearths and braziers do.
		// Wood also identifies smokeless campfire variants without listing individual prefabs.
		return fireplace.m_smokeSpawner
			|| fireplace.m_fuelItem.m_itemData.m_shared.m_name == "$item_wood";
	}

	/// <summary>
	/// Checks for finite fuel and a valid player-built piece without searching nearby objects.
	/// </summary>
	/// <param name="fireplace"> Fireplace to check. </param>
	/// <returns> Is this fireplace refuelable. </returns>
	private static bool IsRefuelable(Fireplace fireplace)
	{
		if (!fireplace
			|| !fireplace.gameObject.activeInHierarchy
			|| !fireplace.m_canRefill
			|| fireplace.m_infiniteFuel
			|| !fireplace.m_fuelItem
			|| fireplace.m_maxFuel <= 0f)
		{
			return false;
		}

		var piece = fireplace.GetComponent<Piece>();
		var view = fireplace.GetComponent<ZNetView>();

		return piece && piece.IsPlacedByPlayer() && view && view.IsValid();
	}

	/// <summary>
	/// Refuels the interacted torch first, followed by nearby lights in distance order.
	/// </summary>
	/// <param name="origin"> Interacted torch. </param>
	/// <param name="player"> Player providing fuel. </param>
	/// <returns> Was any fuel consumed. </returns>
	internal static bool RefuelNearby(Fireplace origin, Player player)
	{
		var inventory = player.GetInventory();

		if (inventory == null || !PrivateArea.CheckAccess(origin.transform.position))
		{
			return false;
		}

		ClearExpiredRequests();
		var campfireGroup = IsCampfire(origin);
		var position = origin.transform.position;
		var pieces = new List<Piece>();
		Piece.GetAllPiecesInRadius(position, Plugin.RefuelRadius.Value, pieces);

		var torches = pieces
			.Select(piece => piece.GetComponent<Fireplace>())
			.Append(origin)
			.Where(IsEligible)
			.Where(torch => IsCampfire(torch) == campfireGroup)
			.Distinct()
			.OrderBy(torch => torch != origin)
			.ThenBy(torch => (torch.transform.position - position).sqrMagnitude)
			.ThenBy(torch => torch.GetInstanceID())
			.ToList();

		var totalAdded = 0;
		var refueledTorches = 0;

		foreach (var torch in torches)
		{
			if (!IsEligible(torch) || !PrivateArea.CheckAccess(torch.transform.position, flash: false))
			{
				continue;
			}

			var added = Refuel(torch, player, inventory);

			if (added > 0)
			{
				totalAdded += added;
				refueledTorches++;
			}
		}

		if (totalAdded > 0)
		{
			player.Message(MessageHud.MessageType.Center,
				$"Added {totalAdded} fuel to {refueledTorches} {(campfireGroup ? "fires" : "torches")}.");
		}
		else
		{
			player.Message(MessageHud.MessageType.Center,
				$"No nearby {(campfireGroup ? "fires" : "torches")} need fuel available in your inventory.");
		}

		return totalAdded > 0;
	}

	/// <summary>
	/// Adds as much matching fuel as the current light and inventory permit.
	/// </summary>
	/// <param name="torch"> Light to refuel. </param>
	/// <param name="player"> Player providing fuel. </param>
	/// <param name="inventory"> Player inventory. </param>
	/// <returns> Number of fuel items consumed. </returns>
	private static int Refuel(Fireplace torch, Player player, Inventory inventory)
	{
		var view = torch.GetComponent<ZNetView>();
		var fuelName = torch.m_fuelItem.m_itemData.m_shared.m_name;
		var currentFuel = GetReservedFuel(torch, view);
		var missingFuel = Math.Max(0, Mathf.CeilToInt(torch.m_maxFuel) - currentFuel);
		var availableFuel = inventory.CountItems(fuelName);
		var amount = Math.Min(missingFuel, availableFuel);

		if (amount <= 0)
		{
			return 0;
		}

		if (!view.HasOwner())
		{
			view.ClaimOwnership();
		}

		var added = 0;

		// A fixed budget prevents an asynchronous owner's fuel value from causing an endless refill.
		// Vanilla UseItem removes matching inventory items and sends the normal AddFuel RPC.
		for (var i = 0; i < amount; i++)
		{
			if (!torch || !view.IsValid())
			{
				break;
			}

			var item = inventory.GetItem(fuelName);

			if (item == null)
			{
				break;
			}

			var before = inventory.CountItems(fuelName);

			if (!torch.UseItem(player, item))
			{
				break;
			}

			var consumed = before - inventory.CountItems(fuelName);

			if (consumed <= 0)
			{
				break;
			}

			added += consumed;
		}

		if (added > 0 && !view.IsOwner())
		{
			PendingFuel[torch] = (currentFuel + added, Time.unscaledTime + PendingFuelTimeout);
		}

		return added;
	}

	/// <summary>
	/// Reads displayed fuel, including requests still waiting for a remote owner's update.
	/// </summary>
	/// <param name="torch"> Light being refueled. </param>
	/// <param name="view"> Light's network view. </param>
	/// <returns> Fuel units already present or reserved. </returns>
	private static int GetReservedFuel(Fireplace torch, ZNetView view)
	{
		var currentFuel = Mathf.CeilToInt(Mathf.Max(0f, view.GetZDO().GetFloat(ZDOVars.s_fuel)));

		if (!PendingFuel.TryGetValue(torch, out var pending))
		{
			return currentFuel;
		}

		if (currentFuel >= pending.Fuel)
		{
			PendingFuel.Remove(torch);
			return currentFuel;
		}

		return pending.Fuel;
	}

	/// <summary>
	/// Releases reservations for destroyed lights or requests that did not receive an update.
	/// </summary>
	private static void ClearExpiredRequests()
	{
		foreach (var torch in PendingFuel
			.Where(pair => !pair.Key || Time.unscaledTime >= pair.Value.ExpiresAt)
			.Select(pair => pair.Key)
			.ToArray())
		{
			PendingFuel.Remove(torch);
		}
	}

	/// <summary>
	/// Clears pending fuel reservations on plugin unloading.
	/// </summary>
	internal static void Reset()
	{
		PendingFuel.Clear();
	}
}
