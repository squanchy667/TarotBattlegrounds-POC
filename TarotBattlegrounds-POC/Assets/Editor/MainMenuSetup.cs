#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UX17: Editor script to set up the redesigned MainMenu scene with atmospheric visuals.
/// Creates: Background (gradient + particles), TitleArea, ButtonArea, BottomBar.
/// Styles all buttons with TarotButton, wires MainMenuManager and MainMenuVisual references.
/// Run via menu: Tools/Game/Setup Main Menu
/// </summary>
public class MainMenuSetup : EditorWindow
{
    // Tarot color palette constants
    private static readonly Color GoldAccent = new Color(1f, 0.78f, 0.15f);
    private static readonly Color DarkPurple = new Color(0.12f, 0.08f, 0.18f);
    private static readonly Color MediumPurple = new Color(0.55f, 0.3f, 0.75f);
    private static readonly Color DimPurple = new Color(0.22f, 0.18f, 0.30f);
    private static readonly Color SubtitleGray = new Color(0.65f, 0.65f, 0.7f);

    [MenuItem("Tools/Game/Setup Main Menu")]
    public static void SetupMainMenu()
    {
        // Ensure we're in the MainMenu scene
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("MainMenu"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not MainMenu. Open MainMenu scene first?\n\n" +
                "(This will save the current scene.)",
                "Open MainMenu", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        if (!EditorUtility.DisplayDialog("UX17: Setup Main Menu",
            "This will create the redesigned main menu layout:\n\n" +
            "  - Background (gradient + particles via BackgroundController)\n" +
            "  - TitleArea (title, subtitle, logo slot)\n" +
            "  - ButtonArea (Play Solo, Multiplayer, Ranked)\n" +
            "  - BottomBar (Collection, Settings, Profile + selectors)\n\n" +
            "All references wired on MainMenuManager and MainMenuVisual.\n\n" +
            "Continue?", "Create", "Cancel"))
            return;

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();
        }

        // Find or create MainMenuManager
        MainMenuManager manager = Object.FindObjectOfType<MainMenuManager>();
        if (manager == null)
        {
            manager = canvas.gameObject.AddComponent<MainMenuManager>();
        }

        // Find or create MainMenuVisual
        MainMenuVisual visual = Object.FindObjectOfType<MainMenuVisual>();
        if (visual == null)
        {
            visual = canvas.gameObject.AddComponent<MainMenuVisual>();
        }

        Undo.RegisterCompleteObjectUndo(manager, "UX17 Setup Main Menu");
        Undo.RegisterCompleteObjectUndo(visual, "UX17 Setup Main Menu");

        SerializedObject managerSO = new SerializedObject(manager);
        SerializedObject visualSO = new SerializedObject(visual);

        // Remove old UX17 layout elements (safe re-run)
        RemoveOldElements(canvas.transform);

        // ===============================
        // 1. CONTENT GROUP (wraps everything for fade-in)
        // ===============================
        GameObject contentGroupObj = new GameObject("ContentGroup");
        contentGroupObj.transform.SetParent(canvas.transform, false);
        RectTransform contentRect = contentGroupObj.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        CanvasGroup contentGroup = contentGroupObj.AddComponent<CanvasGroup>();

        managerSO.FindProperty("contentGroup").objectReferenceValue = contentGroup;

        // ===============================
        // 2. MAIN PANEL (inside ContentGroup)
        // ===============================
        GameObject mainPanel = EditorUiFactory.CreateFullscreenPanel(contentGroupObj.transform, "MainPanel");
        managerSO.FindProperty("mainPanel").objectReferenceValue = mainPanel;

        // ===============================
        // 3. TITLE AREA (top 30%)
        // ===============================
        GameObject titleArea = new GameObject("TitleArea");
        titleArea.transform.SetParent(mainPanel.transform, false);
        RectTransform titleAreaRect = titleArea.AddComponent<RectTransform>();
        titleAreaRect.anchorMin = new Vector2(0f, 0.55f);
        titleAreaRect.anchorMax = new Vector2(1f, 1f);
        titleAreaRect.offsetMin = Vector2.zero;
        titleAreaRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup titleVLG = titleArea.AddComponent<VerticalLayoutGroup>();
        titleVLG.spacing = 8;
        titleVLG.childAlignment = TextAnchor.MiddleCenter;
        titleVLG.childControlWidth = true;
        titleVLG.childControlHeight = false;
        titleVLG.childForceExpandWidth = true;
        titleVLG.childForceExpandHeight = false;
        titleVLG.padding = new RectOffset(50, 50, 60, 20);

        // Logo slot (empty placeholder)
        GameObject logoObj = new GameObject("LogoImage");
        logoObj.transform.SetParent(titleArea.transform, false);
        RectTransform logoRect = logoObj.AddComponent<RectTransform>();
        logoRect.sizeDelta = new Vector2(200, 100);
        Image logoImage = logoObj.AddComponent<Image>();
        logoImage.color = new Color(1f, 1f, 1f, 0.1f); // Very faint placeholder
        logoImage.raycastTarget = false;
        LayoutElement logoLE = logoObj.AddComponent<LayoutElement>();
        logoLE.preferredWidth = 200;
        logoLE.preferredHeight = 100;

        managerSO.FindProperty("logoImage").objectReferenceValue = logoImage;

        // Title text
        GameObject titleTextObj = EditorUiFactory.CreateText(titleArea.transform, "TitleText",
            "TAROT BATTLEGROUNDS", 48, FontStyles.Bold, GoldAccent, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);
        TMP_Text titleTMP = titleTextObj.GetComponent<TMP_Text>();

        managerSO.FindProperty("titleText").objectReferenceValue = titleTMP;
        visualSO.FindProperty("titleText").objectReferenceValue = titleTMP;

        // Subtitle text
        GameObject subtitleObj = EditorUiFactory.CreateText(titleArea.transform, "SubtitleText",
            "A Mystical Auto-Battler", 22, FontStyles.Italic, SubtitleGray, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);
        TMP_Text subtitleTMP = subtitleObj.GetComponent<TMP_Text>();

        managerSO.FindProperty("subtitleText").objectReferenceValue = subtitleTMP;

        // ===============================
        // 4. BUTTON AREA (center)
        // ===============================
        GameObject buttonArea = new GameObject("ButtonArea");
        buttonArea.transform.SetParent(mainPanel.transform, false);
        RectTransform buttonAreaRect = buttonArea.AddComponent<RectTransform>();
        buttonAreaRect.anchorMin = new Vector2(0.25f, 0.2f);
        buttonAreaRect.anchorMax = new Vector2(0.75f, 0.55f);
        buttonAreaRect.offsetMin = Vector2.zero;
        buttonAreaRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup buttonVLG = buttonArea.AddComponent<VerticalLayoutGroup>();
        buttonVLG.spacing = 16;
        buttonVLG.childAlignment = TextAnchor.MiddleCenter;
        buttonVLG.childControlWidth = true;
        buttonVLG.childControlHeight = false;
        buttonVLG.childForceExpandWidth = true;
        buttonVLG.childForceExpandHeight = false;
        buttonVLG.padding = new RectOffset(20, 20, 10, 10);

        // Play Solo button (Primary variant - gold, large)
        GameObject soloBtn = CreateStyledButton(buttonArea.transform, "SoloButton",
            "PLAY SOLO", 350, 65, ButtonVariant.Primary);
        managerSO.FindProperty("soloButton").objectReferenceValue = soloBtn.GetComponent<Button>();

        // Multiplayer button (Secondary variant - purple)
        GameObject mpBtn = CreateStyledButton(buttonArea.transform, "MultiplayerButton",
            "MULTIPLAYER", 320, 55, ButtonVariant.Secondary);
        managerSO.FindProperty("multiplayerButton").objectReferenceValue = mpBtn.GetComponent<Button>();

        // Ranked button (Secondary variant - purple)
        GameObject rankedBtn = CreateStyledButton(buttonArea.transform, "RankedButton",
            "RANKED", 320, 55, ButtonVariant.Secondary);
        managerSO.FindProperty("rankedButton").objectReferenceValue = rankedBtn.GetComponent<Button>();

        // Player info text (below ranked button, for authenticated user display)
        GameObject playerInfoObj = EditorUiFactory.CreateText(buttonArea.transform, "PlayerInfoText",
            "", 16, FontStyles.Normal, SubtitleGray, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);
        managerSO.FindProperty("playerInfoText").objectReferenceValue =
            playerInfoObj.GetComponent<TMP_Text>();

        // ===============================
        // 5. BOTTOM BAR
        // ===============================
        GameObject bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(mainPanel.transform, false);
        RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0.2f);
        bottomRect.offsetMin = new Vector2(20, 10);
        bottomRect.offsetMax = new Vector2(-20, -5);

        // Bottom bar uses horizontal layout with two sections
        HorizontalLayoutGroup bottomHLG = bottomBar.AddComponent<HorizontalLayoutGroup>();
        bottomHLG.spacing = 20;
        bottomHLG.childAlignment = TextAnchor.MiddleCenter;
        bottomHLG.childControlWidth = true;
        bottomHLG.childControlHeight = true;
        bottomHLG.childForceExpandWidth = true;
        bottomHLG.childForceExpandHeight = false;
        bottomHLG.padding = new RectOffset(30, 30, 5, 5);

        // --- Left section: Selectors ---
        GameObject leftSection = new GameObject("SelectorSection");
        leftSection.transform.SetParent(bottomBar.transform, false);
        leftSection.AddComponent<RectTransform>();
        VerticalLayoutGroup leftVLG = leftSection.AddComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 8;
        leftVLG.childAlignment = TextAnchor.MiddleLeft;
        leftVLG.childControlWidth = true;
        leftVLG.childControlHeight = false;
        leftVLG.childForceExpandWidth = true;
        leftVLG.childForceExpandHeight = false;
        LayoutElement leftLE = leftSection.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 2;

        // Difficulty row
        GameObject diffRow = CreateSelectorRow(leftSection.transform, "DifficultyRow", "Difficulty:",
            new string[] { "Easy", "Medium", "Hard" }, visualSO, "difficultyButtons", "difficultyLabels");

        // Player count row
        GameObject countRow = CreateSelectorRow(leftSection.transform, "PlayerCountRow", "Players:",
            new string[] { "4", "6", "8" }, visualSO, "playerCountButtons", "playerCountLabels");

        // Wire player count buttons to MainMenuManager as well
        WirePlayerCountButtons(countRow, managerSO);

        // --- Right section: Icon buttons ---
        GameObject rightSection = new GameObject("IconButtonSection");
        rightSection.transform.SetParent(bottomBar.transform, false);
        rightSection.AddComponent<RectTransform>();
        HorizontalLayoutGroup rightHLG = rightSection.AddComponent<HorizontalLayoutGroup>();
        rightHLG.spacing = 12;
        rightHLG.childAlignment = TextAnchor.MiddleRight;
        rightHLG.childControlWidth = false;
        rightHLG.childControlHeight = false;
        rightHLG.childForceExpandWidth = false;
        rightHLG.childForceExpandHeight = false;
        LayoutElement rightLE = rightSection.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 1;

        // Collection button (small)
        GameObject collectionBtn = CreateSmallButton(rightSection.transform, "CollectionButton", "Collection", 110, 40);
        managerSO.FindProperty("collectionButton").objectReferenceValue = collectionBtn.GetComponent<Button>();

        // Settings button (small)
        GameObject settingsBtn = CreateSmallButton(rightSection.transform, "SettingsButton", "Settings", 100, 40);
        managerSO.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();

        // Quit button (small, danger)
        GameObject quitBtn = CreateSmallButton(rightSection.transform, "QuitButton", "Quit", 80, 40);
        managerSO.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        // Style quit as danger
        TarotButton quitTarot = quitBtn.GetComponent<TarotButton>();
        if (quitTarot != null)
        {
            SerializedObject quitSO = new SerializedObject(quitTarot);
            quitSO.FindProperty("variant").enumValueIndex = (int)ButtonVariant.Danger;
            quitSO.ApplyModifiedProperties();
        }

        // ===============================
        // 6. SOLO PANEL (separate, starts hidden)
        // ===============================
        GameObject soloPanel = CreateSoloPanel(contentGroupObj.transform, managerSO);
        soloPanel.SetActive(false);

        // ===============================
        // 7. WIRE BACKGROUND CONTROLLER (if exists)
        // ===============================
        BackgroundController bgCtrl = Object.FindObjectOfType<BackgroundController>();
        if (bgCtrl != null)
        {
            visualSO.FindProperty("backgroundController").objectReferenceValue = bgCtrl;
        }

        // ===============================
        // APPLY & FINISH
        // ===============================
        managerSO.ApplyModifiedProperties();
        visualSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MainMenuSetup] UX17 main menu setup complete!");
        EditorUtility.DisplayDialog("UX17: Main Menu Setup Complete",
            "Created redesigned main menu:\n\n" +
            "  - ContentGroup (fade-in wrapper)\n" +
            "  - MainPanel:\n" +
            "    - TitleArea (logo, title, subtitle)\n" +
            "    - ButtonArea (Play Solo, Multiplayer, Ranked)\n" +
            "    - BottomBar (selectors + Collection/Settings/Quit)\n" +
            "  - SoloPanel (difficulty + player count + Play/Back)\n\n" +
            "All references wired on MainMenuManager and MainMenuVisual.\n" +
            "Run 'Tools/Game/Setup Background' to add background layers.\n" +
            "Save the scene to keep changes.", "OK");
    }

    // ===================== CLEANUP =====================

    private static void RemoveOldElements(Transform canvasTransform)
    {
        string[] oldNames = {
            "ContentGroup", "MainPanel", "SoloPanel",
            "TitleArea", "ButtonArea", "BottomBar"
        };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[MainMenuSetup] Removed old {name}");
            }
        }
    }

    // ===================== CANVAS =====================

    // ===================== SOLO PANEL =====================

    private static GameObject CreateSoloPanel(Transform parent, SerializedObject managerSO)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "SoloPanel");
        managerSO.FindProperty("soloPanel").objectReferenceValue = panel;

        // Semi-transparent background overlay
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(DarkPurple.r, DarkPurple.g, DarkPurple.b, 0.95f);
        panelBg.raycastTarget = false;

        // Centered container
        GameObject container = new GameObject("SoloContent");
        container.transform.SetParent(panel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(550, 480);

        // Panel background
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.15f, 0.12f, 0.20f, 0.95f);
        containerBg.raycastTarget = false;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(40, 40, 30, 30);

        // Title
        EditorUiFactory.CreateText(container.transform, "SoloTitle", "SOLO GAME", 34,
            FontStyles.Bold, GoldAccent, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        // Player count label
        EditorUiFactory.CreateText(container.transform, "PlayerCountLabel", "Number of Players", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        // Player count buttons row
        GameObject countRow = EditorUiFactory.CreateHorizontalRow(container.transform, "PlayerCountRow", 20);

        GameObject btn4 = CreateStyledButton(countRow.transform, "Players4Button", "4 Players", 150, 50, ButtonVariant.Primary);
        managerSO.FindProperty("players4Button").objectReferenceValue = btn4.GetComponent<Button>();

        GameObject btn6 = CreateStyledButton(countRow.transform, "Players6Button", "6 Players", 150, 50, ButtonVariant.Secondary);
        managerSO.FindProperty("players6Button").objectReferenceValue = btn6.GetComponent<Button>();

        GameObject btn8 = CreateStyledButton(countRow.transform, "Players8Button", "8 Players", 150, 50, ButtonVariant.Secondary);
        managerSO.FindProperty("players8Button").objectReferenceValue = btn8.GetComponent<Button>();

        // Difficulty label
        EditorUiFactory.CreateText(container.transform, "DifficultyLabel", "AI Difficulty", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        // Difficulty dropdown
        GameObject diffDropdown = EditorUiFactory.CreateDropdown(container.transform, "DifficultyDropdown", 250, 45,
            backgroundColor: DimPurple);
        managerSO.FindProperty("difficultyDropdown").objectReferenceValue =
            diffDropdown.GetComponent<TMP_Dropdown>();

        // Spacer
        CreateSpacer(container.transform, 10);

        // Bottom buttons row
        GameObject bottomRow = EditorUiFactory.CreateHorizontalRow(container.transform, "BottomRow", 30);

        GameObject backBtn = CreateStyledButton(bottomRow.transform, "BackButton", "Back", 150, 50, ButtonVariant.Danger);
        managerSO.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();

        GameObject playBtn = CreateStyledButton(bottomRow.transform, "PlayButton", "PLAY", 200, 55, ButtonVariant.Success);
        managerSO.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();

        return panel;
    }

    // ===================== SELECTOR ROWS =====================

    /// <summary>
    /// Create a horizontal row with a label and toggle-style buttons.
    /// Wires button and label lists into the given SerializedObject properties.
    /// </summary>
    private static GameObject CreateSelectorRow(Transform parent, string name, string label,
        string[] options, SerializedObject visualSO, string buttonsPropName, string labelsPropName)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(5, 5, 2, 2);

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 40;
        rowLE.preferredHeight = 40;

        // Label
        GameObject labelObj = EditorUiFactory.CreateText(row.transform, "Label", label,
            16, FontStyles.Bold, SubtitleGray, TextAlignmentOptions.MidlineLeft,
            heightPadding: 14, raycastTarget: false);
        LayoutElement labelLE = labelObj.GetComponent<LayoutElement>();
        if (labelLE != null)
        {
            labelLE.minWidth = 90;
            labelLE.preferredWidth = 90;
        }

        // Option buttons
        SerializedProperty buttonsProp = visualSO.FindProperty(buttonsPropName);
        SerializedProperty labelsProp = visualSO.FindProperty(labelsPropName);

        buttonsProp.arraySize = options.Length;
        labelsProp.arraySize = options.Length;

        for (int i = 0; i < options.Length; i++)
        {
            GameObject optBtn = CreateToggleButton(row.transform, $"{name}_Option{i}", options[i], 70, 34);
            Button btn = optBtn.GetComponent<Button>();
            TMP_Text btnLabel = optBtn.GetComponentInChildren<TMP_Text>();

            buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = btn;
            labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = btnLabel;
        }

        return row;
    }

    /// <summary>
    /// Wire the player count buttons from the selector row into MainMenuManager's players4/6/8Button fields.
    /// </summary>
    private static void WirePlayerCountButtons(GameObject countRow, SerializedObject managerSO)
    {
        // The player count buttons are at child indices 1, 2, 3 (index 0 is the label)
        Transform rowTransform = countRow.transform;
        string[] fieldNames = { "players4Button", "players6Button", "players8Button" };

        for (int i = 0; i < fieldNames.Length; i++)
        {
            int childIndex = i + 1; // Skip the label at index 0
            if (childIndex < rowTransform.childCount)
            {
                Button btn = rowTransform.GetChild(childIndex).GetComponent<Button>();
                if (btn != null)
                {
                    managerSO.FindProperty(fieldNames[i]).objectReferenceValue = btn;
                }
            }
        }
    }

    // ===================== UI FACTORIES =====================

    /// <summary>
    /// Create a styled button with TarotButton component for gradient + hover effects.
    /// </summary>
    private static GameObject CreateStyledButton(Transform parent, string name, string label,
        float width, float height, ButtonVariant variant)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        // Background image (TarotButton will generate gradient texture)
        Image bgImg = btnObj.AddComponent<Image>();
        bgImg.color = DimPurple;
        bgImg.raycastTarget = true;

        // Border image
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(btnObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(MediumPurple.r, MediumPurple.g, MediumPurple.b, 0.4f);
        borderImg.raycastTarget = false;

        // Unity Button component
        Button btn = btnObj.AddComponent<Button>();

        // Text child
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = Mathf.RoundToInt(height * 0.38f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        // Layout element
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;

        // TarotButton component
        TarotButton tarotBtn = btnObj.AddComponent<TarotButton>();
        SerializedObject tarotSO = new SerializedObject(tarotBtn);
        tarotSO.FindProperty("buttonBackground").objectReferenceValue = bgImg;
        tarotSO.FindProperty("buttonBorder").objectReferenceValue = borderImg;
        tarotSO.FindProperty("buttonLabel").objectReferenceValue = tmp;
        tarotSO.FindProperty("variant").enumValueIndex = (int)variant;
        tarotSO.ApplyModifiedProperties();

        return btnObj;
    }

    /// <summary>
    /// Create a small icon-style button with TarotButton styling.
    /// </summary>
    private static GameObject CreateSmallButton(Transform parent, string name, string label,
        float width, float height)
    {
        return CreateStyledButton(parent, name, label, width, height, ButtonVariant.Secondary);
    }

    /// <summary>
    /// Create a toggle-style button (for difficulty/player count selectors).
    /// Compact size, no TarotButton (MainMenuVisual handles styling).
    /// </summary>
    private static GameObject CreateToggleButton(Transform parent, string name, string label,
        float width, float height)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = btnObj.AddComponent<Image>();
        img.color = DimPurple;
        img.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = DimPurple;
        colors.highlightedColor = DimPurple * 1.2f;
        colors.pressedColor = DimPurple * 0.8f;
        colors.selectedColor = DimPurple * 1.1f;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        btn.colors = colors;

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;

        // Text
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 2);
        textRect.offsetMax = new Vector2(-4, -2);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = SubtitleGray;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        return btnObj;
    }

    // ===================== SHARED HELPERS =====================

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
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
