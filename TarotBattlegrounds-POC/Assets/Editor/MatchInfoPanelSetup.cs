#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script to set up the Match Info scoreboard panel in the Game scene.
/// Run via menu: Tools/Game/Setup Match Info Panel
/// </summary>
public class MatchInfoPanelSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Match Info Panel")]
    public static void SetupMatchInfoPanel()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Game"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not Game. Open Game scene first?\n\n" +
                "(This will save the current scene.)",
                "Open Game", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }

        if (!EditorUtility.DisplayDialog("Setup Match Info Panel",
            "This will create:\n" +
            "  - MatchTracker GameObject\n" +
            "  - Fullscreen Match Info panel with scrolling\n" +
            "  - Toggle 'i' button (top-right)\n\n" +
            "And wire references on GameUIManager.\nContinue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas().GetComponent<Canvas>();

        MatchTracker tracker = Object.FindObjectOfType<MatchTracker>();
        if (tracker == null)
        {
            GameObject trackerObj = new GameObject("MatchTracker");
            tracker = trackerObj.AddComponent<MatchTracker>();
            Undo.RegisterCreatedObjectUndo(trackerObj, "Create MatchTracker");
        }

        RemoveOld(canvas.transform);

        GameObject matchInfoObj = CreateMatchInfoUI(canvas.transform);

        GameUIManager guiManager = Object.FindObjectOfType<GameUIManager>();
        if (guiManager != null)
        {
            Undo.RegisterCompleteObjectUndo(guiManager, "Wire MatchInfoUI");
            SerializedObject so = new SerializedObject(guiManager);
            SerializedProperty prop = so.FindProperty("matchInfoUI");
            if (prop != null)
            {
                prop.objectReferenceValue = matchInfoObj.GetComponent<MatchInfoUI>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(guiManager);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Match Info Setup Complete",
            "Created fullscreen Match Info panel.\n" +
            "Save the scene to keep changes.", "OK");
    }

    private static void RemoveOld(Transform canvasTransform)
    {
        string[] oldNames = { "MatchInfoPanel", "MatchInfoToggleButton" };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
        foreach (var existing in Object.FindObjectsOfType<MatchInfoUI>())
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

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

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return canvasObj;
    }

    private static GameObject CreateMatchInfoUI(Transform canvasTransform)
    {
        // Root holder
        GameObject holder = new GameObject("MatchInfoPanel");
        holder.transform.SetParent(canvasTransform, false);
        RectTransform holderRect = holder.AddComponent<RectTransform>();
        holderRect.anchorMin = Vector2.zero;
        holderRect.anchorMax = Vector2.one;
        holderRect.offsetMin = Vector2.zero;
        holderRect.offsetMax = Vector2.zero;

        MatchInfoUI matchInfoUI = holder.AddComponent<MatchInfoUI>();

        // === Toggle Button (top-right corner, always visible) ===
        GameObject toggleBtn = CreateButton(canvasTransform, "MatchInfoToggleButton", "i", 80, 80);
        RectTransform toggleRect = toggleBtn.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1, 1);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.pivot = new Vector2(1, 1);
        toggleRect.anchoredPosition = new Vector2(-10, -10);
        SetButtonColor(toggleBtn, new Color(0.3f, 0.3f, 0.5f, 0.9f));

        TMP_Text toggleText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (toggleText != null)
        {
            toggleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            toggleText.fontSize = 28;
        }

        // === Fullscreen dismiss overlay ===
        GameObject overlay = new GameObject("DismissOverlay");
        overlay.transform.SetParent(holder.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.5f);
        Button overlayBtn = overlay.AddComponent<Button>();
        ColorBlock overlayColors = overlayBtn.colors;
        overlayColors.normalColor = Color.white;
        overlayColors.highlightedColor = Color.white;
        overlayColors.pressedColor = Color.white;
        overlayColors.selectedColor = Color.white;
        overlayBtn.colors = overlayColors;

        // === Main panel (nearly fullscreen, centered) ===
        GameObject panel = new GameObject("InfoPanel");
        panel.transform.SetParent(holder.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f);
        panelRect.anchorMax = new Vector2(0.95f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.05f, 0.1f, 0.96f);

        // === Scroll View inside panel ===
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform svRect = scrollView.AddComponent<RectTransform>();
        svRect.anchorMin = Vector2.zero;
        svRect.anchorMax = Vector2.one;
        svRect.offsetMin = new Vector2(10, 10);
        svRect.offsetMax = new Vector2(-10, -10);
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
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
        vlg.spacing = 12;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRect;
        scrollRect.content = contentRect;

        // === Content sections ===
        // Title
        GameObject titleObj = CreateText(content.transform, "TitleText", "Match Status", 42,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Turn / Phase / Alive
        GameObject turnObj = CreateText(content.transform, "TurnText", "Turn 1  |  Phase: Recruit  |  Alive: 4", 30,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        CreateSeparator(content.transform);

        // Section: Player Statuses
        CreateText(content.transform, "StatusHeader", "PLAYERS", 28,
            FontStyles.Bold, new Color(0.6f, 0.8f, 1f), TextAlignmentOptions.Left);

        GameObject statusObj = CreateText(content.transform, "PlayerStatusText",
            "  P1 (You)  HP: 40  |  Tier 1  |  Coins: 3  |  Hand: 0  |  Board: 0/7\n" +
            "  P2 (AI)   HP: 40  |  Tier 1  |  Coins: 3  |  Hand: 0  |  Board: 0/7",
            24, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Left);

        CreateSeparator(content.transform);

        // Section: Boards
        CreateText(content.transform, "BoardsHeader", "BOARDS", 28,
            FontStyles.Bold, new Color(0.6f, 1f, 0.8f), TextAlignmentOptions.Left);

        GameObject boardsObj = CreateText(content.transform, "BoardsText",
            "  P1 Board: (empty)\n  P2 Board: (empty)",
            22, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left);

        CreateSeparator(content.transform);

        // Section: Last Round
        CreateText(content.transform, "LastRoundHeader", "LAST ROUND", 28,
            FontStyles.Bold, new Color(1f, 0.7f, 0.7f), TextAlignmentOptions.Left);

        GameObject lastRoundObj = CreateText(content.transform, "LastRoundText",
            "No battles yet", 22, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left);

        CreateSeparator(content.transform);

        // Section: History
        CreateText(content.transform, "HistoryHeader", "BATTLE HISTORY", 28,
            FontStyles.Bold, new Color(1f, 0.85f, 0.6f), TextAlignmentOptions.Left);

        GameObject historyObj = CreateText(content.transform, "HistoryText",
            "", 20, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Left);

        // === Wire serialized fields ===
        SerializedObject so = new SerializedObject(matchInfoUI);
        so.FindProperty("infoPanel").objectReferenceValue = panel;
        so.FindProperty("dismissOverlay").objectReferenceValue = overlayBtn;
        so.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TMP_Text>();
        so.FindProperty("turnText").objectReferenceValue = turnObj.GetComponent<TMP_Text>();
        so.FindProperty("playerStatusText").objectReferenceValue = statusObj.GetComponent<TMP_Text>();
        so.FindProperty("boardsText").objectReferenceValue = boardsObj.GetComponent<TMP_Text>();
        so.FindProperty("lastRoundText").objectReferenceValue = lastRoundObj.GetComponent<TMP_Text>();
        so.FindProperty("historyText").objectReferenceValue = historyObj.GetComponent<TMP_Text>();
        so.FindProperty("toggleButton").objectReferenceValue = toggleBtn.GetComponent<Button>();
        so.FindProperty("toggleButtonText").objectReferenceValue = toggleText;
        so.ApplyModifiedProperties();

        // Start hidden
        overlay.SetActive(false);
        panel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(holder, "Create MatchInfoPanel");
        Undo.RegisterCreatedObjectUndo(toggleBtn, "Create MatchInfoToggleButton");

        return holder;
    }

    // ===================== HELPERS =====================

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
        le.flexibleHeight = 1;

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

    private static GameObject CreateSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);
        sep.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
        sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.minHeight = 1;
        le.preferredHeight = 1;
        return sep;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return font;
    }
}
#endif
