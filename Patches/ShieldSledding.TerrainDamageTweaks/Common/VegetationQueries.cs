using System;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;

/// <summary>
/// Filters only the sled mod's obstacle queries, retaining the first solid obstacle behind a plant.
/// </summary>
internal static class VegetationQueries
{
	/// <summary>
	/// Performs a capsule sweep while ignoring eligible plants during local sledding.
	/// </summary>
	internal static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction,
		out RaycastHit hit, float distance, int layerMask, QueryTriggerInteraction triggerInteraction)
	{
		if (!SledContext.ActivePlayer || Plugin.Mode.Value != VegetationMode.IgnoreSmallVegetationCollisions)
		{
			return Physics.CapsuleCast(point1, point2, radius, direction, out hit, distance, layerMask, triggerInteraction);
		}

		return FirstSolid(Physics.CapsuleCastAll(point1, point2, radius, direction, distance, layerMask, triggerInteraction), out hit);
	}

	/// <summary>
	/// Performs the mod's impact probe without hitting plants that are being passed through.
	/// </summary>
	internal static bool SphereCast(Vector3 origin, float radius, Vector3 direction,
		out RaycastHit hit, float distance, int layerMask)
	{
		if (!SledContext.ActivePlayer || Plugin.Mode.Value != VegetationMode.IgnoreSmallVegetationCollisions)
		{
			return Physics.SphereCast(origin, radius, direction, out hit, distance, layerMask);
		}

		return FirstSolid(Physics.SphereCastAll(origin, radius, direction, distance, layerMask), out hit);
	}

	/// <summary>
	/// Removes eligible plants from the sled's depenetration query.
	/// </summary>
	internal static Collider[] OverlapCapsule(Vector3 point1, Vector3 point2, float radius,
		int layerMask, QueryTriggerInteraction triggerInteraction)
	{
		var colliders = Physics.OverlapCapsule(point1, point2, radius, layerMask, triggerInteraction);
		if (!SledContext.ActivePlayer || Plugin.Mode.Value != VegetationMode.IgnoreSmallVegetationCollisions)
		{
			return colliders;
		}

		var count = 0;
		foreach (var collider in colliders)
		{
			if (SledContext.ShouldIgnore(collider))
			{
				SledContext.Contact(collider);
			}
			else
			{
				colliders[count++] = collider;
			}
		}
		Array.Resize(ref colliders, count);
		return colliders;
	}

	/// <summary>
	/// Selects the nearest non-plant hit without making an obstacle behind vegetation passable.
	/// </summary>
	private static bool FirstSolid(RaycastHit[] hits, out RaycastHit hit)
	{
		Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
		foreach (var candidate in hits)
		{
			// All-hit sweeps may include the player's own collider at distance zero.
			if (candidate.collider && candidate.collider.GetComponentInParent<Player>() == SledContext.ActivePlayer)
			{
				continue;
			}
			if (SledContext.ShouldIgnore(candidate.collider))
			{
				SledContext.Contact(candidate.collider);
				continue;
			}
			hit = candidate;
			return true;
		}
		hit = default;
		return false;
	}
}
