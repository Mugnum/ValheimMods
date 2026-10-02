using System;
using System.Diagnostics;
using System.Linq;

namespace Mugnum.ValheimMods.ComfortHints.Common;

/// <summary>
/// Evaluates and displays comfort hints for the current build menu.
/// </summary>
internal static class ComfortHintController
{
	/// <summary>
	/// Build UI awaiting a refresh.
	/// </summary>
	private static BuildUi PendingBuildUi;

	/// <summary>
	/// Player associated with the pending refresh.
	/// </summary>
	private static Player PendingPlayer;

	/// <summary>
	/// Whether a refresh is pending, even if its objects were destroyed.
	/// </summary>
	private static bool RefreshPending;

	/// <summary>
	/// Most recent source of the pending refresh.
	/// </summary>
	private static string PendingSource;

	/// <summary>
	/// Number of requests combined into the pending refresh.
	/// </summary>
	private static int PendingRequestCount;

	/// <summary>
	/// Whether the build menu was open at the last diagnostic observation.
	/// </summary>
	private static bool MenuWasOpen;

	/// <summary>
	/// Queues a refresh, combining repeated requests within a frame.
	/// </summary>
	internal static void Refresh(BuildUi buildUi, Player player, string source = "unspecified")
	{
		if (buildUi == null || player == null || Plugin.Instance == null)
		{
			Plugin.Diagnostic($"Refresh rejected from {source}: buildUiMissing={buildUi == null}, playerMissing={player == null}, pluginMissing={Plugin.Instance == null}.");
			return;
		}

		PendingBuildUi = buildUi;
		PendingPlayer = player;
		RefreshPending = true;
		PendingSource = source;
		PendingRequestCount++;
		Plugin.Diagnostic($"Refresh requested from {source}: combinedRequests={PendingRequestCount}, menuActive={buildUi.gameObject.activeInHierarchy}.");
	}

	/// <summary>
	/// Discards any pending refresh.
	/// </summary>
	internal static void Invalidate(string source = null)
	{
		if (RefreshPending && source != null)
		{
			Plugin.Diagnostic($"Pending refresh cancelled by {source}; combinedRequests={PendingRequestCount}.");
		}

		PendingBuildUi = null;
		PendingPlayer = null;
		RefreshPending = false;
		PendingSource = null;
		PendingRequestCount = 0;
	}

	/// <summary>
	/// Processes the pending refresh synchronously.
	/// </summary>
	internal static void Flush()
	{
		ObserveBuildMenu();

		if (!RefreshPending)
		{
			return;
		}

		var buildUi = PendingBuildUi;
		var player = PendingPlayer;
		var source = PendingSource;
		var requestCount = PendingRequestCount;
		Invalidate();

		if (buildUi == null || player == null)
		{
			Plugin.Diagnostic($"Refresh skipped after {source}: buildUiMissing={buildUi == null}, playerMissing={player == null}.");
			return;
		}

		var isLocalPlayer = player == Player.m_localPlayer;
		var menuActive = buildUi.gameObject.activeInHierarchy;
		var inPlaceMode = player.InPlaceMode();

		if (!isLocalPlayer || !menuActive || !inPlaceMode)
		{
			Plugin.Diagnostic($"Refresh skipped after {source}: localPlayer={isLocalPlayer}, menuActive={menuActive}, inPlaceMode={inPlaceMode}.");
			return;
		}

		Plugin.Diagnostic($"Refresh executing after {source}; combinedRequests={requestCount}.");
		var stopwatch = Stopwatch.StartNew();

		try
		{
			Evaluate(buildUi, player);
		}
		catch (Exception exception)
		{
			ComfortHintUi.ClearAll(buildUi);
			Plugin.Log.LogError($"Comfort hint evaluation failed: {exception}");
		}
		finally
		{
			stopwatch.Stop();
			Plugin.Diagnostic($"Refresh finished in {stopwatch.Elapsed.TotalMilliseconds:F3} ms.");
		}
	}

	/// <summary>
	/// Clears controller state and existing hints.
	/// </summary>
	internal static void Reset()
	{
		Invalidate("controller reset");
		MenuWasOpen = false;

		if (Hud.instance != null && Hud.instance.m_buildUi != null)
		{
			ComfortHintUi.ClearAll(Hud.instance.m_buildUi);
		}
	}

	/// <summary>
	/// Evaluates visible pieces at the player's current position.
	/// </summary>
	private static void Evaluate(BuildUi buildUi, Player player)
	{
		ComfortHintUi.ClearAll(buildUi);
		var inShelter = player.InShelter();
		var position = player.transform.position;
		Plugin.Diagnostic($"Player context: position={position.ToString("F2")}, sheltered={inShelter}, requireNearbyBed={Plugin.RequireNearbyBed.Value}, cachedComfort={player.GetComfortLevel()}.");

		if (!inShelter)
		{
			Plugin.Diagnostic("Evaluation skipped: player is not sheltered.");
			return;
		}

		if (Plugin.RequireNearbyBed.Value && !ComfortContext.HasNearbyBed(position))
		{
			Plugin.Diagnostic("Evaluation skipped: no bed found within the 10 m comfort radius.");
			return;
		}

		var visibleButtons = ComfortHintUi.GetVisiblePieceButtons(buildUi);
		var comfortCount = visibleButtons.Count(button => button.Piece.m_comfort > 0);
		var conditionalCount = visibleButtons.Count(button => button.Piece.m_comfort > 0 && button.Piece.m_comfortObject != null);
		var buttons = visibleButtons.Where(button => CheckIsComfortCandidate(button.Piece)).ToList();
		Plugin.Diagnostic($"Candidate filtering: visibleButtons={visibleButtons.Count}, comfortSources={comfortCount}, conditionalSkipped={conditionalCount}, eligible={buttons.Count}.");

		if (buttons.Count == 0)
		{
			Plugin.Diagnostic("Evaluation skipped: no eligible comfort sources in the displayed pieces.");
			return;
		}

		var currentComfort = ComfortProbe.Calculate(true, position, null);
		var positiveGains = 0;
		var includedCandidates = 0;
		Plugin.Diagnostic($"Vanilla baseline comfort={currentComfort}.");

		foreach (var button in buttons)
		{
			var piece = button.Piece;
			var inclusionCount = ComfortProbe.CandidateInclusionCount;
			var comfortWithPiece = ComfortProbe.Calculate(true, position, piece);
			var candidateIncluded = ComfortProbe.CandidateInclusionCount != inclusionCount;
			var gain = comfortWithPiece - currentComfort;
			ComfortHintUi.SetGain(button, gain);
			positiveGains += gain > 0 ? 1 : 0;
			includedCandidates += candidateIncluded ? 1 : 0;
			Plugin.Diagnostic($"Candidate {piece.gameObject.name} ({piece.m_name}): group={piece.m_comfortGroup}, rawComfort={piece.m_comfort}, effectiveComfort={piece.GetComfort()}, included={candidateIncluded}, result={comfortWithPiece}, gain={gain}.");
		}

		if (includedCandidates != buttons.Count)
		{
			Plugin.Log.LogWarning($"Only {includedCandidates}/{buttons.Count} hypothetical candidates were confirmed in the comfort lookup. Check the lookup patch and other comfort mods.");
		}

		if (Plugin.EnableDiagnostics.Value)
		{
			var activeMarkers = buildUi.GetComponentsInChildren<ComfortHintMarker>(true).Count(marker => marker != null && marker.IsActive);
			Plugin.Diagnostic($"Evaluation complete: candidates={buttons.Count}, positiveGains={positiveGains}, activeMarkers={activeMarkers}.");
		}
	}

	/// <summary>
	/// Reports menu transitions independently of Harmony refresh hooks.
	/// </summary>
	private static void ObserveBuildMenu()
	{
		if (!Plugin.EnableDiagnostics.Value)
		{
			return;
		}

		var buildUi = Hud.instance != null ? Hud.instance.m_buildUi : null;
		var menuOpen = buildUi != null && buildUi.gameObject.activeInHierarchy;

		if (menuOpen != MenuWasOpen)
		{
			MenuWasOpen = menuOpen;
			Plugin.Diagnostic($"Build menu observed {(menuOpen ? "open" : "closed")}; refreshPending={RefreshPending}.");
		}
	}

	/// <summary>
	/// Checks for unconditional comfort sources.
	/// </summary>
	private static bool CheckIsComfortCandidate(Piece piece)
	{
		return piece != null && piece.m_comfort > 0 && piece.m_comfortObject == null;
	}
}
