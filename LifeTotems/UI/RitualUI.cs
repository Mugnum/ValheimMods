using Jotunn.Managers;
using Mugnum.ValheimMods.LifeTotems.Domain;
using Mugnum.ValheimMods.LifeTotems.Services;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mugnum.ValheimMods.LifeTotems.UI;

/// <summary> Shared native fonts, item tooltips and inventory-aware ingredient cells. </summary>
internal static class RitualUI
{
	internal static readonly Vector2 TopLeft = new(0, 1);
	internal static readonly Color Panel = new(0.19f, 0.18f, 0.16f, 0.98f);
	internal static readonly Color Enough = new(0.23f, 0.25f, 0.20f, 1);
	internal static readonly Color Missing = new(0.29f, 0.22f, 0.18f, 1);
	internal static readonly Color Gray = new(0.39f, 0.38f, 0.36f, 1);
	internal static readonly Color Bright = new(0.78f, 0.76f, 0.71f, 1);
	internal static float Scale => Mathf.Clamp(Screen.height / 1440f, 0.6f, 1.5f);
	internal static void ScaleToPixels(Transform root, float scale)
		=> root.localScale = Vector3.one * (scale / Mathf.Max(0.001f, root.parent.lossyScale.x));

	internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
		var rect = (RectTransform)go.transform;
		rect.anchorMin = rect.anchorMax = rect.pivot = TopLeft;
		rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
		return rect;
	}
	internal static Text Label(Transform parent, string text, float x, float y, float width, float height, int size)
	{
		var go = GUIManager.Instance.CreateText(text, parent, TopLeft, TopLeft, new Vector2(x, -y), GUIManager.Instance.AveriaSerif,
			size, GUIManager.Instance.ValheimBeige, true, Color.black, width, height, false);
		var label = go.GetComponent<Text>();
		label.rectTransform.pivot = TopLeft; label.alignment = TextAnchor.MiddleLeft;
		label.raycastTarget = false; return label;
	}
	internal static Button Button(Transform parent, string text, float x, float y, float width, float height, Action clicked, bool silent = false)
	{
		var go = GUIManager.Instance.CreateButton(text, parent, TopLeft, TopLeft, new Vector2(x, -y), width, height);
		((RectTransform)go.transform).pivot = TopLeft;
		foreach (var label in go.GetComponentsInChildren<Text>(true)) label.fontSize = 22;
		// OnDisable unregisters ButtonSfx's click listener and suppresses its selection sound.
		if (silent) foreach (var sound in go.GetComponentsInChildren<ButtonSfx>(true)) sound.enabled = false;
		var button = go.GetComponent<Button>(); button.onClick.AddListener(() => clicked()); return button;
	}
	internal static Action Cell(Transform parent, Offering cost, float x, float y, float width, float height, bool compact = false)
	{
		var cell = Rect(parent, cost.Prefab, x, y, width, height);
		var background = cell.gameObject.AddComponent<Image>();
		var prefab = ObjectDB.instance.GetItemPrefab(cost.Prefab);
		var item = prefab ? prefab.GetComponent<ItemDrop>()?.m_itemData : null;
		var iconSize = compact ? 30 : 48;
		if (item != null)
		{
			var icon = Rect(cell, "Icon", (width - iconSize) / 2, compact ? 2 : 8, iconSize, iconSize).gameObject.AddComponent<Image>();
			icon.sprite = item.GetIcon(); icon.preserveAspect = true; icon.raycastTarget = false;
			var template = InventoryGui.instance ? InventoryGui.instance.GetComponentsInChildren<UITooltip>(true).FirstOrDefault(t => t.m_tooltipPrefab) : null;
			if (template)
			{
				var tooltip = cell.gameObject.AddComponent<UITooltip>(); tooltip.m_tooltipPrefab = template.m_tooltipPrefab;
				tooltip.m_topic = Localization.instance.Localize(item.m_shared.m_name);
				tooltip.m_text = ItemDrop.ItemData.GetTooltip(item, cost.Quality, true, item.m_worldLevel)
					+ $"\nOffering: {cost.Amount}, quality {cost.Quality} or higher. Unequipped items only.";
			}
		}
		else Label(cell, cost.Prefab, 2, 0, width - 4, height / 2, compact ? 12 : 14);
		if (cost.Quality > 1) Label(cell, "★" + cost.Quality, width - 32, 0, 32, 20, 14);
		var count = Label(cell, "", 0, height - (compact ? 24 : 36), width, compact ? 24 : 36, compact ? 16 : 24);
		count.alignment = TextAnchor.MiddleCenter;
		return () =>
		{
			var have = InventoryPayment.Count(Player.m_localPlayer.GetInventory(), cost);
			count.text = $"{have}/{cost.Amount}"; background.color = have >= cost.Amount ? Enough : Missing;
			count.color = have >= cost.Amount ? Color.Lerp(GUIManager.Instance.ValheimBeige, GUIManager.Instance.ValheimOrange, 0.35f) : GUIManager.Instance.ValheimBeige;
		};
	}
	internal static Action<float> MaterialBar(Transform parent, float x, float y, float width)
	{
		var track = Rect(parent, "Materials", x, y, width, 8);
		track.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.09f, 1);
		var fill = Rect(track, "Fill", 0, 0, 0, 8);
		fill.gameObject.AddComponent<Image>().color = Color.Lerp(GUIManager.Instance.ValheimOrange, GUIManager.Instance.ValheimBeige, 0.2f);
		return value => fill.sizeDelta = new Vector2(width * Mathf.Clamp01(value), 8);
	}
}
