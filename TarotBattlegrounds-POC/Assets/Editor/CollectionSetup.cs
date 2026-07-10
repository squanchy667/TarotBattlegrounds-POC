#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

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
            GameObject canvasObj = EditorUiFactory.CreateCanvas();
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
        GameObject collectionPanel = EditorUiFactory.CreateFullscreenPanel(canvas.transform, "CollectionPanel");
        so.FindProperty("collectionPanel").objectReferenceValue = collectionPanel;

        // Dark overlay background
        Image panelBg = collectionPanel.GetComponent<Image>();
        if (panelBg == null) panelBg = collectionPanel.AddComponent<Image>();
        panelBg.color = Tokens.WithAlpha(Tokens.Ash, 0.97f);
        panelBg.raycastTarget = false;

        // ===================== LEFT SECTION (70% for grid area) =====================
        GameObject leftSection = CreatePanel(collectionPanel.transform, "LeftSection",
            Vector2.zero, new Vector2(0.70f, 1f));

        // ===================== TOP BAR: Title + Close =====================
        GameObject topBar = EditorUiFactory.CreateHorizontalRow(leftSection.transform, "TopBar", 10f,
            childAlignment: TextAnchor.MiddleLeft, padding: new RectOffset(20, 20, 10, 5),
            addLayoutElement: false);
        SetAnchors(topBar, new Vector2(0, 0.92f), Vector2.one);

        // Title
        EditorUiFactory.CreateText(topBar.transform, "CollectionTitle", "Card Collection", (int)Tokens.TextH3,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.MidlineLeft, richText: true, raycastTarget: false);

        // Spacer to push close button right
        GameObject titleSpacer = new GameObject("TitleSpacer");
        titleSpacer.transform.SetParent(topBar.transform, false);
        titleSpacer.AddComponent<RectTransform>();
        LayoutElement titleSpacerLE = titleSpacer.AddComponent<LayoutElement>();
        titleSpacerLE.flexibleWidth = 1f;

        // Close button
        GameObject closeBtn = EditorUiFactory.CreateButton(topBar.transform, "CloseButton", "X", 45, 40,
            labelRaycastTarget: false);
        SetButtonColor(closeBtn, Tokens.Blood);
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();

        // Card count
        GameObject countObj = EditorUiFactory.CreateText(topBar.transform, "CardCountText", "0 / 0 cards", (int)Tokens.TextCaption,
            FontStyles.Normal, Tokens.BoneDim, TextAlignmentOptions.MidlineRight, richText: true, raycastTarget: false);
        so.FindProperty("cardCountText").objectReferenceValue = countObj.GetComponent<TMP_Text>();
        LayoutElement countLE = countObj.GetComponent<LayoutElement>();
        if (countLE == null) countLE = countObj.AddComponent<LayoutElement>();
        countLE.minWidth = 120f;

        // ===================== FILTER ROW 1: Tribe Filter Tabs =====================
        GameObject tribeRow = EditorUiFactory.CreateHorizontalRow(leftSection.transform, "TribeFilterRow", 6f,
            childAlignment: TextAnchor.MiddleLeft, padding: new RectOffset(20, 20, 0, 0),
            addLayoutElement: false);
        SetAnchors(tribeRow, new Vector2(0, 0.85f), new Vector2(1f, 0.92f));

        // Label for tribe row
        EditorUiFactory.CreateText(tribeRow.transform, "TribeLabel", "Tribe:", (int)Tokens.TextCaption,
            FontStyles.Bold, Tokens.Bone, TextAlignmentOptions.MidlineLeft, richText: true, raycastTarget: false);

        so.FindProperty("tribeFilterContainer").objectReferenceValue = tribeRow.transform;

        // ===================== FILTER ROW 2: Tier Filter + Search =====================
        GameObject tierRow = EditorUiFactory.CreateHorizontalRow(leftSection.transform, "TierFilterRow", 6f,
            childAlignment: TextAnchor.MiddleLeft, padding: new RectOffset(20, 20, 0, 0),
            addLayoutElement: false);
        SetAnchors(tierRow, new Vector2(0, 0.78f), new Vector2(1f, 0.85f));

        // Label for tier row
        EditorUiFactory.CreateText(tierRow.transform, "TierLabel", "Tier:", (int)Tokens.TextCaption,
            FontStyles.Bold, Tokens.Bone, TextAlignmentOptions.MidlineLeft, richText: true, raycastTarget: false);

        so.FindProperty("tierFilterContainer").objectReferenceValue = tierRow.transform;

        // Spacer between tier buttons and search
        GameObject filterSpacer = new GameObject("FilterSpacer");
        filterSpacer.transform.SetParent(tierRow.transform, false);
        filterSpacer.AddComponent<RectTransform>();
        LayoutElement filterSpacerLE = filterSpacer.AddComponent<LayoutElement>();
        filterSpacerLE.flexibleWidth = 1f;

        // Search input
        GameObject searchObj = EditorUiFactory.CreateInputFieldMasked(tierRow.transform, "SearchInput", "Search cards...", 200f, 34f);
        so.FindProperty("searchInput").objectReferenceValue = searchObj.GetComponent<TMP_InputField>();

        // ===================== CARD GRID (ScrollRect) =====================
        GameObject scrollArea = new GameObject("CardScrollArea");
        scrollArea.transform.SetParent(leftSection.transform, false);
        RectTransform scrollRect = scrollArea.AddComponent<RectTransform>();
        SetAnchors(scrollArea, new Vector2(0, 0), new Vector2(1f, 0.78f));
        scrollRect.offsetMin = new Vector2(Tokens.Space2, Tokens.Space2);
        scrollRect.offsetMax = new Vector2(-Tokens.Space2, -5f);

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
        vpImg.color = Tokens.WithAlpha(Tokens.Ash, 0.003f); // Nearly invisible for raycasting
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
        detailBg.color = Tokens.WithAlpha(Tokens.Umber, 0.95f);
        detailBg.raycastTarget = false;

        RectTransform detailPanelRect = rightSection.GetComponent<RectTransform>();
        so.FindProperty("detailPanel").objectReferenceValue = detailPanelRect;

        // Detail content vertical layout
        GameObject detailContent = new GameObject("DetailContent");
        detailContent.transform.SetParent(rightSection.transform, false);
        RectTransform dcRect = detailContent.AddComponent<RectTransform>();
        dcRect.anchorMin = Vector2.zero;
        dcRect.anchorMax = Vector2.one;
        dcRect.offsetMin = new Vector2(Tokens.Space2, Tokens.Space2);
        dcRect.offsetMax = new Vector2(-Tokens.Space2, -Tokens.Space2);

        VerticalLayoutGroup detailVLG = detailContent.AddComponent<VerticalLayoutGroup>();
        detailVLG.spacing = Tokens.Space2;
        detailVLG.childAlignment = TextAnchor.UpperCenter;
        detailVLG.childControlWidth = true;
        detailVLG.childControlHeight = false;
        detailVLG.childForceExpandWidth = true;
        detailVLG.childForceExpandHeight = false;
        detailVLG.padding = new RectOffset(10, 10, 20, 20);

        // Detail header
        EditorUiFactory.CreateText(detailContent.transform, "DetailHeader", "Card Details", (int)Tokens.TextBody,
            FontStyles.Bold, Tokens.BronzeBright, TextAlignmentOptions.Center, richText: true, raycastTarget: false);

        // Separator line
        GameObject separator = new GameObject("Separator");
        separator.transform.SetParent(detailContent.transform, false);
        RectTransform sepRect = separator.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0, 2);
        Image sepImg = separator.AddComponent<Image>();
        sepImg.color = Tokens.WithAlpha(Tokens.StoneEdge, 0.6f);
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
        cpBg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.8f);
        cpBg.raycastTarget = false;
        LayoutElement cpLE = cardPlaceholder.AddComponent<LayoutElement>();
        cpLE.preferredWidth = 160f;
        cpLE.preferredHeight = 220f;

        // Detail name text
        GameObject detailNameObj = EditorUiFactory.CreateText(detailContent.transform, "DetailName",
            "Select a card...", (int)Tokens.TextCaption, FontStyles.Bold,
            Tokens.BronzeBright, TextAlignmentOptions.Center, richText: true, raycastTarget: false);
        so.FindProperty("detailName").objectReferenceValue = detailNameObj.GetComponent<TMP_Text>();
        LayoutElement dnLE = detailNameObj.GetComponent<LayoutElement>();
        if (dnLE == null) dnLE = detailNameObj.AddComponent<LayoutElement>();
        dnLE.minHeight = 50f;

        // Detail abilities text
        GameObject detailAbilitiesObj = EditorUiFactory.CreateText(detailContent.transform, "DetailAbilities",
            "", (int)Tokens.TextCaption, FontStyles.Normal,
            Tokens.Bone, TextAlignmentOptions.Center, richText: true, raycastTarget: false);
        so.FindProperty("detailAbilities").objectReferenceValue = detailAbilitiesObj.GetComponent<TMP_Text>();
        LayoutElement daLE = detailAbilitiesObj.GetComponent<LayoutElement>();
        if (daLE == null) daLE = detailAbilitiesObj.AddComponent<LayoutElement>();
        daLE.minHeight = 60f;

        // TMP rich text for abilities
        TMP_Text abilitiesTMP = detailAbilitiesObj.GetComponent<TMP_Text>();
        if (abilitiesTMP != null) abilitiesTMP.richText = true;

        // Detail lore text
        GameObject detailLoreObj = EditorUiFactory.CreateText(detailContent.transform, "DetailLore",
            "", (int)Tokens.TextCaption, FontStyles.Italic,
            Tokens.BoneDim, TextAlignmentOptions.Center, richText: true, raycastTarget: false);
        so.FindProperty("detailLore").objectReferenceValue = detailLoreObj.GetComponent<TMP_Text>();
        LayoutElement dlLE = detailLoreObj.GetComponent<LayoutElement>();
        if (dlLE == null) dlLE = detailLoreObj.AddComponent<LayoutElement>();
        dlLE.minHeight = 40f;

        // Selected card info (legacy)
        GameObject infoObj = EditorUiFactory.CreateText(detailContent.transform, "SelectedCardInfo",
            "", (int)Tokens.TextCaption, FontStyles.Normal,
            Tokens.BoneDim, TextAlignmentOptions.Center, richText: true, raycastTarget: false);
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
        trackImg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.5f);
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
        handleImg.color = Tokens.WithAlpha(Tokens.Bronze, 0.7f);

        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.handleRect = hRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImg;

        ColorBlock scrollColors = scrollbar.colors;
        scrollColors.normalColor = Tokens.WithAlpha(Tokens.Bronze, 0.7f);
        scrollColors.highlightedColor = Tokens.Ember;
        scrollColors.pressedColor = Tokens.Ember * 0.8f;
        scrollbar.colors = scrollColors;

        return scrollbarObj;
    }
}
#endif
