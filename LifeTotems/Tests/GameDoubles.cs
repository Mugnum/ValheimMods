using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;

// Minimal managed test doubles. Native Unity rendering, physics and save integration require in-game checks.
namespace UnityEngine
{
	public class Object { public string name; public static implicit operator bool(Object value)=>!ReferenceEquals(value,null); }
	public class GameObject : Object { public ItemDrop Drop; public T GetComponent<T>() where T:class=>Drop as T; }
	public struct Vector3(float x,float y,float z)
	{
		public float x=x,y=y,z=z;
		public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
		public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
	}
	public class Transform { public Vector3 position; }
	public struct Quaternion { public static Quaternion identity=>new(); }
	public static class Time { public static float unscaledTime; }
	public static class JsonUtility
	{
		public static string ToJson(object value,bool pretty=false) { using(var stream=new MemoryStream()) { new DataContractJsonSerializer(value.GetType()).WriteObject(stream,value); return System.Text.Encoding.UTF8.GetString(stream.ToArray()); } }
		public static T FromJson<T>(string text) { using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream); }
	}
}
namespace BepInEx { public static class Paths { public static string ConfigPath; } }
public class ItemDrop : UnityEngine.Object
{
	public static List<ItemData> Drops=[];
	public static int FailDropAt=-1;
	public static ItemDrop DropItem(ItemData item,int amount,UnityEngine.Vector3 position,UnityEngine.Quaternion rotation)
	{
		if(Drops.Count==FailDropAt) throw new IOException("Simulated native drop failure");
		var clone=item.Clone(); clone.m_stack=amount; Drops.Add(clone); return new ItemDrop { m_itemData=clone };
	}
	public ItemData m_itemData=new ItemData();
	public class Shared { public bool m_teleportable=true,m_questItem; public int m_maxQuality=4; public ItemData.ItemType m_itemType=ItemData.ItemType.Material; }
	public class ItemData
	{
		public UnityEngine.GameObject m_dropPrefab; public int m_stack,m_quality=1,Cell,m_variant; public bool m_equipped; public Shared m_shared=new Shared();
		public float m_durability=100; public Dictionary<string,string> m_customData=[];
		public ItemData Clone() { var clone=(ItemData)MemberwiseClone(); clone.m_customData=new Dictionary<string,string>(m_customData); return clone; }
		public bool IsEquipable()=>m_shared.m_itemType==ItemType.Shield;
		public enum ItemType { Material,Shield,Consumable,AmmoNonEquipable }
	}
}
public class ObjectDB
{
	public static ObjectDB instance=new ObjectDB(); private readonly Dictionary<string,UnityEngine.GameObject> Items=[];
	public void Add(string name)=>Items[name]=new UnityEngine.GameObject { name=name,Drop=new ItemDrop() };
	public UnityEngine.GameObject GetItemPrefab(string name)=>Items.TryGetValue(name,out var value)?value:null;
}
public class Inventory
{
	private readonly List<ItemDrop.ItemData> Items=[];
	private int Width=8,Height=4;
	public Inventory() { }
	public Inventory(string name,object icon,int width,int height) { Width=width; Height=height; }
	public int GetWidth()=>Width;
	public int GetHeight()=>Height;
	[Serializable] public class Stored { public string Prefab; public int Count,Quality,Cell,Variant; public float Durability; public Dictionary<string,string> Custom; }
	public void Save(ZPackage package)
	{
		package.Write(109); package.Write((ushort)Items.Count);
		package.Write(UnityEngine.JsonUtility.ToJson(Items.Select(i=>new Stored { Prefab=i.m_dropPrefab.name,Count=i.m_stack,Quality=i.m_quality,Cell=i.Cell,Variant=i.m_variant,Durability=i.m_durability,Custom=i.m_customData }).ToList()));
	}
	public void Load(ZPackage package)
	{
		package.ReadInt(); package.ReadUShort(); Items.Clear();
		foreach(var saved in UnityEngine.JsonUtility.FromJson<List<Stored>>(package.ReadString()))
		{
			var prefab=ObjectDB.instance.GetItemPrefab(saved.Prefab); if(!prefab) continue;
			Items.Add(new ItemDrop.ItemData { m_dropPrefab=prefab,m_stack=saved.Count,m_quality=saved.Quality,Cell=saved.Cell,m_variant=saved.Variant,m_durability=saved.Durability,m_customData=saved.Custom });
		}
	}
	public List<ItemDrop.ItemData> GetAllItems()=>Items;
	public bool RemoveItem(ItemDrop.ItemData item)=>Items.Remove(item);
	public bool RemoveItem(ItemDrop.ItemData item,int amount) { if(!Items.Contains(item)) return false; if(item.m_stack<=amount) return Items.Remove(item); item.m_stack-=amount; return true; }
	private void Changed(bool first=false,bool second=false) { }
}
public class ZPackage
{
	private readonly MemoryStream Stream;
	private readonly BinaryWriter Writer;
	private readonly BinaryReader Reader;
	public ZPackage():this(new byte[0]) { }
	public ZPackage(byte[] bytes) { Stream=new MemoryStream(); Stream.Write(bytes,0,bytes.Length); Stream.Position=0; Writer=new BinaryWriter(Stream); Reader=new BinaryReader(Stream); }
	public byte[] GetArray()=>Stream.ToArray(); public void Write(int value)=>Writer.Write(value); public void Write(ushort value)=>Writer.Write(value); public void Write(string value)=>Writer.Write(value);
	public int ReadInt()=>Reader.ReadInt32(); public ushort ReadUShort()=>Reader.ReadUInt16(); public string ReadString()=>Reader.ReadString();
}
public class Player : UnityEngine.Object
{
	public static Player m_localPlayer;
	public Dictionary<string,string> m_customData=[]; public float m_timeSinceDeath=999999,m_hardDeathCooldown=600;
	public long PlayerId=100; public long GetPlayerID()=>PlayerId;
	public UnityEngine.Transform transform=new();
	private readonly Inventory Items=new Inventory(); public Inventory GetInventory()=>Items;
	public bool IsOwner()=>true; public bool IsDead()=>false; public bool IsTeleporting()=>false; public bool InIntro()=>false;
	public SEMan GetSEMan()=>new SEMan(); public void Message(MessageHud.MessageType type,string text) { }
}
public class SEMan { public static int s_statusEffectSoftDeath; public bool HaveStatusEffect(int hash)=>false; }
public class MessageHud { public enum MessageType { Center } }
public enum GlobalKeys { DeathDeleteItems,DeathDeleteUnequipped,DeathKeepInventory,DeathKeepEquip }
public class ZoneSystem { public static ZoneSystem instance=new ZoneSystem(); public HashSet<string> Keys=[]; public bool GetGlobalKey(string key)=>Keys.Contains(key); public bool GetGlobalKey(GlobalKeys key)=>false; }
public class ZNet : UnityEngine.Object { public static ZNet instance=new ZNet(); public static bool IsSinglePlayer=true; public long World=42; public long GetWorldUID()=>World; }
public class ZDO
{
	public string m_uid;
	public string PersistentId;
	public string GetString(string key, string fallback) => PersistentId ?? fallback;
	public void Set(string key, string value) => PersistentId = value;
}
public class ZDOMan { public static ZDOMan instance=new ZDOMan(); public readonly List<ZDO> Structures=[]; public bool GetAllZDOsWithPrefabIterative(string prefab,List<ZDO> list,ref int index) { list.AddRange(Structures); return true; } }
namespace HarmonyLib
{
	public static class AccessTools
	{
		public delegate ref F FieldRef<T,F>(T instance);
		private static ref float GetTime(Player player)=>ref player.m_timeSinceDeath;
		public static FieldRef<T,F> FieldRefAccess<T,F>(string name)=>(FieldRef<T,F>)(object)(FieldRef<Player,float>)GetTime;
		public static MethodInfo Method(Type type,string name)=>type.GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
		public static T MethodDelegate<T>(MethodInfo method)=>(T)(object)Delegate.CreateDelegate(typeof(T),method);
	}
}
namespace Mugnum.ValheimMods.LifeTotems
{
	internal class Setting<T>(T value) { public T Value=value; }
	internal static class Plugin
	{
		internal const string ReliquaryPrefab="LifeTotems_Reliquary";
		internal static bool IsActive=true;
		internal static Setting<bool> KeepEquipment=new(true),GracePeriod=new(true);
		internal static Logger Log=new Logger();
	}
	internal class Logger { public void LogError(object text) { } public void LogInfo(object text) { } }
}
namespace Mugnum.ValheimMods.LifeTotems.UI { internal static class ReliquaryWindow { internal static void Close() { } } }
namespace Mugnum.ValheimMods.LifeTotems.World
{
	internal static class ReliquaryPrefabs { internal static bool IsRegistered=>true; }
	internal class Reliquary
	{
		internal string Id; internal bool Blocked;
		internal bool CanUse(Player player)=>true;
		internal int FindFreeSocket()=>Blocked?-1:Enumerable.Range(0,3).First(n=>!Services.LedgerStore.Current.Charges.Any(c=>c.Reliquary==Id&&c.Slot==n));
		internal static void RefreshAll() { }
	}
}
