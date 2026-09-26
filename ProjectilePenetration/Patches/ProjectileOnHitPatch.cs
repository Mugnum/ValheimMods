using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ProjectilePenetration.Modules.Penetration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.ProjectilePenetration.Patches;

/// <summary>
/// Patch for <see cref="Projectile.OnHit"/> method.
/// </summary>
[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class ProjectileOnHitPatch
{
	/// <summary>
	/// Prefix before <see cref="Projectile.OnHit"/> method execution.
	/// </summary>
	/// <param name="__instance"> Projectile instance. </param>
	/// <param name="collider"> Collider. </param>
	/// <param name="hitPoint"> Hit point. </param>
	/// <param name="water"> Flag indicating projectile hitting water surface. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[HarmonyPrefix]
	[UsedImplicitly]
	private static bool Prefix(Projectile __instance, Collider collider,
		Vector3 hitPoint, bool water)
	{
		return ProjectilePenetrationHandler.HandleOnHit(__instance, collider, hitPoint, water);
	}
}
