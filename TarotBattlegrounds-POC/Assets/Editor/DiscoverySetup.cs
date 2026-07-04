#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// UX19: Editor script to set up the Discovery Popup visual hierarchy.
/// Creates backdrop, mystical frame, discover banner, card container,
/// and wires all serialized fields on DiscoveryUI.
/// Run via menu: Tools/Game/Setup Discovery Popup
/// </summary>
public class DiscoverySetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Discovery Popup")]
    public static void SetupDiscoveryPopup()
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

        if (!EditorUtility.DisplayDialog("Setup Discovery Popup",
            "This will create/update:\n" +
            "  - Dark backdrop overlay\n" +
            "  - Mystical frame with double border\n" +
            "  - DISCOVER banner with gold text\n" +
            "  - Card container with fan layout\n" +
            "  - CanvasGroup for popup fade\n\n" +
            "Continue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();

        // Find or create DiscoveryUI
        DiscoveryUI discoveryUI = Object.FindObjectOfType<DiscoveryUI>();
        GameObject discoveryRoot;

        if (discoveryUI != null)
        {
            discoveryRoot = discoveryUI.gameObject;
            // Remove old visual children that we'll recreate
            RemoveOldVisuals(discoveryRoot.transform);
        }
        else
        {
            // Create new DiscoveryUI root
            discoveryRoot = new GameObject("DiscoveryPopup");
            discoveryRoot.transform.SetParent(canvas.transform, false);
            discoveryUI = discoveryRoot.AddComponent<DiscoveryUI>();
            Undo.RegisterCreatedObjectUndo(discoveryRoot, "Create DiscoveryPopup");
        }

        // Ensure RectTransform fills canvas
        RectTransform rootRect = discoveryRoot.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = discoveryRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Overlay everything
        discoveryRoot.transform.SetAsLastSibling();

        // Build the visual hierarchy
        BuildDiscoveryVisuals(discoveryRoot, discoveryUI, canvas);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Discovery Popup Setup Complete",
            "Created Discovery Popup visual hierarchy.\n" +
            "Save the scene to keep changes.", "OK");
    }

    private static void RemoveOldVisuals(Transform parent)
    {
        string[] oldNames = { "Backdrop", "DiscoveryPanel", "MysticalFrame", "DiscoverBanner", "CardContainer" };
        foreach (string name in oldNames)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    private static void BuildDiscoveryVisuals(GameObject root, DiscoveryUI discoveryUI, Canvas canvas)
    {
        Undo.RegisterCompleteObjectUndo(discoveryUI, "Setup Discovery Visuals");

        // === 1. Backdrop (full-screen dark overlay, raycast target = true to block clicks) ===
        GameObject backdropObj = new GameObject("Backdrop");
        backdropObj.transform.SetParent(root.transform, false);
        RectTransform backdropRect = backdropObj.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropImage = backdropObj.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.7f);
        backdropImage.raycastTarget = true; // Blocks clicks behind popup

        // === 2. Discovery Panel (the main popup container) ===
        GameObject panelObj = new GameObject("DiscoveryPanel");
        panelObj.transform.SetParent(root.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.15f);
        panelRect.anchorMax = new Vector2(0.85f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // CanvasGroup for popup fade animations
        CanvasGroup popupGroup = panelObj.AddComponent<CanvasGroup>();
        popupGroup.alpha = 1f;

        // Panel background (dark, semi-transparent)
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.04f, 0.12f, 0.95f);
        panelBg.raycastTarget = false;

        // === 3. Mystical Frame (double border with glow effect) ===
        // Outer glow border
        GameObject outerFrameObj = new GameObject("MysticalFrameOuter");
        outerFrameObj.transform.SetParent(panelObj.transform, false);
        RectTransform outerFrameRect = outerFrameObj.AddComponent<RectTransform>();
        outerFrameRect.anchorMin = Vector2.zero;
        outerFrameRect.anchorMax = Vector2.one;
        outerFrameRect.offsetMin = new Vector2(-6f, -6f);
        outerFrameRect.offsetMax = new Vector2(6f, 6f);

        Image outerFrameImage = outerFrameObj.AddComponent<Image>();
        outerFrameImage.color = new Color(1f, 0.78f, 0.15f, 0.25f); // Gold glow, subtle
        outerFrameImage.raycastTarget = false;

        // Add Outline for procedural border effect
        Outline outerOutline = outerFrameObj.AddComponent<Outline>();
        outerOutline.effectColor = new Color(1f, 0.78f, 0.15f, 0.4f);
        outerOutline.effectDistance = new Vector2(3f, -3f);

        // Inner border
        GameObject innerFrameObj = new GameObject("MysticalFrameInner");
        innerFrameObj.transform.SetParent(panelObj.transform, false);
        RectTransform innerFrameRect = innerFrameObj.AddComponent<RectTransform>();
        innerFrameRect.anchorMin = Vector2.zero;
        innerFrameRect.anchorMax = Vector2.one;
        innerFrameRect.offsetMin = new Vector2(-2f, -2f);
        innerFrameRect.offsetMax = new Vector2(2f, 2f);

        Image innerFrameImage = innerFrameObj.AddComponent<Image>();
        innerFrameImage.color = new Color(1f, 0.78f, 0.15f, 0.6f); // Brighter inner border
        innerFrameImage.raycastTarget = false;

        // Add Outline for double-border effect
        Outline innerOutline = innerFrameObj.AddComponent<Outline>();
        innerOutline.effectColor = new Color(1f, 0.82f, 0.12f, 0.8f);
        innerOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Corner flourishes (decorative diamond shapes at corners)
        CreateCornerFlourish(panelObj.transform, new Vector2(0, 0), new Vector2(0, 0));   // bottom-left
        CreateCornerFlourish(panelObj.transform, new Vector2(1, 0), new Vector2(1, 0));   // bottom-right
        CreateCornerFlourish(panelObj.transform, new Vector2(0, 1), new Vector2(0, 1));   // top-left
        CreateCornerFlourish(panelObj.transform, new Vector2(1, 1), new Vector2(1, 1));   // top-right

        // === 4. DISCOVER Banner (gold text at top, slides in) ===
        GameObject bannerObj = new GameObject("DiscoverBanner");
        bannerObj.transform.SetParent(panelObj.transform, false);
        RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0f, 0.8f);
        bannerRect.anchorMax = new Vector2(1f, 0.95f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;

        // Banner background strip
        Image bannerBg = bannerObj.AddComponent<Image>();
        bannerBg.color = new Color(0.08f, 0.05f, 0.15f, 0.8f);
        bannerBg.raycastTarget = false;

        // Banner text
        GameObject bannerTextObj = new GameObject("DiscoverBannerText");
        bannerTextObj.transform.SetParent(bannerObj.transform, false);
        RectTransform bannerTextRect = bannerTextObj.AddComponent<RectTransform>();
        bannerTextRect.anchorMin = Vector2.zero;
        bannerTextRect.anchorMax = Vector2.one;
        bannerTextRect.offsetMin = Vector2.zero;
        bannerTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI bannerTmp = bannerTextObj.AddComponent<TextMeshProUGUI>();
        bannerTmp.text = "DISCOVER";
        bannerTmp.fontSize = 42;
        bannerTmp.fontStyle = FontStyles.Bold;
        bannerTmp.color = new Color(1f, 0.82f, 0.12f); // Gold
        bannerTmp.alignment = TextAlignmentOptions.Center;
        bannerTmp.enableWordWrapping = false;
        bannerTmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) bannerTmp.font = font;

        // === 5. Title Text (below banner: "Triple! Choose a Card:") ===
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.7f);
        titleRect.anchorMax = new Vector2(1f, 0.8f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Triple! Choose a Card:";
        titleTmp.fontSize = 24;
        titleTmp.fontStyle = FontStyles.Normal;
        titleTmp.color = new Color(1f, 0.82f, 0.12f); // Gold accent
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.enableWordWrapping = false;
        titleTmp.raycastTarget = false;

        if (font != null) titleTmp.font = font;

        // === 6. Card Container (centered, holds the 3 fan cards) ===
        GameObject cardContainerObj = new GameObject("CardContainer");
        cardContainerObj.transform.SetParent(panelObj.transform, false);
        RectTransform containerRect = cardContainerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.7f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Use HorizontalLayoutGroup for base positioning (overridden by fan layout in code)
        HorizontalLayoutGroup hlg = cardContainerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // === Wire serialized fields ===
        SerializedObject so = new SerializedObject(discoveryUI);

        // Existing fields
        so.FindProperty("discoveryPanel").objectReferenceValue = panelObj;
        so.FindProperty("titleText").objectReferenceValue = titleTmp;
        so.FindProperty("cardContainer").objectReferenceValue = cardContainerObj.transform;

        // UX19 new fields
        so.FindProperty("backdrop").objectReferenceValue = backdropImage;
        so.FindProperty("mysticalFrame").objectReferenceValue = innerFrameImage;
        so.FindProperty("discoverBanner").objectReferenceValue = bannerTmp;
        so.FindProperty("popupGroup").objectReferenceValue = popupGroup;

        // UX19 tuning values
        so.FindProperty("fanAngle").floatValue = 10f;
        so.FindProperty("cardSpacing").floatValue = 160f;
        so.FindProperty("staggerDelay").floatValue = 0.15f;
        so.FindProperty("cardScaleSpeed").floatValue = 0.3f;

        // UX19 colors
        SetColorProperty(so, "backdropColor", new Color(0f, 0f, 0f, 0.7f));
        SetColorProperty(so, "bannerColor", new Color(1f, 0.82f, 0.12f));
        SetColorProperty(so, "selectionFlash", new Color(1f, 1f, 1f, 0.8f));

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(discoveryUI);

        // Wire to GameUIManager if present
        WireToGameUIManager(discoveryUI);

        Debug.Log("[DiscoverySetup] Discovery Popup visual hierarchy created and wired.");
    }

    /// <summary>
    /// Create a small decorative corner flourish (diamond shape) at a corner of the frame.
    /// </summary>
    private static void CreateCornerFlourish(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject flourish = new GameObject("CornerFlourish");
        flourish.transform.SetParent(parent, false);
        RectTransform rt = flourish.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin; // Pivot at the corner
        rt.sizeDelta = new Vector2(20f, 20f);

        // Offset inward from corner
        float xOffset = anchorMin.x < 0.5f ? -4f : 4f;
        float yOffset = anchorMin.y < 0.5f ? -4f : 4f;
        rt.anchoredPosition = new Vector2(xOffset, yOffset);

        // Diamond rotation
        rt.localRotation = Quaternion.Euler(0, 0, 45f);

        Image flourishImage = flourish.AddComponent<Image>();
        flourishImage.color = new Color(1f, 0.82f, 0.12f, 0.7f); // Gold flourish
        flourishImage.raycastTarget = false;
    }

    /// <summary>
    /// Wire DiscoveryUI to GameUIManager if it has a discovery field.
    /// </summary>
    private static void WireToGameUIManager(DiscoveryUI discoveryUI)
    {
        GameUIManager guiManager = Object.FindObjectOfType<GameUIManager>();
        if (guiManager == null) return;

        SerializedObject so = new SerializedObject(guiManager);
        SerializedProperty prop = so.FindProperty("discoveryUI");
        if (prop != null)
        {
            Undo.RegisterCompleteObjectUndo(guiManager, "Wire DiscoveryUI");
            prop.objectReferenceValue = discoveryUI;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(guiManager);
            Debug.Log("[DiscoverySetup] Wired DiscoveryUI to GameUIManager.");
        }
    }

    private static void SetColorProperty(SerializedObject so, string propertyName, Color color)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.colorValue = color;
        }
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
