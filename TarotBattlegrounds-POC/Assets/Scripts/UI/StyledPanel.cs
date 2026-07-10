using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// Reusable styled panel component that adds semi-transparent, rounded-corner
/// backgrounds with subtle borders to any UI panel. Theme-aware via IThemeable.
/// Uses procedural SDF-based rounded-rect textures with 9-slice scaling.
/// </summary>
public class StyledPanel : MonoBehaviour, IThemeable
{
    [Header("Panel Style")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image panelBorder;
    [SerializeField] private Image headerBar;

    [Header("Settings")]
    [SerializeField] private float backgroundAlpha = 0.7f;
    [SerializeField] private float borderAlpha = 0.3f;
    [SerializeField] private float cornerRadius = 12f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private bool showHeader = true;
    [SerializeField] private float headerHeight = 32f;

    [Header("Colors (overridden by theme)")]
    [SerializeField] private Color backgroundColor = Tokens.WithAlpha(Tokens.Ash, 0.7f);
    [SerializeField] private Color borderColor = Tokens.WithAlpha(Tokens.StoneEdge, 0.3f);
    [SerializeField] private Color headerColor = Tokens.WithAlpha(Tokens.Umber, 0.8f);

    // Texture resolution for procedural generation
    private const int TextureSize = 256;

    // Cache generated sprites to avoid redundant texture creation
    private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        GenerateAndApplyTextures();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;
        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply the given theme configuration to this panel.
    /// Derives panel colors from theme palette.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Background = theme.secondaryColor with backgroundAlpha
        backgroundColor = new Color(
            theme.secondaryColor.r,
            theme.secondaryColor.g,
            theme.secondaryColor.b,
            backgroundAlpha
        );

        // Border = theme.primaryColor with borderAlpha
        borderColor = new Color(
            theme.primaryColor.r,
            theme.primaryColor.g,
            theme.primaryColor.b,
            borderAlpha
        );

        // Header = slightly lighter than background
        headerColor = new Color(
            Mathf.Min(1f, theme.secondaryColor.r + 0.04f),
            Mathf.Min(1f, theme.secondaryColor.g + 0.03f),
            Mathf.Min(1f, theme.secondaryColor.b + 0.06f),
            Mathf.Min(1f, backgroundAlpha + 0.1f)
        );

        GenerateAndApplyTextures();
    }

    /// <summary>
    /// Show or hide the header bar.
    /// </summary>
    public void SetHeaderVisible(bool visible)
    {
        showHeader = visible;
        if (headerBar != null)
            headerBar.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Generate rounded-rect textures and apply them to the panel images.
    /// Uses cached sprites when possible.
    /// </summary>
    private void GenerateAndApplyTextures()
    {
        // Apply background sprite
        if (panelBackground != null)
        {
            Sprite bgSprite = CreateRoundedRectSprite(
                TextureSize, TextureSize, cornerRadius,
                backgroundColor, Color.clear, 0f, "bg"
            );
            panelBackground.sprite = bgSprite;
            panelBackground.type = Image.Type.Sliced;
            panelBackground.color = Color.white; // Color is baked into the texture
            panelBackground.raycastTarget = false;
        }

        // Apply border sprite (outline only)
        if (panelBorder != null)
        {
            Sprite borderSprite = CreateRoundedRectSprite(
                TextureSize, TextureSize, cornerRadius,
                Color.clear, borderColor, borderWidth, "border"
            );
            panelBorder.sprite = borderSprite;
            panelBorder.type = Image.Type.Sliced;
            panelBorder.color = Color.white;
            panelBorder.raycastTarget = false;
        }

        // Apply header bar
        if (headerBar != null)
        {
            headerBar.color = headerColor;
            headerBar.raycastTarget = false;
            headerBar.gameObject.SetActive(showHeader);
        }
    }

    /// <summary>
    /// Create a rounded-rect sprite with SDF-based anti-aliased edges.
    /// Sprites are cached by a key derived from parameters.
    /// Uses 9-slice borders so the sprite scales to any panel size without distortion.
    /// </summary>
    /// <param name="width">Texture width in pixels</param>
    /// <param name="height">Texture height in pixels</param>
    /// <param name="radius">Corner radius in pixels</param>
    /// <param name="fill">Fill color (set alpha to 0 for no fill)</param>
    /// <param name="border">Border color (set alpha to 0 for no border)</param>
    /// <param name="bWidth">Border width in pixels</param>
    /// <param name="tag">Optional tag for cache key differentiation</param>
    /// <returns>A Sprite with 9-slice borders configured for the rounded corners</returns>
    public static Sprite CreateRoundedRectSprite(
        int width, int height, float radius,
        Color fill, Color border, float bWidth, string tag = "")
    {
        // Build cache key from parameters
        string cacheKey = $"{width}x{height}_r{radius:F1}_f{fill}_b{border}_bw{bWidth:F1}_{tag}";

        if (_spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = GenerateRoundedRect(width, height, radius, fill, border, bWidth);

        // Calculate 9-slice border: inset by corner radius + a small margin
        int sliceBorder = Mathf.CeilToInt(radius) + 2;
        sliceBorder = Mathf.Min(sliceBorder, width / 2 - 1);
        sliceBorder = Mathf.Min(sliceBorder, height / 2 - 1);

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder)
        );

        _spriteCache[cacheKey] = sprite;
        return sprite;
    }

    /// <summary>
    /// Generate a Texture2D with a rounded rectangle using signed distance field (SDF)
    /// for smooth, anti-aliased edges.
    /// </summary>
    private static Texture2D GenerateRoundedRect(
        int width, int height, float radius,
        Color fill, Color border, float borderWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Vector2 halfSize = new Vector2(width * 0.5f, height * 0.5f);

        // Scale radius to texture space, clamped to half of the smaller dimension
        float texRadius = Mathf.Min(radius * (width / 256f), Mathf.Min(halfSize.x, halfSize.y) - 1f);
        float texBorderWidth = borderWidth * (width / 256f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Center the coordinate system
                Vector2 point = new Vector2(x - halfSize.x + 0.5f, y - halfSize.y + 0.5f);

                // Calculate SDF distance to rounded rect edge
                float dist = RoundedRectSDF(point, halfSize, texRadius);

                // Anti-aliasing: smoothstep over 1 pixel
                float outerAlpha = 1f - Smoothstep(-0.5f, 0.5f, dist);

                Color pixelColor = Color.clear;

                if (texBorderWidth > 0f && border.a > 0f)
                {
                    // Border zone: between (edge - borderWidth) and edge
                    float innerDist = dist + texBorderWidth;
                    float innerAlpha = 1f - Smoothstep(-0.5f, 0.5f, innerDist);

                    // Fill region (inside the border)
                    if (fill.a > 0f)
                    {
                        // Inner fill
                        float fillAlpha = innerAlpha;
                        pixelColor = fill;
                        pixelColor.a *= fillAlpha;
                    }

                    // Border region (between inner and outer edge)
                    float borderAlphaFactor = outerAlpha - innerAlpha;
                    if (borderAlphaFactor > 0f)
                    {
                        Color borderPixel = border;
                        borderPixel.a *= borderAlphaFactor;
                        // Alpha blend border on top of fill
                        pixelColor = AlphaBlend(pixelColor, borderPixel);
                    }
                }
                else
                {
                    // No border: just fill
                    if (fill.a > 0f)
                    {
                        pixelColor = fill;
                        pixelColor.a *= outerAlpha;
                    }
                }

                pixels[y * width + x] = pixelColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Signed distance field for a rounded rectangle.
    /// Returns negative values inside, positive outside, zero on the edge.
    /// </summary>
    private static float RoundedRectSDF(Vector2 point, Vector2 halfSize, float radius)
    {
        Vector2 d = new Vector2(
            Mathf.Abs(point.x) - halfSize.x + radius,
            Mathf.Abs(point.y) - halfSize.y + radius
        );
        float outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude;
        float inside = Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        return outside + inside - radius;
    }

    /// <summary>
    /// Attempt a smoothstep-like interpolation for anti-aliasing.
    /// </summary>
    private static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Alpha-blend source over destination (premultiplied-style composite).
    /// </summary>
    private static Color AlphaBlend(Color dst, Color src)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA < 0.001f) return Color.clear;

        Color result;
        result.r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        result.g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        result.b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
        result.a = outA;
        return result;
    }
}
