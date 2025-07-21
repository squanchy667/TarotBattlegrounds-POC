using UnityEngine;
using System.Collections.Generic;

public class TavernManager : MonoBehaviour
{

    void Start()  // Keeps original logic, runs auto
    {
        RefreshShop();  // Call our new method
    }
    [SerializeField] public List<Card> allCards = new List<Card>();  // Assign in Inspector
    public List<Card> availableCards;  // Tavern shop
    public int coins = 2;  // Starting coins
    public List<Card> board = new List<Card>();  // Player's board
    private int localTurn = 0;  // Define here

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
        coins = Mathf.Min(coins + 1, 10);  // Increment coins
        availableCards.Clear();  // Reset available cards
        int cardsToShow = Mathf.Min(3, allCards.Count);
        for (int i = 0; i < cardsToShow; i++)
        {
            int randomIndex = Random.Range(0, allCards.Count);
            availableCards.Add(allCards[randomIndex]);
            allCards.RemoveAt(randomIndex);  // Temporary removal (see step 3 for reset)
        }
        Debug.Log($"Tavern refreshed: Turn {localTurn}, Available cards - {availableCards.Count}, Coins increased from {oldCoins} to {coins}");
        localTurn++;  // Increment turn counter
    }
}