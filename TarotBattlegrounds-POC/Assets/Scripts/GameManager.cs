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

    // H12 fix: Event fired when a human player needs to pick a hero power.
    // Subscribers (e.g. GameUIManager) show a selection UI and invoke the callback.
    // If no subscriber calls the callback before the fallback timer, the first choice is auto-assigned.
    public static event System.Action<List<HeroPowerBase>, System.Action<HeroPowerBase>> OnHeroPowerSelectionNeeded;

    [Header("Player Settings (Auto-configured from GameConfig)")]
    [Tooltip("Number of active players (read from GameConfig)")]
    public int playerCount = 2;

    [Header("Dynamic Player Spawning")]
    [Tooltip("Prefab used to instantiate additional players when count exceeds scene objects")]
    [SerializeField] internal GameObject playerPrefab;

    /// <summary>Base recruit seconds (turn 1). Scales up with turn for testability late-game.</summary>
    internal float recruitTimer = 45f;

    /// <summary>
    /// Recruit duration grows as the game goes on so high gold / full boards stay playable.
    /// Turn 1: base · then +6s per turn · hard cap 120s.
    /// </summary>
    public float GetRecruitTimerForTurn(int turnNumber)
    {
        int t = Mathf.Max(1, turnNumber);
        float seconds = recruitTimer + (t - 1) * 6f;
        return Mathf.Clamp(seconds, recruitTimer, 120f);
    }

    [Header("AI Settings (Legacy - use GameConfig instead)")]
    [Tooltip("These are now read from GameConfig automatically")]
    public int humanPlayerIndex = 0;
    public AIDifficulty defaultAIDifficulty = AIDifficulty.Medium;

    private Dictionary<int, AIController> aiControllers = new Dictionary<int, AIController>();

    // H4 fix: Guards against starting the host game loop twice (e.g. both
    // OnNetworkSetupComplete and AssumeHostDuties fire on the same frame).
    private bool _hostGameLoopRunning = false;

    // Extracted god-class helpers (Game/ folder) — constructed in Awake().
    private GameSessionState session;
    private BattlePairingService pairing;
    private BattleExecutor battleExecutor;
    private GameInitializer initializer;

    public GamePhase CurrentPhase => session.CurrentPhase;
    public int TurnNumber => session.TurnNumber;

    /// <summary>
    /// Apply phase and turn from network RPC (client-side).
    /// </summary>
    public void SetPhaseFromNetwork(string phase, int turn)
    {
        session.TurnNumber = turn;
        if (phase == "Combat")
            session.CurrentPhase = GamePhase.Combat;
        else
            session.CurrentPhase = GamePhase.Recruit;
        Debug.Log($"[GameManager] Network phase sync: {session.CurrentPhase}, Turn {session.TurnNumber}");
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
        if (session.CurrentPhase != GamePhase.Recruit) return;

        if (session.IsReady(playerIndex)) return;

        session.MarkReady(playerIndex);
        Debug.Log($"[GameManager] Player {playerIndex + 1} is ready for combat ({session.ReadyCount}/{session.GetAlivePlayerCount(playerCount)}).");
    }

    /// <summary>
    /// Check whether a specific player has already clicked End Turn this phase.
    /// </summary>
    public bool IsPlayerReady(int playerIndex) => session.IsReady(playerIndex);

    /// <summary>
    /// Legacy shortcut — kept for backward compatibility. Marks all alive players as ready.
    /// </summary>
    public void EndRecruitPhaseEarly()
    {
        if (session.CurrentPhase != GamePhase.Recruit) return;

        for (int i = 0; i < playerCount; i++)
        {
            if (session.GetHealth(i) > 0)
                session.MarkReady(i);
        }
        Debug.Log("[GameManager] Recruit phase force-ended (all players marked ready).");
    }

    private void TriggerGameOver(int winnerIndex)
    {
        // Build standings: winner first, then reverse elimination order (last eliminated = 2nd place)
        List<int> standings = session.BuildStandings(winnerIndex, playerCount);

        GameOverData data = new GameOverData
        {
            winnerPlayerIndex = winnerIndex,
            standings = standings,
            totalTurns = session.TurnNumber
        };

        Debug.Log($"[GameManager] Game Over! Winner: Player {(winnerIndex >= 0 ? (winnerIndex + 1).ToString() : "None")}. Turns played: {session.TurnNumber}");
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
        PlayerStateSync.Apply(state, players, session);
    }

    /// <summary>
    /// Check if a player is human-controlled.
    /// </summary>
    public bool IsHumanPlayer(int playerIndex) => GameConfig.IsHumanPlayer(playerIndex);

    public int GetPlayerHealth(int index) => session.GetHealth(index);

    void Start()
    {
        // Load settings from GameConfig
        playerCount = GameConfig.PlayerCount;
        humanPlayerIndex = GameConfig.HumanPlayerIndex;
        defaultAIDifficulty = GameConfig.DefaultAIDifficulty;

        GameConfig.LogConfig();

        // Apply runtime config overrides if available
        initializer.ApplyRuntimeConfig();

        // Apply 8-player scaling if EightPlayerManager is present
        if (EightPlayerManager.Instance != null)
        {
            session.StartingHealth = EightPlayerManager.Instance.GetStartingHealth(playerCount);
            Debug.Log($"[GameManager] 8-player scaling: startingHealth={session.StartingHealth} for {playerCount} players");
        }

        // Spawn additional player objects if the scene doesn't have enough
        initializer.EnsurePlayerCount(playerCount);

        if (players == null || players.Count < playerCount)
        {
            Debug.LogError($"GameManager requires at least {playerCount} Player instances! Found: {players?.Count ?? 0}. Assign playerPrefab in the Inspector.");
            return;
        }

        // In online mode, wait for NetworkGameSetup to finish slot assignment
        if (IsOnlineMode)
        {
            Debug.Log("[GameManager] Online mode: waiting for network setup...");
            initializer.InitializePlayers();
            // Don't start game loop yet — OnNetworkSetupComplete() will do it on host
            return;
        }

        // Offline mode: normal initialization
        initializer.InitializePlayers();
        initializer.InitializeAI();
        initializer.InitializeHeroPowers();
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// H12 fix: Raises OnHeroPowerSelectionNeeded (or auto-assigns the first choice
    /// if there is no subscriber). Events can only be invoked from their declaring
    /// class, so GameInitializer routes through this internal method.
    /// </summary>
    internal void RaiseHeroPowerSelectionNeeded(int playerIndex, List<HeroPowerBase> choices, System.Action<HeroPowerBase> callback)
    {
        if (OnHeroPowerSelectionNeeded != null)
        {
            // Notify any wired UI; the UI is responsible for invoking the callback
            Debug.Log($"[GameManager] Presenting hero power selection UI for player {playerIndex + 1}");
            OnHeroPowerSelectionNeeded.Invoke(choices, callback);
        }
        else
        {
            // No UI wired yet — auto-assign the first choice so the game still works
            Debug.Log($"[GameManager] No hero power UI subscriber; auto-assigning first choice for player {playerIndex + 1}");
            if (choices.Count > 0)
                callback.Invoke(choices[0]);
        }
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
            initializer.InitializeAI();
            // H4 fix: record that the host game loop is now running so AssumeHostDuties
            // does not start a second coroutine if it is called after a host migration.
            _hostGameLoopRunning = true;
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

    /// <summary>
    /// Reset the pairing/matchmaking history. Routed through the facade because
    /// GameInitializer (which needs to call this from InitializePlayers) does not
    /// hold a direct reference to the BattlePairingService instance.
    /// </summary>
    internal void ResetPairingHistory(int count) => pairing.Reset(count);

    // ================================================================
    // H4: HOST DISCONNECT RECOVERY
    // ================================================================

    /// <summary>
    /// H4 fix: Mark a disconnected player as eliminated and keep the game running.
    /// Safe to call from NetworkGameBridge during either phase.
    ///
    /// - Sets health to 0 so the game loop skips this player in future iterations.
    /// - Adds to eliminationOrder so standings are correct.
    /// - Auto-readies the slot so the recruit phase is not blocked waiting for the leaver.
    /// </summary>
    public void EliminateDisconnectedPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerCount) return;
        if (session.PlayerHealths == null || playerIndex >= session.PlayerHealths.Count) return;
        if (session.PlayerHealths[playerIndex] <= 0) return; // Already eliminated

        Debug.LogWarning($"[GameManager] EliminateDisconnectedPlayer: slot {playerIndex} eliminated due to disconnect.");

        session.SetHealth(playerIndex, 0);
        if (players != null && playerIndex < players.Count)
            players[playerIndex].Health = 0;

        session.RecordElimination(playerIndex);

        // Auto-ready the slot so the recruit phase timer is not blocked
        session.MarkReady(playerIndex);

        Debug.Log($"[GameManager] Slot {playerIndex} eliminated (disconnected). " +
                  $"Remaining alive: {session.GetAlivePlayerCount(playerCount)}");
    }

    /// <summary>
    /// H4 fix: Called when this client becomes the new Photon master client (host migration).
    ///
    /// If the game loop is not already running on this client (it was a non-host client
    /// before the switch) this starts the game loop. The current player states were already
    /// seeded via RPCs from the previous host, so we reconstruct health from live Player
    /// objects and start the next recruit phase.
    ///
    /// If the game loop IS already running (this was the host before and migration happened
    /// for another reason) we just push a full state broadcast to re-anchor clients.
    /// </summary>
    public void AssumeHostDuties()
    {
        Debug.LogWarning("[GameManager] AssumeHostDuties: this client is now the game host.");

        if (!IsOnlineMode)
        {
            Debug.LogWarning("[GameManager] AssumeHostDuties called in offline mode — ignoring.");
            return;
        }

#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[GameManager] AssumeHostDuties: not master client — ignoring.");
            return;
        }
#endif

        // Rebuild playerHealths from live Player objects in case we are out of sync.
        session.RebuildFromPlayers(players, playerCount);

        // Ensure recentOpponents map is initialized (may be empty if this was a
        // client); preserves accumulated history like the original null-guard.
        pairing.EnsureInitialized(playerCount);

        // Attach AI controllers to all slots without a live Photon actor.
        for (int i = 0; i < playerCount && i < players.Count; i++)
        {
            bool hasLiveActor = NetworkGameBridge.Instance != null
                                && NetworkGameBridge.Instance.IsNetworkPlayerSlot(i);

            if (!hasLiveActor && !aiControllers.ContainsKey(i))
            {
                AIController ai = players[i].GetComponent<AIController>();
                if (ai == null)
                {
                    ai = players[i].gameObject.AddComponent<AIController>();
                    ai.Initialize(players[i]);
                    ai.difficulty = GameConfig.DefaultAIDifficulty;
                }
                aiControllers[i] = ai;
                Debug.Log($"[GameManager] AssumeHostDuties: registered AI for slot {i}");
            }
        }

        // Re-initialise hero powers for any slot that does not yet have one.
        // GetHeroPower uses playerId (= slotIndex + 1), not slotIndex.
        if (HeroPowerManager.Instance != null)
        {
            for (int i = 0; i < playerCount && i < players.Count; i++)
            {
                if (session.PlayerHealths[i] <= 0) continue;
                int playerId = players[i].playerId;
                if (HeroPowerManager.Instance.GetHeroPower(playerId) == null)
                    HeroPowerManager.Instance.AutoAssignForAI(playerId);
            }
        }

        if (!_hostGameLoopRunning)
        {
            _hostGameLoopRunning = true;
            Debug.Log("[GameManager] AssumeHostDuties: starting game loop as new host.");

            // Push a full state snapshot so all clients re-anchor before the first RPC
            // of the new game loop arrives.
            if (NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastFullStateSnapshot();

            StartCoroutine(GameLoop());
        }
        else
        {
            // Game loop is already ticking on this client. Just push a snapshot so
            // clients that may have missed events during the transition catch up.
            Debug.Log("[GameManager] AssumeHostDuties: game loop already running — pushing full state snapshot.");
            if (NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastFullStateSnapshot();
        }
    }

    IEnumerator GameLoop()
    {
        while (session.PlayerHealths.Any(h => h > 0))
        {
            yield return StartCoroutine(RecruitPhase());
            session.CurrentPhase = GamePhase.Combat;

            // Notify UI of phase change + board env still
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.RefreshAllUI();
                GameUIManager.Instance.ApplyPhaseEnvironmentBackground(GamePhase.Combat, force: true);
            }

#if PHOTON_UNITY_NETWORKING
            // Broadcast phase change to clients
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastPhaseChange("Combat", session.TurnNumber, 0f);
#endif

            Debug.Log("Current Phase: " + session.CurrentPhase);
            List<int> activePlayers = session.PlayerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
            if (activePlayers.Count == 1)
            {
                TriggerGameOver(activePlayers[0]);
                yield break;
            }
            // Clear old opponent history after turn 2
            if (session.TurnNumber > 2)
                pairing.ClearOldOpponentHistory();

            // Delegate to EightPlayerManager for pairings when available (supports ghost opponents)
            List<(int, int)> battles = EightPlayerManager.Instance != null
                ? EightPlayerManager.Instance.GeneratePairings(activePlayers, session.TurnNumber)
                : pairing.GeneratePairwiseBattles(activePlayers);

            // Begin tracking this combat round
            if (MatchTracker.Instance != null)
                MatchTracker.Instance.BeginRound(session.TurnNumber);

            foreach (var (p1, p2) in battles)
            {
                if (p1 >= 0 && p2 >= 0 && p1 < playerCount && p2 < playerCount)
                {
                    yield return battleExecutor.RunBattle(p1, p2, activePlayers.Count);
                }
            }

            // Finalize round tracking and auto-show scoreboard
            if (MatchTracker.Instance != null)
                MatchTracker.Instance.EndRound();
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowMatchInfoAfterCombat();

            session.TurnNumber++;

            // Track newly eliminated players
            session.TrackNewEliminations(playerCount);

#if PHOTON_UNITY_NETWORKING
            // Broadcast all states after combat
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
                NetworkGameBridge.Instance.BroadcastAllPlayerStates();
#endif

            // Check for eliminations and game end AFTER combat
            List<int> remainingPlayers = session.PlayerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
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

    private IEnumerator RecruitPhase()
    {
        session.CurrentPhase = GamePhase.Recruit;

        // T315: Play recruit music
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayRecruitMusic();

#if PHOTON_UNITY_NETWORKING
        // Broadcast phase change to clients
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
            NetworkGameBridge.Instance.BroadcastPhaseChange("Recruit", session.TurnNumber, GetRecruitTimerForTurn(session.TurnNumber));
#endif

        Debug.Log("Current Phase: " + session.CurrentPhase);

        // M5 FIX: Game lifecycle event - Reduce upgrade costs for ALL players by 1 each turn
        if (session.TurnNumber > 1)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (session.GetHealth(i) <= 0) continue;
                var player = players[i];
                player.currentUpgradeCost = Mathf.Max(0, player.currentUpgradeCost - 1);
                Debug.Log($"[Lifecycle Event] Player {i + 1}: Upgrade cost reduced to {player.currentUpgradeCost}");
            }
        }

        float timer = GetRecruitTimerForTurn(session.TurnNumber);
        Debug.Log($"[RecruitTimer] Turn {session.TurnNumber}: {timer:0}s recruit time (base={recruitTimer})");
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.BeginRecruitTimer(timer);
            // Force recruit env art every recruit phase (shop_bg)
            GameUIManager.Instance.ApplyPhaseEnvironmentBackground(GamePhase.Recruit, force: true);
        }

        for (int i = 0; i < playerCount; i++)
        {
            if (session.GetHealth(i) <= 0) continue;
            var player = players[i];
            Debug.Log($"Turn {session.TurnNumber}: Recruit Phase - Time to build your board!");
            int expectedCoins = Mathf.Min(3 + (session.TurnNumber - 1), 10);
            if (session.TurnNumber > 1)
            {
                player.RefreshShop(session.TurnNumber);
            }
            Debug.Log($"Player {i + 1} Recruit Start: Coins = {player.coins}/{expectedCoins}, Upgrade Cost = {player.GetUpgradeCost()}, Current Tier = {player.currentTavernTier}, Hand Size = {player.hand.Count}, Board Size = {player.board.Count}");
        }

        // T113/T115: Reset hero powers for new turn and trigger recruit passives
        if (HeroPowerManager.Instance != null)
        {
            HeroPowerManager.Instance.ResetAllForNewTurn();
            for (int i = 0; i < playerCount; i++)
            {
                if (session.GetHealth(i) <= 0) continue;
                HeroPowerManager.Instance.TriggerRecruitPassives(players[i]);
            }
        }

        // Reset ready state for new recruit phase
        session.ClearReady();

#if PHOTON_UNITY_NETWORKING
        // Broadcast initial player states and shops to human clients
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (session.GetHealth(i) <= 0) continue;
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
            if (session.GetHealth(playerIndex) <= 0) continue;

            AIController ai = kvp.Value;
            Debug.Log($"[GameManager] AI Player {playerIndex + 1} executing turn...");
            ai.ExecuteTurn();
            PlayerReadyForCombat(playerIndex);
        }

        if (GameUIManager.Instance != null && GameUIManager.Instance.GetShopUI() != null)
            GameUIManager.Instance.GetShopUI().RefreshShopDisplay();
        int lastLoggedSecond = Mathf.FloorToInt(timer);
        while (timer > 0 && !session.AllAlivePlayersReady(playerCount))
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
            if (session.GetHealth(i) <= 0) continue;
            var player = players[i];
            player.EndRecruitPhase();
            Debug.Log($"Player {i + 1} Shop Offered: " + string.Join(", ", TavernManager.Instance.availableCards[i + 1].Select(c => c.cardName + " (Tier " + c.tier + ")")));
            Debug.Log($"Player {i + 1} Hand: " + string.Join(", ", player.hand.Select(c => c.cardName)));
            Debug.Log($"Player {i + 1} Board: " + string.Join(", ", player.board.Select(c => c.cardName + " (Tier " + c.tier + ")")) + " Size " + player.board.Count);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        session = new GameSessionState();
        pairing = new BattlePairingService();
        battleExecutor = new BattleExecutor(this, session, pairing);
        initializer = new GameInitializer(this, session, aiControllers);
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
