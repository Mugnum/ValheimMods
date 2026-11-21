using BepInEx;
using Mugnum.ValheimMods.LifeTotems.Domain;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.LifeTotems.Services;

/// <summary> Atomic sidecar persistence; no changes to vanilla world serialization. </summary>
internal static class LedgerStore
{
	internal static WorldLedger Current { get; private set; }
	internal static string Error { get; private set; }
	private static string FilePath;
	private static long LoadedWorld;

	internal static bool Ready()
	{
		if (!ZNet.instance || !ZNet.IsSinglePlayer || ZDOMan.instance == null) return false;
		var world = ZNet.instance.GetWorldUID();
		if (Current != null && world == LoadedWorld) return true;
		if (Error != null && world == LoadedWorld) return false;
		LoadedWorld = world;
		Error = null;
		FilePath = Path.Combine(Paths.ConfigPath, "LifeTotems", world.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json");
		try
		{
			Current = File.Exists(FilePath) ? PersistenceJson.Deserialize<WorldLedger>(File.ReadAllText(FilePath)) : new WorldLedger { World = world };
			Validate(Current, world);
			// All sectors are loaded into ZDOMan even when their scene objects are unloaded.
			var structures = new System.Collections.Generic.List<ZDO>();
			var index = 0;
			while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(Plugin.ReliquaryPrefab, structures, ref index)) { }
			var identities = structures.ToDictionary(zdo => ReliquaryIdentity.Get(zdo), zdo => zdo);
			var existing = identities.Keys.ToHashSet();
			var changed = false;
			// Migrate old session IDs before reconciling missing buildings. Never discard an ambiguous old binding.
			var bindings = Current.Charges.Concat(Current.Recoveries.Where(r => !r.Confirmed).Select(r => r.Charge)).ToArray();
			var legacy = bindings.Select(c => c.Reliquary).Where(id => !ReliquaryIdentity.IsStable(id)).Distinct().ToArray();
			foreach (var old in legacy)
			{
				var match = identities.Where(pair => pair.Value.m_uid.ToString() == old).Select(pair => pair.Key).SingleOrDefault();
				// A lone legacy binding and lone building can be upgraded after the old session ID was lost.
				if (match == null && legacy.Length == 1 && structures.Count == 1) match = identities.Keys.Single();
				if (match == null)
					throw new InvalidDataException("Cannot identify the Reliquary for legacy binding " + old + ". Ledger preserved; manual migration is required.");
				foreach (var charge in bindings.Where(c => c.Reliquary == old)) charge.Reliquary = match;
				changed = true;
				Plugin.Log.LogInfo("Migrated legacy Reliquary binding " + old + " to " + match);
			}
			changed |= Current.Charges.RemoveAll(charge => !existing.Contains(charge.Reliquary)) > 0;
			foreach (var recovery in Current.Recoveries.Where(r => !r.Confirmed).ToArray())
			{
				if (existing.Contains(recovery.Charge.Reliquary))
				{
					// The removal never completed: restore the charge, not its materials.
					if (!Current.Charges.Any(c => c.Reliquary == recovery.Charge.Reliquary && c.Slot == recovery.Charge.Slot))
						Current.Charges.Add(recovery.Charge);
					Current.Recoveries.Remove(recovery);
				}
				else recovery.Confirmed = true;
				changed = true;
			}
			if (changed) Save();
			return true;
		}
		catch (Exception ex)
		{
			Current = null;
			Error = "LifeTotems ledger unavailable. See the log; the existing file has not been reset.";
			Plugin.Log.LogError($"{Error}\n{ex}");
			return false;
		}
	}

	internal static void Validate(WorldLedger ledger, long world)
	{
		if (ledger == null || ledger.Version != 1 || ledger.World != world || ledger.Charges == null
			|| ledger.Charges.Count > 3 || ledger.Charges.Any(c => c == null || string.IsNullOrEmpty(c.Reliquary) || c.Slot < 0 || c.Slot > 2)
			|| ledger.Charges.GroupBy(c => c.Reliquary + ":" + c.Slot).Any(g => g.Count() > 1))
			throw new InvalidDataException("Invalid or unsupported LifeTotems ledger.");
		// The first unreleased prototype did not store recovery data.
		ledger.Recoveries ??= [];
		if (ledger.Recoveries.Any(r => r == null || string.IsNullOrEmpty(r.Id) || r.Charge == null
			|| string.IsNullOrEmpty(r.Charge.Reliquary) || r.Charge.Crafter == 0 || r.Charge.Recipe?.Items == null
			|| r.Charge.Recipe.Items.Count < 1 || r.Charge.Recipe.Items.Any(i => i == null || string.IsNullOrEmpty(i.Prefab) || i.Amount < 1 || i.Quality < 1)
			|| r.Charge.Recipe.Items.Select(i => i.Prefab).Distinct().Count() != r.Charge.Recipe.Items.Count
			|| string.IsNullOrEmpty(r.Charge.PaidInventory) || r.Charge.InventoryWidth < 1 || r.Charge.InventoryHeight < 1
			|| r.RefundedStacks < 0 || r.Charge.Slot < 0 || r.Charge.Slot > 2)
			|| ledger.Recoveries.GroupBy(r => r.Id).Any(g => g.Count() > 1))
			throw new InvalidDataException("Invalid LifeTotems recovery records.");
	}

	/// <summary> Replace only a complete temporary file, keeping the previous ledger as a backup. </summary>
	internal static void Save()
	{
		Validate(Current, LoadedWorld);
		Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
		var temp = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				var bytes = System.Text.Encoding.UTF8.GetBytes(PersistenceJson.Serialize(Current));
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush(true);
			}
			if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak");
			else File.Move(temp, FilePath);
		}
		finally { if (File.Exists(temp)) File.Delete(temp); }
	}

	internal static void Reset() { Current = null; Error = null; LoadedWorld = 0; }
}
