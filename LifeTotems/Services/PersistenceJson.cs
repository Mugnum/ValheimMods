using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Managed serialization preserves the internal DTO lists Unity JsonUtility omitted. </summary>
internal static class PersistenceJson
{
	internal static string Serialize<T>(T value)
	{
		using (var stream = new MemoryStream())
		{
			new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
			return Encoding.UTF8.GetString(stream.ToArray());
		}
	}

	internal static T Deserialize<T>(string text)
	{
		using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
			return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
	}
}
