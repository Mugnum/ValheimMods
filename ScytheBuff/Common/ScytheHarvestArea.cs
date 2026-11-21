using UnityEngine;

namespace Mugnum.ValheimMods.ScytheBuff.Common;

/// <summary>
/// Applies the scythe's horizontal harvest angle to vanilla's existing overlap results.
/// </summary>
internal static class ScytheHarvestArea
{
	/// <summary>
	/// Runs the original harvest query and removes hits outside a qualifying scythe's sweep.
	/// </summary>
	/// <param name="center"> Vanilla harvest sphere center, including the configured reach increase. </param>
	/// <param name="radius"> Vanilla skill-scaled radius, including the configured distance increase. </param>
	/// <param name="results"> Existing reusable collider buffer. </param>
	/// <param name="layerMask"> Original harvest layer mask. </param>
	/// <returns> Number of colliders remaining inside the harvest angle. </returns>
	internal static int OverlapSphere(Vector3 center, float radius, Collider[] results, int layerMask)
	{
		var count = Physics.OverlapSphereNonAlloc(center, radius, results, layerMask);
		var player = ScytheRules.SwingHarvester;

		if (player is null || !Plugin.Enabled.Value || Plugin.SwingAngle.Value >= 360f)
		{
			return count;
		}

		var origin = player.transform.position;
		var forward = player.transform.forward;
		var forwardLength = Mathf.Sqrt(forward.x * forward.x + forward.z * forward.z);

		if (forwardLength <= 0.00001f)
		{
			return count;
		}

		var forwardX = forward.x / forwardLength;
		var forwardZ = forward.z / forwardLength;
		var minimumDot = Mathf.Cos(Plugin.SwingAngle.Value * 0.5f * Mathf.Deg2Rad);
		var accepted = 0;

		for (var i = 0; i < count; i++)
		{
			var collider = results[i];

			if (!collider)
			{
				continue;
			}

			var offset = collider.transform.position - origin;
			var distance = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
			var dot = forwardX * offset.x + forwardZ * offset.z;

			if (distance <= 0.00001f || dot >= minimumDot * distance)
			{
				results[accepted++] = collider;
			}
		}

		// Compact the existing buffer so vanilla's crop and unhealthy-plant loops use the same filter.
		for (var i = accepted; i < count; i++)
		{
			results[i] = null;
		}

		return accepted;
	}
}
