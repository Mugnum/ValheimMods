using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.IncreaseMusicRadius.Common;

/// <summary>
/// Captures and adjusts one scene music trigger after its spawn-time configuration finishes.
/// </summary>
internal sealed class MusicRangeState : MonoBehaviour
{
	/// <summary>
	/// Tracked spatial music triggers.
	/// </summary>
	private static readonly Dictionary<MonoBehaviour, MusicRangeState> States = [];

	/// <summary>
	/// Trigger owning this state.
	/// </summary>
	private MonoBehaviour _target;

	/// <summary>
	/// Current location music volume, if present.
	/// </summary>
	private MusicVolume _volume;

	/// <summary>
	/// Legacy location audio source trigger, if present.
	/// </summary>
	private MusicLocation _legacy;

	/// <summary>
	/// Whether original values have been captured.
	/// </summary>
	private bool _captured;

	/// <summary>
	/// Whether outer box geometry needs the per-side fade distance correction.
	/// </summary>
	private bool _adjustedBox;

	/// <summary>
	/// Radius before this mod's adjustment.
	/// </summary>
	private float _originalRadius;

	/// <summary>
	/// Outer padding before this mod's adjustment.
	/// </summary>
	private float _originalOuter;

	/// <summary>
	/// Box geometry before this mod's adjustment.
	/// </summary>
	private Bounds _originalBounds;

	/// <summary>
	/// Last radius written by this mod.
	/// </summary>
	private float _appliedRadius;

	/// <summary>
	/// Last outer padding written by this mod.
	/// </summary>
	private float _appliedOuter;

	/// <summary>
	/// Last box geometry written by this mod.
	/// </summary>
	private Bounds _appliedBounds;

	/// <summary>
	/// Attaches state without changing prefab values during Awake.
	/// </summary>
	/// <param name="target"> Newly initialized music trigger. </param>
	internal static void Track(MonoBehaviour target)
	{
		if (!target || States.ContainsKey(target))
		{
			return;
		}

		var state = target.gameObject.AddComponent<MusicRangeState>();
		state._target = target;
		States.Add(target, state);
	}

	/// <summary>
	/// Includes scene triggers already loaded when the plugin initializes.
	/// </summary>
	internal static void TrackExisting()
	{
		foreach (var volume in UnityEngine.Object.FindObjectsByType<MusicVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			Track(volume);
		}

		foreach (var location in UnityEngine.Object.FindObjectsByType<MusicLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			Track(location);
		}
	}

	/// <summary>
	/// Captures effective values after vanilla radius additions, room assignment and other Awake patches.
	/// </summary>
	private void Start()
	{
		if (!_target)
		{
			return;
		}

		_volume = _target as MusicVolume;
		_legacy = _target as MusicLocation;
		_originalRadius = _appliedRadius = _volume ? _volume.m_radius : _legacy.m_radius;

		if (_volume)
		{
			_originalOuter = _appliedOuter = _volume.m_outerRadiusExtra;
			_originalBounds = _appliedBounds = _volume.m_boundsInner;
		}

		_captured = true;
		Apply();
	}

	/// <summary>
	/// Reapplies settings from the unmodified baseline rather than accumulating radius increases.
	/// </summary>
	private void Apply()
	{
		if (!_captured || !_target)
		{
			return;
		}

		CaptureExternalChanges();
		var roomMusic = _volume && _volume.m_sizeFromRoom || _target.GetComponentInParent<Room>();
		var hasMusic = !_volume || !string.IsNullOrWhiteSpace(_volume.m_musicName);
		var adjust = Plugin.IsActive && Plugin.Enabled.Value && !roomMusic && hasMusic;
		var extraRadius = adjust ? Plugin.ExtraRadius.Value : 0f;
		var box = _volume && _originalBounds.size.x != 0f;
		var radius = box ? _originalRadius : _originalRadius + extraRadius;

		if (_volume)
		{
			var bounds = _originalBounds;

			if (box && extraRadius > 0f)
			{
				// Bounds.Expand changes full size, so doubling gives the requested padding on each side.
				bounds.Expand(2f * extraRadius);
			}

			var outer = adjust ? Math.Max(_originalOuter, Plugin.MinimumFadeDistance.Value) : _originalOuter;
			_volume.m_radius = _appliedRadius = radius;
			_volume.m_outerRadiusExtra = _appliedOuter = outer;
			_volume.m_boundsInner = _appliedBounds = bounds;
			_adjustedBox = adjust && box && (extraRadius > 0f || Plugin.MinimumFadeDistance.Value > 0f);
			Plugin.Diagnostic($"MusicVolume '{_target.name}', track '{_volume.m_musicName}': " +
				(roomMusic ? "dungeon-room trigger skipped." : box
					? $"box size {_originalBounds.size} -> {bounds.size}; outer padding {_originalOuter}m -> {outer}m."
					: $"radius {_originalRadius}m -> {radius}m; outer padding {_originalOuter}m -> {outer}m."));
		}
		else
		{
			_legacy.m_radius = _appliedRadius = radius;
			Plugin.Diagnostic($"MusicLocation '{_target.name}': " +
				(roomMusic ? "dungeon-room trigger skipped." : $"radius {_originalRadius}m -> {radius}m."));
		}
	}

	/// <summary>
	/// Treats later edits by other mods as a new baseline instead of overwriting them on refresh.
	/// </summary>
	private void CaptureExternalChanges()
	{
		var radius = _volume ? _volume.m_radius : _legacy.m_radius;

		if (radius != _appliedRadius)
		{
			_originalRadius = radius;
		}

		if (!_volume)
		{
			return;
		}

		if (_volume.m_outerRadiusExtra != _appliedOuter)
		{
			_originalOuter = _volume.m_outerRadiusExtra;
		}

		if (!_volume.m_boundsInner.Equals(_appliedBounds))
		{
			_originalBounds = _volume.m_boundsInner;
		}
	}

	/// <summary>
	/// Refreshes loaded triggers only when configuration changes.
	/// </summary>
	internal static void RefreshAll()
	{
		foreach (var state in States.Values.ToArray())
		{
			if (state)
			{
				state.Apply();
			}
		}
	}

	/// <summary>
	/// Makes outer box padding agree with the distance used by vanilla's proximity fade calculation.
	/// </summary>
	/// <param name="volume"> Evaluated location volume. </param>
	/// <param name="bounds"> Vanilla outer world-space bounds. </param>
	internal static void AdjustOuterBounds(MusicVolume volume, ref Bounds bounds)
	{
		if (States.TryGetValue(volume, out var state) && state && state._adjustedBox)
		{
			// Vanilla adds padding once to full size, but its fade calculation expects that distance per side.
			bounds.Expand(volume.m_outerRadiusExtra);
		}
	}

	/// <summary>
	/// Keeps legacy enemy suppression checks at their original distance.
	/// </summary>
	/// <param name="location"> Legacy music trigger. </param>
	/// <returns> Radius before this mod's music range adjustment. </returns>
	internal static float GetEnemyCheckRadius(MusicLocation location)
	{
		return States.TryGetValue(location, out var state)
			&& state
			&& state._captured
			&& location.m_radius == state._appliedRadius
				? state._originalRadius
				: location.m_radius;
	}

	/// <summary>
	/// Restores original geometry while preserving unrelated later edits by other mods.
	/// </summary>
	private void Restore()
	{
		if (!_captured || !_target)
		{
			return;
		}

		CaptureExternalChanges();
		_adjustedBox = false;

		if (_volume)
		{
			_volume.m_radius = _appliedRadius = _originalRadius;
			_volume.m_outerRadiusExtra = _appliedOuter = _originalOuter;
			_volume.m_boundsInner = _appliedBounds = _originalBounds;
		}
		else
		{
			_legacy.m_radius = _appliedRadius = _originalRadius;
		}
	}

	/// <summary>
	/// Restores loaded triggers and removes helper components when the plugin unloads.
	/// </summary>
	internal static void RestoreAll()
	{
		foreach (var state in States.Values.ToArray())
		{
			if (state)
			{
				state.Restore();
				UnityEngine.Object.Destroy(state);
			}
		}

		States.Clear();
	}

	/// <summary>
	/// Releases state when its trigger is destroyed or the helper is removed.
	/// </summary>
	private void OnDestroy()
	{
		Restore();

		if (!ReferenceEquals(_target, null)
			&& States.TryGetValue(_target, out var state)
			&& ReferenceEquals(state, this))
		{
			States.Remove(_target);
		}
	}
}
