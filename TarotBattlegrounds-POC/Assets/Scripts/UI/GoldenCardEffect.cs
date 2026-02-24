using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// UX07: Golden card visual effects — animated shimmer, pulsing gold border,
    /// sparkle particles, and enhanced gold overlay.
    /// Attach alongside CardDisplayUI and CardFrameGenerator.
    /// Only active on golden (tripled) cards.
    /// </summary>
    public class GoldenCardEffect : MonoBehaviour
    {
        [Header("Shimmer")]
        [SerializeField] private Image shimmerOverlay;
        [SerializeField] private float shimmerInterval = 3f;
        [SerializeField] private float shimmerDuration = 0.8f;
        [SerializeField] private float shimmerWidth = 0.15f;  // Width of light band (0-1)

        [Header("Border Pulse")]
        [SerializeField] private Image borderImage;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float pulseMinAlpha = 0.3f;
        [SerializeField] private float pulseMaxAlpha = 0.8f;

        [Header("Sparkles")]
        [SerializeField] private ParticleSystem sparkleParticles;
        [SerializeField] private int sparkleCount = 8;
        [SerializeField] private Color sparkleColor = new Color(1f, 0.9f, 0.3f, 0.8f);

        [Header("Gold Overlay")]
        [SerializeField] private Image goldOverlay;
        [SerializeField] private Color goldTint = new Color(1f, 0.82f, 0.12f, 0.15f);

        private bool isActive = false;
        private RectTransform rectTransform;
        private Coroutine shimmerCoroutine;

        // Cached shimmer strip texture (shared across all golden cards)
        private static Texture2D _cachedShimmerTexture;
        private static Sprite _cachedShimmerSprite;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            // Cleanup: stop all coroutines and particles
            StopAllEffects();
        }

        private void Update()
        {
            if (!isActive) return;

            // Border pulse — smooth sine wave alpha oscillation
            if (borderImage != null)
            {
                float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                    (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
                var c = borderImage.color;
                c.a = alpha;
                borderImage.color = c;
            }
        }

        /// <summary>
        /// Enable or disable all golden card effects.
        /// </summary>
        public void SetActive(bool golden)
        {
            isActive = golden;

            if (golden)
            {
                StartEffects();
            }
            else
            {
                StopAllEffects();
            }
        }

        /// <summary>
        /// Size sparkle emission shape to match the given card rect.
        /// </summary>
        public void SetCardRect(RectTransform cardRect)
        {
            if (cardRect == null || sparkleParticles == null) return;

            var shape = sparkleParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(cardRect.rect.width, cardRect.rect.height, 1f);
        }

        /// <summary>
        /// Returns whether this effect is currently active.
        /// </summary>
        public bool IsActive => isActive;

        // ===== Effect Start/Stop =====

        private void StartEffects()
        {
            // Shimmer
            if (shimmerOverlay != null)
            {
                EnsureShimmerTexture();
                shimmerOverlay.gameObject.SetActive(true);
                shimmerOverlay.raycastTarget = false;
                shimmerOverlay.color = new Color(1f, 1f, 1f, 0f); // Start invisible
                if (shimmerCoroutine != null) StopCoroutine(shimmerCoroutine);
                shimmerCoroutine = StartCoroutine(ShimmerRoutine());
            }

            // Border pulse
            if (borderImage != null)
            {
                borderImage.gameObject.SetActive(true);
                borderImage.raycastTarget = false;
            }

            // Sparkles
            if (sparkleParticles != null)
            {
                ConfigureSparkles();
                sparkleParticles.gameObject.SetActive(true);
                sparkleParticles.Play();
            }

            // Gold overlay
            if (goldOverlay != null)
            {
                goldOverlay.gameObject.SetActive(true);
                goldOverlay.raycastTarget = false;
                goldOverlay.color = goldTint;
            }
        }

        private void StopAllEffects()
        {
            isActive = false;

            if (shimmerCoroutine != null)
            {
                StopCoroutine(shimmerCoroutine);
                shimmerCoroutine = null;
            }

            if (shimmerOverlay != null)
                shimmerOverlay.gameObject.SetActive(false);

            if (borderImage != null)
                borderImage.gameObject.SetActive(false);

            if (sparkleParticles != null)
            {
                sparkleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                sparkleParticles.gameObject.SetActive(false);
            }

            if (goldOverlay != null)
                goldOverlay.gameObject.SetActive(false);
        }

        // ===== Shimmer Effect (No Shader) =====

        /// <summary>
        /// Animate a thin bright diagonal strip across the card surface.
        /// Uses Image anchoredPosition animation with alpha fade at edges.
        /// Repeats every shimmerInterval seconds.
        /// </summary>
        private IEnumerator ShimmerRoutine()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(shimmerInterval);

                if (!isActive || shimmerOverlay == null) yield break;

                RectTransform shimmerRect = shimmerOverlay.rectTransform;
                if (rectTransform == null) yield break;

                float cardWidth = rectTransform.rect.width;
                float cardHeight = rectTransform.rect.height;

                // Travel distance: diagonal of the card
                float diagonal = Mathf.Sqrt(cardWidth * cardWidth + cardHeight * cardHeight);
                float startOffset = -diagonal * 0.3f;
                float endOffset = diagonal * 1.3f;

                float elapsed = 0f;
                while (elapsed < shimmerDuration)
                {
                    if (!isActive || shimmerOverlay == null) yield break;

                    float t = elapsed / shimmerDuration;
                    float pos = Mathf.Lerp(startOffset, endOffset, t);

                    // Move the shimmer strip diagonally (bottom-left to top-right)
                    shimmerRect.anchoredPosition = new Vector2(pos * 0.707f, pos * 0.707f);

                    // Alpha: fade in first 20%, full brightness 60%, fade out last 20%
                    float alpha;
                    if (t < 0.2f)
                        alpha = t / 0.2f;
                    else if (t > 0.8f)
                        alpha = (1f - t) / 0.2f;
                    else
                        alpha = 1f;

                    shimmerOverlay.color = new Color(1f, 0.95f, 0.7f, alpha * 0.3f);

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Reset shimmer to invisible
                shimmerOverlay.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// Ensure the shimmer overlay has a white-to-transparent gradient texture.
        /// Generated procedurally once and cached statically.
        /// </summary>
        private void EnsureShimmerTexture()
        {
            if (shimmerOverlay == null) return;

            if (_cachedShimmerSprite == null)
            {
                _cachedShimmerTexture = GenerateShimmerTexture();
                if (_cachedShimmerTexture != null)
                {
                    _cachedShimmerSprite = Sprite.Create(
                        _cachedShimmerTexture,
                        new Rect(0, 0, _cachedShimmerTexture.width, _cachedShimmerTexture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }

            if (_cachedShimmerSprite != null)
            {
                shimmerOverlay.sprite = _cachedShimmerSprite;
            }
        }

        /// <summary>
        /// Generate a thin diagonal gradient strip texture (64x64).
        /// Center band is bright white, fading to transparent on both sides.
        /// Rotated 45 degrees visually via the gradient direction.
        /// </summary>
        private static Texture2D GenerateShimmerTexture()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float bandCenter = 0.5f;
            float bandWidth = 0.15f; // Width of the bright band

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (float)x / size;
                    float py = (float)y / size;

                    // Diagonal coordinate (perpendicular to the sweep direction)
                    float diag = (px + py) * 0.5f;

                    // Distance from band center
                    float dist = Mathf.Abs(diag - bandCenter);
                    float alpha = 1f - Mathf.Clamp01(dist / bandWidth);

                    // Smooth falloff
                    alpha = alpha * alpha;

                    pixels[y * size + x] = new Color(1f, 0.95f, 0.8f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ===== Sparkle Particles =====

        /// <summary>
        /// Configure the ParticleSystem for small golden sparkles floating upward.
        /// Emission: 2-3 per second, lifetime 1-2s, tiny size, additive blending.
        /// </summary>
        private void ConfigureSparkles()
        {
            if (sparkleParticles == null) return;

            var main = sparkleParticles.main;
            main.startColor = sparkleColor;
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.maxParticles = sparkleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = -0.02f; // Slight upward drift

            var emission = sparkleParticles.emission;
            emission.rateOverTime = 2.5f; // 2-3 per second

            // Shape: rectangle matching card bounds
            var shape = sparkleParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            if (rectTransform != null)
            {
                shape.scale = new Vector3(
                    rectTransform.rect.width * 0.8f,
                    rectTransform.rect.height * 0.8f,
                    1f
                );
            }

            // Size over lifetime: shrink to nothing
            var sizeOverLifetime = sparkleParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // Color over lifetime: fade out
            var colorOverLifetime = sparkleParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = fadeGradient;

            // Renderer: billboard, additive
            var renderer = sparkleParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                // Use default particle material — additive blending will be via shader
                renderer.sortingOrder = 10; // Above card content
            }
        }

        // ===== Cleanup =====

        /// <summary>
        /// Clear cached shimmer texture. Call on application quit or scene unload.
        /// </summary>
        public static void ClearCache()
        {
            if (_cachedShimmerTexture != null)
            {
                Object.Destroy(_cachedShimmerTexture);
                _cachedShimmerTexture = null;
            }
            _cachedShimmerSprite = null;
        }
    }
}
