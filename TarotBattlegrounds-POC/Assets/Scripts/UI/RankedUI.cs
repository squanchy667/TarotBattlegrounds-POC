using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T507-T513: Combined ranked UI — rank display, leaderboard, match history, season info.
    /// </summary>
    public class RankedUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject rankedPanel;

        [Header("Tabs")]
        [SerializeField] private Button profileTabButton;
        [SerializeField] private Button leaderboardTabButton;
        [SerializeField] private Button historyTabButton;
        [SerializeField] private GameObject profileTab;
        [SerializeField] private GameObject leaderboardTab;
        [SerializeField] private GameObject historyTab;

        [Header("Profile Tab (T507)")]
        [SerializeField] private TMP_Text rankTierText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text winsLossesText;
        [SerializeField] private TMP_Text seasonText;
        [SerializeField] private Image rankBadgeImage;
        [SerializeField] private Slider rankProgressBar;

        [Header("Leaderboard Tab (T513)")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardEntryPrefab;

        [Header("Match History Tab (T512)")]
        [SerializeField] private Transform historyContainer;
        [SerializeField] private GameObject historyEntryPrefab;

        [Header("Post-Game Stats (T510)")]
        [SerializeField] private GameObject postGamePanel;
        [SerializeField] private TMP_Text postGameRatingText;
        [SerializeField] private TMP_Text postGameChangeText;
        [SerializeField] private TMP_Text postGameRankText;
        [SerializeField] private Button postGameCloseButton;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        private static readonly Color[] TIER_COLORS = {
            new Color(0.8f, 0.5f, 0.2f),  // Bronze
            new Color(0.75f, 0.75f, 0.75f), // Silver
            new Color(1f, 0.84f, 0f),       // Gold
            new Color(0.4f, 0.8f, 0.8f),    // Platinum
            new Color(0.4f, 0.6f, 1f),      // Diamond
            new Color(0.6f, 0.2f, 0.8f),    // Master
            new Color(1f, 0.4f, 0.2f),       // Legend
        };

        private List<GameObject> leaderboardEntries = new List<GameObject>();
        private List<GameObject> historyEntries = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (postGameCloseButton != null) postGameCloseButton.onClick.AddListener(() => {
                if (postGamePanel != null) postGamePanel.SetActive(false);
            });
            if (profileTabButton != null) profileTabButton.onClick.AddListener(() => ShowTab(0));
            if (leaderboardTabButton != null) leaderboardTabButton.onClick.AddListener(() => ShowTab(1));
            if (historyTabButton != null) historyTabButton.onClick.AddListener(() => ShowTab(2));

            if (rankedPanel != null) rankedPanel.SetActive(false);
            if (postGamePanel != null) postGamePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (RankedManager.Instance != null)
            {
                RankedManager.Instance.OnProfileUpdated += RefreshProfile;
                RankedManager.Instance.OnLeaderboardUpdated += RefreshLeaderboard;
                RankedManager.Instance.OnMatchHistoryUpdated += RefreshHistory;
                RankedManager.Instance.OnSeasonUpdated += RefreshSeason;
                RankedManager.Instance.OnMatchResultProcessed += ShowPostGameStats;
            }
        }

        private void OnDisable()
        {
            if (RankedManager.Instance != null)
            {
                RankedManager.Instance.OnProfileUpdated -= RefreshProfile;
                RankedManager.Instance.OnLeaderboardUpdated -= RefreshLeaderboard;
                RankedManager.Instance.OnMatchHistoryUpdated -= RefreshHistory;
                RankedManager.Instance.OnSeasonUpdated -= RefreshSeason;
                RankedManager.Instance.OnMatchResultProcessed -= ShowPostGameStats;
            }
        }

        public void Open()
        {
            if (rankedPanel != null) rankedPanel.SetActive(true);
            ShowTab(0);

            EnsureRankedManager();
            RankedManager.Instance.FetchProfile();
            RankedManager.Instance.FetchSeason();
        }

        public void Close()
        {
            if (rankedPanel != null) rankedPanel.SetActive(false);
        }

        private void ShowTab(int index)
        {
            if (profileTab != null) profileTab.SetActive(index == 0);
            if (leaderboardTab != null) leaderboardTab.SetActive(index == 1);
            if (historyTab != null) historyTab.SetActive(index == 2);

            if (index == 1) RankedManager.Instance?.FetchLeaderboard();
            if (index == 2) RankedManager.Instance?.FetchMatchHistory();
        }

        private void RefreshProfile()
        {
            if (RankedManager.Instance == null) return;

            var rank = RankedManager.Instance.CurrentRank;
            int rating = RankedManager.Instance.CurrentRating;

            if (rankTierText != null)
            {
                string divStr = rank.division > 0 ? $" {new string('I', rank.division)}" : "";
                rankTierText.text = $"{rank.tier}{divStr}";
                rankTierText.color = rank.tierIndex < TIER_COLORS.Length ? TIER_COLORS[rank.tierIndex] : Color.white;
            }

            if (ratingText != null) ratingText.text = $"Rating: {rating}";

            if (winsLossesText != null)
            {
                int wins = RankedManager.Instance.Wins;
                int losses = RankedManager.Instance.Losses;
                int total = RankedManager.Instance.GamesPlayed;
                float winRate = total > 0 ? (float)wins / total * 100 : 0;
                winsLossesText.text = $"{wins}W / {losses}L ({winRate:F1}% win rate)\n{total} games played";
            }

            if (rankProgressBar != null)
            {
                float progress = (rating - rank.minRating) / (float)Mathf.Max(1, rank.maxRating - rank.minRating);
                rankProgressBar.value = Mathf.Clamp01(progress);
            }
        }

        private void RefreshSeason()
        {
            if (seasonText == null || RankedManager.Instance?.CurrentSeason == null) return;
            var season = RankedManager.Instance.CurrentSeason;
            seasonText.text = season.name;
        }

        private void RefreshLeaderboard()
        {
            ClearList(leaderboardEntries);
            if (RankedManager.Instance == null || leaderboardEntryPrefab == null || leaderboardContainer == null) return;

            foreach (var entry in RankedManager.Instance.Leaderboard)
            {
                var go = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                leaderboardEntries.Add(go);

                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1)
                    texts[0].text = $"#{entry.rank}  {entry.displayName}  [{entry.rating}]  {entry.rankInfo?.tier ?? ""}  {entry.wins}W/{entry.losses}L";
            }
        }

        private void RefreshHistory()
        {
            ClearList(historyEntries);
            if (RankedManager.Instance == null || historyEntryPrefab == null || historyContainer == null) return;

            foreach (var entry in RankedManager.Instance.MatchHistory)
            {
                var go = Instantiate(historyEntryPrefab, historyContainer);
                historyEntries.Add(go);

                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1)
                {
                    string changeStr = entry.ratingChange >= 0 ? $"+{entry.ratingChange}" : $"{entry.ratingChange}";
                    string date = "";
                    if (System.DateTime.TryParse(entry.timestamp, out var dt))
                        date = dt.ToString("MMM dd HH:mm");
                    texts[0].text = $"#{entry.placement}/{entry.playerCount}  {changeStr} MMR  [{entry.newRating}]  {date}";
                }
            }
        }

        /// <summary>T510: Show post-game ranked stats overlay.</summary>
        public void ShowPostGameStats(List<RankedManager.MatchResultEntry> results)
        {
            if (postGamePanel == null) return;

            string localPlayerId = GameAuthManager.Instance?.PlayerId ?? "";
            RankedManager.MatchResultEntry localResult = null;
            foreach (var r in results)
            {
                if (r.playerId == localPlayerId)
                {
                    localResult = r;
                    break;
                }
            }

            if (localResult == null) return;

            postGamePanel.SetActive(true);

            if (postGameRatingText != null) postGameRatingText.text = $"Rating: {localResult.newRating}";

            if (postGameChangeText != null)
            {
                string sign = localResult.ratingChange >= 0 ? "+" : "";
                postGameChangeText.text = $"{sign}{localResult.ratingChange}";
                postGameChangeText.color = localResult.ratingChange >= 0 ? Color.green : Color.red;
            }

            if (postGameRankText != null && localResult.rank != null)
            {
                postGameRankText.text = localResult.rank.tier;
                postGameRankText.color = localResult.rank.tierIndex < TIER_COLORS.Length
                    ? TIER_COLORS[localResult.rank.tierIndex] : Color.white;
            }
        }

        private void ClearList(List<GameObject> list)
        {
            foreach (var go in list)
                if (go != null) Destroy(go);
            list.Clear();
        }

        private void EnsureRankedManager()
        {
            if (RankedManager.Instance == null)
            {
                var go = new GameObject("RankedManager");
                go.AddComponent<RankedManager>();
            }
        }

        public bool IsOpen => rankedPanel != null && rankedPanel.activeSelf;

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (postGameCloseButton != null) postGameCloseButton.onClick.RemoveAllListeners();
            if (profileTabButton != null) profileTabButton.onClick.RemoveAllListeners();
            if (leaderboardTabButton != null) leaderboardTabButton.onClick.RemoveAllListeners();
            if (historyTabButton != null) historyTabButton.onClick.RemoveAllListeners();
        }
    }
}
