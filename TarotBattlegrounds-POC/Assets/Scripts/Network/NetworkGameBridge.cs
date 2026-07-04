#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Core networking class. Scene-object PhotonView (ViewID=1).
/// Routes all gameplay RPCs between host and clients.
///
/// Client → Host: Request RPCs (validated on host, then executed)
/// Host → All: Sync RPCs (broadcast state updates)
///
/// Batch B god-class refactor: bodies extracted into plain-class helpers in
/// Assets/Scripts/Network/ (SlotRegistry, StateBroadcaster, HostRequestHandler,
/// ClientSyncApplier, DisconnectAndMigrationHandler). All 19 PunRPC-attributed
/// methods stay on this class with unchanged names — PUN scans the PhotonView's
/// GameObject for them by name, and the names are frozen in
/// PhotonServerSettings — but their bodies now delegate to the helpers.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class NetworkGameBridge : MonoBehaviourPunCallbacks
{
    public static NetworkGameBridge Instance { get; private set; }

    private SlotRegistry slots;
    private StateBroadcaster broadcaster;
    private HostRequestHandler hostHandler;
    private ClientSyncApplier syncApplier;
    private DisconnectAndMigrationHandler disconnectHandler;

    /// <summary>
    /// Maps Photon ActorNumber → game player slot index (0-based).
    /// </summary>
    public Dictionary<int, int> ActorToSlot => slots.ActorToSlot;

    /// <summary>
    /// Maps game player slot index → Photon ActorNumber. -1 = AI slot.
    /// </summary>
    public Dictionary<int, int> SlotToActor => slots.SlotToActor;

    /// <summary>
    /// The local player's assigned slot index. -1 if not yet assigned.
    /// </summary>
    public int LocalPlayerSlot
    {
        get => slots.LocalPlayerSlot;
        set => slots.LocalPlayerSlot = value;
    }

    // Events for UI to listen to
    public event System.Action<string, int, float> OnPhaseChanged;
    public event System.Action<float> OnTimerUpdated;
    public event System.Action<string> OnCombatResultReceived;
    public event System.Action<string> OnCombatLogReceived;
    public event System.Action<string> OnGameOverReceived;
    public event System.Action<int> OnPlayerDisconnected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Helpers are constructed here (after the singleton guard) so that
        // AssignSlots delegation works immediately — NetworkGameSetup.AssignSlots
        // may fire early in scene load.
        slots = new SlotRegistry();
        broadcaster = new StateBroadcaster(photonView, slots);
        hostHandler = new HostRequestHandler(broadcaster);
        syncApplier = new ClientSyncApplier(slots);
        disconnectHandler = new DisconnectAndMigrationHandler(this, slots, broadcaster);
    }

    // ================================================================
    // SLOT ASSIGNMENT (called by NetworkGameSetup)
    // ================================================================

    /// <summary>
    /// Assign mapping between Photon actors and game slots.
    /// Called by NetworkGameSetup on game load.
    /// </summary>
    public void AssignSlots(Dictionary<int, int> actorToSlot, Dictionary<int, int> slotToActor)
    {
        slots.AssignSlots(actorToSlot, slotToActor);
    }

    /// <summary>
    /// Check if a player slot is controlled by a Photon player (not AI).
    /// </summary>
    public bool IsNetworkPlayerSlot(int slot)
    {
        return slots.IsNetworkPlayerSlot(slot);
    }

    // ================================================================
    // CLIENT → HOST RPCs (Requests)
    // ================================================================

    // --- Public request methods (called by GameUIManager) ---

    public void RequestBuyCard(int shopIndex)
    {
        photonView.RPC(nameof(RPC_RequestBuyCard), RpcTarget.MasterClient, shopIndex);
    }

    public void RequestSellBoardCard(int boardIndex)
    {
        photonView.RPC(nameof(RPC_RequestSellBoardCard), RpcTarget.MasterClient, boardIndex);
    }

    public void RequestSellHandCard(int handIndex)
    {
        photonView.RPC(nameof(RPC_RequestSellHandCard), RpcTarget.MasterClient, handIndex);
    }

    public void RequestPlayCard(int handIndex, int boardPos)
    {
        photonView.RPC(nameof(RPC_RequestPlayCard), RpcTarget.MasterClient, handIndex, boardPos);
    }

    public void RequestUpgradeTavern()
    {
        photonView.RPC(nameof(RPC_RequestUpgradeTavern), RpcTarget.MasterClient);
    }

    public void RequestRerollShop()
    {
        photonView.RPC(nameof(RPC_RequestRerollShop), RpcTarget.MasterClient);
    }

    public void RequestToggleFreeze()
    {
        photonView.RPC(nameof(RPC_RequestToggleFreeze), RpcTarget.MasterClient);
    }

    public void RequestEndTurn()
    {
        photonView.RPC(nameof(RPC_RequestEndTurn), RpcTarget.MasterClient);
    }

    public void RequestSwapBoardCards(int indexA, int indexB)
    {
        photonView.RPC(nameof(RPC_RequestSwapBoardCards), RpcTarget.MasterClient, indexA, indexB);
    }

    public void RequestDiscoveryChoice(int choiceIndex)
    {
        photonView.RPC(nameof(RPC_RequestDiscoveryChoice), RpcTarget.MasterClient, choiceIndex);
    }

    // --- RPC implementations (run on host) ---

    [PunRPC]
    internal void RPC_RequestBuyCard(int shopIndex, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.BuyCard(slot, shopIndex);
    }

    [PunRPC]
    internal void RPC_RequestSellBoardCard(int boardIndex, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.SellBoardCard(slot, boardIndex);
    }

    [PunRPC]
    internal void RPC_RequestSellHandCard(int handIndex, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.SellHandCard(slot, handIndex);
    }

    [PunRPC]
    internal void RPC_RequestPlayCard(int handIndex, int boardPos, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.PlayCard(slot, handIndex, boardPos);
    }

    [PunRPC]
    internal void RPC_RequestUpgradeTavern(PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.UpgradeTavern(slot);
    }

    [PunRPC]
    internal void RPC_RequestRerollShop(PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.RerollShop(slot);
    }

    [PunRPC]
    internal void RPC_RequestToggleFreeze(PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.ToggleFreeze(slot);
    }

    [PunRPC]
    internal void RPC_RequestEndTurn(PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.EndTurn(slot);
    }

    [PunRPC]
    internal void RPC_RequestSwapBoardCards(int indexA, int indexB, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.SwapBoardCards(slot, indexA, indexB);
    }

    [PunRPC]
    internal void RPC_RequestDiscoveryChoice(int choiceIndex, PhotonMessageInfo info)
    {
        int slot = slots.GetSlotForActor(info.Sender.ActorNumber);
        hostHandler.DiscoveryChoice(slot, choiceIndex);
    }

    // ================================================================
    // HOST → ALL RPCs (Broadcasts)
    // ================================================================

    /// <summary>
    /// Broadcast full player state to all clients.
    /// </summary>
    public void BroadcastPlayerState(int playerIndex)
    {
        broadcaster.BroadcastPlayerState(playerIndex);
    }

    /// <summary>
    /// Broadcast shop cards for a specific player.
    /// </summary>
    public void BroadcastShopForPlayer(int playerIndex)
    {
        broadcaster.BroadcastShopForPlayer(playerIndex);
    }

    /// <summary>
    /// M2 FIX: Broadcast discovery options to client when their player forms a triple.
    /// </summary>
    public void BroadcastDiscoveryForPlayer(int playerIndex, List<Card> discoveryCards)
    {
        broadcaster.BroadcastDiscoveryForPlayer(playerIndex, discoveryCards);
    }

    /// <summary>
    /// Broadcast all player states (e.g., after combat).
    /// </summary>
    public void BroadcastAllPlayerStates()
    {
        broadcaster.BroadcastAllPlayerStates();
    }

    /// <summary>
    /// Broadcast phase change to all clients.
    /// </summary>
    public void BroadcastPhaseChange(string phase, int turn, float timer)
    {
        broadcaster.BroadcastPhaseChange(phase, turn, timer);
    }

    /// <summary>
    /// Broadcast timer update (call every ~1s, not every frame).
    /// </summary>
    public void BroadcastTimerUpdate(float remaining)
    {
        broadcaster.BroadcastTimerUpdate(remaining);
    }

    /// <summary>
    /// Broadcast combat result.
    /// </summary>
    public void BroadcastCombatResult(int p1, int p2, string winner, int damage)
    {
        broadcaster.BroadcastCombatResult(p1, p2, winner, damage);
    }

    /// <summary>
    /// Broadcast game over with standings.
    /// </summary>
    public void BroadcastGameOver(GameOverData data)
    {
        broadcaster.BroadcastGameOver(data);
    }

    // --- RPC implementations (run on clients) ---

    [PunRPC]
    internal void RPC_SyncPlayerState(string json)
    {
        syncApplier.SyncPlayerState(json);
    }

    [PunRPC]
    internal void RPC_SyncShopForPlayer(string json)
    {
        syncApplier.SyncShopForPlayer(json);
    }

    [PunRPC]
    internal void RPC_ShowDiscovery(string json)
    {
        syncApplier.ShowDiscovery(json);
    }

    [PunRPC]
    internal void RPC_PhaseChanged(string phase, int turn, float timer)
    {
        syncApplier.PhaseChanged(phase, turn, timer);
        OnPhaseChanged?.Invoke(phase, turn, timer);
    }

    [PunRPC]
    internal void RPC_TimerUpdate(float remaining)
    {
        syncApplier.TimerUpdate(remaining);
        OnTimerUpdated?.Invoke(remaining);
    }

    [PunRPC]
    internal void RPC_CombatResult(string json)
    {
        Debug.Log($"[NetworkGameBridge] Combat result received.");
        OnCombatResultReceived?.Invoke(json);
    }

    [PunRPC]
    internal void RPC_CombatLog(string json)
    {
        OnCombatLogReceived?.Invoke(json);
    }

    [PunRPC]
    internal void RPC_GameOver(string json)
    {
        syncApplier.GameOver(json);
        OnGameOverReceived?.Invoke(json);
    }

    // ================================================================
    // DISCONNECTION HANDLING
    // ================================================================

    public override void OnEnable()
    {
        base.OnEnable(); // Register PUN callbacks
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnPlayerLeftRoomEvent += disconnectHandler.HandlePlayerDisconnect;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable(); // Unregister PUN callbacks
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnPlayerLeftRoomEvent -= disconnectHandler.HandlePlayerDisconnect;
        }
    }

    /// <summary>
    /// Called when any player leaves the room. Runs on all clients, but only the
    /// master client acts on it (converts slot to AI). Non-master clients receive
    /// notification via RPC_NotifyPlayerDisconnected.
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Delegate to the shared handler which guards on IsMasterClient internally.
        disconnectHandler.HandlePlayerDisconnect(otherPlayer);
    }

    // ================================================================
    // HOST MIGRATION (H4 fix)
    // ================================================================

    /// <summary>
    /// Photon automatically promotes the next player in the room to master client
    /// when the original master disconnects. This callback fires on ALL remaining
    /// clients, including the one that just became master.
    ///
    /// Strategy (no full migration required):
    ///   - New master:    call AssumeHostDuties() to start running the game loop.
    ///   - Other clients: do nothing extra — they continue receiving RPC broadcasts
    ///                    from the new master once it assumes host duties.
    ///
    /// We deliberately do NOT kick everyone to the Lobby. The game continues.
    /// </summary>
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.LogWarning($"[NetworkGameBridge] Master client switched to '{newMasterClient.NickName}' " +
                         $"(Actor {newMasterClient.ActorNumber}). " +
                         $"Local is new master: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient)
        {
            // Photon fires OnPlayerLeftRoom for the old master before this callback on
            // clients, but the ordering is not guaranteed across all PUN versions. The
            // stale-actor scan (handled by disconnectHandler) defensively cleans up any
            // actor slots whose owner is no longer in the room; it is idempotent because
            // HandlePlayerDisconnect removes the actor from ActorToSlot on first call.
            disconnectHandler.CleanupStaleActors();

            // Now start running the game loop as the new host
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AssumeHostDuties();
            }
        }
    }

    // ================================================================
    // FULL STATE SYNC (used by new host after migration)
    // ================================================================

    /// <summary>
    /// Push a full snapshot of all player states to all clients.
    /// Called by the new master immediately after assuming host duties.
    /// </summary>
    public void BroadcastFullStateSnapshot()
    {
        broadcaster.BroadcastFullStateSnapshot();
    }

    // ================================================================
    // CLIENT-SIDE DISCONNECT NOTIFICATION RPC
    // ================================================================

    [PunRPC]
    internal void RPC_NotifyPlayerDisconnected(int slot, string nickName)
    {
        Debug.LogWarning($"[NetworkGameBridge] Player '{nickName}' (slot {slot}) disconnected from the game.");
        OnPlayerDisconnected?.Invoke(slot);
    }

    /// <summary>
    /// Raise OnPlayerDisconnected from a helper class. Events can only be invoked
    /// from within the declaring type, so DisconnectAndMigrationHandler calls this
    /// instead of invoking the event directly.
    /// </summary>
    internal void RaisePlayerDisconnected(int slot)
    {
        OnPlayerDisconnected?.Invoke(slot);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
#endif
