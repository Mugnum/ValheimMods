using Mugnum.ValheimMods.StackingMeads.Common;
using UnityEngine;

namespace Mugnum.ValheimMods.StackingMeads.Meads;

/// <summary>
/// Brew of animal whispers: adds taming bonuses over the creature's baseline rate.
/// </summary>
internal sealed class AnimalWhispersMead : MeadDefinition
{
	/// <summary>
	/// Per-player aggregate bonus on the player's own network object.
	/// </summary>
	private static readonly int StrengthKey = "Mugnum.StackingMeads.AnimalWhispers.Strength".GetStableHashCode();

	internal override string Id => "AnimalWhispers";
	internal override string EffectName => "Potion_tamer";

	/// <summary>
	/// Publishes local gameplay state without syncing configuration.
	/// </summary>
	internal override void Publish(Player player, float strength)
	{
		if (!player || player != Player.m_localPlayer)
		{
			return;
		}

		var view = player.GetComponent<ZNetView>();
		if (view && view.IsValid() && view.IsOwner() && view.GetZDO().GetFloat(StrengthKey, -1f) != strength)
		{
			view.GetZDO().Set(StrengthKey, strength);
		}
	}

	/// <summary>
	/// Resolves the bonus for the actual nearby player, including remote players.
	/// </summary>
	internal static float GetTamingMultiplier(float vanillaMultiplier, Character character)
	{
		var strength = -1f;
		if (character && character == Player.m_localPlayer)
		{
			var effect = MeadController.GetGroup(Player.m_localPlayer, MeadRegistry.AnimalWhispers);
			if (effect != null)
			{
				strength = effect.State.TotalStrength;
			}
		}

		if (strength < 0f && character)
		{
			var view = character.GetComponent<ZNetView>();
			if (view && view.IsValid())
			{
				strength = view.GetZDO().GetFloat(StrengthKey, -1f);
			}
		}

		if (float.IsNaN(strength) || float.IsInfinity(strength) || strength <= 0f || strength > 50f)
		{
			return vanillaMultiplier;
		}

		// Future integer-valued adapters should sum fractional bonuses before rounding once.
		var result = 1f + (vanillaMultiplier - 1f) * strength;
		Plugin.Diagnostic($"Animal whispers: player={character.GetInstanceID()}, weight={strength:R}, vanilla={vanillaMultiplier:R}, result={result:R}.");
		return result;
	}
}
