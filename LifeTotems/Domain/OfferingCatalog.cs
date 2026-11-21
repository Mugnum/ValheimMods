using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.LifeTotems.Domain;

/// <summary> Curated renewable items; quantities and weights are shared by resource families. </summary>
internal static class OfferingCatalog
{
	internal enum Kind { Bulk, Common, Food, Metal, Rare, Trophy, Equipment }
	internal sealed class Entry(string prefab, int tier, Kind kind)
	{
		public readonly string Prefab = prefab;
		public readonly int Tier = tier;
		public readonly Kind Family = kind;
	}

	/// <summary> No boss trophies, keys, quest objects, treasure containers or irreplaceable items. </summary>
	internal static readonly List<Entry> Entries = Build();

	private static List<Entry> Build()
	{
		var result = new List<Entry>();
		void Add(int tier, Kind family, string names) => result.AddRange(names.Split(' ').Select(name => new Entry(name, tier, family)));
		Add(0, Kind.Bulk, "Wood Stone");
		Add(0, Kind.Common, "Resin Flint LeatherScraps DeerHide Feathers");
		Add(0, Kind.Food, "Mushroom Raspberry Honey NeckTail RawMeat DeerMeat");
		Add(0, Kind.Trophy, "TrophyBoar TrophyDeer TrophyNeck");
		Add(0, Kind.Equipment, "ShieldWood ShieldWoodTower Bow Club KnifeFlint SpearFlint");
		Add(1, Kind.Bulk, "RoundLog FineWood");
		Add(1, Kind.Common, "BoneFragments GreydwarfEye TrollHide Thistle BronzeNails");
		Add(1, Kind.Food, "MushroomYellow Blueberries Carrot CarrotSeeds");
		Add(1, Kind.Metal, "CopperOre TinOre Copper Tin Bronze");
		Add(1, Kind.Rare, "SurtlingCore");
		Add(1, Kind.Trophy, "TrophyGreydwarf TrophySkeleton TrophyTroll");
		Add(1, Kind.Equipment, "AxeBronze ShieldBronzeBuckler BowFineWood SwordBronze");
		Add(2, Kind.Bulk, "ElderBark");
		Add(2, Kind.Common, "Entrails Bloodbag Ooze");
		Add(2, Kind.Metal, "IronScrap Iron");
		Add(2, Kind.Rare, "Chain Guck");
		Add(2, Kind.Food, "Turnip TurnipSeeds");
		Add(2, Kind.Trophy, "TrophyDraugr TrophyLeech TrophyBlob");
		Add(2, Kind.Equipment, "ShieldIronBuckler SwordIron MaceIron BowHuntsman");
		Add(3, Kind.Common, "WolfPelt WolfFang FreezeGland Obsidian");
		Add(3, Kind.Metal, "SilverOre Silver");
		Add(3, Kind.Food, "WolfMeat Onion OnionSeeds");
		Add(3, Kind.Trophy, "TrophyWolf TrophyHatchling");
		Add(3, Kind.Equipment, "ShieldSilver SwordSilver SpearWolfFang");
		Add(4, Kind.Common, "LoxPelt Needle LinenThread");
		Add(4, Kind.Metal, "BlackMetalScrap BlackMetal");
		Add(4, Kind.Food, "Cloudberry Barley Flax LoxMeat");
		Add(4, Kind.Trophy, "TrophyGoblin TrophyLox TrophyDeathsquito");
		Add(4, Kind.Equipment, "ShieldBlackmetal KnifeBlackMetal SwordBlackmetal");
		Add(5, Kind.Bulk, "BlackMarble YggdrasilWood");
		Add(5, Kind.Rare, "Softtissue Sap Eitr");
		Add(5, Kind.Common, "Carapace ScaleHide");
		Add(5, Kind.Food, "MushroomJotunPuffs MushroomMagecap");
		Add(6, Kind.Common, "Ashwood Grausten CharredBone");
		Add(6, Kind.Metal, "FlametalNew FlametalOreNew");
		Add(6, Kind.Food, "MorgenSinew Vineberry");
		Add(0, Kind.Common, "Coins");
		return result;
	}

	/// <summary> Weighted sampling favors resources over equipment without prescribing easy/hard rows. </summary>
	internal static double Weight(Kind family) => family switch
	{
		Kind.Equipment => 0.18,
		Kind.Rare => 0.45,
		Kind.Trophy => 0.6,
		_ => 1.0
	};

	/// <summary> Coins are an exceptional offering, not a common early-game resource. </summary>
	internal static double Weight(Entry entry) => Weight(entry.Family) * (entry.Prefab == "Coins" ? 0.03 : 1.0);

	/// <summary> Triangular ranges put their mode above the minimum, while still allowing a one-item roll. </summary>
	internal static int Quantity(Entry entry, int stage, Random random)
	{
		var (min, mode, max) = entry.Family switch
		{
			Kind.Bulk => (1, 28, 65),
			Kind.Common when entry.Prefab == "Coins" => (1, 170, 350),
			Kind.Common => (1, 14, 35),
			Kind.Food => (1, 12, 32),
			Kind.Metal => (1, 6, 15),
			Kind.Rare => (1, 3, 8),
			Kind.Trophy => (1, 4, 12),
			_ => (1, 1, 3)
		};
		var scale = entry.Family == Kind.Equipment ? 1.0 : 0.7 + 0.18 * stage + 0.12 * Math.Max(0, stage - entry.Tier);
		var u = random.NextDouble();
		var split = (double)(mode - min) / (max - min);
		var value = u < split
			? min + Math.Sqrt(u * (max - min) * (mode - min))
			: max - Math.Sqrt((1 - u) * (max - min) * (max - mode));
		return Math.Max(1, (int)Math.Round(value * scale));
	}
}
