using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T412-T413: Reusable UI animation utilities for shop refresh and tier-up celebrations.
    /// Provides static coroutine methods that can be called from any MonoBehaviour.
    /// </summary>
    public class UIAnimator : MonoBehaviour
    {
        public static UIAnimator Instance { get; private set; }

        [Header("Celebration Panel (optional)")]
        [SerializeField] private GameObject celebrationPanel;
        [SerializeField] private TMP_Text celebrationText;
        [SerializeField] private CanvasGroup celebrationGroup;

        [Header("Card Play Animations (UX15)")]
        [SerializeField] private float cardPlayArcHeight = 100f;
        [SerializeField] private float cardPlayDuration = 0.4f;
        [SerializeField] private float cardSellDuration = 0.3f;
        [SerializeField] private float cardBuyDuration = 0.3f;
        [SerializeField] private float coinBurstDuration = 0.5f;
        [SerializeField] private Color coinColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        [SerializeField] private Canvas rootCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (celebrationPanel != null) celebrationPanel.SetActive(false);
        }

        /// <summary>
        /// T412: Animate shop cards sliding in from the right.
        /// Call after populating shop slots.
        /// </summary>
        public void AnimateShopRefresh(Transform shopContainer)
        {
            if (shopContainer == null) return;
            StartCoroutine(ShopRefreshRoutine(shopContainer));
        }

        private IEnumerator ShopRefreshRoutine(Transform container)
        {
            float staggerDelay = 0.06f;
            float slideDistance = 200f;
            float duration = 0.25f;

            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                var rect = child.GetComponent<RectTransform>();
                var group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();

                // UX15: Enhanced with scale-from-zero pop and rotation wobble
                StartCoroutine(SlideInEnhanced(rect, group, slideDistance, duration));
                yield return new WaitForSeconds(staggerDelay);
            }
        }

        /// <summary>
        /// UX15: Enhanced slide-in with scale pop (0 → 1.1 → 1.0) and rotation wobble.
        /// </summary>
        private IEnumerator SlideInEnhanced(RectTransform rect, CanvasGroup group, float distance, float duration)
        {
            Vector2 startPos = rect.anchoredPosition + Vector2.right * distance;
            Vector2 endPos = rect.anchoredPosition;
            group.alpha = 0f;

            rect.anchoredPosition = startPos;
            rect.localScale = Vector3.zero;

            // Random rotation wobble target (-5 to +5 degrees)
            float wobbleAngle = Random.Range(-5f, 5f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                // Slide position
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // Fade in
                group.alpha = t;

                // Scale: pop to 1.1x then settle to 1.0x
                float scale;
                if (t < 0.7f)
                {
                    // Scale from 0 to 1.1 over first 70%
                    scale = Mathf.Lerp(0f, 1.1f, t / 0.7f);
                }
                else
                {
                    // Settle from 1.1 to 1.0 over last 30%
                    scale = Mathf.Lerp(1.1f, 1f, (t - 0.7f) / 0.3f);
                }
                rect.localScale = Vector3.one * scale;

                // Rotation wobble that settles to 0
                float rotAngle = Mathf.Lerp(wobbleAngle, 0f, t);
                rect.localRotation = Quaternion.Euler(0f, 0f, rotAngle);

                yield return null;
            }

            rect.anchoredPosition = endPos;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            group.alpha = 1f;
        }

        private IEnumerator SlideIn(RectTransform rect, CanvasGroup group, float distance, float duration)
        {
            Vector2 startPos = rect.anchoredPosition + Vector2.right * distance;
            Vector2 endPos = rect.anchoredPosition;
            group.alpha = 0f;

            rect.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                group.alpha = t;
                yield return null;
            }

            rect.anchoredPosition = endPos;
            group.alpha = 1f;
        }

        /// <summary>
        /// T413: Show tier-up celebration with scaling text.
        /// </summary>
        public void PlayTierUpCelebration(int newTier)
        {
            StartCoroutine(TierUpRoutine(newTier));
        }

        private IEnumerator TierUpRoutine(int newTier)
        {
            if (celebrationPanel == null || celebrationText == null) yield break;

            celebrationPanel.SetActive(true);
            celebrationText.text = $"Tier {newTier}!";

            if (celebrationGroup != null) celebrationGroup.alpha = 0f;

            RectTransform textRect = celebrationText.GetComponent<RectTransform>();

            // Scale up from 0 with bounce
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease out back curve
                float s = 1.70158f;
                float scale = 1f + (s + 1f) * Mathf.Pow(t - 1f, 3f) + s * Mathf.Pow(t - 1f, 2f);
                scale = Mathf.Max(0f, scale);

                if (textRect != null) textRect.localScale = Vector3.one * scale;
                if (celebrationGroup != null) celebrationGroup.alpha = Mathf.Clamp01(t * 3f);

                yield return null;
            }

            if (textRect != null) textRect.localScale = Vector3.one;
            if (celebrationGroup != null) celebrationGroup.alpha = 1f;

            // Hold briefly
            yield return new WaitForSeconds(1f);

            // Fade out
            elapsed = 0f;
            float fadeDuration = 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (celebrationGroup != null) celebrationGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            celebrationPanel.SetActive(false);
        }

        /// <summary>
        /// Generic scale punch animation for any RectTransform.
        /// </summary>
        public void PunchScale(RectTransform target, float punchScale = 1.2f, float duration = 0.2f)
        {
            if (target != null) StartCoroutine(PunchScaleRoutine(target, punchScale, duration));
        }

        private IEnumerator PunchScaleRoutine(RectTransform target, float punchScale, float duration)
        {
            float half = duration * 0.5f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float s = Mathf.Lerp(1f, punchScale, Mathf.SmoothStep(0f, 1f, t));
                target.localScale = Vector3.one * s;
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                float s = Mathf.Lerp(punchScale, 1f, Mathf.SmoothStep(0f, 1f, t));
                target.localScale = Vector3.one * s;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        // ====== UX15: Card Play Animations ======

        /// <summary>
        /// UX15: Animate card flying from hand to board slot with arc trajectory (Bezier curve).
        /// </summary>
        public void AnimateCardPlay(RectTransform card, RectTransform targetSlot, System.Action onComplete)
        {
            if (card == null || targetSlot == null)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(CardPlayRoutine(card, targetSlot, onComplete));
        }

        private IEnumerator CardPlayRoutine(RectTransform card, RectTransform targetSlot, System.Action onComplete)
        {
            // Convert positions to common space (parent canvas)
            Vector3 startWorldPos = card.position;
            Vector3 endWorldPos = targetSlot.position;

            // Bezier control point: midpoint raised by arcHeight
            Vector3 controlPoint = (startWorldPos + endWorldPos) / 2f + Vector3.up * cardPlayArcHeight;

            float elapsed = 0f;
            while (elapsed < cardPlayDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cardPlayDuration);

                // Smooth the parameter
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Quadratic Bezier: B(t) = (1-t)^2*P0 + 2*(1-t)*t*P1 + t^2*P2
                Vector3 pos = Mathf.Pow(1f - smoothT, 2f) * startWorldPos
                            + 2f * (1f - smoothT) * smoothT * controlPoint
                            + smoothT * smoothT * endWorldPos;

                card.position = pos;

                // Slight scale punch at midpoint
                float scalePunch = 1f + 0.15f * Mathf.Sin(smoothT * Mathf.PI);
                card.localScale = Vector3.one * scalePunch;

                yield return null;
            }

            card.position = endWorldPos;
            card.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        /// <summary>
        /// UX15: Animate card sell — shrink to zero with 180-degree rotation spin.
        /// Spawns a coin burst at the card position on completion.
        /// </summary>
        public void AnimateCardSell(RectTransform card, System.Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(CardSellRoutine(card, onComplete));
        }

        private IEnumerator CardSellRoutine(RectTransform card, System.Action onComplete)
        {
            Vector3 startScale = card.localScale;
            Quaternion startRot = card.localRotation;
            Vector3 coinPos = card.position;

            CanvasGroup group = card.GetComponent<CanvasGroup>();
            if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < cardSellDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cardSellDuration);

                // Shrink to zero
                float scale = Mathf.Lerp(1f, 0f, t);
                card.localScale = startScale * scale;

                // Rotate 180 degrees
                float angle = Mathf.Lerp(0f, 180f, t);
                card.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);

                // Fade out in last 30%
                if (t > 0.7f)
                    group.alpha = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);

                yield return null;
            }

            card.localScale = Vector3.zero;
            group.alpha = 0f;

            // Fire coin burst at the card's last known position
            AnimateCoinBurst(coinPos, 5);

            onComplete?.Invoke();
        }

        /// <summary>
        /// UX15: Animate card buy — slide from shop to hand slot with subtle glow.
        /// </summary>
        public void AnimateCardBuy(RectTransform card, RectTransform handSlot, System.Action onComplete)
        {
            if (card == null || handSlot == null)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(CardBuyRoutine(card, handSlot, onComplete));
        }

        private IEnumerator CardBuyRoutine(RectTransform card, RectTransform handSlot, System.Action onComplete)
        {
            Vector3 startPos = card.position;
            Vector3 endPos = handSlot.position;

            CanvasGroup group = card.GetComponent<CanvasGroup>();
            if (group == null) group = card.gameObject.AddComponent<CanvasGroup>();

            // Create subtle glow overlay on the card
            GameObject glowObj = null;
            Image glowImage = null;
            if (card != null)
            {
                glowObj = new GameObject("BuyGlow");
                glowObj.transform.SetParent(card, false);
                RectTransform glowRect = glowObj.AddComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = new Vector2(-8f, -8f);
                glowRect.offsetMax = new Vector2(8f, 8f);
                glowImage = glowObj.AddComponent<Image>();
                glowImage.color = new Color(1f, 0.9f, 0.4f, 0f); // Warm gold glow, starts invisible
                glowImage.raycastTarget = false;

                // Move glow behind card content
                glowRect.SetAsFirstSibling();
            }

            float elapsed = 0f;
            while (elapsed < cardBuyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / cardBuyDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Slide from shop to hand
                card.position = Vector3.Lerp(startPos, endPos, smoothT);

                // Scale punch at midpoint
                float scalePunch = 1f + 0.1f * Mathf.Sin(smoothT * Mathf.PI);
                card.localScale = Vector3.one * scalePunch;

                // Glow intensity peaks at midpoint
                if (glowImage != null)
                {
                    float glowAlpha = 0.5f * Mathf.Sin(smoothT * Mathf.PI);
                    glowImage.color = new Color(1f, 0.9f, 0.4f, glowAlpha);
                }

                yield return null;
            }

            card.position = endPos;
            card.localScale = Vector3.one;

            // Clean up glow
            if (glowObj != null)
                Destroy(glowObj);

            onComplete?.Invoke();
        }

        /// <summary>
        /// UX15: Spawn coinCount small gold circles that fly outward and fade.
        /// Uses a temporary UI canvas overlay for the particles.
        /// </summary>
        public void AnimateCoinBurst(Vector3 worldPosition, int coinCount)
        {
            if (coinCount <= 0) return;
            StartCoroutine(CoinBurstRoutine(worldPosition, coinCount));
        }

        private IEnumerator CoinBurstRoutine(Vector3 worldPosition, int coinCount)
        {
            // Find or create a canvas for the coin particles
            Canvas canvas = rootCanvas;
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null) yield break;

            List<RectTransform> coins = new List<RectTransform>(coinCount);
            List<Image> coinImages = new List<Image>(coinCount);
            List<Vector2> directions = new List<Vector2>(coinCount);

            for (int i = 0; i < coinCount; i++)
            {
                GameObject coinObj = new GameObject($"CoinParticle_{i}");
                coinObj.transform.SetParent(canvas.transform, false);

                RectTransform coinRect = coinObj.AddComponent<RectTransform>();
                coinRect.sizeDelta = new Vector2(16f, 16f);
                coinRect.position = worldPosition;

                Image coinImg = coinObj.AddComponent<Image>();
                coinImg.color = coinColor;
                coinImg.raycastTarget = false;

                coins.Add(coinRect);
                coinImages.Add(coinImg);

                // Random outward direction
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(80f, 180f);
                directions.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed);
            }

            float elapsed = 0f;
            while (elapsed < coinBurstDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / coinBurstDuration);

                for (int i = 0; i < coins.Count; i++)
                {
                    if (coins[i] == null) continue;

                    // Move outward
                    coins[i].anchoredPosition += directions[i] * Time.deltaTime;

                    // Shrink and fade
                    float alpha = Mathf.Lerp(1f, 0f, t);
                    float scale = Mathf.Lerp(1f, 0.3f, t);
                    coins[i].localScale = Vector3.one * scale;

                    if (coinImages[i] != null)
                    {
                        Color c = coinImages[i].color;
                        c.a = alpha;
                        coinImages[i].color = c;
                    }
                }

                yield return null;
            }

            // Clean up
            for (int i = 0; i < coins.Count; i++)
            {
                if (coins[i] != null)
                    Destroy(coins[i].gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
