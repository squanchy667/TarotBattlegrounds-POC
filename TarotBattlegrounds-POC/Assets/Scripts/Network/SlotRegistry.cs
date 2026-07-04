#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Owns the actor↔slot mapping and the local player's assigned slot for
/// network games. Extracted verbatim from NetworkGameBridge (batch B
/// god-class refactor). Constructed by NetworkGameBridge.Awake() so it is
/// ready before NetworkGameSetup.AssignSlots can fire.
/// </summary>
public class SlotRegistry
{
    /// <summary>
    /// Maps Photon ActorNumber → game player slot index (0-based).
    /// </summary>
    public Dictionary<int, int> ActorToSlot { get; private set; } = new Dictionary<int, int>();

    /// <summary>
    /// Maps game player slot index → Photon ActorNumber. -1 = AI slot.
    /// </summary>
    public Dictionary<int, int> SlotToActor { get; private set; } = new Dictionary<int, int>();

    /// <summary>
    /// The local player's assigned slot index. -1 if not yet assigned.
    /// </summary>
    public int LocalPlayerSlot { get; set; } = -1;

    /// <summary>
    /// Assign mapping between Photon actors and game slots.
    /// Called by NetworkGameSetup on game load.
    /// </summary>
    public void AssignSlots(Dictionary<int, int> actorToSlot, Dictionary<int, int> slotToActor)
    {
        ActorToSlot = actorToSlot;
        SlotToActor = slotToActor;

        if (PhotonNetwork.LocalPlayer != null && actorToSlot.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            LocalPlayerSlot = actorToSlot[PhotonNetwork.LocalPlayer.ActorNumber];
        }

        Debug.Log($"[NetworkGameBridge] Slots assigned. Local slot: {LocalPlayerSlot}");
    }

    /// <summary>
    /// Check if a player slot is controlled by a Photon player (not AI).
    /// </summary>
    public bool IsNetworkPlayerSlot(int slot)
    {
        return SlotToActor.ContainsKey(slot) && SlotToActor[slot] >= 0;
    }

    /// <summary>
    /// Get the slot assigned to the given Photon ActorNumber.
    /// Returns -1 if not found. Replaces the former
    /// NetworkGameBridge.GetSenderSlot(PhotonMessageInfo) — callers now
    /// resolve via info.Sender.ActorNumber before delegating.
    /// </summary>
    public int GetSlotForActor(int actorNumber)
    {
        if (ActorToSlot.TryGetValue(actorNumber, out int slot))
            return slot;
        return -1;
    }

    /// <summary>
    /// Release an actor's slot ownership, converting it back to an AI slot.
    /// Deduplicates the `SlotToActor[slot] = -1; ActorToSlot.Remove(actorNumber);`
    /// pattern that used to appear both in HandlePlayerDisconnect and in the
    /// OnMasterClientSwitched stale-actor scan.
    /// </summary>
    public void ReleaseActor(int actorNumber)
    {
        if (ActorToSlot.TryGetValue(actorNumber, out int slot))
        {
            SlotToActor[slot] = -1;
        }
        ActorToSlot.Remove(actorNumber);
    }
}
#endif
