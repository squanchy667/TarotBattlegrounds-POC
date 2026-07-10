using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TarotBattlegrounds.UI;

namespace TarotBattlegrounds.Combat.Animator
{
    /// <summary>
    /// UX16: Combat Arena Visual enhancements.
    /// Provides a battlefield feel with center divider, team labels,
    /// slot indicators, attack trail lines, and a dark arena background.
    /// </summary>
    public class CombatArenaVisual : MonoBehaviour
    {
        [Header("Divider")]
        [SerializeField] private Image dividerLine;
        [SerializeField] private float dividerGlowSpeed = 1.5f;
        [SerializeField] private Color dividerColor = Tokens.WithAlpha(Tokens.Bronze, 0.6f);

        [Header("Team Labels")]
        [SerializeField] private TMP_Text attackerLabel;
        [SerializeField] private TMP_Text defenderLabel;
        [SerializeField] private Image attackerLabelBg;
        [SerializeField] private Image defenderLabelBg;

        [Header("Slot Indicators")]
        [SerializeField] private TMP_Text[] attackerSlotNumbers;
        [SerializeField] private TMP_Text[] defenderSlotNumbers;

        [Header("Attack Trail")]
        [SerializeField] private Image attackTrailLine;
        [SerializeField] private float trailDuration = 0.3f;
        [SerializeField] private Color trailColor = Tokens.WithAlpha(Tokens.Ember, 0.8f);

        [Header("Arena Background")]
        [SerializeField] private Image arenaBackground;
        [SerializeField] private Color arenaColor = Tokens.WithAlpha(Tokens.Ash, 0.9f);

        private bool isActive;
        private Coroutine dividerPulseCoroutine;
        private Coroutine trailFadeCoroutine;

        /// <summary>
        /// Show the arena visuals when combat starts.
        /// Sets team labels and activates the arena overlay.
        /// </summary>
        public void ShowArena(string attackerName, string defenderName)
        {
            isActive = true;

            // Arena background
            if (arenaBackground != null)
            {
                arenaBackground.gameObject.SetActive(true);
                arenaBackground.color = arenaColor;
                arenaBackground.raycastTarget = false;
            }

            // Divider line
            if (dividerLine != null)
            {
                dividerLine.gameObject.SetActive(true);
                dividerLine.color = dividerColor;
                dividerLine.raycastTarget = false;

                if (dividerPulseCoroutine != null)
                    StopCoroutine(dividerPulseCoroutine);
                dividerPulseCoroutine = StartCoroutine(PulseDivider());
            }

            // Team labels
            if (attackerLabel != null)
            {
                attackerLabel.text = !string.IsNullOrEmpty(attackerName) ? attackerName.ToUpper() : "YOUR BOARD";
                attackerLabel.gameObject.SetActive(true);
            }
            if (defenderLabel != null)
            {
                defenderLabel.text = !string.IsNullOrEmpty(defenderName) ? defenderName.ToUpper() : "OPPONENT";
                defenderLabel.gameObject.SetActive(true);
            }
            if (attackerLabelBg != null)
            {
                attackerLabelBg.gameObject.SetActive(true);
                attackerLabelBg.raycastTarget = false;
            }
            if (defenderLabelBg != null)
            {
                defenderLabelBg.gameObject.SetActive(true);
                defenderLabelBg.raycastTarget = false;
            }

            // Slot indicators
            SetSlotIndicatorsActive(true);

            // Hide attack trail initially
            if (attackTrailLine != null)
            {
                attackTrailLine.gameObject.SetActive(false);
                attackTrailLine.raycastTarget = false;
            }
        }

        /// <summary>
        /// Hide all arena visuals when combat ends.
        /// </summary>
        public void HideArena()
        {
            isActive = false;

            if (dividerPulseCoroutine != null)
            {
                StopCoroutine(dividerPulseCoroutine);
                dividerPulseCoroutine = null;
            }

            if (trailFadeCoroutine != null)
            {
                StopCoroutine(trailFadeCoroutine);
                trailFadeCoroutine = null;
            }

            if (arenaBackground != null)
                arenaBackground.gameObject.SetActive(false);
            if (dividerLine != null)
                dividerLine.gameObject.SetActive(false);
            if (attackerLabel != null)
                attackerLabel.gameObject.SetActive(false);
            if (defenderLabel != null)
                defenderLabel.gameObject.SetActive(false);
            if (attackerLabelBg != null)
                attackerLabelBg.gameObject.SetActive(false);
            if (defenderLabelBg != null)
                defenderLabelBg.gameObject.SetActive(false);
            if (attackTrailLine != null)
                attackTrailLine.gameObject.SetActive(false);

            SetSlotIndicatorsActive(false);
        }

        /// <summary>
        /// Show a brief attack trail line from attacker to defender position.
        /// The line is a stretched and rotated Image that fades out over trailDuration.
        /// </summary>
        public void ShowAttackTrail(Vector3 from, Vector3 to)
        {
            if (attackTrailLine == null) return;

            if (trailFadeCoroutine != null)
                StopCoroutine(trailFadeCoroutine);

            // Position the trail line between from and to
            RectTransform trailRect = attackTrailLine.rectTransform;

            // Convert world positions to local positions relative to the trail's parent
            Vector3 localFrom = from;
            Vector3 localTo = to;
            if (trailRect.parent != null)
            {
                RectTransform parentRect = trailRect.parent as RectTransform;
                if (parentRect != null)
                {
                    // Convert screen positions if needed, or use positions directly
                    // Since combat cards are UI elements, their positions are in the canvas space
                    localFrom = parentRect.InverseTransformPoint(from);
                    localTo = parentRect.InverseTransformPoint(to);
                }
            }

            // Calculate midpoint, distance, and angle
            Vector3 midpoint = (localFrom + localTo) * 0.5f;
            float distance = Vector3.Distance(localFrom, localTo);
            Vector3 direction = localTo - localFrom;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Position at midpoint
            trailRect.localPosition = midpoint;

            // Stretch width to match distance, thin height (3px)
            trailRect.sizeDelta = new Vector2(distance, 3f);

            // Rotate to point from -> to
            trailRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Set color and show
            attackTrailLine.color = trailColor;
            attackTrailLine.gameObject.SetActive(true);

            // Start fade-out coroutine
            trailFadeCoroutine = StartCoroutine(FadeTrail());
        }

        // ====== COROUTINES ======

        /// <summary>
        /// Pulse the divider line alpha using a sine wave for a glow effect.
        /// </summary>
        private IEnumerator PulseDivider()
        {
            while (isActive && dividerLine != null)
            {
                float alpha = Mathf.Lerp(0.3f, 0.8f,
                    (Mathf.Sin(Time.time * dividerGlowSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

                Color c = dividerColor;
                c.a = alpha;
                dividerLine.color = c;

                yield return null;
            }
        }

        /// <summary>
        /// Fade the attack trail line out over trailDuration seconds.
        /// </summary>
        private IEnumerator FadeTrail()
        {
            if (attackTrailLine == null) yield break;

            float elapsed = 0f;
            Color startColor = trailColor;

            while (elapsed < trailDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / trailDuration;

                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                attackTrailLine.color = c;

                yield return null;
            }

            attackTrailLine.gameObject.SetActive(false);
            trailFadeCoroutine = null;
        }

        // ====== HELPERS ======

        private void SetSlotIndicatorsActive(bool active)
        {
            if (attackerSlotNumbers != null)
            {
                foreach (var slot in attackerSlotNumbers)
                {
                    if (slot != null) slot.gameObject.SetActive(active);
                }
            }
            if (defenderSlotNumbers != null)
            {
                foreach (var slot in defenderSlotNumbers)
                {
                    if (slot != null) slot.gameObject.SetActive(active);
                }
            }
        }

        private void OnDisable()
        {
            if (dividerPulseCoroutine != null)
            {
                StopCoroutine(dividerPulseCoroutine);
                dividerPulseCoroutine = null;
            }
            if (trailFadeCoroutine != null)
            {
                StopCoroutine(trailFadeCoroutine);
                trailFadeCoroutine = null;
            }
        }
    }
}
