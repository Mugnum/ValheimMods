using System;
using System.Collections.Generic;

namespace Mugnum.ValheimMods.LifeTotems.Domain;

/// <summary> One offering item, identified by prefab and minimum quality. </summary>
[Serializable]
internal sealed class Offering
{
	public string Prefab;
	public int Amount;
	public int Quality = 1;
}

/// <summary> One of the three equally valid ritual alternatives. </summary>
[Serializable]
internal sealed class RitualRecipe
{
	public List<Offering> Items = [];
}

/// <summary> Character- and world-bound rolls; opening another Reliquary does not regenerate them. </summary>
[Serializable]
internal sealed class RitualRolls
{
	public int Version = 1;
	public List<RitualRecipe> Recipes = [];
}

/// <summary> One life anchored to a visual socket on a persistent Reliquary. </summary>
[Serializable]
internal sealed class LifeCharge
{
	public string Reliquary;
	public int Slot;
	public long Crafter;
	public RitualRecipe Recipe;
	public string PaidInventory;
	public int InventoryWidth;
	public int InventoryHeight;
}

/// <summary> Durable dismantling refund and a reusable offering belonging to its original crafter. </summary>
[Serializable]
internal sealed class RecoveredOffering
{
	public string Id;
	public LifeCharge Charge;
	public float X, Y, Z;
	public bool Confirmed;
	public int RefundedStacks;
	public bool RefundComplete;
	public bool Rebound;
}

/// <summary> World-bound honor-system run state, stored outside the vanilla world file. </summary>
[Serializable]
internal sealed class WorldLedger
{
	public int Version = 1;
	public long World;
	public bool GameOver;
	public List<LifeCharge> Charges = [];
	public List<RecoveredOffering> Recoveries = [];

	/// <summary> Consumes one randomly selected charge; the last charge still saves this death. </summary>
	public bool Consume(Random random)
	{
		if (GameOver || Charges.Count == 0)
		{
			GameOver = true;
			return false;
		}
		Charges.RemoveAt(random.Next(Charges.Count));
		return true;
	}
}
