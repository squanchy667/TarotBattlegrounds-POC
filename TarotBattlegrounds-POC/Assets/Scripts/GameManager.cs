using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TarotBattlegrounds.Combat.Animator;
using TarotBattlegrounds.Combat.Audio;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// Data for the game over event.
/// </summary>
public struct GameOverData
{
    public int winnerPlayerIndex;       // -1 if all eliminated
    public List<int> standings;         // Player indices ordered by placement (first = 1st place)
    public int totalTurns;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GamePhase { Recruit, Combat }
    public List<Player> players;

    /// <summary>
    /// Fired when the game ends. Carries standings data.
    /// </summary>
    public static event System.Action<GameOverData> OnGameOver;

    [Header("Player Settings (Auto-configured from GameConfig)")]
    [Tooltip("Number of active players (read from GameConfig)")]
    public int playerCount = 2;

    [Header("Dynamic Player Spawning")]
    [Tooltip("Prefab used to instantiate additional players when count exceeds scene objects")]
    [SerializeField] private GameObject playerPrefab;

    private List<int> playerHealths;
    private GamePhase currentPhase = GamePhase.Recruit;
    private int turnNumber = 1;
    private float recruitTimer = 35f;
    private int startingHealth = 40;

    [Header("AI Settings (Legacy - use GameConfig instead)")]
    [Tooltip("These are now read from GameConfig automatically")]
    public int humanPlayerIndex = 0;
    public AIDifficulty defaultAIDifficulty = AIDifficulty.Medium;

    private Dictionary<int, AIController> aiControllers = new Dictionary<int, AIController>();
    private HashSet<int> playersReadyForCombat = new HashSet<int>();
    private List<int> eliminationOrder = new List<int>(); // Players eliminated in order (first eliminated = last place)

    // Matchmaking history to avoid consecutive same opponents
    private Dictionary<int, HashSet<int>> recentOpponents = new Dictionary<int, HashSet<int>>();

    public GamePhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;

    /// <summary>
    /// Apply phase and turn from network RPC (client-side).
    /// </summary>
    public void SetPhaseFromNetwork(string phase, int turn)
    {
        turnNumber = turn;
        if (phase == "Combat")
            currentPhase = GamePhase.Combat;
        else
            currentPhase = GamePhase.Recruit;
        Debug.Log($"[GameManager] Network phase sync: {currentPhase}, Turn {turnNumber}");
    }

    /// <summary>
    /// True if we are in an online multiplayer game.
    /// </summary>
    public bool IsOnlineMode => GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer;

    /// <summary>
    /// True if this client is the authoritative host (MasterClient) in online mode.
    /// Always true in offline mode.
    /// </summary>
#if PHOTON_UNITY_NETWORKING
    public bool IsHost => !IsOnlineMode || PhotonNetwork.IsMasterClient;
#else
    public bool IsHost => true;
#endif

    /// <summary>
    /// Mark a player as ready to move to combat. The recruit phase ends early
    /// only when ALL alive players have called this.
    /// </summary>
    public void PlayerReadyForCombat(int playerIndex)
    {
        if (currentPhase != GamePhase.Recruit) return;

        if (playersReadyForCombat.Contains(playerIndex)) return;

        playersReadyForCombat.Add(playerIndex);
        Debug.Log($"[GameManager] Player {playerIndex + 1} is ready for combat ({playersReadyForCombat.Count}/{GetAlivePlayerCount()}).");
    }

    /// <summary>
    /// Check whether a specific player has already clicked End Turn this phase.
    /// </summary>
    public bool IsPlayerReady(int playerIndex) => playersReadyForCombat.Contains(playerIndex);

    private int GetAlivePlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0) count++;
        }
        return count;
    }

    private bool AllAlivePlayersReady()
    {
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0 && !playersReadyForCombat.Contains(i))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Legacy shortcut — kept for backward compatibility. Marks all alive players as ready.
    /// </summary>
    public void EndRecruitPhaseEarly()
    {
        if (currentPhase != GamePhase.Recruit) return;

        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0)
                playersReadyForCombat.Add(i);
        }
        Debug.Log("[GameManager] Recruit phase force-ended (all players marked ready).");
    }

    private void TriggerGameOver(int winnerIndex)
    {
        // Build standings: winner first, then reverse elimination order (last eliminated = 2nd place)
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

        GameOverData data = new GameOverData
        {
            winnerPlayerIndex = winnerIndex,
            standings = standings,
            totalTurns = turnNumber
        };

        Debug.Log($"[GameManager] Game Over! Winner: Player {(winnerIndex >= 0 ? (winnerIndex + 1).ToString() : "None")}. Turns played: {turnNumber}");
        for (int i = 0; i < standings.Count; i++)
        {
            Debug.Log($"  #{i + 1}: Player {standings[i] + 1}");
        }

#if PHOTON_UNITY_NETWORKING
        // Broadcast game over to clients
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
            NetworkGameBridge.Instance.BroadcastGameOver(data);
#endif

        // H8 fix: Submit match result to ranked system
        if (RankedManager.Instance != null && standings.Count > 0)
        {
            string matchId = System.Guid.NewGuid().ToString();
            var placements = new List<RankedManager.PlacementData>();
            for (int i = 0; i < standings.Count; i++)
            {
                placements.Add(new RankedManager.PlacementData
                {
                    playerId = players[standings[i]].playerId.ToString(),
                    placement = i + 1,
                    playerCount = standings.Count
                });
            }
            RankedManager.Instance.SubmitMatchResult(matchId, placements);
        }

        OnGameOver?.Invoke(data);
    }

    /// <summary>
    /// Invoke game over from network RPC (client-side).
    /// </summary>
    public static void InvokeGameOverFromNetwork(GameOverData data)
    {
        OnGameOver?.Invoke(data);
    }

    /// <summary>
    /// Apply a full player state received from the network (client-side).
    /// </summary>
    public void ApplyNetworkPlayerState(NetworkPlayerState state)
    {
        if (state.playerIndex < 0 || state.playerIndex >= players.Count) return;

        Player player = players[state.playerIndex];

        // Update basic stats (bypass events temporarily to avoid spam)
        player.coins = state.coins;
        player.currentTavernTier = state.tavernTier;
        player.Health = state.health;
        player.ShopFrozen = state.shopFrozen;
        // M5: Store synced upgrade cost for UI display
        player.SyncedUpgradeCost = state.upgradeCost;

        // Update health tracking
        if (state.playerIndex < playerHealths.Count)
            playerHealths[state.playerIndex] = state.health;

        // Reconstruct hand
        player.hand.Clear();
        if (state.hand != null)
        {
            int handFailed = 0;
            foreach (var cardData in state.hand)
            {
                Card card = cardData.ToCard();
                if (card != null)
                    player.hand.Add(card);
                else
                    handFailed++;
            }
            if (handFailed > 0)
                Debug.LogError($"[ApplyNetworkPlayerState] Player {state.playerIndex}: {handFailed}/{state.hand.Length} hand cards failed to reconstruct");
        }

        // Reconstruct board
        player.board.Clear();
        if (state.board != null)
        {
            int boardFailed = 0;
            foreach (var cardData in state.board)
            {
                Card card = cardData.ToCard();
                if (card != null)
                {
                    player.board.Add(card);
                    // M6: Log card stats to verify ability effects are synced
                    Debug.Log($"[Client/M6] P{state.playerIndex} board card: {card.cardName} {card.attack}/{card.health}, Aegis={card.hasAegis}");
                }
                else
                    boardFailed++;
            }
            if (boardFailed > 0)
                Debug.LogError($"[ApplyNetworkPlayerState] Player {state.playerIndex}: {boardFailed}/{state.board.Length} board cards failed to reconstruct");
        }

        // Reconstruct shop
        if (state.shopCards != null && TavernManager.Instance != null)
        {
            List<Card> shopCards = NetworkCardData.ToCardList(state.shopCards);
            TavernManager.Instance.SetShopFromNetwork(state.playerIndex, shopCards);
        }

        // Notify UI
        player.NotifyAllStateChanged();
    }

    /// <summary>
    /// Check if a player is human-controlled.
    /// </summary>
    public bool IsHumanPlayer(int playerIndex) => GameConfig.IsHumanPlayer(playerIndex);

    public int GetPlayerHealth(int index)
    {
        if (index >= 0 && index < playerHealths.Count)
            return playerHealths[index];
        return 0;
    }

    void Start()
    {
        // Load settings from GameConfig
        playerCount = GameConfig.PlayerCount;
        humanPlayerIndex = GameConfig.HumanPlayerIndex;
        defaultAIDifficulty = GameConfig.DefaultAIDifficulty;

        GameConfig.LogConfig();

        // Apply runtime config overrides if available
        ApplyRuntimeConfig();

        // Apply 8-player scaling if EightPlayerManager is present
        if (EightPlayerManager.Instance != null)
        {
            startingHealth = EightPlayerManager.Instance.GetStartingHealth(playerCount);
            Debug.Log($"[GameManager] 8-player scaling: startingHealth={startingHealth} for {playerCount} players");
        }

        // Spawn additional player objects if the scene doesn't have enough
        EnsurePlayerCount(playerCount);

        if (players == null || players.Count < playerCount)
        {
            Debug.LogError($"GameManager requires at least {playerCount} Player instances! Found: {players?.Count ?? 0}. Assign playerPrefab in the Inspector.");
            return;
        }

        // In online mode, wait for NetworkGameSetup to finish slot assignment
        if (IsOnlineMode)
        {
            Debug.Log("[GameManager] Online mode: waiting for network setup...");
            InitializePlayers();
            // Don't start game loop yet — OnNetworkSetupComplete() will do it on host
            return;
        }

        // Offline mode: normal initialization
        InitializePlayers();
        InitializeAI();
        InitializeHeroPowers();
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// Apply runtime balance config from RuntimeDataLoader if available.
    /// </summary>
    private void ApplyRuntimeConfig()
    {
        if (RuntimeDataLoader.Instance == null || !RuntimeDataLoader.Instance.IsLoaded) return;

        var config = RuntimeDataLoader.Instance.Config;
        if (config == null) return;

        if (config.recruitTimerSeconds > 0)
        {
            recruitTimer = config.recruitTimerSeconds;
            Debug.Log($"[GameManager] Runtime config: recruitTimer={recruitTimer}s");
        }

        if (config.startingHealth > 0)
        {
            startingHealth = config.startingHealth;
            Debug.Log($"[GameManager] Runtime config: startingHealth={startingHealth}");
        }
    }

    /// <summary>
    /// Ensure the players list has enough Player instances for the requested count.
    /// Instantiates additional players from the prefab if needed.
    /// </summary>
    private void EnsurePlayerCount(int required)
    {
        if (players == null)
            players = new List<Player>();

        while (players.Count < required)
        {
            if (playerPrefab == null)
            {
                Debug.LogError($"[GameManager] playerPrefab is null — cannot spawn Player {players.Count + 1}. Assign it in the Inspector.");
                return;
            }

            GameObject obj = Instantiate(playerPrefab);
            obj.name = $"Player {players.Count + 1} (Spawned)";
            Player p = obj.GetComponent<Player>();
            if (p == null)
            {
                Debug.LogError($"[GameManager] playerPrefab has no Player component!");
                Destroy(obj);
                return;
            }
            players.Add(p);
            Debug.Log($"[GameManager] Spawned additional Player {players.Count}");
        }
    }

    /// <summary>
    /// Initialize player objects and tavern slots (shared between online/offline).
    /// </summary>
    private void InitializePlayers()
    {
        // M6: Clear stale ability registrations from previous games
        AbilityManager.ClearAll();

        for (int i = 0; i < playerCount; i++)
        {
            if (players[i] == null)
            {
                Debug.LogError($"Player {i + 1} is null in GameManager.players!");
                return;
            }
            players[i].playerId = i + 1;
            Debug.Log($"Player {i + 1}: {players[i].gameObject.name}, Instance ID: {players[i].GetInstanceID()}");

            if (TavernManager.Instance != null)
            {
                if (!TavernManager.Instance.availableCards.ContainsKey(i + 1))
                {
                    TavernManager.Instance.availableCards[i + 1] = new List<Card>();
                }
            }
            else
            {
                Debug.LogError($"TavernManager.Instance is null during GameManager Start!");
            }
        }

        // Disable unused player objects
        for (int i = playerCount; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                players[i].gameObject.SetActive(false);
                Debug.Log($"[GameManager] Player {i + 1} disabled (not needed for {playerCount}-player game)");
            }
        }

        playerHealths = new List<int>(Enumerable.Repeat(startingHealth, playerCount).ToArray());

        // Apply runtime config to each player (upgrade costs, etc.)
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Config != null)
        {
            for (int i = 0; i < playerCount; i++)
            {
                players[i].Health = startingHealth;
                players[i].ApplyRuntimeConfig(RuntimeDataLoader.Instance.Config);
            }
        }

        // Initialize matchmaking history
        recentOpponents.Clear();
        for (int i = 0; i < playerCount; i++)
            recentOpponents[i] = new HashSet<int>();
    }

    /// <summary>
    /// Initialize AI controllers for non-human players.
    /// </summary>
    private void InitializeAI()
    {
        for (int i = 0; i < playerCount; i++)
        {
            if (!GameConfig.IsHumanPlayer(i))
            {
                AIController ai = players[i].GetComponent<AIController>();
                if (ai == null)
                {
                    ai = players[i].gameObject.AddComponent<AIController>();
                }

                ai.Initialize(players[i]);
                ai.difficulty = GameConfig.GetAIDifficulty(i);

                aiControllers[i] = ai;
                Debug.Log($"[GameManager] Player {i + 1} is AI ({ai.difficulty})");
            }
            else
            {
                Debug.Log($"[GameManager] Player {i + 1} is HUMAN");
            }
        }

        // Refresh shops for all players (offline) or host-side (online)
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] > 0)
                players[i].RefreshShop(1);
        }
    }

    /// <summary>
    /// T115: Auto-assign hero powers for all players at game start.
    /// AI players get random assignments. Human players would get a selection UI (Phase V).
    /// </summary>
    private void InitializeHeroPowers()
    {
        if (HeroPowerManager.Instance == null)
        {
            Debug.Log("[GameManager] No HeroPowerManager found, skipping hero power init");
            return;
        }

        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] <= 0) continue;
            // For now, auto-assign random hero powers for all players
            // Human player selection UI will be added in Phase V
            HeroPowerManager.Instance.AutoAssignForAI(players[i].playerId);
        }

        Debug.Log($"[GameManager] Hero powers assigned for {playerCount} players");
    }

    /// <summary>
    /// Called by NetworkGameSetup after slot assignment is complete.
    /// Host starts the game loop; clients just wait for state syncs.
    /// </summary>
    public void OnNetworkSetupComplete()
    {
        Debug.Log($"[GameManager] Network setup complete. IsHost={IsHost}");

        if (IsHost)
        {
            // Initialize AI for non-human slots on host
            InitializeAI();
            StartCoroutine(GameLoop());
        }
        // Clients don't run the game loop — they receive state via RPCs
    }

    /// <summary>
    /// Register an AI controller for a slot (used when converting disconnected player to AI).
    /// </summary>
    public void RegisterAIController(int slot, AIController ai)
    {
        aiControllers[slot] = ai;
        Debug.Log($"[GameManager] Registered AI controller for slot {slot}");
    }

    IEnumerator GameLoop()
    {
        while (playerHealths.Any(h => h > 0))
        {
            yield return StartCoroutine(RecruitPhase());
            currentPhase = GamePhase.Combat;

            // Notify UI of phase change
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.RefreshAllUI();

#if PHOTON_UNITY_NETWORKING
            // Broadcast phase change to clients
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastPhaseChange("Combat", turnNumber, 0f);
#endif

            Debug.Log("Current Phase: " + currentPhase);
            List<int> activePlayers = playerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
            if (activePlayers.Count == 1)
            {
                TriggerGameOver(activePlayers[0]);
                yield break;
            }
            // Clear old opponent history after turn 2
            if (turnNumber > 2)
                ClearOldOpponentHistory();

            // Delegate to EightPlayerManager for pairings when available (supports ghost opponents)
            List<(int, int)> battles = EightPlayerManager.Instance != null
                ? EightPlayerManager.Instance.GeneratePairings(activePlayers, turnNumber)
                : GeneratePairwiseBattles(activePlayers);

            // Begin tracking this combat round
            if (MatchTracker.Instance != null)
                MatchTracker.Instance.BeginRound(turnNumber);

            foreach (var (p1, p2) in battles)
            {
                if (p1 >= 0 && p2 >= 0 && p1 < playerCount && p2 < playerCount)
                {
                    // T115: Trigger combat-start hero powers before battle
                    if (HeroPowerManager.Instance != null)
                    {
                        HeroPowerManager.Instance.TriggerCombatPassives(players[p1], players[p2]);
                        HeroPowerManager.Instance.TriggerCombatPassives(players[p2], players[p1]);
                    }

                    var board1 = players[p1].board;
                    var board2 = players[p2].board;
                    string p1Name = $"Player {p1 + 1}" + (GameConfig.IsHumanPlayer(p1) ? "" : " (AI)");
                    string p2Name = $"Player {p2 + 1}" + (GameConfig.IsHumanPlayer(p2) ? "" : " (AI)");
                    var (damage, winner) = CombatManager.SimulateBattle(board1, board2, players[p1].currentTavernTier, players[p2].currentTavernTier, p1Name, p2Name);
                    Debug.Log($"[Combat] {p1Name} vs {p2Name}");
                    Debug.Log($"  {p1Name} Board: " + string.Join(", ", board1.Select(c => c.cardName)));
                    Debug.Log($"  {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName)));

                    // T316: Play animated combat replay if this is the local player's battle
                    if (CombatAnimator.Instance != null && CombatManager.lastReplay != null
                        && IsLocalPlayerBattle(p1, p2))
                    {
                        CombatAnimator.Instance.PlayReplay(CombatManager.lastReplay);
                        // Wait for animation to complete before proceeding
                        while (CombatAnimator.Instance.IsPlaying)
                            yield return null;
                    }

                    int winnerIndex;
                    if (winner == "Tie")
                    {
                        playerHealths[p1] -= damage;
                        playerHealths[p2] -= damage;
                        players[p1].Health = playerHealths[p1];
                        players[p2].Health = playerHealths[p2];
                        winnerIndex = -1;
                        Debug.Log($"[Combat] TIE! Both take {damage} damage. {p1Name}: {playerHealths[p1]} HP, {p2Name}: {playerHealths[p2]} HP");
                    }
                    else if (winner == p1Name)
                    {
                        playerHealths[p2] -= damage;
                        players[p2].Health = playerHealths[p2];
                        winnerIndex = p1;
                        Debug.Log($"[Combat] {p1Name} WINS! {p2Name} takes {damage} damage. Health: {playerHealths[p2]}");
                    }
                    else
                    {
                        playerHealths[p1] -= damage;
                        players[p1].Health = playerHealths[p1];
                        winnerIndex = p2;
                        Debug.Log($"[Combat] {p2Name} WINS! {p1Name} takes {damage} damage. Health: {playerHealths[p1]}");
                    }

                    // Record battle for match info scoreboard
                    if (MatchTracker.Instance != null)
                        MatchTracker.Instance.RecordBattle(p1, p2, winnerIndex, damage);

#if PHOTON_UNITY_NETWORKING
                    // Broadcast combat result and updated states to clients
                    if (IsOnlineMode && NetworkGameBridge.Instance != null)
                    {
                        NetworkGameBridge.Instance.BroadcastCombatResult(p1, p2, winner, damage);
                        NetworkGameBridge.Instance.BroadcastPlayerState(p1);
                        NetworkGameBridge.Instance.BroadcastPlayerState(p2);
                    }
#endif

                    // Track recent opponents for matchmaking
                    recentOpponents[p1].Add(p2);
                    recentOpponents[p2].Add(p1);
                }
            }

            // Finalize round tracking and auto-show scoreboard
            if (MatchTracker.Instance != null)
                MatchTracker.Instance.EndRound();
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowMatchInfoAfterCombat();

            turnNumber++;

            // Track newly eliminated players
            for (int i = 0; i < playerCount; i++)
            {
                if (playerHealths[i] <= 0 && !eliminationOrder.Contains(i))
                {
                    eliminationOrder.Add(i);
                    Debug.Log($"[GameManager] Player {i + 1} eliminated! (Elimination #{eliminationOrder.Count})");
                }
            }

#if PHOTON_UNITY_NETWORKING
            // Broadcast all states after combat
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastAllPlayerStates();
#endif

            // Check for eliminations and game end AFTER combat
            List<int> remainingPlayers = playerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
            if (remainingPlayers.Count == 1)
            {
                TriggerGameOver(remainingPlayers[0]);
                yield break;
            }
            if (remainingPlayers.Count == 0)
            {
                TriggerGameOver(-1);
                yield break;
            }
            // Shorten post-combat delay if animated replay already ran
            float postCombatDelay = CombatAnimator.Instance != null ? 1f : 5f;
            yield return new WaitForSeconds(postCombatDelay);
        }
    }

    /// <summary>
    /// T316: Check if a battle involves the local/human player for animated replay.
    /// </summary>
    private bool IsLocalPlayerBattle(int p1, int p2)
    {
        // In offline mode, check if either player is the human player
        if (!IsOnlineMode)
            return GameConfig.IsHumanPlayer(p1) || GameConfig.IsHumanPlayer(p2);

#if PHOTON_UNITY_NETWORKING
        if (NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            return p1 == localSlot || p2 == localSlot;
        }
#endif
        return false;
    }

    /// <summary>
    /// Generate match pairings avoiding recent opponents when possible.
    /// </summary>
    private List<(int, int)> GeneratePairwiseBattles(List<int> activePlayers)
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
    /// </summary>
    private void ClearOldOpponentHistory()
    {
        foreach (var kvp in recentOpponents)
        {
            if (kvp.Value.Count > 2)
                kvp.Value.Clear();
        }
    }

    private IEnumerator RecruitPhase()
    {
        currentPhase = GamePhase.Recruit;

        // T315: Play recruit music
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayRecruitMusic();

#if PHOTON_UNITY_NETWORKING
        // Broadcast phase change to clients
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
            NetworkGameBridge.Instance.BroadcastPhaseChange("Recruit", turnNumber, recruitTimer);
#endif

        Debug.Log("Current Phase: " + currentPhase);

        // M5 FIX: Game lifecycle event - Reduce upgrade costs for ALL players by 1 each turn
        if (turnNumber > 1)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (playerHealths[i] <= 0) continue;
                var player = players[i];
                player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
                Debug.Log($"[Lifecycle Event] Player {i + 1}: Upgrade cost reduced to {player.currentUpgradeCost}");
            }
        }

        float timer = recruitTimer;
        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] <= 0) continue;
            var player = players[i];
            Debug.Log($"Turn {turnNumber}: Recruit Phase - Time to build your board!");
            int expectedCoins = Mathf.Min(3 + (turnNumber - 1), 10);
            if (turnNumber > 1)
            {
                player.RefreshShop(turnNumber);
            }
            Debug.Log($"Player {i + 1} Recruit Start: Coins = {player.coins}/{expectedCoins}, Upgrade Cost = {player.GetUpgradeCost()}, Current Tier = {player.currentTavernTier}, Hand Size = {player.hand.Count}, Board Size = {player.board.Count}");
        }

        // T113/T115: Reset hero powers for new turn and trigger recruit passives
        if (HeroPowerManager.Instance != null)
        {
            HeroPowerManager.Instance.ResetAllForNewTurn();
            for (int i = 0; i < playerCount; i++)
            {
                if (playerHealths[i] <= 0) continue;
                HeroPowerManager.Instance.TriggerRecruitPassives(players[i]);
            }
        }

        // Reset ready state for new recruit phase
        playersReadyForCombat.Clear();

#if PHOTON_UNITY_NETWORKING
        // Broadcast initial player states and shops to human clients
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (playerHealths[i] <= 0) continue;
                NetworkGameBridge.Instance.BroadcastPlayerState(i);
                if (NetworkGameBridge.Instance.IsNetworkPlayerSlot(i))
                    NetworkGameBridge.Instance.BroadcastShopForPlayer(i);
            }
        }
#endif

        // BUG FIX M5 (UI): Refresh UI AFTER lifecycle events and state broadcasts
        // This ensures upgrade costs are updated before UI reads them
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.RefreshAllUI();

        // AI players make their decisions at start of recruit phase, then auto-ready
        foreach (var kvp in aiControllers)
        {
            int playerIndex = kvp.Key;
            if (playerHealths[playerIndex] <= 0) continue;

            AIController ai = kvp.Value;
            Debug.Log($"[GameManager] AI Player {playerIndex + 1} executing turn...");
            ai.ExecuteTurn();
            PlayerReadyForCombat(playerIndex);
        }

        if (GameUIManager.Instance != null && GameUIManager.Instance.GetShopUI() != null)
            GameUIManager.Instance.GetShopUI().RefreshShopDisplay();
        int lastLoggedSecond = Mathf.FloorToInt(timer);
        while (timer > 0 && !AllAlivePlayersReady())
        {
            // Update UI timer
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.UpdateTimer(timer);

#if PHOTON_UNITY_NETWORKING
            // Broadcast timer to clients (~every 1s)
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastTimerUpdate(timer);
#endif

            timer -= Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < playerCount; i++)
        {
            if (playerHealths[i] <= 0) continue;
            var player = players[i];
            player.EndRecruitPhase();
            Debug.Log($"Player {i + 1} Shop Offered: " + string.Join(", ", TavernManager.Instance.availableCards[i + 1].Select(c => c.cardName + " (Tier " + c.tier + ")")));
            Debug.Log($"Player {i + 1} Hand: " + string.Join(", ", player.hand.Select(c => c.cardName)));
            Debug.Log($"Player {i + 1} Board: " + string.Join(", ", player.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " Size " + player.board.Count);
        }
    }

    private void SimulateAI()
    {
        Debug.Log("AI opponent: Randomly buying and positioning cards (placeholder).");
    }

    private List<Card> GenerateAIBoard(int turnNumber, int playerTier)
    {
        List<Card> aiBoard = new List<Card>();
        int aiSize = Mathf.Min(turnNumber + Random.Range(0, 2), 7);
        List<Card> filteredPool = TavernManager.Instance.GetFullPool().Where(c => c.tier <= playerTier + 1).ToList();
        for (int i = 0; i < aiSize; i++)
        {
            if (filteredPool.Count == 0) break;
            int idx = Random.Range(0, filteredPool.Count);
            Card aiCard = filteredPool[idx].Clone();
            aiBoard.Add(aiCard);
            TavernManager.Instance.RemoveCardFromPool(aiCard);
            filteredPool.RemoveAt(idx);
        }
        return aiBoard;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // Optional: Uncomment the line below if GameManager should persist across scene loads
        // DontDestroyOnLoad(this.gameObject);
    }
    private void OnDestroy()
    {
        // M6: Clear ability registrations on game cleanup
        AbilityManager.ClearAll();

        if (Instance == this)
        {
            Instance = null;
        }
    }

}