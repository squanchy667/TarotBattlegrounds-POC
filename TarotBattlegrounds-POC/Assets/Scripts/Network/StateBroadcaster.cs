#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Host→all broadcast RPC senders. Extracted verbatim from NetworkGameBridge
/// (batch B god-class refactor). BuildPlayerState now delegates to the shared
/// PlayerStateSync.Build static (extracted in an earlier batch).
/// </summary>
public class StateBroadcaster
{
    private readonly PhotonView view;
    private readonly SlotRegistry slots;

    private float lastTimerBroadcast = 0f;
    private const float TIMER_BROADCAST_INTERVAL = 1f;

    public StateBroadcaster(PhotonView view, SlotRegistry slots)
    {
        this.view = view;
        this.slots = slots;
    }

    /// <summary>
    /// Broadcast full player state to all clients.
    /// </summary>
    public void BroadcastPlayerState(int playerIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player player = GameManager.Instance.players[playerIndex];
        NetworkPlayerState state = PlayerStateSync.Build(playerIndex, player, GameManager.Instance.IsPlayerReady);
        string json = JsonConvert.SerializeObject(state);
        view.RPC(nameof(NetworkGameBridge.RPC_SyncPlayerState), RpcTarget.Others, json);
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
            if (slots.SlotToActor.TryGetValue(playerIndex, out int actorNum) && actorNum >= 0)
            {
                var targetPlayer = FindPhotonPlayer(actorNum);
                if (targetPlayer != null)
                {
                    view.RPC(nameof(NetworkGameBridge.RPC_SyncShopForPlayer), targetPlayer, json);
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
        if (slots.SlotToActor.TryGetValue(playerIndex, out int actorNum) && actorNum >= 0)
        {
            var targetPlayer = FindPhotonPlayer(actorNum);
            if (targetPlayer != null)
            {
                view.RPC(nameof(NetworkGameBridge.RPC_ShowDiscovery), targetPlayer, json);
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
        view.RPC(nameof(NetworkGameBridge.RPC_PhaseChanged), RpcTarget.Others, phase, turn, timer);
    }

    /// <summary>
    /// Broadcast timer update (call every ~1s, not every frame).
    /// </summary>
    public void BroadcastTimerUpdate(float remaining)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (Time.time - lastTimerBroadcast < TIMER_BROADCAST_INTERVAL) return;
        lastTimerBroadcast = Time.time;

        view.RPC(nameof(NetworkGameBridge.RPC_TimerUpdate), RpcTarget.Others, remaining);
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
        view.RPC(nameof(NetworkGameBridge.RPC_CombatResult), RpcTarget.Others, json);
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
        view.RPC(nameof(NetworkGameBridge.RPC_GameOver), RpcTarget.Others, json);
    }

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
        view.RPC(nameof(NetworkGameBridge.RPC_PhaseChanged), RpcTarget.Others, phaseStr, GameManager.Instance.TurnNumber, 0f);

        // Re-broadcast all player states
        BroadcastAllPlayerStates();
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
}
#endif
