using Mugnum.ValheimMods.StackingMeads.Common;

namespace Mugnum.ValheimMods.StackingMeads.Meads;

/// <summary>
/// Connects one supported mead to shared stack accounting and its specific gameplay adapter.
/// </summary>
internal abstract class MeadDefinition
{
	/// <summary>
	/// Stable persistence Id.
	/// </summary>
	internal abstract string Id { get; }

	/// <summary>
	/// Exact vanilla status effect name; other meads remain unsupported.
	/// </summary>
	internal abstract string EffectName { get; }

	/// <summary>
	/// Vanilla effect hash.
	/// </summary>
	internal int EffectHash => EffectName.GetStableHashCode();

	/// <summary>
	/// Private effect hash, deliberately outside LogoutTweaks' supported vanilla effects.
	/// </summary>
	internal int GroupHash => (Plugin.PluginGuid + "." + Id).GetStableHashCode();

	/// <summary>
	/// Shared vanilla cooldown category, or this effect when no category is set.
	/// </summary>
	internal string GetClassKey(StatusEffect source) => string.IsNullOrEmpty(source.m_category) ? EffectName : source.m_category;

	/// <summary>
	/// Publishes only this local player's aggregate gameplay bonus.
	/// </summary>
	internal abstract void Publish(Player player, float strength);

	/// <summary>
	/// Clears this adapter's network value.
	/// </summary>
	internal void Clear(Player player) => Publish(player, 0f);
}

/// <summary>
/// Explicit opt-in registry for future meads and their gameplay adapters.
/// </summary>
internal static class MeadRegistry
{
	/// <summary>
	/// Animal whispers adapter, independently addressable by its gameplay patch.
	/// </summary>
	internal static readonly AnimalWhispersMead AnimalWhispers = new AnimalWhispersMead();

	/// <summary>
	/// Supported definitions; adding an entry also requires its gameplay adapter.
	/// </summary>
	internal static readonly MeadDefinition[] Definitions = [AnimalWhispers];

	/// <summary>
	/// Finds a supported vanilla consume effect.
	/// </summary>
	internal static MeadDefinition Find(StatusEffect source)
	{
		if (!source)
		{
			return null;
		}

		foreach (var definition in Definitions)
		{
			if (source.NameHash() == definition.EffectHash)
			{
				return definition;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds a persisted definition without relying on localized names.
	/// </summary>
	internal static MeadDefinition Find(string id)
	{
		foreach (var definition in Definitions)
		{
			if (definition.Id == id)
			{
				return definition;
			}
		}

		return null;
	}
}
