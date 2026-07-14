#if PHOTON_UNITY_NETWORKING
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

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
    // Optional phase caption on the connection card ("OPENING THE GATE")
    [SerializeField] private TMP_Text connectionPhaseCaption;

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
    [SerializeField] private TMP_Text playerListText; // legacy fallback
    [SerializeField] private Transform playerSlotContainer; // T750 §8 slot stack
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TMP_Text roomStatusText;

    [Header("T006: Reconnection")]
    [SerializeField] private GameObject reconnectingOverlay;
    [SerializeField] private TMP_Text reconnectingText;

    private List<GameObject> roomListEntries = new List<GameObject>();
    private readonly List<GameObject> playerSlotInstances = new List<GameObject>();
    private bool roomTitleCopyWired;

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

        // Connection-panel escape hatch (built by LobbyRestyleSetup)
        WireConnectionBackButton();

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

        // T006: Hide reconnection overlay
        if (reconnectingOverlay != null) reconnectingOverlay.SetActive(false);

        // T750: room code tap-to-copy (room title)
        WireRoomTitleCopy();

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

        // T005: Check if we arrived via matchmaking
        string matchId = PlayerPrefs.GetString("Matchmaking_MatchId", "");
        if (!string.IsNullOrEmpty(matchId))
        {
            PlayerPrefs.DeleteKey("Matchmaking_MatchId");
            PlayerPrefs.Save();
            pendingMatchId = matchId;
        }
    }

    private string pendingMatchId;

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

    private void WireConnectionBackButton()
    {
        if (connectionPanel == null) return;
        // Include inactive children — panel may already be toggled
        Button[] buttons = connectionPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || buttons[i].gameObject.name != "ConnectionBackButton")
                continue;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(OnBackToMenuClicked);
            // Ensure the hit target can receive clicks above the video
            var img = buttons[i].GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            return;
        }
        Debug.LogWarning("[LobbyUI] ConnectionBackButton not found under ConnectionPanel.");
    }

    private void SetConnectionCopy(string phase, string status)
    {
        if (connectionPhaseCaption == null && connectionPanel != null)
        {
            foreach (var tr in connectionPanel.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "PhaseCaption")
                {
                    connectionPhaseCaption = tr.GetComponent<TMP_Text>();
                    break;
                }
            }
        }
        if (connectionPhaseCaption != null && !string.IsNullOrEmpty(phase))
            connectionPhaseCaption.text = phase;
        if (connectionStatusText != null && status != null)
            connectionStatusText.text = status;
    }

    private void Connect()
    {
        SetActivePanel(PanelState.Connecting);
        SetConnectionCopy("OPENING THE GATE", "Seeking the circle...");

        EnsurePhotonConnector();
        if (PhotonConnector.Instance == null)
        {
            SetConnectionCopy("GATE SEALED", "The connector is missing. Return to the menu.");
            Debug.LogError("[LobbyUI] PhotonConnector.Instance is null after EnsurePhotonConnector.");
            return;
        }
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
        // T005: Auto-join matchmaking room if we have a pending match
        if (!string.IsNullOrEmpty(pendingMatchId))
        {
            string matchRoomName = $"match_{pendingMatchId}";
            Debug.Log($"[LobbyUI] Auto-joining matchmaking room: {matchRoomName}");
            RoomManager.Instance.JoinOrCreateRoom(matchRoomName, 4);
            pendingMatchId = null;
            return;
        }

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
        // T006: Show reconnection overlay if connector is handling reconnection
        if (PhotonConnector.Instance != null && PhotonConnector.Instance.IsReconnecting)
        {
            if (reconnectingOverlay != null) reconnectingOverlay.SetActive(true);
            if (reconnectingText != null)
                reconnectingText.text = "The path frayed. Holding the gate...";
            return;
        }

        SetActivePanel(PanelState.Connecting);
        SetConnectionCopy("PATH CLOSED", "The circle slipped away.\nSeeking it again...");

        // Auto-reconnect after a short delay
        Invoke(nameof(Connect), 2f);
    }

    private void OnJoinRoomFailed()
    {
        if (roomStatusText != null)
            roomStatusText.text = "Could not enter that room. Choose another.";
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
        Debug.Log("[LobbyUI] Return to Menu clicked");
        // Stop pending reconnect attempts so we don't fight the scene load
        CancelInvoke();
        UnsubscribeEvents();
        try
        {
            if (PhotonConnector.Instance != null)
                PhotonConnector.Instance.Disconnect();
            else if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LobbyUI] Disconnect on back: " + e.Message);
        }
        // Load by name + build index fallback
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else
            SceneManager.LoadScene(0);
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

    private void WireRoomTitleCopy()
    {
        if (roomTitleCopyWired || roomTitleText == null) return;
        roomTitleCopyWired = true;

        // TMP_Text is already a Graphic — cannot AddComponent<Image> on the same GO
        // (returns null → NRE). Use a transparent child hit target for tap-to-copy.
        Transform existing = roomTitleText.transform.Find("CopyHit");
        GameObject hitGo;
        if (existing != null)
        {
            hitGo = existing.gameObject;
        }
        else
        {
            hitGo = new GameObject("CopyHit");
            hitGo.transform.SetParent(roomTitleText.transform, false);
            RectTransform rt = hitGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Image img = hitGo.GetComponent<Image>();
        if (img == null) img = hitGo.AddComponent<Image>();
        img.color = Tokens.WithAlpha(Tokens.Ash, 0f);
        img.raycastTarget = true;

        Button btn = hitGo.GetComponent<Button>();
        if (btn == null) btn = hitGo.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveListener(OnRoomCodeClicked);
        btn.onClick.AddListener(OnRoomCodeClicked);

        // Text itself should not steal clicks from the hit child
        roomTitleText.raycastTarget = false;
    }

    private void OnRoomCodeClicked()
    {
        if (roomTitleText == null || string.IsNullOrEmpty(roomTitleText.text)) return;
        GUIUtility.systemCopyBuffer = roomTitleText.text;
        // Ignite flash confirmation (DESIGN §8)
        StartCoroutine(RoomCodeIgniteFlash());
        Debug.Log("[LobbyUI] Room code copied: " + roomTitleText.text);
    }

    private IEnumerator RoomCodeIgniteFlash()
    {
        if (roomTitleText == null) yield break;
        Color was = roomTitleText.color;
        roomTitleText.color = Tokens.Ember;
        float d = UiMotion.Dur(Tokens.DurBase);
        if (d <= 0f)
        {
            roomTitleText.color = was;
            yield break;
        }
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            roomTitleText.color = Color.Lerp(Tokens.Ember, was, Mathf.Clamp01(t / d));
            yield return null;
        }
        roomTitleText.color = was;
    }

    private void UpdateRoomInterior()
    {
        if (!PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;

        if (roomTitleText != null)
            roomTitleText.text = room.Name;

        // T750 §8: visual player slots when container is wired; else legacy text list
        if (playerSlotContainer != null)
        {
            RebuildPlayerSlots(room);
            if (playerListText != null) playerListText.gameObject.SetActive(false);
        }
        else if (playerListText != null)
        {
            playerListText.gameObject.SetActive(true);
            string playerList = "";
            int index = 1;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                string host = player.IsMasterClient ? " (Host)" : "";
                string local = player.IsLocal ? " (You)" : "";
                string displayName = PhotonConnector.GetPlayerDisplayName(player);
                int rating = PhotonConnector.GetPlayerRating(player);
                playerList += index + ". " + displayName + " [" + rating + "]" + host + local + "\n";
                index++;
            }
            for (int i = index; i <= room.MaxPlayers; i++)
                playerList += i + ". [AI]\n";
            playerListText.text = playerList;
        }

        // Status text
        if (roomStatusText != null)
        {
            roomStatusText.text = room.PlayerCount + "/" + room.MaxPlayers + " players. " +
                (PhotonNetwork.IsMasterClient ? "Press Start when ready." : "Waiting for host...");
        }

        // Start button only visible to host, enabled when at least 1 player
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startGameButton.interactable = room.PlayerCount >= 1;
            var ignite = startGameButton.GetComponent<IgniteButton>();
            if (ignite != null) ignite.SetInteractable(startGameButton.interactable);
        }
    }

    /// <summary>
    /// Rebuild notched player slots: filled / empty ("Awaiting challenger").
    /// Host is treated as ready (slot_ready ember) — no green checkmarks.
    /// </summary>
    private void RebuildPlayerSlots(Room room)
    {
        // Clear previous
        foreach (var go in playerSlotInstances)
        {
            if (go != null) Destroy(go);
        }
        playerSlotInstances.Clear();

        var sprites = UiSprites.Instance;
        var players = PhotonNetwork.PlayerList;
        int max = room.MaxPlayers;

        for (int i = 0; i < max; i++)
        {
            bool filled = i < players.Length;
            Photon.Realtime.Player player = filled ? players[i] : null;
            bool ready = filled && player.IsMasterClient; // host = ready chrome for now

            GameObject slot = new GameObject("PlayerSlot_" + i);
            slot.transform.SetParent(playerSlotContainer, false);

            RectTransform rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, Tokens.SlotHeight);

            Image bg = slot.AddComponent<Image>();
            Sprite spr = null;
            if (sprites != null)
            {
                if (!filled) spr = sprites.SlotEmpty;
                else if (ready) spr = sprites.SlotReady;
                else spr = sprites.SlotFilled;
                if (spr != null) UiSprites.ApplySliced(bg, spr);
            }
            if (spr == null)
            {
                bg.color = filled
                    ? (ready ? Tokens.Ember : Tokens.Bronze)
                    : Tokens.WithAlpha(Tokens.StoneEdge, 0.5f);
            }
            bg.raycastTarget = false;

            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.minHeight = Tokens.SlotHeight;
            le.preferredHeight = Tokens.SlotHeight;

            // Label
            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(slot.transform, false);
            RectTransform tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(Tokens.Space3, Tokens.Space1);
            tr.offsetMax = new Vector2(-Tokens.Space3, -Tokens.Space1);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = Tokens.TextBody;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            if (FontRefs.Instance != null && FontRefs.Instance.Label != null)
                tmp.font = FontRefs.Instance.Label;

            if (filled)
            {
                string displayName = PhotonConnector.GetPlayerDisplayName(player);
                int rating = PhotonConnector.GetPlayerRating(player);
                string host = player.IsMasterClient ? " · Host" : "";
                string you = player.IsLocal ? " · You" : "";
                tmp.text = displayName + "  [" + rating + "]" + host + you;
                tmp.color = Tokens.BoneBright;
            }
            else
            {
                tmp.text = "Awaiting challenger";
                tmp.color = Tokens.BoneDim;
            }

            // Fade in at DurBase (DESIGN §8)
            CanvasGroup cg = slot.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            StartCoroutine(FadeSlotIn(cg));

            playerSlotInstances.Add(slot);
        }
    }

    private IEnumerator FadeSlotIn(CanvasGroup cg)
    {
        if (cg == null) yield break;
        float dur = UiMotion.Dur(Tokens.DurBase);
        if (dur <= 0f)
        {
            cg.alpha = 1f;
            yield break;
        }
        float t = 0f;
        while (t < dur && cg != null)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }
        if (cg != null) cg.alpha = 1f;
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
