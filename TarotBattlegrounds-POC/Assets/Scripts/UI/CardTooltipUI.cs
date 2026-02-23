using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton tooltip panel that displays detailed card information on hover.
/// Create a UI panel in your scene and assign this component.
/// </summary>
public class CardTooltipUI : MonoBehaviour
{
    public static CardTooltipUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Card Info")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text tribesText;

    [Header("Ability Section")]
    [SerializeField] private GameObject abilitySection;
    [SerializeField] private TMP_Text abilityTriggerText;
    [SerializeField] private TMP_Text abilityEffectText;
    [SerializeField] private TMP_Text abilityDescriptionText;

    [Header("Legacy Effect Section")]
    [SerializeField] private GameObject legacyEffectSection;
    [SerializeField] private TMP_Text legacyEffectText;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(25f, -25f);
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float tooltipWidth = 300f;
    [SerializeField] private float edgePadding = 15f;

    private Card currentCard;
    private float hoverTimer;
    private bool isWaitingToShow;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Find canvas for positioning
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        // Set tooltip width
        if (tooltipRect != null)
        {
            Vector2 size = tooltipRect.sizeDelta;
            size.x = tooltipWidth;
            tooltipRect.sizeDelta = size;
        }

        Hide();
    }

    private void Update()
    {
        if (isWaitingToShow)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= showDelay)
            {
                ShowTooltip();
                isWaitingToShow = false;
            }
        }

        // Follow mouse if visible
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            PositionTooltip();
        }
    }

    /// <summary>
    /// Call when mouse enters a card.
    /// </summary>
    public void OnCardHoverEnter(Card card)
    {
        if (card == null) return;

        currentCard = card;
        hoverTimer = 0f;
        isWaitingToShow = true;

        // Populate data immediately (will show after delay)
        PopulateTooltip(card);
    }

    /// <summary>
    /// Call when mouse exits a card.
    /// </summary>
    public void OnCardHoverExit()
    {
        isWaitingToShow = false;
        currentCard = null;
        Hide();
    }

    private void ShowTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
            PositionTooltip();
        }
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void PopulateTooltip(Card card)
    {
        // Card Name - bold and larger style via rich text
        if (cardNameText != null)
            cardNameText.text = $"<b>{card.cardName}</b>";

        // Stats with icons/colors
        if (statsText != null)
            statsText.text = $"<color=#FF6B6B>ATK: {card.attack}</color>  |  <color=#6BCB77>HP: {card.health}</color>";

        // Tier with star indicator
        if (tierText != null)
        {
            string stars = new string('*', card.tier);
            tierText.text = $"<color=#FFD93D>Tier {card.tier}</color> {stars}";
        }

        // Tribes with colored badges
        if (tribesText != null)
        {
            string tribeStr = GetTribesString(card);
            if (string.IsNullOrEmpty(tribeStr))
                tribesText.text = "<color=#888888>No Tribe</color>";
            else
                tribesText.text = $"<color=#4ECDC4>{tribeStr}</color>";
        }

        // New Ability System
        bool hasAbility = card.abilityTrigger != AbilityTrigger.None;
        if (abilitySection != null)
            abilitySection.SetActive(hasAbility);

        if (hasAbility)
        {
            if (abilityTriggerText != null)
                abilityTriggerText.text = $"<size=90%>{GetTriggerName(card.abilityTrigger)}</size>";

            if (abilityEffectText != null)
                abilityEffectText.text = $"<i>{GetEffectDescription(card.abilityEffect, card.abilityValue)}</i>";

            if (abilityDescriptionText != null)
            {
                string desc = card.ability;
                if (!string.IsNullOrEmpty(desc))
                    abilityDescriptionText.text = $"<color=#CCCCCC><size=85%>\"{desc}\"</size></color>";
                else
                    abilityDescriptionText.text = "";
            }
        }

        // Legacy Effect System
        bool hasLegacyEffect = card.effectType != Card.EffectType.NoEffect;
        if (legacyEffectSection != null)
            legacyEffectSection.SetActive(hasLegacyEffect && !hasAbility);

        if (hasLegacyEffect && legacyEffectText != null)
        {
            legacyEffectText.text = GetLegacyEffectDescription(card);
        }
    }

    private string GetTribesString(Card card)
    {
        if (card.tribes == null || card.tribes.Length == 0)
        {
            // Fall back to legacy tribe
            return !string.IsNullOrEmpty(card.tribe) ? card.tribe : "";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var tribe in card.tribes)
        {
            if (tribe != TribeType.None)
            {
                if (sb.Length > 0) sb.Append(" / ");
                sb.Append(tribe.ToString());
            }
        }
        return sb.ToString();
    }

    private string GetTriggerName(AbilityTrigger trigger)
    {
        switch (trigger)
        {
            case AbilityTrigger.Battlecry: return "<color=#FFD700>BATTLECRY</color>";
            case AbilityTrigger.Deathrattle: return "<color=#8B4513>DEATHRATTLE</color>";
            case AbilityTrigger.OnAttack: return "<color=#FF4500>ON ATTACK</color>";
            case AbilityTrigger.OnDamaged: return "<color=#DC143C>ON DAMAGED</color>";
            case AbilityTrigger.StartOfCombat: return "<color=#4169E1>START OF COMBAT</color>";
            case AbilityTrigger.EndOfTurn: return "<color=#9370DB>END OF TURN</color>";
            // T406: Phase II triggers
            case AbilityTrigger.OnAllyDeath: return "<color=#696969>ON ALLY DEATH</color>";
            case AbilityTrigger.OnAllySummoned: return "<color=#32CD32>ON ALLY SUMMONED</color>";
            case AbilityTrigger.OnSell: return "<color=#DAA520>ON SELL</color>";
            case AbilityTrigger.Aura: return "<color=#00CED1>AURA</color>";
            default: return trigger.ToString();
        }
    }

    private string GetEffectDescription(Card.AbilityEffectType effect, int value)
    {
        switch (effect)
        {
            // Original effects
            case Card.AbilityEffectType.BuffAdjacentAttack: return $"Give adjacent minions +{value} Attack";
            case Card.AbilityEffectType.BuffAdjacentHealth: return $"Give adjacent minions +{value} Health";
            case Card.AbilityEffectType.BuffAdjacentStats: return $"Give adjacent minions +{value}/+{value}";
            case Card.AbilityEffectType.BuffAllFriendlyAttack: return $"Give all friendly minions +{value} Attack";
            case Card.AbilityEffectType.BuffOtherFriendlyAttack: return $"Give all other friendlies +{value} Attack";
            case Card.AbilityEffectType.GainAegis: return "Gain Aegis (block one attack)";
            case Card.AbilityEffectType.GainCoins: return $"Gain {value} gold";
            case Card.AbilityEffectType.DeathrattleBuffRandomFriendly: return $"Give a random friendly +{value}/+{value}";
            case Card.AbilityEffectType.DeathrattleDamageRandomEnemy: return $"Deal {value} damage to a random enemy";
            case Card.AbilityEffectType.DeathrattleDamageAllEnemies: return $"Deal {value} damage to all enemies";
            case Card.AbilityEffectType.OnAttackBuffSelf: return $"Gain +{value} Attack permanently";
            case Card.AbilityEffectType.OnAttackBonusDamage: return $"Deal +{value} extra damage";
            case Card.AbilityEffectType.OnAttackCleave: return $"Deal {value} damage to adjacent enemies";
            case Card.AbilityEffectType.Taunt: return "Must be attacked first";
            // T406: Phase II keyword abilities
            case Card.AbilityEffectType.Reborn: return "Returns with 1 Health after dying";
            case Card.AbilityEffectType.Windfury: return "Attacks twice each combat";
            case Card.AbilityEffectType.Venomous: return "Instantly destroys any minion it damages";
            case Card.AbilityEffectType.GainArmor: return $"Absorbs {value} damage before Health";
            // T406: Phase II trigger effects
            case Card.AbilityEffectType.OnAllyDeathBuffSelf: return $"Gain +{value}/+{value} when an ally dies";
            case Card.AbilityEffectType.OnAllyDeathBuffRandom: return $"Give a random ally +{value}/+{value} when an ally dies";
            case Card.AbilityEffectType.OnAllySummonedBuffSelf: return $"Gain +{value}/+{value} when an ally is summoned";
            case Card.AbilityEffectType.OnAllySummonedBuffSummoned: return $"Give summoned ally +{value}/+{value}";
            case Card.AbilityEffectType.OnSellGainCoins: return $"Gain {value} extra gold when sold";
            case Card.AbilityEffectType.OnSellBuffAllRemaining: return $"Give all remaining allies +{value}/+{value}";
            case Card.AbilityEffectType.AuraBuffTribematesAttack: return $"Tribemates have +{value} Attack";
            case Card.AbilityEffectType.AuraBuffAdjacentStats: return $"Adjacent allies have +{value}/+{value}";
            case Card.AbilityEffectType.AuraBuffAllFriendlyAttack: return $"All other allies have +{value} Attack";
            // T406: Phase II additional effects
            case Card.AbilityEffectType.SummonTokenOnDeath: return $"Summon a {value}/{value} token on death";
            case Card.AbilityEffectType.SummonTokenOnPlay: return $"Summon a {value}/{value} token when played";
            case Card.AbilityEffectType.StealBuffOnAttack: return $"Steal +{value}/+{value} from target";
            case Card.AbilityEffectType.BuffAllTribeOnPlay: return $"Give all tribemates +{value}/+{value}";
            case Card.AbilityEffectType.BuffAllTribeOnDeath: return $"Give all tribemates +{value}/+{value} on death";
            case Card.AbilityEffectType.RandomTransformOnDeath: return "Transform into a random card on death";
            case Card.AbilityEffectType.BuffSelfHealth: return $"Gain +{value} Health";
            default: return effect.ToString();
        }
    }

    private string GetLegacyEffectDescription(Card card)
    {
        switch (card.effectType)
        {
            case Card.EffectType.Guardian:
                return "<color=#4169E1>GUARDIAN</color>\nMust be attacked first";
            case Card.EffectType.Aegis:
                return "<color=#FFD700>AEGIS</color>\nBlocks one attack";
            case Card.EffectType.Echo:
                return $"<color=#9370DB>ECHO</color>\n{card.effectParameter}";
            case Card.EffectType.Summoning:
                return $"<color=#32CD32>SUMMONING</color>\n{card.effectParameter}";
            case Card.EffectType.LastReading:
                return $"<color=#FF69B4>LAST READING</color>\n{card.effectParameter}";
            default:
                return "";
        }
    }

    private void PositionTooltip()
    {
        if (tooltipRect == null) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 tooltipPos = mousePos + offset;

        // Keep tooltip on screen with proper edge padding
        Vector2 tooltipSize = tooltipRect.sizeDelta;

        // Right edge - flip to left side if needed
        if (tooltipPos.x + tooltipSize.x > Screen.width - edgePadding)
            tooltipPos.x = mousePos.x - tooltipSize.x - offset.x;

        // Left edge - ensure minimum padding
        if (tooltipPos.x < edgePadding)
            tooltipPos.x = edgePadding;

        // Top edge - ensure stays below top
        if (tooltipPos.y > Screen.height - edgePadding)
            tooltipPos.y = Screen.height - edgePadding;

        // Bottom edge - flip above cursor if needed
        if (tooltipPos.y - tooltipSize.y < edgePadding)
            tooltipPos.y = mousePos.y + tooltipSize.y + Mathf.Abs(offset.y);

        tooltipRect.position = tooltipPos;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
