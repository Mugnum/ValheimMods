using System;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> ZDO IDs are reassigned on world load; custom ZDO strings survive those loads. </summary>
internal static class ReliquaryIdentity
{
	private const string Key = "Mugnum.LifeTotems.ReliquaryId";
	internal static bool IsStable(string id) => id.StartsWith("reliquary:", StringComparison.Ordinal);

	internal static string Get(ZDO zdo)
	{
		var id = zdo.GetString(Key, "");
		if (string.IsNullOrEmpty(id))
		{
			id = "reliquary:" + Guid.NewGuid().ToString("N");
			zdo.Set(Key, id);
		}
		return id;
	}
}
