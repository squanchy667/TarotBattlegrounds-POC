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
        while (true)  // Loops forever (add end condition later, e.g., health <= 0)
        {
            // Recruit Phase
            currentPhase = GamePhase.Recruit;
            Debug.Log("Current Phase: " + currentPhase);
            SimulateAI();
            Debug.Log($"Turn {turnNumber}: Recruit Phase - Time to build your board!");
            if (tavern != null)
            {
                tavern.RefreshShop();
                if (tavern.currentTavernTier < 6 && tavern.coins >= tavern.GetUpgradeCost())
                {
                    tavern.UpgradeTavern();
                }
                if (tavern.coins >= 1)  // Test refresh if coins allow
                {
                    tavern.RefreshTavernShop();
                }
                if (tavern.availableCards.Count > 0 && tavern.coins >= 3) tavern.BuyCard(0);
                if (tavern.board.Count > 0) tavern.SellCard(0);
            }
            yield return new WaitForSeconds(recruitTimer);

            // Combat Phase
            currentPhase = GamePhase.Combat;
            Debug.Log("Current Phase: " + currentPhase);
            Debug.Log("Simulating combat... Board: " + string.Join(", ", tavern.board.Select(c => c.cardName + " (Tier " + c.tier + ")")));
            int damage = Mathf.Min(5, turnNumber);  // Fixed: Use Mathf.Min
            health -= damage;  // Update health
            Debug.Log("Combat outcome: Player takes " + damage + " damage. Health remaining: " + health);
            Debug.Log($"Turn {turnNumber}");
            yield return new WaitForSeconds(5f);

            turnNumber++;  // Increment turn
            if (health <= 0) break;  // End game if health <=0
        }
    }

    private void SimulateAI()
    {
        Debug.Log("AI opponent: Randomly buying and positioning cards (placeholder).");
    }
}