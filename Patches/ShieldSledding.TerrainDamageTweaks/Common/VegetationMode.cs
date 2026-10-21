namespace Mugnum.ValheimMods.ShieldSledding.TerrainDamageTweaks.Common;

/// <summary>
/// Sled interactions with bushes and small trees.
/// </summary>
public enum VegetationMode
{
	/// <summary>
	/// Preserve vegetation without changing collision handling.
	/// </summary>
	DisableDamage = 0,

	/// <summary>
	/// Preserve vegetation and pass through eligible small plants.
	/// </summary>
	IgnoreSmallVegetationCollisions = 1,

	/// <summary>
	/// Retain Shield Sledding's original damage.
	/// </summary>
	OriginalDamage = 2
}
