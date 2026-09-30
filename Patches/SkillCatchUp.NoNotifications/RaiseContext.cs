namespace Mugnum.ValheimMods.SkillCatchUp.NoNotifications;

/// <summary>
/// Information about the currently executing Skills.RaiseSkill call.
/// </summary>
/// <param name="skills"> Skills collection. </param>
/// <param name="skillType"> Skill type. </param>
/// <param name="target"> SkillCatchUp remembered position. </param>
internal readonly struct RaiseContext(Skills skills,
	Skills.SkillType skillType, float target)
{
	/// <summary>
	/// Skills collection being modified.
	/// </summary>
	public Skills Skills { get; } = skills;

	/// <summary>
	/// Skill being modified.
	/// </summary>
	public Skills.SkillType SkillType { get; } = skillType;

	/// <summary>
	/// SkillCatchUp remembered position.
	/// </summary>
	public float Target { get; } = target;

	/// <summary>
	/// Is context active.
	/// </summary>
	public bool IsActive => Skills != null && Target > 0f;
}
