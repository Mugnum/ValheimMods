using HarmonyLib;
using Mugnum.ValheimMods.LifeTotems.Domain;
using Mugnum.ValheimMods.LifeTotems.UI;
using Mugnum.ValheimMods.LifeTotems.World;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Captures death decisions before vanilla removes status effects and resets its grace timer. </summary>
internal static class DeathService
{
	internal sealed class Context
	{
		internal Player Player;
		internal Inventory Inventory;
		internal readonly List<(ItemDrop.ItemData Item, bool Equipped)> Retained = [];
	}
	private static readonly AccessTools.FieldRef<Player, float> TimeSinceDeath = AccessTools.FieldRefAccess<Player, float>("m_timeSinceDeath");
	private static readonly System.Random Random = new();
	internal static Context Active;
	private static Player ObservedPlayer;
	private static float NoticeAt;

	internal static Context Begin(Player player)
	{
		if (!Plugin.IsActive || !ReliquaryPrefabs.IsRegistered || !ZNet.IsSinglePlayer || player != Player.m_localPlayer || !player.IsOwner() || player.IsDead() || Active != null) return null;
		ReliquaryWindow.Close();
		if (!LedgerStore.Ready()) { Plugin.Log.LogError(LedgerStore.Error ?? "LifeTotems world state unavailable; using existing death behavior."); return null; }
		var ledger = LedgerStore.Current;
		var grace = Plugin.GracePeriod.Value && (player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectSoftDeath)
			|| TimeSinceDeath(player) <= player.m_hardDeathCooldown);
		var before = new List<LifeCharge>(ledger.Charges);
		var endedBefore = ledger.GameOver;
		string message;
		if (grace)
			message = $"No skill drain: no Life Totem consumed.\n{ledger.Charges.Count} Life Totems remain. Your belongings are in your tombstone.";
		else
		{
			var saved = ledger.Consume(Random);
			try { LedgerStore.Save(); }
			catch (Exception ex)
			{
				ledger.Charges = before; ledger.GameOver = endedBefore;
				Plugin.Log.LogError($"LifeTotems could not record this death. Existing death behavior is preserved.\n{ex}");
				return null;
			}
			message = saved
				? $"A Life Totem has shattered.\n{ledger.Charges.Count} Life Totems remain." + (ledger.Charges.Count == 0 ? " Bind another before your next death." : "")
				: "Your last chance has passed.\nGAME OVER — begin a new character and world.";
		}
		player.m_customData[RitualService.NoticeKey] = message;
		Reliquary.RefreshAll();
		var context = new Context { Player = player, Inventory = player.GetInventory() };
		Active = context;
		// Hardcore and explicit keep-inventory world modifiers remain vanilla until a policy is designed for them.
		var rules = ZoneSystem.instance;
		var specialWorldRules = rules.GetGlobalKey(GlobalKeys.DeathDeleteItems) || rules.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped)
			|| rules.GetGlobalKey(GlobalKeys.DeathKeepInventory) || rules.GetGlobalKey(GlobalKeys.DeathKeepEquip);
		if (!grace && Plugin.KeepEquipment.Value && !specialWorldRules)
		{
			var retained = context.Inventory.GetAllItems().Where(ShouldRetain).Select(item => (Item: item, Equipped: item.m_equipped)).ToArray();
			try
			{
				foreach (var entry in retained)
				{
					// Record before removal, so a throwing inventory callback cannot orphan the item.
					context.Retained.Add(entry);
					if (!context.Inventory.RemoveItem(entry.Item)) context.Retained.Remove(entry);
				}
			}
			catch { End(context); throw; }
		}
		return context;
	}

	/// <summary> Keep personal supplies by type, never cargo that cannot normally be teleported. </summary>
	internal static bool ShouldRetain(ItemDrop.ItemData item) => item.m_shared.m_teleportable
		&& (item.IsEquipable() || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable
			|| item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || item.m_shared.m_questItem);

	/// <summary> Runs even if another death hook throws; preserve exact items, quality, durability, custom data and grid cells. </summary>
	internal static void End(Context context)
	{
		if (context == null) return;
		try
		{
			foreach (var retained in context.Retained)
			{
				if (!context.Inventory.GetAllItems().Contains(retained.Item)) context.Inventory.GetAllItems().Add(retained.Item);
				retained.Item.m_equipped = retained.Equipped;
			}
			InventoryPayment.NotifyChanged(context.Inventory, false, false);
		}
		finally { Active = null; }
	}

	internal static void Tick()
	{
		var player = Player.m_localPlayer;
		if (player != ObservedPlayer) { ObservedPlayer = player; NoticeAt = Time.unscaledTime + 3f; }
		if (!player || !ZNet.IsSinglePlayer || player.IsDead() || player.IsTeleporting() || player.InIntro() || Time.unscaledTime < NoticeAt) return;
		var key = RitualService.NoticeKey;
		if (player.m_customData.TryGetValue(key, out var message))
		{
			player.Message(MessageHud.MessageType.Center, message);
			player.m_customData.Remove(key);
		}
	}
}
