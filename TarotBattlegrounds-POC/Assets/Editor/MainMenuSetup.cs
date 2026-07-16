#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// T750 Phase 3: regenerate MainMenu to DESIGN.md §8 (Dark Tribal Realism).
/// Layout matches design-site Copy A. Factory and scene must ship in agreement.
/// Menu item keeps dialogs for humans; call SetupMainMenuCore() over MCP.
/// </summary>
public class MainMenuSetup : EditorWindow
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string MenuStillPath = "Assets/Art/Env/Menu/menu_bg.jpg";
    // Prefer pre-baked forward+reverse clip (Unity VideoPlayer can't reverse on this backend).
    private const string MenuVideoPingPongPath = "Assets/Art/Env/Menu/menu_video_pingpong.mp4";
    private const string MenuVideoPath = "Assets/Art/Env/Menu/menu_video.mp4";

    // Copy A (design-site)
    private const string TitleCopy = "Tarot\nBattlegrounds";
    private const string SubtitleCopy = "A Mystical Auto-Battler";
    private const string CtaCopy = "Begin the rite";

    [MenuItem("Tools/Game/Setup Main Menu")]
    public static void SetupMainMenu()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("MainMenu"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not MainMenu. Open MainMenu scene first?\n\n" +
                "(This will save the current scene.)",
                "Open MainMenu", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(ScenePath);
        }

        if (!EditorUtility.DisplayDialog("T750: Setup Main Menu (§8)",
            "DESTRUCTIVE regen of MainMenu chrome under SafeArea:\n\n" +
            "  - Hero-left title + underline_ember sigil\n" +
            "  - Copy A rows (≥96px text-first)\n" +
            "  - CTA 'Begin the rite' (button_bronze)\n" +
            "  - menu_bg still + menu_video loop trial\n" +
            "  - SoloPanel retained (config flow)\n\n" +
            "Survivors: ResourceBar, SettingsRoot, BG layers, SafeArea.\n" +
            "Continue?", "Regen", "Cancel"))
            return;

        string report = SetupMainMenuCore();
        EditorUtility.DisplayDialog("T750: Main Menu Setup Complete", report, "OK");
    }

    /// <summary>
    /// Dialog-free core for MCP / batch runs. Returns a human-readable report.
    /// </summary>
    public static string SetupMainMenuCore()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene == null || !activeScene.name.Contains("MainMenu"))
        {
            if (System.IO.File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
                return "ABORT: MainMenu scene not found at " + ScenePath;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();

        MainMenuManager manager = Object.FindObjectOfType<MainMenuManager>();
        if (manager == null)
            manager = canvas.gameObject.AddComponent<MainMenuManager>();

        MainMenuVisual visual = Object.FindObjectOfType<MainMenuVisual>();
        if (visual == null)
            visual = canvas.gameObject.AddComponent<MainMenuVisual>();

        Undo.RegisterCompleteObjectUndo(manager, "T750 Setup Main Menu");
        Undo.RegisterCompleteObjectUndo(visual, "T750 Setup Main Menu");

        SerializedObject managerSO = new SerializedObject(manager);
        SerializedObject visualSO = new SerializedObject(visual);

        // Ensure env import settings before load
        EnsureMenuEnvImportSettings();

        Transform safeArea = EnsureSafeArea(canvas.transform);
        RemoveOldElements(canvas.transform);

        // ===============================
        // Background: still + video trial (outside SafeArea)
        // ===============================
        BackgroundController bgCtrl = EnsureArtBackground(canvas.transform);

        // ===============================
        // Content under SafeArea
        // ===============================
        GameObject contentGroupObj = new GameObject("ContentGroup");
        contentGroupObj.transform.SetParent(safeArea, false);
        RectTransform contentRect = contentGroupObj.AddComponent<RectTransform>();
        StretchFull(contentRect);
        CanvasGroup contentGroup = contentGroupObj.AddComponent<CanvasGroup>();
        managerSO.FindProperty("contentGroup").objectReferenceValue = contentGroup;

        GameObject mainPanel = EditorUiFactory.CreateFullscreenPanel(contentGroupObj.transform, "MainPanel");
        // Transparent shell — no solid panel wash over the env art
        Image mainPanelImg = mainPanel.GetComponent<Image>();
        if (mainPanelImg != null)
        {
            mainPanelImg.color = Tokens.WithAlpha(Tokens.Ash, 0f);
            mainPanelImg.raycastTarget = false;
        }
        managerSO.FindProperty("mainPanel").objectReferenceValue = mainPanel;

        // --- Hero left ---
        GameObject hero = new GameObject("HeroLeft");
        hero.transform.SetParent(mainPanel.transform, false);
        RectTransform heroRect = hero.AddComponent<RectTransform>();
        heroRect.anchorMin = new Vector2(0f, 0f);
        heroRect.anchorMax = new Vector2(0.40f, 1f);
        heroRect.offsetMin = new Vector2(Tokens.Space5, Tokens.Space5);
        heroRect.offsetMax = new Vector2(-Tokens.Space3, -Tokens.Space5);

        VerticalLayoutGroup heroVLG = hero.AddComponent<VerticalLayoutGroup>();
        heroVLG.spacing = Tokens.Space3;
        heroVLG.childAlignment = TextAnchor.MiddleLeft;
        heroVLG.childControlWidth = true;
        heroVLG.childControlHeight = false;
        heroVLG.childForceExpandWidth = true;
        heroVLG.childForceExpandHeight = false;
        heroVLG.padding = new RectOffset(0, 0, 0, 0);

        GameObject titleObj = CreateDisplayTitle(hero.transform, "TitleText", TitleCopy);
        TMP_Text titleTMP = titleObj.GetComponent<TMP_Text>();
        managerSO.FindProperty("titleText").objectReferenceValue = titleTMP;
        visualSO.FindProperty("titleText").objectReferenceValue = titleTMP;

        GameObject sigil = CreateSigilUnderline(hero.transform, "HeroSigil");
        visualSO.FindProperty("sigilUnderline").objectReferenceValue = sigil.GetComponent<CanvasGroup>();

        GameObject subtitleObj = EditorUiFactory.CreateText(hero.transform, "SubtitleText",
            SubtitleCopy, (int)Tokens.TextBody, FontStyles.Normal, Tokens.BoneDim,
            TextAlignmentOptions.MidlineLeft, heightPadding: 14, raycastTarget: false);
        TMP_Text subtitleTMP = subtitleObj.GetComponent<TMP_Text>();
        managerSO.FindProperty("subtitleText").objectReferenceValue = subtitleTMP;

        // Logo field left null — hero_sigil deferred
        managerSO.FindProperty("logoImage").objectReferenceValue = null;

        // --- Menu column right (no plate — full-bleed art shows through) ---
        GameObject menuCol = new GameObject("MenuColumn");
        menuCol.transform.SetParent(mainPanel.transform, false);
        RectTransform menuRect = menuCol.AddComponent<RectTransform>();
        // Slightly taller — primary CTA is now the first menu row
        menuRect.anchorMin = new Vector2(0.52f, 0.16f);
        menuRect.anchorMax = new Vector2(1f, 0.86f);
        menuRect.offsetMin = new Vector2(0f, 0f);
        menuRect.offsetMax = new Vector2(-Tokens.Space4, 0f);

        VerticalLayoutGroup menuVLG = menuCol.AddComponent<VerticalLayoutGroup>();
        menuVLG.spacing = Tokens.Space1; // 8px between rows
        menuVLG.childAlignment = TextAnchor.MiddleLeft;
        menuVLG.childControlWidth = true;
        menuVLG.childControlHeight = false;
        menuVLG.childForceExpandWidth = true;
        menuVLG.childForceExpandHeight = false;
        menuVLG.padding = new RectOffset(0, 0, 0, 0);

        // Copy A: primary action is the first menu row (not a separate bronze CTA)
        GameObject soloRow = CreateMenuRow(menuCol.transform, "SoloButton", CtaCopy);
        managerSO.FindProperty("soloButton").objectReferenceValue = soloRow.GetComponent<Button>();

        GameObject mpRow = CreateMenuRow(menuCol.transform, "MultiplayerButton", "Multiplayer");
        managerSO.FindProperty("multiplayerButton").objectReferenceValue = mpRow.GetComponent<Button>();

        GameObject colRow = CreateMenuRow(menuCol.transform, "CollectionButton", "Collection");
        managerSO.FindProperty("collectionButton").objectReferenceValue = colRow.GetComponent<Button>();

        GameObject setRow = CreateMenuRow(menuCol.transform, "SettingsButton", "Settings");
        managerSO.FindProperty("settingsButton").objectReferenceValue = setRow.GetComponent<Button>();

        GameObject quitRow = CreateMenuRow(menuCol.transform, "QuitButton", "Quit");
        managerSO.FindProperty("quitButton").objectReferenceValue = quitRow.GetComponent<Button>();

        // Ranked dropped from Copy A — clear ref so stale wiring cannot fire
        managerSO.FindProperty("rankedButton").objectReferenceValue = null;

        // Player info under menu column (auth)
        GameObject playerInfoObj = EditorUiFactory.CreateText(menuCol.transform, "PlayerInfoText",
            "", (int)Tokens.TextCaption, FontStyles.Normal, Tokens.BoneDim,
            TextAlignmentOptions.MidlineLeft, heightPadding: 8, raycastTarget: false);
        managerSO.FindProperty("playerInfoText").objectReferenceValue =
            playerInfoObj.GetComponent<TMP_Text>();

        // --- Version bottom-left ---
        GameObject versionObj = EditorUiFactory.CreateText(mainPanel.transform, "VersionText",
            "v" + Application.version, (int)Tokens.TextCaption, FontStyles.Normal, Tokens.BoneDim,
            TextAlignmentOptions.BottomLeft, heightPadding: 8, raycastTarget: false);
        RectTransform versionRect = versionObj.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0f, 0f);
        versionRect.anchorMax = new Vector2(0.4f, 0.12f);
        versionRect.offsetMin = new Vector2(Tokens.Space5, Tokens.Space3);
        versionRect.offsetMax = new Vector2(0f, 0f);
        LayoutElement versionLE = versionObj.GetComponent<LayoutElement>();
        if (versionLE != null) Object.DestroyImmediate(versionLE);

        // --- Solo panel (config) ---
        GameObject soloPanel = CreateSoloPanel(contentGroupObj.transform, managerSO);
        soloPanel.SetActive(false);

        // Clear obsolete bottom-bar selector lists on visual (selectors live in SoloPanel only)
        ClearArrayProp(visualSO, "difficultyButtons");
        ClearArrayProp(visualSO, "difficultyLabels");
        ClearArrayProp(visualSO, "playerCountButtons");
        ClearArrayProp(visualSO, "playerCountLabels");

        if (bgCtrl != null)
            visualSO.FindProperty("backgroundController").objectReferenceValue = bgCtrl;

        // Preserve existing CollectionUI / SettingsUI / AuthUI if present in scene
        var collectionUI = Object.FindObjectOfType<CollectionUI>(true);
        if (collectionUI != null)
            managerSO.FindProperty("collectionUI").objectReferenceValue = collectionUI;
        var settingsUI = Object.FindObjectOfType<SettingsUI>(true);
        if (settingsUI != null)
            managerSO.FindProperty("settingsUI").objectReferenceValue = settingsUI;
        var authUI = Object.FindObjectOfType<AuthUI>(true);
        if (authUI != null)
            managerSO.FindProperty("authUI").objectReferenceValue = authUI;
        var matchmakingUI = Object.FindObjectOfType<MatchmakingUI>(true);
        if (matchmakingUI != null)
            managerSO.FindProperty("matchmakingUI").objectReferenceValue = matchmakingUI;

        // Game HUD leftovers (ResourceBar health/gold/tier, recruit timer, gear)
        // must not show on MainMenu — Settings panel itself stays available for the row.
        string hideReport = HideNonMenuChrome(canvas.transform);

        managerSO.ApplyModifiedProperties();
        visualSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(visual);
        if (bgCtrl != null) EditorUtility.SetDirty(bgCtrl);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string report =
            "T750 §8 MainMenu regen complete (Copy A).\n\n" +
            "  SafeArea parent: yes\n" +
            "  Hero-left title + underline sigil + subtitle\n" +
            "  Rows: Begin the rite / Multiplayer / Collection / Settings / Quit (96px)\n" +
            "  CTA: first menu row (no separate bronze button)\n" +
            "  Ranked: omitted (ref cleared)\n" +
            "  BG: menu_bg still + menu_video ping-pong (reduce-motion → still)\n" +
            "  Rows: no plate; Cinzel Display (title font); art shows through\n" +
            "  SoloPanel: rebuilt under ContentGroup\n" +
            "  Hidden non-menu chrome: " + hideReport + "\n\n" +
            "Save the scene. Gate A = Play mode + Device Simulator.";
        Debug.Log("[MainMenuSetup] " + report.Replace("\n", " | "));
        return report;
    }

    /// <summary>
    /// Deactivate Game-scene HUD that survives under SafeArea from older setups.
    /// Keeps SettingsRoot alive so SettingsUI.Open still works from the menu row;
    /// only the floating gear button is hidden (Settings is a text row now).
    /// </summary>
    private static string HideNonMenuChrome(Transform canvasTransform)
    {
        var hidden = new List<string>();

        // Whole trees that are purely in-game HUD
        string[] deactivateRoots = {
            "ResourceBar",   // gold / health / tier
            "CircularTimer", // recruit timer
            "CollectionPanel" // collection opens via CollectionUI; keep if inactive already
        };
        foreach (string name in deactivateRoots)
        {
            Transform t = FindDeep(canvasTransform, name);
            if (t == null) continue;
            if (t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(false);
                hidden.Add(name);
                EditorUtility.SetDirty(t.gameObject);
            }
            else if (!hidden.Contains(name + "(already off)"))
            {
                // still note if present but already off
            }
        }

        // Settings gear (SettingsRoot/SettingsButton) — not the menu row named SettingsButton
        Transform settingsRoot = FindDeep(canvasTransform, "SettingsRoot");
        if (settingsRoot != null)
        {
            // Prefer direct child gear, not nested panel bits
            for (int i = 0; i < settingsRoot.childCount; i++)
            {
                Transform ch = settingsRoot.GetChild(i);
                if (ch.name == "SettingsButton" && ch.gameObject.activeSelf)
                {
                    ch.gameObject.SetActive(false);
                    hidden.Add("SettingsRoot/SettingsButton(gear)");
                    EditorUtility.SetDirty(ch.gameObject);
                }
            }
        }

        // Any other common HUD names that might linger
        string[] extra = { "PlayerHealthBar", "HealthBar", "GameHUD", "RecruitTimer" };
        foreach (string name in extra)
        {
            Transform t = FindDeep(canvasTransform, name);
            if (t != null && t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(false);
                hidden.Add(name);
                EditorUtility.SetDirty(t.gameObject);
            }
        }

        return hidden.Count == 0 ? "(none found active)" : string.Join(", ", hidden);
    }

    // ===================== BACKGROUND =====================

    private static BackgroundController EnsureArtBackground(Transform canvasTransform)
    {
        // Prefer existing controller
        BackgroundController bgCtrl = Object.FindObjectOfType<BackgroundController>(true);

        // BG_Root owns RectMask2D — never put mask on Canvas (clips TMP titles)
        RectTransform bgRoot = BackgroundController.EnsureBgRoot(canvasTransform);

        Image baseImg = FindNamedImage(canvasTransform, "BG_Base");
        Image gradientImg = FindNamedImage(canvasTransform, "BG_Gradient");
        Image vignetteImg = FindNamedImage(canvasTransform, "BG_Vignette");
        Transform particlesT = FindDeep(canvasTransform, "BG_Particles");

        if (baseImg == null)
        {
            GameObject baseGo = CreateFullscreenImage(bgRoot, "BG_Base", Tokens.Ash, 0);
            baseImg = baseGo.GetComponent<Image>();
        }
        else
        {
            baseImg.transform.SetParent(bgRoot, false);
            StretchFull(baseImg.rectTransform);
        }
        if (vignetteImg == null)
        {
            GameObject vigGo = CreateFullscreenImage(bgRoot, "BG_Vignette", Color.white, 2);
            vignetteImg = vigGo.GetComponent<Image>();
        }
        else
        {
            vignetteImg.transform.SetParent(bgRoot, false);
            StretchFull(vignetteImg.rectTransform);
        }

        // Video layer
        Transform videoT = FindDeep(canvasTransform, "BG_Video");
        GameObject videoGo;
        if (videoT == null)
        {
            videoGo = new GameObject("BG_Video");
            videoGo.transform.SetParent(bgRoot, false);
            RectTransform rt = videoGo.AddComponent<RectTransform>();
            StretchFull(rt);
            videoGo.AddComponent<CanvasRenderer>();
            RawImage raw = videoGo.AddComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;
            VideoPlayer vp = videoGo.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.audioOutputMode = VideoAudioOutputMode.None;
        }
        else
        {
            videoGo = videoT.gameObject;
            videoGo.transform.SetParent(bgRoot, false);
            StretchFull(videoGo.GetComponent<RectTransform>());
        }

        RawImage videoRaw = videoGo.GetComponent<RawImage>();
        if (videoRaw == null) videoRaw = videoGo.AddComponent<RawImage>();
        VideoPlayer videoPlayer = videoGo.GetComponent<VideoPlayer>();
        if (videoPlayer == null) videoPlayer = videoGo.AddComponent<VideoPlayer>();

        if (bgCtrl == null)
        {
            bgCtrl = canvasTransform.gameObject.GetComponent<BackgroundController>();
            if (bgCtrl == null)
                bgCtrl = canvasTransform.gameObject.AddComponent<BackgroundController>();
        }

        ParticleSystem dust = particlesT != null ? particlesT.GetComponent<ParticleSystem>() : null;
        bgCtrl.WireLayers(baseImg, gradientImg, vignetteImg, dust, videoRaw, videoPlayer);

        // Disable procedural gradient when art is present
        if (gradientImg != null)
        {
            gradientImg.enabled = false;
            gradientImg.gameObject.SetActive(false);
        }
        if (dust != null)
            dust.gameObject.SetActive(false);

        Sprite still = AssetDatabase.LoadAssetAtPath<Sprite>(MenuStillPath);
        if (still == null)
        {
            // May be Texture2D if import not yet Sprite — try sub-asset
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(MenuStillPath);
            if (all != null)
            {
                foreach (Object o in all)
                {
                    if (o is Sprite s) { still = s; break; }
                }
            }
        }

        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(MenuVideoPingPongPath);
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(MenuVideoPath);
        // Ping-pong clip already contains reverse half → plain loop is seamless.
        // (Software reverse is a no-op on this VideoPlayer backend.)
        bgCtrl.ConfigureArtBackground(still, clip, loop: true);
        EditorUtility.SetDirty(bgCtrl);

        // Layer order inside BG_Root: Base → Video → Vignette
        if (baseImg != null) baseImg.transform.SetSiblingIndex(0);
        if (videoGo != null) videoGo.transform.SetSiblingIndex(1);
        if (vignetteImg != null) vignetteImg.transform.SetSiblingIndex(2);
        bgRoot.SetAsFirstSibling();

        return bgCtrl;
    }

    private static void EnsureMenuEnvImportSettings()
    {
        var importer = AssetImporter.GetAtPath(MenuStillPath) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }

    // ===================== SAFE AREA / CLEANUP =====================

    private static Transform EnsureSafeArea(Transform canvasTransform)
    {
        Transform existing = FindDeep(canvasTransform, "SafeArea");
        if (existing != null)
        {
            if (existing.GetComponent<SafeAreaHandler>() == null)
                existing.gameObject.AddComponent<SafeAreaHandler>();
            return existing;
        }

        GameObject safe = new GameObject("SafeArea");
        safe.transform.SetParent(canvasTransform, false);
        RectTransform rt = safe.AddComponent<RectTransform>();
        StretchFull(rt);
        safe.AddComponent<SafeAreaHandler>();
        return safe.transform;
    }

    private static void RemoveOldElements(Transform canvasTransform)
    {
        string[] oldNames = {
            "ContentGroup", "MainPanel", "SoloPanel",
            "TitleArea", "ButtonArea", "BottomBar",
            "HeroLeft", "MenuColumn"
        };

        // Destroy every match under canvas (including under SafeArea)
        List<GameObject> toDestroy = new List<GameObject>();
        CollectNamed(canvasTransform, oldNames, toDestroy);

        // Also destroy orphan roots from older factories (ContentGroup was sometimes
        // left at scene root after reparents / partial runs — not under Canvas).
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                for (int n = 0; n < oldNames.Length; n++)
                {
                    if (root.name == oldNames[n])
                    {
                        toDestroy.Add(root);
                        break;
                    }
                }
            }
        }

        foreach (GameObject go in toDestroy)
        {
            if (go == null) continue;
            Debug.Log("[MainMenuSetup] Removed old " + go.name);
            Undo.DestroyObjectImmediate(go);
        }
    }

    private static void CollectNamed(Transform root, string[] names, List<GameObject> results)
    {
        if (root == null) return;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            for (int n = 0; n < names.Length; n++)
            {
                if (child.name == names[n])
                {
                    results.Add(child.gameObject);
                    break;
                }
            }
            CollectNamed(child, names, results);
        }
    }

    // ===================== §8 CONTROLS =====================

    private static GameObject CreateDisplayTitle(Transform parent, string name, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, Tokens.TextDisplay * 2.4f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = Tokens.TextDisplay;
        tmp.characterSpacing = Tokens.TrackingDisplay * 100f;
        tmp.color = Tokens.BoneBright;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        // Explicit newlines only — wrapping + mask bugs ate "TAROT BATTLE…" after scene return
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        var refs = FontRefs.Instance;
        if (refs != null && refs.Display != null) tmp.font = refs.Display;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = Tokens.TextDisplay * 2.2f;
        le.preferredHeight = Tokens.TextDisplay * 2.4f;
        le.flexibleWidth = 1f;
        return obj;
    }

    private static GameObject CreateSigilUnderline(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(264f, 8f);

        Image img = obj.AddComponent<Image>();
        var sprites = UiSprites.Instance;
        if (sprites != null && sprites.UnderlineEmber != null)
            UiSprites.ApplySliced(img, sprites.UnderlineEmber);
        else
            img.color = Tokens.Ember;
        img.raycastTarget = false;

        CanvasGroup cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 0.85f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = 8f;
        le.preferredHeight = 8f;
        le.preferredWidth = 264f;
        le.flexibleWidth = 0f;
        return obj;
    }

    private static GameObject CreateMenuRow(Transform parent, string name, string label)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rect = row.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, Tokens.MenuRowHeight);

        // Invisible hit target — text-first over full-bleed art (no brown slab/plate)
        Image hit = row.AddComponent<Image>();
        hit.color = Tokens.WithAlpha(Tokens.Ash, 0f);
        hit.raycastTarget = true;

        Button btn = row.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        // Hairline bottom edge (subtle only)
        GameObject line = new GameObject("Hairline");
        line.transform.SetParent(row.transform, false);
        RectTransform lineRect = line.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 0f);
        lineRect.pivot = new Vector2(0.5f, 0f);
        lineRect.sizeDelta = new Vector2(0f, Tokens.BorderThin);
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = Tokens.StoneHairline;
        lineImg.raycastTarget = false;

        // Label — same face as title (Cinzel Display), Bone idle → BoneBright + underline on ignite
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(row.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(Tokens.Space2, Tokens.Space1);
        textRect.offsetMax = new Vector2(-Tokens.Space2, -Tokens.Space1);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label; // title case like design-site (not forced CAPS)
        tmp.fontSize = Tokens.TextH2; // closer to title weight; still below Display
        tmp.characterSpacing = Tokens.TrackingDisplay * 100f; // match title tracking
        tmp.color = Tokens.Bone;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;
        var refs = FontRefs.Instance;
        // Same font family as "Tarot Battlegrounds" hero title
        if (refs != null && refs.Display != null) tmp.font = refs.Display;

        // Underline ember (shown when ignited)
        GameObject under = new GameObject("Underline");
        under.transform.SetParent(textObj.transform, false);
        RectTransform underRect = under.AddComponent<RectTransform>();
        underRect.anchorMin = new Vector2(0f, 0f);
        underRect.anchorMax = new Vector2(0.55f, 0f);
        underRect.pivot = new Vector2(0f, 1f);
        underRect.anchoredPosition = new Vector2(0f, -2f);
        underRect.sizeDelta = new Vector2(0f, 6f);
        Image underImg = under.AddComponent<Image>();
        var sprites = UiSprites.Instance;
        if (sprites != null && sprites.UnderlineEmber != null)
            UiSprites.ApplySliced(underImg, sprites.UnderlineEmber);
        else
            underImg.color = Tokens.Ember;
        underImg.raycastTarget = false;
        under.SetActive(false);

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = Tokens.MenuRowHeight;
        le.preferredHeight = Tokens.MenuRowHeight;

        var ignite = row.AddComponent<IgniteButton>();
        SerializedObject igniteSO = new SerializedObject(ignite);
        igniteSO.FindProperty("background").objectReferenceValue = null;
        igniteSO.FindProperty("idleSprite").objectReferenceValue = null;
        igniteSO.FindProperty("ignitedSprite").objectReferenceValue = null;
        igniteSO.FindProperty("pressedSprite").objectReferenceValue = null;
        igniteSO.FindProperty("label").objectReferenceValue = tmp;
        igniteSO.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.Bone;
        igniteSO.FindProperty("underline").objectReferenceValue = under;
        igniteSO.ApplyModifiedProperties();

        return row;
    }

    private static GameObject CreateCtaButton(Transform parent, string name, string label)
    {
        float width = 500f;
        float height = Tokens.SlotHeight; // 112 — thumb zone

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-Tokens.Space5, Tokens.Space4);
        rect.sizeDelta = new Vector2(width, height);

        var sprites = UiSprites.Instance;
        Image bgImg = btnObj.AddComponent<Image>();
        Sprite idle = sprites != null ? sprites.ButtonBronze : null;
        if (idle != null) UiSprites.ApplySliced(bgImg, idle);
        else bgImg.color = Tokens.Bronze;
        bgImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);
        textRect.offsetMin = new Vector2(Tokens.Space3, Tokens.Space1);
        textRect.offsetMax = new Vector2(-Tokens.Space3, -Tokens.Space1);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label; // "Begin the rite" — Display face, title case
        tmp.fontSize = Tokens.TextH2; // 37 — primary action
        tmp.characterSpacing = Tokens.TrackingDisplay * 100f;
        tmp.color = Tokens.BoneBright;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;
        var refs = FontRefs.Instance;
        if (refs != null && refs.Display != null) tmp.font = refs.Display;

        var ignite = btnObj.AddComponent<IgniteButton>();
        SerializedObject igniteSO = new SerializedObject(ignite);
        igniteSO.FindProperty("background").objectReferenceValue = bgImg;
        igniteSO.FindProperty("idleSprite").objectReferenceValue = idle;
        igniteSO.FindProperty("ignitedSprite").objectReferenceValue = sprites != null ? sprites.ButtonEmber : null;
        igniteSO.FindProperty("pressedSprite").objectReferenceValue = sprites != null ? sprites.ButtonBronzePressed : null;
        igniteSO.FindProperty("label").objectReferenceValue = tmp;
        igniteSO.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.BoneBright;
        igniteSO.ApplyModifiedProperties();

        return btnObj;
    }

    private static GameObject CreateSoloPanel(Transform parent, SerializedObject managerSO)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "SoloPanel");
        managerSO.FindProperty("soloPanel").objectReferenceValue = panel;

        Image panelBg = panel.GetComponent<Image>();
        if (panelBg == null) panelBg = panel.AddComponent<Image>();
        panelBg.color = Tokens.WithAlpha(Tokens.Ash, 0.92f);
        panelBg.raycastTarget = true;

        GameObject container = new GameObject("SoloContent");
        container.transform.SetParent(panel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(560f, 520f);

        Image containerBg = container.AddComponent<Image>();
        var sprites = UiSprites.Instance;
        if (sprites != null && sprites.PanelUmber != null)
            UiSprites.ApplySliced(containerBg, sprites.PanelUmber);
        else
            containerBg.color = Tokens.WithAlpha(Tokens.Umber, 0.95f);
        containerBg.raycastTarget = false;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = Tokens.Space2;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(
            (int)Tokens.Space4, (int)Tokens.Space4,
            (int)Tokens.Space3, (int)Tokens.Space3);

        EditorUiFactory.CreateText(container.transform, "SoloTitle", "SOLO GAME", (int)Tokens.TextH2,
            FontStyles.Bold, Tokens.BoneBright, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        EditorUiFactory.CreateText(container.transform, "PlayerCountLabel", "Number of Players", (int)Tokens.TextBody,
            FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        GameObject countRow = EditorUiFactory.CreateHorizontalRow(container.transform, "PlayerCountRow", (int)Tokens.Space2);

        GameObject btn4 = CreateChromeButton(countRow.transform, "Players4Button", "4 Players", 150f, Tokens.MinTouchTarget);
        managerSO.FindProperty("players4Button").objectReferenceValue = btn4.GetComponent<Button>();

        GameObject btn6 = CreateChromeButton(countRow.transform, "Players6Button", "6 Players", 150f, Tokens.MinTouchTarget);
        managerSO.FindProperty("players6Button").objectReferenceValue = btn6.GetComponent<Button>();

        GameObject btn8 = CreateChromeButton(countRow.transform, "Players8Button", "8 Players", 150f, Tokens.MinTouchTarget);
        managerSO.FindProperty("players8Button").objectReferenceValue = btn8.GetComponent<Button>();

        EditorUiFactory.CreateText(container.transform, "DifficultyLabel", "AI Difficulty", (int)Tokens.TextBody,
            FontStyles.Normal, Tokens.Bone, TextAlignmentOptions.Center, heightPadding: 14, raycastTarget: false);

        GameObject diffDropdown = EditorUiFactory.CreateDropdown(container.transform, "DifficultyDropdown", 250, 48,
            backgroundColor: Tokens.CharredWood);
        managerSO.FindProperty("difficultyDropdown").objectReferenceValue =
            diffDropdown.GetComponent<TMP_Dropdown>();

        CreateSpacer(container.transform, Tokens.Space2);

        GameObject bottomRow = EditorUiFactory.CreateHorizontalRow(container.transform, "BottomRow", (int)Tokens.Space3);

        GameObject backBtn = CreateChromeButton(bottomRow.transform, "BackButton", "Back", 150f, Tokens.MinTouchTarget);
        managerSO.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();

        GameObject playBtn = CreateChromeButton(bottomRow.transform, "PlayButton", "PLAY", 200f, Tokens.MinTouchTarget);
        managerSO.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();

        return panel;
    }

    private static GameObject CreateChromeButton(Transform parent, string name, string label,
        float width, float height, bool danger = false)
    {
        var sprites = UiSprites.Instance;

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image bgImg = btnObj.AddComponent<Image>();
        Sprite idle = null;
        if (sprites != null)
        {
            idle = danger ? sprites.ButtonBlood : sprites.ButtonBronze;
            UiSprites.ApplySliced(bgImg, idle);
        }
        bgImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);
        textRect.offsetMin = new Vector2(8, 2);
        textRect.offsetMax = new Vector2(-8, -2);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = Tokens.TextLabel;
        tmp.characterSpacing = Tokens.TrackingLabel * 100f;
        tmp.color = Tokens.Bone;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        var refs = FontRefs.Instance;
        if (refs != null && refs.Label != null) tmp.font = refs.Label;

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;

        var ignite = btnObj.AddComponent<IgniteButton>();
        SerializedObject igniteSO = new SerializedObject(ignite);
        igniteSO.FindProperty("background").objectReferenceValue = bgImg;
        igniteSO.FindProperty("idleSprite").objectReferenceValue = idle;
        igniteSO.FindProperty("ignitedSprite").objectReferenceValue = sprites != null ? sprites.ButtonEmber : null;
        igniteSO.FindProperty("pressedSprite").objectReferenceValue = sprites != null ? sprites.ButtonBronzePressed : null;
        igniteSO.FindProperty("label").objectReferenceValue = tmp;
        igniteSO.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.Bone;
        igniteSO.ApplyModifiedProperties();

        return btnObj;
    }

    // ===================== HELPERS =====================

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateFullscreenImage(Transform parent, string name, Color color, int siblingHint)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        go.transform.SetSiblingIndex(Mathf.Max(0, siblingHint + parent.childCount));
        return go;
    }

    private static Image FindNamedImage(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void ClearArrayProp(SerializedObject so, string propName)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null && p.isArray) p.arraySize = 0;
    }
}
#endif
