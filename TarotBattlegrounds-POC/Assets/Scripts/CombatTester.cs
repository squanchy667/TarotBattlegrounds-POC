using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatTester : MonoBehaviour
{
    public List<Card> playerBoard; // Assign in Inspector
    public List<Card> aiBoard;     // Assign in Inspector
    public int simulationRuns = 10; // Default to 10 for testing
    public int tavernTier = 1;     // Default tavern tier

    void Start()
    {
        if (playerBoard == null || aiBoard == null) Debug.LogWarning("Assign player and AI boards in Inspector!");
        RunCombatTest();
    }

    [ContextMenu("Run Combat Test")]
    public void RunCombatTest()
    {
        if (playerBoard == null || aiBoard == null || playerBoard.Count == 0 || aiBoard.Count == 0)
        {
            Debug.LogError("Cannot run test: Invalid boards.");
            return;
        }

        int playerWins = 0;
        int aiWins = 0;
        int ties = 0;

        for (int i = 0; i < simulationRuns; i++)
        {
            Debug.Log("Calling SimulateBattle for Run " + i);
            Debug.Log($"Run {i + 1} / {simulationRuns}: Boards P={playerBoard.Count}, A={aiBoard.Count}");
            List<Card> pBoard = playerBoard.Select(c => c.Clone()).ToList(); // Fresh clone
            List<Card> aBoard = aiBoard.Select(c => c.Clone()).ToList();     // Fresh clone
            if (pBoard.Count > 0 && aBoard.Count > 0) // Ensure valid boards
            {
                int damage = CombatManager.SimulateBattle(pBoard, aBoard, tavernTier);
                Debug.Log($"Run {i + 1} outcome: Damage = {damage}");
                Debug.Log($"Survivor P={pBoard.Count}, A={aBoard.Count}");

                // Count winner/tie based on final board state
                if (pBoard.Count == 0 && aBoard.Count == 0)
                    ties++;
                else if (aBoard.Count == 0)
                    playerWins++;
                else if (pBoard.Count == 0)
                    aiWins++;
            }
            else
            {
                Debug.LogWarning($"Run {i + 1} skipped: Invalid boards after clone (P={pBoard.Count}, A={aBoard.Count})");
            }
        }

        float pctPlayerWins = (float)playerWins / simulationRuns * 100;
        float pctAIWins = (float)aiWins / simulationRuns * 100;
        float pctTies = (float)ties / simulationRuns * 100;

        string result = $"Player 1 wins: {pctPlayerWins:F2}%, Player 2 wins: {pctAIWins:F2}%, Ties: {pctTies:F2}%";
        Debug.Log(result);
    }
}