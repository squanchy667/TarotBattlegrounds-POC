using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// T601-T605: Extends LobbyManager for 8-player game support.
/// Handles ghost opponents, round-robin pairing for odd players,
/// dynamic elimination brackets, and health scaling.
/// </summary>
public class EightPlayerManager : MonoBehaviour
{
    public static EightPlayerManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Starting health scales with player count")]
    public bool scaleHealthWithPlayerCount = true;

    // Ghost opponent tracking
    private Dictionary<int, int> ghostOpponentLastUsed = new Dictionary<int, int>();

    // Pairing history for round-robin
    private Dictionary<int, HashSet<int>> pairingHistory = new Dictionary<int, HashSet<int>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// T605: Calculate starting health based on player count.
    /// More players = more health to allow for longer games.
    /// </summary>
    public int GetStartingHealth(int playerCount)
    {
        if (!scaleHealthWithPlayerCount) return 40;

        // Base: 40 for 4 players, scale up for more
        return playerCount switch
        {
            2 => 30,
            3 => 35,
            4 => 40,
            5 => 42,
            6 => 45,
            7 => 47,
            8 => 50,
            _ => 40
        };
    }

    /// <summary>
    /// T603: Generate round-robin pairings for any player count.
    /// Returns list of (attacker, defender) pairs. If odd count, one player fights a ghost.
    /// </summary>
    public List<(int, int)> GeneratePairings(List<int> activePlayers, int currentTurn)
    {
        var pairings = new List<(int, int)>();
        var available = new List<int>(activePlayers);

        // Shuffle based on turn for variety
        var rng = new System.Random(currentTurn * 31337);
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        // Initialize pairing history and prune eliminated players
        var activeSet = new HashSet<int>(activePlayers);
        var staleKeys = pairingHistory.Keys.Where(k => !activeSet.Contains(k)).ToList();
        foreach (var key in staleKeys)
            pairingHistory.Remove(key);

        foreach (var p in activePlayers)
        {
            if (!pairingHistory.ContainsKey(p))
                pairingHistory[p] = new HashSet<int>();
        }

        // Try to avoid recent opponents
        var paired = new HashSet<int>();
        var sortedByLeastPaired = available.OrderBy(p => pairingHistory[p].Count).ToList();

        foreach (var p1 in sortedByLeastPaired)
        {
            if (paired.Contains(p1)) continue;

            // Find best opponent (least recently fought)
            int bestOpponent = -1;
            int bestScore = int.MaxValue;

            foreach (var p2 in available)
            {
                if (p2 == p1 || paired.Contains(p2)) continue;
                int score = pairingHistory[p1].Contains(p2) ? 1 : 0;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestOpponent = p2;
                }
            }

            if (bestOpponent >= 0)
            {
                pairings.Add((p1, bestOpponent));
                paired.Add(p1);
                paired.Add(bestOpponent);
                pairingHistory[p1].Add(bestOpponent);
                pairingHistory[bestOpponent].Add(p1);
            }
        }

        // T602: Handle odd player — ghost opponent
        var unpaired = available.Where(p => !paired.Contains(p)).ToList();
        if (unpaired.Count == 1)
        {
            int ghostPlayer = unpaired[0];
            int ghostOpponent = GetGhostOpponent(ghostPlayer, activePlayers, currentTurn);
            pairings.Add((ghostPlayer, ghostOpponent));
            Debug.Log($"[8Player] Ghost pairing: Player {ghostPlayer} fights ghost of Player {ghostOpponent}");
        }

        return pairings;
    }

    /// <summary>
    /// T602: Select a ghost opponent (a copy of a recently eliminated or other player's board).
    /// Avoids using the same ghost repeatedly.
    /// </summary>
    private int GetGhostOpponent(int playerIndex, List<int> activePlayers, int turn)
    {
        // Pick the player who was least recently used as ghost
        int bestGhost = -1;
        int bestTurn = int.MaxValue;

        foreach (var p in activePlayers)
        {
            if (p == playerIndex) continue;
            int lastUsed = ghostOpponentLastUsed.ContainsKey(p) ? ghostOpponentLastUsed[p] : -1;
            if (lastUsed < bestTurn)
            {
                bestTurn = lastUsed;
                bestGhost = p;
            }
        }

        if (bestGhost >= 0)
            ghostOpponentLastUsed[bestGhost] = turn;

        return bestGhost >= 0 ? bestGhost : activePlayers[0];
    }

    /// <summary>
    /// T604: Calculate damage to loser based on turn number and winner's board.
    /// Scales with game progression for dynamic elimination.
    /// </summary>
    public int CalculateCombatDamage(int turnNumber, int winnerBoardStrength, int playerCount)
    {
        // Base damage: turn number (escalates over time)
        int baseDamage = Mathf.Max(1, turnNumber);

        // Board bonus: surviving minions contribute damage
        int boardBonus = Mathf.Max(0, winnerBoardStrength / 3);

        // Scale factor: more players = slightly less damage per round
        float scaleFactor = playerCount switch
        {
            <= 4 => 1.0f,
            5 => 0.95f,
            6 => 0.9f,
            7 => 0.85f,
            >= 8 => 0.8f,
        };

        return Mathf.Max(1, Mathf.RoundToInt((baseDamage + boardBonus) * scaleFactor));
    }

    /// <summary>
    /// T604: Get elimination bracket string (8→4→2→winner).
    /// </summary>
    public string GetBracketDescription(int totalPlayers, int remainingPlayers)
    {
        if (remainingPlayers <= 1) return "Winner!";
        if (remainingPlayers <= 2) return "Final 2";
        if (remainingPlayers <= 4 && totalPlayers >= 6) return "Top 4";
        if (remainingPlayers <= totalPlayers / 2) return $"Top {remainingPlayers}";
        return $"{remainingPlayers} remaining";
    }

    /// <summary>
    /// T616: Get economy adjustments for 8-player games.
    /// </summary>
    public int GetStartingGold(int playerCount)
    {
        return playerCount switch
        {
            <= 4 => 3,
            5 or 6 => 3,
            7 or 8 => 4, // Extra starting gold for longer games
            _ => 3
        };
    }

    /// <summary>
    /// T615: Get shop pool size multiplier based on player count.
    /// </summary>
    public float GetShopPoolMultiplier(int playerCount)
    {
        return playerCount switch
        {
            <= 4 => 1.0f,
            5 or 6 => 1.5f,
            7 or 8 => 2.0f,
            _ => 1.0f
        };
    }

    public void ResetHistory()
    {
        pairingHistory.Clear();
        ghostOpponentLastUsed.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
