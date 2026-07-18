using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TarotBattlegrounds.Combat.Replay;
using TarotBattlegrounds.UI;

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
        [SerializeField] private Color normalColor = Tokens.CharredWood;
        [SerializeField] private Color damagedFlashColor = Tokens.BloodDeep;
        [SerializeField] private Color buffFlashColor = Tokens.BronzeBright;
        [SerializeField] private Color aegisFlashColor = Tokens.EtherBlue; // aegis = magic shield; unified with FloatingNumber (sanctioned ether use in combat)

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
            // UX14: Calculate damage amount before updating health
            int damageAmount = currentHealth - newHealth;

            currentHealth = newHealth;
            if (healthText != null) healthText.text = currentHealth.ToString();
            StartCoroutine(FlashColor(damagedFlashColor, 0.25f));

            // UX14: Show floating damage number
            if (damageAmount > 0 && FloatingNumberManager.Instance != null)
                FloatingNumberManager.Instance.ShowDamage(GetWorldPosition(), damageAmount);
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

            // UX14: Show floating buff number
            if (FloatingNumberManager.Instance != null)
            {
                string buffText;
                if (attackDelta > 0 && healthDelta > 0)
                    buffText = $"+{attackDelta}/+{healthDelta}";
                else if (attackDelta > 0)
                    buffText = $"+{attackDelta} ATK";
                else if (healthDelta > 0)
                    buffText = $"+{healthDelta} HP";
                else
                    return; // No meaningful buff to display

                FloatingNumberManager.Instance.ShowBuff(GetWorldPosition(), buffText);
            }
        }

        /// <summary>
        /// Flash for aegis pop.
        /// </summary>
        public void FlashAegisPop()
        {
            if (aegisIcon != null) aegisIcon.SetActive(false);
            StartCoroutine(FlashColor(aegisFlashColor, 0.3f));

            // UX14: Show floating aegis pop text
            if (FloatingNumberManager.Instance != null)
                FloatingNumberManager.Instance.ShowAegisPop(GetWorldPosition());
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

        // ====== UX14: FLOATING NUMBERS ======

        /// <summary>
        /// UX14: Get the world position of this card for floating number placement.
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
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
            // Larger for mobile readability
            rt.sizeDelta = new Vector2(120f, 168f);

            // Outer edge: side color so attacker vs defender is obvious
            Image edge = cardObj.AddComponent<Image>();
            edge.color = side == 0
                ? Tokens.WithAlpha(Tokens.BronzeBright, 0.95f)
                : Tokens.WithAlpha(Tokens.Blood, 0.95f);
            edge.raycastTarget = false;

            // Inner face
            GameObject face = new GameObject("Face");
            face.transform.SetParent(cardObj.transform, false);
            RectTransform faceRt = face.AddComponent<RectTransform>();
            faceRt.anchorMin = Vector2.zero;
            faceRt.anchorMax = Vector2.one;
            faceRt.offsetMin = new Vector2(4f, 4f);
            faceRt.offsetMax = new Vector2(-4f, -4f);
            Image bg = face.AddComponent<Image>();
            // T763: CharredWood@0.96 was near-black on a dark arena. StoneEdge is the elevated
            // surface token — solid enough for text, bright enough to read ATK/HP chips.
            bg.color = Tokens.WithAlpha(Tokens.StoneEdge, 0.94f);
            bg.raycastTarget = false;
            // Prefer kit frame if available (stone shell)
            if (UiSprites.Instance != null && UiSprites.Instance.GetCardFrame(1) != null)
                UiSprites.ApplySliced(bg, UiSprites.Instance.GetCardFrame(1));

            CanvasGroup cg = cardObj.AddComponent<CanvasGroup>();

            // Name plate
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(face.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.62f);
            nameRt.anchorMax = new Vector2(0.95f, 0.96f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            TMP_Text nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = snap != null ? snap.cardName : "?";
            nameText.fontSize = Tokens.TextCaption;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Tokens.BoneBright;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 10;
            nameText.fontSizeMax = 18;
            nameText.enableWordWrapping = true;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.raycastTarget = false;
            if (FontRefs.Instance != null && FontRefs.Instance.Label != null)
                nameText.font = FontRefs.Instance.Label;

            // Attack chip (bottom-left) — Blood
            TMP_Text atkText = CreateStatChip(face.transform, "AttackText",
                new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.38f),
                snap != null ? snap.attack.ToString() : "0", Tokens.Blood);

            // Health chip (bottom-right) — Ember/positive
            TMP_Text hpText = CreateStatChip(face.transform, "HealthText",
                new Vector2(0.52f, 0.02f), new Vector2(0.98f, 0.38f),
                snap != null ? snap.health.ToString() : "0", Tokens.Ember);

            CombatCardVisual visual = cardObj.AddComponent<CombatCardVisual>();
            visual.cardNameText = nameText;
            visual.attackText = atkText;
            visual.healthText = hpText;
            visual.cardBackground = bg;
            visual.canvasGroup = cg;
            visual.normalColor = bg.color;

            visual.Setup(snap, side, index);
            return visual;
        }

        private static TMP_Text CreateStatChip(Transform parent, string name,
            Vector2 aMin, Vector2 aMax, string value, Color accent)
        {
            GameObject chip = new GameObject(name + "Chip");
            chip.transform.SetParent(parent, false);
            RectTransform chipRt = chip.AddComponent<RectTransform>();
            chipRt.anchorMin = aMin;
            chipRt.anchorMax = aMax;
            chipRt.offsetMin = new Vector2(2f, 2f);
            chipRt.offsetMax = new Vector2(-2f, -2f);
            Image chipBg = chip.AddComponent<Image>();
            chipBg.color = Tokens.WithAlpha(Tokens.Ash, 0.85f);
            chipBg.raycastTarget = false;

            GameObject textGo = new GameObject(name);
            textGo.transform.SetParent(chip.transform, false);
            RectTransform tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = Tokens.TextBody;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = accent;
            tmp.raycastTarget = false;
            if (FontRefs.Instance != null && FontRefs.Instance.Body != null)
                tmp.font = FontRefs.Instance.Body;
            return tmp;
        }
    }
}
