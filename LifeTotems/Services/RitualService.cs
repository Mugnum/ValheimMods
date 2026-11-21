using Mugnum.ValheimMods.LifeTotems.Domain;
using Mugnum.ValheimMods.LifeTotems.World;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Progression-aware personal recipes and single-player ritual transactions. </summary>
internal static class RitualService
{
	private static readonly Random Random = new();
	internal static string RollsKey => "Mugnum.LifeTotems.Rolls." + ZNet.instance.GetWorldUID();
	internal static string NoticeKey => "Mugnum.LifeTotems.Notice." + ZNet.instance.GetWorldUID();

	/// <summary> Recovered recipes displace random choices and remain visible until individually rebuilt. </summary>
	internal sealed class OfferChoice
	{
		internal RitualRecipe Recipe;
		internal RecoveredOffering Recovery;
	}

	internal static List<OfferChoice> GetOffers(Player player)
	{
		var rolls = GetRolls(player);
		var capacity = Math.Max(0, 3 - (LedgerStore.Current?.Charges.Count ?? 0));
		var restored = (LedgerStore.Current?.Recoveries ?? []).Where(r => r.Confirmed && r.Charge.Crafter == player.GetPlayerID() && !r.Rebound)
			.Take(capacity).Select(r => new OfferChoice { Recipe = r.Charge.Recipe, Recovery = r }).ToList();
		// Visibility never deletes or regenerates displaced random choices or queued recoveries.
		return restored.Concat(rolls.Recipes.Take(Math.Max(0, capacity - restored.Count)).Select(r => new OfferChoice { Recipe = r })).ToList();
	}

	internal static int Stage()
	{
		var flags = new[] { "defeated_eikthyr", "defeated_gdking", "defeated_bonemass", "defeated_dragon", "defeated_goblinking", "defeated_queen" };
		var stage = 0;
		while (stage < flags.Length && ZoneSystem.instance.GetGlobalKey(flags[stage])) stage++;
		return stage;
	}

	internal static RitualRolls GetRolls(Player player)
	{
		if (!player.m_customData.TryGetValue(RollsKey, out var text))
		{
			var rolls = Generate();
			player.m_customData[RollsKey] = PersistenceJson.Serialize(rolls);
			return rolls;
		}
		var saved = PersistenceJson.Deserialize<RitualRolls>(text);
		if (saved == null || saved.Version != 1 || saved.Recipes == null || saved.Recipes.Count != 3
			|| saved.Recipes.Any(r => r == null || r.Items == null || r.Items.Count < 1
				|| r.Items.Any(i => i == null || string.IsNullOrEmpty(i.Prefab) || i.Amount < 1 || i.Quality < 1)
				|| r.Items.Select(i => i.Prefab).Distinct().Count() != r.Items.Count))
			throw new InvalidOperationException($"Saved ritual rolls are invalid (version {saved?.Version}, recipe count {saved?.Recipes?.Count.ToString() ?? "missing"}). " +
				"They have not been reset. Use Debug / Reset saved recipes to regenerate random offerings with a backup.");
		return saved;
	}

	internal static RitualRolls Generate()
	{
		var stage = Stage();
		var candidates = OfferingCatalog.Entries.Where(e => e.Tier <= stage && ObjectDB.instance.GetItemPrefab(e.Prefab)).ToList();
		if (candidates.Count < 6) throw new InvalidOperationException("Not enough offering prefabs have loaded.");
		var rolls = new RitualRolls();
		for (var row = 0; row < 3; row++)
		{
			var pool = new List<OfferingCatalog.Entry>(candidates);
			var recipe = new RitualRecipe();
			for (var n = 0; n < 6; n++)
			{
				var pick = Random.NextDouble() * pool.Sum(OfferingCatalog.Weight);
				var chosen = pool[pool.Count - 1];
				foreach (var entry in pool)
				{
					pick -= OfferingCatalog.Weight(entry);
					if (pick <= 0) { chosen = entry; break; }
				}
				pool.Remove(chosen);
				var maxQuality = ObjectDB.instance.GetItemPrefab(chosen.Prefab).GetComponent<ItemDrop>().m_itemData.m_shared.m_maxQuality;
				var quality = 1;
				var ceiling = Math.Min(maxQuality, Math.Min(3, stage + 1 - chosen.Tier));
				if (chosen.Family == OfferingCatalog.Kind.Equipment && ceiling >= 2 && Random.NextDouble() < 0.4)
					quality = Random.Next(2, ceiling + 1);
				recipe.Items.Add(new Offering { Prefab = chosen.Prefab, Amount = OfferingCatalog.Quantity(chosen, stage, Random), Quality = quality });
			}
			rolls.Recipes.Add(recipe);
		}
		return rolls;
	}

	/// <summary> Validates capacity and physical occupancy before spending anything. </summary>
	internal static void Craft(Reliquary reliquary, int recipeIndex)
	{
		if (!LedgerStore.Ready()) throw new InvalidOperationException(LedgerStore.Error ?? "LifeTotems supports single-player worlds only.");
		var ledger = LedgerStore.Current;
		if (ledger.GameOver) throw new InvalidOperationException("This run has ended. Begin a new character and world.");
		if (ledger.Charges.Count >= 3) throw new InvalidOperationException("The world already holds three Life Totems.");
		var player = Player.m_localPlayer;
		if (!reliquary.CanUse(player)) throw new InvalidOperationException("Move closer to the Reliquary.");
		var slot = reliquary.FindFreeSocket();
		if (slot < 0) throw new InvalidOperationException("All sockets are occupied or obstructed. Clear the area around the Reliquary.");
		var offers = GetOffers(player);
		if (recipeIndex < 0 || recipeIndex >= offers.Count) throw new InvalidOperationException("Invalid ritual selection.");
		var selected = offers[recipeIndex];
		// Rebuilding a returned offering must not become a free way to reroll the other recipes.
		var nextRolls = selected.Recovery == null ? PersistenceJson.Serialize(Generate()) : player.m_customData[RollsKey];
		var charge = new LifeCharge
		{
			Reliquary = reliquary.Id, Slot = slot, Crafter = player.GetPlayerID(), Recipe = selected.Recipe,
			InventoryWidth = player.GetInventory().GetWidth(), InventoryHeight = player.GetInventory().GetHeight()
		};
		using (var payment = new InventoryPayment(player.GetInventory(), selected.Recipe))
		{
			charge.PaidInventory = payment.RefundData;
			ledger.Charges.Add(charge);
			if (selected.Recovery != null) selected.Recovery.Rebound = true;
			try { LedgerStore.Save(); }
			catch
			{
				ledger.Charges.Remove(charge);
				if (selected.Recovery != null) selected.Recovery.Rebound = false;
				throw;
			}
			player.m_customData[RollsKey] = nextRolls;
			payment.Commit();
		}
		PinnedRecipe.Clear();
		Reliquary.RefreshAll();
	}

	internal static void Reroll(Reliquary reliquary)
	{
		if (!LedgerStore.Ready() || LedgerStore.Current.GameOver || !reliquary.CanUse(Player.m_localPlayer))
			throw new InvalidOperationException("The Reliquary cannot be used now.");
		if (GetOffers(Player.m_localPlayer).All(o => o.Recovery != null))
			throw new InvalidOperationException("There are no visible random offerings to reroll.");
		var next = PersistenceJson.Serialize(Generate());
		var cost = new RitualRecipe { Items = [new Offering { Prefab = "Coins", Amount = 300 }] };
		using (var payment = new InventoryPayment(Player.m_localPlayer.GetInventory(), cost))
		{
			Player.m_localPlayer.m_customData[RollsKey] = next;
			payment.Commit();
		}
		PinnedRecipe.Clear();
	}

	/// <summary> Explicit one-shot debug reset; preserve invalid input and leave lives/recoveries untouched. </summary>
	internal static void ResetSavedRecipes(Player player)
	{
		var replacement = PersistenceJson.Serialize(Generate());
		var roundTrip = PersistenceJson.Deserialize<RitualRolls>(replacement);
		if (roundTrip?.Recipes?.Count != 3 || roundTrip.Recipes.Any(r => r.Items?.Count != 6))
			throw new InvalidOperationException("Recipe serialization verification failed; saved recipes were not changed.");
		if (player.m_customData.TryGetValue(RollsKey, out var previous))
			player.m_customData[RollsKey + ".Backup." + DateTime.UtcNow.Ticks] = previous;
		player.m_customData[RollsKey] = replacement;
		PinnedRecipe.Clear();
	}
}
