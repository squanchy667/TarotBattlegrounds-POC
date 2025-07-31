using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public enum GamePhase { Recruit, Combat }
    public TavernManager tavern;  // Reference to TavernManager component

    private GamePhase currentPhase = GamePhase.Recruit;  // Default start
    private int turnNumber = 1;  // Turn counter
    private float recruitTimer = 5f;  // Shorter for testing (was 60f)
    private int health = 40;  // Starting health

    void Start()
    {
        if (tavern == null) Debug.LogError("TavernManager not found!");
        StartCoroutine(GameLoop());  // Starts the loop
    }

    IEnumerator GameLoop()
    {
        while (health > 0)
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
                while (tavern.currentTavernTier < 6 && tavern.coins >= tavern.GetUpgradeCost())
                {
                    tavern.UpgradeTavern();
                }
                while (tavern.availableCards.Count > 0 && tavern.coins >= 3 && tavern.board.Count < 7) { tavern.BuyCard(0); }
                int rerolls = 0; while (tavern.coins >= 1 && rerolls < 2) { tavern.RefreshTavernShop(); rerolls++; }
                Debug.Log("Shop Offered: " + string.Join(", ", tavern.availableCards.Select(c => c.cardName + " (Tier " + c.tier + ")")));
                Debug.Log("Board: " + string.Join(", ", tavern.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " Size " + tavern.board.Count);
            }
            yield return new WaitForSeconds(recruitTimer);

            // Combat Phase
            currentPhase = GamePhase.Combat;
            Debug.Log("Current Phase: " + currentPhase);
            List<Card> aiBoard = GenerateAIBoard(turnNumber);
            int damage = CombatManager.SimulateBattle(tavern.board, aiBoard, tavern.currentTavernTier);
            health -= damage;
            Debug.Log("Simulating combat... Player Board: " + string.Join(", ", tavern.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " | AI Board: " + string.Join(", ", aiBoard.Select(c => c.cardName + " (Tier " + c.tier + ")")));
            Debug.Log("Combat outcome: Player takes " + damage + " damage. Health remaining: " + health);
            Debug.Log($"Turn {turnNumber}");
            yield return new WaitForSeconds(5f);

            turnNumber++;
            if (health <= 0) { Debug.Log("Game Over"); break; }
        }
    }

    private void SimulateAI()
    {
        Debug.Log("AI opponent: Randomly buying and positioning cards (placeholder).");
    }

    private List<Card> GenerateAIBoard(int turnNumber)
    {
        List<Card> ai = new List<Card>();
        int aiSize = Mathf.Min(turnNumber + Random.Range(0, 2), 7);
        List<Card> filteredPool = tavern.GetFullPool().Where(c => c.tier <= tavern.currentTavernTier + 1).ToList();
        for (int i = 0; i < aiSize; i++)
        {
            if (filteredPool.Count > 0)
            {
                int idx = Random.Range(0, filteredPool.Count);
                ai.Add(filteredPool[idx]);
                filteredPool.RemoveAt(idx);
            }
        }
        return ai;
    }
}