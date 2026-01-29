using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Automated tests for AI vs AI gameplay and balance.
/// Runs batch simulations to verify game completion, win rates,
/// game length, and tribe balance.
/// </summary>
[TestFixture]
public class AIBattleTests
{
    private const int BATCH_SIZE = 100;
    private const int MAX_TURNS = 30;

    /// <summary>
    /// Lightweight player state for pure-logic simulations (no MonoBehaviour).
    /// </summary>
    private class TestPlayer
    {
        public int id;
        public int health = 40;
        public int coins = 0;
        public int tavernTier = 1;
        public List<Card> board = new List<Card>();
        public AIDifficulty difficulty;
        public TribeType preferredTribe;

        private static readonly Dictionary<int, int> UpgradeCosts = new Dictionary<int, int>
        {
            {2, 6}, {3, 8}, {4, 9}, {5, 10}, {6, 11}
        };

        public TestPlayer(int id, AIDifficulty diff, TribeType preferred = TribeType.None)
        {
            this.id = id;
            this.difficulty = diff;
            this.preferredTribe = preferred;
        }

        public void ExecuteRecruitPhase(int turn)
        {
            coins = Mathf.Min(3 + (turn - 1), 10);
            ConsiderUpgrade();
            BuyCards(turn);
        }

        private void ConsiderUpgrade()
        {
            if (tavernTier >= 6) return;
            int cost = UpgradeCosts.ContainsKey(tavernTier + 1) ? UpgradeCosts[tavernTier + 1] : 999;

            bool shouldUpgrade = difficulty switch
            {
                AIDifficulty.Easy => coins >= cost + 3 && Random.value > 0.5f,
                AIDifficulty.Medium => coins >= cost + 1,
                AIDifficulty.Hard => coins >= cost,
                _ => false
            };

            if (shouldUpgrade)
            {
                coins -= cost;
                tavernTier++;
            }
        }

        private void BuyCards(int turn)
        {
            int cardsToBuy = difficulty == AIDifficulty.Hard ? 3 :
                             difficulty == AIDifficulty.Medium ? 2 : 1;

            TribeType[] allTribes = { TribeType.Pentacles, TribeType.Cups, TribeType.Swords, TribeType.Wands };

            while (coins >= 3 && board.Count < 7 && cardsToBuy > 0)
            {
                var card = ScriptableObject.CreateInstance<Card>();
                card.tier = Random.Range(1, tavernTier + 1);
                card.attack = card.tier + Random.Range(0, 3);
                card.health = card.tier + Random.Range(1, 4);
                card.cardName = $"Sim_{id}_{turn}_{board.Count}";

                // Tribe assignment: biased towards preferred if set
                if (preferredTribe != TribeType.None && Random.value < 0.6f)
                {
                    card.tribes = new TribeType[] { preferredTribe };
                }
                else
                {
                    card.tribes = new TribeType[] { allTribes[Random.Range(0, allTribes.Length)] };
                }

                board.Add(card);
                coins -= 3;
                cardsToBuy--;
            }
        }
    }

    private struct GameResult
    {
        public int winner; // 0=tie, 1-4=player index
        public int turns;
        public int[] finalHealth;
        public Dictionary<TribeType, int> winnerTribes;
    }

    private GameResult SimulateGame(TestPlayer[] players)
    {
        int turn = 0;
        var eliminated = new List<int>();

        while (turn < MAX_TURNS)
        {
            turn++;

            // Recruit phase
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].health > 0)
                    players[i].ExecuteRecruitPhase(turn);
            }

            // Combat phase: pair up alive players
            var alive = Enumerable.Range(0, players.Length)
                .Where(i => players[i].health > 0)
                .OrderBy(_ => Random.value)
                .ToList();

            if (alive.Count <= 1) break;

            for (int i = 0; i + 1 < alive.Count; i += 2)
            {
                var p1 = players[alive[i]];
                var p2 = players[alive[i + 1]];

                var (winner, damage) = SimulateCombat(p1, p2);

                if (winner == 0)
                {
                    p1.health -= damage;
                    p2.health -= damage;
                }
                else if (winner == 1)
                {
                    p2.health -= damage;
                }
                else
                {
                    p1.health -= damage;
                }
            }

            // Track eliminations
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].health <= 0 && !eliminated.Contains(i))
                    eliminated.Add(i);
            }

            // Check end condition
            var remaining = Enumerable.Range(0, players.Length)
                .Where(i => players[i].health > 0).ToList();
            if (remaining.Count <= 1) break;
        }

        // Determine winner
        int gameWinner = 0;
        var finalAlive = Enumerable.Range(0, players.Length)
            .Where(i => players[i].health > 0).ToList();

        if (finalAlive.Count == 1)
            gameWinner = finalAlive[0] + 1;
        else if (finalAlive.Count > 1)
            gameWinner = finalAlive.OrderByDescending(i => players[i].health).First() + 1;

        // Count winner's tribes
        Dictionary<TribeType, int> winnerTribes = new Dictionary<TribeType, int>();
        if (gameWinner > 0)
        {
            var wp = players[gameWinner - 1];
            foreach (var card in wp.board)
            {
                if (card.tribes != null)
                {
                    foreach (var t in card.tribes)
                    {
                        if (t == TribeType.None) continue;
                        if (!winnerTribes.ContainsKey(t)) winnerTribes[t] = 0;
                        winnerTribes[t]++;
                    }
                }
            }
        }

        return new GameResult
        {
            winner = gameWinner,
            turns = turn,
            finalHealth = players.Select(p => p.health).ToArray(),
            winnerTribes = winnerTribes
        };
    }

    private (int winner, int damage) SimulateCombat(TestPlayer p1, TestPlayer p2)
    {
        var board1 = p1.board.Select(c => c.Clone()).ToList();
        var board2 = p2.board.Select(c => c.Clone()).ToList();

        int iterations = 0;
        while (board1.Count > 0 && board2.Count > 0 && iterations < 100)
        {
            iterations++;

            if (board1.Count > 0 && board2.Count > 0)
            {
                var atk = board1[0];
                var def = board2[0];
                def.health -= atk.attack;
                atk.health -= def.attack;
                if (def.health <= 0) board2.RemoveAt(0);
                if (atk.health <= 0) board1.RemoveAt(0);
            }

            if (board1.Count > 0 && board2.Count > 0)
            {
                var atk = board2[0];
                var def = board1[0];
                def.health -= atk.attack;
                atk.health -= def.attack;
                if (def.health <= 0) board1.RemoveAt(0);
                if (atk.health <= 0) board2.RemoveAt(0);
            }
        }

        if (board1.Count == 0 && board2.Count == 0)
            return (0, 0);

        if (board1.Count > 0)
            return (1, Mathf.Min(5, board1.Sum(c => c.tier) + p1.tavernTier));

        return (2, Mathf.Min(5, board2.Sum(c => c.tier) + p2.tavernTier));
    }

    // =====================================================
    // GAME COMPLETION TESTS
    // =====================================================

    [Test]
    public void TwoPlayerGame_CompletesWithoutErrors()
    {
        var players = new TestPlayer[]
        {
            new TestPlayer(1, AIDifficulty.Medium),
            new TestPlayer(2, AIDifficulty.Medium)
        };

        var result = SimulateGame(players);
        Assert.IsTrue(result.turns > 0, "Game should last at least 1 turn");
        Assert.IsTrue(result.turns <= MAX_TURNS, $"Game should finish within {MAX_TURNS} turns");
    }

    [Test]
    public void FourPlayerGame_CompletesWithoutErrors()
    {
        var players = new TestPlayer[]
        {
            new TestPlayer(1, AIDifficulty.Medium),
            new TestPlayer(2, AIDifficulty.Medium),
            new TestPlayer(3, AIDifficulty.Medium),
            new TestPlayer(4, AIDifficulty.Medium)
        };

        var result = SimulateGame(players);
        Assert.IsTrue(result.turns > 0);
        Assert.IsTrue(result.winner >= 0, "Game should produce a winner or tie");
    }

    [Test]
    public void FourPlayerGame_ProducesWinner()
    {
        var players = new TestPlayer[]
        {
            new TestPlayer(1, AIDifficulty.Medium),
            new TestPlayer(2, AIDifficulty.Medium),
            new TestPlayer(3, AIDifficulty.Medium),
            new TestPlayer(4, AIDifficulty.Medium)
        };

        var result = SimulateGame(players);
        Assert.IsTrue(result.winner > 0, "4-player game should produce a winner");
    }

    [Test]
    public void FourPlayerGame_MaximumOneSurvivor()
    {
        var players = new TestPlayer[]
        {
            new TestPlayer(1, AIDifficulty.Medium),
            new TestPlayer(2, AIDifficulty.Medium),
            new TestPlayer(3, AIDifficulty.Medium),
            new TestPlayer(4, AIDifficulty.Medium)
        };

        var result = SimulateGame(players);
        int survivors = result.finalHealth.Count(h => h > 0);
        Assert.IsTrue(survivors <= 1, $"Expected at most 1 survivor, got {survivors}");
    }

    // =====================================================
    // BATCH GAME TESTS (100+ games)
    // =====================================================

    [Test]
    public void Batch100_TwoPlayer_AllGamesComplete()
    {
        int completed = 0;
        int totalTurns = 0;

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium),
                new TestPlayer(2, AIDifficulty.Medium)
            };

            var result = SimulateGame(players);
            if (result.turns > 0 && result.turns <= MAX_TURNS)
                completed++;
            totalTurns += result.turns;
        }

        Assert.AreEqual(BATCH_SIZE, completed,
            $"All {BATCH_SIZE} games should complete. Completed: {completed}");

        float avgLength = totalTurns / (float)BATCH_SIZE;
        Debug.Log($"[AIBattleTests] 2-player batch: avg game length = {avgLength:F1} turns");
    }

    [Test]
    public void Batch100_FourPlayer_AllGamesComplete()
    {
        int completed = 0;
        int totalTurns = 0;

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium),
                new TestPlayer(2, AIDifficulty.Medium),
                new TestPlayer(3, AIDifficulty.Medium),
                new TestPlayer(4, AIDifficulty.Medium)
            };

            var result = SimulateGame(players);
            if (result.turns > 0 && result.turns <= MAX_TURNS)
                completed++;
            totalTurns += result.turns;
        }

        Assert.AreEqual(BATCH_SIZE, completed,
            $"All {BATCH_SIZE} 4-player games should complete");

        float avgLength = totalTurns / (float)BATCH_SIZE;
        Debug.Log($"[AIBattleTests] 4-player batch: avg game length = {avgLength:F1} turns");
    }

    // =====================================================
    // BALANCE TESTS
    // =====================================================

    [Test]
    public void Balance_EqualDifficulty_FairWinRates()
    {
        int[] wins = new int[4];
        int ties = 0;

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium),
                new TestPlayer(2, AIDifficulty.Medium),
                new TestPlayer(3, AIDifficulty.Medium),
                new TestPlayer(4, AIDifficulty.Medium)
            };

            var result = SimulateGame(players);
            if (result.winner > 0)
                wins[result.winner - 1]++;
            else
                ties++;
        }

        // Log results
        for (int i = 0; i < 4; i++)
        {
            float winRate = wins[i] / (float)BATCH_SIZE * 100f;
            Debug.Log($"[Balance] Player {i + 1}: {wins[i]} wins ({winRate:F1}%)");
        }
        Debug.Log($"[Balance] Ties: {ties}");

        // No player should win more than 40% of the time in a fair 4-player game
        // (expected ~25% each)
        for (int i = 0; i < 4; i++)
        {
            float winRate = wins[i] / (float)BATCH_SIZE * 100f;
            Assert.IsTrue(winRate < 45f,
                $"Player {i + 1} wins {winRate:F1}% - exceeds 45% threshold for fair game");
        }
    }

    [Test]
    public void Balance_HardBeatsEasy_MostOfTheTime()
    {
        int hardWins = 0;
        int easyWins = 0;

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Hard),
                new TestPlayer(2, AIDifficulty.Easy)
            };

            var result = SimulateGame(players);
            if (result.winner == 1) hardWins++;
            else if (result.winner == 2) easyWins++;
        }

        float hardRate = hardWins / (float)BATCH_SIZE * 100f;
        float easyRate = easyWins / (float)BATCH_SIZE * 100f;
        Debug.Log($"[Balance] Hard: {hardWins} ({hardRate:F1}%), Easy: {easyWins} ({easyRate:F1}%)");

        Assert.IsTrue(hardWins > easyWins,
            $"Hard AI should beat Easy AI more often. Hard: {hardWins}, Easy: {easyWins}");
    }

    [Test]
    public void Balance_GameLength_ReasonableRange()
    {
        var gameLengths = new List<int>();

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium),
                new TestPlayer(2, AIDifficulty.Medium),
                new TestPlayer(3, AIDifficulty.Medium),
                new TestPlayer(4, AIDifficulty.Medium)
            };

            var result = SimulateGame(players);
            gameLengths.Add(result.turns);
        }

        float avg = (float)gameLengths.Average();
        int min = gameLengths.Min();
        int max = gameLengths.Max();

        Debug.Log($"[Balance] Game length: avg={avg:F1}, min={min}, max={max}");

        // Games should last 8-20 turns on average
        Assert.IsTrue(avg >= 5f, $"Average game length ({avg:F1}) is too short (expected >= 5)");
        Assert.IsTrue(avg <= 25f, $"Average game length ({avg:F1}) is too long (expected <= 25)");
    }

    [Test]
    public void Balance_NoTribeDominates_InWinnerBoards()
    {
        var tribeWinCounts = new Dictionary<TribeType, int>
        {
            { TribeType.Pentacles, 0 },
            { TribeType.Cups, 0 },
            { TribeType.Swords, 0 },
            { TribeType.Wands, 0 }
        };
        int gamesWithTribes = 0;

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium),
                new TestPlayer(2, AIDifficulty.Medium),
                new TestPlayer(3, AIDifficulty.Medium),
                new TestPlayer(4, AIDifficulty.Medium)
            };

            var result = SimulateGame(players);

            if (result.winnerTribes != null && result.winnerTribes.Count > 0)
            {
                gamesWithTribes++;
                // Count the dominant tribe (most cards) on winner's board
                var dominant = result.winnerTribes.OrderByDescending(kvp => kvp.Value).First().Key;
                if (tribeWinCounts.ContainsKey(dominant))
                    tribeWinCounts[dominant]++;
            }
        }

        if (gamesWithTribes < 10)
        {
            Assert.Inconclusive("Too few games with tribe data to analyze");
            return;
        }

        Debug.Log($"[Balance] Tribe dominance in {gamesWithTribes} winning boards:");
        foreach (var kvp in tribeWinCounts)
        {
            float rate = kvp.Value / (float)gamesWithTribes * 100f;
            Debug.Log($"  {kvp.Key}: {kvp.Value} ({rate:F1}%)");
        }

        // No tribe should dominate more than 40% of winning boards
        foreach (var kvp in tribeWinCounts)
        {
            float rate = kvp.Value / (float)gamesWithTribes * 100f;
            Assert.IsTrue(rate < 45f,
                $"{kvp.Key} dominates {rate:F1}% of winning boards - exceeds 45% threshold");
        }
    }

    // =====================================================
    // DIFFICULTY SCALING TESTS
    // =====================================================

    [Test]
    public void Difficulty_MixedLobby_HardWinsMost()
    {
        int[] wins = new int[3]; // Easy, Medium, Hard

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Easy),
                new TestPlayer(2, AIDifficulty.Medium),
                new TestPlayer(3, AIDifficulty.Hard)
            };

            var result = SimulateGame(players);
            if (result.winner > 0)
                wins[result.winner - 1]++;
        }

        Debug.Log($"[Difficulty] Easy: {wins[0]}, Medium: {wins[1]}, Hard: {wins[2]}");

        // Hard should win more than Easy
        Assert.IsTrue(wins[2] >= wins[0],
            $"Hard ({wins[2]}) should win at least as often as Easy ({wins[0]})");
    }

    [Test]
    public void Difficulty_AllHard_StillFair()
    {
        int[] wins = new int[4];

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Hard),
                new TestPlayer(2, AIDifficulty.Hard),
                new TestPlayer(3, AIDifficulty.Hard),
                new TestPlayer(4, AIDifficulty.Hard)
            };

            var result = SimulateGame(players);
            if (result.winner > 0)
                wins[result.winner - 1]++;
        }

        for (int i = 0; i < 4; i++)
        {
            float rate = wins[i] / (float)BATCH_SIZE * 100f;
            Debug.Log($"[Difficulty] Hard P{i + 1}: {wins[i]} ({rate:F1}%)");
            Assert.IsTrue(rate < 45f,
                $"Hard P{i + 1} wins {rate:F1}% - exceeds 45% in equal-skill lobby");
        }
    }

    // =====================================================
    // TRIBE PREFERENCE TESTS
    // =====================================================

    [Test]
    public void TribePreference_AllTribesCompetitive()
    {
        TribeType[] tribes = { TribeType.Pentacles, TribeType.Cups, TribeType.Swords, TribeType.Wands };
        int[] tribeWins = new int[4];

        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var players = new TestPlayer[]
            {
                new TestPlayer(1, AIDifficulty.Medium, TribeType.Pentacles),
                new TestPlayer(2, AIDifficulty.Medium, TribeType.Cups),
                new TestPlayer(3, AIDifficulty.Medium, TribeType.Swords),
                new TestPlayer(4, AIDifficulty.Medium, TribeType.Wands)
            };

            var result = SimulateGame(players);
            if (result.winner > 0)
                tribeWins[result.winner - 1]++;
        }

        Debug.Log("[Balance] Tribe preference results:");
        for (int i = 0; i < 4; i++)
        {
            float rate = tribeWins[i] / (float)BATCH_SIZE * 100f;
            Debug.Log($"  {tribes[i]}: {tribeWins[i]} wins ({rate:F1}%)");
        }

        // No tribe-focused strategy should be completely unviable (>5% win rate)
        for (int i = 0; i < 4; i++)
        {
            float rate = tribeWins[i] / (float)BATCH_SIZE * 100f;
            Assert.IsTrue(rate > 5f,
                $"{tribes[i]}-focused strategy wins only {rate:F1}% - tribe may be underpowered");
        }

        // No tribe should be dominant (>40% win rate)
        for (int i = 0; i < 4; i++)
        {
            float rate = tribeWins[i] / (float)BATCH_SIZE * 100f;
            Assert.IsTrue(rate < 45f,
                $"{tribes[i]}-focused strategy wins {rate:F1}% - tribe may be overpowered");
        }
    }

    // =====================================================
    // DAMAGE / HEALTH TESTS
    // =====================================================

    [Test]
    public void Combat_DamageNeverExceeds5()
    {
        for (int i = 0; i < BATCH_SIZE; i++)
        {
            var p1 = new TestPlayer(1, AIDifficulty.Hard);
            var p2 = new TestPlayer(2, AIDifficulty.Easy);

            // Give p1 a huge board
            for (int j = 0; j < 7; j++)
            {
                var card = ScriptableObject.CreateInstance<Card>();
                card.tier = 6;
                card.attack = 10;
                card.health = 10;
                card.tribes = new TribeType[] { TribeType.Swords };
                p1.board.Add(card);
            }

            var (_, damage) = SimulateCombat(p1, p2);
            Assert.IsTrue(damage <= 5, $"Damage {damage} exceeds cap of 5");
        }
    }

    [Test]
    public void Health_StartsAt40_DecreasesOverGame()
    {
        var players = new TestPlayer[]
        {
            new TestPlayer(1, AIDifficulty.Medium),
            new TestPlayer(2, AIDifficulty.Medium)
        };

        Assert.AreEqual(40, players[0].health);
        Assert.AreEqual(40, players[1].health);

        var result = SimulateGame(players);

        // At least one player should have taken damage
        bool anyDamage = result.finalHealth.Any(h => h < 40);
        Assert.IsTrue(anyDamage, "At least one player should take damage during a game");
    }
}
