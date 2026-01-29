#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script that auto-creates and wires all Phase P UI elements in the Game scene.
/// Run via menu: Tools/Game/Setup Phase P UI
/// </summary>
public class PhasePUISetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Phase P UI")]
    public static void SetupPhasePUI()
    {
        // Find the Canvas in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in scene. Please open the Game scene first.", "OK");
            return;
        }

        // Find GameUIManager
        GameUIManager gameUIManager = FindObjectOfType<GameUIManager>();
        if (gameUIManager == null)
        {
            EditorUtility.DisplayDialog("Error", "No GameUIManager found in scene.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(gameUIManager, "Setup Phase P UI");

        bool anyCreated = false;

        // 1. GameBackgroundImage
        anyCreated |= SetupGameBackgroundImage(canvas.transform, gameUIManager);

        // 2. EndTurnButton + FreezeShopButton in ActionsButtonsPanel
        anyCreated |= SetupActionButtons(canvas.transform, gameUIManager);

        // 3. Wire existing button texts
        anyCreated |= WireExistingButtonTexts(gameUIManager);

        // 4. DiscoveryPanel
        anyCreated |= SetupDiscoveryUI(canvas.transform);

        // 5. GameOverPanel
        anyCreated |= SetupGameOverUI(canvas.transform);

        // 6. Fix sibling order
        FixSiblingOrder(canvas.transform);

        EditorUtility.SetDirty(gameUIManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[PhasePUISetup] Phase P UI setup complete!");
        EditorUtility.DisplayDialog("Success", "Phase P UI setup complete!\n\nCreated/wired: Background, Buttons, GameOverUI, DiscoveryUI.", "OK");
    }

    // ======================================================================
    // 1. GAME BACKGROUND IMAGE
    // ======================================================================

    private static bool SetupGameBackgroundImage(Transform canvasTransform, GameUIManager manager)
    {
        Transform existing = canvasTransform.Find("GameBackgroundImage");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("GameBackgroundImage Exists",
                "GameBackgroundImage already exists. Replace it?", "Replace", "Skip"))
            {
                // Still wire it
                WireManagerField(manager, "gameBackgroundImage", existing.GetComponent<Image>());
                return false;
            }
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject bgObj = new GameObject("GameBackgroundImage");
        Undo.RegisterCreatedObjectUndo(bgObj, "Create GameBackgroundImage");
        bgObj.transform.SetParent(canvasTransform, false);
        bgObj.transform.SetAsFirstSibling();

        RectTransform rect = bgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = bgObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.12f, 0.18f, 1f);
        img.raycastTarget = false;

        WireManagerField(manager, "gameBackgroundImage", img);
        return true;
    }

    // ======================================================================
    // 2. END TURN + FREEZE SHOP BUTTONS
    // ======================================================================

    private static bool SetupActionButtons(Transform canvasTransform, GameUIManager manager)
    {
        // Find ActionsButtonsPanel
        Transform actionsPanel = FindDeep(canvasTransform, "ActionsButtonsPanel");
        if (actionsPanel == null)
        {
            Debug.LogWarning("[PhasePUISetup] ActionsButtonsPanel not found in Canvas hierarchy.");
            return false;
        }

        bool created = false;

        // Widen panel from 900 to 1200
        RectTransform panelRect = actionsPanel.GetComponent<RectTransform>();
        if (panelRect != null && panelRect.sizeDelta.x < 1200)
        {
            Undo.RecordObject(panelRect, "Widen ActionsButtonsPanel");
            panelRect.sizeDelta = new Vector2(1200, panelRect.sizeDelta.y);
        }

        // EndTurnButton
        Transform existingEnd = actionsPanel.Find("EndTurnButton");
        if (existingEnd != null)
        {
            if (!EditorUtility.DisplayDialog("EndTurnButton Exists",
                "EndTurnButton already exists. Replace it?", "Replace", "Skip"))
            {
                WireButton(manager, "endTurnButton", "endTurnButtonText", existingEnd.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(existingEnd.gameObject);
                existingEnd = null;
            }
        }
        if (existingEnd == null)
        {
            GameObject endBtn = CreateButton(actionsPanel, "EndTurnButton", "End Turn", 130, 50);
            WireButton(manager, "endTurnButton", "endTurnButtonText", endBtn);
            created = true;
        }

        // FreezeShopButton
        Transform existingFreeze = actionsPanel.Find("FreezeShopButton");
        if (existingFreeze != null)
        {
            if (!EditorUtility.DisplayDialog("FreezeShopButton Exists",
                "FreezeShopButton already exists. Replace it?", "Replace", "Skip"))
            {
                WireButton(manager, "freezeShopButton", "freezeShopButtonText", existingFreeze.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(existingFreeze.gameObject);
                existingFreeze = null;
            }
        }
        if (existingFreeze == null)
        {
            GameObject freezeBtn = CreateButton(actionsPanel, "FreezeShopButton", "Freeze", 130, 50);
            WireButton(manager, "freezeShopButton", "freezeShopButtonText", freezeBtn);
            created = true;
        }

        return created;
    }

    // ======================================================================
    // 3. WIRE EXISTING BUTTON TEXTS
    // ======================================================================

    private static bool WireExistingButtonTexts(GameUIManager manager)
    {
        SerializedObject so = new SerializedObject(manager);

        bool wired = false;
        wired |= WireExistingButtonText(so, manager, "buyButton", "buyButtonText");
        wired |= WireExistingButtonText(so, manager, "sellButton", "sellButtonText");
        wired |= WireExistingButtonText(so, manager, "playCardButton", "playButtonText");
        wired |= WireExistingButtonText(so, manager, "refreshButton", "refreshButtonText");
        wired |= WireExistingButtonText(so, manager, "upgradeButton", "upgradeButtonText");

        so.ApplyModifiedProperties();
        return wired;
    }

    private static bool WireExistingButtonText(SerializedObject so, GameUIManager manager,
        string buttonFieldName, string textFieldName)
    {
        SerializedProperty textProp = so.FindProperty(textFieldName);
        if (textProp == null) return false;

        // Already wired
        if (textProp.objectReferenceValue != null) return false;

        SerializedProperty buttonProp = so.FindProperty(buttonFieldName);
        if (buttonProp == null || buttonProp.objectReferenceValue == null) return false;

        Button btn = buttonProp.objectReferenceValue as Button;
        if (btn == null) return false;

        TMP_Text tmpText = btn.GetComponentInChildren<TMP_Text>();
        if (tmpText == null) return false;

        textProp.objectReferenceValue = tmpText;
        return true;
    }

    // ======================================================================
    // 4. DISCOVERY UI
    // ======================================================================

    private static bool SetupDiscoveryUI(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("DiscoveryUIRoot");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("DiscoveryUIRoot Exists",
                "DiscoveryUIRoot already exists. Replace it?", "Replace", "Skip"))
                return false;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // DiscoveryUIRoot (always active, has DiscoveryUI component)
        GameObject root = new GameObject("DiscoveryUIRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create DiscoveryUIRoot");
        root.transform.SetParent(canvasTransform, false);
        root.AddComponent<RectTransform>();

        DiscoveryUI discoveryUI = root.AddComponent<DiscoveryUI>();
        SerializedObject so = new SerializedObject(discoveryUI);

        // DiscoveryPanel (starts inactive, full-screen overlay)
        GameObject panel = new GameObject("DiscoveryPanel");
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);

        so.FindProperty("discoveryPanel").objectReferenceValue = panel;

        // ContentPanel (700x400 centered)
        GameObject content = new GameObject("ContentPanel");
        content.transform.SetParent(panel.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(700, 400);

        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.padding = new RectOffset(25, 25, 25, 25);
        contentVLG.spacing = 20;
        contentVLG.childAlignment = TextAnchor.UpperCenter;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;

        // TitleText
        GameObject titleObj = CreateTextObject(content.transform, "TitleText",
            "Triple! Choose a Card:", 28, FontStyles.Bold, new Color(1f, 0.84f, 0f));
        TMP_Text titleTMP = titleObj.GetComponent<TMP_Text>();
        titleTMP.alignment = TextAlignmentOptions.Center;
        so.FindProperty("titleText").objectReferenceValue = titleTMP;

        // CardContainer
        GameObject cardContainer = new GameObject("CardContainer");
        cardContainer.transform.SetParent(content.transform, false);
        cardContainer.AddComponent<RectTransform>();

        HorizontalLayoutGroup cardHLG = cardContainer.AddComponent<HorizontalLayoutGroup>();
        cardHLG.spacing = 20;
        cardHLG.childAlignment = TextAnchor.MiddleCenter;
        cardHLG.childControlWidth = false;
        cardHLG.childControlHeight = false;
        cardHLG.childForceExpandWidth = false;
        cardHLG.childForceExpandHeight = false;

        LayoutElement cardLE = cardContainer.AddComponent<LayoutElement>();
        cardLE.minHeight = 250;

        so.FindProperty("cardContainer").objectReferenceValue = cardContainer.transform;

        // Load CardDisplay prefab
        GameObject cardDisplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Preferbs/UI/CardDisplay.prefab");
        if (cardDisplayPrefab != null)
        {
            so.FindProperty("cardDisplayPrefab").objectReferenceValue = cardDisplayPrefab;
        }
        else
        {
            Debug.LogWarning("[PhasePUISetup] CardDisplay.prefab not found at Assets/Preferbs/UI/CardDisplay.prefab");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(discoveryUI);

        return true;
    }

    // ======================================================================
    // 5. GAME OVER UI
    // ======================================================================

    private static bool SetupGameOverUI(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("GameOverUIRoot");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("GameOverUIRoot Exists",
                "GameOverUIRoot already exists. Replace it?", "Replace", "Skip"))
                return false;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // GameOverUIRoot (always active, has GameOverUI component)
        GameObject root = new GameObject("GameOverUIRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create GameOverUIRoot");
        root.transform.SetParent(canvasTransform, false);
        root.AddComponent<RectTransform>();

        GameOverUI gameOverUI = root.AddComponent<GameOverUI>();
        SerializedObject so = new SerializedObject(gameOverUI);

        // GameOverPanel (starts inactive via Awake, full-screen dark overlay)
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(root.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        so.FindProperty("gameOverPanel").objectReferenceValue = panel;

        // ContentPanel (500x450 centered)
        GameObject content = new GameObject("ContentPanel");
        content.transform.SetParent(panel.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(500, 450);

        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
        contentVLG.padding = new RectOffset(30, 30, 30, 30);
        contentVLG.spacing = 20;
        contentVLG.childAlignment = TextAnchor.UpperCenter;
        contentVLG.childControlWidth = true;
        contentVLG.childControlHeight = true;
        contentVLG.childForceExpandWidth = true;
        contentVLG.childForceExpandHeight = false;

        // TitleText — "Game Over", 36pt bold gold
        GameObject titleObj = CreateTextObject(content.transform, "TitleText",
            "Game Over", 36, FontStyles.Bold, new Color(1f, 0.84f, 0f));
        TMP_Text titleTMP = titleObj.GetComponent<TMP_Text>();
        titleTMP.alignment = TextAlignmentOptions.Center;
        so.FindProperty("titleText").objectReferenceValue = titleTMP;

        // PlacementText — "Victory!", 28pt bold white
        GameObject placementObj = CreateTextObject(content.transform, "PlacementText",
            "Victory!", 28, FontStyles.Bold, Color.white);
        TMP_Text placementTMP = placementObj.GetComponent<TMP_Text>();
        placementTMP.alignment = TextAlignmentOptions.Center;
        so.FindProperty("placementText").objectReferenceValue = placementTMP;

        // StandingsText — 18pt, with LayoutElement(minHeight=100)
        GameObject standingsObj = CreateTextObject(content.transform, "StandingsText",
            "Standings...", 18, FontStyles.Normal, Color.white);
        TMP_Text standingsTMP = standingsObj.GetComponent<TMP_Text>();
        standingsTMP.alignment = TextAlignmentOptions.Center;
        LayoutElement standingsLE = standingsObj.AddComponent<LayoutElement>();
        standingsLE.minHeight = 100;
        so.FindProperty("standingsText").objectReferenceValue = standingsTMP;

        // ButtonsRow (HorizontalLayoutGroup spacing=20)
        GameObject buttonsRow = new GameObject("ButtonsRow");
        buttonsRow.transform.SetParent(content.transform, false);
        buttonsRow.AddComponent<RectTransform>();

        HorizontalLayoutGroup rowHLG = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 20;
        rowHLG.childAlignment = TextAnchor.MiddleCenter;
        rowHLG.childControlWidth = false;
        rowHLG.childControlHeight = false;
        rowHLG.childForceExpandWidth = false;
        rowHLG.childForceExpandHeight = false;

        // PlayAgainButton (180x50)
        GameObject playAgainBtn = CreateButton(buttonsRow.transform, "PlayAgainButton", "Play Again", 180, 50);
        so.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn.GetComponent<Button>();
        so.FindProperty("playAgainButtonText").objectReferenceValue = playAgainBtn.GetComponentInChildren<TMP_Text>();

        // QuitToMenuButton (180x50)
        GameObject quitBtn = CreateButton(buttonsRow.transform, "QuitToMenuButton", "Quit to Menu", 180, 50);
        so.FindProperty("quitToMenuButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        so.FindProperty("quitToMenuButtonText").objectReferenceValue = quitBtn.GetComponentInChildren<TMP_Text>();

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameOverUI);

        return true;
    }

    // ======================================================================
    // 6. SIBLING ORDER
    // ======================================================================

    private static void FixSiblingOrder(Transform canvasTransform)
    {
        // GameBackgroundImage should be index 0
        Transform bg = canvasTransform.Find("GameBackgroundImage");
        if (bg != null)
            bg.SetAsFirstSibling();

        // DiscoveryUIRoot near end
        Transform discovery = canvasTransform.Find("DiscoveryUIRoot");
        if (discovery != null)
            discovery.SetAsLastSibling();

        // GameOverUIRoot after discovery
        Transform gameOver = canvasTransform.Find("GameOverUIRoot");
        if (gameOver != null)
            gameOver.SetAsLastSibling();

        // CardTooltipUI always last
        Transform tooltip = canvasTransform.Find("CardTooltipUI");
        if (tooltip != null)
            tooltip.SetAsLastSibling();
    }

    // ======================================================================
    // HELPER METHODS
    // ======================================================================

    /// <summary>
    /// Create a TMP_Text object matching the pattern from TooltipPrefabGenerator.
    /// </summary>
    private static GameObject CreateTextObject(Transform parent, string name, string defaultText,
        int fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;

        // Try to find a font asset (same pattern as TooltipPrefabGenerator)
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
        if (font != null)
            tmp.font = font;

        return obj;
    }

    /// <summary>
    /// Create a button matching existing button pattern: Image(Sliced), Button, LayoutElement, child TMP_Text.
    /// </summary>
    private static GameObject CreateButton(Transform parent, string name, string label, float width, float height)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        // Image background (matching existing button pattern)
        Image img = btnObj.AddComponent<Image>();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        // Button component with standard color transitions
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        btn.colors = colors;

        // LayoutElement matching existing buttons
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 100;
        le.preferredWidth = 120;

        // Child text
        GameObject textObj = CreateTextObject(btnObj.transform, "Text (TMP)", label, 16,
            FontStyles.Normal, new Color(0.2f, 0.2f, 0.2f, 1f));
        TMP_Text tmpText = textObj.GetComponent<TMP_Text>();
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = false;

        // Make text stretch to fill button
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    /// <summary>
    /// Wire a Button + its text to GameUIManager fields via SerializedObject.
    /// </summary>
    private static void WireButton(GameUIManager manager, string buttonFieldName,
        string textFieldName, GameObject buttonObj)
    {
        SerializedObject so = new SerializedObject(manager);

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            SerializedProperty btnProp = so.FindProperty(buttonFieldName);
            if (btnProp != null)
                btnProp.objectReferenceValue = btn;
        }

        TMP_Text tmpText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            SerializedProperty textProp = so.FindProperty(textFieldName);
            if (textProp != null)
                textProp.objectReferenceValue = tmpText;
        }

        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Wire a single field on GameUIManager via SerializedObject.
    /// </summary>
    private static void WireManagerField(GameUIManager manager, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[PhasePUISetup] Field '{fieldName}' not found on GameUIManager.");
        }
    }

    /// <summary>
    /// Deep-find a child transform by name (recursive).
    /// </summary>
    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
