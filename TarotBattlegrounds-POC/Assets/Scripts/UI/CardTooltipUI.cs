using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TarotBattlegrounds.UI;

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
    [SerializeField] private Vector2 offset = new Vector2(Tokens.Space3, -Tokens.Space3);
    [SerializeField] private float showDelay = Tokens.LongPressTime;
    [SerializeField] private float tooltipWidth = 300f;
    [SerializeField] private float edgePadding = Tokens.Space2;

    [Header("T728 — Tap-and-hold / fixed anchor")]
    [Tooltip("When shown via tap-and-hold (touch), anchor the tooltip here instead of following the cursor. If null, defaults to screen top-center. showDelay doubles as the hold threshold.")]
    [SerializeField] private RectTransform fixedAnchor;

    private Card currentCard;
    private float hoverTimer;
    private bool isWaitingToShow;
    private RectTransform canvasRect;
    private bool _useFixedPosition; // T728: true when shown via tap-and-hold (fixed anchor, not cursor-follow)

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

        // Reposition while visible. Cursor-follow updates every frame; a fixed anchor is placed
        // once in ShowTooltip and stays put, so only re-follow when not in fixed-position mode.
        if (tooltipPanel != null && tooltipPanel.activeSelf && !_useFixedPosition)
        {
            PositionTooltip();
        }
    }

    /// <summary>
    /// Call when the mouse enters a card (desktop hover → cursor-following tooltip).
    /// </summary>
    public void OnCardHoverEnter(Card card)
    {
        BeginShow(card, useFixedPosition: false);
    }

    /// <summary>
    /// T728: Call when a card is pressed-and-held (touch). Shows the tooltip at a fixed anchor after
    /// the same showDelay, so a quick tap doesn't flash it and the finger doesn't cover the tooltip.
    /// </summary>
    public void OnCardPressStart(Card card)
    {
        BeginShow(card, useFixedPosition: true);
    }

    /// <summary>T728: Call when a press-and-hold is released.</summary>
    public void OnCardPressEnd()
    {
        OnCardHoverExit();
    }

    private void BeginShow(Card card, bool useFixedPosition)
    {
        if (card == null) return;

        currentCard = card;
        _useFixedPosition = useFixedPosition;
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
            statsText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>ATK: {card.attack}</color>  |  <color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>HP: {card.health}</color>";

        // Tier with star indicator
        if (tierText != null)
        {
            string stars = new string('*', card.tier);
            tierText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BronzeBright)}>Tier {card.tier}</color> {stars}";
        }

        // Tribes with colored badges
        if (tribesText != null)
        {
            string tribeStr = GetTribesString(card);
            if (string.IsNullOrEmpty(tribeStr))
                tribesText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BoneDim)}>No Tribe</color>";
            else
                tribesText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Bone)}>{tribeStr}</color>";
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
                    abilityDescriptionText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BoneDim)}><size=85%>\"{desc}\"</size></color>";
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
            case AbilityTrigger.Battlecry: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BronzeBright)}>BATTLECRY</color>";
            case AbilityTrigger.Deathrattle: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>DEATHRATTLE</color>";
            case AbilityTrigger.OnAttack: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>ON ATTACK</color>";
            case AbilityTrigger.OnDamaged: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>ON DAMAGED</color>";
            case AbilityTrigger.StartOfCombat: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>START OF COMBAT</color>";
            case AbilityTrigger.EndOfTurn: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>END OF TURN</color>";
            // T406: Phase II triggers
            case AbilityTrigger.OnAllyDeath: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BoneDim)}>ON ALLY DEATH</color>";
            case AbilityTrigger.OnAllySummoned: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>ON ALLY SUMMONED</color>";
            case AbilityTrigger.OnSell: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BronzeBright)}>ON SELL</color>";
            case AbilityTrigger.Aura: return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>AURA</color>";
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
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>GUARDIAN</color>\nMust be attacked first";
            case Card.EffectType.Aegis:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.BronzeBright)}>AEGIS</color>\nBlocks one attack";
            case Card.EffectType.Echo:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>ECHO</color>\n{card.effectParameter}";
            case Card.EffectType.Summoning:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>SUMMONING</color>\n{card.effectParameter}";
            case Card.EffectType.LastReading:
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>LAST READING</color>\n{card.effectParameter}";
            default:
                return "";
        }
    }

    private void PositionTooltip()
    {
        if (tooltipRect == null) return;

        Vector2 tooltipSizeFixed = tooltipRect.sizeDelta;

        // T728: fixed-position anchor (tap-and-hold on touch). A cursor-follow tooltip would sit
        // under the finger, so anchor to `fixedAnchor` if assigned, else screen top-center, clamped.
        if (_useFixedPosition)
        {
            Vector2 anchorPos = fixedAnchor != null
                ? (Vector2)fixedAnchor.position
                : new Vector2(Screen.width * 0.5f, Screen.height - edgePadding);

            float fx = Mathf.Clamp(anchorPos.x - tooltipSizeFixed.x * 0.5f, edgePadding, Mathf.Max(edgePadding, Screen.width - tooltipSizeFixed.x - edgePadding));
            float fy = Mathf.Clamp(anchorPos.y, tooltipSizeFixed.y + edgePadding, Screen.height - edgePadding);
            tooltipRect.position = new Vector2(fx, fy);
            return;
        }

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
