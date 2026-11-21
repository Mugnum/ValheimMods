using Jotunn.Managers;
using Mugnum.ValheimMods.LifeTotems.Services;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mugnum.ValheimMods.LifeTotems.UI;

/// <summary> Passive during play; clicking the bookmark with a vanilla menu cursor removes it. </summary>
internal static class PinnedRecipeHud
{
	private static GameObject Root;
	private static CanvasGroup Input;
	private static readonly List<Action> Refreshers = [];
	private static string ShownKey;
	private static float NextRefresh;
	private static int ScreenHeight;
	private static readonly Vector3[] Corners = new Vector3[4];

	internal static void Tick()
	{
		PinnedRecipe.Validate(Player.m_localPlayer);
		if (PinnedRecipe.Recipe == null) { Destroy(); return; }
		if (!Hud.instance || !GUIManager.CustomGUIFront) { if (Root) Root.SetActive(false); return; }
		if (!Root || ShownKey != PinnedRecipe.Key || Screen.height != ScreenHeight) Build();
		var visible = Player.m_localPlayer && !Player.m_localPlayer.IsDead() && Hud.instance;
		Root.SetActive(visible);
		// No cursor capture or input blocking: the vanilla menus already make the mouse available.
		var clickable = InventoryGui.IsVisible() || Hud.IsPieceSelectionVisible() || ReliquaryWindow.IsOpen;
		Input.blocksRaycasts = Input.interactable = clickable;
		if (!visible || Time.unscaledTime < NextRefresh) return;
		NextRefresh = Time.unscaledTime + .3f;
		PositionAboveVitals();
		foreach (var refresh in Refreshers) refresh();
	}

	private static void Build()
	{
		Destroy();
		var recipe = PinnedRecipe.Recipe;
		var rows = Mathf.CeilToInt(recipe.Items.Count / 6f);
		var rect = RitualUI.Rect(GUIManager.CustomGUIFront.transform, "LifeTotems_PinnedRecipe", 24, 24, 380, 48 + rows * 66);
		Root = rect.gameObject; RitualUI.ScaleToPixels(rect, Mathf.Max(.75f, RitualUI.Scale));
		rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
		var background = Root.AddComponent<Image>(); background.color = new Color(.15f, .14f, .12f, .94f);
		Input = Root.AddComponent<CanvasGroup>();
		var button = Root.AddComponent<Button>(); button.targetGraphic = background;
		var colors = button.colors; colors.highlightedColor = new Color(1.4f, 1.25f, 1.1f, 1); button.colors = colors;
		button.onClick.AddListener(() => { PinnedRecipe.Clear(); Destroy(); });
		RitualUI.Label(rect, "Life Totem", 12, 6, 320, 28, 22);
		var hint = RitualUI.Label(rect, "×", 344, 6, 24, 28, 22); hint.alignment = TextAnchor.MiddleCenter;
		for (var n = 0; n < recipe.Items.Count; n++)
			Refreshers.Add(RitualUI.Cell(rect, recipe.Items[n], 12 + n % 6 * 60, 42 + n / 6 * 66, 54, 60, true));
		ShownKey = PinnedRecipe.Key; ScreenHeight = Screen.height;
		PositionAboveVitals();
		foreach (var refresh in Refreshers) refresh();
	}

	/// <summary> Reserve the actual health/food footprint, including HUD mods that move or rotate it. </summary>
	private static void PositionAboveVitals()
	{
		var rect = (RectTransform)Root.transform;
		var scale = Mathf.Max(.75f, RitualUI.Scale);
		var top = 280 * scale;
		void Include(RectTransform vital)
		{
			if (!vital) return;
			var canvas = vital.GetComponentInParent<Canvas>();
			var camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
			vital.GetWorldCorners(Corners);
			foreach (var corner in Corners) top = Mathf.Max(top, RectTransformUtility.WorldToScreenPoint(camera, corner).y);
		}
		Include(Hud.instance.m_healthBarRoot);
		Include(Hud.instance.m_foodBarRoot);
		foreach (var food in Hud.instance.m_foodIcons) if (food) Include(food.rectTransform);
		var bottom = Mathf.Clamp(top + 32 * scale, 0, Mathf.Max(0, Screen.height - rect.rect.height * scale - 24 * scale));
		var parentScale = rect.parent.lossyScale;
		rect.anchoredPosition = new Vector2(24 / Mathf.Max(.001f, parentScale.x), bottom / Mathf.Max(.001f, parentScale.y));
	}

	internal static void Destroy()
	{
		if (Root) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); }
		Root = null; Input = null; ShownKey = null; Refreshers.Clear();
	}
}
