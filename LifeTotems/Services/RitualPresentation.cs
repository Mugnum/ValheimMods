using Mugnum.ValheimMods.LifeTotems.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Display order never changes the underlying offer indices or hidden random rolls. </summary>
internal static class RitualPresentation
{
	internal static int[] Order(IReadOnlyList<RitualService.OfferChoice> offers) => Enumerable.Range(0, offers.Count)
		.OrderBy(i => offers[i].Recipe.Items.Max(c => Entry(c)?.Tier ?? int.MaxValue))
		.ThenBy(i => offers[i].Recipe.Items.Max(c => Rarity(Entry(c)?.Family)))
		.ThenBy(i => offers[i].Recipe.Items.Sum(c => (long)c.Amount)).ToArray();

	private static OfferingCatalog.Entry Entry(Offering cost) => OfferingCatalog.Entries.FirstOrDefault(e => e.Prefab == cost.Prefab);
	private static int Rarity(OfferingCatalog.Kind? kind) => kind == OfferingCatalog.Kind.Equipment ? 3
		: kind == OfferingCatalog.Kind.Rare ? 2 : kind == OfferingCatalog.Kind.Trophy ? 1 : 0;
	internal static string Key(RitualService.OfferChoice offer) => (offer.Recovery?.Id ?? "random") + ":" + PersistenceJson.Serialize(offer.Recipe);
	internal static float Progress(Inventory inventory, RitualRecipe recipe)
	{
		var total = recipe.Items.Sum(c => (long)c.Amount);
		return total == 0 ? 0 : (float)(recipe.Items.Sum(c => (long)Math.Min(c.Amount, InventoryPayment.Count(inventory, c))) / (double)total);
	}
}
