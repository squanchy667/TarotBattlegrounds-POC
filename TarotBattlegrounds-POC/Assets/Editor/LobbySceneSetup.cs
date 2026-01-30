#if UNITY_EDITOR && PHOTON_UNITY_NETWORKING
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

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
        GameObject canvasObj = CreateCanvas();
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
            "RoomListEntry prefab saved to Assets/Preferbs/UI/RoomListEntry.prefab\n" +
            "Lobby added to Build Settings.\n\n" +
            "You can now test: MainMenu → Multiplayer → Lobby", "OK");
    }

    // ===================== CANVAS =====================

    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.08f, 0.14f, 1f);
        bgImg.raycastTarget = false;

        return canvasObj;
    }

    // ===================== CONNECTION PANEL =====================

    private static GameObject CreateConnectionPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = CreatePanel(parent, "ConnectionPanel");
        so.FindProperty("connectionPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = CreateCenteredContainer(panel.transform, "Content", 500, 300);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Title
        CreateText(content.transform, "TitleText", "Tarot Battlegrounds", 36,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Status text
        GameObject statusObj = CreateText(content.transform, "ConnectionStatusText",
            "Connecting to server...", 22, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        so.FindProperty("connectionStatusText").objectReferenceValue =
            statusObj.GetComponent<TMP_Text>();

        // Player name input
        GameObject nameInput = CreateInputField(content.transform, "PlayerNameInput",
            "Enter your name...", 350, 50);
        so.FindProperty("playerNameInput").objectReferenceValue =
            nameInput.GetComponent<TMP_InputField>();

        return panel;
    }

    // ===================== ROOM BROWSER PANEL =====================

    private static GameObject CreateRoomBrowserPanel(Transform parent, SerializedObject so,
        GameObject roomListEntryPrefab)
    {
        GameObject panel = CreatePanel(parent, "RoomBrowserPanel");
        so.FindProperty("roomBrowserPanel").objectReferenceValue = panel;

        // Main layout
        GameObject content = CreateCenteredContainer(panel.transform, "Content", 800, 700);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Title
        CreateText(content.transform, "BrowserTitle", "Online Lobby", 32,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Create Room Row
        GameObject createRow = CreateHorizontalRow(content.transform, "CreateRoomRow", 10);

        // Room name input
        GameObject roomNameInputObj = CreateInputField(createRow.transform, "RoomNameInput",
            "Room name...", 300, 45);
        so.FindProperty("roomNameInput").objectReferenceValue =
            roomNameInputObj.GetComponent<TMP_InputField>();

        // Max players dropdown
        GameObject dropdownObj = CreateDropdown(createRow.transform, "MaxPlayersDropdown", 160, 45);
        so.FindProperty("maxPlayersDropdown").objectReferenceValue =
            dropdownObj.GetComponent<TMP_Dropdown>();

        // Create Room button
        GameObject createBtn = CreateButton(createRow.transform, "CreateRoomButton",
            "Create Room", 160, 45);
        so.FindProperty("createRoomButton").objectReferenceValue =
            createBtn.GetComponent<Button>();

        // Join Random + Back row
        GameObject actionRow = CreateHorizontalRow(content.transform, "ActionRow", 10);

        GameObject joinRandomBtn = CreateButton(actionRow.transform, "JoinRandomButton",
            "Join Random", 160, 45);
        so.FindProperty("joinRandomButton").objectReferenceValue =
            joinRandomBtn.GetComponent<Button>();

        GameObject backBtn = CreateButton(actionRow.transform, "BackToMenuButton",
            "Back to Menu", 160, 45);
        so.FindProperty("backToMenuButton").objectReferenceValue =
            backBtn.GetComponent<Button>();

        // Separator
        CreateText(content.transform, "RoomsHeader", "Available Rooms", 22,
            FontStyles.Bold, Color.white, TextAlignmentOptions.Left);

        // Room list scroll area
        GameObject scrollArea = CreateScrollView(content.transform, "RoomListScroll", 740, 350);
        Transform roomListContainer = scrollArea.transform.Find("Viewport/Content");
        so.FindProperty("roomListContainer").objectReferenceValue = roomListContainer;

        // No rooms text
        GameObject noRoomsObj = CreateText(content.transform, "NoRoomsText",
            "No rooms available. Create one!", 18, FontStyles.Italic,
            new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
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
        GameObject panel = CreatePanel(parent, "RoomInteriorPanel");
        so.FindProperty("roomInteriorPanel").objectReferenceValue = panel;

        GameObject content = CreateCenteredContainer(panel.transform, "Content", 600, 500);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(30, 30, 30, 30);

        // Room title
        GameObject roomTitleObj = CreateText(content.transform, "RoomTitleText",
            "Room Name", 28, FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);
        so.FindProperty("roomTitleText").objectReferenceValue =
            roomTitleObj.GetComponent<TMP_Text>();

        // Player list
        GameObject playerListObj = CreateText(content.transform, "PlayerListText",
            "1. Waiting...\n2. [AI]\n3. [AI]\n4. [AI]", 20, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Left);
        LayoutElement ple = playerListObj.AddComponent<LayoutElement>();
        ple.minHeight = 150;
        so.FindProperty("playerListText").objectReferenceValue =
            playerListObj.GetComponent<TMP_Text>();

        // Status text
        GameObject statusObj = CreateText(content.transform, "RoomStatusText",
            "Waiting for players...", 18, FontStyles.Normal,
            new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        so.FindProperty("roomStatusText").objectReferenceValue =
            statusObj.GetComponent<TMP_Text>();

        // Buttons row
        GameObject buttonsRow = CreateHorizontalRow(content.transform, "ButtonsRow", 20);

        GameObject startBtn = CreateButton(buttonsRow.transform, "StartGameButton",
            "Start Game", 180, 50);
        so.FindProperty("startGameButton").objectReferenceValue =
            startBtn.GetComponent<Button>();

        GameObject leaveBtn = CreateButton(buttonsRow.transform, "LeaveRoomButton",
            "Leave Room", 180, 50);
        so.FindProperty("leaveRoomButton").objectReferenceValue =
            leaveBtn.GetComponent<Button>();

        return panel;
    }

    // ===================== ROOM LIST ENTRY PREFAB =====================

    private static GameObject CreateRoomListEntryPrefab()
    {
        string prefabPath = "Assets/Preferbs/UI/RoomListEntry.prefab";

        // Check if already exists
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log("[LobbySceneSetup] RoomListEntry prefab already exists.");
            return existing;
        }

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Preferbs"))
            AssetDatabase.CreateFolder("Assets", "Preferbs");
        if (!AssetDatabase.IsValidFolder("Assets/Preferbs/UI"))
            AssetDatabase.CreateFolder("Assets/Preferbs", "UI");

        // Create prefab
        GameObject entryObj = new GameObject("RoomListEntry");

        RectTransform rect = entryObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 50);

        // Background image
        Image img = entryObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.18f, 0.25f, 1f);

        // Button
        Button btn = entryObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.18f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.25f, 0.35f, 1f);
        colors.pressedColor = new Color(0.15f, 0.12f, 0.2f, 1f);
        colors.selectedColor = new Color(0.25f, 0.22f, 0.3f, 1f);
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
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;

        // Try to find font
        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

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

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return panel;
    }

    private static GameObject CreateCenteredContainer(Transform parent, string name,
        float width, float height)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.18f, 0.95f);

        return container;
    }

    private static GameObject CreateText(Transform parent, string name, string text,
        int fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 12);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 12;

        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        float width, float height)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.22f, 0.35f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.3f, 0.45f, 1f);
        colors.pressedColor = new Color(0.18f, 0.15f, 0.28f, 1f);
        colors.selectedColor = new Color(0.3f, 0.27f, 0.4f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors = colors;

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Text child
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        return btnObj;
    }

    private static GameObject CreateInputField(Transform parent, string name,
        string placeholder, float width, float height)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = inputObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.15f, 0.22f, 1f);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder text
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        TextMeshProUGUI phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 16;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_FontAsset font = FindFont();
        if (font != null) phTmp.font = font;

        // Input text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textObjRect = textObj.AddComponent<RectTransform>();
        textObjRect.anchorMin = Vector2.zero;
        textObjRect.anchorMax = Vector2.one;
        textObjRect.offsetMin = Vector2.zero;
        textObjRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 16;
        textTmp.color = Color.white;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) textTmp.font = font;

        // TMP_InputField
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = font;
        inputField.pointSize = 16;

        return inputObj;
    }

    private static GameObject CreateDropdown(Transform parent, string name,
        float width, float height)
    {
        GameObject dropObj = new GameObject(name);
        dropObj.transform.SetParent(parent, false);

        RectTransform rect = dropObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = dropObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.15f, 0.22f, 1f);

        LayoutElement le = dropObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "2 Players";
        labelTmp.fontSize = 16;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_FontAsset font = FindFont();
        if (font != null) labelTmp.font = font;

        // Template (dropdown list)
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropObj.transform, false);
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0, 150);

        Image templateImg = template.AddComponent<Image>();
        templateImg.color = new Color(0.18f, 0.15f, 0.22f, 1f);

        ScrollRect scroll = template.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>();
        viewport.AddComponent<Image>().color = Color.white;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);

        scroll.viewport = vpRect;
        scroll.content = contentRect;

        // Item
        GameObject item = new GameObject("Item");
        item.transform.SetParent(contentObj.transform, false);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 40);
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);

        Toggle toggle = item.AddComponent<Toggle>();

        // Item label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(item.transform, false);
        RectTransform ilRect = itemLabelObj.AddComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero;
        ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(10, 2);
        ilRect.offsetMax = new Vector2(-10, -2);

        TextMeshProUGUI itemTmp = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemTmp.text = "Option";
        itemTmp.fontSize = 16;
        itemTmp.color = Color.white;
        if (font != null) itemTmp.font = font;

        template.SetActive(false);

        // TMP_Dropdown
        TMP_Dropdown dropdown = dropObj.AddComponent<TMP_Dropdown>();
        dropdown.template = templateRect;
        dropdown.captionText = labelTmp;
        dropdown.itemText = itemTmp;

        return dropObj;
    }

    private static GameObject CreateHorizontalRow(Transform parent, string name, float spacing)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 55;

        return row;
    }

    private static GameObject CreateScrollView(Transform parent, string name,
        float width, float height)
    {
        GameObject scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);

        RectTransform rect = scrollObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = scrollObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.1f, 0.16f, 1f);

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

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
        return font;
    }
}
#endif
