using System;

namespace Mugnum.ValheimMods.ScytheBuff.Common;

/// <summary>
/// Identifies scythes and adjusts crop bonus probability only during scythe swings.
/// </summary>
internal static class ScytheRules
{
	/// <summary>
	/// Player executing a qualifying scythe swing on the current thread.
	/// </summary>
	[ThreadStatic]
	internal static Player SwingHarvester;

	/// <summary>
	/// Checks the item's scythe animation category without prefab or localization lookups.
	/// </summary>
	/// <param name="item"> Item to check. </param>
	/// <returns> Is this item a scythe. </returns>
	internal static bool IsScythe(ItemDrop.ItemData item)
	{
		return item?.m_shared?.m_animationState == ItemDrop.ItemData.AnimationState.Scythe;
	}

	/// <summary>
	/// Scales the crop's existing maximum bonus chance before vanilla multiplies it by skill.
	/// </summary>
	/// <param name="chance"> Original crop bonus chance at maximum Farming skill. </param>
	/// <param name="crop"> Pickable being harvested. </param>
	/// <param name="character"> Character harvesting the crop. </param>
	/// <returns> Original or increased maximum bonus chance. </returns>
	internal static float AdjustBonusChance(float chance, Pickable crop, Humanoid character)
	{
		if (SwingHarvester is null
			|| !ReferenceEquals(character, SwingHarvester)
			|| !Plugin.Enabled.Value
			|| !crop.m_harvestable
			|| crop.m_pickRaiseSkill != Skills.SkillType.Farming)
		{
			return chance;
		}

		// The attack patch establishes this scope only for the actual scythe swing.
		// Manual and MassFarming pickup exit before any equipment or inventory evaluation.
		return chance * (1f + Plugin.BonusYieldChanceIncrease.Value * 0.01f);
	}
}
