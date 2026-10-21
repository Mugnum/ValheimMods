using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.MassCheckStacking.Common;

/// <summary>
/// Executes vanilla chest stacking requests one at a time, including their asynchronous responses.
/// </summary>
internal static class ChestStackController
{
	/// <summary>
	/// Maximum wait before cancelling a batch without retrying an ambiguous request.
	/// </summary>
	private const float ResponseTimeout = 10f;

	/// <summary>
	/// Access to the chest's actual network view, including root object overrides.
	/// </summary>
	private static readonly AccessTools.FieldRef<Container, ZNetView> NetworkView =
		AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");

	/// <summary>
	/// Vanilla privacy check.
	/// </summary>
	private static readonly Func<Container, long, bool> CheckPrivacy =
		AccessTools.MethodDelegate<Func<Container, long, bool>>(AccessTools.Method(typeof(Container), "CheckAccess"));

	/// <summary>
	/// Reloads the latest synchronized inventory after ownership is granted.
	/// </summary>
	private static readonly Func<Container, bool> LoadInventory =
		AccessTools.MethodDelegate<Func<Container, bool>>(AccessTools.Method(typeof(Container), "Load"));

	/// <summary>
	/// Chests remaining in the current batch.
	/// </summary>
	private static readonly Queue<Container> Targets = new();

	/// <summary>
	/// Outstanding requests, including cancelled requests whose late replies must be suppressed.
	/// </summary>
	private static readonly Dictionary<Container, StackRequest> Requests = [];

	/// <summary>
	/// Player who started the current batch.
	/// </summary>
	private static Player BatchPlayer;

	/// <summary>
	/// World in which the batch was started.
	/// </summary>
	private static long BatchWorldUid;

	/// <summary>
	/// Chest selected by the player, which may already be open in their inventory UI.
	/// </summary>
	private static Container Origin;

	/// <summary>
	/// Request that must finish before another chest can be processed.
	/// </summary>
	private static StackRequest ActiveRequest;

	/// <summary>
	/// Whether the controller is dispatching a vanilla request rather than handling user input.
	/// </summary>
	internal static bool IsSendingRequest { get; private set; }

	/// <summary>
	/// Whether late network responses still need the response guard.
	/// </summary>
	internal static bool HasOutstandingRequests => Requests.Count > 0;

	/// <summary>
	/// Checks only the viewed chest, without enumerating nearby pieces or items.
	/// </summary>
	/// <param name="container"> Chest to check. </param>
	/// <returns> Is the chest active with a valid network view and a supported inventory. </returns>
	internal static bool IsSupported(Container container)
	{
		if (!container || !container.gameObject.activeInHierarchy || container.m_autoDestroyEmpty)
		{
			return false;
		}

		var view = NetworkView(container);
		return view && view.IsValid() && container.GetInventory() != null;
	}

	/// <summary>
	/// Prevents a second request for a chest whose response is still ambiguous.
	/// </summary>
	/// <param name="container"> Chest to check. </param>
	/// <returns> Is a response still outstanding for this chest. </returns>
	internal static bool IsPending(Container container)
	{
		return Requests.ContainsKey(container);
	}

	/// <summary>
	/// Captures nearby chests only after vanilla's hold-to-stack action is triggered.
	/// </summary>
	/// <param name="origin"> Selected chest, processed before nearby chests. </param>
	internal static void Start(Container origin)
	{
		if (!ReferenceEquals(BatchPlayer, null) && !IsSessionValid(BatchPlayer, BatchWorldUid))
		{
			Cancel();
		}

		if (!Plugin.IsActive || !ReferenceEquals(BatchPlayer, null) || !Player.m_localPlayer || !ZNet.instance || !Game.instance
			|| !IsSupported(origin))
		{
			return;
		}

		var player = Player.m_localPlayer;
		if (!CanAccess(origin, player.GetPlayerID(), true))
		{
			return;
		}

		foreach (var chest in Requests.Keys.Where(chest => !chest).ToArray())
		{
			Requests.Remove(chest);
		}

		ItemCatalog.Refresh();
		var position = origin.transform.position;
		var pieces = new List<Piece>();
		Piece.GetAllPiecesInRadius(position, Plugin.StackRadius.Value, pieces);
		var chests = pieces.Select(piece => piece.GetComponent<Container>())
			.Append(origin)
			.Where(IsSupported)
			.Distinct()
			.OrderBy(chest => chest != origin)
			.ThenBy(chest => (chest.transform.position - position).sqrMagnitude)
			.ThenBy(chest => chest.GetInstanceID());

		Targets.Clear();
		foreach (var chest in chests)
		{
			Targets.Enqueue(chest);
		}

		BatchPlayer = player;
		BatchWorldUid = ZNet.instance.GetWorldUID();
		Origin = origin;
	}

	/// <summary>
	/// Sends the next request only after the previous RPC response and transfer have finished.
	/// </summary>
	internal static void Update()
	{
		if (ReferenceEquals(BatchPlayer, null))
		{
			return;
		}

		if (!IsSessionValid(BatchPlayer, BatchWorldUid))
		{
			Cancel();
			return;
		}

		if (ActiveRequest != null)
		{
			if (Time.unscaledTime - ActiveRequest.StartedAt >= ResponseTimeout)
			{
				Plugin.Log.LogWarning("Mass stacking cancelled: timed out waiting for a chest. Any late reply will be ignored.");
				Cancel();
			}
			return;
		}

		while (Targets.Count > 0)
		{
			var chest = Targets.Dequeue();
			if (!IsSupported(chest) || IsPending(chest)
				|| !CanAccess(chest, BatchPlayer.GetPlayerID(), chest == Origin))
			{
				continue;
			}

			var request = new StackRequest(chest, BatchPlayer, BatchWorldUid);
			Requests[chest] = request;
			ActiveRequest = request;
			IsSendingRequest = true;
			try
			{
				chest.StackAll();
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError($"Mass stacking cancelled while requesting chest access.\r\n{ex}");
				Cancel();
			}
			finally
			{
				IsSendingRequest = false;
			}
			return;
		}

		Cancel();
	}

	/// <summary>
	/// Sets the item filter for this granted response, or suppresses a cancelled response.
	/// </summary>
	/// <param name="chest"> Chest that received the stacking response. </param>
	/// <param name="granted"> Was stacking access granted by the chest owner. </param>
	/// <param name="request"> Matching pending request, or null for an unrelated response. </param>
	/// <returns> Flag indicating need to execute the original response method. </returns>
	internal static bool PrepareResponse(Container chest, bool granted, out StackRequest request)
	{
		if (!Requests.TryGetValue(chest, out request))
		{
			return true;
		}

		if (request.Cancelled || !IsSessionValid(request.Player, request.WorldUid)
			|| !ReferenceEquals(request.Source, request.Player.GetInventory())
			|| !IsSupported(chest)
			|| !CanAccess(chest, request.Player.GetPlayerID(), chest == Origin))
		{
			return false;
		}

		if (granted)
		{
			if (!chest.IsOwner())
			{
				// Never transfer into an inventory before vanilla's ownership grant is visible.
				return false;
			}

			LoadInventory(chest);
			StackItemFilter.BeginTransfer(chest.GetInventory(), request.Source, request.Player);
		}

		return true;
	}

	/// <summary>
	/// Clears filtering even when vanilla or another patch throws during a response.
	/// </summary>
	/// <param name="request"> Completed request, or null for an unrelated response. </param>
	/// <param name="exception"> Exception raised during the response, or null if processing succeeded. </param>
	internal static void CompleteResponse(StackRequest request, Exception exception)
	{
		if (request == null)
		{
			return;
		}

		StackItemFilter.EndTransfer();
		Requests.Remove(request.Chest);
		if (ReferenceEquals(ActiveRequest, request))
		{
			ActiveRequest = null;
		}

		if (exception != null)
		{
			Plugin.Log.LogError($"Mass stacking cancelled while processing a chest.\r\n{exception}");
			Cancel();
		}
	}

	/// <summary>
	/// Applies vanilla privacy and ward rules, and skips occupied chests and wagons.
	/// </summary>
	/// <param name="chest"> Chest to check. </param>
	/// <param name="playerId"> Id of the player requesting access. </param>
	/// <param name="isOrigin"> Is this the selected chest, which may already be open locally. </param>
	/// <returns> Can the player access this chest for stacking. </returns>
	private static bool CanAccess(Container chest, long playerId, bool isOrigin)
	{
		if (!CheckPrivacy(chest, playerId)
			|| (chest.m_checkGuardStone && !PrivateArea.CheckAccess(chest.transform.position, flash: false))
			|| (chest.m_wagon && chest.m_wagon.InUse()))
		{
			return false;
		}

		var view = NetworkView(chest);
		return (isOrigin && view.IsOwner())
			|| (!chest.IsInUse() && view.GetZDO().GetInt(ZDOVars.s_inUse) == 0);
	}

	/// <summary>
	/// Rejects responses after logout, world changes, teleportation or configuration disabling.
	/// </summary>
	/// <param name="player"> Player who started the batch. </param>
	/// <param name="worldUid"> World UId captured when the batch started. </param>
	/// <returns> Is the original player session still valid for mass stacking. </returns>
	private static bool IsSessionValid(Player player, long worldUid)
	{
		return Plugin.IsActive && Plugin.Enabled.Value && Plugin.ModifierKey.Value != KeyCode.None
			&& player && player == Player.m_localPlayer && !player.IsDead() && !player.IsTeleporting()
			&& ZNet.instance && ZNet.instance.GetWorldUID() == worldUid;
	}

	/// <summary>
	/// Cancels the queue while retaining guards for outstanding responses.
	/// </summary>
	internal static void Cancel()
	{
		if (ActiveRequest != null)
		{
			ActiveRequest.Cancelled = true;
		}
		ActiveRequest = null;
		Targets.Clear();
		BatchPlayer = null;
		Origin = null;
	}
}
