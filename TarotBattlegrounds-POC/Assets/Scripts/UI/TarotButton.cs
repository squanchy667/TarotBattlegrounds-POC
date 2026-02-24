using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Button variant determines the color scheme applied to the button.
/// </summary>
public enum ButtonVariant { Primary, Secondary, Danger, Success }

/// <summary>
/// Styled button component that provides gradient backgrounds, glow effects,
/// hover/press animations, and theme-aware color variants.
/// Wraps the existing Unity Button component — does not replace it.
/// </summary>
public class TarotButton : MonoBehaviour, IThemeable,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image buttonBackground;
    [SerializeField] private Image buttonBorder;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Style")]
    [SerializeField] private ButtonVariant variant = ButtonVariant.Primary;
    [SerializeField] private float cornerRadius = 8f;
    [SerializeField] private float borderWidth = 1.5f;

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float animationSpeed = 12f;

    [Header("Colors (overridden by theme)")]
    [SerializeField] private Color primaryGradientTop = new Color(1f, 0.82f, 0.15f);
    [SerializeField] private Color primaryGradientBottom = new Color(0.85f, 0.65f, 0.05f);
    [SerializeField] private Color hoverBorderGlow = new Color(1f, 0.9f, 0.4f, 0.6f);

    private Button unityButton;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private bool isHovered = false;
    private bool isPressed = false;
    private bool isInteractable = true;

    // Cached theme colors
    private Color currentGradientTop;
    private Color currentGradientBottom;
    private Color currentBorderColor;
    private Color currentTextColor;
    private Color currentHoverGlow;

    // Static gradient texture cache — one per variant to avoid redundant allocations
    private static Dictionary<int, Texture2D> gradientTextureCache = new Dictionary<int, Texture2D>();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        unityButton = GetComponent<Button>();
        originalScale = transform.localScale;

        // Initialize default colors from variant
        UpdateVariantColors(null);

        // Ensure background has raycast target enabled
        if (buttonBackground != null)
        {
            buttonBackground.raycastTarget = true;
        }
        if (buttonBorder != null)
        {
            buttonBorder.raycastTarget = true;
        }

        // Apply initial gradient
        ApplyGradientToBackground();
        ApplyBorderColor(currentBorderColor);
        ApplyTextColor(currentTextColor);
    }

    private void OnEnable()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += ApplyTheme;

        // Apply current theme if available
        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    private void Update()
    {
        // Smooth scale lerp toward targetScale
        float current = transform.localScale.x / originalScale.x;
        if (!Mathf.Approximately(current, targetScale))
        {
            float newScale = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * animationSpeed);
            transform.localScale = originalScale * newScale;
        }
    }

    // ========== POINTER EVENT HANDLERS ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovered = true;
        targetScale = hoverScale;

        // Brighten border with glow
        ApplyBorderColor(currentHoverGlow);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetScale = 1f;

        // Restore normal border
        ApplyBorderColor(currentBorderColor);

        // Restore normal gradient
        ApplyGradientToBackground();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isPressed = true;
        targetScale = pressScale;

        // Darken background
        ApplyGradientToBackground(0.75f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isHovered)
        {
            // Return to hover state
            targetScale = hoverScale;
            ApplyGradientToBackground();
            ApplyBorderColor(currentHoverGlow);
        }
        else
        {
            // Return to normal state
            targetScale = 1f;
            ApplyGradientToBackground();
            ApplyBorderColor(currentBorderColor);
        }
    }

    // ========== THEME ==========

    /// <summary>
    /// Apply the given theme configuration to this button.
    /// Maps variant to the appropriate theme colors.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        UpdateVariantColors(theme);
        ApplyGradientToBackground();
        ApplyBorderColor(currentBorderColor);
        ApplyTextColor(currentTextColor);

        // Re-apply disabled look if not interactable
        if (!IsInteractable())
        {
            ApplyDisabledVisuals();
        }
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Set button interactability. Disabled state applies grayscale + reduced alpha.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (unityButton != null)
        {
            unityButton.interactable = interactable;
        }

        if (interactable)
        {
            ApplyGradientToBackground();
            ApplyBorderColor(currentBorderColor);
            ApplyTextColor(currentTextColor);
        }
        else
        {
            // Reset scale when disabled
            targetScale = 1f;
            isHovered = false;
            isPressed = false;
            ApplyDisabledVisuals();
        }
    }

    /// <summary>
    /// Update the button label text.
    /// </summary>
    public void SetLabel(string text)
    {
        if (buttonLabel != null)
        {
            buttonLabel.text = text;
        }
    }

    /// <summary>
    /// Get the current button variant.
    /// </summary>
    public ButtonVariant Variant => variant;

    /// <summary>
    /// Set the button variant and refresh visuals.
    /// </summary>
    public void SetVariant(ButtonVariant newVariant)
    {
        variant = newVariant;
        UpdateVariantColors(ThemeManager.ActiveTheme);
        ApplyGradientToBackground();
        ApplyBorderColor(currentBorderColor);
        ApplyTextColor(currentTextColor);
    }

    // ========== COLOR VARIANT LOGIC ==========

    /// <summary>
    /// Returns (gradientTop, gradientBottom, borderColor, textColor) based on current variant.
    /// Uses theme colors when available, otherwise falls back to sensible defaults.
    /// </summary>
    private (Color top, Color bottom, Color border, Color text) GetVariantColors(ThemeConfig theme)
    {
        Color top, bottom, border, text;

        switch (variant)
        {
            case ButtonVariant.Primary:
                // Gold (accent color)
                if (theme != null)
                {
                    top = theme.accentColor;
                    bottom = DarkenColor(theme.accentColor, 0.7f);
                }
                else
                {
                    top = primaryGradientTop;
                    bottom = primaryGradientBottom;
                }
                border = LightenColor(top, 1.2f);
                text = Color.white;
                break;

            case ButtonVariant.Secondary:
                // Purple (primary color)
                if (theme != null)
                {
                    top = theme.primaryColor;
                    bottom = DarkenColor(theme.primaryColor, 0.65f);
                }
                else
                {
                    top = new Color(0.6f, 0.4f, 0.8f);
                    bottom = new Color(0.4f, 0.25f, 0.6f);
                }
                border = LightenColor(top, 1.2f);
                text = Color.white;
                break;

            case ButtonVariant.Danger:
                // Red (negative color)
                if (theme != null)
                {
                    top = theme.negativeColor;
                    bottom = DarkenColor(theme.negativeColor, 0.65f);
                }
                else
                {
                    top = new Color(0.9f, 0.3f, 0.3f);
                    bottom = new Color(0.7f, 0.15f, 0.15f);
                }
                border = LightenColor(top, 1.2f);
                text = Color.white;
                break;

            case ButtonVariant.Success:
                // Green (positive color)
                if (theme != null)
                {
                    top = theme.positiveColor;
                    bottom = DarkenColor(theme.positiveColor, 0.65f);
                }
                else
                {
                    top = new Color(0.3f, 0.8f, 0.3f);
                    bottom = new Color(0.15f, 0.6f, 0.15f);
                }
                border = LightenColor(top, 1.2f);
                text = Color.white;
                break;

            default:
                top = primaryGradientTop;
                bottom = primaryGradientBottom;
                border = LightenColor(top, 1.2f);
                text = Color.white;
                break;
        }

        return (top, bottom, border, text);
    }

    private void UpdateVariantColors(ThemeConfig theme)
    {
        var colors = GetVariantColors(theme);
        currentGradientTop = colors.top;
        currentGradientBottom = colors.bottom;
        currentBorderColor = colors.border;
        currentTextColor = colors.text;

        // Hover glow: brighter version of border with some alpha
        currentHoverGlow = new Color(
            Mathf.Min(1f, colors.border.r * 1.3f),
            Mathf.Min(1f, colors.border.g * 1.3f),
            Mathf.Min(1f, colors.border.b * 1.3f),
            0.8f
        );
    }

    // ========== GRADIENT TEXTURE ==========

    /// <summary>
    /// Generate a vertical gradient texture (8x64 px) and apply as the background sprite.
    /// Uses darkenMultiplier for press state darkening.
    /// </summary>
    private void ApplyGradientToBackground(float darkenMultiplier = 1f)
    {
        if (buttonBackground == null) return;

        Color top = currentGradientTop * darkenMultiplier;
        Color bottom = currentGradientBottom * darkenMultiplier;
        // Preserve alpha
        top.a = 1f;
        bottom.a = 1f;

        int cacheKey = ComputeGradientHash(top, bottom);

        Texture2D gradientTex;
        if (!gradientTextureCache.TryGetValue(cacheKey, out gradientTex) || gradientTex == null)
        {
            gradientTex = CreateGradientTexture(top, bottom);
            gradientTextureCache[cacheKey] = gradientTex;
        }

        Sprite gradientSprite = Sprite.Create(
            gradientTex,
            new Rect(0, 0, gradientTex.width, gradientTex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        buttonBackground.sprite = gradientSprite;
        buttonBackground.type = Image.Type.Sliced;
        buttonBackground.color = Color.white; // Gradient color is in the texture
    }

    private static Texture2D CreateGradientTexture(Color top, Color bottom)
    {
        int width = 8;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            Color rowColor = Color.Lerp(bottom, top, t); // bottom is y=0, top is y=max
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = rowColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static int ComputeGradientHash(Color top, Color bottom)
    {
        // Simple hash combining both colors
        int hash = 17;
        hash = hash * 31 + Mathf.RoundToInt(top.r * 255f);
        hash = hash * 31 + Mathf.RoundToInt(top.g * 255f);
        hash = hash * 31 + Mathf.RoundToInt(top.b * 255f);
        hash = hash * 31 + Mathf.RoundToInt(bottom.r * 255f);
        hash = hash * 31 + Mathf.RoundToInt(bottom.g * 255f);
        hash = hash * 31 + Mathf.RoundToInt(bottom.b * 255f);
        return hash;
    }

    // ========== VISUAL HELPERS ==========

    private void ApplyBorderColor(Color color)
    {
        if (buttonBorder != null)
        {
            buttonBorder.color = color;
        }
    }

    private void ApplyTextColor(Color color)
    {
        if (buttonLabel != null)
        {
            buttonLabel.color = color;
        }
    }

    /// <summary>
    /// Apply disabled visuals: multiply colors by 0.4, alpha to 0.5.
    /// </summary>
    private void ApplyDisabledVisuals()
    {
        if (buttonBackground != null)
        {
            buttonBackground.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        }
        if (buttonBorder != null)
        {
            Color dimBorder = currentBorderColor * 0.4f;
            dimBorder.a = 0.5f;
            buttonBorder.color = dimBorder;
        }
        if (buttonLabel != null)
        {
            Color dimText = currentTextColor * 0.4f;
            dimText.a = 0.5f;
            buttonLabel.color = dimText;
        }
    }

    private bool IsInteractable()
    {
        if (!isInteractable) return false;
        if (unityButton != null && !unityButton.interactable) return false;
        return true;
    }

    private static Color DarkenColor(Color color, float factor)
    {
        return new Color(
            color.r * factor,
            color.g * factor,
            color.b * factor,
            color.a
        );
    }

    private static Color LightenColor(Color color, float factor)
    {
        return new Color(
            Mathf.Min(1f, color.r * factor),
            Mathf.Min(1f, color.g * factor),
            Mathf.Min(1f, color.b * factor),
            color.a
        );
    }
}
