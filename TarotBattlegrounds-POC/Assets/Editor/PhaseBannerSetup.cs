#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// Editor script to set up the Phase Banner and Turn Badge in the Game scene.
/// Run via menu: Tools/Game/Setup Phase Banner
/// </summary>
public class PhaseBannerSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Phase Banner")]
    public static void SetupPhaseBanner()
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

        if (!EditorUtility.DisplayDialog("Setup Phase Banner",
            "This will create:\n" +
            "  - Phase Banner overlay (center of screen)\n" +
            "  - Turn Badge (top-right corner)\n" +
            "  - PhaseBanner component wired to GameUIManager\n\n" +
            "Continue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();

        RemoveOld(canvas.transform);

        // Create the phase banner and turn badge
        GameObject bannerRoot = CreatePhaseBannerUI(canvas.transform);

        // Wire PhaseBanner to GameUIManager
        GameUIManager guiManager = Object.FindObjectOfType<GameUIManager>();
        if (guiManager != null)
        {
            Undo.RegisterCompleteObjectUndo(guiManager, "Wire PhaseBanner");
            SerializedObject so = new SerializedObject(guiManager);
            SerializedProperty prop = so.FindProperty("phaseBanner");
            if (prop != null)
            {
                prop.objectReferenceValue = bannerRoot.GetComponent<PhaseBanner>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(guiManager);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Phase Banner Setup Complete",
            "Created Phase Banner overlay and Turn Badge.\n" +
            "Save the scene to keep changes.", "OK");
    }

    private static void RemoveOld(Transform canvasTransform)
    {
        string[] oldNames = { "PhaseBannerRoot", "TurnBadge" };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
        foreach (var existing in Object.FindObjectsOfType<PhaseBanner>())
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    private static GameObject CreatePhaseBannerUI(Transform canvasTransform)
    {
        // === Root object for PhaseBanner component ===
        GameObject root = new GameObject("PhaseBannerRoot");
        root.transform.SetParent(canvasTransform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Set to last sibling so it overlays everything
        root.transform.SetAsLastSibling();

        PhaseBanner phaseBanner = root.AddComponent<PhaseBanner>();

        // === Banner group (center of screen, full width) ===
        GameObject bannerGroup = new GameObject("BannerGroup");
        bannerGroup.transform.SetParent(root.transform, false);
        RectTransform bannerGroupRect = bannerGroup.AddComponent<RectTransform>();
        bannerGroupRect.anchorMin = new Vector2(0f, 0.4f);
        bannerGroupRect.anchorMax = new Vector2(1f, 0.6f);
        bannerGroupRect.offsetMin = Vector2.zero;
        bannerGroupRect.offsetMax = Vector2.zero;

        CanvasGroup cg = bannerGroup.AddComponent<CanvasGroup>();
        cg.alpha = 0f; // Start hidden
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Banner background strip (full width, 80px height, centered)
        GameObject bgObj = new GameObject("BannerBackground");
        bgObj.transform.SetParent(bannerGroup.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(0, 80);
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Tokens.WithAlpha(Tokens.Umber, 0.85f);
        bgImage.raycastTarget = false;

        // Shadow text (behind main text, offset by 2, -2)
        GameObject shadowTextObj = new GameObject("BannerShadowText");
        shadowTextObj.transform.SetParent(bannerGroup.transform, false);
        RectTransform shadowRect = shadowTextObj.AddComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = Vector2.zero;
        shadowRect.offsetMax = Vector2.zero;
        shadowRect.anchoredPosition = new Vector2(2, -2);

        TextMeshProUGUI shadowTmp = shadowTextObj.AddComponent<TextMeshProUGUI>();
        shadowTmp.text = "PHASE";
        shadowTmp.fontSize = Tokens.TextDisplay;
        shadowTmp.fontStyle = FontStyles.Bold;
        shadowTmp.color = Tokens.WithAlpha(Tokens.Ash, 0.5f);
        shadowTmp.alignment = TextAlignmentOptions.Center;
        shadowTmp.enableWordWrapping = false;
        shadowTmp.raycastTarget = false;

        TMP_FontAsset font = FontRefs.Instance.Body;
        if (font != null) shadowTmp.font = font;

        // Main banner text (centered, large bold)
        GameObject mainTextObj = new GameObject("BannerText");
        mainTextObj.transform.SetParent(bannerGroup.transform, false);
        RectTransform mainRect = mainTextObj.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        TextMeshProUGUI mainTmp = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainTmp.text = "PHASE";
        mainTmp.fontSize = Tokens.TextDisplay;
        mainTmp.fontStyle = FontStyles.Bold;
        mainTmp.color = Tokens.BronzeBright;
        mainTmp.alignment = TextAlignmentOptions.Center;
        mainTmp.enableWordWrapping = false;
        mainTmp.raycastTarget = false;

        if (font != null) mainTmp.font = font;

        // === Turn Badge (top-right corner) ===
        GameObject turnBadgeObj = new GameObject("TurnBadge");
        turnBadgeObj.transform.SetParent(root.transform, false);
        RectTransform badgeRect = turnBadgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1, 1);
        badgeRect.anchorMax = new Vector2(1, 1);
        badgeRect.pivot = new Vector2(1, 1);
        badgeRect.anchoredPosition = new Vector2(-100, -10); // Offset left to avoid overlap with other top-right elements
        badgeRect.sizeDelta = new Vector2(120, 36);

        Image badgeImage = turnBadgeObj.AddComponent<Image>();
        badgeImage.color = Tokens.WithAlpha(Tokens.Umber, 0.9f);
        badgeImage.raycastTarget = false;

        // Add Outline component for gold border effect
        Outline badgeOutline = turnBadgeObj.AddComponent<Outline>();
        badgeOutline.effectColor = Tokens.WithAlpha(Tokens.BronzeBright, 0.8f);
        badgeOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Turn text inside the badge
        GameObject turnTextObj = new GameObject("TurnText");
        turnTextObj.transform.SetParent(turnBadgeObj.transform, false);
        RectTransform turnTextRect = turnTextObj.AddComponent<RectTransform>();
        turnTextRect.anchorMin = Vector2.zero;
        turnTextRect.anchorMax = Vector2.one;
        turnTextRect.offsetMin = Vector2.zero;
        turnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI turnTmp = turnTextObj.AddComponent<TextMeshProUGUI>();
        turnTmp.text = "Turn I";
        turnTmp.fontSize = Tokens.TextCaption;
        turnTmp.fontStyle = FontStyles.Bold;
        turnTmp.color = Tokens.BronzeBright;
        turnTmp.alignment = TextAlignmentOptions.Center;
        turnTmp.enableWordWrapping = false;
        turnTmp.raycastTarget = false;

        if (font != null) turnTmp.font = font;

        // === Wire serialized fields on PhaseBanner ===
        SerializedObject so = new SerializedObject(phaseBanner);
        so.FindProperty("bannerRect").objectReferenceValue = bannerGroupRect;
        so.FindProperty("bannerBackground").objectReferenceValue = bgImage;
        so.FindProperty("bannerText").objectReferenceValue = mainTmp;
        so.FindProperty("bannerShadowText").objectReferenceValue = shadowTmp;
        so.FindProperty("bannerGroup").objectReferenceValue = cg;
        so.FindProperty("turnBadge").objectReferenceValue = badgeImage;
        so.FindProperty("turnText").objectReferenceValue = turnTmp;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Create PhaseBannerRoot");

        return root;
    }
}
#endif
