using UnityEngine;
using System.Collections.Generic;

public class TavernManager : MonoBehaviour
{
    [SerializeField] private List<Card> masterCards = new List<Card>();  // Assign all samples here
    public List<Card> allCards = new List<Card>();  // Working copy, reset each turn
    public List<Card> availableCards;  // Tavern shop
    public int coins = 2;  // Starting coins
    public List<Card> board = new List<Card>();  // Player's board
    private int localTurn = 0;  // Define here

    private Dictionary<int, int> tierCopies = new Dictionary<int, int>()
    {
        {1, 16}, {2, 15}, {3, 13}, {4, 11}, {5, 9}, {6, 7}
    };

    void Start()  // Keeps original logic, runs auto
    {
        allCards.AddRange(masterCards);  // Initialize with master list
        RefreshShop();  // Call our new method
    }

    public void BuyCard(int index)  // Buy from availableCards by index
    {
        if (index < 0 || index >= availableCards.Count) return;  // Invalid index
        Card card = availableCards[index];
        int cost = card.tier * 3;  // Tier-based cost (e.g., Tier 1 = 3 coins)
        if (coins >= cost && board.Count < 7)  // Check coins and board limit
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

    private List<Card> GenerateFullPool()
    {
        List<Card> fullPool = new List<Card>();
        foreach (Card uniqueCard in masterCards)
        {
            int copies = tierCopies.ContainsKey(uniqueCard.tier) ? tierCopies[uniqueCard.tier] : 1;  // Default 1 if tier not found
            for (int i = 0; i < copies; i++)
            {
                fullPool.Add(uniqueCard);  // Add copies
            }
        }
        return fullPool;
    }

    public void SellCard(int index)  // Sell from board by index
    {
        if (index < 0 || index >= board.Count) return;  // Invalid index
        Card card = board[index];
        int value = card.tier * 2;  // Sell value (e.g., Tier 1 = 2 coins)
        coins += value;
        board.RemoveAt(index);
        Debug.Log($"Sold {card.cardName} (Tier {card.tier}) for {value} coins. Coins: {coins}");
    }

    public void RefreshShop()
    {
        int oldCoins = coins;
        coins = Mathf.Min(coins + 1, 10);
        availableCards.Clear();  // Reset available
        allCards = GenerateFullPool();  // Generate fresh pool each turn
        int cardsToShow = Mathf.Min(3, allCards.Count);
        for (int i = 0; i < cardsToShow; i++)
        {
            int randomIndex = Random.Range(0, allCards.Count);
            availableCards.Add(allCards[randomIndex]);
            allCards.RemoveAt(randomIndex);  // Remove for this turn
        }
        Debug.Log($"Tavern refreshed: Turn {localTurn}, Available cards - {availableCards.Count}, Coins increased from {oldCoins} to {coins}");
        localTurn++;
    }
}