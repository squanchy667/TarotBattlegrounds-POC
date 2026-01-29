using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    private List<int> playerHealths;
    private GamePhase currentPhase = GamePhase.Recruit;
    private int turnNumber = 1;
    private float recruitTimer = 35f;

    [Header("AI Settings (Legacy - use GameConfig instead)")]
    [Tooltip("These are now read from GameConfig automatically")]
    public int humanPlayerIndex = 0;
    public AIDifficulty defaultAIDifficulty = AIDifficulty.Medium;

    private Dictionary<int, AIController> aiControllers = new Dictionary<int, AIController>();
    private HashSet<int> playersReadyForCombat = new HashSet<int>();
    private List<int> eliminationOrder = new List<int>(); // Players eliminated in order (first eliminated = last place)

    public GamePhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;

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

        OnGameOver?.Invoke(data);
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

        if (players == null || players.Count < playerCount)
        {
            Debug.LogError($"GameManager requires at least {playerCount} Player instances! Found: {players?.Count ?? 0}");
            return;
        }

        // Only use the configured number of players
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
                players[i].RefreshShop(1);
            }
            else
            {
                Debug.LogError($"TavernManager.Instance is null during GameManager Start!");
            }

            // Setup AI controllers for non-human players (using GameConfig)
            if (!GameConfig.IsHumanPlayer(i))
            {
                AIController ai = players[i].GetComponent<AIController>();
                if (ai == null)
                {
                    ai = players[i].gameObject.AddComponent<AIController>();
                }

                // Initialize the AI with player reference
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

        // Disable unused player objects
        for (int i = playerCount; i < players.Count; i++)
        {
            if (players[i] != null)
            {
                players[i].gameObject.SetActive(false);
                Debug.Log($"[GameManager] Player {i + 1} disabled (not needed for {playerCount}-player game)");
            }
        }

        playerHealths = new List<int>(Enumerable.Repeat(40, playerCount).ToArray());
        StartCoroutine(GameLoop());
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

            Debug.Log("Current Phase: " + currentPhase);
            List<int> activePlayers = playerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
            if (activePlayers.Count == 1)
            {
                TriggerGameOver(activePlayers[0]);
                yield break;
            }
            List<(int, int)> battles = GeneratePairwiseBattles(activePlayers);
            foreach (var (p1, p2) in battles)
            {
                if (p1 >= 0 && p2 >= 0 && p1 < playerCount && p2 < playerCount)
                {
                    var board1 = players[p1].board;
                    var board2 = players[p2].board;
                    string p1Name = $"Player {p1 + 1}" + (p1 != humanPlayerIndex ? " (AI)" : " (You)");
                    string p2Name = $"Player {p2 + 1}" + (p2 != humanPlayerIndex ? " (AI)" : " (You)");
                    var (damage, winner) = CombatManager.SimulateBattle(board1, board2, Mathf.Max(players[p1].currentTavernTier, players[p2].currentTavernTier), p1Name, p2Name);
                    Debug.Log($"[Combat] {p1Name} vs {p2Name}");
                    Debug.Log($"  {p1Name} Board: " + string.Join(", ", board1.Select(c => c.cardName)));
                    Debug.Log($"  {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName)));

                    if (winner == "Tie")
                    {
                        playerHealths[p1] -= damage;
                        playerHealths[p2] -= damage;
                        players[p1].Health = playerHealths[p1];
                        players[p2].Health = playerHealths[p2];
                        Debug.Log($"[Combat] TIE! Both take {damage} damage. {p1Name}: {playerHealths[p1]} HP, {p2Name}: {playerHealths[p2]} HP");
                    }
                    else if (winner == p1Name)
                    {
                        playerHealths[p2] -= damage;
                        players[p2].Health = playerHealths[p2];
                        Debug.Log($"[Combat] {p1Name} WINS! {p2Name} takes {damage} damage. Health: {playerHealths[p2]}");
                    }
                    else
                    {
                        playerHealths[p1] -= damage;
                        players[p1].Health = playerHealths[p1];
                        Debug.Log($"[Combat] {p2Name} WINS! {p1Name} takes {damage} damage. Health: {playerHealths[p1]}");
                    }
                }
            }
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
            yield return new WaitForSeconds(5f);
        }
    }

    private List<(int, int)> GeneratePairwiseBattles(List<int> activePlayers)
    {
        List<(int, int)> battles = new List<(int, int)>();

        // Clone and shuffle the list
        var shuffled = activePlayers.OrderBy(x => Random.value).ToList();

        // Handle odd number of players - one gets a bye (no battle)
        if (shuffled.Count % 2 != 0)
        {
            int byePlayer = shuffled[shuffled.Count - 1];
            shuffled.RemoveAt(shuffled.Count - 1);
            Debug.Log($"[GameManager] Player {byePlayer + 1} gets a bye this round");
        }

        // Pair up remaining players
        for (int i = 0; i < shuffled.Count; i += 2)
        {
            battles.Add((shuffled[i], shuffled[i + 1]));
        }
        return battles;
    }

    private IEnumerator RecruitPhase()
    {
        currentPhase = GamePhase.Recruit;

        // Notify UI of phase change (enables End Turn, Freeze buttons)
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.RefreshAllUI();

        Debug.Log("Current Phase: " + currentPhase);
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

        // Reset ready state for new recruit phase
        playersReadyForCombat.Clear();

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

            timer -= Time.deltaTime;
            yield return null;
        }
        // while (timer > 0)
        // {
        //     int currentSecond = Mathf.FloorToInt(timer);
        //     if (currentSecond < lastLoggedSecond)
        //     {
        //         Debug.Log($"Recruit Phase: {timer:F1}s remaining");
        //         lastLoggedSecond = currentSecond;
        //     }
        //     timer -= Time.deltaTime;
        //     yield return null;
        // }
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
    if (Instance == this)
    {
        Instance = null;
    }
}      

}