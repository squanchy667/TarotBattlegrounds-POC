using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TavernManager : MonoBehaviour
{
    public static TavernManager Instance { get; private set; }
    public List<Card> masterCards; // Assign 10 Tier 1 cards in Inspector
    private static List<Card> allCards = new List<Card>();
    public Dictionary<int, List<Card>> availableCards = new Dictionary<int, List<Card>>();
    private Dictionary<int, int> tierCopies = new Dictionary<int, int>()
    {
        {1, 16}, {2, 15}, {3, 13}, {4, 11}, {5, 9}, {6, 7}
    };
    private Dictionary<int, int> shopSizes = new Dictionary<int, int>()
    {
        {1, 3}, {2, 4}, {3, 4}, {4, 5}, {5, 5}, {6, 6}
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        if (allCards.Count == 0)
        {
            ResetPool();
        }

        // Initialize synergy system
        InitializeSynergies();
    }

    private void InitializeSynergies()
    {
        // Auto-create SynergyManager if it doesn't exist
        if (SynergyManager.Instance == null)
        {
            Debug.Log("[TavernManager] Creating SynergyManager...");
            GameObject synergyObj = new GameObject("SynergyManager");
            synergyObj.AddComponent<SynergyManager>();
        }

        // Load synergy data and reinitialize cache
        SynergyManager.Instance.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        SynergyManager.Instance.InitializeSynergyCache();
        Debug.Log("[TavernManager] Synergies initialized with 4 tribe synergies");
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
        foreach (Card uniqueCard in masterCards)
        {
            int copies = tierCopies.ContainsKey(uniqueCard.tier) ? tierCopies[uniqueCard.tier] : 1;
            for (int i = 0; i < copies; i++)
            {
                fullPool.Add(uniqueCard);
            }
        }
        Debug.Log($"Generated full pool size: {fullPool.Count}");
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
        availableCards[playerId].Clear();
        List<Card> tempPool = GetFullPool().Where(card => card.tier <= tavernTier).ToList();
        int cardsToShow = shopSizes.ContainsKey(tavernTier) ? shopSizes[tavernTier] : 3;
        cardsToShow = Mathf.Min(cardsToShow, tempPool.Count);
        if (tempPool.Count == 0)
        {
            Debug.LogWarning($"Player {playerId}: No cards available for Tier {tavernTier} in shared pool (size: {allCards.Count})");
            return;
        }
        for (int i = 0; i < cardsToShow; i++)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = Random.Range(0, tempPool.Count);
            availableCards[playerId].Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
            Debug.Log($"Player {playerId}: Available card: {availableCards[playerId][i].cardName} in place {i+1}/{cardsToShow}");
        }
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

