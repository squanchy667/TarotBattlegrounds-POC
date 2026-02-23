#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using ExitGames.Client.Photon;

/// <summary>
/// Manages Photon room creation, joining, and game start.
/// Works alongside PhotonConnector.
/// </summary>
public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance { get; private set; }

    public const string PROP_GAME_STARTED = "gameStarted";
    public const string PROP_HOST_NAME = "hostName";
    public const string PROP_PLAYER_COUNT = "playerCount";

    public event System.Action<List<RoomInfo>> OnRoomListUpdatedEvent;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Create a room with the given name and max players (2-8).
    /// </summary>
    public void CreateRoom(string roomName, byte maxPlayers)
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[RoomManager] Cannot create room: not ready.");
            return;
        }
        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 8);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = new Hashtable
            {
                { PROP_GAME_STARTED, false },
                { PROP_HOST_NAME, PhotonNetwork.NickName },
                { PROP_PLAYER_COUNT, (int)maxPlayers }
            },
            CustomRoomPropertiesForLobby = new string[]
            {
                PROP_GAME_STARTED,
                PROP_HOST_NAME,
                PROP_PLAYER_COUNT
            }
        };

        Debug.Log($"[RoomManager] Creating room '{roomName}' for {maxPlayers} players...");
        PhotonNetwork.CreateRoom(roomName, options);
    }

    /// <summary>
    /// Join a specific room by name.
    /// </summary>
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[RoomManager] Cannot join room '{roomName}': not ready (State: {PhotonNetwork.NetworkClientState})");
            return;
        }
        Debug.Log($"[RoomManager] Joining room '{roomName}'...");
        PhotonNetwork.JoinRoom(roomName);
    }

    /// <summary>
    /// T005: Join an existing room or create it if it doesn't exist.
    /// Used for matchmaking-assigned rooms.
    /// </summary>
    public void JoinOrCreateRoom(string roomName, byte maxPlayers)
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            Debug.LogWarning($"[RoomManager] Cannot join/create room '{roomName}': not ready.");
            return;
        }

        maxPlayers = (byte)Mathf.Clamp(maxPlayers, 2, 8);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = false, // matchmaking rooms are not listed
            IsOpen = true,
            PlayerTtl = 60000, // T006: 60s reconnection window
            CustomRoomProperties = new Hashtable
            {
                { PROP_GAME_STARTED, false },
                { PROP_HOST_NAME, PhotonNetwork.NickName },
                { PROP_PLAYER_COUNT, (int)maxPlayers }
            },
            CustomRoomPropertiesForLobby = new string[]
            {
                PROP_GAME_STARTED, PROP_HOST_NAME, PROP_PLAYER_COUNT
            }
        };

        Debug.Log($"[RoomManager] Joining or creating matchmaking room '{roomName}'...");
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    /// <summary>
    /// Join a random available room.
    /// </summary>
    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[RoomManager] Cannot join random room: not ready.");
            return;
        }
        Debug.Log("[RoomManager] Joining random room...");
        PhotonNetwork.JoinRandomRoom();
    }

    /// <summary>
    /// Leave the current room.
    /// </summary>
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[RoomManager] Leaving room...");
            PhotonNetwork.LeaveRoom();
        }
    }

    /// <summary>
    /// Start the game (host only). Closes the room and loads the Game scene.
    /// </summary>
    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[RoomManager] Only the host can start the game.");
            return;
        }

        // Close room so no one else can join
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Mark game as started in room properties
        Hashtable props = new Hashtable { { PROP_GAME_STARTED, true } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // Store player count in GameConfig
        GameConfig.PlayerCount = PhotonNetwork.CurrentRoom.MaxPlayers;
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        GameConfig.Save();

        Debug.Log($"[RoomManager] Starting game with {PhotonNetwork.CurrentRoom.PlayerCount} human players, {PhotonNetwork.CurrentRoom.MaxPlayers} total slots.");

        // AutomaticallySyncScene is true, so all clients load together
        PhotonNetwork.LoadLevel("Game");
    }

    /// <summary>
    /// Get the list of players in the current room.
    /// </summary>
    public Photon.Realtime.Player[] GetRoomPlayers()
    {
        if (!PhotonNetwork.InRoom) return new Photon.Realtime.Player[0];
        return PhotonNetwork.PlayerList;
    }

    /// <summary>
    /// Get the cached room list.
    /// </summary>
    public List<RoomInfo> GetRoomList()
    {
        return cachedRoomList;
    }

    // === Photon Callbacks ===

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Update cached list: remove closed rooms, add/update open ones
        foreach (var room in roomList)
        {
            int existingIdx = cachedRoomList.FindIndex(r => r.Name == room.Name);

            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible)
            {
                if (existingIdx >= 0)
                    cachedRoomList.RemoveAt(existingIdx);
            }
            else
            {
                if (existingIdx >= 0)
                    cachedRoomList[existingIdx] = room;
                else
                    cachedRoomList.Add(room);
            }
        }

        OnRoomListUpdatedEvent?.Invoke(cachedRoomList);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[RoomManager] Create room failed: {message}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
#endif
