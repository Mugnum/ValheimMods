using Mugnum.ValheimMods.LifeTotems.Services;
using Mugnum.ValheimMods.LifeTotems.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mugnum.ValheimMods.LifeTotems.World;

/// <summary> Interactable building root; socket visuals are never independent saved objects. </summary>
internal sealed class Reliquary : MonoBehaviour, Hoverable, Interactable
{
	private static readonly HashSet<Reliquary> Loaded = [];
	private ZNetView View;
	private readonly GameObject[] Visuals = new GameObject[3];
	internal string Id => View && View.IsValid() && ZNet.IsSinglePlayer ? ReliquaryIdentity.Get(View.GetZDO()) : null;
	internal bool HasCharges => Id != null && LedgerStore.Current != null && LedgerStore.Current.Charges.Any(c => c.Reliquary == Id);

	private void Awake() { View = GetComponent<ZNetView>(); Loaded.Add(this); }
	private void Start()
	{
		if (Id == null || !ZNet.IsSinglePlayer) return;
		if (LedgerStore.Ready()) Refresh();
	}
	private void OnDestroy() => Loaded.Remove(this);
	public string GetHoverName() => "Reliquary";
	public float GetHoverOffset() => 1f;
	public string GetHoverText() => Localization.instance.Localize("Reliquary\n[<color=yellow><b>$KEY_Use</b></color>] Open rituals");
	public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || user != Player.m_localPlayer) return false;
		if (!ZNet.IsSinglePlayer) { user.Message(MessageHud.MessageType.Center, "LifeTotems currently supports single-player worlds only."); return false; }
		if (!CanUse(Player.m_localPlayer)) return false;
		ReliquaryWindow.Open(this);
		return true;
	}
	internal bool CanUse(Player player) => Plugin.IsActive && player && !player.IsDead() && Id != null
		&& Vector3.Distance(transform.position, player.transform.position) <= 6f
		&& PrivateArea.CheckAccess(transform.position, flash: false);

	/// <summary> Checks only unoccupied sockets, including player capsules, pieces and solid scenery. </summary>
	internal int FindFreeSocket()
	{
		for (var slot = 0; slot < 3; slot++)
		{
			if (LedgerStore.Current.Charges.Any(c => c.Reliquary == Id && c.Slot == slot)) continue;
			var point = transform.TransformPoint(ReliquaryPrefabs.Sockets[slot] + Vector3.up * 0.35f);
			var blockers = Physics.OverlapSphere(point, 0.55f, LayerMask.GetMask("character", "character_net", "piece", "Default", "static_solid", "Default_small"), QueryTriggerInteraction.Ignore);
			if (!blockers.Any(c => !c.transform.IsChildOf(transform))) return slot;
		}
		return -1;
	}

	internal void Refresh()
	{
		if (!View || !View.IsValid() || LedgerStore.Current == null) return;
		for (var slot = 0; slot < 3; slot++)
		{
			var visible = LedgerStore.Current.Charges.Any(c => c.Reliquary == Id && c.Slot == slot);
			if (visible && !Visuals[slot] && ReliquaryPrefabs.TotemVisual)
			{
				var model = Instantiate(ReliquaryPrefabs.TotemVisual, transform, false);
				model.name = "LifeTotem_" + slot;
				model.transform.localPosition = ReliquaryPrefabs.Sockets[slot];
				model.transform.localRotation = Quaternion.identity;
				model.transform.localScale *= 0.75f;
				// Item attachment pivots are not necessarily at the feet. Rest the mesh on the plinth.
				var renderers = model.GetComponentsInChildren<Renderer>(true)
					.Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer).ToArray();
				if (renderers.Length > 0)
				{
					var bottom = renderers.Min(renderer => renderer.bounds.min.y);
					model.transform.position += Vector3.up * (transform.TransformPoint(ReliquaryPrefabs.Sockets[slot]).y - bottom);
				}
				// Keep only a display: no pickup, networking, physical collision or gameplay scripts.
				foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(script);
				foreach (var collider in model.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; Destroy(collider); }
				foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
				Visuals[slot] = model;
			}
			if (Visuals[slot]) Visuals[slot].SetActive(visible);
		}
	}
	internal static void RefreshAll() { foreach (var item in Loaded.ToArray()) if (item) item.Refresh(); }
}
