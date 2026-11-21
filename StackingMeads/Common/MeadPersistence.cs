using Mugnum.ValheimMods.StackingMeads.Meads;
using System;
using System.Linq;

namespace Mugnum.ValheimMods.StackingMeads.Common;

/// <summary>
/// Stores independent stacks in the local character, without rewriting LogoutTweaks data.
/// </summary>
internal static class MeadPersistence
{
	/// <summary>
	/// Separate from LogoutTweaks' VV_LogoutData key.
	/// </summary>
	private const string DataKey = "Mugnum.StackingMeads.Stacks.v1";

	/// <summary>
	/// Captures stack state before vanilla serializes custom character data.
	/// </summary>
	internal static void Save(Player player)
	{
		if (!player || player != Player.m_localPlayer)
		{
			return;
		}

		if (!Plugin.SaveOnExit.Value)
		{
			player.m_customData.Remove(DataKey);
			return;
		}

		try
		{
			var groups = player.GetSEMan().GetStatusEffects()
				.OfType<StackedMeadEffect>()
				.Where(effect => effect.State.Count > 0)
				.Select(effect => new SavedMeadGroup { DefinitionId = effect.Definition.Id, Stacks = effect.State.Snapshot() })
				.ToList();

			if (groups.Count == 0)
			{
				player.m_customData.Remove(DataKey);
			}
			else
			{
				player.m_customData[DataKey] = MeadSaveCodec.Encode(groups);
			}

			Plugin.Diagnostic($"Saved {groups.Sum(group => group.Stacks.Count)} mead stacks in {groups.Count} groups for character {player.GetPlayerID()}.");
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"Could not save stacked meads: {ex}");
		}
	}

	/// <summary>
	/// Restores after LogoutTweaks has applied any legacy vanilla effects.
	/// </summary>
	internal static void Load(Player player)
	{
		if (!player || player != Player.m_localPlayer)
		{
			return;
		}

		foreach (var definition in MeadRegistry.Definitions)
		{
			definition.Clear(player);
		}

		if (!Plugin.SaveOnExit.Value)
		{
			player.m_customData.Remove(DataKey);
			return;
		}

		try
		{
			if (player.m_customData.TryGetValue(DataKey, out var data))
			{
				var groups = MeadSaveCodec.Decode(data);
				MeadController.RemoveGroups(player);

				foreach (var saved in groups)
				{
					var definition = MeadRegistry.Find(saved.DefinitionId);
					var source = definition == null ? null : ObjectDB.instance.GetStatusEffect(definition.EffectHash);
					if (!source)
					{
						Plugin.Log.LogWarning($"Skipped saved mead with unavailable definition/effect: {saved.DefinitionId}.");
						continue;
					}

					// Our independent records take priority over a legacy vanilla effect from LogoutTweaks.
					player.GetSEMan().RemoveStatusEffect(definition.EffectHash, true);
					var state = new MeadStackState();
					foreach (var stack in saved.Stacks)
					{
						state.Add(stack);
					}

					MeadController.Attach(player, definition, source, state);
					Plugin.Diagnostic($"Restored {saved.DefinitionId}: active={state.Count}, longest={state.Remaining:R}s, weight={state.TotalStrength:R}.");
				}
			}

			// Also migrate a single pre-installation effect restored by LogoutTweaks, without adding a drink.
			foreach (var definition in MeadRegistry.Definitions)
			{
				var source = ObjectDB.instance.GetStatusEffect(definition.EffectHash);
				if (source)
				{
					MeadController.AdoptVanillaEffect(player, definition, source);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"Could not restore stacked meads; invalid character data was ignored: {ex}");
		}
	}
}
