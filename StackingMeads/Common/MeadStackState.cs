using System;
using System.Collections.Generic;
using System.Linq;

namespace Mugnum.ValheimMods.StackingMeads.Common;

/// <summary>
/// One drink's independent timer and fixed bonus strength.
/// </summary>
internal sealed class MeadStack
{
	/// <summary>
	/// Remaining gameplay seconds.
	/// </summary>
	internal float Remaining;

	/// <summary>
	/// Bonus weight relative to a full-strength drink.
	/// </summary>
	internal readonly float Strength;

	/// <summary>
	/// Creates a validated stack.
	/// </summary>
	internal MeadStack(float remaining, float strength)
	{
		if (!IsPositiveFinite(remaining) || !IsPositiveFinite(strength) || strength > 1f)
		{
			throw new ArgumentOutOfRangeException(nameof(remaining), "Stack duration and strength must be positive finite values; strength cannot exceed one.");
		}

		Remaining = remaining;
		Strength = strength;
	}

	/// <summary>
	/// Checks a persisted or calculated float.
	/// </summary>
	internal static bool IsPositiveFinite(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);

	/// <summary>
	/// Calculates the next drink without modifying existing stacks.
	/// </summary>
	internal static MeadStack Create(float baseDuration, int activeCount, float durationFactor, float effectsFactor)
	{
		var duration = Math.Max(float.Epsilon, (float)(baseDuration * Math.Pow(durationFactor, activeCount)));
		var strength = Math.Max(float.Epsilon, (float)Math.Pow(effectsFactor, activeCount));
		return new MeadStack(duration, strength);
	}
}

/// <summary>
/// Shared timer and additive bonus accounting for a supported mead.
/// </summary>
internal sealed class MeadStackState
{
	/// <summary>
	/// Independent active drinks.
	/// </summary>
	private readonly List<MeadStack> _stacks = [];

	/// <summary>
	/// Active stack count.
	/// </summary>
	internal int Count => _stacks.Count;

	/// <summary>
	/// Longest remaining timer for the grouped icon.
	/// </summary>
	internal float Remaining => _stacks.Count == 0 ? 0f : _stacks.Max(stack => stack.Remaining);

	/// <summary>
	/// Sum of bonus weights; never a product of complete multipliers.
	/// </summary>
	internal float TotalStrength => _stacks.Sum(stack => stack.Strength);

	/// <summary>
	/// Adds a new independent drink.
	/// </summary>
	internal void Add(MeadStack stack) => _stacks.Add(stack);

	/// <summary>
	/// Advances all timers and silently discards expired drinks.
	/// </summary>
	internal int Advance(float dt)
	{
		if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
		{
			return 0;
		}

		foreach (var stack in _stacks)
		{
			stack.Remaining -= dt;
		}

		return _stacks.RemoveAll(stack => stack.Remaining <= 0f);
	}

	/// <summary>
	/// Copies stack data for persistence without sharing mutable timers.
	/// </summary>
	internal List<MeadStack> Snapshot() => _stacks.Select(stack => new MeadStack(stack.Remaining, stack.Strength)).ToList();
}
