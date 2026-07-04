#if PHOTON_UNITY_NETWORKING
using UnityEngine;

/// <summary>
/// Host-side handling of the ten client→host request RPCs. Extracted verbatim
/// from NetworkGameBridge (batch B god-class refactor). The facade's RPC stubs
/// resolve the sender's slot via SlotRegistry and delegate to these methods.
/// </summary>
public class HostRequestHandler
{
    private readonly StateBroadcaster broadcaster;

    public HostRequestHandler(StateBroadcaster broadcaster)
    {
        this.broadcaster = broadcaster;
    }

    public void BuyCard(int slot, int shopIndex)
    {
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
        broadcaster.BroadcastPlayerState(slot);
        broadcaster.BroadcastShopForPlayer(slot); // M4: Sync updated shop to client
    }

    public void SellBoardCard(int slot, int boardIndex)
    {
        if (!ValidateRequest(slot, "SellBoardCard")) return;

        Player player = GameManager.Instance.players[slot];
        player.SellCard(boardIndex);
        broadcaster.BroadcastPlayerState(slot);
    }

    public void SellHandCard(int slot, int handIndex)
    {
        if (!ValidateRequest(slot, "SellHandCard")) return;

        Player player = GameManager.Instance.players[slot];
        player.SellCardFromHand(handIndex);
        broadcaster.BroadcastPlayerState(slot);
    }

    public void PlayCard(int slot, int handIndex, int boardPos)
    {
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

        broadcaster.BroadcastPlayerState(slot);
    }

    public void UpgradeTavern(int slot)
    {
        if (!ValidateRequest(slot, "UpgradeTavern")) return;

        Player player = GameManager.Instance.players[slot];
        player.UpgradeTavern();
        broadcaster.BroadcastPlayerState(slot);
        broadcaster.BroadcastShopForPlayer(slot); // M5: Tier change affects available cards
    }

    public void RerollShop(int slot)
    {
        if (!ValidateRequest(slot, "RerollShop")) return;

        Player player = GameManager.Instance.players[slot];
        player.RefreshTavernShop();
        broadcaster.BroadcastPlayerState(slot);
        broadcaster.BroadcastShopForPlayer(slot);
    }

    public void ToggleFreeze(int slot)
    {
        if (!ValidateRequest(slot, "ToggleFreeze")) return;

        Player player = GameManager.Instance.players[slot];
        player.ToggleShopFreeze();
        broadcaster.BroadcastPlayerState(slot);
    }

    public void EndTurn(int slot)
    {
        if (slot < 0)
        {
            // NOTE (batch B spec deviation): originally logged info.Sender.ActorNumber,
            // but the facade now resolves slot before delegating and PhotonMessageInfo
            // is no longer available here. Logging the resolved slot (-1 = unknown) is
            // the closest verbatim-equivalent; see report for details.
            Debug.LogWarning($"[NetworkGameBridge] EndTurn: unknown sender actor {slot}");
            return;
        }

        GameManager.Instance.PlayerReadyForCombat(slot);
    }

    public void SwapBoardCards(int slot, int indexA, int indexB)
    {
        if (!ValidateRequest(slot, "SwapBoardCards")) return;

        Player player = GameManager.Instance.players[slot];
        player.SwapBoardCards(indexA, indexB);
        broadcaster.BroadcastPlayerState(slot);
    }

    public void DiscoveryChoice(int slot, int choiceIndex)
    {
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
        broadcaster.BroadcastPlayerState(slot);
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
}
#endif
