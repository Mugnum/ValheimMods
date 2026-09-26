using System;
using System.Collections.Generic;

namespace Mugnum.ValheimMods.ProjectilePenetration.Modules.Penetration;

/// <summary>
/// Projectile state.
/// </summary>
internal sealed class ProjectileState
{
	/// <summary>
	/// Projectile Id for debug.
	/// </summary>
	public Guid DebugId = Guid.NewGuid();

	/// <summary>
	/// Projectile owner.
	/// </summary>
	public Player Owner;

	/// <summary>
	/// Hit information.
	/// </summary>
	public HitData BaseHitData;

	/// <summary>
	/// Shooter name.
	/// </summary>
	public string ShooterName;

	/// <summary>
	/// Weapon name.
	/// </summary>
	public string WeaponName;

	/// <summary>
	/// Ammo name.
	/// </summary>
	public string AmmoName;

	/// <summary>
	/// Launch strength (bow draw charge level).
	/// </summary>
	public float LaunchStrength;

	/// <summary>
	/// Launch speed.
	/// </summary>
	public float LaunchSpeed;

	/// <summary>
	/// Number of max available penetrations.
	/// </summary>
	public int MaxPenetrations;

	/// <summary>
	/// Number of characters that already got hit by projectile.
	/// </summary>
	public int CharacterHits;

	/// <summary>
	/// Characters that got hit so far.
	/// </summary>
	public readonly HashSet<int> HitCharacterIds = [];
}
