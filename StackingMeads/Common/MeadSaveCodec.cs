using System;
using System.Collections.Generic;
using System.IO;

namespace Mugnum.ValheimMods.StackingMeads.Common;

/// <summary>
/// A supported mead's saved independent drinks.
/// </summary>
internal sealed class SavedMeadGroup
{
	/// <summary>
	/// Definition Id, independent of localized display names.
	/// </summary>
	internal string DefinitionId;

	/// <summary>
	/// Independent remaining timers and bonus weights.
	/// </summary>
	internal List<MeadStack> Stacks;
}

/// <summary>
/// Versioned, culture-independent character data separate from LogoutTweaks.
/// </summary>
internal static class MeadSaveCodec
{
	/// <summary>
	/// Encodes independent stacks without storing absolute wall-clock times.
	/// </summary>
	internal static string Encode(IReadOnlyList<SavedMeadGroup> groups)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		writer.Write(1);
		writer.Write(groups.Count);

		foreach (var group in groups)
		{
			writer.Write(group.DefinitionId);
			writer.Write(group.Stacks.Count);

			foreach (var stack in group.Stacks)
			{
				writer.Write(stack.Remaining);
				writer.Write(stack.Strength);
			}
		}

		writer.Flush();
		return Convert.ToBase64String(stream.ToArray());
	}

	/// <summary>
	/// Decodes bounded, validated data before any live effects are changed.
	/// </summary>
	internal static List<SavedMeadGroup> Decode(string data)
	{
		if (data == null || data.Length > 65536)
		{
			throw new InvalidDataException("StackingMeads data is missing or too large.");
		}

		using var stream = new MemoryStream(Convert.FromBase64String(data));
		using var reader = new BinaryReader(stream);

		if (reader.ReadInt32() != 1)
		{
			throw new InvalidDataException("Unsupported StackingMeads data version.");
		}

		var groupCount = reader.ReadInt32();
		if (groupCount < 0 || groupCount > 32)
		{
			throw new InvalidDataException("Invalid mead group count.");
		}

		var groups = new List<SavedMeadGroup>();
		var ids = new HashSet<string>(StringComparer.Ordinal);

		for (var i = 0; i < groupCount; i++)
		{
			var id = reader.ReadString();
			var count = reader.ReadInt32();
			if (id.Length == 0 || id.Length > 128 || !ids.Add(id) || count < 1 || count > 50)
			{
				throw new InvalidDataException("Invalid mead group or stack count.");
			}

			var stacks = new List<MeadStack>();
			for (var j = 0; j < count; j++)
			{
				stacks.Add(new MeadStack(reader.ReadSingle(), reader.ReadSingle()));
			}

			groups.Add(new SavedMeadGroup { DefinitionId = id, Stacks = stacks });
		}

		if (stream.Position != stream.Length)
		{
			throw new InvalidDataException("Unexpected trailing StackingMeads data.");
		}

		return groups;
	}
}
