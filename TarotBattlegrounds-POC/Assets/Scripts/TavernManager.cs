using UnityEngine;
using System.Collections.Generic;

public class TavernManager : MonoBehaviour
{
    public List<Card> availableCards;  // Assign in Inspector

    void Start()  // Keeps original logic, runs auto
    {
        RefreshShop();  // Call our new method
    }

    public void RefreshShop()  // Public for external calls
    {
        Debug.Log("Tavern refreshed: Available cards - " + availableCards.Count);
        // Later: Randomize cards, etc.
    }

    public int coins = 3;  // Starting coins
    public List<Card> board = new List<Card>();  // Player's board (max 7)

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

    public void RefreshShop()  // Update to include coin growth
    {
        coins = Mathf.Min(coins + 1, 10);  // Increase coins per turn, cap at 10
        Debug.Log("Tavern refreshed: Available cards - " + availableCards.Count + ", Coins: " + coins);
    }
}