using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// T003: Singleton managing game player authentication.
/// Supports register, login, and guest modes.
/// Stores JWT in PlayerPrefs for persistence across sessions.
/// </summary>
public class GameAuthManager : MonoBehaviour
{
    public static GameAuthManager Instance { get; private set; }

    private const string PREF_TOKEN = "GameAuth_Token";
    private const string PREF_PLAYER_ID = "GameAuth_PlayerId";
    private const string PREF_DISPLAY_NAME = "GameAuth_DisplayName";
    private const string PREF_RATING = "GameAuth_Rating";

    public string Token { get; private set; }
    public string PlayerId { get; private set; }
    public string DisplayName { get; private set; }
    public int Rating { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnLogout;

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

        // Load cached auth
        Token = PlayerPrefs.GetString(PREF_TOKEN, "");
        PlayerId = PlayerPrefs.GetString(PREF_PLAYER_ID, "");
        DisplayName = PlayerPrefs.GetString(PREF_DISPLAY_NAME, "");
        Rating = PlayerPrefs.GetInt(PREF_RATING, 1000);

        // Get API URL from DataConfig
        var dataConfig = Resources.Load<DataConfig>("DataConfig");
        if (dataConfig == null)
            dataConfig = FindObjectOfType<RuntimeDataLoader>()?.dataConfig;

        if (dataConfig != null && !string.IsNullOrEmpty(dataConfig.apiBaseUrl))
        {
            apiBaseUrl = dataConfig.apiBaseUrl.TrimEnd('/');
        }
        else
        {
            apiBaseUrl = "";
            Debug.LogWarning("[GameAuth] No API URL configured in DataConfig. Auth features disabled.");
        }
    }

    private void Start()
    {
        // Try to validate cached token
        if (IsAuthenticated)
        {
            StartCoroutine(ValidateToken());
        }
    }

    /// <summary>Register a new account.</summary>
    public void Register(string email, string password, string displayName, Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            callback?.Invoke(false, "API not configured");
            return;
        }
        StartCoroutine(RegisterCoroutine(email, password, displayName, callback));
    }

    /// <summary>Login with email and password.</summary>
    public void Login(string email, string password, Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            callback?.Invoke(false, "API not configured");
            return;
        }
        StartCoroutine(LoginCoroutine(email, password, callback));
    }

    /// <summary>Login as a guest (anonymous player).</summary>
    public void LoginAsGuest(Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            // Offline guest mode — generate a local identity
            PlayerId = "local_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            DisplayName = "Guest" + UnityEngine.Random.Range(1000, 9999);
            Rating = 1000;
            Token = "offline";
            SaveAuth();
            GameConfig.PlayerName = DisplayName;
            OnLoginSuccess?.Invoke();
            callback?.Invoke(true, null);
            return;
        }
        StartCoroutine(GuestCoroutine(callback));
    }

    /// <summary>Logout and clear cached auth.</summary>
    public void Logout()
    {
        Token = "";
        PlayerId = "";
        DisplayName = "";
        Rating = 1000;
        PlayerPrefs.DeleteKey(PREF_TOKEN);
        PlayerPrefs.DeleteKey(PREF_PLAYER_ID);
        PlayerPrefs.DeleteKey(PREF_DISPLAY_NAME);
        PlayerPrefs.DeleteKey(PREF_RATING);
        PlayerPrefs.Save();
        OnLogout?.Invoke();
        Debug.Log("[GameAuth] Logged out");
    }

    private IEnumerator RegisterCoroutine(string email, string password, string displayName, Action<bool, string> callback)
    {
        var body = JsonConvert.SerializeObject(new { email, password, displayName });
        using var req = CreatePostRequest($"{apiBaseUrl}/game-auth/register", body);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = ParseError(req);
            Debug.LogWarning($"[GameAuth] Register failed: {error}");
            OnLoginFailed?.Invoke(error);
            callback?.Invoke(false, error);
            yield break;
        }

        HandleAuthResponse(req.downloadHandler.text);
        callback?.Invoke(true, null);
    }

    private IEnumerator LoginCoroutine(string email, string password, Action<bool, string> callback)
    {
        var body = JsonConvert.SerializeObject(new { email, password });
        using var req = CreatePostRequest($"{apiBaseUrl}/game-auth/login", body);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = ParseError(req);
            Debug.LogWarning($"[GameAuth] Login failed: {error}");
            OnLoginFailed?.Invoke(error);
            callback?.Invoke(false, error);
            yield break;
        }

        HandleAuthResponse(req.downloadHandler.text);
        callback?.Invoke(true, null);
    }

    private IEnumerator GuestCoroutine(Action<bool, string> callback)
    {
        using var req = CreatePostRequest($"{apiBaseUrl}/game-auth/guest", "{}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = ParseError(req);
            Debug.LogWarning($"[GameAuth] Guest login failed: {error}");
            OnLoginFailed?.Invoke(error);
            callback?.Invoke(false, error);
            yield break;
        }

        HandleAuthResponse(req.downloadHandler.text);
        callback?.Invoke(true, null);
    }

    private IEnumerator ValidateToken()
    {
        if (string.IsNullOrEmpty(apiBaseUrl) || Token == "offline")
        {
            OnLoginSuccess?.Invoke();
            yield break;
        }

        using var req = UnityWebRequest.Get($"{apiBaseUrl}/game-auth/profile");
        req.SetRequestHeader("Authorization", $"Bearer {Token}");
        req.timeout = 5;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("[GameAuth] Cached token invalid, clearing");
            Logout();
            yield break;
        }

        Debug.Log($"[GameAuth] Token valid for {DisplayName}");
        GameConfig.PlayerName = DisplayName;
        OnLoginSuccess?.Invoke();
    }

    private void HandleAuthResponse(string json)
    {
        var response = JsonConvert.DeserializeObject<AuthResponse>(json);
        if (response?.success == true && response.data != null)
        {
            Token = response.data.token;
            PlayerId = response.data.player.playerId;
            DisplayName = response.data.player.displayName;
            Rating = response.data.player.rating;
            SaveAuth();
            GameConfig.PlayerName = DisplayName;
            Debug.Log($"[GameAuth] Authenticated as {DisplayName} (rating: {Rating})");
            OnLoginSuccess?.Invoke();
        }
    }

    private void SaveAuth()
    {
        PlayerPrefs.SetString(PREF_TOKEN, Token);
        PlayerPrefs.SetString(PREF_PLAYER_ID, PlayerId);
        PlayerPrefs.SetString(PREF_DISPLAY_NAME, DisplayName);
        PlayerPrefs.SetInt(PREF_RATING, Rating);
        PlayerPrefs.Save();
    }

    private UnityWebRequest CreatePostRequest(string url, string body)
    {
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
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
    private class AuthResponse
    {
        public bool success;
        public AuthData data;
    }

    [Serializable]
    private class AuthData
    {
        public string token;
        public PlayerInfo player;
    }

    [Serializable]
    private class PlayerInfo
    {
        public string playerId;
        public string displayName;
        public int rating;
    }

    [Serializable]
    private class ErrorResponse
    {
        public bool success;
        public string error;
    }
}
