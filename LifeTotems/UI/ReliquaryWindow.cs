using Jotunn.Managers;
using Mugnum.ValheimMods.LifeTotems.Services;
using Mugnum.ValheimMods.LifeTotems.World;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mugnum.ValheimMods.LifeTotems.UI;

/// <summary> Six-column offering groups, one cancellable build action, and independent recipe pins. </summary>
internal static class ReliquaryWindow
{
	private static GameObject Root;
	private static Reliquary Station;
	private static Text Header, ErrorText, BuildText;
	private static Button BuildButton, RerollButton;
	private static GuiBar CraftBar;
	private static readonly List<Action> Refreshers = [];
	private static List<RitualService.OfferChoice> Offers;
	private static int Selected = -1, Pending = -1, ScreenWidth, ScreenHeight;
	private static string OffersKey, PendingKey;
	private static float NextRefresh, StartedAt, Duration;
	internal static bool IsOpen => Root;

	internal static void Open(Reliquary station)
	{
		Close(); Station = station;
		if (!LedgerStore.Ready()) { Player.m_localPlayer.Message(MessageHud.MessageType.Center, LedgerStore.Error ?? "World state is not ready."); return; }
		try { Build(); GUIManager.BlockInput(true); }
		catch (Exception ex) { Close(); Plugin.Log.LogError(ex); Player.m_localPlayer.Message(MessageHud.MessageType.Center, ex.Message); }
	}
	private static string Snapshot(List<RitualService.OfferChoice> offers) => string.Join("|", offers.Select(RitualPresentation.Key));
	private static bool CanBuild(int index) => index >= 0 && index < Offers.Count && !LedgerStore.Current.GameOver
		&& InventoryPayment.CanPay(Player.m_localPlayer.GetInventory(), Offers[index].Recipe);

	private static void Build()
	{
		Offers = RitualService.GetOffers(Player.m_localPlayer); OffersKey = Snapshot(Offers);
		var order = RitualPresentation.Order(Offers);
		Selected = order.Where(CanBuild).DefaultIfEmpty(-1).First();
		ScreenWidth = Screen.width; ScreenHeight = Screen.height;
		var scale = RitualUI.Scale;
		var width = Mathf.Min(900, Screen.width * 0.94f / scale);
		var cardWidth = width - 48;
		var contentHeight = order.Sum(i => 62 + Mathf.CeilToInt(Offers[i].Recipe.Items.Count / 6f) * 112 + 16) - (Offers.Count > 0 ? 16 : 0);
		var height = Mathf.Min(Screen.height * 0.88f / scale, Mathf.Max(420, contentHeight + 190));
		Root = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, width, height, false);
		RitualUI.ScaleToPixels(Root.transform, scale);
		Header = RitualUI.Label(Root.transform, "Reliquary", 24, 18, width - 110, 48, 32);
		RitualUI.Button(Root.transform, "×", width - 70, 20, 44, 44, Close);
		var viewport = RitualUI.Rect(Root.transform, "Recipes", 24, 90, cardWidth, height - 190);
		viewport.gameObject.AddComponent<RectMask2D>();
		viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.08f);
		if (Offers.Count == 0)
		{
			var empty = RitualUI.Label(viewport, "No recipes available", 0, 0, cardWidth, height - 190, 26);
			empty.alignment = TextAnchor.MiddleCenter; Refresh(); return;
		}
		var content = RitualUI.Rect(viewport, "Groups", 0, 0, cardWidth, contentHeight);
		var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
		scroll.horizontal = false; scroll.vertical = contentHeight > viewport.rect.height;
		scroll.scrollSensitivity = 40; scroll.movementType = ScrollRect.MovementType.Clamped;
		var y = 0f;
		for (var position = 0; position < order.Length; position++)
		{
			var index = order[position]; var offer = Offers[index]; var recipe = offer.Recipe;
			var rows = Mathf.CeilToInt(recipe.Items.Count / 6f); var groupHeight = 62 + rows * 112;
			var card = RitualUI.Rect(content, "Recipe_" + index, 0, y, cardWidth, groupHeight);
			var background = card.gameObject.AddComponent<Image>(); background.color = RitualUI.Panel;
			var outline = card.gameObject.AddComponent<Outline>(); outline.effectDistance = new Vector2(2, -2);
			var select = card.gameObject.AddComponent<Button>(); select.targetGraphic = background; select.transition = Selectable.Transition.None;
			select.onClick.AddListener(() => { if (Pending < 0 && CanBuild(index)) { Selected = index; Refresh(); } });
			RitualUI.Label(card, "#" + (position + 1), 10, 12, 64, 50, 38);
			var pin = RitualUI.Button(card, "Pin", 18, 72, 44, 40, () => { PinnedRecipe.Toggle(Player.m_localPlayer, offer); Refresh(); }, silent: true);
			pin.transition = Selectable.Transition.None;
			foreach (var label in pin.GetComponentsInChildren<Text>(true)) label.fontSize = 16;
			var pinImage = pin.GetComponent<Image>();
			var gridX = 90f; var gridWidth = cardWidth - gridX - 16;
			var cellWidth = (gridWidth - 5 * 9) / 6;
			for (var n = 0; n < recipe.Items.Count; n++)
				Refreshers.Add(RitualUI.Cell(card, recipe.Items[n], gridX + n % 6 * (cellWidth + 9), 14 + n / 6 * 112, cellWidth, 104));
			var materialBar = RitualUI.MaterialBar(card, gridX, 24 + rows * 112, gridWidth);
			Refreshers.Add(() =>
			{
				var ready = CanBuild(index); select.interactable = Pending < 0 && ready; pin.interactable = Pending < 0;
				outline.effectColor = Selected == index ? GUIManager.Instance.ValheimOrange : ready ? RitualUI.Bright : RitualUI.Gray;
				if (pinImage) pinImage.color = PinnedRecipe.Key == RitualPresentation.Key(offer) ? new Color(.46f, .32f, .17f, 1) : Color.white;
				materialBar(RitualPresentation.Progress(Player.m_localPlayer.GetInventory(), recipe));
			});
			y += groupHeight + 16;
		}
		if (Offers.Any(o => o.Recovery == null))
			RerollButton = RitualUI.Button(Root.transform, "↻ 300 coins", 24, height - 76, 190, 46, () => Execute(() => RitualService.Reroll(Station)));
		BuildButton = RitualUI.Button(Root.transform, "Build", width - 162, height - 76, 138, 46, BuildOrCancel);
		BuildText = BuildButton.GetComponentInChildren<Text>();
		// Clone the widget, keeping InventoryGui's live crafting state entirely separate.
		if (InventoryGui.instance.m_craftProgressBar)
		{
			CraftBar = UnityEngine.Object.Instantiate(InventoryGui.instance.m_craftProgressBar, Root.transform, false);
			var rect = (RectTransform)CraftBar.transform; rect.anchorMin = rect.anchorMax = rect.pivot = RitualUI.TopLeft;
			rect.anchoredPosition = new Vector2(238, -(height - 60)); CraftBar.SetWidth(Mathf.Max(20, width - 424));
			CraftBar.SetMaxValue(1); CraftBar.SetValue(0);
			foreach (var graphic in CraftBar.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
			CraftBar.gameObject.SetActive(false);
		}
		ErrorText = RitualUI.Label(Root.transform, "", 24, height - 27, width - 48, 25, 16);
		Refresh();
	}
	private static void BuildOrCancel()
	{
		if (Pending >= 0) { Pending = -1; Refresh(); return; }
		if (!CanBuild(Selected)) return;
		Pending = Selected; PendingKey = RitualPresentation.Key(Offers[Selected]); StartedAt = Time.time;
		Duration = Mathf.Max(.1f, InventoryGui.instance.m_craftDuration); Refresh();
	}
	private static void Execute(Action action)
	{
		try { action(); var station = Station; Open(station); }
		catch (Exception ex) { Pending = -1; Plugin.Log.LogWarning(ex); Refresh(); if (ErrorText) ErrorText.text = ex.Message; }
	}
	private static void Refresh()
	{
		if (!Root || LedgerStore.Current == null) return;
		Header.text = "Reliquary    " + LedgerStore.Current.Charges.Count + " / 3";
		if (Pending < 0 && !CanBuild(Selected)) Selected = RitualPresentation.Order(Offers).Where(CanBuild).DefaultIfEmpty(-1).First();
		foreach (var refresh in Refreshers) refresh();
		if (BuildButton) BuildButton.interactable = Pending >= 0 || CanBuild(Selected);
		if (BuildText) BuildText.text = Pending >= 0 ? "Cancel" : "Build";
		if (CraftBar) CraftBar.gameObject.SetActive(Pending >= 0);
		if (RerollButton) RerollButton.interactable = Pending < 0 && !LedgerStore.Current.GameOver
			&& InventoryPayment.Count(Player.m_localPlayer.GetInventory(), new Domain.Offering { Prefab = "Coins" }) >= 300;
	}
	internal static void Tick()
	{
		if (!Root) return;
		if (!Station || !ZNet.IsSinglePlayer || !Station.CanUse(Player.m_localPlayer) || ZInput.GetKeyDown(KeyCode.Escape)) { Close(); return; }
		try
		{
			if (Time.unscaledTime >= NextRefresh)
			{
				NextRefresh = Time.unscaledTime + .3f;
				if (Screen.width != ScreenWidth || Screen.height != ScreenHeight || Snapshot(RitualService.GetOffers(Player.m_localPlayer)) != OffersKey)
				{ var station = Station; Open(station); return; }
				Refresh();
			}
			if (Pending < 0) return;
			if (!CanBuild(Pending))
			{
				Pending = -1; Refresh();
				if (ErrorText) ErrorText.text = "Required materials are no longer available.";
				return;
			}
			var elapsed = Time.time - StartedAt;
			if (CraftBar) CraftBar.SetValue(Mathf.Clamp01(elapsed / Duration));
			if (elapsed < Duration) return;
			// Verify the original recipe before spending: offer indices may shift while the menu is open.
			var current = RitualService.GetOffers(Player.m_localPlayer);
			if (Pending >= current.Count || RitualPresentation.Key(current[Pending]) != PendingKey)
				throw new InvalidOperationException("The offerings changed. Select a recipe again.");
			var index = Pending; Pending = -1; Execute(() => RitualService.Craft(Station, index));
		}
		catch (Exception ex) { Pending = -1; Plugin.Log.LogWarning(ex); Refresh(); if (ErrorText) ErrorText.text = ex.Message; }
	}
	internal static void Close()
	{
		if (Root) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); GUIManager.BlockInput(false); }
		Root = null; Station = null; Selected = Pending = -1; Offers = null;
		BuildButton = RerollButton = null; CraftBar = null; BuildText = ErrorText = Header = null; Refreshers.Clear();
	}
}
