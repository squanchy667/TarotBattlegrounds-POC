#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script to set up the MainMenu scene with Solo/Multiplayer panel navigation.
/// Run via menu: Tools/Game/Setup Main Menu UI
/// </summary>
public class MainMenuSceneSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Main Menu UI")]
    public static void SetupMainMenuUI()
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

        if (!EditorUtility.DisplayDialog("Setup Main Menu UI",
            "This will create MainPanel (Solo/Multiplayer/Quit) and " +
            "SoloPanel (4/6/8 players + difficulty + Play/Back) in the MainMenu scene " +
            "and wire all references on MainMenuManager.\n\nContinue?",
            "Create", "Cancel"))
            return;

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = CreateCanvas();
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Find or create MainMenuManager
        MainMenuManager manager = Object.FindObjectOfType<MainMenuManager>();
        if (manager == null)
        {
            manager = canvas.gameObject.AddComponent<MainMenuManager>();
        }

        Undo.RegisterCompleteObjectUndo(manager, "Setup Main Menu UI");
        SerializedObject so = new SerializedObject(manager);

        // Remove old UI children that conflict
        RemoveOldPanels(canvas.transform);

        // Create panels
        GameObject mainPanel = CreateMainPanel(canvas.transform, so);
        GameObject soloPanel = CreateSoloPanel(canvas.transform, so);

        // Solo panel starts inactive
        soloPanel.SetActive(false);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MainMenuSceneSetup] Main menu UI setup complete!");
        EditorUtility.DisplayDialog("Main Menu Setup Complete",
            "Created:\n" +
            "  - MainPanel (Solo / Multiplayer / Quit)\n" +
            "  - SoloPanel (4/6/8 players + Difficulty + Play/Back)\n\n" +
            "All references wired on MainMenuManager.\n" +
            "Save the scene to keep changes.", "OK");
    }

    // ===================== CLEANUP =====================

    private static void RemoveOldPanels(Transform canvasTransform)
    {
        string[] oldNames = { "MainPanel", "SoloPanel" };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[MainMenuSceneSetup] Removed old {name}");
            }
        }
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

        // EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        return canvasObj;
    }

    // ===================== MAIN PANEL =====================

    private static GameObject CreateMainPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = CreatePanel(parent, "MainPanel");
        so.FindProperty("mainPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = CreateCenteredContainer(panel.transform, "Content", 500, 450);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 25;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(50, 50, 40, 40);

        // Title
        CreateText(content.transform, "TitleText", "Tarot Battlegrounds", 42,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Subtitle
        CreateText(content.transform, "SubtitleText", "Auto Battler", 20,
            FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(content.transform, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.minHeight = 20;

        // Solo button
        GameObject soloBtn = CreateButton(content.transform, "SoloButton", "Solo", 300, 60);
        so.FindProperty("soloButton").objectReferenceValue = soloBtn.GetComponent<Button>();
        SetButtonColor(soloBtn, new Color(0.2f, 0.45f, 0.2f));

        // Multiplayer button
        GameObject mpBtn = CreateButton(content.transform, "MultiplayerButton", "Multiplayer", 300, 60);
        so.FindProperty("multiplayerButton").objectReferenceValue = mpBtn.GetComponent<Button>();
        SetButtonColor(mpBtn, new Color(0.2f, 0.3f, 0.5f));

        // Quit button
        GameObject quitBtn = CreateButton(content.transform, "QuitButton", "Quit", 300, 60);
        so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        SetButtonColor(quitBtn, new Color(0.45f, 0.2f, 0.2f));

        return panel;
    }

    // ===================== SOLO PANEL =====================

    private static GameObject CreateSoloPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = CreatePanel(parent, "SoloPanel");
        so.FindProperty("soloPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = CreateCenteredContainer(panel.transform, "Content", 550, 500);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(40, 40, 30, 30);

        // Title
        CreateText(content.transform, "SoloTitle", "Solo Game", 32,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Player count label
        CreateText(content.transform, "PlayerCountLabel", "Number of Players", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        // Player count buttons row
        GameObject countRow = CreateHorizontalRow(content.transform, "PlayerCountRow", 20);

        GameObject btn4 = CreateButton(countRow.transform, "Players4Button", "4 Players", 150, 55);
        so.FindProperty("players4Button").objectReferenceValue = btn4.GetComponent<Button>();
        SetButtonColor(btn4, new Color(0.4f, 0.8f, 0.4f)); // Green = default selected

        GameObject btn6 = CreateButton(countRow.transform, "Players6Button", "6 Players", 150, 55);
        so.FindProperty("players6Button").objectReferenceValue = btn6.GetComponent<Button>();

        GameObject btn8 = CreateButton(countRow.transform, "Players8Button", "8 Players", 150, 55);
        so.FindProperty("players8Button").objectReferenceValue = btn8.GetComponent<Button>();

        // Difficulty label
        CreateText(content.transform, "DifficultyLabel", "AI Difficulty", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        // Difficulty dropdown
        GameObject diffDropdown = CreateDropdown(content.transform, "DifficultyDropdown", 250, 45);
        so.FindProperty("difficultyDropdown").objectReferenceValue =
            diffDropdown.GetComponent<TMP_Dropdown>();

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(content.transform, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.minHeight = 10;

        // Bottom buttons row
        GameObject bottomRow = CreateHorizontalRow(content.transform, "BottomRow", 30);

        GameObject backBtn = CreateButton(bottomRow.transform, "BackButton", "Back", 150, 55);
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        SetButtonColor(backBtn, new Color(0.45f, 0.2f, 0.2f));

        GameObject playBtn = CreateButton(bottomRow.transform, "PlayButton", "Play", 200, 55);
        so.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();
        SetButtonColor(playBtn, new Color(0.2f, 0.45f, 0.2f));

        return panel;
    }

    // ===================== HELPER METHODS =====================

    private static void SetButtonColor(GameObject btnObj, Color color)
    {
        Image img = btnObj.GetComponent<Image>();
        if (img != null) img.color = color;

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            colors.selectedColor = color * 1.1f;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            btn.colors = colors;
        }
    }

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
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        return btnObj;
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
        labelTmp.text = "Medium";
        labelTmp.fontSize = 18;
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

        item.AddComponent<Toggle>();

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
        itemTmp.fontSize = 18;
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
        le.minHeight = 60;

        return row;
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
