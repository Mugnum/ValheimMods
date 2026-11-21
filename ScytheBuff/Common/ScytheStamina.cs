using HarmonyLib;

namespace Mugnum.ValheimMods.ScytheBuff.Common;

/// <summary>
/// Removes only the local scythe attack's contribution to regeneration delays.
/// </summary>
internal static class ScytheStamina
{
	/// <summary>
	/// Access to the current attack without repeated reflection or equipment searches.
	/// </summary>
	private static AccessTools.FieldRef<Humanoid, Attack> CurrentAttack;

	/// <summary>
	/// Access to the remaining stamina regeneration delay.
	/// </summary>
	private static AccessTools.FieldRef<Player, float> RegenerationTimer;

	/// <summary>
	/// Resolves private fields once while plugin initialization can report layout failures.
	/// </summary>
	internal static void Initialize()
	{
		CurrentAttack = AccessTools.FieldRefAccess<Humanoid, Attack>("m_currentAttack");
		RegenerationTimer = AccessTools.FieldRefAccess<Player, float>("m_staminaRegenTimer");
	}

	/// <summary>
	/// Checks whether regeneration exemptions are enabled for this character.
	/// </summary>
	/// <param name="character"> Character performing the action. </param>
	/// <returns> Can this character receive the scythe exemption. </returns>
	private static bool IsEnabledFor(Character character)
	{
		return Plugin.Enabled.Value
			&& Plugin.RemoveStaminaRegenerationDelay.Value
			&& Player.m_localPlayer
			&& character == Player.m_localPlayer;
	}

	/// <summary>
	/// Spends the configured attack cost without resetting an older regeneration delay.
	/// </summary>
	/// <param name="character"> Attacking character. </param>
	/// <param name="amount"> Calculated attack stamina cost. </param>
	/// <param name="attack"> Attack causing this expenditure. </param>
	internal static void UseAttackStamina(Character character, float amount, Attack attack)
	{
		if (!IsEnabledFor(character) || !ScytheRules.IsScythe(attack.GetWeapon()))
		{
			character.UseStamina(amount);
			return;
		}

		var player = Player.m_localPlayer;
		var previousTimer = RegenerationTimer(player);

		try
		{
			character.UseStamina(amount);
		}
		finally
		{
			// Keep the actual expenditure, but preserve any cooldown from another action.
			RegenerationTimer(player) = previousTimer;
		}
	}

	/// <summary>
	/// Checks attack blocking only for the stamina portion of the regeneration update.
	/// </summary>
	/// <param name="character"> Character whose stamina is being updated. </param>
	/// <returns> Should this attack animation prevent stamina regeneration. </returns>
	internal static bool IsAttackBlockingRegeneration(Character character)
	{
		if (!character.InAttack())
		{
			return false;
		}

		return !IsEnabledFor(character)
			|| !ScytheRules.IsScythe(CurrentAttack(Player.m_localPlayer)?.GetWeapon());
	}
}
