using UnityEngine;
using System.Collections.Generic;

public class TavernManager : MonoBehaviour
{
    [SerializeField] private List<Card> masterCards = new List<Card>();  // Assign unique cards in Inspector
    private List<Card> allCards = new List<Card>();  // Runtime pool
    public List<Card> availableCards;  // Tavern shop
    public int coins = 3;  // Starting coins
    public List<Card> board = new List<Card>();  // Player's board
    private int localTurn = 0;  // Turn counter

    private Dictionary<int, int> tierCopies = new Dictionary<int, int>()
    {
        {1, 16}, {2, 15}, {3, 13}, {4, 11}, {5, 9}, {6, 7}
    };

    void Start()
    {
        ResetPool();  // Initial pool
        RefreshShop();  // Initial refresh
    }

    public void BuyCard(int index)
    {
        if (index < 0 || index >= availableCards.Count) return;
        Card card = availableCards[index];
        int cost = card.tier * 3;  // Tier-based cost
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
        int value = card.tier * 2;  // Sell value
        coins += value;
        board.RemoveAt(index);
        Debug.Log($"Sold {card.cardName} (Tier {card.tier}) for {value} coins. Coins: {coins}");
    }

    public void RefreshShop()
    {
        int oldCoins = coins;
        coins = Mathf.Min(coins + 1, 10);
        availableCards.Clear();
        Debug.Log("Master cards count: " + masterCards.Count);  // NEW LOG
        List<Card> tempPool = GetFullPool();  // Temp for selection
        int cardsToShow = Mathf.Min(3, tempPool.Count);
        for (int i = 0; i < cardsToShow; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            availableCards.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        Debug.Log($"Tavern refreshed: Turn {localTurn}, Available cards - {availableCards.Count}, Coins increased from {oldCoins} to {coins}");
        if (tempPool.Count == 0) Debug.LogWarning("Temp pool empty! Check masterCards or tierCopies.");
        localTurn++;
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
        Debug.Log("Generated full pool size: " + fullPool.Count);
        foreach (Card uniqueCard in masterCards)
        {
            int copies = tierCopies.ContainsKey(uniqueCard.tier) ? tierCopies[uniqueCard.tier] : 1;
            for (int i = 0; i < copies; i++)
            {
                fullPool.Add(uniqueCard);
            }
        }
        return fullPool;
    }
}