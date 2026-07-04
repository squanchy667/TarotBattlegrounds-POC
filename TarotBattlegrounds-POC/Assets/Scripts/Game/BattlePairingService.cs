using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Pairwise-matchmaking history and pairing generation for the recruit-to-combat
/// loop. Extracted from GameManager. Plain C# class — constructed once in
/// GameManager.Awake().
/// </summary>
public class BattlePairingService
{
    // Matchmaking history to avoid consecutive same opponents
    private Dictionary<int, HashSet<int>> recentOpponents = new Dictionary<int, HashSet<int>>();

    /// <summary>
    /// Generate match pairings avoiding recent opponents when possible.
    /// Moved verbatim from GameManager.GeneratePairwiseBattles.
    /// </summary>
    public List<(int, int)> GeneratePairwiseBattles(List<int> activePlayers)
    {
        List<(int, int)> battles = new List<(int, int)>();
        var available = new List<int>(activePlayers);

        // Shuffle for randomness base
        available = available.OrderBy(x => Random.value).ToList();

        // Handle odd number of players - give bye to player with fewest recent fights
        if (available.Count % 2 != 0)
        {
            int byePlayer = available.OrderBy(p => recentOpponents.ContainsKey(p) ? recentOpponents[p].Count : 0).First();
            available.Remove(byePlayer);
            Debug.Log($"[GameManager] Player {byePlayer + 1} gets a bye this round (fewest recent matches)");
        }

        // Pair up players, preferring opponents not recently fought
        while (available.Count >= 2)
        {
            int p1 = available[0];
            available.RemoveAt(0);

            int bestOpponent = -1;
            int bestScore = int.MaxValue;

            foreach (int p2 in available)
            {
                int score = (recentOpponents.ContainsKey(p1) && recentOpponents[p1].Contains(p2)) ? 10 : 0;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestOpponent = p2;
                }
            }

            if (bestOpponent >= 0)
            {
                available.Remove(bestOpponent);
                battles.Add((p1, bestOpponent));
            }
        }

        return battles;
    }

    /// <summary>
    /// Clear opponent history when it gets too large.
    /// Moved verbatim from GameManager.ClearOldOpponentHistory.
    /// </summary>
    public void ClearOldOpponentHistory()
    {
        foreach (var kvp in recentOpponents)
        {
            if (kvp.Value.Count > 2)
                kvp.Value.Clear();
        }
    }

    /// <summary>
    /// Track recent opponents for matchmaking.
    /// Moved verbatim from GameManager.GameLoop's per-battle block.
    /// </summary>
    public void RecordOpponents(int p1, int p2)
    {
        recentOpponents[p1].Add(p2);
        recentOpponents[p2].Add(p1);
    }

    /// <summary>
    /// (Re)initialize the matchmaking history.
    /// Moved verbatim from GameManager.InitializePlayers / GameManager.AssumeHostDuties.
    /// </summary>
    public void Reset(int playerCount)
    {
        recentOpponents.Clear();
        for (int i = 0; i < playerCount; i++)
            recentOpponents[i] = new HashSet<int>();
    }

    /// <summary>
    /// Initialize the matchmaking history only if it is empty, preserving
    /// any existing history (original AssumeHostDuties null-guard semantics:
    /// a promoted host keeps pairing history it already accumulated).
    /// </summary>
    public void EnsureInitialized(int playerCount)
    {
        if (recentOpponents.Count == 0)
            Reset(playerCount);
    }
}
