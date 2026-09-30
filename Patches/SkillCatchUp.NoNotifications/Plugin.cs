using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.SkillCatchUp.NoNotifications;

/// <summary>
/// Patch for SkillCatchUp, reducing vanilla skill level-up notification spam
/// while a skill is recovering toward its remembered level.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MainPluginGuid, MainPluginVersion)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Plugin : BaseUnityPlugin
{
	#region Constants

	/// <summary>
	/// Plugin Id.
	/// </summary>
	public const string PluginGuid = "Mugnum.SkillCatchUp.NoNotifications";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "SkillCatchUp NoNotifications Patch";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.0.0";

	/// <summary>
	/// SkillCatchUp plugin Id.
	/// </summary>
	private const string MainPluginGuid = "qua8ion.valheim.skillcatchup";

	/// <summary>
	/// SkillCatchUp plugin version.
	/// </summary>
	private const string MainPluginVersion = "1.0.0";

	/// <summary>
	/// Floating point tolerance used by SkillCatchUp when comparing
	/// current skill position against its recovery target.
	/// </summary>
	private const float RecoveryEpsilon = 0.0005f;

	#region Fields

	/// <summary>
	/// Logger.
	/// </summary>
	private static ManualLogSource Log;

	/// <summary>
	/// Notification interval during skill catch-up.
	/// </summary>
	private static ConfigEntry<int> NotificationInterval;

	/// <summary>
	/// SkillCatchUp master enabled configuration.
	/// </summary>
	private static ConfigEntry<bool> SkillCatchUpEnabled;

	/// <summary>
	/// Delegate for reading SkillCatchUp recovery targets.
	/// </summary>
	private static ReadTargetsDelegate ReadTargets;

	/// <summary>
	/// Current RaiseSkill context.
	/// Thread-static so nested or unrelated calls on another thread cannot
	/// observe the local player's notification state.
	/// </summary>
	[ThreadStatic]
	private static RaiseContext CurrentRaiseContext;

	/// <summary>
	/// Harmony instance.
	/// </summary>
	private Harmony _harmony;

	#endregion

	#endregion

	#region Types

	/// <summary>
	/// Delegate for "SkillCatchUp.Targets.Read".
	/// </summary>
	private delegate Dictionary<int, float> ReadTargetsDelegate(Player player);

	#endregion

	#region Methods: Private

	/// <summary>
	/// Initializes mod.
	/// </summary>
	private void Awake()
	{
		Log = Logger;
		NotificationInterval = Config.Bind("Notifications", "CatchUpLevelInterval", 5,
			new ConfigDescription("Display notification every N levels while SkillCatchUp is active.",
				new AcceptableValueRange<int>(1, 100)));

		var skillCatchUpPluginType = AccessTools.TypeByName("SkillCatchUp.SkillCatchUpPlugin");
		var targetsType = AccessTools.TypeByName("SkillCatchUp.Targets");

		if (skillCatchUpPluginType == null || targetsType == null)
		{
			Logger.LogError("Patch failed to apply: Couldn't find expected types.");
			return;
		}

		var enabledField = AccessTools.Field(skillCatchUpPluginType, "Enabled");
		var readTargetsMethod = AccessTools.Method(targetsType, "Read", [typeof(Player)]);

		if (enabledField == null || readTargetsMethod == null)
		{
			Logger.LogError("Patch failed to apply: SkillCatchUp internals don't match expected layout.");
			return;
		}

		try
		{
			SkillCatchUpEnabled = enabledField.GetValue(null) as ConfigEntry<bool>;
			ReadTargets = (ReadTargetsDelegate)Delegate.CreateDelegate(
				typeof(ReadTargetsDelegate), readTargetsMethod);
		}
		catch (Exception ex)
		{
			Logger.LogError($"Patch failed to apply while binding SkillCatchUp internals:\r\n{ex}");
			return;
		}

		if (SkillCatchUpEnabled == null || ReadTargets == null)
		{
			Logger.LogError("Patch failed to apply: Couldn't bind SkillCatchUp configuration or target reader.");
			return;
		}

		var raiseSkillMethod = AccessTools.Method(typeof(Skills), nameof(Skills.RaiseSkill),
		[
			typeof(Skills.SkillType),
			typeof(float)
		]);
		var characterMessageMethod = AccessTools.Method(typeof(Character), nameof(Character.Message),
		[
			typeof(MessageHud.MessageType),
			typeof(string),
			typeof(int),
			typeof(Sprite),
			typeof(bool)
		]);
		var effectListCreateMethod = AccessTools.Method(typeof(EffectList), nameof(EffectList.Create),
		[
			typeof(Vector3),
			typeof(Quaternion),
			typeof(Transform),
			typeof(float),
			typeof(int),
			typeof(ZDOID)
		]);

		if (raiseSkillMethod == null
			|| characterMessageMethod == null
			|| effectListCreateMethod == null)
		{
			Logger.LogError("Patch failed to apply: Valheim methods don't match expected layout.");
			return;
		}

		_harmony = new Harmony(PluginGuid);

		try
		{
			_harmony.Patch(raiseSkillMethod,
				prefix: new HarmonyMethod(
					AccessTools.Method(typeof(Plugin), nameof(SkillsRaiseSkillPrefix))),
				finalizer: new HarmonyMethod(
					AccessTools.Method(typeof(Plugin), nameof(SkillsRaiseSkillFinalizer))));

			_harmony.Patch(characterMessageMethod,
				prefix: new HarmonyMethod(
					AccessTools.Method(typeof(Plugin),nameof(CharacterMessagePrefix))));

			_harmony.Patch(effectListCreateMethod,
				prefix: new HarmonyMethod(
					AccessTools.Method(typeof(Plugin), nameof(EffectListCreatePrefix))));
		}
		catch (Exception ex)
		{
			_harmony.UnpatchSelf();
			Logger.LogError($"Patch failed to apply:\r\n{ex}");
			return;
		}

		Logger.LogInfo("SkillCatchUp notifications patched: " +
			$"normal level-up feedback will be shown every {NotificationInterval.Value} " +
			"levels while recovering.");
	}

	/// <summary>
	/// Cleans up Harmony patches.
	/// </summary>
	private void OnDestroy()
	{
		CurrentRaiseContext = default;
		_harmony?.UnpatchSelf();
	}

	/// <summary>
	/// Prefix for <see cref="Skills.RaiseSkill"/>.
	/// Establishes context used by the notification patches.
	/// </summary>
	/// <param name="__instance"> Skills instance. </param>
	/// <param name="__0"> Skill type. </param>
	/// <param name="__state"> Previous context, for nested calls. </param>
	private static void SkillsRaiseSkillPrefix(Skills __instance,
		Skills.SkillType __0, out RaiseContext __state)
	{
		var skillType = __0;
		__state = CurrentRaiseContext;
		CurrentRaiseContext = default;

		try
		{
			CurrentRaiseContext = CreateRaiseContext(__instance, skillType);
		}
		catch (Exception ex)
		{
			Log.LogError($"Failed to establish SkillCatchUp notification context:\r\n{ex}");
		}
	}

	/// <summary>
	/// Finalizer for <see cref="Skills.RaiseSkill"/>.
	/// Restores previous context even if RaiseSkill throws.
	/// </summary>
	/// <param name="__state"> Previous context. </param>
	/// <param name="__exception"> Original exception. </param>
	/// <returns> Original exception. </returns>
	private static Exception SkillsRaiseSkillFinalizer(
		RaiseContext __state,
		Exception __exception)
	{
		CurrentRaiseContext = __state;
		return __exception;
	}

	/// <summary>
	/// Prefix for <see cref="Character.Message"/>.
	/// Suppresses vanilla skill-up text and icon on non-milestone
	/// SkillCatchUp levels.
	/// </summary>
	/// <param name="__instance"> Character receiving message. </param>
	/// <param name="__1"> Message text/token. </param>
	/// <returns> Whether original method should execute. </returns>
	private static bool CharacterMessagePrefix(Character __instance, string __1)
	{
		if (!ShouldSuppressCurrentLevelUp())
		{
			return true;
		}

		var message = __1;
		var player = Player.m_localPlayer;

		if (!player || __instance != player)
		{
			return true;
		}

		// Only suppress Valheim's own skill level-up notification.
		// SkillCatchUp's separate "Skill recovered" message is intentionally
		// left untouched.
		return string.IsNullOrEmpty(message)
			|| !message.StartsWith("$msg_skillup", StringComparison.Ordinal);
	}

	/// <summary>
	/// Prefix for the current Valheim <see cref="EffectList.Create"/> overload.
	/// Suppresses the player's skill level-up VFX and audio on non-milestone
	/// SkillCatchUp levels.
	/// </summary>
	/// <param name="__instance"> Effect list being created. </param>
	/// <param name="__result"> Empty result when effect creation is suppressed. </param>
	/// <returns> Whether original method should execute. </returns>
	private static bool EffectListCreatePrefix(EffectList __instance, ref GameObject[] __result)
	{
		if (!ShouldSuppressCurrentLevelUp())
		{
			return true;
		}

		var player = Player.m_localPlayer;

		if (!player || !ReferenceEquals(__instance, player.m_skillLevelupEffects))
		{
			return true;
		}

		__result = [];
		return false;
	}

	/// <summary>
	/// Creates notification context if this RaiseSkill call is currently
	/// inside SkillCatchUp recovery.
	/// </summary>
	/// <param name="skills"> Skills instance. </param>
	/// <param name="skillType"> Skill being raised. </param>
	/// <returns> Catch-up context or default context. </returns>
	private static RaiseContext CreateRaiseContext(Skills skills, Skills.SkillType skillType)
	{
		if (skills == null
			|| NotificationInterval.Value <= 1
			|| !SkillCatchUpEnabled.Value)
		{
			return default;
		}

		var player = Player.m_localPlayer;

		if (!player || player.GetSkills() != skills)
		{
			return default;
		}

		var targets = ReadTargets(player);

		if (targets == null
			|| !targets.TryGetValue((int)skillType, out var target)
			|| target <= 0f)
		{
			return default;
		}

		var skill = FindSkill(skills, skillType);
		var position = skill != null
			? GetSkillPosition(skill)
			: 0f;

		return !IsBelowTarget(position, target)
			? default
			: new RaiseContext(skills, skillType, target);
	}

	/// <summary>
	/// Determines whether the current vanilla skill level-up notification
	/// should be suppressed.
	/// </summary>
	/// <returns> Whether notification should be hidden. </returns>
	private static bool ShouldSuppressCurrentLevelUp()
	{
		var context = CurrentRaiseContext;

		if (!context.IsActive)
		{
			return false;
		}

		var interval = NotificationInterval.Value;

		if (interval <= 1)
		{
			return false;
		}

		var skill = FindSkill(context.Skills, context.SkillType);

		if (skill == null)
		{
			return false;
		}

		var position = GetSkillPosition(skill);

		if (!IsBelowTarget(position, context.Target))
		{
			return false;
		}

		var level = Mathf.FloorToInt(skill.m_level + 0.0001f);
		return level <= 0 || level % interval != 0;
	}

	/// <summary>
	/// Finds skill data.
	/// </summary>
	/// <param name="skills"> Skills collection. </param>
	/// <param name="skillType"> Skill type. </param>
	/// <returns> Skill data or null. </returns>
	private static Skills.Skill FindSkill(Skills skills, Skills.SkillType skillType)
	{
		return skills.GetSkillList()
			.FirstOrDefault(skill => skill?.m_info != null && skill.m_info.m_skill == skillType);
	}

	/// <summary>
	/// Gets SkillCatchUp-compatible skill position.
	/// </summary>
	/// <param name="skill"> Skill data. </param>
	/// <returns> Current fractional skill position. </returns>
	private static float GetSkillPosition(Skills.Skill skill)
	{
		return skill.m_level + skill.GetLevelPercentage();
	}

	/// <summary>
	/// Checks whether a skill remains below its remembered target.
	/// Mirrors SkillCatchUp's recovery comparison.
	/// </summary>
	/// <param name="position"> Current fractional skill position. </param>
	/// <param name="target"> Remembered target. </param>
	/// <returns> Whether skill is still catching up. </returns>
	private static bool IsBelowTarget(float position, float target)
	{
		return target > 0f
			&& position < target - RecoveryEpsilon;
	}

	#endregion
}
