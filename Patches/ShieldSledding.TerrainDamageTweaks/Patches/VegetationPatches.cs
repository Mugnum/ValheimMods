using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Patches;

/// <summary>
/// Patches vegetation damage entry points without changing wildlife damage.
/// </summary>
[HarmonyPatch]
internal static class VegetationDamagePatch
{
	/// <summary>
	/// Retrieves damage and vegetation scan methods.
	/// </summary>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		var type = SledContext.RequireType("ShieldSledding.VegetationImpact");
		yield return SledContext.RequireMethod(type, "TryRam");
		yield return SledContext.RequireMethod(type, "ApplyBreak");
	}

	/// <summary>
	/// Prevents the original high damage outside the unchanged mode.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(MethodBase __originalMethod)
	{
		return Plugin.Mode.Value == VegetationMode.OriginalDamage
			|| __originalMethod.Name == "TryRam" && Plugin.Mode.Value == VegetationMode.IgnoreSmallVegetationCollisions;
	}
}

/// <summary>
/// Replaces the destructive plant contact with a nondestructive collision event.
/// </summary>
[HarmonyPatch]
internal static class VegetationContactPatch
{
	/// <summary>
	/// Retrieves the original plant contact handler.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.VegetationImpact"), "TryHitCollider");

	/// <summary>
	/// Keeps original damage only in the unchanged mode and otherwise records a safe pass-through candidate.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Collider collider)
	{
		if (Plugin.Mode.Value == VegetationMode.OriginalDamage)
		{
			return true;
		}
		// The original ram probe looks several meters ahead. Ignore that collider now,
		// but charge a penalty only when a movement sweep or overlap actually reaches it.
		SledContext.Contact(collider, applyPenalty: false);
		return false;
	}
}

/// <summary>
/// Filters only Shield Sledding's obstacle and depenetration queries.
/// </summary>
[HarmonyPatch]
internal static class ObstacleQueryPatch
{
	/// <summary>
	/// Retrieves movement sweeps, overlap resolution and the solid-impact probe.
	/// </summary>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		var physics = SledContext.RequireType("ShieldSledding.PhysicsController");
		yield return SledContext.RequireMethod(physics, "ResolveObstacleMovement");
		yield return SledContext.RequireMethod(physics, "DepenetrateBlockingSolids");
		yield return SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.CollisionController"), "TryFindObstacle");
	}

	/// <summary>
	/// Redirects the expected physics query to a plant-aware wrapper.
	/// </summary>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var count = 0;
		foreach (var code in instructions)
		{
			if (code.operand is MethodInfo method && method.DeclaringType == typeof(Physics)
				&& (method.Name == "CapsuleCast" || method.Name == "OverlapCapsule" || method.Name == "SphereCast"))
			{
				var parameters = Array.ConvertAll(method.GetParameters(), parameter => parameter.ParameterType);
				code.opcode = OpCodes.Call;
				code.operand = AccessTools.Method(typeof(VegetationQueries), method.Name, parameters)
					?? throw new MissingMethodException($"Unsupported physics query: {method}.");
				count++;
			}
			yield return code;
		}
		if (count != 1)
		{
			throw new InvalidOperationException($"Expected one obstacle query, but found {count}.");
		}
	}
}

/// <summary>
/// Stops ignored plants from being mistaken for ground while preserving all terrain tests.
/// </summary>
[HarmonyPatch]
internal static class VegetationGroundPatch
{
	/// <summary>
	/// Retrieves the mod's ground-hit acceptance check.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.Utility"), "IsAcceptableGroundHit");

	/// <summary>
	/// Rejects ignored plants as ground without accepting any previously rejected surface.
	/// </summary>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(RaycastHit __0, ref bool __result)
	{
		if (__result && SledContext.ShouldIgnore(__0.collider))
		{
			__result = false;
		}
	}
}

/// <summary>
/// Bounds collision penalties to one local sled physics tick.
/// </summary>
[HarmonyPatch]
internal static class SledTickPatch
{
	/// <summary>
	/// Retrieves the local sled physics entry point.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.ShieldSledController"), "ApplySledPhysics");

	/// <summary>
	/// Starts contact deduplication and penalty accumulation for this tick.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player player) => SledContext.Begin(player);

	/// <summary>
	/// Applies penalties only after a successful tick and always releases the active player.
	/// </summary>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer(Exception __exception) => SledContext.Finish(__exception == null);
}

/// <summary>
/// Includes solid collisions in the optional stamina and speed penalties.
/// </summary>
[HarmonyPatch]
internal static class SolidImpactPatch
{
	/// <summary>
	/// Retrieves the original solid-impact callback.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.CollisionController"), "ApplyImpact");

	/// <summary>
	/// Adds the solid impact to the shared penalty budget.
	/// </summary>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(float speed) => SledContext.QueuePenalty(speed);
}

/// <summary>
/// Restores collider pairs on every sled exit, including manual cancellation and death.
/// </summary>
[HarmonyPatch]
internal static class SledEndPatch
{
	/// <summary>
	/// Retrieves the sled shutdown method.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => SledContext.RequireMethod(SledContext.RequireType("ShieldSledding.ShieldSledController"), "EndSledding");

	/// <summary>
	/// Restores collider pairs even if another shutdown step fails.
	/// </summary>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer() => SledContext.Reset();
}
