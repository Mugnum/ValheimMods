using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mugnum.ValheimMods.MassCheckStacking.Common;

/// <summary>
/// Classifies items using recipes and station conversions, with no per-frame item scans.
/// </summary>
internal static class ItemCatalog
{
	/// <summary>
	/// Names of food produced through crafting, cooking or baking.
	/// </summary>
	private static readonly HashSet<string> PreparedFood = new(StringComparer.Ordinal);

	/// <summary>
	/// Names of ingredients used for food preparation.
	/// </summary>
	private static readonly HashSet<string> RawFood = new(StringComparer.Ordinal);

	/// <summary>
	/// Names of fermentation inputs and outputs.
	/// </summary>
	private static readonly HashSet<string> Meads = new(StringComparer.Ordinal);

	/// <summary>
	/// Names of unprocessed smelting inputs.
	/// </summary>
	private static readonly HashSet<string> Ore = new(StringComparer.Ordinal);

	/// <summary>
	/// Database used for the current classification cache.
	/// </summary>
	private static ObjectDB Database;

	/// <summary>
	/// Scene used for the current station conversion cache.
	/// </summary>
	private static ZNetScene Scene;

	/// <summary>
	/// Recipe count when the cache was last built.
	/// </summary>
	private static int RecipeCount;

	/// <summary>
	/// Prefab count when the cache was last built.
	/// </summary>
	private static int PrefabCount;

	/// <summary>
	/// Ordered category rules. New categories can be added without changing transfer code.
	/// </summary>
	private static readonly (ItemCategory Category, Func<ItemDrop.ItemData, bool> Matches)[] Rules =
	[
		(ItemCategory.UnprocessedOre, item => Ore.Contains(item.m_shared.m_name)),
		(ItemCategory.Arrows, item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo),
		(ItemCategory.Meads, item => Meads.Contains(item.m_shared.m_name)
			|| (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
				&& item.m_shared.m_consumeStatusEffect && !IsFood(item))),
		(ItemCategory.CookedFood, item => PreparedFood.Contains(item.m_shared.m_name)),
		(ItemCategory.RawFood, item => RawFood.Contains(item.m_shared.m_name)
			|| IsFood(item) || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Fish)
	];

	/// <summary>
	/// Refreshes category data at batch start if the world's item database changed.
	/// </summary>
	internal static void Refresh()
	{
		var database = ObjectDB.instance;
		var scene = ZNetScene.instance;

		if (!database || !scene
			|| (database == Database && scene == Scene
				&& database.m_recipes.Count == RecipeCount && scene.m_prefabs.Count == PrefabCount))
		{
			return;
		}

		PreparedFood.Clear();
		RawFood.Clear();
		Meads.Clear();
		Ore.Clear();

		foreach (var prefab in scene.m_prefabs)
		{
			if (!prefab)
			{
				continue;
			}

			var cooking = prefab.GetComponent<CookingStation>();
			if (cooking)
			{
				foreach (var conversion in cooking.m_conversion)
				{
					AddFoodConversion(conversion.m_from, conversion.m_to);
				}
			}

			var fermenter = prefab.GetComponent<Fermenter>();
			if (fermenter)
			{
				foreach (var conversion in fermenter.m_conversion)
				{
					Add(Meads, conversion.m_from);
					Add(Meads, conversion.m_to);
				}
			}

			var smelter = prefab.GetComponent<Smelter>();
			if (smelter)
			{
				foreach (var conversion in smelter.m_conversion)
				{
					AddFoodConversion(conversion.m_from, conversion.m_to);
					if (IsOreConversion(conversion.m_from, conversion.m_to))
					{
						Add(Ore, conversion.m_from);
					}
				}
			}
		}

		foreach (var recipe in database.m_recipes)
		{
			if (!recipe || !recipe.m_item
				|| (!IsFood(recipe.m_item.m_itemData) && !RawFood.Contains(recipe.m_item.m_itemData.m_shared.m_name)))
			{
				continue;
			}

			Add(PreparedFood, recipe.m_item);
			foreach (var resource in recipe.m_resources)
			{
				Add(RawFood, resource.m_resItem);
			}
		}

		Database = database;
		Scene = scene;
		RecipeCount = database.m_recipes.Count;
		PrefabCount = scene.m_prefabs.Count;
	}

	/// <summary>
	/// Returns the first registered category matching the item.
	/// </summary>
	/// <param name="item"> Item to classify. </param>
	/// <returns> First matching category, or <see cref="ItemCategory.Other"/> when none matches. </returns>
	internal static ItemCategory Classify(ItemDrop.ItemData item)
	{
		foreach (var rule in Rules)
		{
			if (rule.Matches(item))
			{
				return rule.Category;
			}
		}

		return ItemCategory.Other;
	}

	/// <summary>
	/// Recognizes edible food, including stamina-only or eitr-only variants.
	/// </summary>
	/// <param name="item"> Item to check. </param>
	/// <returns> Does the item provide health, stamina or eitr as food. </returns>
	private static bool IsFood(ItemDrop.ItemData item)
	{
		return item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f;
	}

	/// <summary>
	/// Registers cooked output and its uncooked input.
	/// </summary>
	/// <param name="input"> Ingredient converted into food. </param>
	/// <param name="output"> Conversion result, registered only when it provides food stats. </param>
	private static void AddFoodConversion(ItemDrop input, ItemDrop output)
	{
		if (output && IsFood(output.m_itemData))
		{
			Add(PreparedFood, output);
			Add(RawFood, input);
		}
	}

	/// <summary>
	/// Distinguishes ore processing from kiln, windmill and spinning-wheel conversions.
	/// </summary>
	/// <param name="input"> Material consumed by the conversion. </param>
	/// <param name="output"> Material produced by the conversion. </param>
	/// <returns> Does the conversion identify its input as unprocessed ore or metal scrap. </returns>
	private static bool IsOreConversion(ItemDrop input, ItemDrop output)
	{
		if (!input || !output
			|| input.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Material
			|| output.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Material)
		{
			return false;
		}

		return (!input.m_itemData.m_shared.m_teleportable && !output.m_itemData.m_shared.m_teleportable)
			|| input.name.EndsWith("Ore", StringComparison.OrdinalIgnoreCase)
			|| input.name.EndsWith("Scrap", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Registers an item's stable internal name.
	/// </summary>
	/// <param name="names"> Category name set to update. </param>
	/// <param name="item"> Item to register, or null to skip registration. </param>
	private static void Add(HashSet<string> names, ItemDrop item)
	{
		if (item)
		{
			names.Add(item.m_itemData.m_shared.m_name);
		}
	}
}
