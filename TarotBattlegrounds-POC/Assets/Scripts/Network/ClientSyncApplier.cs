#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Client-side application of host→client sync RPCs. Extracted verbatim from
/// NetworkGameBridge (batch B god-class refactor). The facade's RPC stubs call
/// these methods and then raise the corresponding public event themselves —
/// events stay declared and raised on the facade (composition point).
/// </summary>
public class ClientSyncApplier
{
    private readonly SlotRegistry slots;

    public ClientSyncApplier(SlotRegistry slots)
    {
        this.slots = slots;
    }

    public void SyncPlayerState(string json)
    {
        NetworkPlayerState state = JsonConvert.DeserializeObject<NetworkPlayerState>(json);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyNetworkPlayerState(state);
        }
    }

    public void SyncShopForPlayer(string json)
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
        if (data.playerIndex == slots.LocalPlayerSlot && GameUIManager.Instance != null)
        {
            Debug.Log($"[Client/M3] This is our shop (LocalPlayerSlot={slots.LocalPlayerSlot}), refreshing UI");
            var shopUI = GameUIManager.Instance.GetShopUI();
            if (shopUI != null) shopUI.RefreshShopDisplay();
        }
    }

    public void ShowDiscovery(string json)
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
        if (data.playerIndex == slots.LocalPlayerSlot)
        {
            Debug.Log($"[Client/M2] This is our discovery (LocalPlayerSlot={slots.LocalPlayerSlot}), showing UI");
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
            Debug.Log($"[Client/M2] Not our discovery (LocalPlayerSlot={slots.LocalPlayerSlot}), storing only");
        }
    }

    public void PhaseChanged(string phase, int turn, float timer)
    {
        Debug.Log($"[NetworkGameBridge] Phase changed: {phase}, Turn {turn}, Timer {timer}");

        // Apply phase and turn to GameManager so UI reads correct values
        if (GameManager.Instance != null)
            GameManager.Instance.SetPhaseFromNetwork(phase, turn);

        if (GameUIManager.Instance != null)
            GameUIManager.Instance.RefreshAllUI();
    }

    public void TimerUpdate(float remaining)
    {
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateTimer(remaining);
    }

    public void GameOver(string json)
    {
        Debug.Log("[NetworkGameBridge] Game over received.");
        NetworkGameOverData netData = JsonConvert.DeserializeObject<NetworkGameOverData>(json);

        GameOverData data = new GameOverData
        {
            winnerPlayerIndex = netData.winnerPlayerIndex,
            standings = new List<int>(netData.standings),
            totalTurns = netData.totalTurns
        };

        // Trigger the game over event locally so GameOverUI picks it up
        // We invoke the static event via reflection or a public method
        GameManager.InvokeGameOverFromNetwork(data);
    }
}
#endif
