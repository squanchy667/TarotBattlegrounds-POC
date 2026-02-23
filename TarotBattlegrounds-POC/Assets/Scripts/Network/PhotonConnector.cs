#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using ExitGames.Client.Photon;

/// <summary>
/// Singleton that manages Photon connection lifecycle.
/// T006: Extended with player identity (custom properties) and reconnection.
/// DontDestroyOnLoad — persists across scenes.
/// </summary>
public class PhotonConnector : MonoBehaviourPunCallbacks
{
    public static PhotonConnector Instance { get; private set; }

    // T006: Custom property keys for player identity
    public const string PROP_PLAYER_ID = "pid";
    public const string PROP_DISPLAY_NAME = "dname";
    public const string PROP_RATING = "rating";

    public event Action OnConnectedToMasterEvent;
    public event Action OnJoinedLobbyEvent;
    public event Action<Photon.Realtime.Player> OnPlayerJoinedRoomEvent;
    public event Action<Photon.Realtime.Player> OnPlayerLeftRoomEvent;
    public event Action OnJoinedRoomEvent;
    public event Action OnLeftRoomEvent;
    public event Action<DisconnectCause> OnDisconnectedEvent;
    public event Action OnJoinRoomFailedEvent;
    public event Action OnReconnectedEvent;

    public bool IsConnectedToMaster => PhotonNetwork.IsConnectedAndReady;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public bool IsReconnecting { get; private set; }

    private const float RECONNECT_TIMEOUT = 60f;
    private float reconnectTimer;
    private bool wasInRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        PhotonNetwork.PhotonServerSettings.AppSettings.Protocol =
            ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
#endif

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// Connect to Photon servers using settings from PhotonServerSettings asset.
    /// T006: Sets custom properties with player identity from GameAuthManager.
    /// </summary>
    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[PhotonConnector] Already connected.");
            return;
        }

        string playerName = GameConfig.PlayerName;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Player" + UnityEngine.Random.Range(1000, 9999);
            GameConfig.PlayerName = playerName;
        }

#if UNITY_EDITOR
        // ParrelSync clones have "_clone_" in their project path and share PlayerPrefs
        if (Application.dataPath.Contains("_clone_"))
        {
            playerName = "Clone" + Mathf.Abs(System.Guid.NewGuid().GetHashCode() % 9000 + 1000);
        }
#endif

        PhotonNetwork.NickName = playerName;

        // T006: Allow room rejoin for reconnection
        PhotonNetwork.PhotonServerSettings.AppSettings.PlayerTtl = (int)(RECONNECT_TIMEOUT * 1000);

        Debug.Log($"[PhotonConnector] Connecting as '{playerName}'...");
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// T006: Set player identity as Photon custom properties.
    /// Called after joining a room.
    /// </summary>
    public void SetPlayerIdentity()
    {
        if (!PhotonNetwork.InRoom) return;

        var props = new Hashtable();

        if (GameAuthManager.Instance != null && GameAuthManager.Instance.IsAuthenticated)
        {
            props[PROP_PLAYER_ID] = GameAuthManager.Instance.PlayerId;
            props[PROP_DISPLAY_NAME] = GameAuthManager.Instance.DisplayName;
            props[PROP_RATING] = GameAuthManager.Instance.Rating;
        }
        else
        {
            props[PROP_PLAYER_ID] = "";
            props[PROP_DISPLAY_NAME] = PhotonNetwork.NickName;
            props[PROP_RATING] = 1000;
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[PhotonConnector] Set identity: {props[PROP_DISPLAY_NAME]} (rating: {props[PROP_RATING]})");
    }

    /// <summary>
    /// T006: Get display name for a Photon player (from custom props or NickName).
    /// </summary>
    public static string GetPlayerDisplayName(Photon.Realtime.Player player)
    {
        if (player.CustomProperties.TryGetValue(PROP_DISPLAY_NAME, out object name) && name is string sName && !string.IsNullOrEmpty(sName))
            return sName;
        return player.NickName;
    }

    /// <summary>
    /// T006: Get rating for a Photon player.
    /// </summary>
    public static int GetPlayerRating(Photon.Realtime.Player player)
    {
        if (player.CustomProperties.TryGetValue(PROP_RATING, out object rating) && rating is int iRating)
            return iRating;
        return 1000;
    }

    /// <summary>
    /// T006: Attempt to reconnect and rejoin the last room.
    /// </summary>
    public void TryReconnect()
    {
        if (IsReconnecting) return;
        IsReconnecting = true;
        reconnectTimer = RECONNECT_TIMEOUT;
        Debug.Log("[PhotonConnector] Attempting reconnection...");
        PhotonNetwork.ReconnectAndRejoin();
    }

    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    // === Photon Callbacks ===

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonConnector] Connected to Master server.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonConnector] Joined lobby.");
        OnJoinedLobbyEvent?.Invoke();
        OnConnectedToMasterEvent?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PhotonConnector] Disconnected: {cause}");

        // T006: Auto-reconnect if we were in a room and it was unexpected
        if (wasInRoom && cause != DisconnectCause.DisconnectByClientLogic &&
            cause != DisconnectCause.DisconnectByServerLogic)
        {
            TryReconnect();
        }

        OnDisconnectedEvent?.Invoke(cause);
    }

    public override void OnJoinedRoom()
    {
        wasInRoom = true;
        IsReconnecting = false;

        Debug.Log($"[PhotonConnector] Joined room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} players)");

        // T006: Set player identity on room join
        SetPlayerIdentity();

        OnJoinedRoomEvent?.Invoke();

        if (IsReconnecting)
        {
            IsReconnecting = false;
            OnReconnectedEvent?.Invoke();
        }
    }

    public override void OnLeftRoom()
    {
        wasInRoom = false;
        Debug.Log("[PhotonConnector] Left room.");
        OnLeftRoomEvent?.Invoke();
    }

    private void Update()
    {
        if (IsReconnecting)
        {
            reconnectTimer -= Time.deltaTime;
            if (reconnectTimer <= 0f)
            {
                IsReconnecting = false;
                Debug.LogWarning("[PhotonConnector] Reconnection timed out.");
            }
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"[PhotonConnector] Player joined: {newPlayer.NickName} (Actor {newPlayer.ActorNumber})");
        OnPlayerJoinedRoomEvent?.Invoke(newPlayer);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"[PhotonConnector] Player left: {otherPlayer.NickName} (Actor {otherPlayer.ActorNumber})");
        OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonConnector] Join room failed: {message}");
        OnJoinRoomFailedEvent?.Invoke();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonConnector] Join random room failed: {message}");
        OnJoinRoomFailedEvent?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
#endif
