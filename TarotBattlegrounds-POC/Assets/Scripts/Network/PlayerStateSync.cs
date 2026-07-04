using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared, Photon-free network player-state (de)serialization helpers used by
/// both GameManager (client-side apply) and NetworkGameBridge (host-side build).
/// Extracted from GameManager.ApplyNetworkPlayerState / NetworkGameBridge.BuildPlayerState.
/// </summary>
public static class PlayerStateSync
{
    /// <summary>
    /// Apply a full player state received from the network (client-side).
    /// Moved verbatim from GameManager.ApplyNetworkPlayerState.
    /// </summary>
    public static void Apply(NetworkPlayerState state, List<Player> players, GameSessionState session)
    {
        if (state.playerIndex < 0 || state.playerIndex >= players.Count) return;

        Player player = players[state.playerIndex];

        // Update basic stats (bypass events temporarily to avoid spam)
        player.coins = state.coins;
        player.currentTavernTier = state.tavernTier;
        player.Health = state.health;
        player.ShopFrozen = state.shopFrozen;
        // M5: Store synced upgrade cost for UI display
        player.SyncedUpgradeCost = state.upgradeCost;

        // Update health tracking
        if (state.playerIndex < session.PlayerHealths.Count)
            session.SetHealth(state.playerIndex, state.health);

        // Reconstruct hand
        player.hand.Clear();
        if (state.hand != null)
        {
            int handFailed = 0;
            foreach (var cardData in state.hand)
            {
                Card card = cardData.ToCard();
                if (card != null)
                    player.hand.Add(card);
                else
                    handFailed++;
            }
            if (handFailed > 0)
                Debug.LogError($"[ApplyNetworkPlayerState] Player {state.playerIndex}: {handFailed}/{state.hand.Length} hand cards failed to reconstruct");
        }

        // Reconstruct board
        player.board.Clear();
        if (state.board != null)
        {
            int boardFailed = 0;
            foreach (var cardData in state.board)
            {
                Card card = cardData.ToCard();
                if (card != null)
                {
                    player.board.Add(card);
                    // M6: Log card stats to verify ability effects are synced
                    Debug.Log($"[Client/M6] P{state.playerIndex} board card: {card.cardName} {card.attack}/{card.health}, Aegis={card.hasAegis}");
                }
                else
                    boardFailed++;
            }
            if (boardFailed > 0)
                Debug.LogError($"[ApplyNetworkPlayerState] Player {state.playerIndex}: {boardFailed}/{state.board.Length} board cards failed to reconstruct");
        }

        // Reconstruct shop
        if (state.shopCards != null && TavernManager.Instance != null)
        {
            List<Card> shopCards = NetworkCardData.ToCardList(state.shopCards);
            TavernManager.Instance.SetShopFromNetwork(state.playerIndex, shopCards);
        }

        // Notify UI
        player.NotifyAllStateChanged();
    }

    /// <summary>
    /// Build a network player state snapshot (host-side).
    /// Copied verbatim from NetworkGameBridge.BuildPlayerState (batch B wires the
    /// bridge to call this; NOT modifying NetworkGameBridge.cs here).
    /// Known inconsistency preserved: uses playerIndex (slot) for GetHeroPower,
    /// while GameManager uses playerId = slotIndex + 1 elsewhere — see spec §3.4.
    /// </summary>
    public static NetworkPlayerState Build(int playerIndex, Player player, System.Func<int, bool> isReady)
    {
        int playerId = player.playerId;
        NetworkCardData[] shopData = new NetworkCardData[0];

        if (TavernManager.Instance != null && TavernManager.Instance.availableCards.ContainsKey(playerId))
        {
            var shopCards = TavernManager.Instance.availableCards[playerId];
            // M4: Filter out null cards before serializing
            var validShopCards = new List<Card>();
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
            isReady = isReady(playerIndex),
            hand = NetworkCardData.FromCardList(player.hand),
            board = NetworkCardData.FromCardList(player.board),
            shopCards = shopData,
            upgradeCost = player.GetUpgradeCost(), // M5: Include upgrade cost
            // H5 fix: Include hero power info for client sync
            heroPowerId = HeroPowerManager.Instance?.GetHeroPower(playerIndex)?.PowerName ?? "",
            heroPowerUsedThisTurn = HeroPowerManager.Instance?.GetHeroPower(playerIndex)?.UsedThisTurn ?? false
        };
    }
}
