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
/// T750 Phase 4: restyle existing Lobby.unity to DESIGN.md §8.
/// Dialog-free core for MCP. Does NOT recreate the whole scene from scratch —
/// mutates SafeArea panels in place and wires env video/still.
/// </summary>
public static class LobbyRestyleSetup
{
    private const string ScenePath = "Assets/Scenes/Lobby.unity";
    private const string LobbyStillPath = "Assets/Art/Env/Lobby/lobby_bg.jpg";
    private const string LobbyVideoPingPongPath = "Assets/Art/Env/Lobby/lobby_video_pingpong.mp4";
    private const string LobbyVideoPath = "Assets/Art/Env/Lobby/lobby_video.mp4";

    [MenuItem("Tools/Game/Restyle Lobby (T750 §8)")]
    public static void RestyleLobbyMenu()
    {
        if (!EditorUtility.DisplayDialog("T750: Restyle Lobby",
            "Restyle Lobby scene to DESIGN.md §8:\n" +
            "  - lobby video ping-pong + still poster\n" +
            "  - player slots (empty/filled/ready)\n" +
            "  - room code tap-to-copy\n" +
            "  - bronze Start in thumb zone\n" +
            "  - tokenized chrome / Cinzel titles\n\nContinue?",
            "Restyle", "Cancel"))
            return;

        string report = RestyleLobbyCore();
        EditorUtility.DisplayDialog("T750: Lobby Restyle Complete", report, "OK");
    }

    /// <summary>Dialog-free core for MCP / batch.</summary>
    public static string RestyleLobbyCore()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == null || !scene.name.Contains("Lobby"))
        {
            if (System.IO.File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
                return "ABORT: Lobby scene missing at " + ScenePath;
            scene = EditorSceneManager.GetActiveScene();
        }

        EnsureLobbyStillImport();

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            return "ABORT: No Canvas in Lobby scene.";

        // Lobby.unity was originally built with withEventSystem:false — without this,
        // loading Lobby from MainMenu destroys the menu EventSystem and NO buttons work.
        EnsureEventSystem();

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        LobbyUI lobbyUI = Object.FindObjectOfType<LobbyUI>(true);
        if (lobbyUI == null)
            return "ABORT: No LobbyUI in Lobby scene.";

        Undo.RegisterCompleteObjectUndo(lobbyUI, "T750 Restyle Lobby");
        SerializedObject so = new SerializedObject(lobbyUI);

        // Background outside SafeArea
        EnsureLobbyBackground(canvas.transform);

        Transform safe = EnsureSafeArea(canvas.transform);

        // Move panels under SafeArea if needed
        ReparentIfNeeded(canvas.transform, safe, "ConnectionPanel");
        ReparentIfNeeded(canvas.transform, safe, "RoomBrowserPanel");
        ReparentIfNeeded(canvas.transform, safe, "RoomInteriorPanel");

        // Restyle each panel
        RestyleConnectionPanel(safe, so);
        RestyleRoomBrowserPanel(safe, so);
        RestyleRoomInteriorPanel(safe, so);

        // Critical: no opaque fullscreen plates over the video
        EnsureBackgroundShowsThrough(canvas.transform, safe);

        // Default panel visibility in the saved scene (runtime LobbyUI.Start re-asserts)
        SetPanelActive(safe, "ConnectionPanel", true);
        SetPanelActive(safe, "RoomBrowserPanel", false);
        SetPanelActive(safe, "RoomInteriorPanel", false);
        SetPanelActive(safe, "ReconnectingOverlay", false);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lobbyUI);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();

        return "T750 §8 Lobby restyle complete.\n\n" +
               "  BG: lobby_bg still + lobby_video_pingpong (visible behind UI)\n" +
               "  EventSystem: ensured (required for all button clicks)\n" +
               "  Panels: transparent shells; only small cards use plate chrome\n" +
               "  Room interior: slots + room code + Start\n" +
               "  Buttons: IgniteButton bronze, ≥88px\n\n" +
               "Scene saved.";
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        // Project activeInputHandler=Input Manager (old) → StandaloneInputModule
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(esObj, "T750 Ensure EventSystem");
    }

    private static void SetPanelActive(Transform safe, string name, bool active)
    {
        Transform t = FindDeep(safe, name);
        if (t == null)
        {
            // overlays may live under canvas root
            var all = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) { t = all[i]; break; }
            }
        }
        if (t != null)
            t.gameObject.SetActive(active);
    }

    /// <summary>
    /// Kill leftover opaque fullscreen / huge Content Images that bury the env video.
    /// Connection keeps a small centered card only.
    /// </summary>
    private static void EnsureBackgroundShowsThrough(Transform canvas, Transform safe)
    {
        // Layer order: Base → Video → Vignette → SafeArea
        Transform bgBase = FindDeep(canvas, "BG_Base");
        Transform bgVideo = FindDeep(canvas, "BG_Video");
        Transform bgVig = FindDeep(canvas, "BG_Vignette");
        if (bgBase != null) bgBase.SetAsFirstSibling();
        if (bgVideo != null) bgVideo.SetSiblingIndex(1);
        if (bgVig != null) bgVig.SetSiblingIndex(2);
        if (safe != null) safe.SetAsLastSibling();

        // Any direct "Background" leftover
        Transform legacy = canvas.Find("Background");
        if (legacy != null)
        {
            var limg = legacy.GetComponent<Image>();
            if (limg != null)
            {
                limg.color = Tokens.WithAlpha(Tokens.Ash, 0f);
                limg.raycastTarget = false;
            }
        }

        string[] panels = { "ConnectionPanel", "RoomBrowserPanel", "RoomInteriorPanel" };
        foreach (string name in panels)
        {
            Transform panel = FindDeep(safe, name);
            if (panel == null) continue;

            // Fullscreen panel shell must be fully clear
            Image shell = panel.GetComponent<Image>();
            if (shell != null)
            {
                shell.color = Tokens.WithAlpha(Tokens.Ash, 0f);
                shell.raycastTarget = false;
                shell.enabled = true;
            }

            Transform content = panel.Find("Content");
            if (content == null) content = FindDeep(panel, "Content");
            if (content == null) continue;

            Image contentImg = content.GetComponent<Image>();
            if (contentImg == null) continue;

            // Connection keeps a readable card (semi plate); browser/interior float over video
            if (name == "ConnectionPanel")
            {
                // Keep 9-slice plate but drop solid opacity if it was a flat color wash
                if (contentImg.sprite == null)
                    contentImg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.82f);
                // If sprite plate, leave white at full — grain is baked; card is small
                contentImg.raycastTarget = false;
            }
            else
            {
                // Remove the huge opaque centered plate entirely
                contentImg.color = Tokens.WithAlpha(Tokens.Ash, 0f);
                contentImg.raycastTarget = false;
                contentImg.enabled = false;
            }
            EditorUtility.SetDirty(contentImg);
        }

        // Soft vignette only — do not black out the art
        if (bgVig != null)
        {
            var vImg = bgVig.GetComponent<Image>();
            if (vImg != null)
            {
                // Procedural vignette sprite uses baked alpha; keep Image white, enabled
                vImg.color = Color.white;
                vImg.raycastTarget = false;
                vImg.enabled = true;
            }
        }
    }

    // ─── Background ───────────────────────────────────────────

    private static void EnsureLobbyBackground(Transform canvasTransform)
    {
        // BG_Root owns RectMask2D — never mask the Canvas (clips TMP titles)
        RectTransform bgRoot = BackgroundController.EnsureBgRoot(canvasTransform);

        // Remove plain solid "Background" if present (old factory)
        Transform oldBg = canvasTransform.Find("Background");
        if (oldBg != null)
        {
            // Keep as base layer rename if no BG_Base yet
            if (FindDeep(canvasTransform, "BG_Base") == null)
                oldBg.name = "BG_Base";
            else
                Object.DestroyImmediate(oldBg.gameObject);
        }

        // Find anywhere under canvas, then reparent into BG_Root (avoid duplicate layers)
        Image baseImg = FindNamedImage(canvasTransform, "BG_Base");
        if (baseImg == null)
            baseImg = GetOrCreateFullscreenImage(bgRoot, "BG_Base", Tokens.Ash, 0);
        else
        {
            baseImg.transform.SetParent(bgRoot, false);
            StretchFull(baseImg.rectTransform);
        }
        Image vignetteImg = FindNamedImage(canvasTransform, "BG_Vignette");
        if (vignetteImg == null)
            vignetteImg = GetOrCreateFullscreenImage(bgRoot, "BG_Vignette", Color.white, 2);
        else
        {
            vignetteImg.transform.SetParent(bgRoot, false);
            StretchFull(vignetteImg.rectTransform);
        }

        Transform videoT = FindDeep(canvasTransform, "BG_Video");
        GameObject videoGo;
        if (videoT == null)
        {
            videoGo = new GameObject("BG_Video");
            videoGo.transform.SetParent(bgRoot, false);
            StretchFull(videoGo.AddComponent<RectTransform>());
            videoGo.AddComponent<CanvasRenderer>();
            videoGo.AddComponent<RawImage>().raycastTarget = false;
            var vp = videoGo.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.RenderTexture;
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

        BackgroundController bg = canvasTransform.GetComponent<BackgroundController>();
        if (bg == null) bg = canvasTransform.gameObject.AddComponent<BackgroundController>();

        Image gradient = FindNamedImage(canvasTransform, "BG_Gradient");
        bg.WireLayers(baseImg, gradient, vignetteImg, null, videoRaw, videoPlayer);

        if (gradient != null)
        {
            gradient.enabled = false;
            gradient.gameObject.SetActive(false);
        }

        Sprite still = LoadSprite(LobbyStillPath);
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(LobbyVideoPingPongPath);
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(LobbyVideoPath);
        bg.ConfigureArtBackground(still, clip, loop: true);
        EditorUtility.SetDirty(bg);

        // Order inside BG_Root: base, video, vignette — SafeArea stays outside mask
        baseImg.transform.SetSiblingIndex(0);
        videoGo.transform.SetSiblingIndex(1);
        vignetteImg.transform.SetSiblingIndex(2);
        bgRoot.SetAsFirstSibling();
    }

    private static void EnsureLobbyStillImport()
    {
        var importer = AssetImporter.GetAtPath(LobbyStillPath) as TextureImporter;
        if (importer == null) return;
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
        if (dirty) importer.SaveAndReimport();
    }

    // ─── Panels ───────────────────────────────────────────────

    private static void RestyleConnectionPanel(Transform safe, SerializedObject so)
    {
        Transform panelT = FindDeep(safe, "ConnectionPanel");
        if (panelT == null) return;
        GameObject panel = panelT.gameObject;

        // Transparent shell — full-bleed video shows through
        Image shell = panel.GetComponent<Image>();
        if (shell == null) shell = panel.AddComponent<Image>();
        shell.color = Tokens.WithAlpha(Tokens.Ash, 0f);
        shell.raycastTarget = false;
        StretchFull(panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>());

        // Wipe old content and rebuild to match MainMenu language
        Transform oldContent = panelT.Find("Content");
        if (oldContent != null)
            Object.DestroyImmediate(oldContent.gameObject);

        // Centered card
        GameObject card = new GameObject("Content");
        card.transform.SetParent(panelT, false);
        RectTransform cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        // Wider card so "BATTLEGROUNDS" fits Cinzel without mid-word wrap
        cardRt.sizeDelta = new Vector2(640f, 460f);

        Image cardBg = card.AddComponent<Image>();
        var sprites = UiSprites.Instance;
        if (sprites != null && sprites.PanelCharred != null)
        {
            UiSprites.ApplySliced(cardBg, sprites.PanelCharred);
            // Slight transparency so env video glows through the card edges
            cardBg.color = Tokens.WithAlpha(Color.white, 0.88f);
        }
        else
            cardBg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.82f);
        cardBg.raycastTarget = false;

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = Tokens.Space3;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        // true so title / fields use full card width; thin sigil lives in a centered child row
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(
            (int)Tokens.Space4, (int)Tokens.Space4,
            (int)Tokens.Space4, (int)Tokens.Space4);

        // Title — two clean lines (never wrap mid-word like BATTLEGRO|UNDS)
        GameObject titleGo = CreatePlainText(card.transform, "TitleText", "TAROT\nBATTLEGROUNDS",
            Tokens.TextH1, Tokens.BoneBright, TextAlignmentOptions.Center, displayFont: true);
        var titleTmp = titleGo.GetComponent<TMP_Text>();
        titleTmp.enableWordWrapping = false; // explicit newlines only — never mid-word wrap
        titleTmp.overflowMode = TextOverflowModes.Overflow;
        titleTmp.enableAutoSizing = true;
        // Cinzel display floor (Tokens.TextDisplay note: never below 30) — no raw literals
        titleTmp.fontSizeMin = Tokens.TextH3;
        titleTmp.fontSizeMax = Tokens.TextH1;
        var titleLe = titleGo.GetComponent<LayoutElement>();
        titleLe.minHeight = Tokens.TextH1 * 2.3f;
        titleLe.preferredHeight = Tokens.TextH1 * 2.5f;
        titleLe.flexibleWidth = 1f;

        // Thin ember underline: full-width ROW + fixed-width child (VLG force-expand cannot fatten the bar)
        CreateCenteredSigilUnderline(card.transform, "HeroSigil", width: 220f, height: 6f);

        // Phase caption
        GameObject phaseGo = CreatePlainText(card.transform, "PhaseCaption", "OPENING THE GATE",
            Tokens.TextCaption, Tokens.BronzeBright, TextAlignmentOptions.Center, displayFont: false, labelFont: true);
        var phaseTmp = phaseGo.GetComponent<TMP_Text>();
        phaseTmp.characterSpacing = Tokens.TrackingLabel * 100f;
        phaseTmp.fontStyle = FontStyles.Bold;

        // Status body
        GameObject statusGo = CreatePlainText(card.transform, "ConnectionStatusText",
            "Seeking the circle...", Tokens.TextBody, Tokens.Bone, TextAlignmentOptions.Center, displayFont: false);
        so.FindProperty("connectionStatusText").objectReferenceValue = statusGo.GetComponent<TMP_Text>();

        // Name label + field
        CreatePlainText(card.transform, "NameLabel", "Your name",
            Tokens.TextCaption, Tokens.BoneDim, TextAlignmentOptions.Center, displayFont: false, labelFont: true);

        GameObject nameInputGo = new GameObject("PlayerNameInput");
        nameInputGo.transform.SetParent(card.transform, false);
        RectTransform nameRt = nameInputGo.AddComponent<RectTransform>();
        nameRt.sizeDelta = new Vector2(400f, Tokens.MinTouchTarget);
        Image nameBg = nameInputGo.AddComponent<Image>();
        if (sprites != null && sprites.PanelUmber != null)
            UiSprites.ApplySliced(nameBg, sprites.PanelUmber);
        else
            nameBg.color = Tokens.Umber;

        TMP_InputField input = nameInputGo.AddComponent<TMP_InputField>();
        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(nameInputGo.transform, false);
        RectTransform taRt = textArea.AddComponent<RectTransform>();
        StretchFull(taRt);
        taRt.offsetMin = new Vector2(Tokens.Space2, Tokens.Space1);
        taRt.offsetMax = new Vector2(-Tokens.Space2, -Tokens.Space1);
        textArea.AddComponent<RectMask2D>();

        GameObject placeholder = CreatePlainText(textArea.transform, "Placeholder", "Enter your name...",
            Tokens.TextBody, Tokens.BoneDim, TextAlignmentOptions.MidlineLeft, displayFont: false, labelFont: true);
        placeholder.GetComponent<TMP_Text>().raycastTarget = false;
        StretchFull(placeholder.GetComponent<RectTransform>());

        GameObject text = CreatePlainText(textArea.transform, "Text", "",
            Tokens.TextBody, Tokens.BoneBright, TextAlignmentOptions.MidlineLeft, displayFont: false);
        text.GetComponent<TMP_Text>().raycastTarget = false;
        StretchFull(text.GetComponent<RectTransform>());

        input.textViewport = taRt;
        input.textComponent = text.GetComponent<TextMeshProUGUI>();
        input.placeholder = placeholder.GetComponent<TMP_Text>();
        input.caretColor = Tokens.Ember;
        input.selectionColor = Tokens.WithAlpha(Tokens.Ember, 0.35f);
        input.fontAsset = FontRefs.Instance != null ? FontRefs.Instance.Body : null;

        LayoutElement nameLe = nameInputGo.AddComponent<LayoutElement>();
        nameLe.minHeight = Tokens.MinTouchTarget;
        nameLe.preferredHeight = Tokens.MinTouchTarget;
        nameLe.preferredWidth = 400f;

        so.FindProperty("playerNameInput").objectReferenceValue = input;
        StyleInputField(input, Tokens.MinTouchTarget);

        // Optional phase caption ref (LobbyUI soft-finds by name if unwired)
        var phaseProp = so.FindProperty("connectionPhaseCaption");
        if (phaseProp != null)
            phaseProp.objectReferenceValue = phaseGo.GetComponent<TMP_Text>();

        // Escape hatch — not stuck if Photon fails (Cinzel label, reliable hit target)
        GameObject backGo = new GameObject("ConnectionBackButton");
        backGo.transform.SetParent(card.transform, false);
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.sizeDelta = new Vector2(400f, Tokens.MinTouchTarget);
        Image backImg = backGo.AddComponent<Image>();
        Sprite bronze = sprites != null ? sprites.ButtonBronze : null;
        if (bronze != null) UiSprites.ApplySliced(backImg, bronze);
        else backImg.color = Tokens.Bronze;
        backImg.raycastTarget = true;
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        backBtn.targetGraphic = backImg;
        LayoutElement backLe = backGo.AddComponent<LayoutElement>();
        backLe.minHeight = Tokens.MinTouchTarget;
        backLe.preferredHeight = Tokens.MinTouchTarget;
        backLe.minWidth = 320f;
        backLe.preferredWidth = 400f;
        backLe.flexibleWidth = 1f;

        GameObject backLabel = CreatePlainText(backGo.transform, "Text (TMP)", "Return to Menu",
            Tokens.TextLabel, Tokens.BoneBright, TextAlignmentOptions.Center, displayFont: true, labelFont: false);
        StretchFull(backLabel.GetComponent<RectTransform>());
        var backLabelRt = backLabel.GetComponent<RectTransform>();
        backLabelRt.offsetMin = new Vector2(Tokens.Space3, Tokens.Space1);
        backLabelRt.offsetMax = new Vector2(-Tokens.Space3, -Tokens.Space1);
        var backTmp = backLabel.GetComponent<TMP_Text>();
        backTmp.raycastTarget = false;
        backTmp.enableWordWrapping = false;
        backTmp.overflowMode = TextOverflowModes.Overflow;
        backTmp.characterSpacing = Tokens.TrackingLabel * 100f;

        var ignite = backGo.AddComponent<IgniteButton>();
        SerializedObject iso = new SerializedObject(ignite);
        iso.FindProperty("background").objectReferenceValue = backImg;
        iso.FindProperty("idleSprite").objectReferenceValue = bronze;
        iso.FindProperty("ignitedSprite").objectReferenceValue = sprites != null ? sprites.ButtonEmber : null;
        iso.FindProperty("pressedSprite").objectReferenceValue = sprites != null ? sprites.ButtonBronzePressed : null;
        iso.FindProperty("label").objectReferenceValue = backTmp;
        iso.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.BoneBright;
        iso.ApplyModifiedProperties();

        // Name field uses full card width under force-expand
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 400f;

        EnsureReconnectingOverlay(safe, so);
        EditorUtility.SetDirty(panel);
    }

    private static void EnsureReconnectingOverlay(Transform safe, SerializedObject so)
    {
        Transform existing = FindDeep(safe, "ReconnectingOverlay");
        GameObject overlay;
        if (existing == null)
        {
            overlay = new GameObject("ReconnectingOverlay");
            overlay.transform.SetParent(safe, false);
            StretchFull(overlay.AddComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = Tokens.WithAlpha(Tokens.Ash, 0.72f);
            dim.raycastTarget = true;

            GameObject card = new GameObject("Card");
            card.transform.SetParent(overlay.transform, false);
            RectTransform cRt = card.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(480f, 200f);
            Image cBg = card.AddComponent<Image>();
            var sprites = UiSprites.Instance;
            if (sprites != null && sprites.PanelCharred != null)
                UiSprites.ApplySliced(cBg, sprites.PanelCharred);
            else
                cBg.color = Tokens.CharredWood;

            VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = Tokens.Space2;
            vlg.padding = new RectOffset((int)Tokens.Space4, (int)Tokens.Space4, (int)Tokens.Space4, (int)Tokens.Space4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            CreatePlainText(card.transform, "PhaseCaption", "REJOINING THE CIRCLE",
                Tokens.TextCaption, Tokens.BronzeBright, TextAlignmentOptions.Center, false, true);
            GameObject msg = CreatePlainText(card.transform, "ReconnectingText", "The path frayed. Holding the gate...",
                Tokens.TextBody, Tokens.Bone, TextAlignmentOptions.Center, false, false);

            so.FindProperty("reconnectingOverlay").objectReferenceValue = overlay;
            so.FindProperty("reconnectingText").objectReferenceValue = msg.GetComponent<TMP_Text>();
            overlay.SetActive(false);
        }
        else
        {
            overlay = existing.gameObject;
            so.FindProperty("reconnectingOverlay").objectReferenceValue = overlay;
            var msg = FindDeep(existing, "ReconnectingText");
            if (msg != null)
                so.FindProperty("reconnectingText").objectReferenceValue = msg.GetComponent<TMP_Text>();
            overlay.SetActive(false);
        }
    }

    private static GameObject CreatePlainText(Transform parent, string name, string text,
        float size, Color color, TextAlignmentOptions align, bool displayFont, bool labelFont = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, size + Tokens.Space2);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        var refs = FontRefs.Instance;
        if (displayFont && refs != null && refs.Display != null) tmp.font = refs.Display;
        else if (labelFont && refs != null && refs.Label != null) tmp.font = refs.Label;
        else if (refs != null && refs.Body != null) tmp.font = refs.Body;
        if (displayFont)
        {
            tmp.characterSpacing = Tokens.TrackingDisplay * 100f;
            tmp.fontStyle = FontStyles.Bold;
        }
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = size + Tokens.Space1;
        le.preferredHeight = size + Tokens.Space2;
        return go;
    }

    private static void RestyleRoomBrowserPanel(Transform safe, SerializedObject so)
    {
        Transform panel = FindDeep(safe, "RoomBrowserPanel");
        if (panel == null) return;

        Image shell = panel.GetComponent<Image>();
        if (shell != null)
        {
            shell.color = Tokens.WithAlpha(Tokens.Ash, 0f);
            shell.raycastTarget = false;
        }

        // Content was a huge 800×700 plate at 95% alpha — kill plate; widen for button rows
        Transform content = panel.Find("Content") ?? FindDeep(panel, "Content");
        if (content != null)
        {
            Image cImg = content.GetComponent<Image>();
            if (cImg != null)
            {
                cImg.enabled = false;
                cImg.color = Tokens.WithAlpha(Tokens.Ash, 0f);
                cImg.raycastTarget = false;
            }
            var rt = content.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(960f, 700f);
            var cle = content.GetComponent<LayoutElement>();
            if (cle != null)
            {
                cle.minWidth = 900f;
                cle.preferredWidth = 960f;
            }
        }

        StyleTitleText(panel, "BrowserTitle", "Online Lobby", Tokens.TextH1, Tokens.BoneBright);
        // Match connection gate: no mid-word wrap on the browser title
        Transform browserTitleT = FindDeep(panel, "BrowserTitle");
        if (browserTitleT != null)
        {
            var bt = browserTitleT.GetComponent<TMP_Text>();
            if (bt != null)
            {
                bt.enableWordWrapping = false;
                bt.overflowMode = TextOverflowModes.Overflow;
            }
        }
        StyleBodyText(panel, "RoomsHeader", "Available Rooms", Tokens.TextH3, Tokens.BoneBright);
        StyleBodyText(panel, "NoRoomsText", "No rooms available. Create one!", Tokens.TextBody, Tokens.BoneDim);

        StyleInputField(so.FindProperty("roomNameInput").objectReferenceValue as TMP_InputField,
            Tokens.MinTouchTarget, preferredWidth: 280f);
        // 4 / 6 / 8 player picker — full rebuild of template so options are readable
        StyleDropdown(so.FindProperty("maxPlayersDropdown").objectReferenceValue as TMP_Dropdown,
            Tokens.MinTouchTarget, preferredWidth: 220f);

        // Wide enough for Cinzel labels (160px was clipping "Join Random" / "Create Room").
        // WO-05: real sizeDelta + LayoutElement mins (not inert mins alone) via StyleAsIgniteButton.
        StyleAsIgniteButton(so.FindProperty("createRoomButton").objectReferenceValue as Button,
            "Create Room", Tokens.MinTouchTarget, primary: true, minWidth: 300f);
        StyleAsIgniteButton(so.FindProperty("joinRandomButton").objectReferenceValue as Button,
            "Join Random", Tokens.MinTouchTarget, primary: true, minWidth: 300f);
        // Same copy + Cinzel as connection gate
        StyleAsIgniteButton(so.FindProperty("backToMenuButton").objectReferenceValue as Button,
            "Return to Menu", Tokens.MinTouchTarget, primary: false, minWidth: 320f, forceDisplayFont: true);

        // Rows: give children room; don't crush buttons into 160px
        FixHorizontalRow(FindDeep(panel, "CreateRoomRow"), Tokens.Space2);
        FixHorizontalRow(FindDeep(panel, "ActionRow"), Tokens.Space3);

        // Room list scroll was solid CharredWood — soften so video shows around rows
        Transform scroll = FindDeep(panel, "RoomListScroll");
        if (scroll != null)
        {
            Image sImg = scroll.GetComponent<Image>();
            if (sImg != null)
            {
                sImg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.45f);
                sImg.raycastTarget = true; // still need to catch scroll
            }
            var sle = scroll.GetComponent<LayoutElement>();
            if (sle != null)
            {
                sle.minWidth = 900f;
                sle.preferredWidth = 920f;
            }
            var srt = scroll.GetComponent<RectTransform>();
            if (srt != null && srt.sizeDelta.x < 900f)
                srt.sizeDelta = new Vector2(920f, srt.sizeDelta.y);
        }

        RestyleRoomListEntryPrefab();
    }

    private static void RestyleRoomInteriorPanel(Transform safe, SerializedObject so)
    {
        Transform panel = FindDeep(safe, "RoomInteriorPanel");
        if (panel == null) return;

        Image shell = panel.GetComponent<Image>();
        if (shell != null)
        {
            shell.color = Tokens.WithAlpha(Tokens.Ash, 0f);
            shell.raycastTarget = false;
        }

        // Find or build Content root
        Transform content = panel.Find("Content");
        if (content == null)
        {
            // search deep
            content = FindDeep(panel, "Content");
        }

        // Same as browser: no opaque content plate over the env art
        if (content != null)
        {
            Image cImg = content.GetComponent<Image>();
            if (cImg != null)
            {
                cImg.enabled = false;
                cImg.color = Tokens.WithAlpha(Tokens.Ash, 0f);
                cImg.raycastTarget = false;
            }
        }

        // Room title → room code style (Numeric / Display)
        var roomTitle = so.FindProperty("roomTitleText").objectReferenceValue as TMP_Text;
        if (roomTitle != null)
        {
            roomTitle.fontSize = Tokens.TextH1;
            roomTitle.color = Tokens.BoneBright;
            roomTitle.characterSpacing = Tokens.TrackingDisplay * 100f;
            roomTitle.alignment = TextAlignmentOptions.Center;
            var refs = FontRefs.Instance;
            if (refs != null && refs.Numeric != null) roomTitle.font = refs.Numeric;
            else if (refs != null && refs.Display != null) roomTitle.font = refs.Display;
            roomTitle.fontStyle = FontStyles.Bold;
            // Enable raycast for tap-to-copy
            roomTitle.raycastTarget = true;
            var le = roomTitle.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = Tokens.MinTouchTarget; le.preferredHeight = Tokens.MinTouchTarget; }
        }

        // Hide legacy plain player list text if present — slots replace it
        var playerListText = so.FindProperty("playerListText").objectReferenceValue as TMP_Text;
        if (playerListText != null)
            playerListText.gameObject.SetActive(false);

        // Ensure PlayerSlotColumn
        Transform slotCol = FindDeep(panel, "PlayerSlotColumn");
        if (slotCol == null && content != null)
        {
            GameObject col = new GameObject("PlayerSlotColumn");
            col.transform.SetParent(content, false);
            RectTransform rt = col.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 500f);
            VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = Tokens.Space2;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            LayoutElement le = col.AddComponent<LayoutElement>();
            le.minHeight = Tokens.SlotHeight * 4f + Tokens.Space2 * 3f;
            le.preferredHeight = le.minHeight;
            le.flexibleWidth = 1f;
            // Insert near top after title
            if (roomTitle != null)
                col.transform.SetSiblingIndex(roomTitle.transform.GetSiblingIndex() + 1);
            slotCol = col.transform;
        }

        if (slotCol != null)
        {
            var slotProp = so.FindProperty("playerSlotContainer");
            if (slotProp != null)
                slotProp.objectReferenceValue = slotCol;
        }

        // Status BoneDim + design font
        var status = so.FindProperty("roomStatusText").objectReferenceValue as TMP_Text;
        if (status != null)
        {
            status.fontSize = Tokens.TextCaption;
            status.color = Tokens.BoneDim;
            status.fontStyle = FontStyles.Normal;
            var refs = FontRefs.Instance;
            if (refs != null && refs.Body != null) status.font = refs.Body;
        }

        // Start = bronze primary, large thumb
        StyleAsIgniteButton(so.FindProperty("startGameButton").objectReferenceValue as Button,
            "Start", Tokens.SlotHeight, primary: true, minWidth: 280f);
        StyleAsIgniteButton(so.FindProperty("leaveRoomButton").objectReferenceValue as Button,
            "Leave", Tokens.MinTouchTarget, primary: false, minWidth: 180f);
        FixHorizontalRow(FindDeep(panel, "ButtonsRow"), Tokens.Space3);

        // Move start to feel bottom-right: parent ButtonsRow alignment
        Transform buttonsRow = FindDeep(panel, "ButtonsRow");
        if (buttonsRow != null)
        {
            var hlg = buttonsRow.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.spacing = Tokens.Space3;
                hlg.reverseArrangement = false;
            }
            // Ensure Leave then Start order (Start on the right)
            var start = so.FindProperty("startGameButton").objectReferenceValue as Button;
            var leave = so.FindProperty("leaveRoomButton").objectReferenceValue as Button;
            if (start != null) start.transform.SetAsLastSibling();
            if (leave != null) leave.transform.SetAsFirstSibling();
        }
    }

    // ─── Button / text helpers ────────────────────────────────

    private static void FixHorizontalRow(Transform row, float spacing)
    {
        if (row == null) return;
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childScaleWidth = false;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        LayoutElement rowLe = row.GetComponent<LayoutElement>();
        if (rowLe == null) rowLe = row.gameObject.AddComponent<LayoutElement>();
        rowLe.minHeight = Tokens.MinTouchTarget;
        rowLe.preferredHeight = Tokens.MinTouchTarget;
        rowLe.flexibleWidth = 1f;
    }

    /// <summary>
    /// Full-width layout row with a fixed-width underline centered inside.
    /// Prevents VerticalLayoutGroup.childForceExpandWidth from turning a 6px strip into a fat bar.
    /// </summary>
    private static GameObject CreateCenteredSigilUnderline(Transform parent, string name,
        float width = 220f, float height = 6f)
    {
        GameObject row = new GameObject(name + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, height);

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = height;
        rowLe.preferredHeight = height;
        rowLe.flexibleWidth = 1f;
        rowLe.flexibleHeight = 0f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 0f;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        GameObject sigil = new GameObject(name);
        sigil.transform.SetParent(row.transform, false);
        RectTransform sigilRt = sigil.AddComponent<RectTransform>();
        sigilRt.sizeDelta = new Vector2(width, height);

        Image sigilImg = sigil.AddComponent<Image>();
        var sprites = UiSprites.Instance;
        if (sprites != null && sprites.UnderlineEmber != null)
            UiSprites.ApplySliced(sigilImg, sprites.UnderlineEmber);
        else
            sigilImg.color = Tokens.Ember;
        sigilImg.raycastTarget = false;

        LayoutElement sigilLe = sigil.AddComponent<LayoutElement>();
        sigilLe.minWidth = width;
        sigilLe.preferredWidth = width;
        sigilLe.minHeight = height;
        sigilLe.preferredHeight = height;
        sigilLe.flexibleWidth = 0f;
        sigilLe.flexibleHeight = 0f;

        CanvasGroup cg = sigil.AddComponent<CanvasGroup>();
        cg.alpha = 0.9f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        return row;
    }

    private static void StyleAsIgniteButton(Button btn, string label, float height,
        bool primary, float minWidth = 0f, bool forceDisplayFont = false)
    {
        if (btn == null) return;
        var sprites = UiSprites.Instance;
        Image img = btn.GetComponent<Image>();
        if (img == null) img = btn.gameObject.AddComponent<Image>();

        Sprite idle = null;
        if (sprites != null)
        {
            idle = sprites.ButtonBronze;
            UiSprites.ApplySliced(img, idle);
        }
        else
        {
            img.color = primary ? Tokens.Bronze : Tokens.CharredWood;
        }
        img.raycastTarget = true;

        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;

        // Multi-word Cinzel needs real width — 160px clips "Join Random" / "Create Room"
        string finalLabel = string.IsNullOrEmpty(label) ? (btn.GetComponentInChildren<TMP_Text>(true)?.text ?? "") : label;
        float fontSize = Tokens.TextLabel;
        // ~0.62em average Cinzel advance + horizontal padding for 9-slice notches
        float estimated = finalLabel.Length * fontSize * 0.62f + Tokens.Space5 * 2f;
        float w = Mathf.Max(minWidth > 0f ? minWidth : Tokens.MinTouchTarget * 2f, estimated, Tokens.MinTouchTarget * 2f);
        w = Mathf.Min(w, 400f); // "Return to Menu" needs headroom
        // Gate A: height is a real canvas size, never below MinTouchTarget (WO-04 F3 lesson)
        float h = Mathf.Max(height, Tokens.MinTouchTarget);

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(w, h);

        LayoutElement le = btn.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
        le.minWidth = w;
        le.preferredWidth = w;
        le.flexibleWidth = 0f;

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            if (!string.IsNullOrEmpty(label)) tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Tokens.BoneBright;
            tmp.characterSpacing = Tokens.TrackingLabel * 100f;
            var refs = FontRefs.Instance;
            // Cinzel for primary / forced Return; Label for other secondary
            bool useDisplay = primary || forceDisplayFont;
            if (useDisplay && refs != null && refs.Display != null) tmp.font = refs.Display;
            else if (refs != null && refs.Label != null) tmp.font = refs.Label;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            // Inset text from notched edges
            RectTransform tr = tmp.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(Tokens.Space3, Tokens.Space1);
            tr.offsetMax = new Vector2(-Tokens.Space3, -Tokens.Space1);
        }

        IgniteButton ignite = btn.GetComponent<IgniteButton>();
        if (ignite == null) ignite = btn.gameObject.AddComponent<IgniteButton>();
        SerializedObject iso = new SerializedObject(ignite);
        iso.FindProperty("background").objectReferenceValue = img;
        iso.FindProperty("idleSprite").objectReferenceValue = idle;
        iso.FindProperty("ignitedSprite").objectReferenceValue = sprites != null ? sprites.ButtonEmber : null;
        iso.FindProperty("pressedSprite").objectReferenceValue = sprites != null ? sprites.ButtonBronzePressed : null;
        iso.FindProperty("label").objectReferenceValue = tmp;
        iso.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.BoneBright;
        iso.ApplyModifiedProperties();
        EditorUtility.SetDirty(btn);
    }

    private static void StyleTitleText(Transform root, string name, string text, float size, Color color)
    {
        Transform t = FindDeep(root, name);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        if (!string.IsNullOrEmpty(text)) tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.characterSpacing = Tokens.TrackingDisplay * 100f;
        tmp.fontStyle = FontStyles.Bold;
        var refs = FontRefs.Instance;
        if (refs != null && refs.Display != null) tmp.font = refs.Display;
        EditorUtility.SetDirty(tmp);
    }

    private static void StyleBodyText(Transform root, string name, string text, float size, Color color)
    {
        Transform t = FindDeep(root, name);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        if (!string.IsNullOrEmpty(text)) tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        var refs = FontRefs.Instance;
        if (refs != null && refs.Body != null) tmp.font = refs.Body;
        EditorUtility.SetDirty(tmp);
    }

    private static void StyleBodyText(Transform root, string name, float size, Color color)
    {
        StyleBodyText(root, name, null, size, color);
    }

    private static void StyleInputField(TMP_InputField field, float height, float preferredWidth = 0f)
    {
        if (field == null) return;
        var refs = FontRefs.Instance;
        TMP_FontAsset body = refs != null ? refs.Body : null;
        TMP_FontAsset label = refs != null ? refs.Label : body;

        float w = preferredWidth > 0f ? preferredWidth : 280f;
        var le = field.GetComponent<LayoutElement>();
        if (le == null) le = field.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.minWidth = w;
        le.preferredWidth = w;
        le.flexibleWidth = 1f;

        var rt = field.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(w, height);

        // Background
        Image bg = field.GetComponent<Image>();
        if (bg != null)
        {
            var sprites = UiSprites.Instance;
            if (sprites != null && sprites.PanelCharred != null)
                UiSprites.ApplySliced(bg, sprites.PanelCharred);
            else
                bg.color = Tokens.CharredWood;
        }

        if (field.textComponent != null)
        {
            field.textComponent.fontSize = Tokens.TextBody;
            field.textComponent.color = Tokens.BoneBright;
            if (body != null) field.textComponent.font = body;
            field.textComponent.raycastTarget = false;
        }
        if (field.placeholder is TMP_Text ph)
        {
            ph.fontSize = Tokens.TextBody;
            ph.color = Tokens.BoneDim;
            if (label != null) ph.font = label;
            ph.raycastTarget = false;
        }
        field.caretColor = Tokens.Ember;
        field.selectionColor = Tokens.WithAlpha(Tokens.Ember, 0.35f);
        EditorUtility.SetDirty(field);
    }

    /// <summary>
    /// Tokenized TMP_Dropdown: dark plate, readable Bone labels, ember hover —
    /// not Unity default grey/white toggle colors. Template tall enough for 3 options.
    /// </summary>
    private static void StyleDropdown(TMP_Dropdown dropdown, float height, float preferredWidth = 0f)
    {
        if (dropdown == null) return;
        var refs = FontRefs.Instance;
        var sprites = UiSprites.Instance;
        TMP_FontAsset labelFont = refs != null && refs.Label != null ? refs.Label
            : (refs != null ? refs.Body : null);
        TMP_FontAsset bodyFont = refs != null ? refs.Body : labelFont;

        float w = preferredWidth > 0f ? preferredWidth : 220f;
        var le = dropdown.GetComponent<LayoutElement>();
        if (le == null) le = dropdown.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.minWidth = w;
        le.preferredWidth = w;
        le.flexibleWidth = 0f;

        var rt = dropdown.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(w, height);

        Image bg = dropdown.GetComponent<Image>();
        if (bg == null) bg = dropdown.gameObject.AddComponent<Image>();
        if (sprites != null && sprites.PanelCharred != null)
            UiSprites.ApplySliced(bg, sprites.PanelCharred);
        else
            bg.color = Tokens.CharredWood;
        bg.raycastTarget = true;

        // Caption on the closed control
        if (dropdown.captionText != null)
        {
            dropdown.captionText.fontSize = Tokens.TextLabel;
            dropdown.captionText.color = Tokens.BoneBright;
            dropdown.captionText.fontStyle = FontStyles.Bold;
            dropdown.captionText.enableWordWrapping = false;
            dropdown.captionText.overflowMode = TextOverflowModes.Ellipsis;
            dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            if (labelFont != null) dropdown.captionText.font = labelFont;
            var capRt = dropdown.captionText.rectTransform;
            capRt.anchorMin = Vector2.zero;
            capRt.anchorMax = Vector2.one;
            capRt.offsetMin = new Vector2(Tokens.Space2, Tokens.Space1);
            capRt.offsetMax = new Vector2(-Tokens.Space4, -Tokens.Space1); // room for arrow
        }

        // Soft ember interaction — no Unity default white flash
        var colors = dropdown.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Tokens.WithAlpha(Tokens.Ember, 0.25f);
        colors.pressedColor = Tokens.WithAlpha(Tokens.Ember, 0.40f);
        colors.selectedColor = Tokens.WithAlpha(Tokens.BronzeBright, 0.30f);
        colors.disabledColor = Tokens.WithAlpha(Tokens.BoneDim, 0.35f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        dropdown.colors = colors;
        dropdown.transition = Selectable.Transition.ColorTint;
        dropdown.targetGraphic = bg;

        // Rebuild dropdown list template (item bg + layout + all 3 options visible)
        RebuildDropdownTemplate(dropdown, w, height, labelFont, bodyFont, sprites);

        // Seed options so the closed caption is correct even before Play
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "4 Players", "6 Players", "8 Players" });
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        EditorUtility.SetDirty(dropdown);
    }

    private static void RebuildDropdownTemplate(TMP_Dropdown dropdown, float width, float rowHeight,
        TMP_FontAsset labelFont, TMP_FontAsset bodyFont, UiSprites sprites)
    {
        // Destroy previous template if present
        if (dropdown.template != null)
        {
            Object.DestroyImmediate(dropdown.template.gameObject);
            dropdown.template = null;
        }

        // Also remove leftover Template children by name
        Transform existing = dropdown.transform.Find("Template");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        float listHeight = rowHeight * 3f + Tokens.Space1 * 2f; // 3 options fully visible

        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdown.transform, false);
        RectTransform templateRt = template.AddComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0f, 0f);
        templateRt.anchorMax = new Vector2(1f, 0f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = new Vector2(0f, 2f);
        templateRt.sizeDelta = new Vector2(0f, listHeight);

        Image templateBg = template.AddComponent<Image>();
        if (sprites != null && sprites.PanelCharred != null)
            UiSprites.ApplySliced(templateBg, sprites.PanelCharred);
        else
            templateBg.color = Tokens.CharredWood;
        templateBg.raycastTarget = true;

        // Drop shadow of depth — slight bronze rim via secondary plate optional skip
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform vpRt = viewport.AddComponent<RectTransform>();
        StretchFull(vpRt);
        vpRt.offsetMin = new Vector2(Tokens.Space1, Tokens.Space1);
        vpRt.offsetMax = new Vector2(-Tokens.Space1, -Tokens.Space1);
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = Tokens.WithAlpha(Tokens.Ash, 0.92f);
        vpImg.raycastTarget = true;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.spacing = 2f;
        contentVlg.padding = new RectOffset(0, 0, 0, 0);
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = contentRt;

        // Item prototype
        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        RectTransform itemRt = item.AddComponent<RectTransform>();
        itemRt.anchorMin = new Vector2(0f, 0.5f);
        itemRt.anchorMax = new Vector2(1f, 0.5f);
        itemRt.sizeDelta = new Vector2(0f, rowHeight);
        LayoutElement itemLe = item.AddComponent<LayoutElement>();
        itemLe.minHeight = rowHeight;
        itemLe.preferredHeight = rowHeight;

        Image itemBg = item.AddComponent<Image>();
        itemBg.color = Tokens.WithAlpha(Tokens.Umber, 0.0f); // transparent idle; tint via toggle
        itemBg.raycastTarget = true;

        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemBg;
        toggle.isOn = false;
        var tColors = toggle.colors;
        tColors.normalColor = Tokens.WithAlpha(Tokens.Umber, 0.0f);
        tColors.highlightedColor = Tokens.WithAlpha(Tokens.Ember, 0.35f);
        tColors.pressedColor = Tokens.WithAlpha(Tokens.Ember, 0.55f);
        tColors.selectedColor = Tokens.WithAlpha(Tokens.Bronze, 0.55f);
        tColors.disabledColor = Tokens.WithAlpha(Tokens.Ash, 0.3f);
        tColors.colorMultiplier = 1f;
        tColors.fadeDuration = 0.06f;
        toggle.colors = tColors;
        toggle.transition = Selectable.Transition.ColorTint;

        // Checkmark (selected indicator — thin ember bar left)
        GameObject check = new GameObject("Item Checkmark");
        check.transform.SetParent(item.transform, false);
        RectTransform checkRt = check.AddComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0f, 0.2f);
        checkRt.anchorMax = new Vector2(0f, 0.8f);
        checkRt.pivot = new Vector2(0f, 0.5f);
        checkRt.anchoredPosition = new Vector2(6f, 0f);
        checkRt.sizeDelta = new Vector2(4f, 0f);
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = Tokens.Ember;
        checkImg.raycastTarget = false;
        toggle.graphic = checkImg;

        // Item label
        GameObject itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(item.transform, false);
        RectTransform ilRt = itemLabel.AddComponent<RectTransform>();
        ilRt.anchorMin = Vector2.zero;
        ilRt.anchorMax = Vector2.one;
        ilRt.offsetMin = new Vector2(Tokens.Space3, Tokens.Space1);
        ilRt.offsetMax = new Vector2(-Tokens.Space2, -Tokens.Space1);
        TextMeshProUGUI itemTmp = itemLabel.AddComponent<TextMeshProUGUI>();
        itemTmp.text = "Option";
        itemTmp.fontSize = Tokens.TextLabel;
        itemTmp.color = Tokens.BoneBright;
        itemTmp.fontStyle = FontStyles.Bold;
        itemTmp.alignment = TextAlignmentOptions.MidlineLeft;
        itemTmp.enableWordWrapping = false;
        itemTmp.raycastTarget = false;
        if (labelFont != null) itemTmp.font = labelFont;
        else if (bodyFont != null) itemTmp.font = bodyFont;

        // Wire dropdown
        dropdown.template = templateRt;
        dropdown.captionText = dropdown.captionText; // keep existing caption child if present
        // Ensure caption exists
        if (dropdown.captionText == null)
        {
            Transform labelT = dropdown.transform.Find("Label");
            if (labelT != null)
                dropdown.captionText = labelT.GetComponent<TMP_Text>();
        }
        dropdown.itemText = itemTmp;
        dropdown.itemImage = null;

        // Arrow (optional)
        Transform arrowT = dropdown.transform.Find("Arrow");
        if (arrowT != null)
        {
            Image arrowImg = arrowT.GetComponent<Image>();
            if (arrowImg != null)
            {
                arrowImg.color = Tokens.BronzeBright;
                arrowImg.raycastTarget = false;
            }
        }

        template.SetActive(false);
        EditorUtility.SetDirty(dropdown);
    }

    private static void RestyleRoomListEntryPrefab()
    {
        const string prefabPath = "Assets/Prefabs/UI/RoomListEntry.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        string path = prefabPath;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var img = root.GetComponent<Image>();
            if (img != null)
            {
                var sprites = UiSprites.Instance;
                if (sprites != null && sprites.PanelUmber != null)
                    UiSprites.ApplySliced(img, sprites.PanelUmber);
                else
                    img.color = Tokens.Umber;
            }

            var btn = root.GetComponent<Button>();
            if (btn != null)
            {
                // Text-first row: no bronze sprite swap (panel stays umber); label ignites.
                btn.transition = Selectable.Transition.None;
                var ignite = root.GetComponent<IgniteButton>();
                if (ignite == null) ignite = root.AddComponent<IgniteButton>();
                var tmp = root.GetComponentInChildren<TMP_Text>(true);
                SerializedObject iso = new SerializedObject(ignite);
                iso.FindProperty("background").objectReferenceValue = null;
                iso.FindProperty("idleSprite").objectReferenceValue = null;
                iso.FindProperty("ignitedSprite").objectReferenceValue = null;
                iso.FindProperty("pressedSprite").objectReferenceValue = null;
                iso.FindProperty("label").objectReferenceValue = tmp;
                iso.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.Bone;
                iso.ApplyModifiedProperties();
            }

            var le = root.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minHeight = Tokens.MinTouchTarget;
                le.preferredHeight = Tokens.MinTouchTarget;
            }
            var rt = root.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, Tokens.MinTouchTarget);

            var label = root.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = Tokens.TextBody;
                label.color = Tokens.BoneBright;
                var refs = FontRefs.Instance;
                if (refs != null && refs.Body != null) label.font = refs.Body;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ─── Hierarchy helpers ────────────────────────────────────

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
        StretchFull(safe.AddComponent<RectTransform>());
        safe.AddComponent<SafeAreaHandler>();
        return safe.transform;
    }

    private static void ReparentIfNeeded(Transform canvas, Transform safe, string panelName)
    {
        // If panel is direct child of canvas (not under safe), move it
        Transform panel = canvas.Find(panelName);
        if (panel == null) return;
        if (panel.parent == safe) return;
        panel.SetParent(safe, false);
    }

    private static Image GetOrCreateFullscreenImage(Transform parent, string name, Color color, int sibling)
    {
        Transform t = FindDeep(parent, name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
            StretchFull(go.AddComponent<RectTransform>());
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            go.transform.SetSiblingIndex(Mathf.Min(sibling, parent.childCount - 1));
            return img;
        }
        Image existing = t.GetComponent<Image>();
        if (existing == null) existing = t.gameObject.AddComponent<Image>();
        existing.raycastTarget = false;
        return existing;
    }

    private static Image FindNamedImage(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        if (all == null) return null;
        foreach (Object o in all)
            if (o is Sprite sp) return sp;
        return null;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindDeep(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
#endif
