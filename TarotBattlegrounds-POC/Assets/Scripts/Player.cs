using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Player : MonoBehaviour
{
    public int playerId;
    public List<Card> hand = new List<Card>();
    public List<Card> board = new List<Card>();
    public int coins = 0;
    public int currentTavernTier = 1;
    private TavernManager tavern;
    // Dictionary for base upgrade costs: key = target tier, value = base cost
    private Dictionary<int, int> baseUpgradeCosts = new Dictionary<int, int>()
    {
        {2, 6}, {3, 8}, {4, 9}, {5, 10}, {6, 11}
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
        int cost = 3 + card.buyCostModifier;
        if (coins >= cost && hand.Count < 10)
        {
            coins -= cost;
            Card newCard = card.Clone();
            hand.Add(newCard);
            tavern.RemoveCardFromPool(card);
            tavern.availableCards[playerId].RemoveAt(index);
            Debug.Log($"Player {playerId}: Bought {newCard.cardName} (Tier {newCard.tier}) for {cost} coins. Hand size: {hand.Count}, Coins: {coins}, Pool size: {tavern.GetFullPool().Count}");
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
        if (index < 0 || index >= board.Count) return;
        Card card = board[index];
        int value = 1 + card.sellValueModifier;
        coins += value;
        tavern.ReturnCardToPool(card);
        board.RemoveAt(index);
        Debug.Log($"Player {playerId}: Sold {card.cardName} (Tier {card.tier}) for {value} coins. Coins: {coins}, Pool size: {tavern.GetFullPool().Count}");
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
        if (boardIndex < 0 || boardIndex > board.Count) boardIndex = board.Count;
        board.Insert(boardIndex, card);
        hand.RemoveAt(handIndex);
        TriggerSummoning(card);
        Debug.Log($"Player {playerId}: Played {card.cardName} (Tier {card.tier}) to board position {boardIndex}. Board size: {board.Count}, Hand size: {hand.Count}");
    }

    public void EndRecruitPhase()
    {
        foreach (var card in board)
        {
            if (card.effectType == Card.EffectType.LastReading)
            {
                TriggerLastReading(card);
            }
        }
        Debug.Log($"Player {playerId}: Recruit phase ended. LastReading effects triggered.");
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
        switch (effect)
        {
            case "BuffWands":
                foreach (var c in board)
                {
                    if (c.tribe == "Wands")
                    {
                        c.attack += value;
                        c.health += value;
                        Debug.Log($"Player {playerId}: LastReading: {card.cardName} buffs {c.cardName} by {value}/{value} (New Stats: {c.attack}/{c.health})");
                    }
                }
                break;
            default:
                Debug.LogWarning($"Player {playerId}: Unknown LastReading effect: {effect} for {card.cardName}");
                break;
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
        if (coins >= cost)
        {
            coins -= cost;
            currentTavernTier++;
            tierTurnCounter[currentTavernTier] = 0; // Reset turn counter for new tier
            Debug.Log($"Player {playerId}: Upgraded to Tavern Tier {currentTavernTier} for {cost} coins. Coins left: {coins}, Next Upgrade Cost: {GetUpgradeCost()}");
        }
        else
        {
            Debug.Log($"Player {playerId}: Cannot upgrade tavern: Coins = {coins}, Required = {cost}");
        }
    }

    public int GetUpgradeCost()
    {
        int nextTier = currentTavernTier + 1;
        if (!baseUpgradeCosts.ContainsKey(nextTier))
        {
            Debug.LogWarning($"Player {playerId}: No upgrade cost defined for Tier {nextTier}");
            return 999; // Prevent upgrading beyond max tier
        }
        int baseCost = baseUpgradeCosts[nextTier];
        int turnsElapsed = tierTurnCounter[currentTavernTier];
        int cost = Mathf.Max(baseCost - turnsElapsed, 1);
        return cost;
    }

    public void RefreshShop(int gameTurn)
    {
        if (tavern == null)
        {
            Debug.LogError($"Player {playerId}: Cannot refresh shop, TavernManager not found!");
            return;
        }
        int oldCoins = coins;
        coins = Mathf.Min(3 + (gameTurn - 1), 10);
        tierTurnCounter[currentTavernTier] = tierTurnCounter.ContainsKey(currentTavernTier) ? tierTurnCounter[currentTavernTier] + 1 : 1;
        tavern.RefreshPlayerShop(playerId, currentTavernTier);
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
            coins -= 1;
            tavern.RefreshPlayerShop(playerId, currentTavernTier);
            Debug.Log($"Player {playerId}: Tavern shop rerolled: Available cards: {tavern.availableCards[playerId].Count}, Coins: {coins}, Tier: {currentTavernTier}, Upgrade Cost: {GetUpgradeCost()}");
        }
        else
        {
            Debug.Log($"Player {playerId}: Cannot reroll: Coins = {coins}");
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