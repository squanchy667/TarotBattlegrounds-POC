using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TavernManager : MonoBehaviour
{
    public static TavernManager Instance { get; private set; }
    public List<Card> masterCards; // Assign 10 Tier 1 cards in Inspector
    private List<Card> allCards = new List<Card>();
    public Dictionary<int, List<Card>> availableCards = new Dictionary<int, List<Card>>();
    private Dictionary<int, int> tierCopies = new Dictionary<int, int>()
    {
        {1, 16}, {2, 15}, {3, 13}, {4, 11}, {5, 9}, {6, 7}
    };
    private Dictionary<int, int> shopSizes = new Dictionary<int, int>()
    {
        {1, 3}, {2, 4}, {3, 4}, {4, 5}, {5, 5}, {6, 6}
    };

    /// <summary>
    /// Apply runtime config overrides for tierCopies and shopSizes.
    /// Called by CardPoolInitializer after RuntimeDataLoader completes.
    /// </summary>
    public void ApplyRuntimeConfig(RuntimeGameConfig config)
    {
        if (config == null) return;

        if (config.tierCopies != null)
        {
            tierCopies.Clear();
            foreach (var kvp in config.tierCopies)
            {
                if (int.TryParse(kvp.Key, out int tier))
                    tierCopies[tier] = kvp.Value;
            }
            Debug.Log($"[TavernManager] Applied runtime tierCopies: {string.Join(", ", tierCopies.Select(k => $"{k.Key}:{k.Value}"))}");
        }

        if (config.shopSizes != null)
        {
            shopSizes.Clear();
            foreach (var kvp in config.shopSizes)
            {
                if (int.TryParse(kvp.Key, out int tier))
                    shopSizes[tier] = kvp.Value;
            }
            Debug.Log($"[TavernManager] Applied runtime shopSizes: {string.Join(", ", shopSizes.Select(k => $"{k.Key}:{k.Value}"))}");
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Only persist across scenes in offline mode
            if (GameConfig.CurrentGameMode != GameConfig.GameMode.Multiplayer)
            {
                DontDestroyOnLoad(gameObject);
            }
            Debug.Log($"TavernManager initialized, Instance ID: {GetInstanceID()}, GameObject: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Duplicate TavernManager found on {gameObject.name}, destroying. Existing Instance ID: {Instance.GetInstanceID()}");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize immediately with CardDatabase (uses RuntimeDataLoader if already loaded,
        // otherwise uses built-in 35 cards). This must be synchronous so GameManager can
        // use the pool right away.
        masterCards = CardDatabase.GenerateAllCards();
        Debug.Log($"[TavernManager] Loaded {masterCards.Count} cards (runtime={CardDatabase.IsUsingRuntimeData})");

        if (allCards.Count == 0)
        {
            ResetPool();
        }

        // Initialize synergy system
        InitializeSynergies();

        // If RuntimeDataLoader is still loading, subscribe to reload when it finishes
        if (RuntimeDataLoader.Instance != null && !RuntimeDataLoader.Instance.IsComplete)
        {
            Debug.Log("[TavernManager] RuntimeDataLoader still loading, will reload when complete...");
            RuntimeDataLoader.Instance.OnLoadComplete += OnRuntimeDataLoaded;
        }
        else if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded)
        {
            // Already loaded — apply config overrides
            if (RuntimeDataLoader.Instance.Config != null)
            {
                ApplyRuntimeConfig(RuntimeDataLoader.Instance.Config);
                ResetPool();
            }
        }
    }

    private void OnRuntimeDataLoaded()
    {
        if (RuntimeDataLoader.Instance != null)
            RuntimeDataLoader.Instance.OnLoadComplete -= OnRuntimeDataLoaded;

        if (RuntimeDataLoader.Instance == null || !RuntimeDataLoader.Instance.IsLoaded)
        {
            Debug.Log("[TavernManager] RuntimeDataLoader failed, keeping built-in cards.");
            return;
        }

        // Reload cards from runtime data
        masterCards = CardDatabase.GenerateAllCards();
        Debug.Log($"[TavernManager] Reloaded {masterCards.Count} cards from runtime data");

        // Apply config overrides
        if (RuntimeDataLoader.Instance.Config != null)
            ApplyRuntimeConfig(RuntimeDataLoader.Instance.Config);

        ResetPool();

        // Reload synergies
        InitializeSynergies();
    }

    private void InitializeSynergies()
    {
        // Ensure ThemeManager exists
        ThemeManager.EnsureExists();

        // Auto-create SynergyManager if it doesn't exist
        if (SynergyManager.Instance == null)
        {
            Debug.Log("[TavernManager] Creating SynergyManager...");
            GameObject synergyObj = new GameObject("SynergyManager");
            synergyObj.AddComponent<SynergyManager>();
        }

        // Try runtime synergies first
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Synergies != null)
        {
            var runtimeSynergies = RuntimeDataLoader.Instance.BuildSynergies();
            if (runtimeSynergies != null && runtimeSynergies.Length > 0)
            {
                SynergyManager.Instance.tribeSynergies = runtimeSynergies;
                SynergyManager.Instance.InitializeSynergyCache();
                Debug.Log($"[TavernManager] Synergies initialized with {runtimeSynergies.Length} runtime synergies");
                return;
            }
        }

        // Fallback to built-in synergy data
        SynergyManager.Instance.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        SynergyManager.Instance.InitializeSynergyCache();
        Debug.Log("[TavernManager] Synergies initialized with 4 built-in tribe synergies");
    }

    public void ResetPool()
    {
        allCards.Clear();
        allCards = GenerateFullPool();
    }

    public List<Card> GetFullPool()
    {
        return allCards;
    }

    private List<Card> GenerateFullPool()
    {
        List<Card> fullPool = new List<Card>();
        // H11 fix: Scale pool size for 8-player games
        float poolMultiplier = 1.0f;
        if (EightPlayerManager.Instance != null && GameManager.Instance != null)
        {
            poolMultiplier = EightPlayerManager.Instance.GetShopPoolMultiplier(GameManager.Instance.playerCount);
        }
        foreach (Card uniqueCard in masterCards)
        {
            int baseCopies = tierCopies.ContainsKey(uniqueCard.tier) ? tierCopies[uniqueCard.tier] : 1;
            int copies = Mathf.Max(1, Mathf.RoundToInt(baseCopies * poolMultiplier));
            for (int i = 0; i < copies; i++)
            {
                fullPool.Add(uniqueCard);
            }
        }
        Debug.Log($"Generated full pool size: {fullPool.Count} (multiplier: {poolMultiplier:F1}x)");
        return fullPool;
    }

    public void RemoveCardFromPool(Card card)
    {
        Card poolCard = allCards.FirstOrDefault(c => c.cardName == card.cardName && c.tier == card.tier);
        if (poolCard != null)
        {
            allCards.Remove(poolCard);
            Debug.Log($"Removed {card.cardName} (Tier {card.tier}) from shared pool. Pool size: {allCards.Count}");
        }
        else
        {
            Debug.LogError($"Card {card.cardName} (Tier {card.tier}) not found in allCards!");
        }
    }

    public void ReturnCardToPool(Card card)
    {
        allCards.Add(card);
        Debug.Log($"Returned {card.cardName} to shared pool. Pool size: {allCards.Count}");
    }

    public void RefreshPlayerShop(int playerId, int tavernTier)
    {
        if (!availableCards.ContainsKey(playerId))
        {
            availableCards[playerId] = new List<Card>();
        }

        // M3: Debug logging before refresh
        Debug.Log($"[TavernManager/M3] RefreshPlayerShop START: playerId={playerId}, tier={tavernTier}, " +
                  $"current shop size={availableCards[playerId].Count}, pool size={allCards.Count}");

        // M3: Return previous un-bought shop cards to pool before generating new shop
        int returnedCount = 0;
        foreach (var card in availableCards[playerId])
        {
            if (card != null)
            {
                allCards.Add(card);
                returnedCount++;
            }
        }
        Debug.Log($"[TavernManager/M3] Returned {returnedCount} cards to pool, new pool size={allCards.Count}");
        availableCards[playerId].Clear();

        List<Card> tempPool = allCards.Where(card => card.tier <= tavernTier).ToList();
        int cardsToShow = shopSizes.ContainsKey(tavernTier) ? shopSizes[tavernTier] : 3;
        cardsToShow = Mathf.Min(cardsToShow, tempPool.Count);
        Debug.Log($"[TavernManager/M3] Target shop size={cardsToShow}, eligible cards in pool={tempPool.Count}");

        if (tempPool.Count == 0)
        {
            Debug.LogWarning($"[TavernManager/M3] Player {playerId}: No cards available for Tier {tavernTier} in shared pool (size: {allCards.Count})");
            return;
        }
        for (int i = 0; i < cardsToShow; i++)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = Random.Range(0, tempPool.Count);
            Card picked = tempPool[randomIndex];
            availableCards[playerId].Add(picked);
            // M3: Remove from master pool to reserve this card
            allCards.Remove(picked);
            tempPool.RemoveAt(randomIndex);
            Debug.Log($"[TavernManager/M3] Player {playerId}: Shop[{i}] = {picked.cardName} (Tier {picked.tier})");
        }
        Debug.Log($"[TavernManager/M3] RefreshPlayerShop COMPLETE: playerId={playerId}, final shop size={availableCards[playerId].Count}");
    }

    /// <summary>
    /// Get random discovery cards from the pool at the specified tier.
    /// Cards are reserved (removed from pool) until the player chooses.
    /// Call ReturnDiscoveryCards() with unchosen cards after selection.
    /// </summary>
    public List<Card> GetDiscoveryCards(int tier, int count)
    {
        List<Card> candidates = allCards.Where(c => c.tier == tier).ToList();

        // If no cards at exact tier, try the tier below
        if (candidates.Count == 0 && tier > 1)
        {
            candidates = allCards.Where(c => c.tier == tier - 1).ToList();
        }

        // Deduplicate by card name to offer variety
        var uniqueByName = new Dictionary<string, Card>();
        foreach (var card in candidates)
        {
            if (!uniqueByName.ContainsKey(card.cardName))
                uniqueByName[card.cardName] = card;
        }
        var uniqueCards = new List<Card>(uniqueByName.Values);

        // Shuffle and take up to count
        List<Card> result = new List<Card>();
        var shuffled = uniqueCards.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            result.Add(shuffled[i]);
        }

        // Reserve discovery cards from pool so other players can't get them
        foreach (var card in result)
        {
            allCards.Remove(card);
        }
        Debug.Log($"[TavernManager] Reserved {result.Count} discovery cards from pool (pool size: {allCards.Count})");

        return result;
    }

    /// <summary>
    /// Return unchosen discovery cards back to the pool after player selects one.
    /// </summary>
    public void ReturnDiscoveryCards(List<Card> unchosenCards)
    {
        foreach (var card in unchosenCards)
        {
            allCards.Add(card);
        }
        if (unchosenCards.Count > 0)
            Debug.Log($"[TavernManager] Returned {unchosenCards.Count} unchosen discovery cards to pool (pool size: {allCards.Count})");
    }

    /// <summary>
    /// Set shop contents from network data (client-side).
    /// Uses playerIndex (0-based) which maps to playerId (1-based).
    /// </summary>
    public void SetShopFromNetwork(int playerIndex, List<Card> shopCards)
    {
        int playerId = playerIndex + 1;
        if (!availableCards.ContainsKey(playerId))
        {
            availableCards[playerId] = new List<Card>();
        }
        availableCards[playerId].Clear();
        availableCards[playerId].AddRange(shopCards);
        Debug.Log($"[TavernManager] Shop set from network for player {playerId}: {shopCards.Count} cards");
    }

    public void LogPool()
    {
        Debug.Log($"Pool size: {allCards.Count}");
        foreach (var card in allCards.GroupBy(c => c.cardName))
        {
            Debug.Log($"Card: {card.Key}, Count: {card.Count()}");
        }
    }

    // public List<Card> LogShop(int playerNum)
    // {
    //     Debug.Log($"player num:{playerNum}");
    //     foreach (var obj in availableCards)
    //     {
    //         if(obj.Key == playerNum){
    //             return obj.Value;
    //         }
    //     }
        
    // }
}

