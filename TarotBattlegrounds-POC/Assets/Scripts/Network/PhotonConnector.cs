using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

/// <summary>
/// Singleton that manages Photon connection lifecycle.
/// DontDestroyOnLoad — persists across scenes.
/// </summary>
public class PhotonConnector : MonoBehaviourPunCallbacks
{
    public static PhotonConnector Instance { get; private set; }

    public event Action OnConnectedToMasterEvent;
    public event Action OnJoinedLobbyEvent;
    public event Action<Photon.Realtime.Player> OnPlayerJoinedRoomEvent;
    public event Action<Photon.Realtime.Player> OnPlayerLeftRoomEvent;
    public event Action OnJoinedRoomEvent;
    public event Action OnLeftRoomEvent;
    public event Action<DisconnectCause> OnDisconnectedEvent;
    public event Action OnJoinRoomFailedEvent;

    public bool IsConnectedToMaster => PhotonNetwork.IsConnectedAndReady;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// Connect to Photon servers using settings from PhotonServerSettings asset.
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
        PhotonNetwork.NickName = playerName;

        Debug.Log($"[PhotonConnector] Connecting as '{playerName}'...");
        PhotonNetwork.ConnectUsingSettings();
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
        OnDisconnectedEvent?.Invoke(cause);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonConnector] Joined room: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount} players)");
        OnJoinedRoomEvent?.Invoke();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonConnector] Left room.");
        OnLeftRoomEvent?.Invoke();
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
