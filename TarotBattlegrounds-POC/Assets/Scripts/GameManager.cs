using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum GamePhase { Recruit, Combat }
    public List<Player> players;
    public int playerCount = 4;
    private List<int> playerHealths;
    private GamePhase currentPhase = GamePhase.Recruit;
    private int turnNumber = 1;
    private float recruitTimer = 35f;

    public GamePhase CurrentPhase => currentPhase;
    public int TurnNumber => turnNumber;

    public int GetPlayerHealth(int index)
    {
        if (index >= 0 && index < playerHealths.Count)
            return playerHealths[index];
        return 0;
    }

    void Start()
    {
        if (players == null || players.Count != playerCount)
        {
            Debug.LogError($"GameManager requires exactly {playerCount} Player instances!");
            return;
        }
        for (int i = 0; i < players.Count; i++)
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
            Debug.Log("Current Phase: " + currentPhase);
            List<int> activePlayers = playerHealths.Select((h, i) => h > 0 ? i : -1).Where(i => i >= 0).ToList();
            if (activePlayers.Count == 1)
            {
                Debug.Log($"Game Over: Player {activePlayers[0] + 1} wins!");
                break;
            }
            List<(int, int)> battles = GeneratePairwiseBattles(activePlayers);
            foreach (var (p1, p2) in battles)
            {
                if (p1 >= 0 && p2 >= 0)
                {
                    var board1 = players[p1].board;
                    var board2 = p2 < playerCount ? players[p2].board : GenerateAIBoard(turnNumber, players[p1].currentTavernTier);
                    string p2Name = p2 < playerCount ? $"Player {p2 + 1}" : "AI";
                    var (damage, winner) = CombatManager.SimulateBattle(board1, board2, players[p1].currentTavernTier, $"Player {p1 + 1}", p2Name);
                    if (winner == "Tie")
                    {
                        playerHealths[p1] -= damage;
                        players[p1].Health = playerHealths[p1]; // Sync to Player.Health to fire OnHealthChanged
                        if (p2 < playerCount)
                        {
                            playerHealths[p2] -= damage;
                            players[p2].Health = playerHealths[p2]; // Sync to Player.Health to fire OnHealthChanged
                        }
                        Debug.Log($"Simulating combat... Player {p1 + 1} Board: " + string.Join(", ", board1.Select(c => c.cardName + " (Tier " + c.tier + ")")) + $" | {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName + " (Tier " + c.tier + ")")));
                        Debug.Log($"Combat outcome: Tie - Player {p1 + 1} takes {damage} damage. Health remaining: {playerHealths[p1]}");
                        if (p2 < playerCount)
                            Debug.Log($"Combat outcome: Tie - {p2Name} takes {damage} damage. Health remaining: {playerHealths[p2]}");
                    }
                    else if (winner == $"Player {p1 + 1}")
                    {
                        if (p2 < playerCount)
                        {
                            playerHealths[p2] -= damage;
                            players[p2].Health = playerHealths[p2]; // Sync to Player.Health to fire OnHealthChanged
                        }
                        Debug.Log($"Simulating combat... Player {p1 + 1} Board: " + string.Join(", ", board1.Select(c => c.cardName + " (Tier " + c.tier + ")")) + $" | {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName + " (Tier " + c.tier + ")")));
                        Debug.Log($"Combat outcome: Player {p1 + 1} wins, Health unchanged: {playerHealths[p1]}");
                        if (p2 < playerCount)
                            Debug.Log($"Combat outcome: {p2Name} takes {damage} damage. Health remaining: {playerHealths[p2]}");
                    }
                    else
                    {
                        playerHealths[p1] -= damage;
                        players[p1].Health = playerHealths[p1]; // Sync to Player.Health to fire OnHealthChanged
                        Debug.Log($"Simulating combat... Player {p1 + 1} Board: " + string.Join(", ", board1.Select(c => c.cardName + " (Tier " + c.tier + ")")) + $" | {p2Name} Board: " + string.Join(", ", board2.Select(c => c.cardName + " (Tier " + c.tier + ")")));
                        Debug.Log($"Combat outcome: {p2Name} wins, Player {p1 + 1} takes {damage} damage. Health remaining: {playerHealths[p1]}");
                        if (p2 < playerCount)
                            Debug.Log($"Combat outcome: {p2Name} Health unchanged: {playerHealths[p2]}");
                    }
                }
            }
            turnNumber++;
            if (playerHealths.All(h => h <= 0))
            {
                Debug.Log("Game Over: All players defeated");
                break;
            }
            yield return new WaitForSeconds(5f);
        }
    }

    private List<(int, int)> GeneratePairwiseBattles(List<int> activePlayers)
    {
        List<(int, int)> battles = new List<(int, int)>();
        if (activePlayers.Count % 2 != 0)
        {
            activePlayers.Add(playerCount);
        }
        activePlayers = activePlayers.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < activePlayers.Count; i += 2)
        {
            battles.Add((activePlayers[i], activePlayers[i + 1]));
        }
        return battles;
    }

    private IEnumerator RecruitPhase()
    {
        currentPhase = GamePhase.Recruit;
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
        if (GameUIManager.Instance != null && GameUIManager.Instance.GetShopUI() != null)
            GameUIManager.Instance.GetShopUI().RefreshShopDisplay();
        int lastLoggedSecond = Mathf.FloorToInt(timer);
        while (timer > 0)
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