using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

                StartCoroutine(SlideIn(rect, group, slideDistance, duration));
                yield return new WaitForSeconds(staggerDelay);
            }
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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
