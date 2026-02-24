#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// UX20: Editor setup script for the Collection Viewer UI in the MainMenu scene.
/// Creates the full collection browser hierarchy: tribe filter tabs, tier filter buttons,
/// search bar, card grid with ScrollRect, and right-side detail panel.
/// Run via menu: Tools/Game/Setup Collection UI
/// </summary>
public class CollectionSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Collection UI")]
    public static void SetupCollectionUI()
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

        if (!EditorUtility.DisplayDialog("Setup Collection UI",
            "This will create or replace the CollectionPanel hierarchy in the MainMenu scene:\n\n" +
            "- Tribe filter tab row (All + 6 tribes)\n" +
            "- Tier filter star buttons (All + 6 tiers)\n" +
            "- Search input field\n" +
            "- Card grid with ScrollRect + GridLayoutGroup\n" +
            "- Right-side detail panel (30% width)\n" +
            "- Close button\n\n" +
            "All references will be wired on CollectionUI.\nContinue?",
            "Create", "Cancel"))
            return;

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = CreateCanvas();
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Find or create CollectionUI component
        TarotBattlegrounds.UI.CollectionUI collectionUI = Object.FindObjectOfType<TarotBattlegrounds.UI.CollectionUI>();
        if (collectionUI == null)
        {
            collectionUI = canvas.gameObject.AddComponent<TarotBattlegrounds.UI.CollectionUI>();
        }

        Undo.RegisterCompleteObjectUndo(collectionUI, "Setup Collection UI");
        SerializedObject so = new SerializedObject(collectionUI);

        // Remove old CollectionPanel if exists
        Transform existing = canvas.transform.Find("CollectionPanel");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            Debug.Log("[CollectionSetup] Removed old CollectionPanel");
        }

        // Create root CollectionPanel (fullscreen overlay)
        GameObject collectionPanel = CreateFullscreenPanel(canvas.transform, "CollectionPanel");
        so.FindProperty("collectionPanel").objectReferenceValue = collectionPanel;

        // Dark overlay background
        Image panelBg = collectionPanel.GetComponent<Image>();
        if (panelBg == null) panelBg = collectionPanel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.12f, 0.97f);
        panelBg.raycastTarget = false;

        // ===================== LEFT SECTION (70% for grid area) =====================
        GameObject leftSection = CreatePanel(collectionPanel.transform, "LeftSection",
            Vector2.zero, new Vector2(0.70f, 1f));

        // ===================== TOP BAR: Title + Close =====================
        GameObject topBar = CreateHorizontalRow(leftSection.transform, "TopBar", 10f,
            new RectOffset(20, 20, 10, 5));
        SetAnchors(topBar, new Vector2(0, 0.92f), Vector2.one);

        // Title
        CreateText(topBar.transform, "CollectionTitle", "Card Collection", 28,
            FontStyles.Bold, new Color(1f, 0.78f, 0.15f), TextAlignmentOptions.MidlineLeft);

        // Spacer to push close button right
        GameObject titleSpacer = new GameObject("TitleSpacer");
        titleSpacer.transform.SetParent(topBar.transform, false);
        titleSpacer.AddComponent<RectTransform>();
        LayoutElement titleSpacerLE = titleSpacer.AddComponent<LayoutElement>();
        titleSpacerLE.flexibleWidth = 1f;

        // Close button
        GameObject closeBtn = CreateButton(topBar.transform, "CloseButton", "X", 45, 40);
        SetButtonColor(closeBtn, new Color(0.6f, 0.15f, 0.15f));
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();

        // Card count
        GameObject countObj = CreateText(topBar.transform, "CardCountText", "0 / 0 cards", 16,
            FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.MidlineRight);
        so.FindProperty("cardCountText").objectReferenceValue = countObj.GetComponent<TMP_Text>();
        LayoutElement countLE = countObj.GetComponent<LayoutElement>();
        if (countLE == null) countLE = countObj.AddComponent<LayoutElement>();
        countLE.minWidth = 120f;

        // ===================== FILTER ROW 1: Tribe Filter Tabs =====================
        GameObject tribeRow = CreateHorizontalRow(leftSection.transform, "TribeFilterRow", 6f,
            new RectOffset(20, 20, 0, 0));
        SetAnchors(tribeRow, new Vector2(0, 0.85f), new Vector2(1f, 0.92f));

        // Label for tribe row
        CreateText(tribeRow.transform, "TribeLabel", "Tribe:", 14,
            FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);

        so.FindProperty("tribeFilterContainer").objectReferenceValue = tribeRow.transform;

        // ===================== FILTER ROW 2: Tier Filter + Search =====================
        GameObject tierRow = CreateHorizontalRow(leftSection.transform, "TierFilterRow", 6f,
            new RectOffset(20, 20, 0, 0));
        SetAnchors(tierRow, new Vector2(0, 0.78f), new Vector2(1f, 0.85f));

        // Label for tier row
        CreateText(tierRow.transform, "TierLabel", "Tier:", 14,
            FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);

        so.FindProperty("tierFilterContainer").objectReferenceValue = tierRow.transform;

        // Spacer between tier buttons and search
        GameObject filterSpacer = new GameObject("FilterSpacer");
        filterSpacer.transform.SetParent(tierRow.transform, false);
        filterSpacer.AddComponent<RectTransform>();
        LayoutElement filterSpacerLE = filterSpacer.AddComponent<LayoutElement>();
        filterSpacerLE.flexibleWidth = 1f;

        // Search input
        GameObject searchObj = CreateInputField(tierRow.transform, "SearchInput", "Search cards...", 200f, 34f);
        so.FindProperty("searchInput").objectReferenceValue = searchObj.GetComponent<TMP_InputField>();

        // ===================== CARD GRID (ScrollRect) =====================
        GameObject scrollArea = new GameObject("CardScrollArea");
        scrollArea.transform.SetParent(leftSection.transform, false);
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        SetAnchors(scrollArea, new Vector2(0, 0), new Vector2(1f, 0.78f));
        scrollRect.offsetMin = new Vector2(15f, 15f);
        scrollRect.offsetMax = new Vector2(-15f, -5f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollArea.transform, false);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.003f); // Nearly invisible for raycasting
        vpImg.raycastTarget = false;

        // Content (the grid)
        GameObject content = new GameObject("CardGridContent");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // GridLayoutGroup on content
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(130f, 180f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        // ContentSizeFitter for scrolling
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        so.FindProperty("cardGrid").objectReferenceValue = grid;
        so.FindProperty("cardGridContainer").objectReferenceValue = content.transform;

        // ScrollRect component
        ScrollRect sr = scrollArea.AddComponent<ScrollRect>();
        sr.content = contentRect;
        sr.viewport = vpRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 30f;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // Scrollbar
        GameObject scrollbar = CreateVerticalScrollbar(scrollArea.transform);
        sr.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();

        // ===================== RIGHT SECTION: Detail Panel (30%) =====================
        GameObject rightSection = CreatePanel(collectionPanel.transform, "DetailPanel",
            new Vector2(0.70f, 0f), Vector2.one);

        // Dark background for detail panel
        Image detailBg = rightSection.GetComponent<Image>();
        if (detailBg == null) detailBg = rightSection.AddComponent<Image>();
        detailBg.color = new Color(0.10f, 0.08f, 0.14f, 0.95f);
        detailBg.raycastTarget = false;

        RectTransform detailPanelRect = rightSection.GetComponent<RectTransform>();
        so.FindProperty("detailPanel").objectReferenceValue = detailPanelRect;

        // Detail content vertical layout
        GameObject detailContent = new GameObject("DetailContent");
        detailContent.transform.SetParent(rightSection.transform, false);
        RectTransform dcRect = detailContent.AddComponent<RectTransform>();
        dcRect.anchorMin = Vector2.zero;
        dcRect.anchorMax = Vector2.one;
        dcRect.offsetMin = new Vector2(15f, 15f);
        dcRect.offsetMax = new Vector2(-15f, -15f);

        VerticalLayoutGroup detailVLG = detailContent.AddComponent<VerticalLayoutGroup>();
        detailVLG.spacing = 15f;
        detailVLG.childAlignment = TextAnchor.UpperCenter;
        detailVLG.childControlWidth = true;
        detailVLG.childControlHeight = false;
        detailVLG.childForceExpandWidth = true;
        detailVLG.childForceExpandHeight = false;
        detailVLG.padding = new RectOffset(10, 10, 20, 20);

        // Detail header
        CreateText(detailContent.transform, "DetailHeader", "Card Details", 22,
            FontStyles.Bold, new Color(1f, 0.78f, 0.15f), TextAlignmentOptions.Center);

        // Separator line
        GameObject separator = new GameObject("Separator");
        separator.transform.SetParent(detailContent.transform, false);
        RectTransform sepRect = separator.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0, 2);
        Image sepImg = separator.AddComponent<Image>();
        sepImg.color = new Color(0.55f, 0.3f, 0.75f, 0.6f);
        sepImg.raycastTarget = false;
        LayoutElement sepLE = separator.AddComponent<LayoutElement>();
        sepLE.minHeight = 2f;
        sepLE.flexibleWidth = 1f;

        // Detail card display placeholder (CardDisplayUI will be created at runtime via prefab)
        // For now, create a placeholder area
        GameObject cardPlaceholder = new GameObject("DetailCardArea");
        cardPlaceholder.transform.SetParent(detailContent.transform, false);
        RectTransform cpRect = cardPlaceholder.AddComponent<RectTransform>();
        cpRect.sizeDelta = new Vector2(160f, 220f);
        Image cpBg = cardPlaceholder.AddComponent<Image>();
        cpBg.color = new Color(0.15f, 0.12f, 0.2f, 0.8f);
        cpBg.raycastTarget = false;
        LayoutElement cpLE = cardPlaceholder.AddComponent<LayoutElement>();
        cpLE.preferredWidth = 160f;
        cpLE.preferredHeight = 220f;

        // Detail name text
        GameObject detailNameObj = CreateText(detailContent.transform, "DetailName",
            "Select a card...", 18, FontStyles.Bold,
            new Color(1f, 0.78f, 0.15f), TextAlignmentOptions.Center);
        so.FindProperty("detailName").objectReferenceValue = detailNameObj.GetComponent<TMP_Text>();
        LayoutElement dnLE = detailNameObj.GetComponent<LayoutElement>();
        if (dnLE == null) dnLE = detailNameObj.AddComponent<LayoutElement>();
        dnLE.minHeight = 50f;

        // Detail abilities text
        GameObject detailAbilitiesObj = CreateText(detailContent.transform, "DetailAbilities",
            "", 15, FontStyles.Normal,
            Color.white, TextAlignmentOptions.Center);
        so.FindProperty("detailAbilities").objectReferenceValue = detailAbilitiesObj.GetComponent<TMP_Text>();
        LayoutElement daLE = detailAbilitiesObj.GetComponent<LayoutElement>();
        if (daLE == null) daLE = detailAbilitiesObj.AddComponent<LayoutElement>();
        daLE.minHeight = 60f;

        // TMP rich text for abilities
        TMP_Text abilitiesTMP = detailAbilitiesObj.GetComponent<TMP_Text>();
        if (abilitiesTMP != null) abilitiesTMP.richText = true;

        // Detail lore text
        GameObject detailLoreObj = CreateText(detailContent.transform, "DetailLore",
            "", 13, FontStyles.Italic,
            new Color(0.65f, 0.65f, 0.65f), TextAlignmentOptions.Center);
        so.FindProperty("detailLore").objectReferenceValue = detailLoreObj.GetComponent<TMP_Text>();
        LayoutElement dlLE = detailLoreObj.GetComponent<LayoutElement>();
        if (dlLE == null) dlLE = detailLoreObj.AddComponent<LayoutElement>();
        dlLE.minHeight = 40f;

        // Selected card info (legacy)
        GameObject infoObj = CreateText(detailContent.transform, "SelectedCardInfo",
            "", 12, FontStyles.Normal,
            new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);
        so.FindProperty("selectedCardInfo").objectReferenceValue = infoObj.GetComponent<TMP_Text>();

        // CollectionPanel starts inactive
        collectionPanel.SetActive(false);

        // Apply properties
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(collectionUI);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[CollectionSetup] Collection UI setup complete!");
        EditorUtility.DisplayDialog("Collection UI Setup Complete",
            "Created:\n" +
            "  - CollectionPanel (fullscreen overlay)\n" +
            "  - Tribe filter tab row (7 buttons)\n" +
            "  - Tier filter star buttons (7 buttons)\n" +
            "  - Search input field\n" +
            "  - Card grid with ScrollRect + GridLayoutGroup\n" +
            "  - Detail panel (right 30%)\n" +
            "  - Close button + card count\n\n" +
            "All references wired on CollectionUI.\n" +
            "Save the scene to keep changes.", "OK");
    }

    // ===================== HELPER METHODS =====================

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

    private static GameObject CreateFullscreenPanel(Transform parent, string name)
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

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return panel;
    }

    private static void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateHorizontalRow(Transform parent, string name, float spacing,
        RectOffset padding = null)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        if (padding != null) hlg.padding = padding;

        return row;
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
        tmp.richText = true;
        tmp.raycastTarget = false;

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
        tmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        return btnObj;
    }

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

    private static GameObject CreateInputField(Transform parent, string name,
        string placeholder, float width, float height)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.2f, 1f);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 2);
        taRect.offsetMax = new Vector2(-10, -2);
        textArea.AddComponent<RectMask2D>();

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
        phTmp.fontSize = 14;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) phTmp.font = font;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 14;
        textTmp.color = Color.white;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.raycastTarget = false;

        if (font != null) textTmp.font = font;

        // TMP_InputField
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = font;
        inputField.pointSize = 14;

        // Style the caret
        inputField.caretColor = new Color(1f, 0.78f, 0.15f);
        inputField.selectionColor = new Color(0.55f, 0.3f, 0.75f, 0.4f);

        return inputObj;
    }

    private static GameObject CreateVerticalScrollbar(Transform parent)
    {
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(parent, false);

        RectTransform rect = scrollbarObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 0.5f);
        rect.sizeDelta = new Vector2(10f, 0);
        rect.offsetMin = new Vector2(-10f, 0);
        rect.offsetMax = new Vector2(0, 0);

        Image trackImg = scrollbarObj.AddComponent<Image>();
        trackImg.color = new Color(0.15f, 0.12f, 0.2f, 0.5f);
        trackImg.raycastTarget = false;

        // Sliding area
        GameObject slidingArea = new GameObject("Sliding Area");
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform saRect = slidingArea.AddComponent<RectTransform>();
        saRect.anchorMin = Vector2.zero;
        saRect.anchorMax = Vector2.one;
        saRect.offsetMin = Vector2.zero;
        saRect.offsetMax = Vector2.zero;

        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(slidingArea.transform, false);
        RectTransform hRect = handle.AddComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(10f, 10f);

        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.55f, 0.3f, 0.75f, 0.7f);

        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.handleRect = hRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImg;

        ColorBlock scrollColors = scrollbar.colors;
        scrollColors.normalColor = new Color(0.55f, 0.3f, 0.75f, 0.7f);
        scrollColors.highlightedColor = new Color(0.65f, 0.4f, 0.85f, 0.9f);
        scrollColors.pressedColor = new Color(0.45f, 0.2f, 0.65f, 1f);
        scrollbar.colors = scrollColors;

        return scrollbarObj;
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
