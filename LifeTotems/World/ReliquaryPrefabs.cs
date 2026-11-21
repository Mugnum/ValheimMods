using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.LifeTotems.World;

/// <summary> Builds the placeholder from game assets, without distributing or changing vanilla prefabs. </summary>
internal static class ReliquaryPrefabs
{
	internal static readonly Vector3[] Sockets = [new(-1.7f, 0.24f, 0.4f), new(0f, 0.24f, 1.8f), new(1.7f, 0.24f, 0.4f)];
	internal static GameObject TotemVisual;
	private static bool Registered;
	internal static bool IsRegistered => Registered;

	internal static void Register()
	{
		if (Registered) return;
		try
		{
			var source = PrefabManager.Instance.GetPrefab("GoblinTotem");
			if (!source) throw new InvalidOperationException("Fuling Totem prefab is unavailable.");
			var attach = source.transform.Find("attach") ?? source.transform.Find("model");
			if (!attach) throw new InvalidOperationException("Fuling Totem visual hierarchy was not found.");
			TotemVisual = attach.gameObject;
			var stone = PrefabManager.Instance.GetPrefab("Placeable_Stone");
			var stoneMesh = stone ? stone.GetComponentsInChildren<MeshFilter>(true)
				.Where(filter => filter.sharedMesh && filter.GetComponent<MeshRenderer>())
				.OrderByDescending(filter => filter.sharedMesh.bounds.size.sqrMagnitude).FirstOrDefault() : null;
			if (!stoneMesh) throw new InvalidOperationException("Placeable stone mesh is unavailable.");
			var stoneBounds = stoneMesh.sharedMesh.bounds;
			if (stoneBounds.size.x <= 0 || stoneBounds.size.y <= 0 || stoneBounds.size.z <= 0)
				throw new InvalidOperationException("Placeable stone mesh has invalid bounds.");
			var prefab = PrefabManager.Instance.CreateClonedPrefab(Plugin.ReliquaryPrefab, "guard_stone");
			// Ward tutorials are independent of PrivateArea; do not inherit its Hugin advice.
			foreach (var guide in prefab.GetComponentsInChildren<GuidePoint>(true))
				UnityEngine.Object.DestroyImmediate(guide);
			var ward = prefab.GetComponent<PrivateArea>();
			if (ward)
			{
				if (ward.m_enabledEffect) ward.m_enabledEffect.SetActive(false);
				if (ward.m_areaMarker) ward.m_areaMarker.gameObject.SetActive(false);
				if (ward.m_inRangeEffect) ward.m_inRangeEffect.SetActive(false);
				UnityEngine.Object.DestroyImmediate(ward);
			}
			prefab.AddComponent<Reliquary>();
			// Copy only the decorative stone's mesh and materials, without its pickup or gameplay components.
			foreach (var socket in Sockets)
			{
				var plinth = new GameObject("LifeTotemSocket");
				plinth.name = "LifeTotemSocket";
				plinth.layer = prefab.layer;
				plinth.transform.SetParent(prefab.transform, false);
				plinth.transform.localPosition = new Vector3(socket.x, 0.12f, socket.z);
				plinth.transform.localScale = Vector3.one;
				var rock = new GameObject("Stone");
				rock.layer = prefab.layer;
				rock.transform.SetParent(plinth.transform, false);
				var scale = new Vector3(0.8f / stoneBounds.size.x, 0.24f / stoneBounds.size.y, 0.8f / stoneBounds.size.z);
				rock.transform.localScale = scale;
				rock.transform.localPosition = -Vector3.Scale(stoneBounds.center, scale);
				rock.AddComponent<MeshFilter>().sharedMesh = stoneMesh.sharedMesh;
				rock.AddComponent<MeshRenderer>().sharedMaterials = stoneMesh.GetComponent<MeshRenderer>().sharedMaterials;
				var collider = plinth.AddComponent<BoxCollider>();
				collider.size = new Vector3(0.8f, 0.24f, 0.8f);
			}
			var config = new PieceConfig
			{
				Name = "Reliquary",
				Description = "Bind up to three Life Totems across the world. Bring an offering to prepare another chance at life.",
				PieceTable = "_HammerPieceTable", Category = "Furniture", CraftingStation = "piece_workbench",
				Requirements = [new RequirementConfig("Stone", 20, 0, true), new RequirementConfig("FineWood", 14, 0, true), new RequirementConfig("SurtlingCore", 5, 0, true)]
			};
			if (!PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config)))
				throw new InvalidOperationException("Jotunn could not register the Reliquary.");
			Registered = true;
			Plugin.Log.LogInfo("Reliquary registered using the ward, Fuling Totem and stone plinth placeholders.");
		}
		catch (Exception ex) { Plugin.Log.LogError($"Reliquary registration failed.\n{ex}"); }
	}
}
