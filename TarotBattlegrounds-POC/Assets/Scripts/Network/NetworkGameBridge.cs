#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Core networking class. Scene-object PhotonView (ViewID=1).
/// Routes all gameplay RPCs between host and clients.
///
/// Client → Host: Request RPCs (validated on host, then executed)
/// Host → All: Sync RPCs (broadcast state updates)
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class NetworkGameBridge : MonoBehaviourPunCallbacks
{
    public static NetworkGameBridge Instance { get; private set; }

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

    // Events for UI to listen to
    public event System.Action<string, int, float> OnPhaseChanged;
    public event System.Action<float> OnTimerUpdated;
    public event System.Action<string> OnCombatResultReceived;
    public event System.Action<string> OnCombatLogReceived;
    public event System.Action<string> OnGameOverReceived;
    public event System.Action<int> OnPlayerDisconnected;

    private float lastTimerBroadcast = 0f;
    private const float TIMER_BROADCAST_INTERVAL = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    /// Get the sender's assigned slot from their ActorNumber.
    /// Returns -1 if not found.
    /// </summary>
    private int GetSenderSlot(PhotonMessageInfo info)
    {
        if (ActorToSlot.TryGetValue(info.Sender.ActorNumber, out int slot))
            return slot;
        return -1;
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
    private void RPC_RequestBuyCard(int shopIndex, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "BuyCard")) return;

        Player player = GameManager.Instance.players[slot];
        int playerId = player.playerId;

        // M4: Debug logging for buy request
        Debug.Log($"[Host/M4] RPC_RequestBuyCard: sender=P{slot} (playerId={playerId}), shopIndex={shopIndex}, " +
                  $"coins={player.coins}");

        // M4: Bounds check against host's shop before buying
        if (TavernManager.Instance == null || !TavernManager.Instance.availableCards.ContainsKey(playerId))
        {
            Debug.LogError($"[Host/M4] BuyCard FAILED: TavernManager or shop not found for playerId {playerId}");
            return;
        }

        var hostShop = TavernManager.Instance.availableCards[playerId];
        Debug.Log($"[Host/M4] Host shop for P{playerId} has {hostShop.Count} cards");

        if (shopIndex < 0 || shopIndex >= hostShop.Count)
        {
            Debug.LogError($"[Host/M4] BuyCard FAILED: invalid shopIndex {shopIndex}, shop has {hostShop.Count} cards");
            return;
        }

        var cardToBuy = hostShop[shopIndex];
        Debug.Log($"[Host/M4] Attempting to buy: {(cardToBuy != null ? cardToBuy.cardName : "NULL")} from index {shopIndex}");

        player.BuyCard(shopIndex);
        BroadcastPlayerState(slot);
        BroadcastShopForPlayer(slot); // M4: Sync updated shop to client
    }

    [PunRPC]
    private void RPC_RequestSellBoardCard(int boardIndex, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "SellBoardCard")) return;

        Player player = GameManager.Instance.players[slot];
        player.SellCard(boardIndex);
        BroadcastPlayerState(slot);
    }

    [PunRPC]
    private void RPC_RequestSellHandCard(int handIndex, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "SellHandCard")) return;

        Player player = GameManager.Instance.players[slot];
        player.SellCardFromHand(handIndex);
        BroadcastPlayerState(slot);
    }

    [PunRPC]
    private void RPC_RequestPlayCard(int handIndex, int boardPos, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "PlayCard")) return;

        Player player = GameManager.Instance.players[slot];

        // Get card name before playing (for logging)
        string cardName = handIndex >= 0 && handIndex < player.hand.Count ? player.hand[handIndex].cardName : "Unknown";

        player.PlayCard(handIndex, boardPos);

        // Log card stats after ability triggers (for debugging M6)
        if (boardPos >= 0 && boardPos < player.board.Count)
        {
            Card playedCard = player.board[boardPos];
            Debug.Log($"[Host/M6] P{slot} played {cardName}: {playedCard.attack}/{playedCard.health}, Aegis={playedCard.hasAegis}");
        }

        BroadcastPlayerState(slot);
    }

    [PunRPC]
    private void RPC_RequestUpgradeTavern(PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "UpgradeTavern")) return;

        Player player = GameManager.Instance.players[slot];
        player.UpgradeTavern();
        BroadcastPlayerState(slot);
        BroadcastShopForPlayer(slot); // M5: Tier change affects available cards
    }

    [PunRPC]
    private void RPC_RequestRerollShop(PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "RerollShop")) return;

        Player player = GameManager.Instance.players[slot];
        player.RefreshTavernShop();
        BroadcastPlayerState(slot);
        BroadcastShopForPlayer(slot);
    }

    [PunRPC]
    private void RPC_RequestToggleFreeze(PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "ToggleFreeze")) return;

        Player player = GameManager.Instance.players[slot];
        player.ToggleShopFreeze();
        BroadcastPlayerState(slot);
    }

    [PunRPC]
    private void RPC_RequestEndTurn(PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (slot < 0)
        {
            Debug.LogWarning($"[NetworkGameBridge] EndTurn: unknown sender actor {info.Sender.ActorNumber}");
            return;
        }

        GameManager.Instance.PlayerReadyForCombat(slot);
    }

    [PunRPC]
    private void RPC_RequestSwapBoardCards(int indexA, int indexB, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (!ValidateRequest(slot, "SwapBoardCards")) return;

        Player player = GameManager.Instance.players[slot];
        player.SwapBoardCards(indexA, indexB);
        BroadcastPlayerState(slot);
    }

    [PunRPC]
    private void RPC_RequestDiscoveryChoice(int choiceIndex, PhotonMessageInfo info)
    {
        int slot = GetSenderSlot(info);
        if (slot < 0)
        {
            Debug.LogWarning($"[NetworkGameBridge] DiscoveryChoice: unknown sender");
            return;
        }

        Player player = GameManager.Instance.players[slot];
        int playerId = player.playerId;
        // M2: Look up per-player pending discovery
        if (DiscoveryUI.PendingDiscoveryByPlayer.TryGetValue(playerId, out var pendingCards)
            && choiceIndex >= 0 && choiceIndex < pendingCards.Count)
        {
            player.AddDiscoveryCard(pendingCards[choiceIndex]);
            DiscoveryUI.PendingDiscoveryByPlayer.Remove(playerId);
        }
        BroadcastPlayerState(slot);
    }

    private bool ValidateRequest(int slot, string action)
    {
        if (slot < 0)
        {
            Debug.LogWarning($"[NetworkGameBridge] {action}: unknown sender slot");
            return false;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[NetworkGameBridge] {action}: GameManager not available");
            return false;
        }
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Recruit)
        {
            Debug.LogWarning($"[NetworkGameBridge] {action}: not in recruit phase");
            return false;
        }
        if (slot >= GameManager.Instance.players.Count)
        {
            Debug.LogWarning($"[NetworkGameBridge] {action}: invalid slot {slot}");
            return false;
        }
        return true;
    }

    // ================================================================
    // HOST → ALL RPCs (Broadcasts)
    // ================================================================

    /// <summary>
    /// Broadcast full player state to all clients.
    /// </summary>
    public void BroadcastPlayerState(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player player = GameManager.Instance.players[playerIndex];
        NetworkPlayerState state = BuildPlayerState(playerIndex, player);
        string json = JsonConvert.SerializeObject(state);
        photonView.RPC(nameof(RPC_SyncPlayerState), RpcTarget.Others, json);
    }

    /// <summary>
    /// Broadcast shop cards for a specific player.
    /// </summary>
    public void BroadcastShopForPlayer(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player player = GameManager.Instance.players[playerIndex];
        int playerId = player.playerId;

        if (TavernManager.Instance != null && TavernManager.Instance.availableCards.ContainsKey(playerId))
        {
            var shopCards = TavernManager.Instance.availableCards[playerId];
            NetworkCardData[] shopData = NetworkCardData.FromCardList(shopCards);

            // M3: Debug logging to track shop sync
            Debug.Log($"[Host/M3] Broadcasting shop for P{playerIndex} (playerId={playerId}): {shopCards.Count} cards in host shop");
            for (int i = 0; i < shopCards.Count; i++)
            {
                if (shopCards[i] != null)
                    Debug.Log($"[Host/M3]   Shop[{i}]: {shopCards[i].cardName} (Tier {shopCards[i].tier})");
                else
                    Debug.LogWarning($"[Host/M3]   Shop[{i}]: NULL CARD!");
            }
            Debug.Log($"[Host/M3] Serialized to {shopData.Length} NetworkCardData entries");

            var payload = new ShopSyncData { playerIndex = playerIndex, shopCards = shopData };
            string json = JsonConvert.SerializeObject(payload);

            // Send only to the owning player
            if (SlotToActor.TryGetValue(playerIndex, out int actorNum) && actorNum >= 0)
            {
                var targetPlayer = FindPhotonPlayer(actorNum);
                if (targetPlayer != null)
                {
                    photonView.RPC(nameof(RPC_SyncShopForPlayer), targetPlayer, json);
                }
            }
        }
    }

    /// <summary>
    /// M2 FIX: Broadcast discovery options to client when their player forms a triple.
    /// </summary>
    public void BroadcastDiscoveryForPlayer(int playerIndex, List<Card> discoveryCards)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (discoveryCards == null || discoveryCards.Count == 0) return;

        Player player = GameManager.Instance.players[playerIndex];
        NetworkCardData[] cardData = NetworkCardData.FromCardList(discoveryCards);

        var payload = new DiscoverySyncData
        {
            playerIndex = playerIndex,
            discoveryCards = cardData
        };
        string json = JsonConvert.SerializeObject(payload);

        Debug.Log($"[Host/M2] Broadcasting discovery for P{playerIndex}: {discoveryCards.Count} choices");

        // Send to the owning player only
        if (SlotToActor.TryGetValue(playerIndex, out int actorNum) && actorNum >= 0)
        {
            var targetPlayer = FindPhotonPlayer(actorNum);
            if (targetPlayer != null)
            {
                photonView.RPC(nameof(RPC_ShowDiscovery), targetPlayer, json);
            }
        }
    }

    /// <summary>
    /// Broadcast all player states (e.g., after combat).
    /// </summary>
    public void BroadcastAllPlayerStates()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        for (int i = 0; i < GameManager.Instance.playerCount; i++)
        {
            BroadcastPlayerState(i);
        }
    }

    /// <summary>
    /// Broadcast phase change to all clients.
    /// </summary>
    public void BroadcastPhaseChange(string phase, int turn, float timer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_PhaseChanged), RpcTarget.Others, phase, turn, timer);
    }

    /// <summary>
    /// Broadcast timer update (call every ~1s, not every frame).
    /// </summary>
    public void BroadcastTimerUpdate(float remaining)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (Time.time - lastTimerBroadcast < TIMER_BROADCAST_INTERVAL) return;
        lastTimerBroadcast = Time.time;

        photonView.RPC(nameof(RPC_TimerUpdate), RpcTarget.Others, remaining);
    }

    /// <summary>
    /// Broadcast combat result.
    /// </summary>
    public void BroadcastCombatResult(int p1, int p2, string winner, int damage)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var result = new CombatResultData
        {
            player1Index = p1,
            player2Index = p2,
            winner = winner,
            damage = damage
        };
        string json = JsonConvert.SerializeObject(result);
        photonView.RPC(nameof(RPC_CombatResult), RpcTarget.Others, json);
    }

    /// <summary>
    /// Broadcast game over with standings.
    /// </summary>
    public void BroadcastGameOver(GameOverData data)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var netData = new NetworkGameOverData
        {
            winnerPlayerIndex = data.winnerPlayerIndex,
            standings = data.standings.ToArray(),
            totalTurns = data.totalTurns
        };
        string json = JsonConvert.SerializeObject(netData);
        photonView.RPC(nameof(RPC_GameOver), RpcTarget.Others, json);
    }

    // --- RPC implementations (run on clients) ---

    [PunRPC]
    private void RPC_SyncPlayerState(string json)
    {
        NetworkPlayerState state = JsonConvert.DeserializeObject<NetworkPlayerState>(json);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyNetworkPlayerState(state);
        }
    }

    [PunRPC]
    private void RPC_SyncShopForPlayer(string json)
    {
        ShopSyncData data = JsonConvert.DeserializeObject<ShopSyncData>(json);

        // M3: Debug logging to track client-side shop reception
        Debug.Log($"[Client/M3] Received shop sync for P{data.playerIndex}: {data.shopCards.Length} NetworkCardData entries");

        if (TavernManager.Instance != null)
        {
            List<Card> shopCards = NetworkCardData.ToCardList(data.shopCards);
            Debug.Log($"[Client/M3] Deserialized to {shopCards.Count} Card objects");
            for (int i = 0; i < shopCards.Count; i++)
            {
                if (shopCards[i] != null)
                    Debug.Log($"[Client/M3]   Shop[{i}]: {shopCards[i].cardName} (Tier {shopCards[i].tier})");
                else
                    Debug.LogWarning($"[Client/M3]   Shop[{i}]: NULL CARD!");
            }

            TavernManager.Instance.SetShopFromNetwork(data.playerIndex, shopCards);
            Debug.Log($"[Client/M3] Applied to TavernManager for P{data.playerIndex}");
        }

        // Refresh shop UI if this is our player
        if (data.playerIndex == LocalPlayerSlot && GameUIManager.Instance != null)
        {
            Debug.Log($"[Client/M3] This is our shop (LocalPlayerSlot={LocalPlayerSlot}), refreshing UI");
            var shopUI = GameUIManager.Instance.GetShopUI();
            if (shopUI != null) shopUI.RefreshShopDisplay();
        }
    }

    [PunRPC]
    private void RPC_ShowDiscovery(string json)
    {
        DiscoverySyncData data = JsonConvert.DeserializeObject<DiscoverySyncData>(json);

        Debug.Log($"[Client/M2] Received discovery for P{data.playerIndex}: {data.discoveryCards.Length} choices");

        if (GameManager.Instance == null || data.playerIndex < 0 || data.playerIndex >= GameManager.Instance.players.Count)
        {
            Debug.LogError($"[Client/M2] Invalid player index: {data.playerIndex}");
            return;
        }

        Player player = GameManager.Instance.players[data.playerIndex];
        List<Card> cards = NetworkCardData.ToCardList(data.discoveryCards);

        // Store for network resolution when player makes choice
        DiscoveryUI.PendingDiscoveryByPlayer[player.playerId] = cards;

        // Only show UI if this is the local player's discovery
        if (data.playerIndex == LocalPlayerSlot)
        {
            Debug.Log($"[Client/M2] This is our discovery (LocalPlayerSlot={LocalPlayerSlot}), showing UI");
            var discoveryUI = UnityEngine.Object.FindObjectOfType<DiscoveryUI>();
            if (discoveryUI != null)
            {
                discoveryUI.ShowDiscoveryFromNetwork(player, cards);
            }
            else
            {
                Debug.LogError("[Client/M2] DiscoveryUI not found in scene!");
            }
        }
        else
        {
            Debug.Log($"[Client/M2] Not our discovery (LocalPlayerSlot={LocalPlayerSlot}), storing only");
        }
    }

    [PunRPC]
    private void RPC_PhaseChanged(string phase, int turn, float timer)
    {
        Debug.Log($"[NetworkGameBridge] Phase changed: {phase}, Turn {turn}, Timer {timer}");

        // Apply phase and turn to GameManager so UI reads correct values
        if (GameManager.Instance != null)
            GameManager.Instance.SetPhaseFromNetwork(phase, turn);

        OnPhaseChanged?.Invoke(phase, turn, timer);

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.RefreshAllUI();
    }

    [PunRPC]
    private void RPC_TimerUpdate(float remaining)
    {
        OnTimerUpdated?.Invoke(remaining);
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateTimer(remaining);
    }

    [PunRPC]
    private void RPC_CombatResult(string json)
    {
        Debug.Log($"[NetworkGameBridge] Combat result received.");
        OnCombatResultReceived?.Invoke(json);
    }

    [PunRPC]
    private void RPC_CombatLog(string json)
    {
        OnCombatLogReceived?.Invoke(json);
    }

    [PunRPC]
    private void RPC_GameOver(string json)
    {
        Debug.Log("[NetworkGameBridge] Game over received.");
        NetworkGameOverData netData = JsonConvert.DeserializeObject<NetworkGameOverData>(json);

        GameOverData data = new GameOverData
        {
            winnerPlayerIndex = netData.winnerPlayerIndex,
            standings = new List<int>(netData.standings),
            totalTurns = netData.totalTurns
        };

        OnGameOverReceived?.Invoke(json);

        // Trigger the game over event locally so GameOverUI picks it up
        // We invoke the static event via reflection or a public method
        GameManager.InvokeGameOverFromNetwork(data);
    }

    // ================================================================
    // DISCONNECTION HANDLING
    // ================================================================

    public override void OnEnable()
    {
        base.OnEnable(); // Register PUN callbacks
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnPlayerLeftRoomEvent += HandlePlayerDisconnect;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable(); // Unregister PUN callbacks
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnPlayerLeftRoomEvent -= HandlePlayerDisconnect;
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
        HandlePlayerDisconnect(otherPlayer);
    }

    private void HandlePlayerDisconnect(Photon.Realtime.Player player)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (ActorToSlot.TryGetValue(player.ActorNumber, out int slot))
        {
            Debug.Log($"[NetworkGameBridge] Player '{player.NickName}' (slot {slot}) disconnected. Converting to AI.");

            // Convert slot to AI
            SlotToActor[slot] = -1;
            ActorToSlot.Remove(player.ActorNumber);

            // Notify all other clients so they can show the disconnect banner
            photonView.RPC(nameof(RPC_NotifyPlayerDisconnected), RpcTarget.Others, slot, player.NickName);

            // Eliminate the disconnected player if the game is in progress
            if (GameManager.Instance != null && slot < GameManager.Instance.players.Count)
            {
                if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat)
                {
                    // Mid-combat: eliminate immediately (treats them as defeated)
                    GameManager.Instance.EliminateDisconnectedPlayer(slot);
                    BroadcastAllPlayerStates();
                }
                else
                {
                    // Recruit phase: attach AI and auto-ready the slot
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

            OnPlayerDisconnected?.Invoke(slot);
        }
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
            // clients, but the ordering is not guaranteed across all PUN versions. We
            // scan for stale actor slots defensively — it is idempotent because
            // HandlePlayerDisconnect removes the actor from ActorToSlot on first call.

            // Find and clean up any actor slots whose owner is no longer in the room
            var currentActors = new System.Collections.Generic.HashSet<int>();
            foreach (var p in PhotonNetwork.PlayerList)
                currentActors.Add(p.ActorNumber);

            var staleActors = new System.Collections.Generic.List<int>();
            foreach (var actorNum in ActorToSlot.Keys)
            {
                if (!currentActors.Contains(actorNum))
                    staleActors.Add(actorNum);
            }

            foreach (int actorNum in staleActors)
            {
                // Synthesise a minimal player stub sufficient for HandlePlayerDisconnect
                // We only need the actor number; create a fake lookup via slot.
                int slot = ActorToSlot[actorNum];
                Debug.LogWarning($"[NetworkGameBridge] Host migration cleanup: actor {actorNum} (slot {slot}) gone.");

                SlotToActor[slot] = -1;
                ActorToSlot.Remove(actorNum);

                photonView.RPC(nameof(RPC_NotifyPlayerDisconnected), RpcTarget.Others, slot, "Disconnected Player");

                if (GameManager.Instance != null && slot < GameManager.Instance.players.Count)
                {
                    if (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat)
                    {
                        GameManager.Instance.EliminateDisconnectedPlayer(slot);
                    }
                    else
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
                    OnPlayerDisconnected?.Invoke(slot);
                }
            }

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
        if (!PhotonNetwork.IsMasterClient) return;
        if (GameManager.Instance == null) return;

        Debug.Log("[NetworkGameBridge] Broadcasting full state snapshot after host migration.");

        // Re-broadcast current phase so clients re-anchor their phase state
        string phaseStr = GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat ? "Combat" : "Recruit";
        photonView.RPC(nameof(RPC_PhaseChanged), RpcTarget.Others, phaseStr, GameManager.Instance.TurnNumber, 0f);

        // Re-broadcast all player states
        BroadcastAllPlayerStates();
    }

    // ================================================================
    // CLIENT-SIDE DISCONNECT NOTIFICATION RPC
    // ================================================================

    [PunRPC]
    private void RPC_NotifyPlayerDisconnected(int slot, string nickName)
    {
        Debug.LogWarning($"[NetworkGameBridge] Player '{nickName}' (slot {slot}) disconnected from the game.");
        OnPlayerDisconnected?.Invoke(slot);
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private NetworkPlayerState BuildPlayerState(int playerIndex, Player player)
    {
        int playerId = player.playerId;
        NetworkCardData[] shopData = new NetworkCardData[0];

        if (TavernManager.Instance != null && TavernManager.Instance.availableCards.ContainsKey(playerId))
        {
            var shopCards = TavernManager.Instance.availableCards[playerId];
            // M4: Filter out null cards before serializing
            var validShopCards = new System.Collections.Generic.List<Card>();
            foreach (var card in shopCards)
            {
                if (card != null)
                    validShopCards.Add(card);
            }
            shopData = NetworkCardData.FromCardList(validShopCards);
        }

        return new NetworkPlayerState
        {
            playerIndex = playerIndex,
            coins = player.coins,
            tavernTier = player.currentTavernTier,
            health = player.Health,
            shopFrozen = player.ShopFrozen,
            isAlive = player.Health > 0,
            isReady = GameManager.Instance.IsPlayerReady(playerIndex),
            hand = NetworkCardData.FromCardList(player.hand),
            board = NetworkCardData.FromCardList(player.board),
            shopCards = shopData,
            upgradeCost = player.GetUpgradeCost(), // M5: Include upgrade cost
            // H5 fix: Include hero power info for client sync
            heroPowerId = HeroPowerManager.Instance?.GetHeroPower(playerIndex)?.PowerName ?? "",
            heroPowerUsedThisTurn = HeroPowerManager.Instance?.GetHeroPower(playerIndex)?.UsedThisTurn ?? false
        };
    }

    private Photon.Realtime.Player FindPhotonPlayer(int actorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
                return p;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

// ================================================================
// JSON helper structs
// ================================================================

[System.Serializable]
public struct ShopSyncData
{
    public int playerIndex;
    public NetworkCardData[] shopCards;
}

[System.Serializable]
public struct CombatResultData
{
    public int player1Index;
    public int player2Index;
    public string winner;
    public int damage;
}

[System.Serializable]
public struct NetworkGameOverData
{
    public int winnerPlayerIndex;
    public int[] standings;
    public int totalTurns;
}

[System.Serializable]
public struct DiscoverySyncData
{
    public int playerIndex;
    public NetworkCardData[] discoveryCards;
}
#endif
