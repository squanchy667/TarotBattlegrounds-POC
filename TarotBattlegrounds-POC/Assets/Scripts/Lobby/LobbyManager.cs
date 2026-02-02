using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages a 4-player lobby with 1 human and 3 AI opponents.
/// Handles game flow, matchmaking, eliminations, and placements.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // ====== EVENTS ======
    public static event Action<int> OnTurnStarted; // turnNumber
    public static event Action<GamePhase> OnPhaseChanged; // phase
    public static event Action<int, int> OnPlayerEliminated; // playerId, placement
    public static event Action<int> OnGameEnded; // winnerId
    public static event Action<int, int, int> OnMatchResult; // player1Id, player2Id, winnerId

    public enum GamePhase
    {
        Lobby,      // Pre-game setup
        Recruit,    // Shop phase
        Combat,     // Auto-battle phase
        Results     // Post-game
    }

    [Header("Configuration")]
    [Tooltip("Starting health for all players")]
    public int startingHealth = 40;

    [Tooltip("Duration of recruit phase in seconds")]
    public float recruitPhaseDuration = 35f;

    [Tooltip("Delay between combat animations")]
    public float combatDelay = 2f;

    [Header("AI Settings")]
    public AIDifficulty ai1Difficulty = AIDifficulty.Easy;
    public AIDifficulty ai2Difficulty = AIDifficulty.Medium;
    public AIDifficulty ai3Difficulty = AIDifficulty.Hard;

    [Header("Player References")]
    [Tooltip("Assign 4 Player components (index 0 = human)")]
    public List<Player> players = new List<Player>();

    [Header("Runtime State")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Lobby;
    [SerializeField] private int currentTurn = 0;
    [SerializeField] private List<int> eliminationOrder = new List<int>();
    [SerializeField] private int humanPlayerId = 1;

    // Internal state
    private Dictionary<int, int> playerHealths = new Dictionary<int, int>();
    private Dictionary<int, AIController> aiControllers = new Dictionary<int, AIController>();
    private List<int> activePlayers = new List<int>();
    private bool gameInProgress = false;

    // Round-robin matchmaking history
    private Dictionary<int, HashSet<int>> recentOpponents = new Dictionary<int, HashSet<int>>();

    public GamePhase CurrentPhase => currentPhase;
    public int CurrentTurn => currentTurn;
    public int HumanPlayerId => humanPlayerId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-initialize if players assigned
        if (players.Count == 4)
        {
            InitializeLobby();
        }
    }

    /// <summary>
    /// Initialize the lobby with 4 players.
    /// </summary>
    public void InitializeLobby()
    {
        if (players.Count != 4)
        {
            Debug.LogError("[LobbyManager] Exactly 4 players required!");
            return;
        }

        Debug.Log("[LobbyManager] Initializing 4-player lobby...");

        // Reset state
        playerHealths.Clear();
        aiControllers.Clear();
        activePlayers.Clear();
        eliminationOrder.Clear();
        recentOpponents.Clear();

        // Setup each player
        for (int i = 0; i < 4; i++)
        {
            int playerId = i + 1;
            players[i].playerId = playerId;
            players[i].Health = startingHealth;
            playerHealths[playerId] = startingHealth;
            activePlayers.Add(playerId);
            recentOpponents[playerId] = new HashSet<int>();

            // Setup AI controllers for non-human players
            if (playerId != humanPlayerId)
            {
                AIController ai = players[i].GetComponent<AIController>();
                if (ai == null)
                {
                    ai = players[i].gameObject.AddComponent<AIController>();
                }

                // Assign difficulty
                switch (playerId)
                {
                    case 2: ai.difficulty = ai1Difficulty; break;
                    case 3: ai.difficulty = ai2Difficulty; break;
                    case 4: ai.difficulty = ai3Difficulty; break;
                }

                aiControllers[playerId] = ai;
                Debug.Log($"[LobbyManager] Player {playerId} is AI ({ai.difficulty})");
            }
            else
            {
                Debug.Log($"[LobbyManager] Player {playerId} is HUMAN");
            }
        }

        currentPhase = GamePhase.Lobby;
        Debug.Log("[LobbyManager] Lobby initialized. Call StartGame() to begin.");
    }

    /// <summary>
    /// Start the game loop.
    /// </summary>
    [ContextMenu("Start Game")]
    public void StartGame()
    {
        if (gameInProgress)
        {
            Debug.LogWarning("[LobbyManager] Game already in progress!");
            return;
        }

        if (players.Count != 4)
        {
            Debug.LogError("[LobbyManager] Cannot start - need exactly 4 players!");
            return;
        }

        gameInProgress = true;
        currentTurn = 0;
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// Main game loop coroutine.
    /// </summary>
    private IEnumerator GameLoop()
    {
        Debug.Log("[LobbyManager] === GAME STARTED ===");

        while (gameInProgress && activePlayers.Count > 1)
        {
            currentTurn++;
            OnTurnStarted?.Invoke(currentTurn);
            Debug.Log($"[LobbyManager] === TURN {currentTurn} ===");

            // Recruit Phase
            yield return StartCoroutine(RecruitPhase());

            // Combat Phase
            yield return StartCoroutine(CombatPhase());

            // Check for eliminations
            CheckEliminations();

            // Check for game end
            if (activePlayers.Count <= 1)
            {
                break;
            }

            // Brief pause between turns
            yield return new WaitForSeconds(1f);
        }

        // Game Over
        EndGame();
    }

    /// <summary>
    /// Handle the recruit phase for all players.
    /// </summary>
    private IEnumerator RecruitPhase()
    {
        currentPhase = GamePhase.Recruit;
        OnPhaseChanged?.Invoke(currentPhase);
        Debug.Log($"[LobbyManager] Recruit Phase - Turn {currentTurn}");

        // Refresh shops for all active players
        foreach (int playerId in activePlayers)
        {
            Player player = GetPlayer(playerId);
            if (player != null)
            {
                player.RefreshShop(currentTurn);
            }
        }

        // AI players make their decisions immediately
        foreach (int playerId in activePlayers)
        {
            if (playerId != humanPlayerId && aiControllers.ContainsKey(playerId))
            {
                aiControllers[playerId].ExecuteTurn();
            }
        }

        // Wait for recruit phase duration (human player has this time)
        float timer = recruitPhaseDuration;
        while (timer > 0)
        {
            // Update UI timer if available
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.UpdateTimer(timer);
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        // End recruit phase for all players
        foreach (int playerId in activePlayers)
        {
            Player player = GetPlayer(playerId);
            if (player != null)
            {
                player.EndRecruitPhase();
            }
        }
    }

    /// <summary>
    /// Handle the combat phase with round-robin matchmaking.
    /// </summary>
    private IEnumerator CombatPhase()
    {
        currentPhase = GamePhase.Combat;
        OnPhaseChanged?.Invoke(currentPhase);
        Debug.Log($"[LobbyManager] Combat Phase - Turn {currentTurn}");

        // Generate matches using round-robin
        var matches = GenerateMatches();

        foreach (var (p1Id, p2Id) in matches)
        {
            if (p1Id < 0 || p2Id < 0) continue;

            Player player1 = GetPlayer(p1Id);
            Player player2 = GetPlayer(p2Id);

            if (player1 == null || player2 == null) continue;

            Debug.Log($"[LobbyManager] Match: Player {p1Id} vs Player {p2Id}");

            // Run combat
            var (damage, winner) = CombatManager.SimulateBattle(
                player1.board,
                player2.board,
                player1.currentTavernTier,
                player2.currentTavernTier,
                $"Player {p1Id}",
                $"Player {p2Id}"
            );

            // Apply damage
            int winnerId = 0;
            if (winner == $"Player {p1Id}")
            {
                winnerId = p1Id;
                ApplyDamage(p2Id, damage);
            }
            else if (winner == $"Player {p2Id}")
            {
                winnerId = p2Id;
                ApplyDamage(p1Id, damage);
            }
            else // Tie
            {
                ApplyDamage(p1Id, damage);
                ApplyDamage(p2Id, damage);
            }

            OnMatchResult?.Invoke(p1Id, p2Id, winnerId);

            // Update recent opponents for matchmaking
            recentOpponents[p1Id].Add(p2Id);
            recentOpponents[p2Id].Add(p1Id);

            yield return new WaitForSeconds(combatDelay);
        }

        // Clear old opponent history (keep only last 2 rounds)
        if (currentTurn > 2)
        {
            ClearOldOpponentHistory();
        }
    }

    /// <summary>
    /// Generate match pairings using round-robin style.
    /// Tries to avoid repeat matchups when possible.
    /// </summary>
    private List<(int, int)> GenerateMatches()
    {
        var matches = new List<(int, int)>();
        var available = new List<int>(activePlayers);

        // Shuffle for randomness
        available = available.OrderBy(x => UnityEngine.Random.value).ToList();

        // Handle odd number of players (ghost match)
        if (available.Count % 2 != 0)
        {
            // Player with fewest recent fights gets a bye (no damage taken)
            int byePlayer = available.OrderBy(p => recentOpponents[p].Count).First();
            available.Remove(byePlayer);
            Debug.Log($"[LobbyManager] Player {byePlayer} gets a bye this round");
        }

        // Pair up remaining players
        while (available.Count >= 2)
        {
            int p1 = available[0];
            available.RemoveAt(0);

            // Find best opponent (preferring someone not recently fought)
            int bestOpponent = -1;
            int bestScore = int.MaxValue;

            foreach (int p2 in available)
            {
                int score = recentOpponents[p1].Contains(p2) ? 10 : 0;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestOpponent = p2;
                }
            }

            if (bestOpponent >= 0)
            {
                available.Remove(bestOpponent);
                matches.Add((p1, bestOpponent));
            }
        }

        return matches;
    }

    /// <summary>
    /// Apply damage to a player and update health tracking.
    /// </summary>
    private void ApplyDamage(int playerId, int damage)
    {
        if (!playerHealths.ContainsKey(playerId)) return;

        playerHealths[playerId] -= damage;
        Player player = GetPlayer(playerId);
        if (player != null)
        {
            player.Health = playerHealths[playerId];
        }

        Debug.Log($"[LobbyManager] Player {playerId} takes {damage} damage. Health: {playerHealths[playerId]}");
    }

    /// <summary>
    /// Check for and handle player eliminations.
    /// </summary>
    private void CheckEliminations()
    {
        var toEliminate = activePlayers.Where(p => playerHealths[p] <= 0).ToList();

        foreach (int playerId in toEliminate)
        {
            activePlayers.Remove(playerId);
            eliminationOrder.Add(playerId);

            int placement = 4 - eliminationOrder.Count + 1; // 4th, 3rd, 2nd...
            Debug.Log($"[LobbyManager] Player {playerId} ELIMINATED! Placement: {placement}");

            OnPlayerEliminated?.Invoke(playerId, placement);
        }
    }

    /// <summary>
    /// Clear opponent history older than 2 rounds.
    /// </summary>
    private void ClearOldOpponentHistory()
    {
        foreach (var kvp in recentOpponents)
        {
            if (kvp.Value.Count > 2)
            {
                kvp.Value.Clear();
            }
        }
    }

    /// <summary>
    /// End the game and declare winner.
    /// </summary>
    private void EndGame()
    {
        currentPhase = GamePhase.Results;
        OnPhaseChanged?.Invoke(currentPhase);
        gameInProgress = false;

        int winnerId = activePlayers.Count > 0 ? activePlayers[0] : 0;

        Debug.Log("[LobbyManager] === GAME OVER ===");
        Debug.Log($"[LobbyManager] Winner: Player {winnerId}!");
        Debug.Log("[LobbyManager] Final Placements:");

        // Winner is 1st place
        if (winnerId > 0)
        {
            Debug.Log($"  1st: Player {winnerId}");
        }

        // Show elimination order (reversed = placement order)
        for (int i = eliminationOrder.Count - 1; i >= 0; i--)
        {
            int placement = 4 - i;
            Debug.Log($"  {placement}st/nd/rd/th: Player {eliminationOrder[i]}");
        }

        bool humanWon = winnerId == humanPlayerId;
        Debug.Log(humanWon ? "[LobbyManager] VICTORY! You are the champion!" : "[LobbyManager] DEFEAT! Better luck next time.");

        OnGameEnded?.Invoke(winnerId);
    }

    /// <summary>
    /// Get a player by ID.
    /// </summary>
    public Player GetPlayer(int playerId)
    {
        int index = playerId - 1;
        if (index >= 0 && index < players.Count)
        {
            return players[index];
        }
        return null;
    }

    /// <summary>
    /// Get player health.
    /// </summary>
    public int GetPlayerHealth(int playerId)
    {
        return playerHealths.ContainsKey(playerId) ? playerHealths[playerId] : 0;
    }

    /// <summary>
    /// Check if a player is still active (not eliminated).
    /// </summary>
    public bool IsPlayerActive(int playerId)
    {
        return activePlayers.Contains(playerId);
    }

    /// <summary>
    /// Get list of active player IDs.
    /// </summary>
    public List<int> GetActivePlayers()
    {
        return new List<int>(activePlayers);
    }

    /// <summary>
    /// Get player placement (1-4, or 0 if still playing).
    /// </summary>
    public int GetPlayerPlacement(int playerId)
    {
        if (activePlayers.Contains(playerId))
        {
            // Still playing - if game over, they won
            return currentPhase == GamePhase.Results ? 1 : 0;
        }

        int elimIndex = eliminationOrder.IndexOf(playerId);
        if (elimIndex >= 0)
        {
            return 4 - elimIndex; // First eliminated = 4th place
        }

        return 0;
    }

    /// <summary>
    /// Restart the game.
    /// </summary>
    [ContextMenu("Restart Game")]
    public void RestartGame()
    {
        StopAllCoroutines();
        gameInProgress = false;

        // Reset all player health
        foreach (var player in players)
        {
            player.Health = startingHealth;
            player.board.Clear();
            player.hand.Clear();
            player.coins = 0;
        }

        // Reset tavern pool
        if (TavernManager.Instance != null)
        {
            TavernManager.Instance.ResetPool();
        }

        // Reinitialize
        InitializeLobby();
        StartGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
