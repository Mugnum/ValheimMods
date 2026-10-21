using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ShieldSledding.BounceLimits.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Mugnum.ValheimMods.ShieldSledding.BounceLimits.Patches;

/// <summary>
/// Resolves expected Shield Sledding methods without an assembly reference to its internal types.
/// </summary>
internal static class Targets
{
	/// <summary>
	/// Retrieves a required mod type.
	/// </summary>
	internal static Type Type(string name) => AccessTools.TypeByName("ShieldSledding." + name) ?? throw new TypeLoadException(name);

	/// <summary>
	/// Retrieves a required mod method.
	/// </summary>
	internal static MethodInfo Method(string type, string name) => AccessTools.Method(Type(type), name) ?? throw new MissingMethodException(type, name);
}

/// <summary>
/// Shares one position budget across the whole sled tick, including all motion substeps.
/// </summary>
[HarmonyPatch]
internal static class SledTickPatch
{
	/// <summary>
	/// Retrieves the complete sled physics tick.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => Targets.Method("ShieldSledController", "ApplySledPhysics");

	/// <summary>
	/// Captures the tick anchor before any physics or penetration corrections.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player player, float dt, out MotionLimits.TickState __state) => __state = MotionLimits.Begin(player, dt);

	/// <summary>
	/// Restores the enclosing scope even when the original method throws.
	/// </summary>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer(MotionLimits.TickState __state) => MotionLimits.Current = __state;
}

/// <summary>
/// Also guards startup snapping and input-driven jumps outside the ordinary physics tick.
/// </summary>
[HarmonyPatch]
internal static class SledEntryMotionPatch
{
	/// <summary>
	/// Retrieves entry points that may move the player before the next simulation tick.
	/// </summary>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return Targets.Method("ShieldSledController", "BeginSledding");
		yield return Targets.Method("PhysicsController", "TryJump");
	}

	/// <summary>
	/// Reuses an existing tick budget or establishes one for this input-driven movement.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player player, out MotionLimits.TickState __state) => __state = MotionLimits.Begin(player, Time.fixedDeltaTime);

	/// <summary>
	/// Restores the outer tick scope even if the original mod throws.
	/// </summary>
	[HarmonyFinalizer]
	[UsedImplicitly]
	private static void Finalizer(MotionLimits.TickState __state) => MotionLimits.Current = __state;
}

/// <summary>
/// Replaces direct root/body moves with guarded setters in the known correction paths.
/// </summary>
[HarmonyPatch]
internal static class PositionWritePatch
{
	/// <summary>
	/// Retrieves the known startup, ground, water and penetration correction paths.
	/// </summary>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		foreach (var name in new[] { "PeelFromPin", "TryJump", "PrewarmKinematicSwitch", "FinalizeDynamicStartup",
			"Simulate", "ConstrainFeetToGround", "ConstrainFeetToHeightmap", "RescueDeepPenetration",
			"ApplySimulatedMotion", "DepenetrateBlockingSolids", "ConstrainWaterSkimAgainstLand" })
		{
			yield return Targets.Method("PhysicsController", name);
		}
		foreach (var name in new[] { "FixGroundClip", "RescueTerrainPenetration", "RescueBuiltPiecePenetration", "SnapToGroundOnSledStart" })
		{
			yield return Targets.Method("SledGroundSolver", name);
		}
		yield return Targets.Method("WaterSledHelper", "ApplySoftWaterFollow");
	}

	/// <summary>
	/// Redirects native position setters while preserving original instruction labels.
	/// </summary>
	[HarmonyTranspiler]
	[UsedImplicitly]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var count = 0;
		foreach (var code in instructions)
		{
			if (code.operand is MethodInfo method)
			{
				var replacement = method.DeclaringType == typeof(Rigidbody)
					? method.Name == "set_position" ? nameof(MotionLimits.SetBodyPosition)
						: method.Name == "MovePosition" ? nameof(MotionLimits.MoveBodyPosition) : null
					: method.DeclaringType == typeof(Transform) && method.Name == "set_position" ? nameof(MotionLimits.SetTransformPosition) : null;
				if (replacement != null)
				{
					code.opcode = OpCodes.Call;
					code.operand = AccessTools.Method(typeof(MotionLimits), replacement);
					count++;
				}
			}
			yield return code;
		}
		if (count == 0)
		{
			throw new InvalidOperationException("Expected a player position write in the motion correction method.");
		}
	}
}

/// <summary>
/// Caps velocity before movement integration and at the mod's final hard-cap checkpoint.
/// </summary>
[HarmonyPatch]
internal static class VelocityPatch
{
	/// <summary>
	/// Retrieves velocity integration, assignment and final hard-cap checkpoints.
	/// </summary>
	[UsedImplicitly]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return Targets.Method("PhysicsController", "ApplySimulatedMotion");
		yield return Targets.Method("PhysicsController", "SetSimulatedVelocity");
		yield return Targets.Method("PhysicsController", "ClampToBoardHardCap");
	}

	/// <summary>
	/// Sanitizes velocity before a method consumes it.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(ref Vector3 velocity) => velocity = MotionLimits.ClampVelocity(velocity);

	/// <summary>
	/// Reapplies limits after the original method changes velocity.
	/// </summary>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(ref Vector3 velocity) => velocity = MotionLimits.ClampVelocity(velocity);
}

/// <summary>
/// Sanitizes the stored velocity before formulas consume it at the start of simulation.
/// </summary>
[HarmonyPatch]
internal static class SimulationInputPatch
{
	/// <summary>
	/// Retrieves the simulation method that reads stored or native body velocity.
	/// </summary>
	[UsedImplicitly]
	private static MethodBase TargetMethod() => Targets.Method("PhysicsController", "Simulate");

	/// <summary>
	/// Makes both velocity sources and the tick duration safe before calculations begin.
	/// </summary>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static void Prefix(Player player, ref Vector3 ____lastVelocity, ref float dt)
	{
		____lastVelocity = MotionLimits.ClampVelocity(____lastVelocity);
		dt = float.IsNaN(dt) || float.IsInfinity(dt) ? 0.02f : Mathf.Clamp(dt, 0.001f, 0.05f);
		var body = player ? player.GetComponent<Rigidbody>() : null;
		if (body && !body.isKinematic)
		{
			body.linearVelocity = MotionLimits.ClampVelocity(body.linearVelocity);
		}
	}
}
