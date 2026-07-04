using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Owns per-game session state extracted from GameManager: player healths,
/// elimination order, per-turn ready tracking, current phase/turn and starting
/// health. Plain C# class — constructed once in GameManager.Awake().
/// </summary>
public class GameSessionState
{
    private List<int> playerHealths;
    private readonly HashSet<int> playersReadyForCombat = new HashSet<int>();
    private readonly List<int> eliminationOrder = new List<int>();

    public GameManager.GamePhase CurrentPhase { get; set; } = GameManager.GamePhase.Recruit;
    public int TurnNumber { get; set; } = 1;
    public int StartingHealth { get; set; } = 40;

    /// <summary>
    /// Read-only view of the raw health list, for the facade's remaining
    /// aliveness scans (GameLoop/RecruitPhase LINQ queries).
    /// </summary>
    public IReadOnlyList<int> PlayerHealths => playerHealths;

    public int ReadyCount => playersReadyForCombat.Count;

    /// <summary>
    /// Moved verbatim from GameManager.GetAlivePlayerCount.
    /// </summary>
    public int GetAlivePlayerCount(int playerCount)
    {
        int count = 0;
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0) count++;
        }
        return count;
    }

    /// <summary>
    /// Moved verbatim from GameManager.AllAlivePlayersReady.
    /// </summary>
    public bool AllAlivePlayersReady(int playerCount)
    {
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0 && !playersReadyForCombat.Contains(i))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Build final standings for game-over: winner first, then reverse elimination
    /// order (last eliminated = 2nd place), then any remaining players.
    /// Moved verbatim from GameManager.TriggerGameOver's standings-building block.
    /// </summary>
    public List<int> BuildStandings(int winnerIndex, int playerCount)
    {
        List<int> standings = new List<int>();

        if (winnerIndex >= 0)
            standings.Add(winnerIndex);

        // Add eliminated players in reverse order (last eliminated is highest placement)
        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            if (!standings.Contains(eliminationOrder[i]))
                standings.Add(eliminationOrder[i]);
        }

        // Add any remaining players not yet in standings
        for (int i = 0; i < playerCount; i++)
        {
            if (!standings.Contains(i))
                standings.Add(i);
        }

        return standings;
    }

    /// <summary>
    /// Track newly eliminated players. Moved verbatim from GameManager.GameLoop.
    /// </summary>
    public void TrackNewEliminations(int playerCount)
    {
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] <= 0 && !eliminationOrder.Contains(i))
            {
                eliminationOrder.Add(i);
                Debug.Log($"[GameManager] Player {i + 1} eliminated! (Elimination #{eliminationOrder.Count})");
            }
        }
    }

    /// <summary>
    /// Moved verbatim from GameManager.EliminateDisconnectedPlayer.
    /// </summary>
    public void RecordElimination(int playerIndex)
    {
        if (!eliminationOrder.Contains(playerIndex))
            eliminationOrder.Add(playerIndex);
    }

    public void MarkReady(int playerIndex) => playersReadyForCombat.Add(playerIndex);
    public bool IsReady(int playerIndex) => playersReadyForCombat.Contains(playerIndex);
    public void ClearReady() => playersReadyForCombat.Clear();

    /// <summary>
    /// Bounds-checked health accessor. Moved verbatim from GameManager.GetPlayerHealth.
    /// </summary>
    public int GetHealth(int index)
    {
        if (index >= 0 && index < playerHealths.Count)
            return playerHealths[index];
        return 0;
    }

    public void SetHealth(int index, int value)
    {
        playerHealths[index] = value;
    }

    /// <summary>
    /// Apply damage to a player's health and return the resulting value.
    /// </summary>
    public int ApplyDamage(int playerIndex, int damage)
    {
        playerHealths[playerIndex] -= damage;
        return playerHealths[playerIndex];
    }

    /// <summary>
    /// (Re)initialize the health list for a fresh game.
    /// Moved verbatim from GameManager.InitializePlayers.
    /// </summary>
    public void InitializeHealths(int startingHealth, int playerCount)
    {
        playerHealths = new List<int>(Enumerable.Repeat(startingHealth, playerCount).ToArray());
    }

    /// <summary>
    /// Rebuild playerHealths from live Player objects (host migration recovery).
    /// Moved verbatim from GameManager.AssumeHostDuties.
    /// </summary>
    public void RebuildFromPlayers(List<Player> players, int playerCount)
    {
        if (playerHealths == null)
            playerHealths = new List<int>(new int[playerCount]);

        for (int i = 0; i < playerCount && i < players.Count; i++)
            playerHealths[i] = players[i].Health;
    }
}
