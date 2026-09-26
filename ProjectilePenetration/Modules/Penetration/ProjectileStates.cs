using System.Runtime.CompilerServices;

namespace Mugnum.ValheimMods.ProjectilePenetration.Modules.Penetration;

/// <summary>
/// Tracker of projectile states.
/// </summary>
internal static class ProjectileStates
{
	/// <summary>
	/// Table of tracked projectiles. Cleared on GC.
	/// </summary>
	private static readonly ConditionalWeakTable<Projectile, ProjectileState> States = new();

	/// <summary>
	/// Resets projectile state.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="owner"> Projectile owner. </param>
	/// <param name="baseHitData"> Hit information. </param>
	/// <param name="weapon"> Weapon. </param>
	/// <param name="ammo"> Ammo. </param>
	/// <param name="launchStrength"> Launch strength (charge level). </param>
	/// <param name="launchSpeed"> Launch speed. </param>
	/// <param name="maxPenetrations"> Number of max available penetrations. </param>
	/// <returns> Projectile state. </returns>
	public static ProjectileState Reset(Projectile projectile, Player owner,
		HitData baseHitData, ItemDrop.ItemData weapon, ItemDrop.ItemData ammo,
		float launchStrength, float launchSpeed, int maxPenetrations)
	{
		const string UnknownTag = "<unknown>";
		States.Remove(projectile);
		var state = new ProjectileState
		{
			Owner = owner,
			BaseHitData = baseHitData.Clone(),
			ShooterName = owner.GetPlayerName(),
			WeaponName = weapon?.m_shared?.m_name ?? UnknownTag,
			AmmoName = ammo?.m_shared?.m_name ?? UnknownTag,
			LaunchStrength = launchStrength,
			LaunchSpeed = launchSpeed,
			MaxPenetrations = maxPenetrations
		};

		States.Add(projectile, state);
		return state;
	}

	/// <summary>
	/// Tries to retrieve projectile's state.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="state"> Found projectile state. </param>
	/// <returns> Projectile exists in tracked storage. </returns>
	public static bool TryGet(Projectile projectile, out ProjectileState state)
	{
		return States.TryGetValue(projectile, out state);
	}

	/// <summary>
	/// Removes tracked projectile.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	public static void Remove(Projectile projectile)
	{
		States.Remove(projectile);
	}
}
