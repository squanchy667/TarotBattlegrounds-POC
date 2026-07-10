#if UNITY_EDITOR && PHOTON_UNITY_NETWORKING
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// Editor script to create the Lobby scene with all UI elements for LobbyUI.
/// Run via menu: Tools/Game/Setup Lobby Scene
/// Also creates the RoomListEntry prefab.
/// </summary>
public class LobbySceneSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Lobby Scene")]
    public static void SetupLobbyScene()
    {
        // Confirm before creating
        if (!EditorUtility.DisplayDialog("Create Lobby Scene",
            "This will create a new Lobby scene at Assets/Scenes/Lobby.unity " +
            "and a RoomListEntry prefab.\n\nContinue?", "Create", "Cancel"))
            return;

        // Create RoomListEntry prefab first (needed by LobbyUI)
        GameObject roomListEntryPrefab = CreateRoomListEntryPrefab();

        // Create new scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove default directional light (lobby doesn't need it)
        var light = Object.FindObjectOfType<Light>();
        if (light != null)
            Object.DestroyImmediate(light.gameObject);

        // Create Canvas
        GameObject canvasObj = EditorUiFactory.CreateCanvas(withBackground: true, withEventSystem: false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();

        // Create LobbyUI root and component
        LobbyUI lobbyUI = canvasObj.AddComponent<LobbyUI>();
        SerializedObject so = new SerializedObject(lobbyUI);

        // Create panels
        GameObject connectionPanel = CreateConnectionPanel(canvasObj.transform, so);
        GameObject roomBrowserPanel = CreateRoomBrowserPanel(canvasObj.transform, so, roomListEntryPrefab);
        GameObject roomInteriorPanel = CreateRoomInteriorPanel(canvasObj.transform, so);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lobbyUI);

        // Add Lobby to Build Settings
        AddLobbyToBuildSettings();

        // Save scene
        string scenePath = "Assets/Scenes/Lobby.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        Debug.Log("[LobbySceneSetup] Lobby scene created and saved!");
        EditorUtility.DisplayDialog("Lobby Scene Created",
            "Lobby scene saved to Assets/Scenes/Lobby.unity\n" +
            "RoomListEntry prefab saved to Assets/Prefabs/UI/RoomListEntry.prefab\n" +
            "Lobby added to Build Settings.\n\n" +
            "You can now test: MainMenu → Multiplayer → Lobby", "OK");
    }

    // ===================== CONNECTION PANEL =====================

    private static GameObject CreateConnectionPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "ConnectionPanel");
        so.FindProperty("connectionPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = EditorUiFactory.CreateCenteredContainer(panel.transform, "Content", 500, 300);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Title
        EditorUiFactory.CreateText(content.transform, "TitleText", "Tarot Battlegrounds", (int)Tokens.TextH2,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Center);

        // Status text
        GameObject statusObj = EditorUiFactory.CreateText(content.transform, "ConnectionStatusText",
            "Connecting to server...", (int)Tokens.TextBody, FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Center);
        so.FindProperty("connectionStatusText").objectReferenceValue =
            statusObj.GetComponent<TMP_Text>();

        // Player name input
        GameObject nameInput = EditorUiFactory.CreateInputField(content.transform, "PlayerNameInput",
            "Enter your name...", 350, 50);
        so.FindProperty("playerNameInput").objectReferenceValue =
            nameInput.GetComponent<TMP_InputField>();

        return panel;
    }

    // ===================== ROOM BROWSER PANEL =====================

    private static GameObject CreateRoomBrowserPanel(Transform parent, SerializedObject so,
        GameObject roomListEntryPrefab)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "RoomBrowserPanel");
        so.FindProperty("roomBrowserPanel").objectReferenceValue = panel;

        // Main layout
        GameObject content = EditorUiFactory.CreateCenteredContainer(panel.transform, "Content", 800, 700);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = Tokens.Space2;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Title
        EditorUiFactory.CreateText(content.transform, "BrowserTitle", "Online Lobby", (int)Tokens.TextH2,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Center);

        // Create Room Row
        GameObject createRow = EditorUiFactory.CreateHorizontalRow(content.transform, "CreateRoomRow", 10);

        // Room name input
        GameObject roomNameInputObj = EditorUiFactory.CreateInputField(createRow.transform, "RoomNameInput",
            "Room name...", 300, 45);
        so.FindProperty("roomNameInput").objectReferenceValue =
            roomNameInputObj.GetComponent<TMP_InputField>();

        // Max players dropdown
        GameObject dropdownObj = EditorUiFactory.CreateDropdown(createRow.transform, "MaxPlayersDropdown", 160, 45,
            labelText: "2 Players", fontSize: (int)Tokens.TextLabel);
        so.FindProperty("maxPlayersDropdown").objectReferenceValue =
            dropdownObj.GetComponent<TMP_Dropdown>();

        // Create Room button
        GameObject createBtn = EditorUiFactory.CreateButton(createRow.transform, "CreateRoomButton",
            "Create Room", 160, 45);
        so.FindProperty("createRoomButton").objectReferenceValue =
            createBtn.GetComponent<Button>();

        // Join Random + Back row
        GameObject actionRow = EditorUiFactory.CreateHorizontalRow(content.transform, "ActionRow", 10);

        GameObject joinRandomBtn = EditorUiFactory.CreateButton(actionRow.transform, "JoinRandomButton",
            "Join Random", 160, 45);
        so.FindProperty("joinRandomButton").objectReferenceValue =
            joinRandomBtn.GetComponent<Button>();

        GameObject backBtn = EditorUiFactory.CreateButton(actionRow.transform, "BackToMenuButton",
            "Back to Menu", 160, 45);
        so.FindProperty("backToMenuButton").objectReferenceValue =
            backBtn.GetComponent<Button>();

        // Separator
        EditorUiFactory.CreateText(content.transform, "RoomsHeader", "Available Rooms", (int)Tokens.TextBody,
            FontStyles.Bold, Tokens.BoneBright, TextAlignmentOptions.Left);

        // Room list scroll area
        GameObject scrollArea = CreateScrollView(content.transform, "RoomListScroll", 740, 350);
        Transform roomListContainer = scrollArea.transform.Find("Viewport/Content");
        so.FindProperty("roomListContainer").objectReferenceValue = roomListContainer;

        // No rooms text
        GameObject noRoomsObj = EditorUiFactory.CreateText(content.transform, "NoRoomsText",
            "No rooms available. Create one!", (int)Tokens.TextCaption, FontStyles.Italic,
            Tokens.BoneDim, TextAlignmentOptions.Center);
        so.FindProperty("noRoomsText").objectReferenceValue =
            noRoomsObj.GetComponent<TMP_Text>();

        // Room list entry prefab reference
        if (roomListEntryPrefab != null)
        {
            so.FindProperty("roomListEntryPrefab").objectReferenceValue = roomListEntryPrefab;
        }

        return panel;
    }

    // ===================== ROOM INTERIOR PANEL =====================

    private static GameObject CreateRoomInteriorPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "RoomInteriorPanel");
        so.FindProperty("roomInteriorPanel").objectReferenceValue = panel;

        GameObject content = EditorUiFactory.CreateCenteredContainer(panel.transform, "Content", 600, 500);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = Tokens.Space2;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Room title
        GameObject roomTitleObj = EditorUiFactory.CreateText(content.transform, "RoomTitleText",
            "Room Name", (int)Tokens.TextH3, FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Center);
        so.FindProperty("roomTitleText").objectReferenceValue =
            roomTitleObj.GetComponent<TMP_Text>();

        // Player list
        GameObject playerListObj = EditorUiFactory.CreateText(content.transform, "PlayerListText",
            "1. Waiting...\n2. [AI]\n3. [AI]\n4. [AI]", (int)Tokens.TextBody, FontStyles.Normal,
            Tokens.Bone, TextAlignmentOptions.Left);
        LayoutElement ple = playerListObj.AddComponent<LayoutElement>();
        ple.minHeight = 150;
        so.FindProperty("playerListText").objectReferenceValue =
            playerListObj.GetComponent<TMP_Text>();

        // Status text
        GameObject statusObj = EditorUiFactory.CreateText(content.transform, "RoomStatusText",
            "Waiting for players...", (int)Tokens.TextCaption, FontStyles.Normal,
            Tokens.BoneDim, TextAlignmentOptions.Center);
        so.FindProperty("roomStatusText").objectReferenceValue =
            statusObj.GetComponent<TMP_Text>();

        // Buttons row
        GameObject buttonsRow = EditorUiFactory.CreateHorizontalRow(content.transform, "ButtonsRow", 20);

        GameObject startBtn = EditorUiFactory.CreateButton(buttonsRow.transform, "StartGameButton",
            "Start Game", 180, 50);
        so.FindProperty("startGameButton").objectReferenceValue =
            startBtn.GetComponent<Button>();

        GameObject leaveBtn = EditorUiFactory.CreateButton(buttonsRow.transform, "LeaveRoomButton",
            "Leave Room", 180, 50);
        so.FindProperty("leaveRoomButton").objectReferenceValue =
            leaveBtn.GetComponent<Button>();

        return panel;
    }

    // ===================== ROOM LIST ENTRY PREFAB =====================

    private static GameObject CreateRoomListEntryPrefab()
    {
        string prefabPath = "Assets/Prefabs/UI/RoomListEntry.prefab";

        // Check if already exists
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log("[LobbySceneSetup] RoomListEntry prefab already exists.");
            return existing;
        }

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        // Create prefab
        GameObject entryObj = new GameObject("RoomListEntry");

        RectTransform rect = entryObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 50);

        // Background image
        Image img = entryObj.AddComponent<Image>();
        img.color = Tokens.Umber;

        // Button
        Button btn = entryObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Tokens.Umber;
        colors.highlightedColor = Tokens.Ember;
        colors.pressedColor = Tokens.CharredWood;
        colors.selectedColor = Tokens.Ember;
        btn.colors = colors;

        // LayoutElement
        LayoutElement le = entryObj.AddComponent<LayoutElement>();
        le.minHeight = 50;
        le.preferredHeight = 50;

        // Text child
        GameObject textObj = new GameObject("RoomInfoText");
        textObj.transform.SetParent(entryObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 5);
        textRect.offsetMax = new Vector2(-15, -5);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Room Name (2/4) - Host: Player";
        tmp.fontSize = Tokens.TextCaption;
        tmp.color = Tokens.Bone;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;

        tmp.font = FontRefs.Instance.Body;

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(entryObj, prefabPath);
        Object.DestroyImmediate(entryObj);

        Debug.Log("[LobbySceneSetup] Created RoomListEntry prefab at " + prefabPath);
        return prefab;
    }

    // ===================== BUILD SETTINGS =====================

    private static void AddLobbyToBuildSettings()
    {
        string lobbyPath = "Assets/Scenes/Lobby.unity";
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        // Check if already added
        foreach (var s in scenes)
        {
            if (s.path == lobbyPath) return;
        }

        // Insert Lobby after MainMenu (index 1)
        int insertAt = 1;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path.Contains("MainMenu"))
            {
                insertAt = i + 1;
                break;
            }
        }

        scenes.Insert(insertAt, new EditorBuildSettingsScene(lobbyPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[LobbySceneSetup] Added Lobby to Build Settings at index " + insertAt);
    }

    // ===================== HELPER METHODS =====================

    private static GameObject CreateScrollView(Transform parent, string name,
        float width, float height)
    {
        GameObject scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);

        RectTransform rect = scrollObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = scrollObj.AddComponent<Image>();
        img.color = Tokens.CharredWood;

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        LayoutElement le = scrollObj.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);

        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;

        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRect;
        scroll.content = contentRect;

        return scrollObj;
    }

}
#endif
