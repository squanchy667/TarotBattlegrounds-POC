using UnityEngine;
using System.Collections;  // For Coroutines
using System.Collections.Generic;  // For List<T>
using System.Linq;


public class GameManager : MonoBehaviour
{
    public enum GamePhase { Recruit, Combat }
    public TavernManager tavern;  // Reference to TavernManager component

    private GamePhase currentPhase = GamePhase.Recruit;  // Default start
    private int turnNumber = 1;  // Turn counter
    private float recruitTimer = 5f;  // Shorter for testing (was 60f)
    private int health = 40;  // New: Starting health

    void Start()
    {
        StartCoroutine(GameLoop());  // Starts the loop
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            // Recruit Phase
            currentPhase = GamePhase.Recruit;
            Debug.Log("Current Phase: " + currentPhase);
            SimulateAI();
            Debug.Log($"Turn {turnNumber}: Recruit Phase - Time to build your board!");
            if (tavern != null)
            {
                int expectedCoins = Mathf.Min(3 + (turnNumber - 1), 10);
                Debug.Log($"Recruit Start: Coins = {tavern.coins}/{expectedCoins}, Upgrade Cost = {tavern.GetUpgradeCost()}, Current Tier = {tavern.currentTavernTier}");
                tavern.RefreshShop();
                // Upgrade (comment for other tests)
                while (tavern.currentTavernTier < 6 && tavern.coins >= tavern.GetUpgradeCost())
                {
                    tavern.UpgradeTavern();
                }
                // Buy/sell (comment for other tests)
                while (tavern.availableCards.Count > 0 && tavern.coins >= 3 && tavern.board.Count < 7)
                {
                    tavern.BuyCard(0);
                }
                while (tavern.board.Count > 0)
                {
                    tavern.SellCard(0);
                }
                // Reroll (comment for other tests)
                while (tavern.coins >= 1)
                {
                    tavern.RefreshTavernShop();
                }
                Debug.Log("Shop Offered: " + string.Join(", ", tavern.availableCards.Select(c => c.cardName + " (Tier " + c.tier + ")")));
                Debug.Log("Board: " + string.Join(", ", tavern.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " Size " + tavern.board.Count);
            }
            yield return new WaitForSeconds(recruitTimer);

            // Combat Phase
            currentPhase = GamePhase.Combat;
            Debug.Log("Current Phase: " + currentPhase);
            Debug.Log("Simulating combat... Player Board: " + string.Join(", ", tavern.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " | AI Board: Placeholder AI cards");
            int damage = Mathf.Min(5, turnNumber);
            health -= damage;
            Debug.Log("Combat outcome: Player takes " + damage + " damage. Health remaining: " + health);
            Debug.Log($"Turn {turnNumber}");
            yield return new WaitForSeconds(5f);

            turnNumber++;
            if (health <= 0) break;
        }
    }

    private void SimulateAI()
    {
        Debug.Log("AI opponent: Randomly buying and positioning cards (placeholder).");
    }
}