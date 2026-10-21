using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;

/// <summary>
/// Tracks local sled collisions and restores temporarily ignored collider pairs.
/// </summary>
internal static class SledContext
{
	/// <summary>
	/// Original vegetation classifier.
	/// </summary>
	private static Func<Collider, bool> IsOriginalVegetation;

	/// <summary>
	/// Getter for the active sled controller.
	/// </summary>
	private static MethodInfo GetController;

	/// <summary>
	/// Getter for simulated velocity.
	/// </summary>
	private static MethodInfo GetVelocity;

	/// <summary>
	/// Method for changing simulated velocity without changing its vertical component.
	/// </summary>
	private static MethodInfo SetVelocity;

	/// <summary>
	/// Objects recently passed through, deduplicated across their child colliders.
	/// </summary>
	private static readonly Dictionary<int, float> RecentContacts = [];

	/// <summary>
	/// Vegetation colliders whose collision with the player was disabled by this patch.
	/// </summary>
	private static readonly HashSet<Collider> IgnoredColliders = [];

	/// <summary>
	/// Player collider associated with ignored collision pairs.
	/// </summary>
	private static Collider PlayerCollider;

	/// <summary>
	/// Local player currently inside a sled physics tick.
	/// </summary>
	internal static Player ActivePlayer;

	/// <summary>
	/// Controller for the current physics tick.
	/// </summary>
	private static object Controller;

	/// <summary>
	/// Current horizontal speed used for penalties.
	/// </summary>
	private static float Speed;

	/// <summary>
	/// Accumulated stamina cost, capped once per tick.
	/// </summary>
	private static float StaminaCost;

	/// <summary>
	/// Accumulated horizontal speed loss, capped once per tick.
	/// </summary>
	private static float SpeedLoss;

	/// <summary>
	/// Resolves the supported mod's internal methods once on startup.
	/// </summary>
	internal static void Initialize()
	{
		var utility = RequireType("ShieldSledding.Utility");
		var classifier = RequireMethod(utility, "IsBreakableSledVegetation");
		IsOriginalVegetation = (Func<Collider, bool>)Delegate.CreateDelegate(typeof(Func<Collider, bool>), classifier);
		var controller = RequireType("ShieldSledding.ShieldSledController");
		GetController = AccessTools.PropertyGetter(controller, "Instance") ?? throw new MissingMethodException(controller.FullName, "get_Instance");
		GetVelocity = AccessTools.PropertyGetter(controller, "SimulatedVelocity") ?? throw new MissingMethodException(controller.FullName, "get_SimulatedVelocity");
		SetVelocity = RequireMethod(controller, "SetSimulatedVelocity");
	}

	/// <summary>
	/// Finds an expected mod type.
	/// </summary>
	internal static Type RequireType(string name) => AccessTools.TypeByName(name) ?? throw new TypeLoadException($"Could not find {name}.");

	/// <summary>
	/// Finds an expected method.
	/// </summary>
	internal static MethodInfo RequireMethod(Type type, string name) => AccessTools.Method(type, name) ?? throw new MissingMethodException(type.FullName, name);

	/// <summary>
	/// Begins a local sled tick without scanning the world.
	/// </summary>
	internal static void Begin(Player player)
	{
		ActivePlayer = player && player == Player.m_localPlayer ? player : null;
		StaminaCost = 0f;
		SpeedLoss = 0f;
		if (!ActivePlayer)
		{
			return;
		}

		var playerCollider = player.GetComponent<CapsuleCollider>();
		if (PlayerCollider != playerCollider)
		{
			RestoreCollisions();
			PlayerCollider = playerCollider;
		}

		Controller = GetController.Invoke(null, null);
		var velocity = Controller != null ? (Vector3)GetVelocity.Invoke(Controller, null) : player.GetVelocity();
		Speed = HorizontalSpeed(velocity);
		var expired = new List<int>();
		foreach (var entry in RecentContacts)
		{
			if (Time.time >= entry.Value)
			{
				expired.Add(entry.Key);
			}
		}
		foreach (var id in expired)
		{
			RecentContacts.Remove(id);
		}
	}

	/// <summary>
	/// Applies capped penalties after the mod has committed its simulated movement.
	/// </summary>
	internal static void Finish(bool applyPenalties)
	{
		try
		{
			if (!applyPenalties || !ActivePlayer)
			{
				return;
			}

			var stamina = Mathf.Min(StaminaCost, Mathf.Max(0f, ActivePlayer.GetStamina()));
			if (stamina > 0f)
			{
				ActivePlayer.UseStamina(stamina);
			}

			if (SpeedLoss > 0f && Controller != null)
			{
				var velocity = (Vector3)GetVelocity.Invoke(Controller, null);
				var speed = HorizontalSpeed(velocity);
				if (speed > 0.001f)
				{
					var factor = Mathf.Max(0f, speed - SpeedLoss) / speed;
					velocity.x *= factor;
					velocity.z *= factor;
					SetVelocity.Invoke(Controller, [ActivePlayer, velocity]);
				}
			}
		}
		finally
		{
			ActivePlayer = null;
			Controller = null;
		}
	}

	/// <summary>
	/// Checks a conservative subset of the original mod's vegetation candidates.
	/// </summary>
	internal static bool IsSmallVegetation(Collider collider)
	{
		if (!collider || collider.isTrigger || collider is TerrainCollider
			|| collider.GetComponentInParent<Heightmap>()
			|| collider.GetComponentInParent<Piece>() || collider.GetComponentInParent<WearNTear>()
			|| collider.GetComponentInParent<Character>() || collider.GetComponentInParent<Ship>()
			|| collider.GetComponentInParent<TreeLog>() || collider.GetComponentInParent<MineRock>()
			|| collider.GetComponentInParent<MineRock5>() || !IsOriginalVegetation(collider))
		{
			return false;
		}

		// Require a destructible plant component as well as a matching name.
		// A terrain or building collider named after a bush must never become passable.
		if (!collider.GetComponentInParent<Destructible>() && !collider.GetComponentInParent<TreeBase>())
		{
			return false;
		}

		var size = collider.bounds.size;
		return size.y <= 4f && size.x <= 3f && size.z <= 3f;
	}

	/// <summary>
	/// Whether a collision query may skip this plant during the current sled tick.
	/// </summary>
	internal static bool ShouldIgnore(Collider collider)
	{
		return ActivePlayer && Plugin.Mode.Value == VegetationMode.IgnoreSmallVegetationCollisions && IsSmallVegetation(collider);
	}

	/// <summary>
	/// Records a plant contact, disabling only its collision pair with the local player.
	/// </summary>
	internal static void Contact(Collider collider, bool applyPenalty = true)
	{
		if (!ShouldIgnore(collider))
		{
			return;
		}

		if (PlayerCollider && !Physics.GetIgnoreCollision(PlayerCollider, collider))
		{
			Physics.IgnoreCollision(PlayerCollider, collider, true);
			IgnoredColliders.Add(collider);
		}
		if (!applyPenalty)
		{
			return;
		}

		var destructible = collider.GetComponentInParent<IDestructible>() as Component;
		var id = destructible ? destructible.GetInstanceID() : collider.GetInstanceID();
		if (RecentContacts.ContainsKey(id))
		{
			return;
		}
		RecentContacts[id] = Time.time + 0.75f;
		QueuePenalty(Speed);
	}

	/// <summary>
	/// Accumulates finite, nonnegative penalties without exceeding the per-tick caps.
	/// </summary>
	internal static void QueuePenalty(float speed)
	{
		if (!ActivePlayer || float.IsNaN(speed) || float.IsInfinity(speed))
		{
			return;
		}

		speed = Mathf.Clamp(speed, 0f, 100f);
		StaminaCost = PenaltyLimits.Accumulate(StaminaCost, speed, Plugin.StaminaPerSpeed.Value, Plugin.MaximumStaminaCost.Value);
		SpeedLoss = PenaltyLimits.Accumulate(SpeedLoss, speed, Mathf.Clamp01(Plugin.SpeedLossPercent.Value / 100f), Plugin.MaximumSpeedLoss.Value);
	}

	/// <summary>
	/// Gets a finite horizontal speed.
	/// </summary>
	private static float HorizontalSpeed(Vector3 velocity)
	{
		var speed = new Vector2(velocity.x, velocity.z).magnitude;
		return float.IsNaN(speed) || float.IsInfinity(speed) ? 0f : speed;
	}

	/// <summary>
	/// Restores collision pairs without changing pairs that were already ignored before this patch.
	/// </summary>
	private static void RestoreCollisions()
	{
		foreach (var collider in IgnoredColliders)
		{
			if (PlayerCollider && collider)
			{
				Physics.IgnoreCollision(PlayerCollider, collider, false);
			}
		}
		IgnoredColliders.Clear();
		PlayerCollider = null;
	}

	/// <summary>
	/// Restores physical collisions and clears session state.
	/// </summary>
	internal static void Reset()
	{
		RestoreCollisions();
		RecentContacts.Clear();
		ActivePlayer = null;
		Controller = null;
	}
}
