using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TavernManager : MonoBehaviour
{
    [SerializeField] private List<Card> masterCards = new List<Card>();  // Assign unique cards in Inspector
    private List<Card> allCards = new List<Card>();  // Runtime pool
    public List<Card> availableCards;  // Tavern shop
    public int coins = 3;  // Starting coins
    public List<Card> board = new List<Card>();  // Player's board
    private int localTurn = 0;  // Turn counter
    public int currentTavernTier = 1;
    private int upgradeCostReduction = 0;  // Accumulates if not upgraded
    
    private Dictionary<int, int> baseUpgradeCosts = new Dictionary<int, int>()
    {
        {2, 5}, {3, 7}, {4, 8}, {5, 9}, {6, 10}
    };

    private Dictionary<int, int> tierCopies = new Dictionary<int, int>()
    {
        {1, 16}, {2, 15}, {3, 13}, {4, 11}, {5, 9}, {6, 7}
    };

    void Start()
    {
        ResetPool();  // Initial pool
    }

    public void BuyCard(int index)
    {
        if (index < 0 || index >= availableCards.Count) return;
        Card card = availableCards[index];
        int cost = 3;  // Fixed, unless effect overrides (add check later)
        // Example override placeholder: if (card.ability.Contains("cost reduction")) cost -= 1;
        if (coins >= cost && board.Count < 7)
        {
            coins -= cost;
            board.Add(card);
            availableCards.RemoveAt(index);
            Debug.Log($"Bought {card.cardName} (Tier {card.tier}) for {cost} coins. Board size: {board.Count}, Coins left: {coins}");
        }
        else
        {
            Debug.Log("Cannot buy: Insufficient coins or board full.");
        }
    }

    public void SellCard(int index)
    {
        if (index < 0 || index >= board.Count) return;
        Card card = board[index];
        int value = 1;
        coins += value;  // Ensure addition
        board.RemoveAt(index);
        Debug.Log($"Sold {card.cardName} (Tier {card.tier}) for {value} coins. Coins: {coins}");
    }

    public void RefreshShop()
    {
        int oldCoins = coins;
        int expectedCoins = Mathf.Min((3 + localTurn), 10);  // Reset to expected
        coins = expectedCoins;
        upgradeCostReduction++;
        availableCards.Clear();
        Debug.Log("Master cards count: " + masterCards.Count);
        List<Card> tempPool = GetFullPool().Where(card => card.tier <= currentTavernTier).ToList();
        int cardsToShow = GetShopSize();  // Scale per tier
        cardsToShow = Mathf.Min(cardsToShow, tempPool.Count);
        if (tempPool.Count == 0) Debug.LogWarning("Temp pool empty! Check masterCards or tierCopies.");
        for (int i = 0; i < cardsToShow; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            availableCards.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        Debug.Log($"Tavern refreshed: Turn {localTurn}, Available cards - {availableCards.Count}, Coins increased from {oldCoins} to {coins}, Current Tier: {currentTavernTier}");
        localTurn++;
    }

    private int GetShopSize()  // New
    {
        switch (currentTavernTier)
        {
            case 1: return 3;
            case 2: return 4;
            case 3: return 4;
            case 4: return 5;
            case 5: return 5;
            case 6: return 6;
            default: return 3;
        }
    }

    public void ResetPool()
    {
        allCards = GetFullPool();  // Reset to full pool
    }

    public List<Card> GetFullPool()
    {
        return GenerateFullPool();
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
        Debug.Log("Generated full pool size: " + fullPool.Count);  // Move here
        return fullPool;
    }

    public void UpgradeTavern()
    {
        if (currentTavernTier >= 6) return;
        int nextTier = currentTavernTier + 1;
        int baseCost = baseUpgradeCosts.ContainsKey(nextTier) ? baseUpgradeCosts[nextTier] : 10;
        int actualCost = Mathf.Max(1, baseCost - upgradeCostReduction);
        if (coins >= actualCost)
        {
            coins -= actualCost;
            currentTavernTier++;
            upgradeCostReduction = 0;  // Reset after upgrade
            Debug.Log($"Upgraded to Tavern Tier {currentTavernTier} for {actualCost} coins.");
        }
        else
        {
            Debug.Log("Cannot upgrade: Insufficient coins.");
        }
    }

    public int GetUpgradeCost()
    {
        if (currentTavernTier >= 6) return int.MaxValue;
        int nextTier = currentTavernTier + 1;
        int baseCost = baseUpgradeCosts.ContainsKey(nextTier) ? baseUpgradeCosts[nextTier] : 10;
        return Mathf.Max(1, baseCost - upgradeCostReduction);
    }

    public void RefreshTavernShop()
    {
        if (coins >= 1)
        {
            coins -= 1;
            availableCards.Clear();
            List<Card> tempPool = GetFullPool().Where(card => card.tier <= currentTavernTier).ToList();
            int cardsToShow = GetShopSize();  // New helper for size per tier
            cardsToShow = Mathf.Min(cardsToShow, tempPool.Count);
            for (int i = 0; i < cardsToShow; i++)
            {
                int randomIndex = Random.Range(0, tempPool.Count);
                availableCards.Add(tempPool[randomIndex]);
                tempPool.RemoveAt(randomIndex);
            }
            Debug.Log($"Tavern shop rerolled: Available cards - {availableCards.Count}, Coins: {coins}, Current Tier: {currentTavernTier}");
        }
        else
        {
            Debug.Log("Cannot reroll: Insufficient coins.");
        }
    }
}