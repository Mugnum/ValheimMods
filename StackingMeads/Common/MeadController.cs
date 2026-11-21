using Mugnum.ValheimMods.StackingMeads.Meads;
using System.Linq;

namespace Mugnum.ValheimMods.StackingMeads.Common;

/// <summary>
/// Manages only the local character's supported meads and vanilla cooldown boundaries.
/// </summary>
internal static class MeadController
{
	/// <summary>
	/// Checks ownership by the actual local player rather than server/host identity.
	/// </summary>
	internal static bool IsLocal(SEMan manager) => Player.m_localPlayer && ReferenceEquals(manager, Player.m_localPlayer.GetSEMan());

	/// <summary>
	/// Resolves an active grouped effect.
	/// </summary>
	internal static StackedMeadEffect GetGroup(Player player, MeadDefinition definition)
	{
		return player.GetSEMan().GetStatusEffect(definition.GroupHash) as StackedMeadEffect;
	}

	/// <summary>
	/// Counts supported drinks sharing the vanilla cooldown category.
	/// </summary>
	internal static int CountClass(SEMan manager, string classKey)
	{
		var count = 0;
		foreach (var effect in manager.GetStatusEffects())
		{
			if (effect.IsDone())
			{
				continue;
			}

			if (effect is StackedMeadEffect group)
			{
				if (group.ClassKey == classKey)
				{
					count += group.State.Count;
				}
			}
			else
			{
				var definition = MeadRegistry.Find(effect);
				if (definition != null && definition.GetClassKey(effect) == classKey)
				{
					count++;
				}
			}
		}

		return count;
	}

	/// <summary>
	/// Replaces only supported-mead duplicate checks inside vanilla CanConsumeItem.
	/// </summary>
	internal static bool HasBlockingEffect(SEMan manager, int hash, ItemDrop.ItemData item)
	{
		var source = item?.m_shared?.m_consumeStatusEffect;
		var definition = MeadRegistry.Find(source);
		return definition != null && IsLocal(manager) && hash == definition.EffectHash
			? CountClass(manager, definition.GetClassKey(source)) >= Plugin.MaxStacks.Value
			: manager.HaveStatusEffect(hash);
	}

	/// <summary>
	/// Preserves cooldowns imposed by unsupported meads in the same category.
	/// </summary>
	internal static bool HasBlockingCategory(SEMan manager, string category, ItemDrop.ItemData item)
	{
		var source = item?.m_shared?.m_consumeStatusEffect;
		var definition = MeadRegistry.Find(source);
		if (definition == null || !IsLocal(manager) || string.IsNullOrEmpty(category))
		{
			return manager.HaveStatusEffectCategory(category);
		}

		foreach (var effect in manager.GetStatusEffects())
		{
			if (effect.IsDone() || effect.m_category != category)
			{
				continue;
			}

			if (effect is StackedMeadEffect || MeadRegistry.Find(effect) != null)
			{
				continue;
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Adds one drink after vanilla consumption eligibility succeeds.
	/// </summary>
	internal static StackedMeadEffect AddDrink(StatusEffect source, MeadDefinition definition)
	{
		var player = Player.m_localPlayer;
		var manager = player.GetSEMan();
		var count = CountClass(manager, definition.GetClassKey(source));
		if (count >= Plugin.MaxStacks.Value || !MeadStack.IsPositiveFinite(source.m_ttl))
		{
			return GetGroup(player, definition);
		}

		var group = AdoptVanillaEffect(player, definition, source);
		var state = group?.State ?? new MeadStackState();
		var stack = MeadStack.Create(source.m_ttl, count, Plugin.DurationMultiplier.Value, Plugin.EffectsMultiplier.Value);
		state.Add(stack);
		group = group ?? Attach(player, definition, source, state);
		group.RefreshDisplay();
		definition.Publish(player, state.TotalStrength);
		Plugin.Diagnostic($"{definition.Id}: active={state.Count}, newDuration={stack.Remaining:R}s, newStrength={stack.Strength:R}, totalStrength={state.TotalStrength:R}.");
		return group;
	}

	/// <summary>
	/// Converts one pre-existing vanilla/LogoutTweaks effect without resetting its timer.
	/// </summary>
	internal static StackedMeadEffect AdoptVanillaEffect(Player player, MeadDefinition definition, StatusEffect source)
	{
		var manager = player.GetSEMan();
		var group = GetGroup(player, definition);
		var vanilla = manager.GetStatusEffect(definition.EffectHash);
		if (vanilla && MeadStack.IsPositiveFinite(vanilla.GetRemaningTime()))
		{
			var state = group?.State ?? new MeadStackState();
			state.Add(new MeadStack(vanilla.GetRemaningTime(), 1f));
			manager.RemoveStatusEffect(vanilla, true);
			group = group ?? Attach(player, definition, source, state);
			group.RefreshDisplay();
			definition.Publish(player, state.TotalStrength);
		}

		return group;
	}

	/// <summary>
	/// Installs a grouped marker through the ordinary SEMan lifetime path.
	/// </summary>
	internal static StackedMeadEffect Attach(Player player, MeadDefinition definition, StatusEffect source, MeadStackState state)
	{
		return (StackedMeadEffect)player.GetSEMan().AddStatusEffect(StackedMeadEffect.Prepare(definition, source, state));
	}

	/// <summary>
	/// Removes stale group markers before an explicit restore.
	/// </summary>
	internal static void RemoveGroups(Player player)
	{
		foreach (var effect in player.GetSEMan().GetStatusEffects().OfType<StackedMeadEffect>().ToArray())
		{
			player.GetSEMan().RemoveStatusEffect(effect, true);
		}
	}

	/// <summary>
	/// Removes local effects on plugin unloading without touching remote characters.
	/// </summary>
	internal static void DetachLocalPlayer()
	{
		if (Player.m_localPlayer)
		{
			MeadPersistence.Save(Player.m_localPlayer);
			RemoveGroups(Player.m_localPlayer);
		}
	}
}
