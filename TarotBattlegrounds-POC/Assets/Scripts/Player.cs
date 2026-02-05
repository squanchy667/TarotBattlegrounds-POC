using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Player : MonoBehaviour
{
    // ====== EVENTS FOR UI REFRESH ======
    // These events fire when player state changes, allowing UI to react
    public event Action OnHandChanged;
    public event Action OnBoardChanged;
    public event Action OnCoinsChanged;
    public event Action OnTierChanged;
    public event Action<int> OnHealthChanged;
    public event Action OnShopRefreshed;
    public event Action<bool> OnShopFreezeChanged;
    public event Action<Player, List<Card>> OnTripleDiscovery;

    // Static event for any player state change (useful for global listeners)
    public static event Action<Player> OnAnyPlayerStateChanged;
    
    public int playerId;
    public List<Card> hand = new List<Card>();
    public List<Card> board = new List<Card>();
    
    // Backing field for coins with event trigger
    public const int MAX_COINS = 10;

    [SerializeField] private int _coins = 0;
    public int coins
    {
        get => _coins;
        set
        {
            int clamped = Mathf.Clamp(value, 0, MAX_COINS);
            if (_coins != clamped)
            {
                _coins = clamped;
                OnCoinsChanged?.Invoke();
                OnAnyPlayerStateChanged?.Invoke(this);
            }
        }
    }
    
    // Backing field for tier with event trigger
    [SerializeField] private int _currentTavernTier = 1;
    public int currentTavernTier
    {
        get => _currentTavernTier;
        set
        {
            if (_currentTavernTier != value)
            {
                _currentTavernTier = value;
                OnTierChanged?.Invoke();
                OnAnyPlayerStateChanged?.Invoke(this);
            }
        }
    }
    
    // Health property (synced with GameManager)
    private int _health = 40;
    public int Health
    {
        get => _health;
        set
        {
            if (_health != value)
            {
                _health = value;
                OnHealthChanged?.Invoke(_health);
                OnAnyPlayerStateChanged?.Invoke(this);
            }
        }
    }
    
    // Shop freeze state
    private bool _shopFrozen = false;
    public bool ShopFrozen
    {
        get => _shopFrozen;
        set
        {
            if (_shopFrozen != value)
            {
                _shopFrozen = value;
                OnShopFreezeChanged?.Invoke(_shopFrozen);
                OnAnyPlayerStateChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Current upgrade cost for this player. Reduces by 1 each turn (game lifecycle event).
    /// Resets to base cost when player upgrades to new tier.
    /// Synced from host in multiplayer.
    /// </summary>
    public int currentUpgradeCost = 5; // Default for tier 1→2

    /// <summary>
    /// Upgrade cost synced from host. Used by clients to display correct cost.
    /// -1 means use local calculation.
    /// </summary>
    public int SyncedUpgradeCost { get; set; } = -1;

    public void ToggleShopFreeze()
    {
        ShopFrozen = !ShopFrozen;
        Debug.Log($"Player {playerId}: Shop freeze toggled to {ShopFrozen}");
    }

    private TavernManager tavern;
    private List<Card> _pendingDiscoveryCards = new List<Card>();
    
    // Dictionary for base upgrade costs: key = target tier, value = base cost
    // Based on Hearthstone Battlegrounds standard costs
    private Dictionary<int, int> baseUpgradeCosts = new Dictionary<int, int>()
    {
        {2, 5}, {3, 8}, {4, 11}, {5, 11}, {6, 11}
    };
    
    // Dictionary to track turns since each tier was reached
    private Dictionary<int, int> tierTurnCounter = new Dictionary<int, int>()
    {
        {1, 0}, {2, 0}, {3, 0}, {4, 0}, {5, 0}
    };
    
    void Start()
    {
        tavern = TavernManager.Instance;
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: No TavernManager found in Start!");
        }
        else
        {
            if (!tavern.availableCards.ContainsKey(playerId))
            {
                tavern.availableCards[playerId] = new List<Card>();
            }
        }
    }
    
    public void BuyCard(int index)
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot buy card, TavernManager not found!");
            return;
        }
        
        if (index < 0 || index >= tavern.availableCards[playerId].Count)
        {
            Debug.LogWarning($"Player {playerId}: Invalid index: {index}, Available cards: {tavern.availableCards[playerId].Count}");
            return;
        }
        
        Card card = tavern.availableCards[playerId][index];

        // Calculate effective cost with synergy reduction (centralized in SynergyManager)
        int cost = SynergyManager.Instance != null
            ? SynergyManager.Instance.GetEffectiveCost(card, board)
            : Mathf.Max(0, 3 + card.buyCostModifier);

        if (coins >= cost && hand.Count < 10)
        {
            coins -= cost; // This triggers OnCoinsChanged via property setter
            Card newCard = card.Clone();
            hand.Add(newCard);
            // M3: Card is already reserved (removed from pool) when placed in shop
            tavern.availableCards[playerId].RemoveAt(index);
            
            // Fire events
            OnHandChanged?.Invoke();
            OnShopRefreshed?.Invoke(); // Shop changed too
            OnAnyPlayerStateChanged?.Invoke(this);
            
            Debug.Log($"Player {playerId}: Bought {newCard.cardName} (Tier {newCard.tier}) for {cost} coins. Hand size: {hand.Count}, Coins: {coins}, Pool size: {tavern.GetFullPool().Count}");

            // Check for triples after buying
            CheckAndResolveTriples();
        }
        else
        {
            Debug.Log($"Player {playerId}: Cannot buy: Coins = {coins}, Hand size = {hand.Count}");
        }
    }
    
    public void SellCard(int index)
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot sell card, TavernManager not found!");
            return;
        }

        if (index < 0 || index >= board.Count)
        {
            Debug.LogWarning($"Player {playerId}: Invalid board index: {index}");
            return;
        }

        Card card = board[index];
        int value = Mathf.Max(0, 1 + card.sellValueModifier);

        // Apply synergy sell bonus (e.g., Pentacles) — M1: use per-player snapshot
        if (SynergyManager.Instance != null)
        {
            var snapshot = SynergyManager.Instance.CalculateSynergies(board);
            int synergyBonus = SynergyManager.Instance.GetSellBonus(card, snapshot);
            value += synergyBonus;
            if (synergyBonus > 0)
                Debug.Log($"[Synergy] Sell bonus: +{synergyBonus} gold");
        }

        coins += value; // This triggers OnCoinsChanged via property setter
        AbilityManager.UnregisterCard(card); // Clean up abilities

        // Golden cards are consumed (never return to pool), normal cards reset and return
        if (card.isGolden)
        {
            Debug.Log($"Player {playerId}: Golden card {card.cardName} consumed on sell (not returned to pool)");
        }
        else
        {
            card.ResetToBaseStats();
            tavern.ReturnCardToPool(card);
        }
        board.RemoveAt(index);

        // Fire events
        OnBoardChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Sold {card.cardName} (Tier {card.tier}) for {value} coins. Coins: {coins}, Pool size: {tavern.GetFullPool().Count}");
    }

    public void SellCardFromHand(int index)
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot sell card, TavernManager not found!");
            return;
        }

        if (index < 0 || index >= hand.Count)
        {
            Debug.LogWarning($"Player {playerId}: Invalid hand index: {index}");
            return;
        }

        Card card = hand[index];
        int value = Mathf.Max(0, 1 + card.sellValueModifier);
        coins += value; // This triggers OnCoinsChanged via property setter
        AbilityManager.UnregisterCard(card); // Clean up abilities

        // Golden cards are consumed (never return to pool), normal cards reset and return
        if (card.isGolden)
        {
            Debug.Log($"Player {playerId}: Golden card {card.cardName} consumed on sell from hand (not returned to pool)");
        }
        else
        {
            card.ResetToBaseStats();
            tavern.ReturnCardToPool(card);
        }
        hand.RemoveAt(index);

        // Fire events
        OnHandChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Sold {card.cardName} (Tier {card.tier}) from hand for {value} coins. Coins: {coins}, Pool size: {tavern.GetFullPool().Count}");
    }
    
    /// <summary>
    /// Check all cards in hand + board for triples (3 non-golden copies of the same card).
    /// If found, remove 3 copies and create a golden version in hand.
    /// </summary>
    public void CheckAndResolveTriples()
    {
        // Group all non-golden cards by name across hand + board
        var allCards = new List<(Card card, bool inHand, int index)>();
        for (int i = 0; i < hand.Count; i++)
            allCards.Add((hand[i], true, i));
        for (int i = 0; i < board.Count; i++)
            allCards.Add((board[i], false, i));

        var groups = new Dictionary<string, List<(Card card, bool inHand, int index)>>();
        foreach (var entry in allCards)
        {
            if (entry.card.isGolden) continue;
            if (!groups.ContainsKey(entry.card.cardName))
                groups[entry.card.cardName] = new List<(Card, bool, int)>();
            groups[entry.card.cardName].Add(entry);
        }

        foreach (var kvp in groups)
        {
            if (kvp.Value.Count < 3) continue;

            // Take the first 3 copies
            var toMerge = kvp.Value.GetRange(0, 3);
            Card baseCard = toMerge[0].card;

            Debug.Log($"Player {playerId}: Triple detected for {baseCard.cardName}! Creating golden version.");

            // Remove cards from board and hand (remove in reverse index order to preserve indices)
            var boardRemovals = new List<int>();
            var handRemovals = new List<int>();
            foreach (var entry in toMerge)
            {
                if (entry.inHand)
                    handRemovals.Add(entry.index);
                else
                    boardRemovals.Add(entry.index);
            }

            // Sort descending to remove from end first
            boardRemovals.Sort((a, b) => b.CompareTo(a));
            handRemovals.Sort((a, b) => b.CompareTo(a));

            foreach (int idx in boardRemovals)
            {
                AbilityManager.UnregisterCard(board[idx]);
                // Triple copies are consumed, not returned to pool (matches Hearthstone)
                board.RemoveAt(idx);
            }
            foreach (int idx in handRemovals)
            {
                AbilityManager.UnregisterCard(hand[idx]);
                // Triple copies are consumed, not returned to pool (matches Hearthstone)
                hand.RemoveAt(idx);
            }

            // Create golden card and add to hand (can temporarily exceed limit, like Hearthstone Battlegrounds)
            Card goldenCard = Card.CreateGoldenVersion(baseCard);
            hand.Add(goldenCard);

            if (hand.Count > 10)
            {
                Debug.Log($"Player {playerId}: Golden triple added at hand position {hand.Count} (exceeds normal limit). Sell or play cards before buying more.");
            }

            // Fire UI events
            OnHandChanged?.Invoke();
            OnBoardChanged?.Invoke();
            OnAnyPlayerStateChanged?.Invoke(this);

            // Trigger discovery reward (cards are reserved from pool until player chooses)
            int discoveryTier = Mathf.Min(currentTavernTier + 1, 6);
            if (tavern != null)
            {
                List<Card> discoveryCards = tavern.GetDiscoveryCards(discoveryTier, 3);
                if (discoveryCards.Count > 0)
                {
                    _pendingDiscoveryCards = new List<Card>(discoveryCards);
                    Debug.Log($"Player {playerId}: Triple discovery! Offering {discoveryCards.Count} tier {discoveryTier} cards.");

                    // Trigger local event (for host's UI)
                    OnTripleDiscovery?.Invoke(this, discoveryCards);

#if PHOTON_UNITY_NETWORKING
                    // M2 FIX: In online mode, broadcast discovery to client
                    if (GameManager.Instance != null && GameManager.Instance.IsOnlineMode &&
                        NetworkGameBridge.Instance != null && GameManager.Instance.IsHost)
                    {
                        NetworkGameBridge.Instance.BroadcastDiscoveryForPlayer(playerId - 1, discoveryCards);
                    }
#endif
                }
                else
                {
                    Debug.LogWarning($"Player {playerId}: No discovery cards available at tier {discoveryTier}. Pool may be depleted.");
                }
            }

            // Only resolve one triple per buy
            break;
        }
    }

    /// <summary>
    /// Add a discovered card to the player's hand (from triple discovery).
    /// </summary>
    public void AddDiscoveryCard(Card card)
    {
        Card newCard = card.Clone();
        hand.Add(newCard);

        // Return unchosen discovery cards to pool (chosen card stays removed since GetDiscoveryCards reserved it)
        if (tavern != null && _pendingDiscoveryCards.Count > 0)
        {
            var unchosen = _pendingDiscoveryCards.Where(c => c != card).ToList();
            tavern.ReturnDiscoveryCards(unchosen);
            _pendingDiscoveryCards.Clear();
        }

        OnHandChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Discovered {newCard.cardName} (Tier {newCard.tier}). Hand size: {hand.Count}");

        // Check for triples after discovering (matches BuyCard behavior)
        CheckAndResolveTriples();
    }

    public void PlayCard(int handIndex, int boardIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count)
        {
            Debug.LogWarning($"Player {playerId}: Invalid hand index: {handIndex}, Hand size: {hand.Count}");
            return;
        }
        
        if (board.Count >= 7)
        {
            Debug.Log($"Player {playerId}: Cannot play: Board full.");
            return;
        }
        
        Card card = hand[handIndex];
        if (boardIndex < 0 || boardIndex > board.Count)
            boardIndex = board.Count;
            
        board.Insert(boardIndex, card);
        hand.RemoveAt(handIndex);
        TriggerSummoning(card);

        // Register card's ability (new ability system)
        card.RegisterAbility();

        // Trigger Battlecry abilities (new ability system)
        var battlecryContext = AbilityManager.CreateBattlecryContext(card, this);
        AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, battlecryContext);

        // Fire events
        OnHandChanged?.Invoke();
        OnBoardChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Played {card.cardName} (Tier {card.tier}) to board position {boardIndex}. Board size: {board.Count}, Hand size: {hand.Count}");

        // Check for triples after playing (card from hand may complete a triple on board)
        CheckAndResolveTriples();
    }
    
    public void SwapBoardCards(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= board.Count || indexB < 0 || indexB >= board.Count)
        {
            Debug.LogWarning($"Player {playerId}: Invalid swap indices: {indexA}, {indexB}. Board size: {board.Count}");
            return;
        }
        if (indexA == indexB) return;

        Card temp = board[indexA];
        board[indexA] = board[indexB];
        board[indexB] = temp;

        OnBoardChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Swapped board cards at positions {indexA} and {indexB}");
    }

    /// <summary>
    /// Notify UI that board state changed after external mutation (e.g., AI sorting).
    /// </summary>
    public void NotifyBoardChanged()
    {
        OnBoardChanged?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);
    }

    public void EndRecruitPhase()
    {
        // BUG FIX: Disabled legacy LastReading system to prevent triple buffing
        // LastReading was buffing cards, then Synergies buffed again, then Abilities buffed again
        // Result: Cards became way too strong (3/3 → 9/X after one combat!)
        //
        // LastReading is now REPLACED by:
        // - Synergy System (handles tribal buffs based on 2/4/6 thresholds)
        // - Ability System (handles per-card OnAttack, Battlecry, etc.)
        //
        // Keeping the method below for reference, but commented out:
        /*
        foreach (var card in board)
        {
            if (card.effectType == Card.EffectType.LastReading)
            {
                TriggerLastReading(card);
            }
        }
        */

        // Trigger EndOfTurn synergies — M1: use per-player snapshot
        if (SynergyManager.Instance != null)
        {
            var snapshot = SynergyManager.Instance.CalculateSynergies(board);
            SynergyManager.Instance.TriggerSynergies(SynergyTrigger.EndOfTurn, board, this, snapshot);
        }

        Debug.Log($"Player {playerId}: Recruit phase ended. Synergy effects triggered.");
    }
    
    private void TriggerSummoning(Card card)
    {
        if (card.effectType != Card.EffectType.Summoning) return;
        
        string[] param = card.effectParameter.Split(':');
        string effect = param[0];
        int value = param.Length > 1 && int.TryParse(param[1], out int v) ? v : 0;
        
        switch (effect)
        {
            case "BuffAttack":
                foreach (var c in GetAdjacentCards(card))
                {
                    c.attack += value;
                    Debug.Log($"Player {playerId}: Summoning: {card.cardName} buffs {c.cardName} attack by {value} (New Attack: {c.attack})");
                }
                OnBoardChanged?.Invoke(); // Stats changed
                break;
            default:
                Debug.LogWarning($"Player {playerId}: Unknown Summoning effect: {effect} for {card.cardName}");
                break;
        }
    }
    
    private void TriggerLastReading(Card card)
    {
        if (card.effectType != Card.EffectType.LastReading) return;

        string[] param = card.effectParameter.Split(':');
        string effect = param[0];
        int value = param.Length > 1 && int.TryParse(param[1], out int v) ? v : 0;

        // Legacy effects - "BuffTribe" pattern (e.g., "BuffWands", "BuffCups")
        if (effect.StartsWith("Buff"))
        {
            string tribeName = effect.Substring(4); // Remove "Buff" prefix
            TribeType targetTribe = ThemeManager.ParseTribeName(tribeName);

            if (targetTribe != TribeType.None)
            {
                foreach (var c in board)
                {
                    if (c.HasTribe(targetTribe))
                    {
                        c.attack += value;
                        c.health += value;
                        Debug.Log($"Player {playerId}: LastReading: {card.cardName} buffs {c.cardName} by {value}/{value} (New Stats: {c.attack}/{c.health})");
                    }
                }
                OnBoardChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning($"Player {playerId}: Unknown tribe in LastReading effect: {tribeName}");
            }
        }
        else
        {
            Debug.LogWarning($"Player {playerId}: Unknown LastReading effect: {effect} for {card.cardName}");
        }
    }
    
    private List<Card> GetAdjacentCards(Card card)
    {
        int index = board.IndexOf(card);
        List<Card> adjacent = new List<Card>();
        if (index > 0) adjacent.Add(board[index - 1]);
        if (index < board.Count - 1) adjacent.Add(board[index + 1]);
        return adjacent;
    }
    
    public void UpgradeTavern()
    {
        int cost = GetUpgradeCost();
        if (coins >= cost && currentTavernTier < 6)
        {
            coins -= cost; // Triggers OnCoinsChanged
            currentTavernTier++; // Triggers OnTierChanged

            // M5 FIX: Reset current upgrade cost to base cost for next tier
            int nextTier = currentTavernTier + 1;
            if (baseUpgradeCosts.ContainsKey(nextTier))
            {
                currentUpgradeCost = baseUpgradeCosts[nextTier];
            }
            else
            {
                currentUpgradeCost = 0; // Max tier reached
            }

            Debug.Log($"Player {playerId}: Upgraded to Tavern Tier {currentTavernTier} for {cost} coins. Coins left: {coins}, Next Upgrade Cost: {currentUpgradeCost}");
        }
        else
        {
            Debug.Log($"Player {playerId}: Cannot upgrade tavern: Coins = {coins}, Required = {cost}");
        }
    }
    
    public int GetUpgradeCost()
    {
        // M5 FIX (REVISED): Simple game lifecycle mechanic
        // In multiplayer, clients use synced value from host
        if (GameManager.Instance != null && GameManager.Instance.IsOnlineMode && !GameManager.Instance.IsHost)
        {
            return SyncedUpgradeCost >= 0 ? SyncedUpgradeCost : currentUpgradeCost;
        }

        // Host/offline: Just return the current tracked cost
        // (Reduced by 1 each turn via game lifecycle event in GameManager)
        return Mathf.Max(0, currentUpgradeCost);
    }
    
    public void RefreshShop(int gameTurn)
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot refresh shop, TavernManager not found!");
            return;
        }

        int oldCoins = coins;
        coins = Mathf.Min(3 + (gameTurn - 1), 10); // M8: Use property setter

        if (tierTurnCounter.ContainsKey(currentTavernTier))
            tierTurnCounter[currentTavernTier]++;
        else
            tierTurnCounter[currentTavernTier] = 1;

        if (_shopFrozen)
        {
            // Keep current shop, auto-unfreeze
            Debug.Log($"Player {playerId}: Shop was frozen, keeping current offers. Auto-unfreezing.");
            ShopFrozen = false;
        }
        else
        {
            tavern.RefreshPlayerShop(playerId, currentTavernTier);
        }

        // Fire events (coins event already fired by property setter)
        OnShopRefreshed?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);

        Debug.Log($"Player {playerId}: Tavern refreshed: Game Turn {gameTurn}, Available cards: {tavern.availableCards[playerId].Count}, Coins: {oldCoins} -> {coins}, Tier: {currentTavernTier}, Upgrade Cost: {GetUpgradeCost()}");
    }
    
    public void RefreshTavernShop()
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot reroll shop, TavernManager not found!");
            return;
        }

        if (coins >= 1)
        {
            // Clear freeze when manually rerolling (player chose to replace frozen shop)
            if (_shopFrozen)
            {
                ShopFrozen = false;
                Debug.Log($"Player {playerId}: Shop freeze cleared by manual reroll.");
            }
            coins -= 1; // Triggers OnCoinsChanged
            tavern.RefreshPlayerShop(playerId, currentTavernTier);
            
            OnShopRefreshed?.Invoke();
            
            Debug.Log($"Player {playerId}: Tavern shop rerolled: Available cards: {tavern.availableCards[playerId].Count}, Coins: {coins}, Tier: {currentTavernTier}, Upgrade Cost: {GetUpgradeCost()}");
        }
        else
        {
            Debug.Log($"Player {playerId}: Cannot reroll: Coins = {coins}");
        }
    }
    
    /// <summary>
    /// Force refresh all UI by firing all events. Call when switching players.
    /// </summary>
    public void NotifyAllStateChanged()
    {
        OnHandChanged?.Invoke();
        OnBoardChanged?.Invoke();
        OnCoinsChanged?.Invoke();
        OnTierChanged?.Invoke();
        OnHealthChanged?.Invoke(_health);
        OnShopRefreshed?.Invoke();
        OnAnyPlayerStateChanged?.Invoke(this);
    }
    
    void OnDestroy()
    {
        // Return any pending discovery cards to the pool on disconnect/destroy
        if (tavern != null && _pendingDiscoveryCards.Count > 0)
        {
            tavern.ReturnDiscoveryCards(_pendingDiscoveryCards);
            Debug.Log($"Player {playerId}: OnDestroy - returned {_pendingDiscoveryCards.Count} pending discovery cards to pool");
            _pendingDiscoveryCards.Clear();
        }
    }

    public void LogBoardState()
    {
        if (board.Count == 0)
        {
            Debug.Log($"Player {playerId}: Board is empty.");
            return;
        }
        
        string boardLog = $"Player {playerId} Board (Size: {board.Count}/7):\n";
        for (int i = 0; i < board.Count; i++)
        {
            Card card = board[i];
            string leftNeighbor = (i > 0) ? board[i - 1].cardName : "None";
            string rightNeighbor = (i < board.Count - 1) ? board[i + 1].cardName : "None";
            boardLog += $"  Position {i}: {card.cardName} (Tier {card.tier}) | Attack: {card.attack} | Health: {card.health} " +
                        $"| Tribe: {card.tribe} | Effect: {card.effectType} " +
                        $"| Left: {leftNeighbor} | Right: {rightNeighbor}\n";
        }
        Debug.Log(boardLog.TrimEnd());
    }
}
