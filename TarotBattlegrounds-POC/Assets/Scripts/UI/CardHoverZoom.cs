using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T405: Hover zoom — shows an enlarged card preview with full stats.
    /// Creates a floating zoom panel that follows the cursor when hovering over cards.
    /// Attach to a Canvas-level GameObject in the scene.
    /// </summary>
    public class CardHoverZoom : MonoBehaviour
    {
        public static CardHoverZoom Instance { get; private set; }

        [Header("Zoom Panel")]
        [SerializeField] private GameObject zoomPanel;
        [SerializeField] private RectTransform zoomRect;
        [SerializeField] private CanvasGroup zoomCanvasGroup;

        [Header("Card Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private TMP_Text tribeText;
        [SerializeField] private TMP_Text abilityText;
        [SerializeField] private TMP_Text loreText;

        [Header("Visuals")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image cardArtwork;

        [Header("Settings")]
        [SerializeField] private float zoomScale = 1.5f;
        [SerializeField] private float showDelay = 0.4f;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private Vector2 offset = new Vector2(180f, 0f);

        private Card currentCard;
        private float hoverTimer;
        private bool isShowing;
        private float targetAlpha;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Hide();
        }

        private void Update()
        {
            if (currentCard != null && !isShowing)
            {
                hoverTimer += Time.deltaTime;
                if (hoverTimer >= showDelay)
                {
                    isShowing = true;
                    targetAlpha = 1f;
                    if (zoomPanel != null) zoomPanel.SetActive(true);
                }
            }

            // Smooth fade
            if (zoomCanvasGroup != null)
            {
                zoomCanvasGroup.alpha = Mathf.MoveTowards(zoomCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
                if (zoomCanvasGroup.alpha <= 0.01f && targetAlpha == 0f && zoomPanel != null)
                    zoomPanel.SetActive(false);
            }

            // Position
            if (isShowing) PositionPanel();
        }

        public void ShowCard(Card card)
        {
            if (card == null) return;
            currentCard = card;
            hoverTimer = 0f;
            PopulateCard(card);
        }

        public void Hide()
        {
            currentCard = null;
            isShowing = false;
            hoverTimer = 0f;
            targetAlpha = 0f;
        }

        private void PopulateCard(Card card)
        {
            if (nameText != null) nameText.text = card.isGolden ? $"<color=#FFD700>{card.cardName}</color>" : card.cardName;
            if (attackText != null) attackText.text = card.attack.ToString();
            if (healthText != null) healthText.text = card.health.ToString();
            if (tierText != null) tierText.text = $"Tier {card.tier}";

            // Tribe display
            if (tribeText != null)
            {
                string tribes = "";
                if (card.tribes != null)
                {
                    foreach (var t in card.tribes)
                    {
                        if (t != TribeType.None)
                        {
                            if (tribes.Length > 0) tribes += " / ";
                            tribes += t.ToString();
                        }
                    }
                }
                tribeText.text = string.IsNullOrEmpty(tribes) ? "" : tribes;
                tribeText.color = card.tribes != null && card.tribes.Length > 0
                    ? CardFrameGenerator.GetTribeAccentColor(card.tribes[0])
                    : Color.gray;
            }

            // Ability description
            if (abilityText != null)
            {
                string abilityDesc = GetFullAbilityDescription(card);
                abilityText.text = abilityDesc;
                abilityText.gameObject.SetActive(!string.IsNullOrEmpty(abilityDesc));
            }

            // Lore/flavor text
            if (loreText != null)
            {
                loreText.text = !string.IsNullOrEmpty(card.ability) ? $"<i>\"{card.ability}\"</i>" : "";
                loreText.gameObject.SetActive(!string.IsNullOrEmpty(card.ability));
            }

            // Visuals
            if (cardFrame != null)
                cardFrame.color = CardFrameGenerator.GetTierFrameColor(card.tier);

            if (cardBackground != null)
            {
                TribeType primary = card.GetPrimaryTribe();
                cardBackground.color = CardFrameGenerator.GetTribeBackgroundColor(primary);
            }

            if (cardArtwork != null)
            {
                if (card.cardImage != null) { cardArtwork.sprite = card.cardImage; cardArtwork.color = Color.white; }
                else { cardArtwork.sprite = null; cardArtwork.color = new Color(0.3f, 0.3f, 0.3f, 1f); }
            }
        }

        private string GetFullAbilityDescription(Card card)
        {
            if (card.abilityTrigger == AbilityTrigger.None && card.abilityEffect == Card.AbilityEffectType.None)
                return "";

            string trigger = card.abilityTrigger.ToString();
            string effect = GetEffectText(card.abilityEffect, card.abilityValue);

            // Keyword abilities (no trigger needed)
            switch (card.abilityEffect)
            {
                case Card.AbilityEffectType.Reborn: return "<color=#90EE90>Reborn</color> — Returns with 1 Health after dying.";
                case Card.AbilityEffectType.Windfury: return "<color=#87CEEB>Windfury</color> — Attacks twice each combat.";
                case Card.AbilityEffectType.Venomous: return "<color=#9ACD32>Venomous</color> — Instantly destroys any minion it damages.";
                case Card.AbilityEffectType.Taunt: return "<color=#4169E1>Guardian</color> — Must be attacked first.";
                case Card.AbilityEffectType.GainArmor: return $"<color=#C0C0C0>Armor {card.abilityValue}</color> — Absorbs {card.abilityValue} damage before taking Health damage.";
            }

            return $"<color=#FFD700>{trigger}</color>: {effect}";
        }

        private string GetEffectText(Card.AbilityEffectType effect, int value)
        {
            switch (effect)
            {
                case Card.AbilityEffectType.BuffAdjacentAttack: return $"Give adjacent minions +{value} Attack.";
                case Card.AbilityEffectType.BuffAdjacentHealth: return $"Give adjacent minions +{value} Health.";
                case Card.AbilityEffectType.BuffAdjacentStats: return $"Give adjacent minions +{value}/+{value}.";
                case Card.AbilityEffectType.BuffAllFriendlyAttack: return $"Give all friendly minions +{value} Attack.";
                case Card.AbilityEffectType.BuffOtherFriendlyAttack: return $"Give all other friendly minions +{value} Attack.";
                case Card.AbilityEffectType.GainAegis: return "Gain Aegis (blocks one attack).";
                case Card.AbilityEffectType.GainCoins: return $"Gain {value} gold.";
                case Card.AbilityEffectType.DeathrattleBuffRandomFriendly: return $"Give a random friendly minion +{value}/+{value}.";
                case Card.AbilityEffectType.DeathrattleDamageRandomEnemy: return $"Deal {value} damage to a random enemy.";
                case Card.AbilityEffectType.DeathrattleDamageAllEnemies: return $"Deal {value} damage to all enemies.";
                case Card.AbilityEffectType.OnAttackBuffSelf: return $"Gain +{value} Attack permanently.";
                case Card.AbilityEffectType.OnAttackBonusDamage: return $"Deal +{value} extra damage.";
                case Card.AbilityEffectType.OnAttackCleave: return $"Deal {value} damage to adjacent enemies (Cleave).";
                case Card.AbilityEffectType.OnAllyDeathBuffSelf: return $"Gain +{value}/+{value} when an ally dies.";
                case Card.AbilityEffectType.OnAllyDeathBuffRandom: return $"Give a random ally +{value}/+{value} when an ally dies.";
                case Card.AbilityEffectType.OnAllySummonedBuffSelf: return $"Gain +{value}/+{value} when an ally is summoned.";
                case Card.AbilityEffectType.OnAllySummonedBuffSummoned: return $"Give summoned ally +{value}/+{value}.";
                case Card.AbilityEffectType.OnSellGainCoins: return $"Gain {value} extra gold when sold.";
                case Card.AbilityEffectType.OnSellBuffAllRemaining: return $"Give all remaining allies +{value}/+{value} when sold.";
                case Card.AbilityEffectType.AuraBuffTribematesAttack: return $"Tribemates have +{value} Attack (Aura).";
                case Card.AbilityEffectType.AuraBuffAdjacentStats: return $"Adjacent allies have +{value}/+{value} (Aura).";
                case Card.AbilityEffectType.AuraBuffAllFriendlyAttack: return $"All other allies have +{value} Attack (Aura).";
                case Card.AbilityEffectType.SummonTokenOnDeath: return $"Summon a {value}/{value} token when this dies.";
                case Card.AbilityEffectType.SummonTokenOnPlay: return $"Summon a {value}/{value} token when played.";
                case Card.AbilityEffectType.StealBuffOnAttack: return $"Steal +{value}/+{value} from the target when attacking.";
                case Card.AbilityEffectType.BuffAllTribeOnPlay: return $"Give all tribemates +{value}/+{value} when played.";
                case Card.AbilityEffectType.BuffAllTribeOnDeath: return $"Give all tribemates +{value}/+{value} on death.";
                case Card.AbilityEffectType.RandomTransformOnDeath: return "Transform into a random card on death.";
                case Card.AbilityEffectType.BuffSelfHealth: return $"Gain +{value} Health.";
                default: return effect.ToString();
            }
        }

        private void PositionPanel()
        {
            if (zoomRect == null) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 pos = mousePos + offset;

            // Keep on screen
            Vector2 size = zoomRect.sizeDelta * zoomScale;
            if (pos.x + size.x > Screen.width - 10f) pos.x = mousePos.x - size.x - 10f;
            if (pos.y + size.y * 0.5f > Screen.height - 10f) pos.y = Screen.height - size.y * 0.5f - 10f;
            if (pos.y - size.y * 0.5f < 10f) pos.y = size.y * 0.5f + 10f;

            zoomRect.position = pos;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
