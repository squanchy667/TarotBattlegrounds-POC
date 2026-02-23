using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// T503/T506/T514: Manages ranked data, MMR, match history, leaderboard.
/// Calls DevZone API ranked endpoints.
/// </summary>
public class RankedManager : MonoBehaviour
{
    public static RankedManager Instance { get; private set; }

    public RankInfo CurrentRank { get; private set; }
    public int CurrentRating { get; private set; }
    public int GamesPlayed { get; private set; }
    public int Wins { get; private set; }
    public int Losses { get; private set; }

    public List<MatchHistoryEntry> MatchHistory { get; private set; } = new List<MatchHistoryEntry>();
    public List<LeaderboardEntry> Leaderboard { get; private set; } = new List<LeaderboardEntry>();
    public SeasonInfo CurrentSeason { get; private set; }

    public event Action OnProfileUpdated;
    public event Action OnLeaderboardUpdated;
    public event Action OnMatchHistoryUpdated;
    public event Action OnSeasonUpdated;
    public event Action<List<MatchResultEntry>> OnMatchResultProcessed;

    private string apiBaseUrl;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var dataConfig = Resources.Load<DataConfig>("DataConfig");
        if (dataConfig != null && !string.IsNullOrEmpty(dataConfig.apiBaseUrl))
            apiBaseUrl = dataConfig.apiBaseUrl.TrimEnd('/');
        else
            apiBaseUrl = "";
    }

    public void FetchProfile() => StartCoroutine(FetchProfileCoroutine());
    public void FetchLeaderboard() => StartCoroutine(FetchLeaderboardCoroutine());
    public void FetchMatchHistory() => StartCoroutine(FetchMatchHistoryCoroutine());
    public void FetchSeason() => StartCoroutine(FetchSeasonCoroutine());

    public void SubmitMatchResult(string matchId, List<PlacementData> placements)
        => StartCoroutine(SubmitMatchResultCoroutine(matchId, placements));

    private IEnumerator FetchProfileCoroutine()
    {
        if (string.IsNullOrEmpty(apiBaseUrl)) yield break;

        using var req = CreateAuthGet($"{apiBaseUrl}/ranked/profile");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<ProfileResponse>(req.downloadHandler.text);
            if (resp?.success == true && resp.data != null)
            {
                CurrentRating = resp.data.rating;
                GamesPlayed = resp.data.gamesPlayed;
                Wins = resp.data.wins;
                Losses = resp.data.losses;
                CurrentRank = resp.data.rank;
                OnProfileUpdated?.Invoke();
            }
        }
    }

    private IEnumerator FetchLeaderboardCoroutine()
    {
        if (string.IsNullOrEmpty(apiBaseUrl)) yield break;

        using var req = UnityWebRequest.Get($"{apiBaseUrl}/ranked/leaderboard");
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<LeaderboardResponse>(req.downloadHandler.text);
            if (resp?.success == true && resp.data != null)
            {
                Leaderboard = resp.data;
                OnLeaderboardUpdated?.Invoke();
            }
        }
    }

    private IEnumerator FetchMatchHistoryCoroutine()
    {
        if (string.IsNullOrEmpty(apiBaseUrl)) yield break;

        using var req = CreateAuthGet($"{apiBaseUrl}/ranked/match-history");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<MatchHistoryResponse>(req.downloadHandler.text);
            if (resp?.success == true && resp.data != null)
            {
                MatchHistory = resp.data;
                OnMatchHistoryUpdated?.Invoke();
            }
        }
    }

    private IEnumerator FetchSeasonCoroutine()
    {
        if (string.IsNullOrEmpty(apiBaseUrl)) yield break;

        using var req = UnityWebRequest.Get($"{apiBaseUrl}/ranked/season");
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<SeasonResponse>(req.downloadHandler.text);
            if (resp?.success == true && resp.data != null)
            {
                CurrentSeason = resp.data;
                OnSeasonUpdated?.Invoke();
            }
        }
    }

    private IEnumerator SubmitMatchResultCoroutine(string matchId, List<PlacementData> placements)
    {
        if (string.IsNullOrEmpty(apiBaseUrl)) yield break;

        var body = JsonConvert.SerializeObject(new { matchId, placements });
        var req = new UnityWebRequest($"{apiBaseUrl}/ranked/match-result", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        string token = GameAuthManager.Instance?.Token;
        if (!string.IsNullOrEmpty(token) && token != "offline")
            req.SetRequestHeader("Authorization", $"Bearer {token}");
        req.timeout = 10;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonConvert.DeserializeObject<MatchResultResponse>(req.downloadHandler.text);
            if (resp?.success == true && resp.data != null)
            {
                OnMatchResultProcessed?.Invoke(resp.data);
                FetchProfile(); // refresh profile with new rating
            }
        }

        req.Dispose();
    }

    private UnityWebRequest CreateAuthGet(string url)
    {
        var req = UnityWebRequest.Get(url);
        string token = GameAuthManager.Instance?.Token;
        if (!string.IsNullOrEmpty(token) && token != "offline")
            req.SetRequestHeader("Authorization", $"Bearer {token}");
        req.timeout = 10;
        return req;
    }

    /// <summary>Get rank info from rating (client-side mirror of server logic).</summary>
    public static RankInfo GetRankFromRating(int rating)
    {
        var tiers = new[] {
            ("Bronze",   0,    999),
            ("Silver",   1000, 1499),
            ("Gold",     1500, 1999),
            ("Platinum", 2000, 2499),
            ("Diamond",  2500, 2999),
            ("Master",   3000, 3499),
            ("Legend",   3500, 99999),
        };

        for (int i = tiers.Length - 1; i >= 0; i--)
        {
            if (rating >= tiers[i].Item2)
            {
                int range = tiers[i].Item3 - tiers[i].Item2;
                int divSize = Mathf.Max(1, range / 4);
                int division = tiers[i].Item1 == "Legend" ? 0 :
                    Mathf.Min(4, (rating - tiers[i].Item2) / divSize + 1);

                return new RankInfo
                {
                    tier = tiers[i].Item1,
                    division = division,
                    tierIndex = i,
                    minRating = tiers[i].Item2,
                    maxRating = tiers[i].Item3,
                };
            }
        }
        return new RankInfo { tier = "Bronze", division = 1, tierIndex = 0 };
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Response types
    [Serializable] public class RankInfo { public string tier; public int division; public int tierIndex; public int minRating; public int maxRating; }
    [Serializable] public class LeaderboardEntry { public int rank; public string playerId; public string displayName; public int rating; public int gamesPlayed; public int wins; public int losses; public RankInfo rankInfo; }
    [Serializable] public class MatchHistoryEntry { public string matchId; public string timestamp; public int placement; public int playerCount; public int ratingChange; public int newRating; }
    [Serializable] public class SeasonInfo { public string seasonId; public string name; public string startDate; public string endDate; public bool isActive; }
    [Serializable] public class PlacementData { public string playerId; public int placement; public int playerCount; }
    [Serializable] public class MatchResultEntry { public string playerId; public int oldRating; public int newRating; public int ratingChange; public RankInfo rank; }

    [Serializable] private class ProfileResponse { public bool success; public ProfileData data; }
    [Serializable] private class ProfileData { public int rating; public int gamesPlayed; public int wins; public int losses; public RankInfo rank; }
    [Serializable] private class LeaderboardResponse { public bool success; public List<LeaderboardEntry> data; }
    [Serializable] private class MatchHistoryResponse { public bool success; public List<MatchHistoryEntry> data; }
    [Serializable] private class SeasonResponse { public bool success; public SeasonInfo data; }
    [Serializable] private class MatchResultResponse { public bool success; public List<MatchResultEntry> data; }
}
