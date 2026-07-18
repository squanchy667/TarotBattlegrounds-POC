using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TarotBattlegrounds.UI;

/// <summary>
/// Background layers for menu/lobby scenes.
/// Modes:
///  - Procedural: Ash base + radial gradient + vignette + optional dust
///  - Art still: full-bleed sprite + vignette (≤2 full-screen overdraws)
///  - Art video: VideoPlayer → RawImage, still as poster/fallback;
///    default menu mode is ping-pong (forward then reverse) so the loop has no jump.
///    Reduce-motion and load failure stay on still (T750 Phase 3 — MainMenu only).
/// </summary>
public class BackgroundController : MonoBehaviour, IThemeable
{
    [Header("Layers")]
    [SerializeField] private Image backgroundBase;        // Solid Ash or still poster
    [SerializeField] private Image gradientOverlay;       // Procedural gradient OR unused in art mode
    [SerializeField] private Image vignetteOverlay;       // Edge darkening
    [SerializeField] private ParticleSystem ambientDust;  // Floating particles
    [SerializeField] private RawImage videoDisplay;       // Optional video layer (menu trial)

    [Header("Settings")]
    [SerializeField] private bool useProceduralBackground = true;
    [SerializeField] private Sprite customBackgroundSprite;

    [Header("Video (optional — MainMenu trial)")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip backgroundVideo;
    [SerializeField] private bool loopBackgroundVideo = true;
    [Header("Gradient")]
    [SerializeField] private Color gradientCenter = Tokens.CharredWood;
    [SerializeField] private Color gradientEdge = Tokens.Ash;

    [Header("Particles")]
    [SerializeField] private Color dustColor = Tokens.WithAlpha(Tokens.BronzeBright, 0.15f);
    [SerializeField] private int dustCount = 40;
    [SerializeField] private float dustSpeed = 8f;
    [SerializeField] private float dustSize = 3f;

    private Texture2D gradientTexture;
    private Texture2D vignetteTexture;
    private RenderTexture videoRT;
    private bool artMode;

    private void Start()
    {
        ApplyCurrentMode();
        SetupParticles();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;

        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
        StopVideo();
    }

    /// <summary>
    /// Wire still + optional video. Still is always the reduce-motion / failure
    /// poster. Does not block interactivity on prepare.
    /// Prefer a pre-baked forward+reverse clip (menu_video_pingpong) — this
    /// VideoPlayer backend clamps negative playbackSpeed to 0, so software
    /// reverse is not viable.
    /// </summary>
    public void ConfigureArtBackground(Sprite still, VideoClip clip, bool loop)
    {
        customBackgroundSprite = still;
        backgroundVideo = clip;
        loopBackgroundVideo = loop;
        // Baked forward+reverse clips just hard-loop seamlessly.
        bool bakedPingPong = clip != null && clip.name != null
            && clip.name.IndexOf("pingpong", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (bakedPingPong)
            loopBackgroundVideo = true;
        useProceduralBackground = still == null && clip == null;
        artMode = !useProceduralBackground;
        ApplyCurrentMode();
    }

    /// <summary>
    /// WO-09: swap full-bleed still only (no video). Used for recruit/combat phase env art.
    /// Stills only — no extra full-screen chrome layers.
    /// </summary>
    public void SetStillSprite(Sprite still)
    {
        if (still == null) return;
        EnsureBackgroundBaseLayer();
        ConfigureArtBackground(still, null, false);
        // Force first-sibling under canvas so shop/board UI draws on top of art
        if (backgroundBase != null)
        {
            var canvas = backgroundBase.canvas != null ? backgroundBase.canvas.transform : transform;
            BackgroundController.EnsureBgRoot(canvas);
            backgroundBase.transform.SetParent(
                canvas.Find("BG_Root") != null ? canvas.Find("BG_Root") : canvas, false);
            if (backgroundBase.transform.parent != null && backgroundBase.transform.parent.name == "BG_Root")
                backgroundBase.transform.parent.SetAsFirstSibling();
            backgroundBase.enabled = true;
            backgroundBase.gameObject.SetActive(true);
            backgroundBase.raycastTarget = false;
        }
        ApplyStillPoster();
        Debug.Log($"[BackgroundController] SetStillSprite → {still.name} (base={(backgroundBase != null ? backgroundBase.name : "null")})");
    }

    /// <summary>
    /// Ensure a full-screen Image exists for still posters (fixes zero-size BG_Base from bad setup).
    /// </summary>
    public void EnsureBackgroundBaseLayer()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        RectTransform bgRoot = EnsureBgRoot(canvas.transform);
        if (backgroundBase == null)
        {
            // Prefer existing BG_Base under canvas tree
            foreach (var img in canvas.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img.gameObject.name == "BG_Base")
                {
                    backgroundBase = img;
                    break;
                }
            }
        }

        if (backgroundBase == null)
        {
            GameObject go = new GameObject("BG_Base");
            go.transform.SetParent(bgRoot != null ? (Transform)bgRoot : canvas.transform, false);
            go.layer = canvas.gameObject.layer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            backgroundBase = img;
            Debug.Log("[BackgroundController] Created BG_Base full-screen layer");
        }
        else
        {
            // Repair broken zero-size anchors from earlier setup
            RectTransform rt = backgroundBase.rectTransform;
            if (bgRoot != null && backgroundBase.transform.parent != bgRoot)
                backgroundBase.transform.SetParent(bgRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            backgroundBase.raycastTarget = false;
            backgroundBase.gameObject.SetActive(true);
            backgroundBase.enabled = true;
        }
    }

    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        Color baseColor = theme.gameBackgroundColor;
        gradientCenter = new Color(
            Mathf.Min(baseColor.r * 1.6f, 0.2f),
            Mathf.Min(baseColor.g * 1.6f, 0.15f),
            Mathf.Min(baseColor.b * 1.4f, 0.25f)
        );
        gradientEdge = new Color(
            baseColor.r * 0.4f,
            baseColor.g * 0.3f,
            baseColor.b * 0.5f
        );

        if (artMode || customBackgroundSprite != null || backgroundVideo != null)
        {
            // Art mode owns colors; theme does not override full-bleed env art.
            return;
        }

        if (backgroundBase != null)
        {
            backgroundBase.color = baseColor;
        }

        if (useProceduralBackground)
        {
            CreateGradientTexture();
            CreateVignetteTexture();
        }
        else if (theme.gameBackground != null && gradientOverlay != null)
        {
            gradientOverlay.sprite = theme.gameBackground;
            gradientOverlay.color = Color.white;
        }
    }

    private void ApplyCurrentMode()
    {
        artMode = customBackgroundSprite != null || backgroundVideo != null;

        if (!artMode && useProceduralBackground)
        {
            HideVideoLayer();
            if (backgroundBase != null)
            {
                backgroundBase.enabled = true;
                backgroundBase.sprite = null;
                backgroundBase.color = Tokens.Ash;
            }
            if (gradientOverlay != null)
            {
                gradientOverlay.enabled = true;
                gradientOverlay.gameObject.SetActive(true);
            }
            CreateGradientTexture();
            CreateVignetteTexture(Tokens.MenuVignette);
            return;
        }

        // Art mode: still poster + optional video + vignette. No procedural gradient.
        useProceduralBackground = false;
        if (gradientOverlay != null)
        {
            gradientOverlay.enabled = false;
            gradientOverlay.gameObject.SetActive(false);
        }

        ApplyStillPoster();
        CreateVignetteTexture(Tokens.MenuVignette);

        bool wantVideo = backgroundVideo != null && !UiMotion.ReduceMotion;
        if (wantVideo)
            TryStartVideo();
        else
            HideVideoLayer();
    }

    private void ApplyStillPoster()
    {
        if (backgroundBase == null) return;

        backgroundBase.enabled = true;
        if (customBackgroundSprite != null)
        {
            backgroundBase.sprite = customBackgroundSprite;
            backgroundBase.type = Image.Type.Simple;
            // Stretch-to-fill distorts landscape env art on portrait; cover crops edges instead.
            backgroundBase.preserveAspect = false;
            backgroundBase.color = Color.white;
            float aspect = 16f / 9f;
            if (customBackgroundSprite.rect.height > 1f)
                aspect = customBackgroundSprite.rect.width / customBackgroundSprite.rect.height;
            ApplyCoverFit(backgroundBase.rectTransform, aspect);
        }
        else
        {
            backgroundBase.sprite = null;
            backgroundBase.color = Tokens.Ash;
            ClearCoverFit(backgroundBase.rectTransform);
        }
    }

    private void TryStartVideo()
    {
        if (videoPlayer == null || videoDisplay == null || backgroundVideo == null)
        {
            HideVideoLayer();
            return;
        }

        if (UiMotion.ReduceMotion)
        {
            HideVideoLayer();
            return;
        }

        EnsureVideoRenderTexture();

        videoPlayer.playOnAwake = false;
        // Seamless when clip is baked forward+reverse; otherwise plain loop.
        videoPlayer.isLooping = loopBackgroundVideo;
        videoPlayer.playbackSpeed = 1f;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRT;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.clip = backgroundVideo;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (UiMotion.ReduceMotion)
        {
            HideVideoLayer();
            return;
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = videoRT;
            videoDisplay.color = Color.white;
            videoDisplay.enabled = true;
            videoDisplay.gameObject.SetActive(true);
            videoDisplay.raycastTarget = false;
            float aspect = 16f / 9f;
            if (source != null && source.height > 0)
                aspect = (float)source.width / source.height;
            else if (backgroundVideo != null && backgroundVideo.height > 0)
                aspect = (float)backgroundVideo.width / backgroundVideo.height;
            ApplyCoverFit(videoDisplay.rectTransform, aspect);
        }

        // Still remains under video for first-frame safety; hide once playing to
        // keep full-screen overdraw at video + vignette (≤2).
        if (backgroundBase != null)
            backgroundBase.enabled = false;

        source.playbackSpeed = 1f;
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[BackgroundController] Video failed — staying on still poster. " + message);
        HideVideoLayer();
        ApplyStillPoster();
    }

    private void EnsureVideoRenderTexture()
    {
        int w = 1280;
        int h = 720;
        if (backgroundVideo != null)
        {
            w = Mathf.Max(16, (int)backgroundVideo.width);
            h = Mathf.Max(16, (int)backgroundVideo.height);
        }

        if (videoRT != null && (videoRT.width != w || videoRT.height != h))
        {
            videoRT.Release();
            Destroy(videoRT);
            videoRT = null;
        }

        if (videoRT == null)
        {
            videoRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            videoRT.name = "BG_VideoRT";
            videoRT.Create();
        }
    }

    private void HideVideoLayer()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            if (videoPlayer.isPlaying) videoPlayer.Stop();
            videoPlayer.playbackSpeed = 1f;
        }
        if (videoDisplay != null)
        {
            videoDisplay.enabled = false;
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }
        if (backgroundBase != null)
            backgroundBase.enabled = true;
    }

    /// <summary>
    /// Cover-fit media (center-crop, no stretch-distort) via AspectRatioFitter.EnvelopeParent.
    /// Clipping lives on <c>BG_Root</c> only — never on the Canvas. A root RectMask2D
    /// incorrectly clips TMP titles (SafeArea) after scene reloads / safe-area changes.
    /// </summary>
    private static void ApplyCoverFit(RectTransform rt, float mediaAspect)
    {
        if (rt == null || mediaAspect <= 0.01f) return;

        EnsureMediaUnderBgRoot(rt);

        var fitter = rt.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = rt.gameObject.AddComponent<AspectRatioFitter>();
        fitter.enabled = true;
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = mediaAspect;
        LayoutRebuilder.MarkLayoutForRebuild(rt);
    }

    /// <summary>
    /// Reparents a BG media layer under Canvas/BG_Root (masked) and strips any
    /// RectMask2D that was wrongly added on the Canvas itself.
    /// </summary>
    public static RectTransform EnsureBgRoot(Transform canvasTransform)
    {
        if (canvasTransform == null) return null;

        // Strip Canvas-level mask — it clips SafeArea TMP after Lobby↔Menu travel
        var canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas != null)
        {
            var badMask = canvas.GetComponent<RectMask2D>();
            if (badMask != null)
            {
                if (Application.isPlaying) Object.Destroy(badMask);
                else Object.DestroyImmediate(badMask);
            }
        }

        Transform rootT = canvasTransform.Find("BG_Root");
        GameObject rootGo;
        if (rootT == null)
        {
            rootGo = new GameObject("BG_Root");
            rootGo.transform.SetParent(canvasTransform, false);
            RectTransform rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootGo.AddComponent<RectMask2D>();
            // Bottom of canvas, under SafeArea chrome
            rootGo.transform.SetAsFirstSibling();
        }
        else
        {
            rootGo = rootT.gameObject;
            if (rootGo.GetComponent<RectMask2D>() == null)
                rootGo.AddComponent<RectMask2D>();
            rootGo.transform.SetAsFirstSibling();
        }

        return rootGo.GetComponent<RectTransform>();
    }

    private static void EnsureMediaUnderBgRoot(RectTransform mediaRt)
    {
        if (mediaRt == null) return;

        Canvas canvas = mediaRt.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform bgRoot = EnsureBgRoot(canvas.transform);
        if (bgRoot == null) return;

        if (mediaRt.parent != bgRoot)
        {
            mediaRt.SetParent(bgRoot, false);
            // Leave anchors for AspectRatioFitter to drive; seed full stretch first
            mediaRt.anchorMin = Vector2.zero;
            mediaRt.anchorMax = Vector2.one;
            mediaRt.offsetMin = Vector2.zero;
            mediaRt.offsetMax = Vector2.zero;
            mediaRt.pivot = new Vector2(0.5f, 0.5f);
            mediaRt.anchoredPosition = Vector2.zero;
            mediaRt.sizeDelta = Vector2.zero;
            mediaRt.localScale = Vector3.one;
        }
    }

    private static void ClearCoverFit(RectTransform rt)
    {
        if (rt == null) return;
        var fitter = rt.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
            if (Application.isPlaying) Object.Destroy(fitter);
            else Object.DestroyImmediate(fitter);
        }
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void StopVideo()
    {
        if (videoPlayer == null) return;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;
        if (videoPlayer.isPlaying) videoPlayer.Stop();
        videoPlayer.playbackSpeed = 1f;
    }

    /// <summary>Public re-evaluate (e.g. after reduce-motion toggle).</summary>
    public void RefreshMotionPreference()
    {
        ApplyCurrentMode();
    }

    private void CreateGradientTexture()
    {
        if (gradientOverlay == null) return;

        int size = 512;
        if (gradientTexture == null)
        {
            gradientTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            gradientTexture.wrapMode = TextureWrapMode.Clamp;
            gradientTexture.filterMode = FilterMode.Bilinear;
        }

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                pixels[y * size + x] = Color.Lerp(gradientCenter, gradientEdge, t);
            }
        }

        gradientTexture.SetPixels(pixels);
        gradientTexture.Apply();

        Sprite gradientSprite = Sprite.Create(
            gradientTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        gradientOverlay.sprite = gradientSprite;
        gradientOverlay.color = Color.white;
        gradientOverlay.enabled = true;
    }

    private void CreateVignetteTexture(float edgeAlpha = 0.6f)
    {
        if (vignetteOverlay == null) return;

        int size = 512;
        if (vignetteTexture == null)
        {
            vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            vignetteTexture.wrapMode = TextureWrapMode.Clamp;
            vignetteTexture.filterMode = FilterMode.Bilinear;
        }

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                float alpha = t * t * edgeAlpha;
                pixels[y * size + x] = Tokens.WithAlpha(Tokens.Ash, alpha);
            }
        }

        vignetteTexture.SetPixels(pixels);
        vignetteTexture.Apply();

        Sprite vignetteSprite = Sprite.Create(
            vignetteTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        vignetteOverlay.sprite = vignetteSprite;
        vignetteOverlay.color = Color.white;
        vignetteOverlay.enabled = true;
        vignetteOverlay.raycastTarget = false;
    }

    private void SetupParticles()
    {
        if (ambientDust == null) return;

        // Art/video menus keep overdraw lean — dust optional and dim.
        if (artMode)
        {
            ambientDust.gameObject.SetActive(false);
            return;
        }

        ambientDust.gameObject.SetActive(true);

        var main = ambientDust.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
        main.startSpeed = dustSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(dustSize * 0.5f, dustSize);
        main.startColor = dustColor;
        main.gravityModifier = -0.02f;
        main.maxParticles = dustCount * 2;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ambientDust.emission;
        emission.enabled = true;
        float avgLifetime = 10f;
        emission.rateOverTime = (float)dustCount / avgLifetime;

        var shape = ambientDust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 12f, 1f);

        var noise = ambientDust.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var colorOverLifetime = ambientDust.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(dustColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(dustColor.a, 0.2f),
                new GradientAlphaKey(dustColor.a, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = ambientDust.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (renderer.material == null || renderer.material.name == "Default-Material")
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                if (renderer.material != null)
                {
                    renderer.material.SetFloat("_Mode", 1f);
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                }
            }
            renderer.sortingOrder = -1;
        }

        if (ambientDust.isPlaying)
        {
            ambientDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ambientDust.Play();
    }

    public void SetCustomBackground(Sprite sprite)
    {
        customBackgroundSprite = sprite;
        backgroundVideo = null;
        useProceduralBackground = sprite == null;
        artMode = sprite != null;
        ApplyCurrentMode();
    }

    /// <summary>Editor/runtime wiring for existing BG_Image layers + optional video GO.</summary>
    public void WireLayers(Image baseImg, Image gradient, Image vignette, ParticleSystem dust,
        RawImage videoImg, VideoPlayer player)
    {
        backgroundBase = baseImg;
        gradientOverlay = gradient;
        vignetteOverlay = vignette;
        ambientDust = dust;
        videoDisplay = videoImg;
        videoPlayer = player;
    }

    private void OnDestroy()
    {
        StopVideo();
        if (gradientTexture != null) Destroy(gradientTexture);
        if (vignetteTexture != null) Destroy(vignetteTexture);
        if (videoRT != null)
        {
            videoRT.Release();
            Destroy(videoRT);
        }
    }
}
