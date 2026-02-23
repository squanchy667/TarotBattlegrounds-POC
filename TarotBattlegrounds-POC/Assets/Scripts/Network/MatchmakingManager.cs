using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// T005: Manages matchmaking queue via DevZone API.
/// Polls for match status and auto-joins a Photon room when matched.
/// </summary>
public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    public enum QueueState { Idle, Joining, Waiting, Matched, Error }

    public QueueState State { get; private set; } = QueueState.Idle;
    public int WaitTimeSeconds { get; private set; }
    public int PlayersInQueue { get; private set; }
    public string MatchId { get; private set; }
    public string ErrorMessage { get; private set; }

    public event Action<QueueState> OnStateChanged;
    public event Action<string> OnMatchFound;

    private const float POLL_INTERVAL = 2f;
    private string apiBaseUrl;
    private Coroutine pollCoroutine;

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
        if (dataConfig == null)
            dataConfig = FindObjectOfType<RuntimeDataLoader>()?.dataConfig;

        if (dataConfig != null && !string.IsNullOrEmpty(dataConfig.apiBaseUrl))
            apiBaseUrl = dataConfig.apiBaseUrl.TrimEnd('/');
        else
            apiBaseUrl = "";
    }

    /// <summary>Join the matchmaking queue.</summary>
    public void JoinQueue()
    {
        if (State == QueueState.Waiting || State == QueueState.Joining)
        {
            Debug.Log("[Matchmaking] Already in queue");
            return;
        }

        if (!GameAuthManager.Instance?.IsAuthenticated ?? true)
        {
            SetState(QueueState.Error);
            ErrorMessage = "Not authenticated";
            return;
        }

        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            SetState(QueueState.Error);
            ErrorMessage = "API not configured";
            return;
        }

        StartCoroutine(JoinQueueCoroutine());
    }

    /// <summary>Cancel matchmaking.</summary>
    public void CancelQueue()
    {
        if (State != QueueState.Waiting && State != QueueState.Joining) return;

        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }

        StartCoroutine(CancelQueueCoroutine());
    }

    private IEnumerator JoinQueueCoroutine()
    {
        SetState(QueueState.Joining);

        using var req = CreateAuthRequest($"{apiBaseUrl}/matchmaking/join", "POST", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ErrorMessage = ParseError(req);
            SetState(QueueState.Error);
            yield break;
        }

        var response = JsonConvert.DeserializeObject<MatchmakingResponse>(req.downloadHandler.text);
        if (response?.data?.status == "matched")
        {
            MatchId = response.data.matchId;
            SetState(QueueState.Matched);
            OnMatchFound?.Invoke(MatchId);
            JoinPhotonRoom(MatchId);
            yield break;
        }

        SetState(QueueState.Waiting);
        pollCoroutine = StartCoroutine(PollMatchStatus());
    }

    private IEnumerator PollMatchStatus()
    {
        while (State == QueueState.Waiting)
        {
            yield return new WaitForSeconds(POLL_INTERVAL);

            using var req = CreateAuthRequest($"{apiBaseUrl}/matchmaking/status", "GET");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Matchmaking] Poll failed: {req.error}");
                continue;
            }

            var response = JsonConvert.DeserializeObject<MatchmakingResponse>(req.downloadHandler.text);
            if (response?.data == null) continue;

            if (response.data.status == "matched")
            {
                MatchId = response.data.matchId;
                SetState(QueueState.Matched);
                OnMatchFound?.Invoke(MatchId);
                JoinPhotonRoom(MatchId);
                yield break;
            }

            if (response.data.status == "not_in_queue")
            {
                SetState(QueueState.Idle);
                yield break;
            }

            WaitTimeSeconds = response.data.waitTimeSeconds;
            PlayersInQueue = response.data.playersInQueue;
        }
    }

    private IEnumerator CancelQueueCoroutine()
    {
        using var req = CreateAuthRequest($"{apiBaseUrl}/matchmaking/cancel", "DELETE");
        yield return req.SendWebRequest();

        SetState(QueueState.Idle);
        Debug.Log("[Matchmaking] Left queue");
    }

    private void JoinPhotonRoom(string matchId)
    {
#if PHOTON_UNITY_NETWORKING
        Debug.Log($"[Matchmaking] Match found: {matchId}. Joining Photon room...");
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        GameConfig.Save();

        // Navigate to lobby scene — the lobby will handle Photon room joining
        // Store matchId so LobbyUI can join the correct room
        PlayerPrefs.SetString("Matchmaking_MatchId", matchId);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Lobby");
#else
        Debug.LogWarning("[Matchmaking] Photon not available");
#endif
    }

    private void SetState(QueueState newState)
    {
        State = newState;
        // M10 fix: Clean up stale matchId when returning to idle or error
        if (newState == QueueState.Idle || newState == QueueState.Error)
        {
            MatchId = null;
            PlayerPrefs.DeleteKey("Matchmaking_MatchId");
        }
        OnStateChanged?.Invoke(newState);
    }

    private UnityWebRequest CreateAuthRequest(string url, string method, string body = null)
    {
        UnityWebRequest req;
        if (method == "GET")
        {
            req = UnityWebRequest.Get(url);
        }
        else if (method == "DELETE")
        {
            req = UnityWebRequest.Delete(url);
            req.downloadHandler = new DownloadHandlerBuffer();
        }
        else
        {
            req = new UnityWebRequest(url, method);
            if (body != null)
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
        }

        string token = GameAuthManager.Instance?.Token;
        if (!string.IsNullOrEmpty(token) && token != "offline")
            req.SetRequestHeader("Authorization", $"Bearer {token}");

        req.timeout = 10;
        return req;
    }

    private string ParseError(UnityWebRequest req)
    {
        try
        {
            var err = JsonConvert.DeserializeObject<ErrorResponse>(req.downloadHandler.text);
            return err?.error ?? req.error;
        }
        catch
        {
            return req.error;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Serializable]
    private class MatchmakingResponse
    {
        public bool success;
        public MatchData data;
    }

    [Serializable]
    private class MatchData
    {
        public string status;
        public string matchId;
        public int waitTimeSeconds;
        public int playersInQueue;
    }

    [Serializable]
    private class ErrorResponse
    {
        public bool success;
        public string error;
    }
}
