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

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lobbyUI);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();

        return "T750 §8 Lobby restyle complete.\n\n" +
               "  BG: lobby_bg still + lobby_video_pingpong (visible behind UI)\n" +
               "  Panels: transparent shells; only small cards use plate chrome\n" +
               "  Room interior: slots + room code + Start\n" +
               "  Buttons: IgniteButton bronze, ≥88px\n\n" +
               "Scene saved.";
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

        Image baseImg = GetOrCreateFullscreenImage(canvasTransform, "BG_Base", Tokens.Ash, 0);
        Image vignetteImg = GetOrCreateFullscreenImage(canvasTransform, "BG_Vignette", Color.white, 2);

        Transform videoT = FindDeep(canvasTransform, "BG_Video");
        GameObject videoGo;
        if (videoT == null)
        {
            videoGo = new GameObject("BG_Video");
            videoGo.transform.SetParent(canvasTransform, false);
            StretchFull(videoGo.AddComponent<RectTransform>());
            videoGo.AddComponent<CanvasRenderer>();
            videoGo.AddComponent<RawImage>().raycastTarget = false;
            var vp = videoGo.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.RenderTexture;
        }
        else videoGo = videoT.gameObject;

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

        // Order: base, video, vignette, then SafeArea on top
        baseImg.transform.SetAsFirstSibling();
        videoGo.transform.SetSiblingIndex(1);
        vignetteImg.transform.SetSiblingIndex(2);
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
        // false: keep sigil / buttons at preferred width (true stretched underline into a fat bar)
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(
            (int)Tokens.Space4, (int)Tokens.Space4,
            (int)Tokens.Space4, (int)Tokens.Space4);

        // Title — two clean lines (never wrap mid-word like BATTLEGRO|UNDS)
        GameObject titleGo = CreatePlainText(card.transform, "TitleText", "TAROT\nBATTLEGROUNDS",
            Tokens.TextH1, Tokens.BoneBright, TextAlignmentOptions.Center, displayFont: true);
        var titleTmp = titleGo.GetComponent<TMP_Text>();
        titleTmp.enableWordWrapping = false; // explicit newlines only
        titleTmp.overflowMode = TextOverflowModes.Overflow;
        titleTmp.enableAutoSizing = true;
        titleTmp.fontSizeMin = 32f;
        titleTmp.fontSizeMax = Tokens.TextH1;
        var titleLe = titleGo.GetComponent<LayoutElement>();
        titleLe.minHeight = Tokens.TextH1 * 2.3f;
        titleLe.preferredHeight = Tokens.TextH1 * 2.5f;

        // Thin ember underline (NOT a full-width orange slab — VLG must not stretch it)
        GameObject sigil = new GameObject("HeroSigil");
        sigil.transform.SetParent(card.transform, false);
        RectTransform sigilRt = sigil.AddComponent<RectTransform>();
        sigilRt.sizeDelta = new Vector2(220f, 6f);
        Image sigilImg = sigil.AddComponent<Image>();
        if (sprites != null && sprites.UnderlineEmber != null)
            UiSprites.ApplySliced(sigilImg, sprites.UnderlineEmber);
        else
            sigilImg.color = Tokens.Ember;
        sigilImg.raycastTarget = false;
        LayoutElement sigilLe = sigil.AddComponent<LayoutElement>();
        sigilLe.minHeight = 6f;
        sigilLe.preferredHeight = 6f;
        sigilLe.minWidth = 220f;
        sigilLe.preferredWidth = 220f;
        sigilLe.flexibleWidth = 0f;
        sigilLe.flexibleHeight = 0f;

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

        // Escape hatch — not stuck if Photon fails
        GameObject backGo = new GameObject("ConnectionBackButton");
        backGo.transform.SetParent(card.transform, false);
        RectTransform backRt = backGo.AddComponent<RectTransform>();
        backRt.sizeDelta = new Vector2(280f, Tokens.MinTouchTarget);
        Image backImg = backGo.AddComponent<Image>();
        Sprite bronze = sprites != null ? sprites.ButtonBronze : null;
        if (bronze != null) UiSprites.ApplySliced(backImg, bronze);
        else backImg.color = Tokens.Bronze;
        Button backBtn = backGo.AddComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        LayoutElement backLe = backGo.AddComponent<LayoutElement>();
        backLe.minHeight = Tokens.MinTouchTarget;
        backLe.preferredHeight = Tokens.MinTouchTarget;
        backLe.preferredWidth = 280f;

        GameObject backLabel = CreatePlainText(backGo.transform, "Text (TMP)", "Return to Menu",
            Tokens.TextLabel, Tokens.BoneBright, TextAlignmentOptions.Center, displayFont: true, labelFont: false);
        StretchFull(backLabel.GetComponent<RectTransform>());
        var backTmp = backLabel.GetComponent<TMP_Text>();
        backTmp.raycastTarget = false;
        backTmp.enableWordWrapping = false;
        backTmp.overflowMode = TextOverflowModes.Ellipsis;

        var ignite = backGo.AddComponent<IgniteButton>();
        SerializedObject iso = new SerializedObject(ignite);
        iso.FindProperty("background").objectReferenceValue = backImg;
        iso.FindProperty("idleSprite").objectReferenceValue = bronze;
        iso.FindProperty("ignitedSprite").objectReferenceValue = sprites != null ? sprites.ButtonEmber : null;
        iso.FindProperty("pressedSprite").objectReferenceValue = sprites != null ? sprites.ButtonBronzePressed : null;
        iso.FindProperty("label").objectReferenceValue = backTmp;
        iso.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.BoneBright;
        iso.ApplyModifiedProperties();

        // Also assign as backToMenuButton so LobbyUI.Start always wires onClick
        // (ConnectionBackButton is the same control when on the gate screen).
        if (so.FindProperty("backToMenuButton") != null
            && so.FindProperty("backToMenuButton").objectReferenceValue == null)
        {
            so.FindProperty("backToMenuButton").objectReferenceValue = backBtn;
        }

        // Layout elements for name field + back so they still expand to card width
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 400f;
        backLe.flexibleWidth = 1f;
        backLe.minWidth = 280f;
        backLe.preferredWidth = 400f;
        backRt.sizeDelta = new Vector2(400f, Tokens.MinTouchTarget);

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
        StyleBodyText(panel, "RoomsHeader", "Available Rooms", Tokens.TextH3, Tokens.BoneBright);
        StyleBodyText(panel, "NoRoomsText", "No rooms available. Create one!", Tokens.TextBody, Tokens.BoneDim);

        StyleInputField(so.FindProperty("roomNameInput").objectReferenceValue as TMP_InputField,
            Tokens.MinTouchTarget, preferredWidth: 280f);
        StyleDropdown(so.FindProperty("maxPlayersDropdown").objectReferenceValue as TMP_Dropdown,
            Tokens.MinTouchTarget, preferredWidth: 180f);

        // Wide enough for Cinzel labels (160px was clipping "Join Random" / "Create Room")
        StyleAsIgniteButton(so.FindProperty("createRoomButton").objectReferenceValue as Button,
            "Create Room", Tokens.MinTouchTarget, primary: true, minWidth: 260f);
        StyleAsIgniteButton(so.FindProperty("joinRandomButton").objectReferenceValue as Button,
            "Join Random", Tokens.MinTouchTarget, primary: true, minWidth: 260f);
        StyleAsIgniteButton(so.FindProperty("backToMenuButton").objectReferenceValue as Button,
            "Back to Menu", Tokens.MinTouchTarget, primary: false, minWidth: 240f);

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

    private static void StyleAsIgniteButton(Button btn, string label, float height,
        bool primary, float minWidth = 0f)
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

        btn.transition = Selectable.Transition.None;

        // Multi-word Cinzel needs real width — 160px clips "Join Random" / "Create Room"
        string finalLabel = string.IsNullOrEmpty(label) ? (btn.GetComponentInChildren<TMP_Text>(true)?.text ?? "") : label;
        float fontSize = Tokens.TextLabel; // 24 — fits chrome; H3 overflowed at 160px
        // ~0.62em average Cinzel advance + horizontal padding for 9-slice notches
        float estimated = finalLabel.Length * fontSize * 0.62f + Tokens.Space5 * 2f;
        float w = Mathf.Max(minWidth > 0f ? minWidth : 200f, estimated, 200f);
        w = Mathf.Min(w, 360f); // cap so rows don't explode

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
            rt.sizeDelta = new Vector2(w, height);

        LayoutElement le = btn.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
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
            // Cinzel for primary actions (matches MainMenu CTA face), Label for secondary
            if (primary && refs != null && refs.Display != null) tmp.font = refs.Display;
            else if (refs != null && refs.Label != null) tmp.font = refs.Label;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
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

    private static void StyleDropdown(TMP_Dropdown dropdown, float height, float preferredWidth = 0f)
    {
        if (dropdown == null) return;
        var refs = FontRefs.Instance;
        TMP_FontAsset body = refs != null ? refs.Body : null;

        float w = preferredWidth > 0f ? preferredWidth : 180f;
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
        if (bg != null)
        {
            var sprites = UiSprites.Instance;
            if (sprites != null && sprites.PanelCharred != null)
                UiSprites.ApplySliced(bg, sprites.PanelCharred);
            else
                bg.color = Tokens.CharredWood;
        }

        if (dropdown.captionText != null)
        {
            dropdown.captionText.fontSize = Tokens.TextLabel;
            dropdown.captionText.color = Tokens.BoneBright;
            if (body != null) dropdown.captionText.font = body;
        }
        if (dropdown.itemText != null)
        {
            dropdown.itemText.fontSize = Tokens.TextLabel;
            dropdown.itemText.color = Tokens.BoneBright;
            if (body != null) dropdown.itemText.font = body;
        }
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
