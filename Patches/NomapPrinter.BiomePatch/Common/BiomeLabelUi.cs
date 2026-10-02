using TMPro;
using UnityEngine;

namespace Mugnum.ValheimMods.NomapPrinter.BiomePatch.Common;

/// <summary>
/// Displays the player's current biome inside the baked map window.
/// </summary>
internal static class BiomeLabelUi
{
	/// <summary>
	/// Baked map window owning the label.
	/// </summary>
	private static GameObject MapRoot;

	/// <summary>
	/// Biome name label.
	/// </summary>
	private static TMP_Text Label;

	/// <summary>
	/// Refreshes the label while the baked map is visible.
	/// </summary>
	/// <param name="mapRoot"> Baked map window root. </param>
	internal static void Refresh(GameObject mapRoot)
	{
		if (MapRoot != mapRoot)
		{
			Reset();
			MapRoot = mapRoot;
		}

		var player = Player.m_localPlayer;
		var minimap = Minimap.instance;

		if (!mapRoot
			|| !mapRoot.activeInHierarchy
			|| !Game.m_noMap
			|| !player
			|| !minimap
			|| minimap.m_mode == Minimap.MapMode.Large
			|| player.GetCurrentBiome() == Heightmap.Biome.None)
		{
			Hide();
			return;
		}

		if (!Label && !CreateLabel(minimap.m_biomeNameLarge))
		{
			return;
		}

		// Use the same localized biome data as vanilla's player biome display.
		// The cursor and baked texture have no part in this lookup.
		var biomeName = player.GetCurrentBiomeData().GetName();

		if (Label.text != biomeName)
		{
			Label.text = biomeName;
		}

		Label.gameObject.SetActive(true);
	}

	/// <summary>
	/// Creates a fixed label using the vanilla fullscreen map's text styling.
	/// </summary>
	/// <param name="source"> Vanilla biome label used as a style reference. </param>
	/// <returns> Was the label created. </returns>
	private static bool CreateLabel(TMP_Text source)
	{
		if (!source)
		{
			return false;
		}

		var root = new GameObject("NomapPrinter_BiomeLabel", typeof(RectTransform))
		{
			layer = MapRoot.layer
		};
		root.SetActive(false);
		root.transform.SetParent(MapRoot.transform, false);

		var rectTransform = (RectTransform)root.transform;
		rectTransform.anchorMin = Vector2.one;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.pivot = Vector2.one;
		rectTransform.anchoredPosition = new Vector2(-170f, -120f);
		rectTransform.sizeDelta = new Vector2(500f, 64f);

		// Parent to the window, outside the scrolling and zooming map content.
		Label = root.AddComponent<TextMeshProUGUI>();
		Label.font = source.font;
		Label.fontSharedMaterial = source.fontSharedMaterial;
		Label.fontSize = source.fontSize;
		Label.fontStyle = source.fontStyle;
		Label.color = source.color;
		Label.alignment = TextAlignmentOptions.TopRight;
		Label.enableAutoSizing = true;
		Label.fontSizeMin = Mathf.Min(18f, source.fontSize);
		Label.fontSizeMax = source.fontSize;
		Label.overflowMode = TextOverflowModes.Ellipsis;
		Label.raycastTarget = false;
		Label.text = string.Empty;

		return true;
	}

	/// <summary>
	/// Hides an existing label.
	/// </summary>
	private static void Hide()
	{
		if (Label)
		{
			Label.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Removes the label when its window changes or the plugin unloads.
	/// </summary>
	internal static void Reset()
	{
		if (Label)
		{
			Label.gameObject.SetActive(false);
			Object.Destroy(Label.gameObject);
		}

		Label = null;
		MapRoot = null;
	}
}
