using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.ComfortHints.Common;

/// <summary>
/// Manages comfort hint markers in the build UI.
/// </summary>
internal static class ComfortHintUi
{
	/// <summary>
	/// Gets active build piece buttons with assigned pieces.
	/// </summary>
	internal static List<BuildUiPieceButton> GetVisiblePieceButtons(BuildUi buildUi)
	{
		if (buildUi == null)
		{
			return [];
		}

		return
		[
			..buildUi.GetComponentsInChildren<BuildUiPieceButton>(true)
				.Where(button => button != null
					&& button.gameObject.activeInHierarchy
					&& button.Piece != null)
		];
	}

	/// <summary>
	/// Displays a positive gain or hides an existing marker.
	/// </summary>
	internal static void SetGain(BuildUiPieceButton button, int gain)
	{
		if (button == null)
		{
			return;
		}

		var marker = button.GetComponent<ComfortHintMarker>();

		if (marker == null)
		{
			if (gain <= 0)
			{
				return;
			}

			marker = button.gameObject.AddComponent<ComfortHintMarker>();
		}

		marker.SetGain(gain);
	}

	/// <summary>
	/// Hides an existing marker.
	/// </summary>
	internal static void Clear(BuildUiPieceButton button)
	{
		SetGain(button, 0);
	}

	/// <summary>
	/// Hides markers on active and pooled buttons.
	/// </summary>
	internal static void ClearAll(BuildUi buildUi)
	{
		if (buildUi == null)
		{
			return;
		}

		foreach (var button in buildUi.GetComponentsInChildren<BuildUiPieceButton>(true))
		{
			Clear(button);
		}
	}
}
