using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TarotBattlegrounds.UI;

/// <summary>
/// Unified card display UI that supports theming.
/// Used for shop, hand, and board cards.
/// </summary>
public class CardDisplayUI : MonoBehaviour, IThemeable
{
    [Header("Card Info")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text tribeText;
    [SerializeField] private TMP_Text costText;

    [Header("Visuals")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardFrame;
    [SerializeField] private Image cardArtwork;
    [SerializeField] private Image selectionBorder;

    [Header("UX05: Tier Indicator")]
    [SerializeField] private Image tierIndicatorImage;

    [Header("UX06: Layout - Stat Badges")]
    [SerializeField] private Image attackBadge;        // Red circle behind ATK number
    [SerializeField] private Image healthBadge;        // Green circle behind HP number
    [SerializeField] private Image costBadge;          // Blue/gold circle behind cost

    [Header("UX06: Layout - Sections")]
    [SerializeField] private RectTransform nameBanner;     // Top strip for name
    [SerializeField] private RectTransform artworkArea;    // Center area
    [SerializeField] private RectTransform abilityArea;    // Below artwork
    [SerializeField] private RectTransform statBar;        // Bottom bar

    [Header("UX06: Typography")]
    [SerializeField] private float nameSize = 16f;
    [SerializeField] private float statSize = 22f;
    [SerializeField] private float costSize = 18f;
    [SerializeField] private float abilitySize = 11f;
    [SerializeField] private TMP_Text abilityText;

    [Header("UX06: Stat Badge Components")]
    [SerializeField] private StatBadge attackStatBadge;
    [SerializeField] private StatBadge healthStatBadge;
    [SerializeField] private StatBadge costStatBadge;

    // UX06: Badge color constants
    private static readonly Color attackBadgeColor = new Color(0.7f, 0.15f, 0.15f, 1f);
    private static readonly Color healthBadgeColor = new Color(0.15f, 0.55f, 0.15f, 1f);
    private static readonly Color costBadgeColor = new Color(0.2f, 0.4f, 0.8f, 1f);

    [Header("Selection Colors (from theme if available)")]
    [SerializeField] private Color normalColor = new Color(0.10f, 0.07f, 0.16f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.55f, 0.3f, 0.75f, 1f);

    private static readonly Color frozenBorderColor = new Color(0.3f, 0.55f, 1.0f, 1f);

    private Card card;
    private int index;
    private Action<int> onClickCallback;
    private Button cardButton;
    private ThemeConfig currentTheme;
    private bool isFrozen;
    private bool isSelected;
    private CardFrameGenerator frameGenerator;

    // UX07: Golden card effects component (shimmer, pulse, sparkles)
    private GoldenCardEffect goldenCardEffect;

    // UX08: Card interaction feedback component (hover, selection glow, frozen shimmer)
    private CardInteractionFeedback interactionFeedback;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClicked);

        // UX05: Cache CardFrameGenerator for layered frame delegation
        frameGenerator = GetComponent<CardFrameGenerator>();

        // UX07: Cache GoldenCardEffect for golden card visuals
        goldenCardEffect = GetComponent<GoldenCardEffect>();

        // UX08: Cache CardInteractionFeedback for hover/selection/frozen effects
        interactionFeedback = GetComponent<CardInteractionFeedback>();
    }

    private void OnEnable()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += ApplyTheme;

        // Apply current theme if available
        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply theme colors and visuals to this card display.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply card background color (golden cards get golden tint)
        if (cardBackground != null)
        {
            if (card != null && card.isGolden)
                normalColor = theme.goldenCardColor;
            else
                normalColor = theme.cardBackgroundColor;
        }

        // Apply selection color from theme
        selectedColor = theme.accentColor;

        // Apply card frame based on tier (if card is set)
        if (card != null)
        {
            ApplyCardFrame(theme, card.tier);
            ApplyTribeColor(theme, card);
        }

        // Update text colors
        if (cardNameText != null)
        {
            // UX06: Golden cards get gold name color, otherwise white
            if (card != null && card.isGolden)
                cardNameText.color = theme.accentColor;
            else
                cardNameText.color = theme.textColorLight;
        }
        if (tierText != null)
            tierText.color = theme.textColorLight;
        if (costText != null)
            costText.color = theme.accentColor;
        if (attackText != null)
            attackText.color = theme.negativeColor;
        if (healthText != null)
            healthText.color = theme.positiveColor;

        // UX06: Apply ability text color (muted light gray)
        if (abilityText != null)
            abilityText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // UX06: Apply name banner background if present
        if (nameBanner != null)
        {
            Image bannerImage = nameBanner.GetComponent<Image>();
            if (bannerImage != null)
                bannerImage.color = new Color(0f, 0f, 0f, 0.5f);
        }

        // Re-apply visual state to update colors
        SetSelected(isSelected);
    }

    private void ApplyCardFrame(ThemeConfig theme, int tier)
    {
        // UX05: Delegate to CardFrameGenerator for full layered frame visuals
        if (frameGenerator != null && card != null)
        {
            frameGenerator.ApplyCardVisuals(card);
        }

        if (cardFrame == null || theme == null) return;

        Sprite frame = theme.GetCardFrame(tier);
        if (frame != null)
            cardFrame.sprite = frame;

        // UX05: Update standalone tier indicator if present (for cards without CardFrameGenerator)
        ApplyTierIndicatorDisplay(tier);
    }

    /// <summary>
    /// UX05: Apply tier indicator gems display.
    /// Only used when tierIndicatorImage is set but CardFrameGenerator is not present
    /// (CardFrameGenerator handles its own tier indicator internally).
    /// </summary>
    private void ApplyTierIndicatorDisplay(int tier)
    {
        if (tierIndicatorImage == null) return;
        if (frameGenerator != null) return; // CardFrameGenerator handles this

        Texture2D gemTex = CardFrameGenerator.GenerateTierGemsStatic(tier);
        if (gemTex != null)
        {
            Sprite gemSprite = Sprite.Create(
                gemTex,
                new Rect(0, 0, gemTex.width, gemTex.height),
                new Vector2(0.5f, 0.5f)
            );
            tierIndicatorImage.sprite = gemSprite;
            tierIndicatorImage.color = Color.white;
            tierIndicatorImage.gameObject.SetActive(true);
        }
    }

    private void ApplyTribeColor(ThemeConfig theme, Card cardData)
    {
        if (tribeText == null || theme == null || cardData == null) return;

        // Get the primary tribe's color
        TribeType primaryTribe = cardData.GetPrimaryTribe();
        if (primaryTribe != TribeType.None)
        {
            tribeText.color = theme.GetTribeColor(primaryTribe);
        }
        else
        {
            tribeText.color = theme.textColorLight;
        }
    }

    [Header("Minimum Font Sizes")]
    [SerializeField] private float minNameFontSize = 14f;
    [SerializeField] private float minStatsFontSize = 18f;
    [SerializeField] private float minTierFontSize = 12f;
    [SerializeField] private float minTribeFontSize = 11f;
    [SerializeField] private float minCostFontSize = 14f;

    private void EnforceMinFontSizes()
    {
        if (cardNameText != null && cardNameText.fontSize < minNameFontSize)
            cardNameText.fontSize = minNameFontSize;
        if (attackText != null && attackText.fontSize < minStatsFontSize)
            attackText.fontSize = minStatsFontSize;
        if (healthText != null && healthText.fontSize < minStatsFontSize)
            healthText.fontSize = minStatsFontSize;
        if (tierText != null && tierText.fontSize < minTierFontSize)
            tierText.fontSize = minTierFontSize;
        if (tribeText != null && tribeText.fontSize < minTribeFontSize)
            tribeText.fontSize = minTribeFontSize;
        if (costText != null && costText.fontSize < minCostFontSize)
            costText.fontSize = minCostFontSize;

        // UX06: Apply typography sizes if new layout fields are present
        ApplyTypographySizes();
    }

    /// <summary>
    /// UX06: Apply card-game-style typography sizes to text elements.
    /// Only applies when new layout sections are wired up; graceful no-op otherwise.
    /// </summary>
    private void ApplyTypographySizes()
    {
        // Only apply UX06 typography if layout sections are wired
        if (nameBanner == null && statBar == null) return;

        if (cardNameText != null)
        {
            cardNameText.fontSize = Mathf.Max(nameSize, minNameFontSize);
            cardNameText.fontStyle = TMPro.FontStyles.Bold;
            cardNameText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        if (attackText != null)
        {
            attackText.fontSize = Mathf.Max(statSize, minStatsFontSize);
            attackText.fontStyle = TMPro.FontStyles.Bold;
            attackText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        if (healthText != null)
        {
            healthText.fontSize = Mathf.Max(statSize, minStatsFontSize);
            healthText.fontStyle = TMPro.FontStyles.Bold;
            healthText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        if (costText != null)
        {
            costText.fontSize = Mathf.Max(costSize, minCostFontSize);
            costText.fontStyle = TMPro.FontStyles.Bold;
            costText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        if (abilityText != null)
        {
            abilityText.fontSize = abilitySize;
            abilityText.fontStyle = TMPro.FontStyles.Italic;
            abilityText.alignment = TMPro.TextAlignmentOptions.Center;
            abilityText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        if (tribeText != null)
        {
            tribeText.fontSize = Mathf.Max(12f, minTribeFontSize);
            tribeText.fontStyle = TMPro.FontStyles.Bold;
            tribeText.alignment = TMPro.TextAlignmentOptions.Center;
        }
    }

    public void Setup(Card cardData, int cardIndex, Action<int> onClick)
    {
        card = cardData;
        index = cardIndex;
        onClickCallback = onClick;

        // Enforce minimum font sizes for readability
        EnforceMinFontSizes();

        // Display card data (golden cards get a star prefix)
        if (cardNameText != null)
            cardNameText.text = card.isGolden ? $"* {card.cardName} *" : card.cardName;
        if (attackText != null) attackText.text = card.attack.ToString();
        if (healthText != null) healthText.text = card.health.ToString();
        if (tierText != null) tierText.text = $"T{card.tier}";

        // Display tribe using theme-aware name
        // UX06: Show tribe in all-caps when stat bar is wired
        if (tribeText != null)
        {
            TribeType primaryTribe = card.GetPrimaryTribe();
            string tribeName;
            if (primaryTribe != TribeType.None && ThemeManager.ActiveTheme != null)
            {
                tribeName = ThemeManager.GetTribeName(primaryTribe);
            }
            else
            {
                tribeName = card.tribe;
            }

            // UX06: All-caps tribe name when layout is active
            if (statBar != null && !string.IsNullOrEmpty(tribeName))
                tribeText.text = tribeName.ToUpper();
            else
                tribeText.text = tribeName;
        }

        // Display artwork
        if (cardArtwork != null)
        {
            if (card.cardImage != null)
            {
                cardArtwork.sprite = card.cardImage;
                cardArtwork.color = Color.white;
            }
            else
            {
                cardArtwork.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Gray placeholder
            }
        }

        // UX06: Update stat badges with buff/damage indicators
        UpdateStatBadges();

        // UX06: Display ability text
        UpdateAbilityText();

        // Apply theme now that we have card data
        if (currentTheme != null)
        {
            ApplyCardFrame(currentTheme, card.tier);
            ApplyTribeColor(currentTheme, card);
        }
        else if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }

        // UX07: Enable/disable golden card effects based on card state
        if (goldenCardEffect != null)
        {
            goldenCardEffect.SetActive(card.isGolden);
        }

        SetSelected(false);
    }

    public void SetCostVisible(bool visible, int cost = 3)
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(visible);
            if (visible) costText.text = $"{cost}g";
        }

        // UX06: Show/hide cost badge and update via StatBadge
        if (costBadge != null)
            costBadge.gameObject.SetActive(visible);
        if (costStatBadge != null)
        {
            costStatBadge.gameObject.SetActive(visible);
            if (visible)
            {
                costStatBadge.SetBadgeColor(costBadgeColor);
                costStatBadge.SetValue(cost);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // UX08: Delegate to CardInteractionFeedback if present
        if (interactionFeedback != null)
        {
            interactionFeedback.SetSelected(selected);
        }

        // Backward compat: still update border and background color
        UpdateBorderVisual();

        if (cardBackground != null)
            cardBackground.color = selected ? selectedColor : normalColor;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        // UX08: Delegate to CardInteractionFeedback if present
        if (interactionFeedback != null)
        {
            interactionFeedback.SetFrozen(frozen);
        }

        // Backward compat: still update border visual
        UpdateBorderVisual();
    }

    private void UpdateBorderVisual()
    {
        if (selectionBorder == null) return;

        bool showBorder = isSelected || isFrozen;
        selectionBorder.gameObject.SetActive(showBorder);
        if (showBorder)
            selectionBorder.color = isSelected ? selectedColor : frozenBorderColor;
    }

    public void UpdateStats(int attack, int health)
    {
        if (attackText != null) attackText.text = attack.ToString();
        if (healthText != null) healthText.text = health.ToString();

        // UX06: Update stat badges with buff/damage coloring
        if (card != null)
        {
            if (attackStatBadge != null)
            {
                attackStatBadge.SetBadgeColor(attackBadgeColor);
                attackStatBadge.SetValue(attack, card.BaseAttack);
            }
            if (healthStatBadge != null)
            {
                healthStatBadge.SetBadgeColor(healthBadgeColor);
                healthStatBadge.SetValue(health, card.BaseHealth);
            }
        }
    }

    /// <summary>
    /// UX06: Update stat badge components with current card stats and buff/damage coloring.
    /// Gracefully falls back if stat badge fields are null.
    /// </summary>
    private void UpdateStatBadges()
    {
        if (card == null) return;

        if (attackStatBadge != null)
        {
            attackStatBadge.SetBadgeColor(attackBadgeColor);
            attackStatBadge.SetValue(card.attack, card.BaseAttack);
        }

        if (healthStatBadge != null)
        {
            healthStatBadge.SetBadgeColor(healthBadgeColor);
            healthStatBadge.SetValue(card.health, card.BaseHealth);
        }

        // Cost badge is handled by SetCostVisible, but set color here for consistency
        if (costStatBadge != null)
        {
            costStatBadge.SetBadgeColor(costBadgeColor);
        }
    }

    /// <summary>
    /// UX06: Display ability description text below artwork area.
    /// Shows first ability's trigger type + short description.
    /// Truncates with "..." if too long. Graceful no-op if abilityText is null.
    /// </summary>
    private void UpdateAbilityText()
    {
        if (abilityText == null || card == null) return;

        string abilityDesc = GetAbilityDescription(card);
        if (!string.IsNullOrEmpty(abilityDesc))
        {
            // Truncate if too long (max ~40 chars for small card area)
            const int maxLength = 40;
            if (abilityDesc.Length > maxLength)
                abilityDesc = abilityDesc.Substring(0, maxLength - 3) + "...";

            abilityText.text = abilityDesc;
            abilityText.gameObject.SetActive(true);
        }
        else
        {
            abilityText.text = "";
            abilityText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// UX06: Build a short ability description string from card data.
    /// Format: "{TriggerType}: {Description}"
    /// </summary>
    private static string GetAbilityDescription(Card card)
    {
        if (card.abilityTrigger == AbilityTrigger.None && card.abilityEffect == Card.AbilityEffectType.None)
        {
            // Check legacy ability string
            if (!string.IsNullOrEmpty(card.ability))
                return card.ability;
            return null;
        }

        string trigger = FormatTriggerName(card.abilityTrigger);
        string effect = FormatEffectDescription(card.abilityEffect, card.abilityValue);

        if (string.IsNullOrEmpty(effect)) return null;

        if (!string.IsNullOrEmpty(trigger))
            return $"{trigger}: {effect}";
        return effect;
    }

    /// <summary>
    /// UX06: Format trigger name for display (e.g., "Battlecry", "Deathrattle").
    /// </summary>
    private static string FormatTriggerName(AbilityTrigger trigger)
    {
        switch (trigger)
        {
            case AbilityTrigger.Battlecry:     return "Battlecry";
            case AbilityTrigger.Deathrattle:   return "Deathrattle";
            case AbilityTrigger.OnAttack:      return "On Attack";
            case AbilityTrigger.OnDamaged:     return "On Damaged";
            case AbilityTrigger.StartOfCombat: return "Start of Combat";
            case AbilityTrigger.EndOfTurn:     return "End of Turn";
            case AbilityTrigger.OnAllyDeath:   return "Ally Death";
            case AbilityTrigger.OnAllySummoned: return "Ally Summoned";
            case AbilityTrigger.OnSell:        return "On Sell";
            case AbilityTrigger.Aura:          return "Aura";
            default:                           return "";
        }
    }

    /// <summary>
    /// UX06: Format effect type into a short human-readable description.
    /// </summary>
    private static string FormatEffectDescription(Card.AbilityEffectType effect, int value)
    {
        switch (effect)
        {
            case Card.AbilityEffectType.BuffAdjacentAttack:       return $"+{value} ATK adjacent";
            case Card.AbilityEffectType.BuffAdjacentHealth:       return $"+{value} HP adjacent";
            case Card.AbilityEffectType.BuffAdjacentStats:        return $"+{value}/+{value} adjacent";
            case Card.AbilityEffectType.BuffAllFriendlyAttack:    return $"+{value} ATK all allies";
            case Card.AbilityEffectType.BuffOtherFriendlyAttack:  return $"+{value} ATK others";
            case Card.AbilityEffectType.GainAegis:                return "Gain Aegis";
            case Card.AbilityEffectType.GainCoins:                return $"Gain {value} gold";
            case Card.AbilityEffectType.DeathrattleBuffRandomFriendly: return $"+{value}/+{value} random ally";
            case Card.AbilityEffectType.DeathrattleDamageRandomEnemy:  return $"Deal {value} to random enemy";
            case Card.AbilityEffectType.DeathrattleDamageAllEnemies:   return $"Deal {value} to all enemies";
            case Card.AbilityEffectType.OnAttackBuffSelf:         return $"Gain +{value} ATK";
            case Card.AbilityEffectType.OnAttackBonusDamage:      return $"+{value} bonus damage";
            case Card.AbilityEffectType.OnAttackCleave:           return "Cleave";
            case Card.AbilityEffectType.Taunt:                    return "Taunt";
            case Card.AbilityEffectType.OnAllyDeathBuffSelf:      return $"Gain +{value}/+{value}";
            case Card.AbilityEffectType.OnAllyDeathBuffRandom:    return $"+{value}/+{value} random ally";
            case Card.AbilityEffectType.OnAllySummonedBuffSummoned: return $"+{value}/+{value} summoned";
            case Card.AbilityEffectType.OnAllySummonedBuffSelf:   return $"+{value} ATK per summon";
            case Card.AbilityEffectType.OnSellGainCoins:          return $"+{value} gold";
            case Card.AbilityEffectType.OnSellBuffAllRemaining:   return $"+{value}/+{value} all remaining";
            case Card.AbilityEffectType.AuraBuffTribematesAttack: return $"+{value} ATK tribemates";
            case Card.AbilityEffectType.AuraBuffAdjacentStats:    return $"+{value}/+{value} adjacent";
            case Card.AbilityEffectType.AuraBuffAllFriendlyAttack: return $"+{value} ATK allies";
            case Card.AbilityEffectType.Reborn:                   return "Reborn";
            case Card.AbilityEffectType.Windfury:                 return "Windfury";
            case Card.AbilityEffectType.Venomous:                 return "Venomous";
            case Card.AbilityEffectType.SummonTokenOnDeath:       return $"Summon {value}/{value} token";
            case Card.AbilityEffectType.SummonTokenOnPlay:        return $"Summon {value}/{value} token";
            case Card.AbilityEffectType.StealBuffOnAttack:        return $"Steal +{value}/+{value}";
            case Card.AbilityEffectType.GainArmor:                return $"Armor {value}";
            case Card.AbilityEffectType.BuffAllTribeOnPlay:       return $"+{value}/+{value} tribe";
            case Card.AbilityEffectType.BuffAllTribeOnDeath:      return $"+{value}/+{value} tribe";
            case Card.AbilityEffectType.RandomTransformOnDeath:   return "Transform on death";
            case Card.AbilityEffectType.BuffSelfHealth:           return $"+{value} HP";
            default:                                              return "";
        }
    }

    private void OnCardClicked()
    {
        onClickCallback?.Invoke(index);
    }

    public Card GetCard() => card;
    public int GetIndex() => index;

    private void OnDestroy()
    {
        if (cardButton != null)
            cardButton.onClick.RemoveAllListeners();
    }
}