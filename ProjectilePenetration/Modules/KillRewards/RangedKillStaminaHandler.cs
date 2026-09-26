namespace Mugnum.ValheimMods.ProjectilePenetration.Modules.KillRewards;

/// <summary>
/// Handler for stamina recovery on ranged kill.
/// </summary>
internal static class RangedKillStaminaHandler
{
	/// <summary>
	/// Handles before event of applying damage.
	/// </summary>
	/// <param name="target"> Target. </param>
	/// <param name="hit"> Hit information. </param>
	/// <returns> Ranged kill state. </returns>
	internal static RangedKillDamageState HandleBeforeDamage(Character target, HitData hit)
	{
		if (!Plugin.IsModEnabled.Value
			|| !Plugin.IsKillRewardsEnabled.Value
		    || !target
		    || hit is not { m_ranged: true })
		{
			return default;
		}

		var attacker = hit.GetAttacker() as Player;

		if (!attacker)
		{
			return default;
		}

		var wasAlive = !target.IsDead() && target.GetHealth() > 0f;
		return !wasAlive
			? default
			: new RangedKillDamageState(true, attacker, true);
	}

	/// <summary>
	/// Handles after event of applying damage.
	/// </summary>
	/// <param name="target"> Target. </param>
	/// <param name="state"> Ranged kill state. </param>
	internal static void HandleAfterDamage(Character target, RangedKillDamageState state)
	{
		if (!state.IsEligible
			|| !state.WasAlive
			|| !state.Attacker
			|| !target)
		{
			return;
		}

		var isDead = target.IsDead() || target.GetHealth() <= 0f;
		var staminaToRestore = Plugin.RestoredStaminaOnRangedKill.Value;

		if (!isDead || staminaToRestore <= 0)
		{
			return;
		}

		// TODO: multiplayer would need different solution.
		if (state.Attacker != Player.m_localPlayer)
		{
			Plugin.LogDebug($"RANGED-KILL remote attacker " +
				$"target=\"{target.name}\" " +
				$"attacker=\"{state.Attacker.GetPlayerName()}\" " +
				$"staminaRewardSkipped={staminaToRestore:F1}");

			return;
		}

		state.Attacker.AddStamina(staminaToRestore);
		Plugin.LogDebug($"RANGED-KILL " +
			$"target=\"{target.name}\" " +
			$"attacker=\"{state.Attacker.GetPlayerName()}\" " +
			$"staminaRestored={staminaToRestore:F1}");
	}
}
