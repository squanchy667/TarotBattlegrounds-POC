using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T402-T404: Generates procedural card frames with rarity borders,
    /// tribe-colored backgrounds, and golden overlays.
    /// UX05: Multi-layered card frame system with outer border, inner frame,
    /// tribe accent stripe, tier gem indicator, inner shadow, and smooth corners.
    /// Attach alongside CardDisplayUI for automatic visual enhancement.
    /// </summary>
    public class CardFrameGenerator : MonoBehaviour
    {
        [Header("Frame References")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image goldenOverlay;
        [SerializeField] private Image tribeAccentBar;

        [Header("Frame Layers")]
        [SerializeField] private Image outerFrame;        // Tier-colored outer border
        [SerializeField] private Image innerFrame;        // Subtle inner border
        [SerializeField] private Image innerShadow;       // Edge darkening overlay
        [SerializeField] private Image tierIndicator;     // Tier gem display

        [Header("Frame Width")]
        [SerializeField] private float frameBorderWidth = 4f;

        [Header("Frame Settings")]
        [SerializeField] private float outerBorderWidth = 3f;
        [SerializeField] private float innerBorderWidth = 1f;
        [SerializeField] private float accentBarHeight = 6f;

        // Texture size for procedural frame generation
        private const int FrameTextureSize = 128;
        // Texture size for tier gem indicator
        private const int GemTextureWidth = 64;
        private const int GemTextureHeight = 16;
        // Corner radius for rounded rectangles (in texture pixels)
        private const float CornerRadius = 10f;

        // Cached procedural frame textures per tier (generated once)
        private static Dictionary<int, Texture2D> _cachedFrameTextures = new Dictionary<int, Texture2D>();
        // Cached tier gem textures per tier
        private static Dictionary<int, Texture2D> _cachedGemTextures = new Dictionary<int, Texture2D>();

        // Tier-based rarity colors
        private static readonly Color[] TierFrameColors = new Color[]
        {
            new Color(0.55f, 0.55f, 0.60f, 1f),   // Tier 1: Cool gray (Common)
            new Color(0.15f, 0.75f, 0.30f, 1f),   // Tier 2: Rich green (Uncommon)
            new Color(0.20f, 0.45f, 0.95f, 1f),   // Tier 3: Vivid blue (Rare)
            new Color(0.65f, 0.18f, 0.85f, 1f),   // Tier 4: Royal purple (Epic)
            new Color(1.0f, 0.55f, 0.05f, 1f),    // Tier 5: Blazing orange (Legendary)
            new Color(1.0f, 0.82f, 0.10f, 1f)     // Tier 6: Legendary gold (Mythic)
        };

        // Tribe background tints (subtle, darkened)
        public static Color GetTribeBackgroundColor(TribeType tribe)
        {
            switch (tribe)
            {
                case TribeType.Pentacles: return new Color(0.12f, 0.18f, 0.08f, 1f);
                case TribeType.Cups:      return new Color(0.08f, 0.12f, 0.25f, 1f);
                case TribeType.Swords:    return new Color(0.22f, 0.08f, 0.10f, 1f);
                case TribeType.Wands:     return new Color(0.25f, 0.12f, 0.05f, 1f);
                case TribeType.Stars:     return new Color(0.18f, 0.18f, 0.08f, 1f);
                case TribeType.Coins:     return new Color(0.22f, 0.18f, 0.05f, 1f);
                default:                  return new Color(0.10f, 0.08f, 0.14f, 1f);
            }
        }

        // Tribe accent colors (vibrant, for accent bar)
        public static Color GetTribeAccentColor(TribeType tribe)
        {
            switch (tribe)
            {
                case TribeType.Pentacles: return new Color(0.2f, 0.8f, 0.3f, 1f);
                case TribeType.Cups:      return new Color(0.3f, 0.5f, 1f, 1f);
                case TribeType.Swords:    return new Color(0.9f, 0.2f, 0.2f, 1f);
                case TribeType.Wands:     return new Color(0.95f, 0.5f, 0.1f, 1f);
                case TribeType.Stars:     return new Color(1f, 0.9f, 0.3f, 1f);
                case TribeType.Coins:     return new Color(0.9f, 0.7f, 0.2f, 1f);
                default:                  return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        /// <summary>
        /// Apply full card frame visuals based on card data.
        /// </summary>
        public void ApplyCardVisuals(Card card)
        {
            if (card == null) return;

            // T402: Rarity border based on tier
            ApplyRarityFrame(card.tier);

            // UX05: Apply layered frame visuals
            ApplyOuterFrame(card.tier);
            ApplyInnerFrame(card.tier);
            ApplyInnerShadow();
            ApplyTierIndicator(card.tier);

            // T403: Tribe-colored background
            TribeType primaryTribe = card.GetPrimaryTribe();
            ApplyTribeBackground(primaryTribe);

            // T404: Golden overlay for triples (enhanced with gradient glow)
            ApplyGoldenOverlay(card.isGolden);
        }

        private void ApplyRarityFrame(int tier)
        {
            if (frameImage == null) return;

            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            frameImage.color = TierFrameColors[index];
        }

        private void ApplyTribeBackground(TribeType tribe)
        {
            if (backgroundImage != null)
                backgroundImage.color = GetTribeBackgroundColor(tribe);

            if (tribeAccentBar != null)
            {
                tribeAccentBar.color = GetTribeAccentColor(tribe);
                tribeAccentBar.gameObject.SetActive(tribe != TribeType.None);
            }
        }

        private void ApplyGoldenOverlay(bool isGolden)
        {
            if (goldenOverlay == null) return;

            goldenOverlay.gameObject.SetActive(isGolden);
            if (isGolden)
            {
                // UX05: Enhanced golden overlay — apply gradient glow texture for premium feel
                Texture2D glowTex = GenerateGoldenGlowTexture();
                if (glowTex != null)
                {
                    Sprite glowSprite = Sprite.Create(
                        glowTex,
                        new Rect(0, 0, glowTex.width, glowTex.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect,
                        new Vector4(8, 8, 8, 8) // 9-slice borders
                    );
                    goldenOverlay.sprite = glowSprite;
                    goldenOverlay.type = Image.Type.Sliced;
                }
                goldenOverlay.color = Color.white; // Texture carries the color/alpha
            }

            // UX07: Also activate GoldenCardEffect component if present
            GoldenCardEffect goldenEffect = GetComponent<GoldenCardEffect>();
            if (goldenEffect != null)
            {
                goldenEffect.SetActive(isGolden);
            }
        }

        /// <summary>
        /// Get frame color for a given tier (used by other UI systems).
        /// </summary>
        public static Color GetTierFrameColor(int tier)
        {
            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            return TierFrameColors[index];
        }

        // ===== UX05: Layered Frame Methods =====

        /// <summary>
        /// Apply outer frame with tier-colored procedural texture.
        /// Falls back to flat color if outerFrame is null.
        /// </summary>
        private void ApplyOuterFrame(int tier)
        {
            if (outerFrame == null) return;

            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            Color tierColor = TierFrameColors[index];

            Texture2D frameTex = GenerateFrameTexture(tier);
            if (frameTex != null)
            {
                Sprite frameSprite = Sprite.Create(
                    frameTex,
                    new Rect(0, 0, frameTex.width, frameTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(outerBorderWidth + innerBorderWidth + 4, outerBorderWidth + innerBorderWidth + 4,
                                outerBorderWidth + innerBorderWidth + 4, outerBorderWidth + innerBorderWidth + 4)
                );
                outerFrame.sprite = frameSprite;
                outerFrame.type = Image.Type.Sliced;
                outerFrame.color = Color.white; // Texture carries the tier color
            }
            else
            {
                outerFrame.color = tierColor;
            }
        }

        /// <summary>
        /// Apply inner frame — same tier color but 30% brighter for subtle depth.
        /// </summary>
        private void ApplyInnerFrame(int tier)
        {
            if (innerFrame == null) return;

            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            Color tierColor = TierFrameColors[index];

            // Brighten by 30%
            Color brighterColor = new Color(
                Mathf.Min(tierColor.r * 1.3f, 1f),
                Mathf.Min(tierColor.g * 1.3f, 1f),
                Mathf.Min(tierColor.b * 1.3f, 1f),
                tierColor.a
            );
            innerFrame.color = brighterColor;
        }

        /// <summary>
        /// Apply inner shadow — gradient overlay that darkens edges by 15% for depth.
        /// </summary>
        private void ApplyInnerShadow()
        {
            if (innerShadow == null) return;

            Texture2D shadowTex = GenerateInnerShadowTexture();
            if (shadowTex != null)
            {
                Sprite shadowSprite = Sprite.Create(
                    shadowTex,
                    new Rect(0, 0, shadowTex.width, shadowTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(16, 16, 16, 16) // 9-slice borders for edge gradient
                );
                innerShadow.sprite = shadowSprite;
                innerShadow.type = Image.Type.Sliced;
                innerShadow.color = Color.white;
            }
        }

        /// <summary>
        /// Apply tier indicator — small diamond gems showing tier count.
        /// </summary>
        private void ApplyTierIndicator(int tier)
        {
            if (tierIndicator == null) return;

            Texture2D gemTex = GenerateTierGems(tier);
            if (gemTex != null)
            {
                Sprite gemSprite = Sprite.Create(
                    gemTex,
                    new Rect(0, 0, gemTex.width, gemTex.height),
                    new Vector2(0.5f, 0.5f)
                );
                tierIndicator.sprite = gemSprite;
                tierIndicator.color = Color.white;
                tierIndicator.gameObject.SetActive(true);
            }
        }

        // ===== Procedural Texture Generation =====

        /// <summary>
        /// Generate a procedural rounded-rect frame texture with:
        /// - Outer border at specified width and tier color
        /// - Inner border at 1px and lighter color
        /// - Fill area transparent (so card content shows through)
        /// - Anti-aliased edges via SDF smoothstep
        /// Cached per tier (6 textures total, generated once).
        /// </summary>
        public Texture2D GenerateFrameTexture(int tier)
        {
            if (_cachedFrameTextures.ContainsKey(tier))
                return _cachedFrameTextures[tier];

            int size = FrameTextureSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            Color tierColor = TierFrameColors[index];
            Color innerColor = new Color(
                Mathf.Min(tierColor.r * 1.3f, 1f),
                Mathf.Min(tierColor.g * 1.3f, 1f),
                Mathf.Min(tierColor.b * 1.3f, 1f),
                tierColor.a
            );

            float outerWidth = outerBorderWidth;
            float innerWidth = innerBorderWidth;
            float radius = CornerRadius;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // SDF for rounded rectangle (outer edge)
                    float distOuter = RoundedRectSDF(px, py, size, size, radius);
                    // SDF for inner edge of outer border
                    float distInnerOuter = RoundedRectSDF(px, py, size - outerWidth * 2, size - outerWidth * 2, Mathf.Max(radius - outerWidth, 1f));
                    // SDF for inner edge of inner border
                    float distInnerInner = RoundedRectSDF(px, py, size - (outerWidth + innerWidth) * 2, size - (outerWidth + innerWidth) * 2, Mathf.Max(radius - outerWidth - innerWidth, 0f));

                    // Anti-aliased outer edge
                    float outerAlpha = 1f - Smoothstep(-0.5f, 0.5f, distOuter);
                    // Inner edge of outer border
                    float outerInnerAlpha = 1f - Smoothstep(-0.5f, 0.5f, distInnerOuter);
                    // Inner edge of inner border
                    float innerInnerAlpha = 1f - Smoothstep(-0.5f, 0.5f, distInnerInner);

                    // Outer border region: between outer edge and inner edge of outer border
                    float outerBorderMask = outerAlpha * (1f - outerInnerAlpha);
                    // Inner border region: between inner edge of outer border and inner edge of inner border
                    float innerBorderMask = outerInnerAlpha * (1f - innerInnerAlpha);

                    // Combine: outer border in tier color, inner border in brighter color, fill transparent
                    Color pixel = Color.clear;
                    if (outerBorderMask > 0.001f)
                    {
                        pixel = Color.Lerp(pixel, tierColor, outerBorderMask);
                    }
                    if (innerBorderMask > 0.001f)
                    {
                        pixel = Color.Lerp(pixel, innerColor, innerBorderMask);
                    }

                    pixels[y * size + x] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedFrameTextures[tier] = tex;
            return tex;
        }

        /// <summary>
        /// Generate a small texture (64x16) with tier number of diamond shapes.
        /// Diamond shapes in tier color, arranged horizontally.
        /// Background transparent. Tier 6 = 6 small golden diamonds in a row.
        /// Cached per tier.
        /// </summary>
        public Texture2D GenerateTierGems(int tier)
        {
            int clampedTier = Mathf.Clamp(tier, 1, 6);

            if (_cachedGemTextures.ContainsKey(clampedTier))
                return _cachedGemTextures[clampedTier];

            int width = GemTextureWidth;
            int height = GemTextureHeight;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int index = Mathf.Clamp(clampedTier - 1, 0, TierFrameColors.Length - 1);
            Color gemColor = TierFrameColors[index];

            Color[] pixels = new Color[width * height];

            // Clear to transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Diamond parameters
            float diamondSize = 5f;  // Half-size of each diamond
            float spacing = 9f;      // Center-to-center spacing
            float totalWidth = (clampedTier - 1) * spacing;
            float startX = (width - totalWidth) * 0.5f;
            float centerY = height * 0.5f;

            for (int gem = 0; gem < clampedTier; gem++)
            {
                float cx = startX + gem * spacing;
                float cy = centerY;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float px = x + 0.5f;
                        float py = y + 0.5f;

                        // Diamond SDF: |dx| + |dy| <= size
                        float dx = Mathf.Abs(px - cx);
                        float dy = Mathf.Abs(py - cy);
                        float dist = dx + dy - diamondSize;

                        float alpha = 1f - Smoothstep(-0.5f, 0.5f, dist);
                        if (alpha > 0.001f)
                        {
                            // Brighten center for gem-like effect
                            float centerDist = (dx + dy) / diamondSize;
                            float highlight = 1f - centerDist * 0.4f;
                            Color highlightedColor = new Color(
                                Mathf.Min(gemColor.r * highlight + 0.15f * (1f - centerDist), 1f),
                                Mathf.Min(gemColor.g * highlight + 0.15f * (1f - centerDist), 1f),
                                Mathf.Min(gemColor.b * highlight + 0.15f * (1f - centerDist), 1f),
                                alpha
                            );

                            int pi = y * width + x;
                            // Composite over existing pixel
                            Color existing = pixels[pi];
                            pixels[pi] = Color.Lerp(existing, highlightedColor, alpha);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedGemTextures[clampedTier] = tex;
            return tex;
        }

        /// <summary>
        /// Generate inner shadow texture — gradient that darkens edges by 15%.
        /// Creates a vignette-like effect for depth.
        /// </summary>
        private Texture2D GenerateInnerShadowTexture()
        {
            // Use tier 0 as key for the single shadow texture
            const int cacheKey = 0;
            if (_cachedFrameTextures.ContainsKey(cacheKey))
                return _cachedFrameTextures[cacheKey];

            int size = FrameTextureSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float edgeWidth = size * 0.25f; // Shadow gradient width (25% of texture from each edge)

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Distance from each edge (normalized 0-1)
                    float distLeft = px / edgeWidth;
                    float distRight = (size - px) / edgeWidth;
                    float distBottom = py / edgeWidth;
                    float distTop = (size - py) / edgeWidth;

                    // Take minimum distance to any edge
                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));
                    minDist = Mathf.Clamp01(minDist);

                    // Apply smoothstep for gradual fade
                    float shadowStrength = 1f - Smoothstep(0f, 1f, minDist);
                    // 15% max darkening
                    float alpha = shadowStrength * 0.15f;

                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedFrameTextures[cacheKey] = tex;
            return tex;
        }

        /// <summary>
        /// Generate golden glow texture — gradient that's brighter at edges.
        /// Creates a glowing frame effect for golden cards.
        /// </summary>
        private Texture2D GenerateGoldenGlowTexture()
        {
            const int cacheKey = -1; // Use negative key to avoid collision with tier textures
            if (_cachedFrameTextures.ContainsKey(cacheKey))
                return _cachedFrameTextures[cacheKey];

            int size = FrameTextureSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color goldenColor = new Color(1f, 0.85f, 0f);
            Color[] pixels = new Color[size * size];
            float edgeWidth = size * 0.3f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // Distance from each edge (normalized)
                    float distLeft = px / edgeWidth;
                    float distRight = (size - px) / edgeWidth;
                    float distBottom = py / edgeWidth;
                    float distTop = (size - py) / edgeWidth;

                    float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));
                    minDist = Mathf.Clamp01(minDist);

                    // Brighter at edges, subtle in center
                    float edgeGlow = 1f - Smoothstep(0f, 1f, minDist);
                    // Edge alpha: 0.35 at edges, 0.10 in center
                    float alpha = Mathf.Lerp(0.10f, 0.35f, edgeGlow);

                    pixels[y * size + x] = new Color(goldenColor.r, goldenColor.g, goldenColor.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedFrameTextures[cacheKey] = tex;
            return tex;
        }

        // ===== SDF Utilities =====

        /// <summary>
        /// Signed Distance Field for a centered rounded rectangle.
        /// Returns negative inside, zero on boundary, positive outside.
        /// </summary>
        private static float RoundedRectSDF(float px, float py, float rectWidth, float rectHeight, float radius)
        {
            float halfW = rectWidth * 0.5f;
            float halfH = rectHeight * 0.5f;
            float centerX = halfW + (FrameTextureSize - rectWidth) * 0.5f;
            float centerY = halfH + (FrameTextureSize - rectHeight) * 0.5f;

            // Offset point relative to center
            float dx = Mathf.Abs(px - centerX) - halfW + radius;
            float dy = Mathf.Abs(py - centerY) - halfH + radius;

            float outsideDist = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float insideDist = Mathf.Min(Mathf.Max(dx, dy), 0f);

            return outsideDist + insideDist - radius;
        }

        /// <summary>
        /// Smoothstep interpolation for anti-aliased edges.
        /// </summary>
        private static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Static helper to generate tier gem textures without an instance.
        /// Used by CardDisplayUI when no CardFrameGenerator is attached.
        /// </summary>
        public static Texture2D GenerateTierGemsStatic(int tier)
        {
            int clampedTier = Mathf.Clamp(tier, 1, 6);

            if (_cachedGemTextures.ContainsKey(clampedTier))
                return _cachedGemTextures[clampedTier];

            int width = GemTextureWidth;
            int height = GemTextureHeight;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int index = Mathf.Clamp(clampedTier - 1, 0, TierFrameColors.Length - 1);
            Color gemColor = TierFrameColors[index];

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            float diamondSize = 5f;
            float spacing = 9f;
            float totalWidth = (clampedTier - 1) * spacing;
            float startX = (width - totalWidth) * 0.5f;
            float centerY = height * 0.5f;

            for (int gem = 0; gem < clampedTier; gem++)
            {
                float cx = startX + gem * spacing;
                float cy = centerY;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float px = x + 0.5f;
                        float py = y + 0.5f;

                        float dx = Mathf.Abs(px - cx);
                        float dy = Mathf.Abs(py - cy);
                        float dist = dx + dy - diamondSize;

                        float alpha = 1f - Smoothstep(-0.5f, 0.5f, dist);
                        if (alpha > 0.001f)
                        {
                            float centerDist = (dx + dy) / diamondSize;
                            float highlight = 1f - centerDist * 0.4f;
                            Color highlightedColor = new Color(
                                Mathf.Min(gemColor.r * highlight + 0.15f * (1f - centerDist), 1f),
                                Mathf.Min(gemColor.g * highlight + 0.15f * (1f - centerDist), 1f),
                                Mathf.Min(gemColor.b * highlight + 0.15f * (1f - centerDist), 1f),
                                alpha
                            );

                            int pi = y * width + x;
                            Color existing = pixels[pi];
                            pixels[pi] = Color.Lerp(existing, highlightedColor, alpha);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedGemTextures[clampedTier] = tex;
            return tex;
        }

        /// <summary>
        /// Clear all cached textures. Call when changing frame settings at runtime.
        /// </summary>
        public static void ClearTextureCache()
        {
            foreach (var tex in _cachedFrameTextures.Values)
            {
                if (tex != null) Object.Destroy(tex);
            }
            _cachedFrameTextures.Clear();

            foreach (var tex in _cachedGemTextures.Values)
            {
                if (tex != null) Object.Destroy(tex);
            }
            _cachedGemTextures.Clear();
        }
    }
}
