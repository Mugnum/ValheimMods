using UnityEngine;
using UnityEngine.UI;

namespace Mugnum.ValheimMods.ComfortHints.Common;

/// <summary>
/// Displays comfort improvement marker on a build menu cell.
/// </summary>
internal sealed class ComfortHintMarker : MonoBehaviour
{
	/// <summary>
	/// Marker root.
	/// </summary>
	private GameObject _root;

	/// <summary>
	/// Primary chevron.
	/// </summary>
	private RectTransform _primaryChevron;

	/// <summary>
	/// Secondary chevron.
	/// </summary>
	private RectTransform _secondaryChevron;

	/// <summary>
	/// Whether the marker root is active in the UI hierarchy.
	/// </summary>
	internal bool IsActive => _root != null && _root.activeInHierarchy;

	/// <summary>
	/// Sets displayed comfort improvement.
	/// </summary>
	/// <param name="gain"> Comfort level increase. </param>
	internal void SetGain(int gain)
	{
		EnsureCreated();

		if (gain <= 0)
		{
			_root.SetActive(false);
			return;
		}

		_root.SetActive(true);
		var isDoubleChevron = gain >= 2;
		var iconPosition = isDoubleChevron
			? 3f
			: 0f;

		_primaryChevron.anchoredPosition = new Vector2(0f, iconPosition);
		_secondaryChevron.anchoredPosition = new Vector2(0f, -3f);
		_secondaryChevron.gameObject.SetActive(isDoubleChevron);
		_root.transform.SetAsLastSibling();
	}

	/// <summary>
	/// Creates marker objects when needed.
	/// </summary>
	private void EnsureCreated()
	{
		if (_root != null)
		{
			return;
		}

		_root = new GameObject("comfort_hint", typeof(RectTransform));
		_root.transform.SetParent(transform, false);
		var rootTransform = (RectTransform)_root.transform;
		rootTransform.anchorMin = new Vector2(1f, 1f);
		rootTransform.anchorMax = new Vector2(1f, 1f);
		rootTransform.pivot = new Vector2(1f, 1f);
		rootTransform.anchoredPosition = new Vector2(-4f, -4f);
		rootTransform.sizeDelta = new Vector2(14f, 14f);

		_primaryChevron = CreateChevron(rootTransform, "primary");
		_secondaryChevron = CreateChevron(rootTransform, "secondary");
		_root.SetActive(false);
	}

	/// <summary>
	/// Creates an upwards pointing chevron.
	/// </summary>
	/// <param name="parent"> Parent transform. </param>
	/// <param name="name"> Chevron name. </param>
	/// <returns> Chevron transform. </returns>
	private static RectTransform CreateChevron(Transform parent, string name)
	{
		var root = new GameObject(name, typeof(RectTransform));
		root.transform.SetParent(parent, false);
		var rootTransform = (RectTransform)root.transform;
		rootTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rootTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rootTransform.pivot = new Vector2(0.5f, 0.5f);
		rootTransform.sizeDelta = new Vector2(10f, 5f);

		CreateBar(rootTransform, new Vector2(-1.75f, 0f), 45f);
		CreateBar(rootTransform, new Vector2(1.75f, 0f), -45f);

		return rootTransform;
	}

	/// <summary>
	/// Creates a single chevron segment.
	/// </summary>
	/// <param name="parent"> Parent transform. </param>
	/// <param name="position"> Local position. </param>
	/// <param name="rotation"> Rotation in degrees. </param>
	private static void CreateBar(Transform parent,
		Vector2 position, float rotation)
	{
		var bar = new GameObject("bar", typeof(RectTransform));
		bar.transform.SetParent(parent, false);

		var transform = (RectTransform)bar.transform;
		transform.anchorMin = new Vector2(0.5f, 0.5f);
		transform.anchorMax = new Vector2(0.5f, 0.5f);
		transform.pivot = new Vector2(0.5f, 0.5f);
		transform.anchoredPosition = position;
		transform.sizeDelta = new Vector2(5f, 1.75f);
		transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

		var image = bar.AddComponent<RawImage>();
		image.texture = Texture2D.whiteTexture;
		image.color = new Color32(100, 255, 100, 255);
		image.raycastTarget = false;
	}
}
