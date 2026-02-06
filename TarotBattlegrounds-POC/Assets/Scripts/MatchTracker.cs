using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tracks round-by-round battle history for the Match Info scoreboard.
/// Singleton — lives on a dedicated GameObject in the Game scene.
/// </summary>
public class MatchTracker : MonoBehaviour
{
    public static MatchTracker Instance { get; private set; }

    [System.Serializable]
    public struct BattleResult
    {
        public int p1Index;
        public int p2Index;
        public int winnerIndex; // -1 = tie
        public int damage;
        public int p1HpAfter;
        public int p2HpAfter;
    }

    [System.Serializable]
    public class RoundResult
    {
        public int turnNumber;
        public List<BattleResult> battles = new List<BattleResult>();
    }

    public struct PlayerStatus
    {
        public int index;
        public string name;
        public int hp;
        public int tier;
        public bool alive;
        public int placement; // 0 = still alive, 1+ = final placement
    }

    public List<RoundResult> roundHistory = new List<RoundResult>();
    private RoundResult currentRound;

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
    /// Begin tracking a new combat round.
    /// </summary>
    public void BeginRound(int turnNumber)
    {
        currentRound = new RoundResult { turnNumber = turnNumber };
    }

    /// <summary>
    /// Record a single battle result. Called from GameManager after each fight.
    /// </summary>
    public void RecordBattle(int p1, int p2, int winnerIndex, int damage)
    {
        if (currentRound == null)
            currentRound = new RoundResult { turnNumber = GameManager.Instance != null ? GameManager.Instance.TurnNumber : 0 };

        int p1Hp = GameManager.Instance != null ? GameManager.Instance.GetPlayerHealth(p1) : 0;
        int p2Hp = GameManager.Instance != null ? GameManager.Instance.GetPlayerHealth(p2) : 0;

        currentRound.battles.Add(new BattleResult
        {
            p1Index = p1,
            p2Index = p2,
            winnerIndex = winnerIndex,
            damage = damage,
            p1HpAfter = p1Hp,
            p2HpAfter = p2Hp
        });
    }

    /// <summary>
    /// Finalize the current round and add it to history.
    /// </summary>
    public void EndRound()
    {
        if (currentRound != null && currentRound.battles.Count > 0)
        {
            roundHistory.Add(currentRound);
            currentRound = null;
        }
    }

    /// <summary>
    /// Get the most recent completed round, or null if none.
    /// </summary>
    public RoundResult GetLastRound()
    {
        if (roundHistory.Count == 0) return null;
        return roundHistory[roundHistory.Count - 1];
    }

    /// <summary>
    /// Get a snapshot of all player statuses.
    /// </summary>
    public List<PlayerStatus> GetAllPlayerStatuses()
    {
        var statuses = new List<PlayerStatus>();
        if (GameManager.Instance == null) return statuses;

        var gm = GameManager.Instance;
        for (int i = 0; i < gm.players.Count && i < gm.playerCount; i++)
        {
            var player = gm.players[i];
            if (player == null) continue;

            int hp = gm.GetPlayerHealth(i);
            statuses.Add(new PlayerStatus
            {
                index = i,
                name = GetPlayerDisplayName(i),
                hp = hp,
                tier = player.currentTavernTier,
                alive = hp > 0,
                placement = 0
            });
        }
        return statuses;
    }

    private string GetPlayerDisplayName(int index)
    {
        bool isHuman = GameConfig.IsHumanPlayer(index);

#if PHOTON_UNITY_NETWORKING
        if (GameManager.Instance.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            if (NetworkGameBridge.Instance.IsNetworkPlayerSlot(index))
            {
                int actorNum = NetworkGameBridge.Instance.SlotToActor[index];
                foreach (var p in Photon.Pun.PhotonNetwork.PlayerList)
                {
                    if (p.ActorNumber == actorNum)
                    {
                        bool isLocal = p.IsLocal;
                        return isLocal ? $"{p.NickName} (You)" : p.NickName;
                    }
                }
            }
            return $"Player {index + 1} (AI)";
        }
#endif

        if (isHuman)
            return $"Player {index + 1} (You)";
        return $"Player {index + 1} (AI)";
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
