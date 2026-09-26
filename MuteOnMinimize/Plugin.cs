using BepInEx;
using UnityEngine;

namespace Mugnum.ValheimMods.MuteOnMinimize;

/// <summary>
/// Audio manager for muting game on minimize.
/// </summary>
[BepInPlugin(PluginId, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
	#region Constants

	/// <summary>
	/// Plugin identifier.
	/// </summary>
	private const string PluginId = "Mugnum.MuteOnMinimize";

	/// <summary>
	/// Plugin name.
	/// </summary>
	public const string PluginName = "Mute On Minimize";

	/// <summary>
	/// Plugin version.
	/// </summary>
	public const string PluginVersion = "1.1.0";

	#endregion Constants

	#region Methods: Private

	/// <summary>
	/// Process intialization of the mod.
	/// </summary>
	private void Awake()
	{
		Application.focusChanged -= OnFocusChanged;
		Application.focusChanged += OnFocusChanged;
	}

	/// <summary>
	/// Process unloading of the mod.
	/// </summary>
	private void OnDestroy()
	{
		Application.focusChanged -= OnFocusChanged;
	}

	/// <summary>
	/// Handle focus change.
	/// </summary>
	/// <param name="isFocused"> Is game in focus (not minimized). </param>
	private static void OnFocusChanged(bool isFocused)
	{
		AudioListener.pause = !isFocused;
	}

	#endregion
}
