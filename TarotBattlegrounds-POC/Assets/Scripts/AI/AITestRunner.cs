using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runs automated AI vs AI battles for balance testing.
/// Attach to a test scene GameObject to run simulations.
/// </summary>
public class AITestRunner : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Number of games to simulate")]
    public int gamesToRun = 100;

    [Tooltip("Maximum turns per game")]
    public int maxTurnsPerGame = 30;

    [Tooltip("Delay between games (seconds)")]
    public float delayBetweenGames = 0.1f;

    [Header("AI Settings")]
    public AIDifficulty ai1Difficulty = AIDifficulty.Medium;
    public AIDifficulty ai2Difficulty = AIDifficulty.Medium;

    [Header("Results")]
    [SerializeField] private int ai1Wins = 0;
    [SerializeField] private int ai2Wins = 0;
    [SerializeField] private int ties = 0;
    [SerializeField] private int gamesCompleted = 0;
    [SerializeField] private float avgGameLength = 0f;

    private bool isRunning = false;
    private List<int> gameLengths = new List<int>();

    /// <summary>
    /// Start running the balance test suite.
    /// </summary>
    [ContextMenu("Run Balance Tests")]
    public void StartTests()
    {
        if (isRunning)
        {
            Debug.LogWarning("[AITestRunner] Tests already running!");
            return;
        }

        ResetStats();
        StartCoroutine(RunTestSuite());
    }

    /// <summary>
    /// Stop running tests.
    /// </summary>
    [ContextMenu("Stop Tests")]
    public void StopTests()
    {
        StopAllCoroutines();
        isRunning = false;
        Debug.Log("[AITestRunner] Tests stopped.");
        PrintResults();
    }

    private void ResetStats()
    {
        ai1Wins = 0;
        ai2Wins = 0;
        ties = 0;
        gamesCompleted = 0;
        avgGameLength = 0f;
        gameLengths.Clear();
    }

    private IEnumerator RunTestSuite()
    {
        isRunning = true;
        Debug.Log($"[AITestRunner] Starting {gamesToRun} AI vs AI games...");
        Debug.Log($"[AITestRunner] AI1: {ai1Difficulty} vs AI2: {ai2Difficulty}");

        for (int i = 0; i < gamesToRun && isRunning; i++)
        {
            var result = SimulateSingleGame();
            gameLengths.Add(result.turns);

            if (result.winner == 1)
                ai1Wins++;
            else if (result.winner == 2)
                ai2Wins++;
            else
                ties++;

            gamesCompleted++;

            if (gamesCompleted % 10 == 0)
            {
                Debug.Log($"[AITestRunner] Progress: {gamesCompleted}/{gamesToRun} games");
            }

            yield return new WaitForSeconds(delayBetweenGames);
        }

        avgGameLength = gameLengths.Count > 0 ? (float)gameLengths.Average() : 0f;
        isRunning = false;
        PrintResults();
    }

    /// <summary>
    /// Simulate a single game between two AI players.
    /// Returns (winner: 0=tie, 1=AI1, 2=AI2, turns: game length)
    /// </summary>
    private (int winner, int turns) SimulateSingleGame()
    {
        // Create simulated players
        var ai1State = new SimulatedPlayer(1, ai1Difficulty);
        var ai2State = new SimulatedPlayer(2, ai2Difficulty);

        int turn = 0;

        while (turn < maxTurnsPerGame && ai1State.health > 0 && ai2State.health > 0)
        {
            turn++;

            // Recruit phase for both AIs
            ai1State.ExecuteRecruitPhase(turn);
            ai2State.ExecuteRecruitPhase(turn);

            // Combat phase
            var combatResult = SimulateCombat(ai1State, ai2State);

            // Apply damage
            if (combatResult.winner == 0) // Tie
            {
                ai1State.health -= combatResult.damage;
                ai2State.health -= combatResult.damage;
            }
            else if (combatResult.winner == 1)
            {
                ai2State.health -= combatResult.damage;
            }
            else
            {
                ai1State.health -= combatResult.damage;
            }
        }

        // Determine winner
        int winner = 0;
        if (ai1State.health > ai2State.health)
            winner = 1;
        else if (ai2State.health > ai1State.health)
            winner = 2;

        return (winner, turn);
    }

    /// <summary>
    /// Simplified combat simulation for testing.
    /// </summary>
    private (int winner, int damage) SimulateCombat(SimulatedPlayer ai1, SimulatedPlayer ai2)
    {
        // Clone boards
        var board1 = ai1.board.Select(c => c.Clone()).ToList();
        var board2 = ai2.board.Select(c => c.Clone()).ToList();

        // Simple combat: alternating attacks until one side eliminated
        while (board1.Count > 0 && board2.Count > 0)
        {
            // AI1 attacks
            if (board1.Count > 0 && board2.Count > 0)
            {
                var attacker = board1[0];
                var target = board2[0];

                target.health -= attacker.attack;
                attacker.health -= target.attack;

                if (target.health <= 0) board2.RemoveAt(0);
                if (attacker.health <= 0) board1.RemoveAt(0);
            }

            // AI2 attacks
            if (board1.Count > 0 && board2.Count > 0)
            {
                var attacker = board2[0];
                var target = board1[0];

                target.health -= attacker.attack;
                attacker.health -= target.attack;

                if (target.health <= 0) board1.RemoveAt(0);
                if (attacker.health <= 0) board2.RemoveAt(0);
            }
        }

        // Calculate damage and winner
        int damage = 0;
        int winner = 0;

        if (board1.Count == 0 && board2.Count == 0)
        {
            winner = 0; // Tie
        }
        else if (board1.Count > 0)
        {
            winner = 1;
            damage = Mathf.Min(5, board1.Sum(c => c.tier) + ai1.tavernTier);
        }
        else
        {
            winner = 2;
            damage = Mathf.Min(5, board2.Sum(c => c.tier) + ai2.tavernTier);
        }

        return (winner, damage);
    }

    private void PrintResults()
    {
        float ai1WinRate = gamesCompleted > 0 ? (ai1Wins / (float)gamesCompleted) * 100f : 0f;
        float ai2WinRate = gamesCompleted > 0 ? (ai2Wins / (float)gamesCompleted) * 100f : 0f;
        float tieRate = gamesCompleted > 0 ? (ties / (float)gamesCompleted) * 100f : 0f;

        Debug.Log("=== AI BALANCE TEST RESULTS ===");
        Debug.Log($"Games completed: {gamesCompleted}");
        Debug.Log($"AI1 ({ai1Difficulty}): {ai1Wins} wins ({ai1WinRate:F1}%)");
        Debug.Log($"AI2 ({ai2Difficulty}): {ai2Wins} wins ({ai2WinRate:F1}%)");
        Debug.Log($"Ties: {ties} ({tieRate:F1}%)");
        Debug.Log($"Average game length: {avgGameLength:F1} turns");
        Debug.Log("===============================");
    }

    /// <summary>
    /// Simplified player state for testing without full Unity objects.
    /// </summary>
    private class SimulatedPlayer
    {
        public int id;
        public int health = 40;
        public int coins = 0;
        public int tavernTier = 1;
        public List<Card> board = new List<Card>();
        public List<Card> hand = new List<Card>();
        public AIDifficulty difficulty;

        // Simulated upgrade costs
        private Dictionary<int, int> upgradeCosts = new Dictionary<int, int>
        {
            {2, 6}, {3, 8}, {4, 9}, {5, 10}, {6, 11}
        };

        public SimulatedPlayer(int id, AIDifficulty diff)
        {
            this.id = id;
            this.difficulty = diff;
        }

        public void ExecuteRecruitPhase(int turn)
        {
            // Get coins for this turn
            coins = Mathf.Min(3 + (turn - 1), 10);

            // Simple AI logic
            ConsiderUpgrade();
            BuySimulatedCards(turn);
            PlayCardsToBoard();
        }

        private void ConsiderUpgrade()
        {
            if (tavernTier >= 6) return;

            int cost = upgradeCosts.ContainsKey(tavernTier + 1) ? upgradeCosts[tavernTier + 1] : 999;

            // Upgrade decision based on difficulty
            bool shouldUpgrade = false;

            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    shouldUpgrade = coins >= cost + 3 && Random.value > 0.5f;
                    break;
                case AIDifficulty.Medium:
                    shouldUpgrade = coins >= cost + 1;
                    break;
                case AIDifficulty.Hard:
                    shouldUpgrade = coins >= cost;
                    break;
            }

            if (shouldUpgrade)
            {
                coins -= cost;
                tavernTier++;
            }
        }

        private void BuySimulatedCards(int turn)
        {
            // Create simulated cards based on tier
            int cardsToBuy = difficulty == AIDifficulty.Hard ? 3 :
                            difficulty == AIDifficulty.Medium ? 2 : 1;

            while (coins >= 3 && hand.Count < 10 && cardsToBuy > 0)
            {
                // Create a simulated card
                Card card = ScriptableObject.CreateInstance<Card>();
                card.tier = Random.Range(1, tavernTier + 1);
                card.attack = card.tier + Random.Range(0, 3);
                card.health = card.tier + Random.Range(1, 4);
                card.cardName = $"SimCard_{id}_{turn}_{hand.Count}";

                // Assign random tribe
                TribeType[] allTribes = { TribeType.Pentacles, TribeType.Cups, TribeType.Swords, TribeType.Wands };
                card.tribes = new TribeType[] { allTribes[Random.Range(0, allTribes.Length)] };

                hand.Add(card);
                coins -= 3;
                cardsToBuy--;
            }
        }

        private void PlayCardsToBoard()
        {
            while (hand.Count > 0 && board.Count < 7)
            {
                board.Add(hand[0]);
                hand.RemoveAt(0);
            }
        }
    }
}
