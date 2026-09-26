namespace Mugnum.ValheimMods.ProjectilePenetration.Modules.KillRewards;

/// <summary>
/// State for ranged kill damage.
/// </summary>
/// <param name="wasAlive"> Indicates whether target was alive. </param>
/// <param name="attacker"> Attacker. </param>
/// <param name="isEligible"> Is kill eligible for reward. </param>
internal readonly struct RangedKillDamageState(
	bool wasAlive, Player attacker, bool isEligible)
{
	/// <summary>
	/// Indicates whether target was alive.
	/// </summary>
	public bool WasAlive { get; } = wasAlive;

	/// <summary>
	/// Attacker.
	/// </summary>
	public Player Attacker { get; } = attacker;

	/// <summary>
	/// Is kill eligible for reward.
	/// </summary>
	public bool IsEligible { get; } = isEligible;
}
