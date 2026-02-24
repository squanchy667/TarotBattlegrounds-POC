using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Styled resource bar displaying coin, health, and tier info with procedural icons.
/// Replaces plain text HUD with animated, themed containers.
/// </summary>
public class ResourceBar : MonoBehaviour, IThemeable
{
    [Header("Coin Display")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image coinContainer;

    [Header("Health Display")]
    [SerializeField] private Image healthIcon;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthContainer;

    [Header("Tier Display")]
    [SerializeField] private Image tierIcon;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Image tierContainer;

    [Header("Colors")]
    [SerializeField] private Color coinColor = new Color(1f, 0.82f, 0.12f);
    [SerializeField] private Color healthColor = new Color(0.9f, 0.2f, 0.25f);
    [SerializeField] private Color tierColor = new Color(0.55f, 0.3f, 0.75f);
    [SerializeField] private Color containerColor = new Color(0.08f, 0.05f, 0.14f, 0.8f);

    [Header("Animation")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float punchScale = 1.2f;

    private int lastCoinValue = -1;
    private int lastHealthValue = -1;
    private int lastTierValue = -1;

    // Cached procedural textures (generated once, shared across instances)
    private static Texture2D cachedCoinTexture;
    private static Texture2D cachedHeartTexture;
    private static Texture2D cachedTierBadgeTexture;

    private Coroutine coinFlashCoroutine;
    private Coroutine healthFlashCoroutine;
    private Coroutine tierFlashCoroutine;

    private void Awake()
    {
        // Generate procedural icons once
        if (cachedCoinTexture == null)
            cachedCoinTexture = GenerateCoinIcon();
        if (cachedHeartTexture == null)
            cachedHeartTexture = GenerateHeartIcon();
        if (cachedTierBadgeTexture == null)
            cachedTierBadgeTexture = GenerateTierBadge();

        // Apply generated textures to icon images
        ApplyProceduralIcons();

        // Apply container colors
        ApplyContainerColors();
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
    /// Apply theme colors to the resource bar.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Use theme accent color for coins, negative for health, primary for tier
        coinColor = theme.accentColor;
        healthColor = theme.negativeColor;
        tierColor = theme.primaryColor;
        containerColor = new Color(
            theme.secondaryColor.r,
            theme.secondaryColor.g,
            theme.secondaryColor.b,
            0.8f
        );

        ApplyContainerColors();
        ApplyIconColors();
    }

    // ====== ICON GENERATION (SDF-based procedural textures) ======

    /// <summary>
    /// Generate a 32x32 coin icon: circle with inner ring detail.
    /// </summary>
    private static Texture2D GenerateCoinIcon()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        float center = (size - 1) / 2f;
        float outerRadius = size / 2f - 1f;
        float innerRadius = outerRadius - 3f;
        float innerRingOuter = outerRadius - 2f;
        float innerRingInner = outerRadius - 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color pixel = Color.clear;

                if (dist <= outerRadius)
                {
                    // Outer fill - bright gold
                    float outerAlpha = Mathf.Clamp01((outerRadius - dist) * 2f);
                    pixel = new Color(1f, 0.82f, 0.12f, outerAlpha);

                    // Inner ring detail - darker gold
                    if (dist >= innerRingInner && dist <= innerRingOuter)
                    {
                        float ringAlpha = 1f - Mathf.Abs(dist - (innerRingInner + innerRingOuter) / 2f) / ((innerRingOuter - innerRingInner) / 2f);
                        Color ringColor = new Color(0.8f, 0.6f, 0.05f, ringAlpha * outerAlpha);
                        pixel = Color.Lerp(pixel, ringColor, ringAlpha * 0.6f);
                    }

                    // Inner highlight (top-left)
                    if (dist < innerRadius)
                    {
                        float highlightAngle = Mathf.Atan2(dy, dx);
                        float highlight = Mathf.Clamp01((-highlightAngle - 0.5f) * 0.3f);
                        pixel = Color.Lerp(pixel, new Color(1f, 0.95f, 0.6f, outerAlpha), highlight * 0.4f);
                    }
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 32x32 heart icon: two circles + triangle forming heart shape.
    /// </summary>
    private static Texture2D GenerateHeartIcon()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        float cx = (size - 1) / 2f;
        float cy = (size - 1) / 2f;

        // Heart shape using SDF: two circles on top + triangle pointing down
        float circleRadius = size * 0.22f;
        float leftCx = cx - circleRadius * 0.85f;
        float rightCx = cx + circleRadius * 0.85f;
        float circleCy = cy + circleRadius * 0.3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x;
                float py = y;

                // SDF for left circle
                float dLeft = Mathf.Sqrt((px - leftCx) * (px - leftCx) + (py - circleCy) * (py - circleCy)) - circleRadius;

                // SDF for right circle
                float dRight = Mathf.Sqrt((px - rightCx) * (px - rightCx) + (py - circleCy) * (py - circleCy)) - circleRadius;

                // SDF for triangle (bottom point)
                float triTop = circleCy - circleRadius * 0.3f;
                float triBottom = cy - circleRadius * 1.8f;
                float triWidth = (leftCx - circleRadius * 0.15f);

                float dTri = float.MaxValue;
                if (py < triTop && py > triBottom)
                {
                    float t = (triTop - py) / (triTop - triBottom);
                    float halfWidth = (cx - triWidth) * (1f - t);
                    float distFromCenter = Mathf.Abs(px - cx);
                    dTri = distFromCenter - halfWidth;
                }
                else if (py <= triBottom)
                {
                    dTri = Mathf.Sqrt((px - cx) * (px - cx) + (py - triBottom) * (py - triBottom));
                }

                // Union of shapes
                float d = Mathf.Min(Mathf.Min(dLeft, dRight), dTri);

                Color pixel = Color.clear;
                if (d < 1.0f)
                {
                    float alpha = Mathf.Clamp01(1.0f - d);
                    // Base red
                    pixel = new Color(0.9f, 0.2f, 0.25f, alpha);

                    // Highlight on upper-left
                    float highlightDist = Mathf.Sqrt((px - leftCx + 2f) * (px - leftCx + 2f) + (py - circleCy - 2f) * (py - circleCy - 2f));
                    float highlight = Mathf.Clamp01(1f - highlightDist / (circleRadius * 1.2f));
                    pixel = Color.Lerp(pixel, new Color(1f, 0.5f, 0.55f, alpha), highlight * 0.4f);
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Generate a 32x32 tier badge: diamond/shield shape.
    /// </summary>
    private static Texture2D GenerateTierBadge()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        float cx = (size - 1) / 2f;
        float cy = (size - 1) / 2f;

        // Diamond / shield shape using SDF
        float halfWidth = size * 0.38f;
        float halfHeight = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x - cx) / halfWidth;
                float py = (y - cy) / halfHeight;

                // Rotated square SDF (diamond)
                float d = (Mathf.Abs(px) + Mathf.Abs(py) - 1f) * Mathf.Min(halfWidth, halfHeight);

                Color pixel = Color.clear;
                if (d < 1.0f)
                {
                    float alpha = Mathf.Clamp01(1.0f - d);

                    // Base purple
                    pixel = new Color(0.55f, 0.3f, 0.75f, alpha);

                    // Inner border detail
                    float innerD = (Mathf.Abs(px) + Mathf.Abs(py) - 0.75f) * Mathf.Min(halfWidth, halfHeight);
                    if (Mathf.Abs(innerD) < 1.5f)
                    {
                        float borderAlpha = 1f - Mathf.Abs(innerD) / 1.5f;
                        pixel = Color.Lerp(pixel, new Color(0.75f, 0.5f, 0.95f, alpha), borderAlpha * 0.5f);
                    }

                    // Top highlight
                    float highlight = Mathf.Clamp01((py + 0.5f) * 0.8f);
                    pixel = Color.Lerp(pixel, new Color(0.7f, 0.45f, 0.9f, alpha), highlight * 0.3f);
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ====== VALUE UPDATES ======

    /// <summary>
    /// Update coin display. Flashes gold on change.
    /// </summary>
    public void UpdateCoins(int current, int max)
    {
        if (coinText != null)
            coinText.text = $"{current}/{max}";

        if (current != lastCoinValue && lastCoinValue >= 0)
        {
            if (coinFlashCoroutine != null)
                StopCoroutine(coinFlashCoroutine);

            RectTransform container = coinContainer != null ? coinContainer.rectTransform : null;
            if (coinText != null && container != null)
                coinFlashCoroutine = StartCoroutine(FlashValue(coinText, container, coinColor));
        }

        lastCoinValue = current;
    }

    /// <summary>
    /// Update health display. Flashes red on damage, green on heal.
    /// </summary>
    public void UpdateHealth(int current)
    {
        if (healthText != null)
            healthText.text = current.ToString();

        if (current != lastHealthValue && lastHealthValue >= 0)
        {
            Color flashColor = current < lastHealthValue ? healthColor : new Color(0.2f, 0.85f, 0.4f);

            if (healthFlashCoroutine != null)
                StopCoroutine(healthFlashCoroutine);

            RectTransform container = healthContainer != null ? healthContainer.rectTransform : null;
            if (healthText != null && container != null)
                healthFlashCoroutine = StartCoroutine(FlashValue(healthText, container, flashColor));
        }

        lastHealthValue = current;
    }

    /// <summary>
    /// Update tier display with Roman numerals and upgrade cost.
    /// </summary>
    public void UpdateTier(int tier, int upgradeCost)
    {
        if (tierText != null)
            tierText.text = $"Tier {ToRoman(tier)}";

        if (upgradeCostText != null)
        {
            if (tier >= 6)
            {
                string maxText = "MAX";
                if (ThemeManager.ActiveTheme != null)
                    maxText = ThemeManager.ActiveTheme.maxTierText;
                upgradeCostText.text = maxText;
            }
            else
            {
                upgradeCostText.text = $"{upgradeCost}g to upgrade";
            }
        }

        if (tier != lastTierValue && lastTierValue >= 0)
        {
            if (tierFlashCoroutine != null)
                StopCoroutine(tierFlashCoroutine);

            RectTransform container = tierContainer != null ? tierContainer.rectTransform : null;
            if (tierText != null && container != null)
                tierFlashCoroutine = StartCoroutine(FlashValue(tierText, container, tierColor));
        }

        lastTierValue = tier;
    }

    // ====== CHANGE ANIMATION ======

    /// <summary>
    /// Flash text color and punch scale on value change.
    /// </summary>
    private IEnumerator FlashValue(TMP_Text text, RectTransform container, Color flashColor)
    {
        var originalColor = text.color;
        text.color = flashColor;
        float elapsed = 0;
        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            float scale = 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI);
            container.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        container.localScale = Vector3.one;
        text.color = originalColor;
    }

    // ====== ROMAN NUMERAL CONVERSION ======

    /// <summary>
    /// Convert tier number to Roman numeral string.
    /// </summary>
    private string ToRoman(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        _ => tier.ToString()
    };

    // ====== HELPER METHODS ======

    /// <summary>
    /// Apply procedural icon textures to Image components.
    /// </summary>
    private void ApplyProceduralIcons()
    {
        if (coinIcon != null && cachedCoinTexture != null)
        {
            coinIcon.sprite = Sprite.Create(
                cachedCoinTexture,
                new Rect(0, 0, cachedCoinTexture.width, cachedCoinTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            coinIcon.raycastTarget = false;
        }

        if (healthIcon != null && cachedHeartTexture != null)
        {
            healthIcon.sprite = Sprite.Create(
                cachedHeartTexture,
                new Rect(0, 0, cachedHeartTexture.width, cachedHeartTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            healthIcon.raycastTarget = false;
        }

        if (tierIcon != null && cachedTierBadgeTexture != null)
        {
            tierIcon.sprite = Sprite.Create(
                cachedTierBadgeTexture,
                new Rect(0, 0, cachedTierBadgeTexture.width, cachedTierBadgeTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            tierIcon.raycastTarget = false;
        }

        ApplyIconColors();
    }

    /// <summary>
    /// Tint icons with their respective type colors.
    /// </summary>
    private void ApplyIconColors()
    {
        if (coinIcon != null) coinIcon.color = coinColor;
        if (healthIcon != null) healthIcon.color = healthColor;
        if (tierIcon != null) tierIcon.color = tierColor;
    }

    /// <summary>
    /// Apply dark container background colors.
    /// </summary>
    private void ApplyContainerColors()
    {
        if (coinContainer != null)
        {
            coinContainer.color = containerColor;
            coinContainer.raycastTarget = false;
        }
        if (healthContainer != null)
        {
            healthContainer.color = containerColor;
            healthContainer.raycastTarget = false;
        }
        if (tierContainer != null)
        {
            tierContainer.color = containerColor;
            tierContainer.raycastTarget = false;
        }
    }

    /// <summary>
    /// Reset cached values so next update triggers animations.
    /// Call when switching players.
    /// </summary>
    public void ResetCachedValues()
    {
        lastCoinValue = -1;
        lastHealthValue = -1;
        lastTierValue = -1;
    }
}
