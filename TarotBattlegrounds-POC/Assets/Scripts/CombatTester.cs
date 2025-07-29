using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatTester : MonoBehaviour
{
    public List<Card> playerBoard; // Assign in Inspector
    public List<Card> aiBoard;     // Assign in Inspector
    public int simulationRuns = 10; // Default to 10 for testing
    public int tavernTier = 1;     // Default tavern tier
    private CombatManager combatManager;

    void Start()
    {
        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null) Debug.LogError("CombatManager not found in scene!");
        if (playerBoard == null || aiBoard == null) Debug.LogWarning("Assign player and AI boards in Inspector!");
        RunCombatTest();
    }

    [ContextMenu("Run Combat Test")]
    public void RunCombatTest()
    {
        if (combatManager == null || playerBoard == null || aiBoard == null || playerBoard.Count == 0 || aiBoard.Count == 0)
        {
            Debug.LogError("Cannot run test: Missing CombatManager or invalid boards.");
            return;
        }

        int playerWins = 0;
        int aiWins = 0;
        int ties = 0;

        for (int i = 0; i < simulationRuns; i++)
        {
            Debug.Log($"Run {i + 1} / {simulationRuns}: Boards P={playerBoard.Count}, A={aiBoard.Count}");
            List<Card> pBoard = playerBoard.Select(c => c.Clone()).ToList();
            List<Card> aBoard = aiBoard.Select(c => c.Clone()).ToList();
            int damage = combatManager.SimulateBattle(pBoard, aBoard, tavernTier);
            Debug.Log($"Run {i + 1} outcome: Damage = {damage}");

            // Count winner/tie based on last logged state
            if (pBoard.Count == 0 && aBoard.Count == 0)
                ties++;
            else if (aBoard.Count == 0)
                playerWins++;
            else if (pBoard.Count == 0)
                aiWins++;
        }

        float pctPlayerWins = (float)playerWins / simulationRuns * 100;
        float pctAIWins = (float)aiWins / simulationRuns * 100;
        float pctTies = (float)ties / simulationRuns * 100;

        string result = $"Player wins: {pctPlayerWins:F2}%, AI wins: {pctAIWins:F2}%, Ties: {pctTies:F2}%";
        Debug.Log(result);
    }
}