using HarmonyLib;
using JetBrains.Annotations;
using Mugnum.ValheimMods.ProjectilePenetration.Modules.Penetration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Mugnum.ValheimMods.ProjectilePenetration.Patches;

/// <summary>
/// Patch for <see cref="Projectile.Setup"/> method.
/// </summary>
[HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal class ProjectileSetupPatch
{
	/// <summary>
	/// Postfix after <see cref="Projectile.Setup"/> method execution.
	/// </summary>
	/// <param name="__instance"> Projectile instance. </param>
	/// <param name="owner"> Projectile owner. </param>
	/// <param name="velocity"> Velocity. </param>
	/// <param name="hitData"> Hit information. </param>
	/// <param name="item"> Weapon. </param>
	/// <param name="ammo"> Ammo. </param>
	[HarmonyPostfix]
	[UsedImplicitly]
	private static void Postfix(Projectile __instance,
		Character owner, Vector3 velocity, HitData hitData,
		ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		ProjectilePenetrationHandler.SetupProjectileInfo(__instance, owner,
			velocity, hitData, item, ammo);
	}
}
