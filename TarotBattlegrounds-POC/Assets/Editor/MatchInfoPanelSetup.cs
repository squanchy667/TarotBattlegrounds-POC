#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

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
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();

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

        // === Toggle Button (top-right, LEFT of Settings gear so they don't stack) ===
        // Settings gear is at (-16,-16) size 72; leave a gap: 16 + 72 + 12 = 100 from right.
        GameObject toggleBtn = EditorUiFactory.CreateButton(canvasTransform, "MatchInfoToggleButton", "i", 72, 72,
            labelFontSize: 22, addLayoutElement: false);
        RectTransform toggleRect = toggleBtn.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1, 1);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.pivot = new Vector2(1, 1);
        toggleRect.anchoredPosition = new Vector2(-100, -16);
        SetButtonColor(toggleBtn, Tokens.WithAlpha(Tokens.CharredWood, 0.9f));

        TMP_Text toggleText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (toggleText != null)
        {
            toggleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            toggleText.fontSize = Tokens.TextH3;
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
        overlayImg.color = Tokens.WithAlpha(Tokens.Ash, 0.5f);
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
        panelBg.color = Tokens.WithAlpha(Tokens.Umber, 0.96f);

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
        viewport.AddComponent<Image>().color = Tokens.Umber;

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
        GameObject titleObj = EditorUiFactory.CreateText(content.transform, "TitleText", "Match Status", (int)Tokens.TextH2,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Center, flexibleHeight: 1f);

        // Turn / Phase / Alive
        GameObject turnObj = EditorUiFactory.CreateText(content.transform, "TurnText", "Turn 1  |  Phase: Recruit  |  Alive: 4", (int)Tokens.TextH3,
            FontStyles.Normal, Tokens.BoneBright, TextAlignmentOptions.Center, flexibleHeight: 1f);

        CreateSeparator(content.transform);

        // Section: Player Statuses
        EditorUiFactory.CreateText(content.transform, "StatusHeader", "PLAYERS", (int)Tokens.TextH3,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Left, flexibleHeight: 1f);

        GameObject statusObj = EditorUiFactory.CreateText(content.transform, "PlayerStatusText",
            "  P1 (You)  HP: 40  |  Tier 1  |  Coins: 3  |  Hand: 0  |  Board: 0/7\n" +
            "  P2 (AI)   HP: 40  |  Tier 1  |  Coins: 3  |  Hand: 0  |  Board: 0/7",
            (int)Tokens.TextBody, FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Left, flexibleHeight: 1f);

        CreateSeparator(content.transform);

        // Section: Boards
        EditorUiFactory.CreateText(content.transform, "BoardsHeader", "BOARDS", (int)Tokens.TextH3,
            FontStyles.Bold, Tokens.Ember, TextAlignmentOptions.Left, flexibleHeight: 1f); // was green; no green in design system

        GameObject boardsObj = EditorUiFactory.CreateText(content.transform, "BoardsText",
            "  P1 Board: (empty)\n  P2 Board: (empty)",
            (int)Tokens.TextBody, FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Left, flexibleHeight: 1f);

        CreateSeparator(content.transform);

        // Section: Last Round
        EditorUiFactory.CreateText(content.transform, "LastRoundHeader", "LAST ROUND", (int)Tokens.TextH3,
            FontStyles.Bold, Tokens.Blood, TextAlignmentOptions.Left, flexibleHeight: 1f);

        GameObject lastRoundObj = EditorUiFactory.CreateText(content.transform, "LastRoundText",
            "No battles yet", (int)Tokens.TextBody, FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Left, flexibleHeight: 1f);

        CreateSeparator(content.transform);

        // Section: History
        EditorUiFactory.CreateText(content.transform, "HistoryHeader", "BATTLE HISTORY", (int)Tokens.TextH3,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Left, flexibleHeight: 1f);

        GameObject historyObj = EditorUiFactory.CreateText(content.transform, "HistoryText",
            "", (int)Tokens.TextCaption, FontStyles.Normal, Tokens.BoneDim, TextAlignmentOptions.Left, flexibleHeight: 1f);

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
            colors.disabledColor = Tokens.WithAlpha(Tokens.BoneDim, 0.5f);
            btn.colors = colors;
        }
    }

    private static GameObject CreateSeparator(Transform parent)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);
        sep.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
        sep.AddComponent<Image>().color = Tokens.WithAlpha(Tokens.StoneEdge, 0.12f);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.minHeight = 1;
        le.preferredHeight = 1;
        return sep;
    }
}
#endif
