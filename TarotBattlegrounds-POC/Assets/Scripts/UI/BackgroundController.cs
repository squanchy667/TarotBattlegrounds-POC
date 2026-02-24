using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Multi-layered procedural background system that creates a mystical tavern atmosphere.
/// Layers: base color, radial gradient overlay, vignette, and ambient dust particles.
/// Supports custom background sprite override and integrates with the theme system.
/// </summary>
public class BackgroundController : MonoBehaviour, IThemeable
{
    [Header("Layers")]
    [SerializeField] private Image backgroundBase;        // Solid base
    [SerializeField] private Image gradientOverlay;       // Radial gradient
    [SerializeField] private Image vignetteOverlay;       // Edge darkening
    [SerializeField] private ParticleSystem ambientDust;  // Floating particles

    [Header("Settings")]
    [SerializeField] private bool useProceduralBackground = true;
    [SerializeField] private Sprite customBackgroundSprite;

    [Header("Gradient")]
    [SerializeField] private Color gradientCenter = new Color(0.08f, 0.05f, 0.14f);
    [SerializeField] private Color gradientEdge = new Color(0.02f, 0.01f, 0.05f);

    [Header("Particles")]
    [SerializeField] private Color dustColor = new Color(1f, 0.82f, 0.12f, 0.15f);
    [SerializeField] private int dustCount = 40;
    [SerializeField] private float dustSpeed = 8f;
    [SerializeField] private float dustSize = 3f;

    private Texture2D gradientTexture;
    private Texture2D vignetteTexture;

    private void Start()
    {
        if (useProceduralBackground && customBackgroundSprite == null)
        {
            CreateGradientTexture();
            CreateVignetteTexture();
        }
        else if (customBackgroundSprite != null)
        {
            ApplyCustomBackground();
        }

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
    }

    /// <summary>
    /// Apply theme to background layers. Derives gradient colors from gameBackgroundColor.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Derive gradient center from theme background color (slightly brighter)
        Color baseColor = theme.gameBackgroundColor;
        gradientCenter = new Color(
            Mathf.Min(baseColor.r * 1.6f, 0.2f),
            Mathf.Min(baseColor.g * 1.6f, 0.15f),
            Mathf.Min(baseColor.b * 1.4f, 0.25f)
        );
        // Edge is darker version
        gradientEdge = new Color(
            baseColor.r * 0.4f,
            baseColor.g * 0.3f,
            baseColor.b * 0.5f
        );

        // Apply base color
        if (backgroundBase != null)
        {
            backgroundBase.color = baseColor;
        }

        // Regenerate procedural textures if using procedural mode
        if (useProceduralBackground && customBackgroundSprite == null)
        {
            CreateGradientTexture();
            CreateVignetteTexture();
        }
        else if (theme.gameBackground != null)
        {
            // Theme provides a background sprite -- use it as custom override
            if (gradientOverlay != null)
            {
                gradientOverlay.sprite = theme.gameBackground;
                gradientOverlay.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Create a 512x512 radial gradient texture from gradientCenter to gradientEdge.
    /// </summary>
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
    }

    /// <summary>
    /// Create a procedural vignette texture: transparent center fading to black edges (alpha 0-0.6).
    /// </summary>
    private void CreateVignetteTexture()
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
                // Ease-in for smoother vignette falloff
                float alpha = t * t * 0.6f;
                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
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
    }

    /// <summary>
    /// Apply custom background sprite, hiding the procedural gradient.
    /// </summary>
    private void ApplyCustomBackground()
    {
        if (gradientOverlay != null && customBackgroundSprite != null)
        {
            gradientOverlay.sprite = customBackgroundSprite;
            gradientOverlay.color = Color.white;
        }

        // Hide vignette when using custom sprite (artist controls their own vignette)
        if (vignetteOverlay != null)
        {
            vignetteOverlay.enabled = true;
        }
    }

    /// <summary>
    /// Configure the particle system for ambient mystical dust.
    /// Slow upward drift with slight horizontal sway, golden specks at very low alpha.
    /// </summary>
    private void SetupParticles()
    {
        if (ambientDust == null) return;

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

        // Emission
        var emission = ambientDust.emission;
        emission.enabled = true;
        float avgLifetime = 10f; // average of 8 and 12
        emission.rateOverTime = (float)dustCount / avgLifetime;

        // Shape: Box covering screen area
        var shape = ambientDust.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 12f, 1f);

        // Noise for gentle sway
        var noise = ambientDust.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // Color over lifetime: fade in and out
        var colorOverLifetime = ambientDust.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(dustColor.r, dustColor.g, dustColor.b), 0f),
                new GradientColorKey(new Color(dustColor.r, dustColor.g, dustColor.b), 1f)
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

        // Renderer settings
        var renderer = ambientDust.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            // Use default particle material if none assigned
            if (renderer.material == null || renderer.material.name == "Default-Material")
            {
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                if (renderer.material != null)
                {
                    renderer.material.SetFloat("_Mode", 1f); // Additive
                    renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                }
            }
            renderer.sortingOrder = -1; // Behind UI
        }

        // Restart if already playing
        if (ambientDust.isPlaying)
        {
            ambientDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ambientDust.Play();
    }

    /// <summary>
    /// Set a custom background sprite at runtime.
    /// Pass null to revert to procedural background.
    /// </summary>
    public void SetCustomBackground(Sprite sprite)
    {
        customBackgroundSprite = sprite;

        if (sprite != null)
        {
            useProceduralBackground = false;
            ApplyCustomBackground();
        }
        else
        {
            useProceduralBackground = true;
            CreateGradientTexture();
            CreateVignetteTexture();
        }
    }

    private void OnDestroy()
    {
        // Clean up procedural textures
        if (gradientTexture != null)
        {
            Destroy(gradientTexture);
        }
        if (vignetteTexture != null)
        {
            Destroy(vignetteTexture);
        }
    }
}
