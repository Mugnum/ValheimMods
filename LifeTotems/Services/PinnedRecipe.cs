using Mugnum.ValheimMods.LifeTotems.Domain;
using System.Linq;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> A session-only bookmark; it never saves a new roll or reveals a hidden offer. </summary>
internal static class PinnedRecipe
{
	internal static RitualRecipe Recipe { get; private set; }
	internal static string Key { get; private set; }
	private static Player Owner;
	private static long World;
	private static string Rolls, Recovery;

	internal static void Toggle(Player player, RitualService.OfferChoice offer)
	{
		var key = RitualPresentation.Key(offer);
		if (Owner == player && Key == key) { Clear(); return; }
		Owner = player; World = ZNet.instance.GetWorldUID();
		Key = key; Recipe = offer.Recipe; Recovery = offer.Recovery?.Id;
		player.m_customData.TryGetValue(RitualService.RollsKey, out Rolls);
	}

	internal static void Validate(Player player)
	{
		if (Recipe == null) return;
		if (!ZNet.instance || !ZNet.IsSinglePlayer || !player || player != Owner || ZNet.instance.GetWorldUID() != World)
		{ Clear(); return; }
		player.m_customData.TryGetValue(RitualService.RollsKey, out var rolls);
		if (Recovery == null && rolls != Rolls || Recovery != null && LedgerStore.Current != null
			&& !LedgerStore.Current.Recoveries.Any(r => r.Id == Recovery && r.Confirmed && !r.Rebound)) Clear();
	}

	internal static void Clear() { Recipe = null; Key = Rolls = Recovery = null; Owner = null; World = 0; }
}
