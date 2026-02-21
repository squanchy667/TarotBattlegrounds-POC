using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TarotBattlegrounds.Combat.Replay;

namespace TarotBattlegrounds.Combat.Animator
{
    /// <summary>
    /// T305: Per-card visual representation during combat replay playback.
    /// Manages card display (name, attack, health, status) and provides
    /// animation methods for attack lunge, death fade, damage flash, etc.
    /// </summary>
    public class CombatCardVisual : MonoBehaviour
    {
        [Header("Card Display")]
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text healthText;

        [Header("Status Icons")]
        [SerializeField] private GameObject tauntIcon;
        [SerializeField] private GameObject aegisIcon;
        [SerializeField] private GameObject rebornIcon;
        [SerializeField] private GameObject venomousIcon;
        [SerializeField] private GameObject windfuryIcon;

        [Header("Visual")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.3f, 0.2f, 0.4f, 1f);
        [SerializeField] private Color damagedFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color buffFlashColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color aegisFlashColor = new Color(0.9f, 0.8f, 0.2f, 1f);

        // Snapshot data
        private CombatCardSnapshot snapshot;
        private int currentAttack;
        private int currentHealth;
        private Vector3 homePosition;
        private bool isDead;

        /// <summary>
        /// True if this card has been killed (health <= 0 and not reborn).
        /// </summary>
        public bool IsDead => isDead;

        /// <summary>
        /// The board side this card belongs to (0 = attacker, 1 = defender).
        /// </summary>
        public int Side { get; private set; }

        /// <summary>
        /// The board position index.
        /// </summary>
        public int BoardIndex { get; private set; }

        /// <summary>
        /// Initialize from a combat card snapshot.
        /// </summary>
        public void Setup(CombatCardSnapshot snap, int side, int boardIndex)
        {
            snapshot = snap;
            Side = side;
            BoardIndex = boardIndex;
            currentAttack = snap.attack;
            currentHealth = snap.health;
            isDead = false;

            if (cardNameText != null) cardNameText.text = snap.cardName;
            if (attackText != null) attackText.text = currentAttack.ToString();
            if (healthText != null) healthText.text = currentHealth.ToString();

            UpdateStatusIcons();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 1f;
            homePosition = transform.localPosition;

            if (cardBackground != null)
                cardBackground.color = normalColor;
        }

        /// <summary>
        /// Update displayed attack and health values.
        /// </summary>
        public void UpdateStats(int attack, int health)
        {
            currentAttack = attack;
            currentHealth = health;
            if (attackText != null) attackText.text = currentAttack.ToString();
            if (healthText != null) healthText.text = currentHealth.ToString();
        }

        /// <summary>
        /// Flash the card to indicate damage taken.
        /// </summary>
        public void FlashDamage(int newHealth)
        {
            currentHealth = newHealth;
            if (healthText != null) healthText.text = currentHealth.ToString();
            StartCoroutine(FlashColor(damagedFlashColor, 0.25f));
        }

        /// <summary>
        /// Flash the card to indicate a buff.
        /// </summary>
        public void FlashBuff(int attackDelta, int healthDelta)
        {
            currentAttack += attackDelta;
            currentHealth += healthDelta;
            if (attackText != null) attackText.text = currentAttack.ToString();
            if (healthText != null) healthText.text = currentHealth.ToString();
            StartCoroutine(FlashColor(buffFlashColor, 0.25f));
        }

        /// <summary>
        /// Flash for aegis pop.
        /// </summary>
        public void FlashAegisPop()
        {
            if (aegisIcon != null) aegisIcon.SetActive(false);
            StartCoroutine(FlashColor(aegisFlashColor, 0.3f));
        }

        // ====== T306: ATTACK ANIMATION ======

        /// <summary>
        /// T306: Lunge attack animation — card moves toward target and snaps back.
        /// </summary>
        public IEnumerator PlayAttackAnimation(Vector3 targetWorldPos, float duration)
        {
            Vector3 startPos = transform.localPosition;
            Vector3 lungeTarget = transform.parent != null
                ? transform.parent.InverseTransformPoint(targetWorldPos)
                : targetWorldPos;

            // Move partway toward target (70% of the way)
            Vector3 lungePos = Vector3.Lerp(startPos, lungeTarget, 0.7f);

            float halfDuration = duration * 0.4f;

            // Lunge forward
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                transform.localPosition = Vector3.Lerp(startPos, lungePos, t);
                yield return null;
            }

            // Brief pause at impact point
            yield return new WaitForSeconds(duration * 0.1f);

            // Snap back
            elapsed = 0f;
            float returnDuration = duration * 0.5f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                transform.localPosition = Vector3.Lerp(lungePos, startPos, t);
                yield return null;
            }

            transform.localPosition = startPos;
        }

        /// <summary>
        /// Simple shake to indicate being hit (for the target card).
        /// </summary>
        public IEnumerator PlayHitReaction(float duration)
        {
            Vector3 startPos = transform.localPosition;
            float elapsed = 0f;
            float shakeIntensity = 8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float offsetX = Random.Range(-shakeIntensity, shakeIntensity) * decay;
                float offsetY = Random.Range(-shakeIntensity, shakeIntensity) * decay;
                transform.localPosition = startPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            transform.localPosition = startPos;
        }

        // ====== T307: DEATH ANIMATION ======

        /// <summary>
        /// T307: Death animation — fade out + slight shrink.
        /// </summary>
        public IEnumerator PlayDeathAnimation(float duration)
        {
            isDead = true;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Fade out
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                // Slight shrink
                float scale = Mathf.Lerp(1f, 0.6f, t);
                transform.localScale = startScale * scale;

                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// T105: Reborn animation — flash and restore visibility.
        /// </summary>
        public IEnumerator PlayRebornAnimation(float duration)
        {
            isDead = false;
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.5f;

            if (rebornIcon != null) rebornIcon.SetActive(false);
            currentHealth = 1;
            if (healthText != null) healthText.text = "1";

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                float scale = Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0f, 1f, t));
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Summon animation for tokens — pop in from nothing.
        /// </summary>
        public IEnumerator PlaySummonAnimation(float duration)
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                float scale = Mathf.SmoothStep(0f, 1.1f, t);
                if (t > 0.8f)
                    scale = Mathf.Lerp(1.1f, 1f, (t - 0.8f) / 0.2f);
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        // ====== HELPERS ======

        private void UpdateStatusIcons()
        {
            if (snapshot == null) return;
            if (tauntIcon != null) tauntIcon.SetActive(snapshot.hasTaunt);
            if (aegisIcon != null) aegisIcon.SetActive(snapshot.hasAegis);
            if (rebornIcon != null) rebornIcon.SetActive(snapshot.hasReborn);
            if (venomousIcon != null) venomousIcon.SetActive(snapshot.hasVenomous);
            if (windfuryIcon != null) windfuryIcon.SetActive(snapshot.hasWindfury);
        }

        private IEnumerator FlashColor(Color flashColor, float duration)
        {
            if (cardBackground == null) yield break;

            Color original = cardBackground.color;
            cardBackground.color = flashColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                cardBackground.color = Color.Lerp(flashColor, original, t);
                yield return null;
            }

            cardBackground.color = original;
        }

        /// <summary>
        /// Programmatically build the card visual when no prefab is available.
        /// Creates all UI elements from code.
        /// </summary>
        public static CombatCardVisual CreateFromCode(Transform parent, CombatCardSnapshot snap, int side, int index)
        {
            GameObject cardObj = new GameObject($"CombatCard_{snap.cardName}_{side}_{index}");
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 140f);

            // Background
            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.2f, 0.4f, 0.9f);

            // Canvas group for fading
            CanvasGroup cg = cardObj.AddComponent<CanvasGroup>();

            // Name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.7f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            TMP_Text nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = snap.cardName;
            nameText.fontSize = 12;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8;
            nameText.fontSizeMax = 14;

            // Attack text (bottom-left)
            GameObject atkObj = new GameObject("AttackText");
            atkObj.transform.SetParent(cardObj.transform, false);
            RectTransform atkRt = atkObj.AddComponent<RectTransform>();
            atkRt.anchorMin = new Vector2(0f, 0f);
            atkRt.anchorMax = new Vector2(0.5f, 0.3f);
            atkRt.offsetMin = Vector2.zero;
            atkRt.offsetMax = Vector2.zero;
            TMP_Text atkText = atkObj.AddComponent<TextMeshProUGUI>();
            atkText.text = snap.attack.ToString();
            atkText.fontSize = 18;
            atkText.fontStyle = FontStyles.Bold;
            atkText.alignment = TextAlignmentOptions.Center;
            atkText.color = new Color(1f, 0.8f, 0.2f);

            // Health text (bottom-right)
            GameObject hpObj = new GameObject("HealthText");
            hpObj.transform.SetParent(cardObj.transform, false);
            RectTransform hpRt = hpObj.AddComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0.5f, 0f);
            hpRt.anchorMax = new Vector2(1f, 0.3f);
            hpRt.offsetMin = Vector2.zero;
            hpRt.offsetMax = Vector2.zero;
            TMP_Text hpText = hpObj.AddComponent<TextMeshProUGUI>();
            hpText.text = snap.health.ToString();
            hpText.fontSize = 18;
            hpText.fontStyle = FontStyles.Bold;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = new Color(0.2f, 1f, 0.2f);

            // Add the component and set references
            CombatCardVisual visual = cardObj.AddComponent<CombatCardVisual>();
            visual.cardNameText = nameText;
            visual.attackText = atkText;
            visual.healthText = hpText;
            visual.cardBackground = bg;
            visual.canvasGroup = cg;

            visual.Setup(snap, side, index);
            return visual;
        }
    }
}
