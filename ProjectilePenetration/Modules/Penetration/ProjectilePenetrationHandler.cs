using HarmonyLib;
using System;
using UnityEngine;

namespace Mugnum.ValheimMods.ProjectilePenetration.Modules.Penetration;

/// <summary>
/// Handler for projectile penetration module.
/// </summary>
internal class ProjectilePenetrationHandler
{
	#region Fields

	/// <summary>
	/// Determines if target is valid.
	/// </summary>
	private static readonly IsValidTargetDelegate IsValidTarget = CreateIsValidTargetDelegate();

	#endregion

	#region Types

	/// <summary>
	/// Delegate for determining if target is valid.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="target"> Target. </param>
	/// <returns> Flag indicating if target is valid. </returns>
	private delegate bool IsValidTargetDelegate(Projectile projectile, IDestructible target);

	#endregion

	#region Methods: Internal

	/// <summary>
	/// Setups information about projectile.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="owner"> Projectile owner. </param>
	/// <param name="velocity"> Velocity. </param>
	/// <param name="hitData"> Hit information. </param>
	/// <param name="weapon"> Weapon. </param>
	/// <param name="ammo"> Ammo. </param>
	internal static void SetupProjectileInfo(Projectile projectile,
		Character owner, Vector3 velocity, HitData hitData,
		ItemDrop.ItemData weapon, ItemDrop.ItemData ammo)
	{
		if (!Plugin.IsModEnabled.Value
			|| !Plugin.IsPenetrationEnabled.Value
			|| owner is not Player player
			|| hitData == null
			|| weapon?.m_shared is not { m_skillType: Skills.SkillType.Bows })
		{
			return;
		}

		var launchSpeed = velocity.magnitude;
		var launchStrength = GetLaunchStrength(weapon, launchSpeed);
		var maxPenetrations = GetMaxPenetrations(launchStrength);
		var state = ProjectileStates.Reset(projectile, player, hitData, weapon,
			ammo, launchStrength, launchSpeed, maxPenetrations);

		var penetrationsLog = state.MaxPenetrations == int.MaxValue
			? "unlimited"
			: state.MaxPenetrations.ToString();

		Plugin.LogDebug($"Arrow {state.DebugId} CREATED " +
			$"player=\"{state.ShooterName}\" " +
			$"weapon=\"{state.WeaponName}\" " +
			$"ammo=\"{state.AmmoName}\" " +
			$"strength={state.LaunchStrength:P1} " +
			$"speed={state.LaunchSpeed:F2} " +
			$"damage={state.BaseHitData.GetTotalDamage():F2} " +
			$"penetrations={penetrationsLog}");
	}

	/// <summary>
	/// Handles event for hitting a target with projectile.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="collider"> Collider. </param>
	/// <param name="hitPoint"> Hit point. </param>
	/// <param name="isWater"> Flag indicating projectile hitting a water surface. </param>
	/// <returns> Is need to execute original method: <see cref="Projectile.OnHit"/>. </returns>
	internal static bool HandleOnHit(Projectile projectile, Collider collider,
		Vector3 hitPoint, bool isWater)
	{
		if (!Plugin.IsModEnabled.Value
			|| !Plugin.IsPenetrationEnabled.Value
			|| !ProjectileStates.TryGet(projectile, out var state)
			|| !collider
			|| isWater)
		{
			return true;
		}

		const float MinDamageThreshold = 0.05f;
		var hitObject = Projectile.FindHitObject(collider);
		var destructible = hitObject
			? hitObject.GetComponent<IDestructible>()
			: null;

		if (destructible is not Character target)
		{
			if (state.CharacterHits <= 0)
			{
				Plugin.LogDebug($"Arrow {state.DebugId} TERMINAL-VANILLA " +
					$"object=\"{GetHitObjectName(hitObject, collider)}\" " +
					$"afterHits={state.CharacterHits}");

				return true;
			}

			Plugin.LogDebug($"Arrow {state.DebugId} EXHAUSTED " +
				$"object=\"{GetHitObjectName(hitObject, collider)}\" " +
				$"afterHits={state.CharacterHits}");

			DestroyProjectile(projectile);
			return false;
		}

		if (!IsValidTarget(projectile, destructible))
		{
			Plugin.LogDebug($"Arrow {state.DebugId} INVALID-TARGET " +
				$"target=\"{target.name}\" " +
				$"targetId={target.GetInstanceID()}");

			return true;
		}

		var targetId = target.GetInstanceID();
		var targetName = target.name;

		if (!state.HitCharacterIds.Add(targetId))
		{
			Plugin.LogDebug($"Arrow {state.DebugId} DUPLICATE-COLLIDER " +
				$"target=\"{targetName}\" " +
				$"targetId={targetId} " +
				$"collider=\"{collider.name}\"");

			return false;
		}

		var hitNumber = state.CharacterHits + 1;
		var damageMultiplier = Mathf.Pow(Plugin.DamageRetentionRatio.Value, state.CharacterHits);
		var hit = BuildRangedHit(projectile, state, collider, hitPoint, damageMultiplier);
		var damage = hit.GetTotalDamage();

		if (state.CharacterHits > 0 && damage < MinDamageThreshold)
		{
			Plugin.LogDebug($"Arrow {state.DebugId} DAMAGE-EXHAUSTED " +
				$"target=\"{targetName}\" " +
				$"hit={hitNumber} " +
				$"damage={damage:F4} " +
				$"multiplier={damageMultiplier:F4}");

			DestroyProjectile(projectile);
			return false;
		}

		var canPenetrateThisTarget = state.MaxPenetrations == int.MaxValue
			|| state.CharacterHits < state.MaxPenetrations;

		Plugin.LogDebug($"Arrow {state.DebugId} HIT " +
			$"hit={hitNumber} " +
			$"target=\"{targetName}\" " +
			$"targetId={targetId} " +
			$"collider=\"{collider.name}\" " +
			$"multiplier={damageMultiplier:F4} " +
			$"damage={hit.GetTotalDamage():F2} " +
			$"canPenetrate={canPenetrateThisTarget}");

		target.Damage(hit);
		ApplyHealthReturn(state, hit);
		CreateHitEffects(projectile, hitPoint);
		CreateHitNoise(projectile, state);
		RaiseSkillIfApplicable(state, hit);
		state.CharacterHits++;

		if (!canPenetrateThisTarget)
		{
			Plugin.LogDebug($"Arrow {state.DebugId} PENETRATION-LIMIT " +
				$"target=\"{targetName}\" " +
				$"totalUniqueHits={state.CharacterHits} " +
				$"maxPenetrations={state.MaxPenetrations}");

			DestroyProjectile(projectile);
			return false;
		}

		var nextMultiplier = Mathf.Pow(Plugin.DamageRetentionRatio.Value, state.CharacterHits);
		var remainingPenetrationsLog = state.MaxPenetrations == int.MaxValue
			? "unlimited"
			: (state.MaxPenetrations - state.CharacterHits).ToString();

		Plugin.LogDebug($"Arrow {state.DebugId} PENETRATED " +
			$"target=\"{targetName}\" " +
			$"totalUniqueHits={state.CharacterHits} " +
			$"remainingPenetrations={remainingPenetrationsLog} " +
			$"nextMultiplier={nextMultiplier:F4}");

		return false;
	}

	#endregion

	#region Methods: Private

	/// <summary>
	/// Initializes <see cref="IsValidTargetDelegate"/> for Projectile.IsValidTarget method.
	/// </summary>
	/// <returns> Projectile.IsValidTarget method. </returns>
	/// <exception cref="MissingMethodException"> Method not found. </exception>
	private static IsValidTargetDelegate CreateIsValidTargetDelegate()
	{
		const string MethodName = "IsValidTarget";
		var method = AccessTools.Method(typeof(Projectile), MethodName, [typeof(IDestructible)])
			?? throw new MissingMethodException(typeof(Projectile).FullName, $"{MethodName}({nameof(IDestructible)})");

		return AccessTools.MethodDelegate<IsValidTargetDelegate>(method);
	}

	/// <summary>
	/// Builds hit information for ranged attack.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="state"> Projectile state. </param>
	/// <param name="collider"> Collider. </param>
	/// <param name="hitPoint"> Hit point. </param>
	/// <param name="damageMultiplier"> Damage multiplier per penetration. </param>
	/// <returns> Hit information </returns>
	private static HitData BuildRangedHit(Projectile projectile, ProjectileState state,
		Collider collider, Vector3 hitPoint, float damageMultiplier)
	{
		var hit = state.BaseHitData.Clone();
		hit.m_hitCollider = collider;
		hit.m_point = hitPoint;
		hit.m_dir = projectile.transform.forward;
		hit.m_ranged = true;
		hit.m_hitType = HitData.HitType.PlayerHit;
		hit.m_skill = Skills.SkillType.Bows;
		hit.m_skillLevel = state.Owner.GetSkillLevel(Skills.SkillType.Bows);
		hit.m_damage.Modify(damageMultiplier);
		hit.m_pushForce *= Mathf.Min(damageMultiplier, 1f);
		hit.SetAttacker(state.Owner);

		return hit;
	}

	/// <summary>
	/// Applies vanilla healing on hit.
	/// </summary>
	/// <param name="state"> Projectile state. </param>
	/// <param name="hit"> Hit information. </param>
	private static void ApplyHealthReturn(ProjectileState state, HitData hit)
	{
		if (hit.m_healthReturn <= 0f || !state.Owner)
		{
			return;
		}

		state.Owner.Heal(hit.m_healthReturn);
	}

	/// <summary>
	/// Creates surface hit VFX.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="hitPoint"> Hit point. </param>
	private static void CreateHitEffects(Projectile projectile, Vector3 hitPoint)
	{
		projectile.m_hitEffects.Create(hitPoint, Quaternion.identity);
	}

	/// <summary>
	/// Creates surface hit SFX.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	/// <param name="state"> Projectile state. </param>
	private static void CreateHitNoise(Projectile projectile, ProjectileState state)
	{
		if (projectile.m_hitNoise <= 0f)
		{
			return;
		}

		BaseAI.DoProjectileHitNoise(projectile.transform.position,
			projectile.m_hitNoise, state.Owner);
	}

	/// <summary>
	/// Raises skill on hit, if necessary.
	/// </summary>
	/// <param name="state"> Projectile state. </param>
	/// <param name="hit"> Hit information. </param>
	private static void RaiseSkillIfApplicable(ProjectileState state, HitData hit)
	{
		if ((Plugin.IsSkillGainLimitedToFirstHit.Value && state.CharacterHits > 0)
			|| hit.m_skillRaiseAmount <= 0f
			|| !state.Owner)
		{
			return;
		}

		state.Owner.RaiseSkill(hit.m_skill, hit.m_skillRaiseAmount);
	}

	/// <summary>
	/// Returns name of object being hit.
	/// </summary>
	/// <param name="hitObject"> Object that is being hit. </param>
	/// <param name="collider"> Collider. </param>
	/// <returns> Object name. </returns>
	private static string GetHitObjectName(GameObject hitObject, Collider collider)
	{
		string name = null;

		if (hitObject)
		{
			name = hitObject.name;
		}
		else if (collider)
		{
			name = collider.name;
		}

		return !string.IsNullOrEmpty(name)
			? name
			: "<unknown>";
	}

	/// <summary>
	/// Destroys projectile.
	/// </summary>
	/// <param name="projectile"> Projectile. </param>
	private static void DestroyProjectile(Projectile projectile)
	{
		ProjectileStates.Remove(projectile);

		if (!projectile)
		{
			return;
		}
		if (ZNetScene.instance)
		{
			ZNetScene.instance.Destroy(projectile.gameObject);
			return;
		}

		UnityEngine.Object.Destroy(projectile.gameObject);
	}

	/// <summary>
	/// Returns launch strength (bow charge level).
	/// </summary>
	/// <param name="weapon"> Weapon. </param>
	/// <param name="launchSpeed"> Launch speed of projectile. </param>
	/// <returns> Launch strength. </returns>
	private static float GetLaunchStrength(ItemDrop.ItemData weapon, float launchSpeed)
	{
		if (weapon?.m_shared?.m_attack == null)
		{
			return 1f;
		}

		var attack = weapon.m_shared.m_attack;
		var minVelocity = attack.m_projectileVelMin;
		var maxVelocity = attack.m_projectileVel;

		return maxVelocity <= minVelocity
			? 1f
			: Mathf.InverseLerp(minVelocity, maxVelocity, launchSpeed);
	}

	/// <summary>
	/// Calculates maximum penetrations available for projectile.
	/// </summary>
	/// <param name="launchStrength"> Launch strength (bow charge level). </param>
	/// <returns> Number of penetrations. </returns>
	private static int GetMaxPenetrations(float launchStrength)
	{
		if (!Plugin.IsChargeLimitedPenetrationEnabled.Value)
		{
			return int.MaxValue;
		}

		var unlimitedStrength = Plugin.UnlimitedPenetrationStrength.Value;

		if (launchStrength >= unlimitedStrength
			|| unlimitedStrength <= 0f)
		{
			return int.MaxValue;
		}

		var normalizedStrength = launchStrength / unlimitedStrength;
		return Mathf.FloorToInt(normalizedStrength * Plugin.LowChargePenetrationFactor.Value);
	}

	#endregion
}
