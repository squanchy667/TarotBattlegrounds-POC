using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// UX06: Small component for stat circle badges (ATK, HP, Cost).
    /// Displays a number inside a colored circular background.
    /// Supports buffed/damaged stat color indication.
    /// </summary>
    public class StatBadge : MonoBehaviour
    {
        [SerializeField] private Image badgeBackground;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Color badgeColor = Color.gray;

        // Cached procedural circle texture (32x32, shared across all badges)
        private static Texture2D _cachedCircleTexture;
        private static Sprite _cachedCircleSprite;

        // Stat value color constants
        private static readonly Color normalColor = Color.white;
        private static readonly Color buffedColor = new Color(0.3f, 1f, 0.3f, 1f);   // Bright green
        private static readonly Color damagedColor = new Color(1f, 0.25f, 0.25f, 1f); // Red

        /// <summary>
        /// Set the displayed value and optionally indicate buff/damage state.
        /// </summary>
        /// <param name="value">The stat value to display.</param>
        /// <param name="comparisonBase">Base stat value to compare against. -1 means no comparison.</param>
        public void SetValue(int value, int comparisonBase = -1)
        {
            if (valueText != null)
            {
                valueText.text = value.ToString();

                // Determine color based on comparison to base stat
                if (comparisonBase >= 0)
                {
                    if (value > comparisonBase)
                        valueText.color = buffedColor;
                    else if (value < comparisonBase)
                        valueText.color = damagedColor;
                    else
                        valueText.color = normalColor;
                }
                else
                {
                    valueText.color = normalColor;
                }
            }

            // Ensure circle texture is applied
            ApplyCircleTexture();
        }

        /// <summary>
        /// Set the badge background color.
        /// </summary>
        public void SetBadgeColor(Color color)
        {
            badgeColor = color;
            if (badgeBackground != null)
                badgeBackground.color = color;
        }

        /// <summary>
        /// Apply the procedural circle texture to the badge background.
        /// Generates once and caches statically.
        /// </summary>
        private void ApplyCircleTexture()
        {
            if (badgeBackground == null) return;

            if (_cachedCircleSprite == null)
            {
                _cachedCircleTexture = GenerateCircleTexture(32);
                if (_cachedCircleTexture != null)
                {
                    _cachedCircleSprite = Sprite.Create(
                        _cachedCircleTexture,
                        new Rect(0, 0, 32, 32),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect,
                        new Vector4(8, 8, 8, 8) // 9-slice borders
                    );
                }
            }

            if (_cachedCircleSprite != null && badgeBackground.sprite != _cachedCircleSprite)
            {
                badgeBackground.sprite = _cachedCircleSprite;
                badgeBackground.type = Image.Type.Sliced;
            }

            badgeBackground.color = badgeColor;
        }

        /// <summary>
        /// Generate a 32x32 circle texture with a slight shadow/border for depth.
        /// Anti-aliased using SDF smoothstep.
        /// </summary>
        private static Texture2D GenerateCircleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float radius = center - 1.5f; // Leave room for anti-aliasing
            float shadowRadius = center - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dist = Mathf.Sqrt((px - center) * (px - center) + (py - center) * (py - center));

                    // Shadow ring (slightly larger, darker)
                    float shadowAlpha = 1f - Smoothstep(shadowRadius - 0.5f, shadowRadius + 0.5f, dist);
                    // Main circle
                    float circleAlpha = 1f - Smoothstep(radius - 0.5f, radius + 0.5f, dist);

                    // Combine: shadow outside circle boundary
                    Color pixel = Color.clear;
                    if (shadowAlpha > 0.001f)
                    {
                        pixel = new Color(0f, 0f, 0f, shadowAlpha * 0.4f); // Dark shadow
                    }
                    if (circleAlpha > 0.001f)
                    {
                        // White fill — the Image.color will tint this
                        pixel = new Color(1f, 1f, 1f, circleAlpha);
                    }

                    pixels[y * size + x] = pixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
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
        /// Clear the cached circle texture. Call on application quit or scene unload.
        /// </summary>
        public static void ClearCache()
        {
            if (_cachedCircleTexture != null)
            {
                Object.Destroy(_cachedCircleTexture);
                _cachedCircleTexture = null;
            }
            _cachedCircleSprite = null;
        }
    }
}
