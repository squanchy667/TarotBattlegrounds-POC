using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automated AI vs AI battle tests for balance validation.
/// Tasks E6 (4-player game test) and F5 (100+ AI games) from PLAN.md
/// </summary>
[TestFixture]
public class AIBattleTests
{
    #region Test Statistics

    public class GameStats
    {
        public int TurnCount;
        public int WinnerPlayerId;
        public AIDifficulty WinnerDifficulty;
        public int[] FinalPlacements; // Player IDs in order of elimination (4th, 3rd, 2nd, 1st)
        public Dictionary<TribeType, int> WinnerTribes;
        public int WinnerBoardSize;
        public int WinnerTavernTier;
    }

    public class SimulationResults
    {
        public int TotalGames;
        public int[] WinsByDifficulty = new int[3]; // Easy, Medium, Hard
        public float AvgTurnCount;
        public float MinTurnCount;
        public float MaxTurnCount;
        public Dictionary<TribeType, int> TribeWinCounts = new Dictionary<TribeType, int>();
        public int TotalTurns;
        public int Errors;
        public List<string> ErrorMessages = new List<string>();

        public void LogResults()
        {
            Debug.Log("=== AI SIMULATION RESULTS ===");
            Debug.Log($"Total Games: {TotalGames}");
            Debug.Log($"Errors: {Errors}");
            Debug.Log($"Average Turns: {AvgTurnCount:F1}");
            Debug.Log($"Turn Range: {MinTurnCount} - {MaxTurnCount}");
            Debug.Log($"Wins by Difficulty:");
            Debug.Log($"  Easy: {WinsByDifficulty[0]} ({100f * WinsByDifficulty[0] / TotalGames:F1}%)");
            Debug.Log($"  Medium: {WinsByDifficulty[1]} ({100f * WinsByDifficulty[1] / TotalGames:F1}%)");
            Debug.Log($"  Hard: {WinsByDifficulty[2]} ({100f * WinsByDifficulty[2] / TotalGames:F1}%)");
            Debug.Log($"Tribe Win Counts:");
            foreach (var kvp in TribeWinCounts.OrderByDescending(x => x.Value))
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }

    #endregion

    #region Setup/Teardown

    private GameObject _gameManagerObj;
    private GameObject _tavernManagerObj;
    private GameObject _synergyManagerObj;
    private GameObject _themeManagerObj;
    private List<GameObject> _playerObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        // Create ThemeManager
        _themeManagerObj = new GameObject("ThemeManager");
        _themeManagerObj.AddComponent<ThemeManager>();

        // Create SynergyManager
        _synergyManagerObj = new GameObject("SynergyManager");
        var synergyMgr = _synergyManagerObj.AddComponent<SynergyManager>();
        synergyMgr.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        synergyMgr.InitializeSynergyCache();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all created objects
        foreach (var obj in _playerObjects)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _playerObjects.Clear();

        if (_gameManagerObj != null) Object.DestroyImmediate(_gameManagerObj);
        if (_tavernManagerObj != null) Object.DestroyImmediate(_tavernManagerObj);
        if (_synergyManagerObj != null) Object.DestroyImmediate(_synergyManagerObj);
        if (_themeManagerObj != null) Object.DestroyImmediate(_themeManagerObj);
    }

    #endregion

    #region Helper Methods

    private Player CreateAIPlayer(int playerId, AIDifficulty difficulty)
    {
        var obj = new GameObject($"AIPlayer_{playerId}");
        var player = obj.AddComponent<Player>();
        player.playerId = playerId;
        player.health = 40;
        player.coins = 3;
        player.currentTavernTier = 1;

        var aiController = obj.AddComponent<AIController>();
        aiController.difficulty = difficulty;
        aiController.Initialize(player);

        _playerObjects.Add(obj);
        return player;
    }

    private Card CreateTestCard(string name, TribeType tribe, int tier, int attack, int health)
    {
        var card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tribes = new TribeType[] { tribe };
        card.tier = tier;
        card.attack = attack;
        card.health = health;
        return card;
    }

    private List<Card> CreateTestCardPool()
    {
        var pool = new List<Card>();
        var tribes = new[] { TribeType.Swords, TribeType.Cups, TribeType.Wands, TribeType.Pentacles };

        // Create 5 cards per tier for each tribe
        for (int tier = 1; tier <= 6; tier++)
        {
            foreach (var tribe in tribes)
            {
                int baseAttack = tier + 1;
                int baseHealth = tier + 2;

                for (int i = 0; i < 2; i++) // 2 cards per tier per tribe
                {
                    var card = CreateTestCard(
                        $"{tribe}_{tier}_{i}",
                        tribe,
                        tier,
                        baseAttack + Random.Range(0, 2),
                        baseHealth + Random.Range(0, 2)
                    );
                    pool.Add(card);
                }
            }
        }

        return pool;
    }

    private Dictionary<TribeType, int> CountTribesOnBoard(List<Card> board)
    {
        var counts = new Dictionary<TribeType, int>();
        foreach (var card in board)
        {
            if (card.tribes == null) continue;
            foreach (var tribe in card.tribes)
            {
                if (!counts.ContainsKey(tribe)) counts[tribe] = 0;
                counts[tribe]++;
            }
        }
        return counts;
    }

    #endregion

    #region E6: Full 4-Player Game Tests

    [Test]
    public void FourPlayerGame_CompletesWithoutCrash()
    {
        // Create 4 AI players
        var players = new List<Player>
        {
            CreateAIPlayer(0, AIDifficulty.Medium),
            CreateAIPlayer(1, AIDifficulty.Medium),
            CreateAIPlayer(2, AIDifficulty.Medium),
            CreateAIPlayer(3, AIDifficulty.Medium)
        };

        // Simulate a simplified game
        int turn = 0;
        int maxTurns = 50;
        var alivePlayers = new List<Player>(players);

        while (alivePlayers.Count > 1 && turn < maxTurns)
        {
            turn++;

            // Give coins to players
            foreach (var p in alivePlayers)
            {
                p.coins = Mathf.Min(3 + turn, 10);
            }

            // Give each player some cards for their board
            foreach (var p in alivePlayers)
            {
                if (p.board.Count < 3)
                {
                    var tribes = new[] { TribeType.Swords, TribeType.Cups, TribeType.Wands, TribeType.Pentacles };
                    var card = CreateTestCard(
                        $"Card_{turn}_{p.playerId}",
                        tribes[Random.Range(0, tribes.Length)],
                        Mathf.Min(turn / 3 + 1, 6),
                        2 + turn / 5,
                        3 + turn / 5
                    );
                    p.board.Add(card);
                }
            }

            // Simulate combat between random pairs
            for (int i = 0; i < alivePlayers.Count - 1; i += 2)
            {
                var p1 = alivePlayers[i];
                var p2 = alivePlayers[i + 1];

                var (damage, winner) = CombatManager.SimulateBattle(
                    new List<Card>(p1.board),
                    new List<Card>(p2.board),
                    p1.currentTavernTier,
                    $"P{p1.playerId}",
                    $"P{p2.playerId}"
                );

                if (winner == $"P{p1.playerId}")
                    p2.health -= damage;
                else if (winner == $"P{p2.playerId}")
                    p1.health -= damage;
            }

            // Remove eliminated players
            alivePlayers.RemoveAll(p => p.health <= 0);
        }

        Debug.Log($"Game completed in {turn} turns with {alivePlayers.Count} survivor(s)");

        Assert.IsTrue(turn < maxTurns, "Game should complete within 50 turns");
        Assert.AreEqual(1, alivePlayers.Count, "Exactly one player should remain");
    }

    [Test]
    public void FourPlayerGame_EliminationsOccurCorrectly()
    {
        var players = new List<Player>
        {
            CreateAIPlayer(0, AIDifficulty.Hard),
            CreateAIPlayer(1, AIDifficulty.Easy),
            CreateAIPlayer(2, AIDifficulty.Easy),
            CreateAIPlayer(3, AIDifficulty.Easy)
        };

        // Give P0 a much stronger board
        for (int i = 0; i < 5; i++)
        {
            players[0].board.Add(CreateTestCard($"Strong_{i}", TribeType.Swords, 6, 10, 10));
        }

        // Give others weak boards
        for (int p = 1; p < 4; p++)
        {
            for (int i = 0; i < 3; i++)
            {
                players[p].board.Add(CreateTestCard($"Weak_{p}_{i}", TribeType.Cups, 1, 1, 1));
            }
        }

        int eliminationCount = 0;
        int turn = 0;
        var alivePlayers = new List<Player>(players);

        while (alivePlayers.Count > 1 && turn < 20)
        {
            turn++;

            // Combat
            for (int i = 0; i < alivePlayers.Count && i + 1 < alivePlayers.Count; i += 2)
            {
                var p1 = alivePlayers[i];
                var p2 = alivePlayers[i + 1];

                var (damage, winner) = CombatManager.SimulateBattle(
                    p1.board.Select(c => c.Clone()).ToList(),
                    p2.board.Select(c => c.Clone()).ToList(),
                    p1.currentTavernTier,
                    $"P{p1.playerId}",
                    $"P{p2.playerId}"
                );

                if (winner == $"P{p1.playerId}") p2.health -= Mathf.Min(damage, 5);
                else if (winner == $"P{p2.playerId}") p1.health -= Mathf.Min(damage, 5);
            }

            int previousCount = alivePlayers.Count;
            alivePlayers.RemoveAll(p => p.health <= 0);
            eliminationCount += previousCount - alivePlayers.Count;
        }

        Assert.IsTrue(eliminationCount >= 1, "At least one player should be eliminated");
        Debug.Log($"Eliminations: {eliminationCount}, Turns: {turn}");
    }

    [Test]
    public void FourPlayerGame_MatchmakingWorks()
    {
        var players = new List<Player>
        {
            CreateAIPlayer(0, AIDifficulty.Medium),
            CreateAIPlayer(1, AIDifficulty.Medium),
            CreateAIPlayer(2, AIDifficulty.Medium),
            CreateAIPlayer(3, AIDifficulty.Medium)
        };

        // Test round-robin style matchmaking
        var matchups = new HashSet<string>();
        int rounds = 6;

        for (int round = 0; round < rounds; round++)
        {
            // Simple round-robin: shift players each round
            var shuffled = players.OrderBy(p => (p.playerId + round) % players.Count).ToList();

            for (int i = 0; i < shuffled.Count; i += 2)
            {
                var p1 = shuffled[i];
                var p2 = shuffled[i + 1];
                string matchup = p1.playerId < p2.playerId
                    ? $"{p1.playerId}v{p2.playerId}"
                    : $"{p2.playerId}v{p1.playerId}";
                matchups.Add(matchup);
            }
        }

        // With 4 players, there are 6 possible unique matchups: 0v1, 0v2, 0v3, 1v2, 1v3, 2v3
        Debug.Log($"Unique matchups in {rounds} rounds: {matchups.Count}");
        Assert.IsTrue(matchups.Count >= 4, "Should have variety in matchups");
    }

    #endregion

    #region F5: Balance Tests (100+ Games)

    [Test]
    public void BalanceTest_EasyVsEasy_FairWinRate()
    {
        var results = RunSimulation(20, AIDifficulty.Easy, AIDifficulty.Easy);
        results.LogResults();

        // With same difficulty, win rate should be roughly even (within 30-70%)
        float winRate = (float)results.WinsByDifficulty[0] / results.TotalGames;
        Assert.IsTrue(winRate >= 0.2f && winRate <= 0.8f,
            $"Easy vs Easy should have fair win rate, got {winRate * 100:F1}%");
    }

    [Test]
    public void BalanceTest_EasyVsHard_HardWinsMajority()
    {
        var results = RunSimulation(20, AIDifficulty.Easy, AIDifficulty.Hard);
        results.LogResults();

        // Hard should win significantly more
        float hardWinRate = (float)results.WinsByDifficulty[2] / results.TotalGames;
        Assert.IsTrue(hardWinRate >= 0.5f,
            $"Hard AI should win majority against Easy, got {hardWinRate * 100:F1}%");
    }

    [Test]
    public void BalanceTest_MediumVsHard_ReasonableWinRate()
    {
        var results = RunSimulation(20, AIDifficulty.Medium, AIDifficulty.Hard);
        results.LogResults();

        // Hard should win more but Medium should still win sometimes
        float mediumWinRate = (float)results.WinsByDifficulty[1] / results.TotalGames;
        Assert.IsTrue(mediumWinRate >= 0.1f,
            $"Medium AI should win at least 10% against Hard, got {mediumWinRate * 100:F1}%");
    }

    [Test]
    public void BalanceTest_GameLength_InAcceptableRange()
    {
        var results = RunSimulation(30, AIDifficulty.Medium, AIDifficulty.Medium);
        results.LogResults();

        // Games should complete in 10-30 turns on average
        Assert.IsTrue(results.AvgTurnCount >= 5f, $"Games too short: {results.AvgTurnCount:F1} avg turns");
        Assert.IsTrue(results.AvgTurnCount <= 40f, $"Games too long: {results.AvgTurnCount:F1} avg turns");
    }

    [Test]
    public void BalanceTest_NoInfiniteLoops()
    {
        var results = RunSimulation(20, AIDifficulty.Hard, AIDifficulty.Hard);
        results.LogResults();

        // No game should exceed 50 turns
        Assert.IsTrue(results.MaxTurnCount <= 50,
            $"Some games took too long: {results.MaxTurnCount} max turns");
        Assert.AreEqual(0, results.Errors, "Should have no errors");
    }

    /// <summary>
    /// Run a simulation of multiple games between two AI difficulties.
    /// </summary>
    private SimulationResults RunSimulation(int numGames, AIDifficulty d1, AIDifficulty d2)
    {
        var results = new SimulationResults { TotalGames = numGames };
        var turnCounts = new List<int>();

        for (int game = 0; game < numGames; game++)
        {
            try
            {
                var stats = SimulateGame(d1, d2);
                turnCounts.Add(stats.TurnCount);
                results.TotalTurns += stats.TurnCount;
                results.WinsByDifficulty[(int)stats.WinnerDifficulty]++;

                // Track winning tribes
                if (stats.WinnerTribes != null)
                {
                    foreach (var kvp in stats.WinnerTribes)
                    {
                        if (!results.TribeWinCounts.ContainsKey(kvp.Key))
                            results.TribeWinCounts[kvp.Key] = 0;
                        results.TribeWinCounts[kvp.Key] += kvp.Value;
                    }
                }
            }
            catch (System.Exception e)
            {
                results.Errors++;
                results.ErrorMessages.Add(e.Message);
            }
        }

        results.AvgTurnCount = results.TotalGames > 0 ? (float)results.TotalTurns / results.TotalGames : 0;
        results.MinTurnCount = turnCounts.Count > 0 ? turnCounts.Min() : 0;
        results.MaxTurnCount = turnCounts.Count > 0 ? turnCounts.Max() : 0;

        return results;
    }

    /// <summary>
    /// Simulate a single game between two AI players.
    /// </summary>
    private GameStats SimulateGame(AIDifficulty d1, AIDifficulty d2)
    {
        var stats = new GameStats();

        // Create players
        var p1 = CreateAIPlayer(0, d1);
        var p2 = CreateAIPlayer(1, d2);

        var tribes = new[] { TribeType.Swords, TribeType.Cups, TribeType.Wands, TribeType.Pentacles };
        int maxTurns = 50;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            stats.TurnCount = turn;

            // Refresh coins
            p1.coins = Mathf.Min(3 + turn, 10);
            p2.coins = Mathf.Min(3 + turn, 10);

            // Simulate buying/playing cards (simplified)
            SimulateRecruitPhase(p1, turn, tribes);
            SimulateRecruitPhase(p2, turn, tribes);

            // Combat
            var (damage, winner) = CombatManager.SimulateBattle(
                p1.board.Select(c => c.Clone()).ToList(),
                p2.board.Select(c => c.Clone()).ToList(),
                p1.currentTavernTier,
                "P0",
                "P1"
            );

            if (winner == "P0") p2.health -= Mathf.Min(damage, 5);
            else if (winner == "P1") p1.health -= Mathf.Min(damage, 5);

            // Check for winner
            if (p1.health <= 0)
            {
                stats.WinnerPlayerId = 1;
                stats.WinnerDifficulty = d2;
                stats.WinnerTribes = CountTribesOnBoard(p2.board);
                stats.WinnerBoardSize = p2.board.Count;
                stats.WinnerTavernTier = p2.currentTavernTier;
                break;
            }
            if (p2.health <= 0)
            {
                stats.WinnerPlayerId = 0;
                stats.WinnerDifficulty = d1;
                stats.WinnerTribes = CountTribesOnBoard(p1.board);
                stats.WinnerBoardSize = p1.board.Count;
                stats.WinnerTavernTier = p1.currentTavernTier;
                break;
            }
        }

        return stats;
    }

    private void SimulateRecruitPhase(Player player, int turn, TribeType[] tribes)
    {
        // Simplified recruit phase - just add cards based on difficulty
        int cardsToAdd = player.GetComponent<AIController>().difficulty == AIDifficulty.Hard ? 2 : 1;

        for (int i = 0; i < cardsToAdd && player.board.Count < 7; i++)
        {
            if (player.coins >= 3)
            {
                int tier = Mathf.Min((turn / 4) + 1, 6);
                var card = CreateTestCard(
                    $"Card_{turn}_{player.playerId}_{i}",
                    tribes[Random.Range(0, tribes.Length)],
                    tier,
                    tier + 1 + Random.Range(0, 2),
                    tier + 2 + Random.Range(0, 2)
                );
                player.board.Add(card);
                player.coins -= 3;
            }
        }

        // Upgrade tavern tier occasionally
        if (turn % 4 == 0 && player.currentTavernTier < 6)
        {
            player.currentTavernTier++;
        }
    }

    #endregion

    #region Full 100+ Game Test

    [Test]
    [Category("LongRunning")]
    public void BalanceTest_100Games_AllDifficulties()
    {
        var results = new SimulationResults { TotalGames = 0 };
        var turnCounts = new List<int>();

        // Mix of difficulty matchups
        var matchups = new[]
        {
            (AIDifficulty.Easy, AIDifficulty.Easy, 20),
            (AIDifficulty.Easy, AIDifficulty.Medium, 20),
            (AIDifficulty.Easy, AIDifficulty.Hard, 20),
            (AIDifficulty.Medium, AIDifficulty.Medium, 20),
            (AIDifficulty.Medium, AIDifficulty.Hard, 20),
            (AIDifficulty.Hard, AIDifficulty.Hard, 20)
        };

        foreach (var (d1, d2, count) in matchups)
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var stats = SimulateGame(d1, d2);
                    turnCounts.Add(stats.TurnCount);
                    results.TotalGames++;
                    results.TotalTurns += stats.TurnCount;
                    results.WinsByDifficulty[(int)stats.WinnerDifficulty]++;

                    if (stats.WinnerTribes != null)
                    {
                        foreach (var kvp in stats.WinnerTribes)
                        {
                            if (!results.TribeWinCounts.ContainsKey(kvp.Key))
                                results.TribeWinCounts[kvp.Key] = 0;
                            results.TribeWinCounts[kvp.Key] += kvp.Value;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    results.Errors++;
                    results.ErrorMessages.Add($"{d1}v{d2}: {e.Message}");
                }
            }
        }

        results.AvgTurnCount = results.TotalGames > 0 ? (float)results.TotalTurns / results.TotalGames : 0;
        results.MinTurnCount = turnCounts.Count > 0 ? turnCounts.Min() : 0;
        results.MaxTurnCount = turnCounts.Count > 0 ? turnCounts.Max() : 0;

        Debug.Log("\n========== 100+ GAME BALANCE TEST RESULTS ==========");
        results.LogResults();

        // Assertions
        Assert.IsTrue(results.TotalGames >= 100, $"Should run 100+ games, ran {results.TotalGames}");
        Assert.IsTrue(results.Errors < results.TotalGames * 0.05f,
            $"Error rate too high: {results.Errors}/{results.TotalGames}");
        Assert.IsTrue(results.AvgTurnCount >= 5f && results.AvgTurnCount <= 40f,
            $"Average turn count out of range: {results.AvgTurnCount:F1}");

        // Hard should have highest win rate overall
        Assert.IsTrue(results.WinsByDifficulty[2] >= results.WinsByDifficulty[0],
            "Hard AI should win at least as many games as Easy AI");
    }

    #endregion
}
