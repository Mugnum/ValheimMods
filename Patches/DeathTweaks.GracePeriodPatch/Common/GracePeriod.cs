using DeathTweaksPlugin = global::DeathTweaks.Plugin;
using HarmonyLib;
using System;

namespace Mugnum.ValheimMods.DeathTweaks.GracePeriodPatch.Common;

/// <summary>
/// Detects vanilla grace-period protection and preserves it through one death operation.
/// </summary>
internal static class GracePeriod
{
	/// <summary>
	/// Access to the unmodified vanilla death timer.
	/// </summary>
	private static AccessTools.FieldRef<Player, float> TimeSinceDeath;

	/// <summary>
	/// Player whose current death started with grace-period protection.
	/// Thread-local so nested deaths can save and restore their own context.
	/// </summary>
	[ThreadStatic]
	private static Player ProtectedPlayer;

	/// <summary>
	/// Whether item destruction should be prevented in the current death operation.
	/// </summary>
	internal static bool IsProtectedDeath => ProtectedPlayer;

	/// <summary>
	/// Resolves the original death timer without calling the patched HardDeath method.
	/// </summary>
	internal static void Initialize()
	{
		TimeSinceDeath = AccessTools.FieldRefAccess<Player, float>("m_timeSinceDeath");
	}

	/// <summary>
	/// Checks the active buff or the vanilla grace window that DeathTweaks can otherwise disable.
	/// </summary>
	/// <param name="player"> Player being checked. </param>
	/// <returns> Is grace-period protection active. </returns>
	internal static bool IsActive(Player player)
	{
		if (!DeathTweaksPlugin.Enabled || !player || !player.IsOwner())
		{
			return false;
		}

		return player.GetSEMan()?.HaveStatusEffect(SEMan.s_statusEffectSoftDeath) == true
			|| TimeSinceDeath(player) <= player.m_hardDeathCooldown;
	}

	/// <summary>
	/// Captures protection before death removes buffs or resets the death timer.
	/// </summary>
	/// <param name="player"> Dying player. </param>
	/// <returns> Previous context for a nested death. </returns>
	internal static Player Enter(Player player)
	{
		var previous = ProtectedPlayer;
		ProtectedPlayer = IsActive(player) ? player : null;
		return previous;
	}

	/// <summary>
	/// Checks whether the current death protects this particular skill collection.
	/// </summary>
	/// <param name="skills"> Skills being reduced or cleared. </param>
	/// <returns> Should this skill collection be preserved. </returns>
	internal static bool Protects(Skills skills)
	{
		return ProtectedPlayer && ProtectedPlayer.GetSkills() == skills;
	}

	/// <summary>
	/// Restores the enclosing context, including after an exception.
	/// </summary>
	/// <param name="previous"> Context captured on entry. </param>
	internal static void Exit(Player previous)
	{
		ProtectedPlayer = previous;
	}

	/// <summary>
	/// Clears any current context on plugin unloading.
	/// </summary>
	internal static void Reset()
	{
		ProtectedPlayer = null;
	}
}
