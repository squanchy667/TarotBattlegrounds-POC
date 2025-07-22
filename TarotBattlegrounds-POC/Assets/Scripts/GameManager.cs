using UnityEngine;
using System.Collections;  // For Coroutines
using System.Collections.Generic;  // For List<T>

public class GameManager : MonoBehaviour  // Base class for Unity scripts
{
    // Enum for phases - easy to expand (e.g., add HeroSelect)
    public enum GamePhase { Recruit, Combat }
    public TavernManager tavern;  // Reference to TavernManager component

    private GamePhase currentPhase = GamePhase.Recruit;  // Default start
    private int turnNumber = 1;  // Turn counter
    private float recruitTimer = 5f;  // Shorter for testing (was 60f)

    void Start()  // Runs once on scene load
    {
        StartCoroutine(GameLoop());  // Starts the loop
    }

    IEnumerator GameLoop()  // Coroutine method for sequencing
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
                    // Test buy first card
                    if (tavern.availableCards.Count > 0) tavern.BuyCard(0);
                    // Test sell if board has cards
                    if (tavern.board.Count > 0) tavern.SellCard(0);
                }
            yield return new WaitForSeconds(recruitTimer);  // Pause for timer

            // Combat Phase
            currentPhase = GamePhase.Combat;
            if (tavern != null && tavern.availableCards.Count == 0)
                {
                    tavern.allCards = tavern.GenerateFullPool();  // Force full repopulation
                }
            Debug.Log($"Turn {turnNumber}: Combat Phase - Battles commence!");
            yield return new WaitForSeconds(5f);  // Short combat sim

            turnNumber++;  // Increment turn
        }
    }

    private void SimulateAI()
{
    Debug.Log("AI opponent: Randomly buying and positioning cards (placeholder).");
    // Later: Expand to actual random actions for testing combats
}
}