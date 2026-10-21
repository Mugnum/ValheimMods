using UnityEngine;

namespace Mugnum.ValheimMods.MassCheckStacking.Common;

/// <summary>
/// Identifies one pending vanilla stack response and the player inventory it belongs to.
/// </summary>
internal sealed class StackRequest
{
	/// <summary>
	/// Destination for this request.
	/// </summary>
	internal readonly Container Chest;

	/// <summary>
	/// Player who requested the transfer.
	/// </summary>
	internal readonly Player Player;

	/// <summary>
	/// Exact inventory supplying the items.
	/// </summary>
	internal readonly Inventory Source;

	/// <summary>
	/// World in which the request was sent.
	/// </summary>
	internal readonly long WorldUid;

	/// <summary>
	/// Unscaled time when the request was sent.
	/// </summary>
	internal readonly float StartedAt;

	/// <summary>
	/// Whether a late response must be suppressed.
	/// </summary>
	internal bool Cancelled;

	/// <summary>
	/// Captures the inventory and session before dispatching the request.
	/// </summary>
	/// <param name="chest"> Chest receiving the stacking request. </param>
	/// <param name="player"> Player whose inventory supplies items. </param>
	/// <param name="worldUid"> UId of the world in which the request is sent. </param>
	internal StackRequest(Container chest, Player player, long worldUid)
	{
		Chest = chest;
		Player = player;
		Source = player.GetInventory();
		WorldUid = worldUid;
		StartedAt = Time.unscaledTime;
	}
}
