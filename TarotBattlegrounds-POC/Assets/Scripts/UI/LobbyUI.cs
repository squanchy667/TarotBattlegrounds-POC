#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// UI controller for the Lobby scene.
/// Manages connection, room browser, room interior, and game start.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Connection Panel")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_InputField playerNameInput;

    [Header("Room Browser Panel")]
    [SerializeField] private GameObject roomBrowserPanel;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRandomButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomListEntryPrefab;
    [SerializeField] private TMP_Text noRoomsText;

    [Header("Room Interior Panel")]
    [SerializeField] private GameObject roomInteriorPanel;
    [SerializeField] private TMP_Text roomTitleText;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TMP_Text roomStatusText;

    private List<GameObject> roomListEntries = new List<GameObject>();

    private void Start()
    {
        // Being in the Lobby means we're in multiplayer mode — ensure GameConfig reflects this
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        GameConfig.Save();

        // Start all panels hidden
        SetActivePanel(PanelState.Connecting);

        // Setup button listeners
        if (createRoomButton != null) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRandomButton != null) joinRandomButton.onClick.AddListener(OnJoinRandomClicked);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (leaveRoomButton != null) leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // Setup max players dropdown
        if (maxPlayersDropdown != null)
        {
            maxPlayersDropdown.ClearOptions();
            maxPlayersDropdown.AddOptions(new List<string> { "4 Players", "6 Players", "8 Players" });
            maxPlayersDropdown.value = 0;
        }

        // Load player name
        if (playerNameInput != null)
        {
            playerNameInput.text = GameConfig.PlayerName;
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }

        // Ensure PhotonConnector exists
        EnsurePhotonConnector();

        // Subscribe to events
        SubscribeEvents();

        // Connect if not already connected
        if (!PhotonNetwork.IsConnected)
        {
            Connect();
        }
        else if (PhotonNetwork.InRoom)
        {
            // Returning from game
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SetActivePanel(PanelState.RoomBrowser);
        }
    }

    private void EnsurePhotonConnector()
    {
        if (PhotonConnector.Instance == null)
        {
            GameObject connectorObj = new GameObject("PhotonConnector");
            connectorObj.AddComponent<PhotonConnector>();
        }
        if (RoomManager.Instance == null)
        {
            GameObject roomMgrObj = new GameObject("RoomManager");
            roomMgrObj.AddComponent<RoomManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnConnectedToMasterEvent += OnConnectedToMaster;
            PhotonConnector.Instance.OnJoinedRoomEvent += OnJoinedRoom;
            PhotonConnector.Instance.OnLeftRoomEvent += OnLeftRoom;
            PhotonConnector.Instance.OnPlayerJoinedRoomEvent += OnPlayerUpdated;
            PhotonConnector.Instance.OnPlayerLeftRoomEvent += OnPlayerUpdated;
            PhotonConnector.Instance.OnDisconnectedEvent += OnDisconnected;
            PhotonConnector.Instance.OnJoinRoomFailedEvent += OnJoinRoomFailed;
        }
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomListUpdatedEvent += OnRoomListUpdated;
        }
    }

    private void UnsubscribeEvents()
    {
        if (PhotonConnector.Instance != null)
        {
            PhotonConnector.Instance.OnConnectedToMasterEvent -= OnConnectedToMaster;
            PhotonConnector.Instance.OnJoinedRoomEvent -= OnJoinedRoom;
            PhotonConnector.Instance.OnLeftRoomEvent -= OnLeftRoom;
            PhotonConnector.Instance.OnPlayerJoinedRoomEvent -= OnPlayerUpdated;
            PhotonConnector.Instance.OnPlayerLeftRoomEvent -= OnPlayerUpdated;
            PhotonConnector.Instance.OnDisconnectedEvent -= OnDisconnected;
            PhotonConnector.Instance.OnJoinRoomFailedEvent -= OnJoinRoomFailed;
        }
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomListUpdatedEvent -= OnRoomListUpdated;
        }
    }

    private void Connect()
    {
        SetActivePanel(PanelState.Connecting);
        if (connectionStatusText != null) connectionStatusText.text = "Connecting to server...";
        PhotonConnector.Instance.Connect();
    }

    // === Panel Management ===

    private enum PanelState { Connecting, RoomBrowser, RoomInterior }

    private void SetActivePanel(PanelState state)
    {
        if (connectionPanel != null) connectionPanel.SetActive(state == PanelState.Connecting);
        if (roomBrowserPanel != null) roomBrowserPanel.SetActive(state == PanelState.RoomBrowser);
        if (roomInteriorPanel != null) roomInteriorPanel.SetActive(state == PanelState.RoomInterior);
    }

    // === Event Handlers ===

    private void OnConnectedToMaster()
    {
        SetActivePanel(PanelState.RoomBrowser);
    }

    private void OnJoinedRoom()
    {
        SetActivePanel(PanelState.RoomInterior);
        UpdateRoomInterior();
    }

    private void OnLeftRoom()
    {
        SetActivePanel(PanelState.RoomBrowser);
    }

    private void OnPlayerUpdated(Photon.Realtime.Player player)
    {
        UpdateRoomInterior();
    }

    private void OnDisconnected(DisconnectCause cause)
    {
        SetActivePanel(PanelState.Connecting);
        if (connectionStatusText != null) connectionStatusText.text = $"Disconnected: {cause}\nReconnecting...";

        // Auto-reconnect after a short delay
        Invoke(nameof(Connect), 2f);
    }

    private void OnJoinRoomFailed()
    {
        if (roomStatusText != null) roomStatusText.text = "Failed to join room.";
        SetActivePanel(PanelState.RoomBrowser);
    }

    private void OnRoomListUpdated(List<RoomInfo> rooms)
    {
        RefreshRoomList(rooms);
    }

    // === Button Handlers ===

    private void OnCreateRoomClicked()
    {
        string roomName = roomNameInput != null ? roomNameInput.text : "";
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = PhotonNetwork.NickName + "'s Room";
        }

        int[] playerOptions = { 4, 6, 8 };
        byte maxPlayers = (byte)(maxPlayersDropdown != null ? playerOptions[maxPlayersDropdown.value] : 4);
        RoomManager.Instance.CreateRoom(roomName, maxPlayers);
    }

    private void OnJoinRandomClicked()
    {
        RoomManager.Instance.JoinRandomRoom();
    }

    private void OnBackToMenuClicked()
    {
        UnsubscribeEvents();
        PhotonConnector.Instance.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    private void OnStartGameClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[LobbyUI] Only the host can start the game.");
            return;
        }

        RoomManager.Instance.StartGame();
    }

    private void OnLeaveRoomClicked()
    {
        RoomManager.Instance.LeaveRoom();
    }

    private void OnPlayerNameChanged(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            GameConfig.PlayerName = newName;
            PhotonNetwork.NickName = newName;
        }
    }

    // === Room Interior ===

    private void UpdateRoomInterior()
    {
        if (!PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;

        if (roomTitleText != null)
            roomTitleText.text = room.Name;

        // Player list
        if (playerListText != null)
        {
            string playerList = "";
            int index = 1;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                string host = player.IsMasterClient ? " (Host)" : "";
                string local = player.IsLocal ? " (You)" : "";
                playerList += $"{index}. {player.NickName}{host}{local}\n";
                index++;
            }

            // Show empty AI slots
            for (int i = index; i <= room.MaxPlayers; i++)
            {
                playerList += $"{i}. [AI]\n";
            }

            playerListText.text = playerList;
        }

        // Status text
        if (roomStatusText != null)
        {
            roomStatusText.text = $"{room.PlayerCount}/{room.MaxPlayers} players. " +
                (PhotonNetwork.IsMasterClient ? "Press Start when ready." : "Waiting for host...");
        }

        // Start button only visible to host, enabled when at least 1 player
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startGameButton.interactable = room.PlayerCount >= 1;
        }
    }

    // === Room List ===

    private void RefreshRoomList(List<RoomInfo> rooms)
    {
        // Clear existing entries
        foreach (var entry in roomListEntries)
        {
            if (entry != null) Destroy(entry);
        }
        roomListEntries.Clear();

        if (noRoomsText != null)
            noRoomsText.gameObject.SetActive(rooms.Count == 0);

        foreach (var room in rooms)
        {
            if (!room.IsOpen || !room.IsVisible) continue;

            // Check if game already started
            if (room.CustomProperties.ContainsKey(RoomManager.PROP_GAME_STARTED))
            {
                bool started = (bool)room.CustomProperties[RoomManager.PROP_GAME_STARTED];
                if (started) continue;
            }

            CreateRoomListEntry(room);
        }
    }

    private void CreateRoomListEntry(RoomInfo room)
    {
        if (roomListEntryPrefab == null || roomListContainer == null) return;

        GameObject entry = Instantiate(roomListEntryPrefab, roomListContainer);
        roomListEntries.Add(entry);

        // Try to set up the entry text and button
        TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>();
        if (entryText != null)
        {
            string hostName = "";
            if (room.CustomProperties.ContainsKey(RoomManager.PROP_HOST_NAME))
                hostName = (string)room.CustomProperties[RoomManager.PROP_HOST_NAME];

            entryText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers}) - Host: {hostName}";
        }

        Button entryButton = entry.GetComponent<Button>();
        if (entryButton == null)
            entryButton = entry.GetComponentInChildren<Button>();

        if (entryButton != null)
        {
            string roomName = room.Name;
            entryButton.onClick.AddListener(() => RoomManager.Instance.JoinRoom(roomName));
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (createRoomButton != null) createRoomButton.onClick.RemoveAllListeners();
        if (joinRandomButton != null) joinRandomButton.onClick.RemoveAllListeners();
        if (backToMenuButton != null) backToMenuButton.onClick.RemoveAllListeners();
        if (startGameButton != null) startGameButton.onClick.RemoveAllListeners();
        if (leaveRoomButton != null) leaveRoomButton.onClick.RemoveAllListeners();
    }
}
#endif
