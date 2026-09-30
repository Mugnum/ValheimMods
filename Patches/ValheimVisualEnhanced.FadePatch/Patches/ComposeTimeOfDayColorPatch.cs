using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace Mugnum.ValheimMods.ValheimVisualEnhanced.FadePatch.Patches;

/// <summary>
/// Patch for "CloudShadows.EnvironmentPaletteController.ComposeTimeOfDayColor" method.
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "InlineTemporaryVariable")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
internal static class ComposeTimeOfDayColorPatch
{
	/// <summary>
	/// Patched method's namespace.
	/// </summary>
	private const string TypeName = "CloudShadows.EnvironmentPaletteController";

	/// <summary>
	/// Patched method name.
	/// </summary>
	private const string MethodName = "ComposeTimeOfDayColor";

	/// <summary>
	/// Retrieves patched method.
	/// </summary>
	/// <returns> Method. </returns>
	/// <exception cref="TypeLoadException"> Type not found. </exception>
	/// <exception cref="MissingMethodException"> Method not found. </exception>
	[UsedImplicitly]
	private static MethodBase TargetMethod()
	{
		var type = AccessTools.TypeByName(TypeName)
			?? throw new TypeLoadException($"Could not find {TypeName}.");

		var method = AccessTools.Method(type, MethodName,
		[
			typeof(Color), // dayColor.
			typeof(Color), // nightColor.
			typeof(Color), // morningColor.
			typeof(Color), // eveningColor.
			typeof(Color), // paletteDayColor.
			typeof(Color), // paletteNightColor.
			typeof(Color), // paletteMorningColor.
			typeof(Color), // paletteEveningColor.
			typeof(float), // dayIntensity.
			typeof(float), // nightIntensity.
			typeof(float), // morningIntensity.
			typeof(float), // eveningIntensity.
			typeof(float)  // strength.
		]);

		return method != null
			? method
			: throw new MissingMethodException($"Could not find {TypeName}.{MethodName}.");
	}

	/// <summary>
	/// Prefix before "ComposeTimeOfDayColor" method execution.
	/// </summary>
	/// <param name="__0"> Day color. </param>
	/// <param name="__1"> Night color. </param>
	/// <param name="__2"> Morning color. </param>
	/// <param name="__3"> Evening color. </param>
	/// <param name="__4"> Palette day color. </param>
	/// <param name="__5"> Palette night color. </param>
	/// <param name="__6"> Palette morning color. </param>
	/// <param name="__7"> Palette evening color. </param>
	/// <param name="__8"> Day intensity. </param>
	/// <param name="__9"> Night intensity. </param>
	/// <param name="__10"> Morning intensity. </param>
	/// <param name="__11"> Evening intensity. </param>
	/// <param name="__12"> Strength. </param>
	/// <param name="__result"> Calculated color. </param>
	/// <returns> Flag indicating need to execute original method. </returns>
	[UsedImplicitly]
	private static bool Prefix(Color __0, Color __1, Color __2, Color __3,
		Color __4, Color __5, Color __6, Color __7,
		float __8, float __9, float __10, float __11,
		float __12, ref Color __result)
	{
		var dayColor = __0;
		var nightColor = __1;
		var morningColor = __2;
		var eveningColor = __3;
		var paletteDayColor = __4;
		var paletteNightColor = __5;
		var paletteMorningColor = __6;
		var paletteEveningColor = __7;
		var dayIntensity = __8;
		var nightIntensity = __9;
		var morningIntensity = __10;
		var eveningIntensity = __11;
		var strength = __12;
		__result = BlendRgb(dayColor, paletteDayColor, strength) * dayIntensity +
			BlendRgb(nightColor, paletteNightColor, strength) * nightIntensity +
			BlendRgb(morningColor, paletteMorningColor, strength) * morningIntensity +
			BlendRgb(eveningColor, paletteEveningColor, strength) * eveningIntensity;

		return false;
	}

	/// <summary>
	/// Blends RGB color.
	/// </summary>
	/// <param name="source"> Source color. </param>
	/// <param name="target"> Target color. </param>
	/// <param name="strength"> Strength. </param>
	/// <returns> Blended color. </returns>
	private static Color BlendRgb(
		Color source,
		Color target,
		float strength)
	{
		return new Color(Mathf.Lerp(source.r, target.r, strength),
			Mathf.Lerp(source.g, target.g, strength),
			Mathf.Lerp(source.b, target.b, strength),
			source.a);
	}
}
