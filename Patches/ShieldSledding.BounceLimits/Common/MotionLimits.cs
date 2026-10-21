using BepInEx.Configuration;
using System;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.BounceLimits.Common;

/// <summary>
/// Bounds velocities and direct movement of the local sledding player.
/// </summary>
internal static class MotionLimits
{
	/// <summary>
	/// Active tick scope, also used by guarded position writes in helper methods.
	/// </summary>
	[ThreadStatic]
	internal static TickState Current;

	/// <summary>
	/// Creates a movement budget for an entire tick, including nested rescue calls.
	/// </summary>
	internal static TickState Begin(Player player, float dt)
	{
		var previous = Current;
		if (!player || player != Player.m_localPlayer || player.IsTeleporting())
		{
			Current = null;
			return previous;
		}
		if (Current != null && Current.Player == player)
		{
			return previous;
		}

		var body = player.GetComponent<Rigidbody>();
		var position = body ? body.position : player.transform.position;
		if (!IsFinite(position))
		{
			position = player.transform.position;
		}
		Current = IsFinite(position) ? new TickState(player, body, position, dt) : null;
		return previous;
	}

	/// <summary>
	/// Clamps finite velocity components and removes invalid values before physics consumes them.
	/// </summary>
	internal static Vector3 ClampVelocity(Vector3 velocity)
	{
		velocity.x = IsFinite(velocity.x) ? velocity.x : 0f;
		velocity.y = IsFinite(velocity.y) ? velocity.y : 0f;
		velocity.z = IsFinite(velocity.z) ? velocity.z : 0f;
		velocity.y = Mathf.Clamp(velocity.y, -Limit(Plugin.MaximumFallSpeed, 60f), Limit(Plugin.MaximumUpwardSpeed, 8f));
		return ClampHorizontal(velocity, Limit(Plugin.MaximumHorizontalSpeed, 60f));
	}

	/// <summary>
	/// Clamps a position relative to the start of the tick, so repeated setters cannot stack lifts.
	/// </summary>
	internal static Vector3 ClampPosition(Vector3 position)
	{
		var state = Current;
		if (state == null)
		{
			return position;
		}
		if (!IsFinite(position))
		{
			return state.Start;
		}

		var correction = Limit(Plugin.MaximumCorrection, 0.5f);
		var horizontalBudget = Limit(Plugin.MaximumHorizontalSpeed, 60f) * state.DeltaTime + correction;
		var riseBudget = Limit(Plugin.MaximumUpwardSpeed, 8f) * state.DeltaTime + correction;
		var fallBudget = Limit(Plugin.MaximumFallSpeed, 60f) * state.DeltaTime + correction;
		// Subtract in double precision so finite extreme positions cannot overflow the delta.
		var x = (double)position.x - state.Start.x;
		var z = (double)position.z - state.Start.z;
		var length = Math.Sqrt(x * x + z * z);
		if (length > horizontalBudget)
		{
			var scale = horizontalBudget / length;
			x *= scale;
			z *= scale;
		}
		var y = Math.Max(-fallBudget, Math.Min(riseBudget, (double)position.y - state.Start.y));
		return new Vector3(state.Start.x + (float)x, state.Start.y + (float)y, state.Start.z + (float)z);
	}

	/// <summary>
	/// Guards the player's rigidbody position without affecting other bodies.
	/// </summary>
	internal static void SetBodyPosition(Rigidbody body, Vector3 position)
	{
		body.position = Current != null && body == Current.Body ? ClampPosition(position) : position;
	}

	/// <summary>
	/// Guards the player's root transform without affecting visual meshes or other objects.
	/// </summary>
	internal static void SetTransformPosition(Transform transform, Vector3 position)
	{
		transform.position = Current != null && transform == Current.Player.transform ? ClampPosition(position) : position;
	}

	/// <summary>
	/// Guards scheduled kinematic movement of the player's body.
	/// </summary>
	internal static void MoveBodyPosition(Rigidbody body, Vector3 position)
	{
		body.MovePosition(Current != null && body == Current.Body ? ClampPosition(position) : position);
	}

	/// <summary>
	/// Reads a finite nonnegative configuration value.
	/// </summary>
	private static float Limit(ConfigEntry<float> entry, float fallback)
	{
		var value = entry?.Value ?? fallback;
		return IsFinite(value) ? Mathf.Max(0f, value) : fallback;
	}

	/// <summary>
	/// Limits horizontal magnitude without overflowing on extreme finite input.
	/// </summary>
	private static Vector3 ClampHorizontal(Vector3 velocity, float maximum)
	{
		var length = Math.Sqrt((double)velocity.x * velocity.x + (double)velocity.z * velocity.z);
		if (length > maximum)
		{
			var scale = maximum / length;
			velocity.x = (float)(velocity.x * scale);
			velocity.z = (float)(velocity.z * scale);
		}
		return velocity;
	}

	/// <summary>
	/// Checks all components for NaN and infinity.
	/// </summary>
	internal static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

	/// <summary>
	/// Checks a scalar for NaN and infinity.
	/// </summary>
	private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

	/// <summary>
	/// Shared movement budget for one local sled tick.
	/// </summary>
	internal sealed class TickState
	{
		/// <summary>
		/// Local player whose root transform is guarded.
		/// </summary>
		internal readonly Player Player;

		/// <summary>
		/// Local player body whose position is guarded.
		/// </summary>
		internal readonly Rigidbody Body;

		/// <summary>
		/// Position at the start of the enclosing tick.
		/// </summary>
		internal readonly Vector3 Start;

		/// <summary>
		/// Finite tick duration used to compute speed-based movement allowances.
		/// </summary>
		internal readonly float DeltaTime;

		/// <summary>
		/// Captures one movement budget without querying the world.
		/// </summary>
		internal TickState(Player player, Rigidbody body, Vector3 start, float dt)
		{
			Player = player;
			Body = body;
			Start = start;
			DeltaTime = IsFinite(dt) ? Mathf.Clamp(dt, 0.001f, 0.05f) : 0.02f;
		}
	}
}
