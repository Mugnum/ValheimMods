using Mugnum.ValheimMods.StackingMeads.Meads;
using System.Collections.Generic;
using UnityEngine;

namespace Mugnum.ValheimMods.StackingMeads.Common;

/// <summary>
/// One HUD effect backed by independent drink timers and bonus weights.
/// </summary>
internal sealed class StackedMeadEffect : StatusEffect
{
	/// <summary>
	/// One Unity template per definition; SEMan clones its managed state for the player.
	/// </summary>
	private static readonly Dictionary<string, StackedMeadEffect> Templates = [];

	/// <summary>
	/// Specific mead adapter.
	/// </summary>
	internal MeadDefinition Definition;

	/// <summary>
	/// Vanilla shared cooldown key.
	/// </summary>
	internal string ClassKey;

	/// <summary>
	/// Independent stack state belonging to this effect instance.
	/// </summary>
	internal MeadStackState State;

	/// <summary>
	/// Unmodified display name for returning to one active stack.
	/// </summary>
	private string _baseName;

	/// <summary>
	/// Prepares a template; existing runtime clones retain their own state references.
	/// </summary>
	internal static StackedMeadEffect Prepare(MeadDefinition definition, StatusEffect source, MeadStackState state)
	{
		if (!Templates.TryGetValue(definition.Id, out var template) || !template)
		{
			template = CreateInstance<StackedMeadEffect>();
			template.name = Plugin.PluginGuid + "." + definition.Id;
			Templates[definition.Id] = template;
		}

		template.Definition = definition;
		template.State = state;
		template.ClassKey = definition.GetClassKey(source);
		template._baseName = source.m_name;
		template.m_category = source.m_category;
		template.m_icon = source.m_icon;
		template.m_tooltip = source.m_tooltip;
		template.m_attributes = source.m_attributes;
		template.RefreshDisplay();
		return template;
	}

	/// <summary>
	/// Updates grouped text without flashing or posting notifications.
	/// </summary>
	internal void RefreshDisplay()
	{
		m_name = State.Count > 1 ? $"{_baseName} (x{State.Count})" : _baseName;
		m_ttl = State.Remaining;
		m_time = 0f;
		m_isNew = false;
	}

	/// <summary>
	/// Associates the effect with its actual player.
	/// </summary>
	public override void Setup(Character character)
	{
		base.Setup(character);
		Definition.Publish(character as Player, State.TotalStrength);
	}

	/// <summary>
	/// Advances only gameplay time; older stacks retain their original strengths.
	/// </summary>
	public override void UpdateStatusEffect(float dt)
	{
		var expired = State.Advance(dt);
		RefreshDisplay();
		Definition.Publish(m_character as Player, State.TotalStrength);
		if (expired > 0)
		{
			Plugin.Diagnostic($"{Definition.Id}: expired={expired}, active={State.Count}, weight={State.TotalStrength:R}.");
		}
	}

	/// <summary>
	/// A zero-stack group must expire even though its displayed TTL is zero.
	/// </summary>
	public override bool IsDone() => State.Count == 0;

	/// <summary>
	/// Displays the longest independent timer.
	/// </summary>
	public override string GetIconText() => GetTimeString(State.Remaining, alwaysShowMinutes: true);

	/// <summary>
	/// Clears the published bonus when removed, including normal death and effect clearing.
	/// </summary>
	public override void Stop()
	{
		Definition.Clear(m_character as Player);
		base.Stop();
	}
}
