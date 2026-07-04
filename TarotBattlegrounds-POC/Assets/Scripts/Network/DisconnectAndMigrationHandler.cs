#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Disconnect handling and host-migration cleanup. Extracted from
/// NetworkGameBridge (batch B god-class refactor). The Photon callbacks
/// themselves (OnPlayerLeftRoom, OnMasterClientSwitched, OnEnable/OnDisable)
/// cannot move — MonoBehaviourPunCallbacks overrides must stay on the
/// MonoBehaviour — so they stay on the facade and delegate here.
///
/// The two recruit-phase "attach AI and auto-ready the slot" blocks that used
/// to live separately in HandlePlayerDisconnect and in the OnMasterClientSwitched
/// stale-actor scan were diffed and found behavior-identical, so they are
/// unified into ConvertSlotToAI(int). The surrounding Combat-phase branches
/// were NOT identical (HandlePlayerDisconnect also broadcasts all player states
/// after eliminating; the migration scan does not) and are preserved verbatim,
/// un-merged, in their respective methods.
/// </summary>
public class DisconnectAndMigrationHandler
{
    private readonly NetworkGameBridge bridge;
    private readonly SlotRegistry slots;
    private readonly StateBroadcaster broadcaster;

    public DisconnectAndMigrationHandler(NetworkGameBridge bridge, SlotRegistry slots, StateBroadcaster broadcaster)
    {
        this.bridge = bridge;
        this.slots = slots;
        this.broadcaster = broadcaster;
    }

    /// <summary>
    /// Called when any player leaves the room. Runs on all clients, but only the
    /// master client acts on it (converts slot to AI). Non-master clients receive
    /// notification via RPC_NotifyPlayerDisconnected.
    /// Moved verbatim from NetworkGameBridge.HandlePlayerDisconnect.
    /// </summary>
    public void HandlePlayerDisconnect(Photon.Realtime.Player player)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int slot = slots.GetSlotForActor(player.ActorNumber);
        if (slot < 0) return;

        Debug.Log($"[NetworkGameBridge] Player '{player.NickName}' (slot {slot}) disconnected. Converting to AI.");

        // Convert slot to AI
        slots.ReleaseActor(player.ActorNumber);

        // Notify all other clients so they can show the disconnect banner
        bridge.photonView.RPC(nameof(NetworkGameBridge.RPC_NotifyPlayerDisconnected), RpcTarget.Others, slot, player.NickName);

        // Eliminate the disconnected player if the game is in progress
        if (GameManager.Instance != null && slot < GameManager.Instance.players.Count)
        {
            if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat)
            {
                // Mid-combat: eliminate immediately (treats them as defeated)
                GameManager.Instance.EliminateDisconnectedPlayer(slot);
                broadcaster.BroadcastAllPlayerStates();
            }
            else
            {
                // Recruit phase: attach AI and auto-ready the slot
                ConvertSlotToAI(slot);
            }
        }

        bridge.RaisePlayerDisconnected(slot);
    }

    /// <summary>
    /// Photon automatically promotes the next player in the room to master client
    /// when the original master disconnects. This callback fires on ALL remaining
    /// clients, including the one that just became master.
    ///
    /// Photon fires OnPlayerLeftRoom for the old master before this callback on
    /// clients, but the ordering is not guaranteed across all PUN versions. We
    /// scan for stale actor slots defensively — it is idempotent because
    /// HandlePlayerDisconnect removes the actor from ActorToSlot on first call.
    ///
    /// Moved verbatim from the stale-actor scan inside
    /// NetworkGameBridge.OnMasterClientSwitched. The facade still calls
    /// GameManager.Instance.AssumeHostDuties() itself after this returns.
    /// </summary>
    public void CleanupStaleActors()
    {
        // Find and clean up any actor slots whose owner is no longer in the room
        var currentActors = new System.Collections.Generic.HashSet<int>();
        foreach (var p in PhotonNetwork.PlayerList)
            currentActors.Add(p.ActorNumber);

        var staleActors = new System.Collections.Generic.List<int>();
        foreach (var actorNum in slots.ActorToSlot.Keys)
        {
            if (!currentActors.Contains(actorNum))
                staleActors.Add(actorNum);
        }

        foreach (int actorNum in staleActors)
        {
            // Synthesise a minimal player stub sufficient for HandlePlayerDisconnect
            // We only need the actor number; create a fake lookup via slot.
            int slot = slots.ActorToSlot[actorNum];
            Debug.LogWarning($"[NetworkGameBridge] Host migration cleanup: actor {actorNum} (slot {slot}) gone.");

            slots.ReleaseActor(actorNum);

            bridge.photonView.RPC(nameof(NetworkGameBridge.RPC_NotifyPlayerDisconnected), RpcTarget.Others, slot, "Disconnected Player");

            if (GameManager.Instance != null && slot < GameManager.Instance.players.Count)
            {
                if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat)
                {
                    GameManager.Instance.EliminateDisconnectedPlayer(slot);
                }
                else
                {
                    ConvertSlotToAI(slot);
                }
                bridge.RaisePlayerDisconnected(slot);
            }
        }
    }

    /// <summary>
    /// Attach AI and auto-ready a slot during the Recruit phase. Unifies the two
    /// previously-duplicated blocks from HandlePlayerDisconnect (698–710) and
    /// OnMasterClientSwitched (776–789) — verified behavior-identical.
    /// </summary>
    private void ConvertSlotToAI(int slot)
    {
        Player gamePlayer = GameManager.Instance.players[slot];
        AIController ai = gamePlayer.GetComponent<AIController>();
        if (ai == null)
        {
            ai = gamePlayer.gameObject.AddComponent<AIController>();
            ai.Initialize(gamePlayer);
            ai.difficulty = GameConfig.DefaultAIDifficulty;
        }
        GameManager.Instance.RegisterAIController(slot, ai);
        ai.ExecuteTurn();
        GameManager.Instance.PlayerReadyForCombat(slot);
    }
}
#endif
