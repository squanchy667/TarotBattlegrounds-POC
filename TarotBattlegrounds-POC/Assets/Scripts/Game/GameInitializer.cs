using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player/AI/hero-power initialization extracted from GameManager. Plain C#
/// class — constructed once in GameManager.Awake().
/// </summary>
public class GameInitializer
{
    private readonly GameManager gm;
    private readonly GameSessionState session;
    private readonly Dictionary<int, AIController> aiControllers;

    // H12 fix: Stores the callback so UI can invoke it after player picks.
    private System.Action<HeroPowerBase> _heroPowerSelectionCallback;

    public GameInitializer(GameManager gm, GameSessionState session, Dictionary<int, AIController> aiControllers)
    {
        this.gm = gm;
        this.session = session;
        this.aiControllers = aiControllers;
    }

    /// <summary>
    /// Apply runtime balance config from RuntimeDataLoader if available.
    /// Moved verbatim from GameManager.ApplyRuntimeConfig.
    /// </summary>
    public void ApplyRuntimeConfig()
    {
        if (RuntimeDataLoader.Instance == null || !RuntimeDataLoader.Instance.IsLoaded) return;

        var config = RuntimeDataLoader.Instance.Config;
        if (config == null) return;

        if (config.recruitTimerSeconds > 0)
        {
            gm.recruitTimer = config.recruitTimerSeconds;
            Debug.Log($"[GameManager] Runtime config: recruitTimer={gm.recruitTimer}s");
        }

        if (config.startingHealth > 0)
        {
            session.StartingHealth = config.startingHealth;
            Debug.Log($"[GameManager] Runtime config: startingHealth={session.StartingHealth}");
        }
    }

    /// <summary>
    /// Ensure the players list has enough Player instances for the requested count.
    /// Instantiates additional players from the prefab if needed.
    /// Moved verbatim from GameManager.EnsurePlayerCount.
    /// </summary>
    public void EnsurePlayerCount(int required)
    {
        if (gm.players == null)
            gm.players = new List<Player>();

        while (gm.players.Count < required)
        {
            if (gm.playerPrefab == null)
            {
                Debug.LogError($"[GameManager] playerPrefab is null — cannot spawn Player {gm.players.Count + 1}. Assign it in the Inspector.");
                return;
            }

            GameObject obj = Object.Instantiate(gm.playerPrefab);
            obj.name = $"Player {gm.players.Count + 1} (Spawned)";
            Player p = obj.GetComponent<Player>();
            if (p == null)
            {
                Debug.LogError($"[GameManager] playerPrefab has no Player component!");
                Object.Destroy(obj);
                return;
            }
            gm.players.Add(p);
            Debug.Log($"[GameManager] Spawned additional Player {gm.players.Count}");
        }
    }

    /// <summary>
    /// Initialize player objects and tavern slots (shared between online/offline).
    /// Moved verbatim from GameManager.InitializePlayers.
    /// </summary>
    public void InitializePlayers()
    {
        // M6: Clear stale ability registrations from previous games
        AbilityManager.ClearAll();

        for (int i = 0; i < gm.playerCount; i++)
        {
            if (gm.players[i] == null)
            {
                Debug.LogError($"Player {i + 1} is null in GameManager.players!");
                return;
            }
            gm.players[i].playerId = i + 1;
            Debug.Log($"Player {i + 1}: {gm.players[i].gameObject.name}, Instance ID: {gm.players[i].GetInstanceID()}");

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
        for (int i = gm.playerCount; i < gm.players.Count; i++)
        {
            if (gm.players[i] != null)
            {
                gm.players[i].gameObject.SetActive(false);
                Debug.Log($"[GameManager] Player {i + 1} disabled (not needed for {gm.playerCount}-player game)");
            }
        }

        session.InitializeHealths(session.StartingHealth, gm.playerCount);

        // Apply runtime config to each player (upgrade costs, etc.)
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Config != null)
        {
            for (int i = 0; i < gm.playerCount; i++)
            {
                gm.players[i].Health = session.StartingHealth;
                gm.players[i].ApplyRuntimeConfig(RuntimeDataLoader.Instance.Config);
            }
        }

        // Initialize matchmaking history
        gm.ResetPairingHistory(gm.playerCount);
    }

    /// <summary>
    /// Initialize AI controllers for non-human players.
    /// Moved verbatim from GameManager.InitializeAI.
    /// </summary>
    public void InitializeAI()
    {
        for (int i = 0; i < gm.playerCount; i++)
        {
            if (!GameConfig.IsHumanPlayer(i))
            {
                AIController ai = gm.players[i].GetComponent<AIController>();
                if (ai == null)
                {
                    ai = gm.players[i].gameObject.AddComponent<AIController>();
                }

                ai.Initialize(gm.players[i]);
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
        for (int i = 0; i < gm.playerCount; i++)
        {
            if (session.PlayerHealths[i] > 0)
                gm.players[i].RefreshShop(1);
        }
    }

    /// <summary>
    /// T115 / H12 fix: Assign hero powers at game start.
    /// AI players get a random auto-assignment.
    /// Human players fire OnHeroPowerSelectionNeeded so UI can present 3 choices;
    /// the first choice is auto-assigned as a fallback if no UI subscriber responds.
    /// Moved verbatim from GameManager.InitializeHeroPowers.
    /// </summary>
    public void InitializeHeroPowers()
    {
        if (HeroPowerManager.Instance == null)
        {
            Debug.Log("[GameManager] No HeroPowerManager found, skipping hero power init");
            return;
        }

        for (int i = 0; i < gm.playerCount; i++)
        {
            if (session.PlayerHealths[i] <= 0) continue;

            // H12 fix: human players get a selection UI; AI players get random auto-assignment
            if (GameConfig.IsHumanPlayer(i))
            {
                int playerId = gm.players[i].playerId;
                List<HeroPowerBase> choices = HeroPowerManager.Instance.GetRandomChoices(3);

                // Build the callback that assigns whichever power the player picks
                _heroPowerSelectionCallback = (chosen) =>
                {
                    HeroPowerManager.Instance.AssignHeroPower(playerId, chosen);
                    Debug.Log($"[GameManager] Human player {i + 1} selected hero power: {chosen.PowerName}");
                };

                gm.RaiseHeroPowerSelectionNeeded(i, choices, _heroPowerSelectionCallback);
            }
            else
            {
                HeroPowerManager.Instance.AutoAssignForAI(gm.players[i].playerId);
            }
        }

        Debug.Log($"[GameManager] Hero powers initialised for {gm.playerCount} players");
    }
}
