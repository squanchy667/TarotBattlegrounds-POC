using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    [Header("Selection Colors (from theme if available)")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.5f, 0.3f, 1f);

    private static readonly Color frozenBorderColor = new Color(0.3f, 0.6f, 1f, 1f);

    private Card card;
    private int index;
    private Action<int> onClickCallback;
    private Button cardButton;
    private ThemeConfig currentTheme;
    private bool isFrozen;
    private bool isSelected;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClicked);
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
            cardNameText.color = theme.textColorLight;
        if (tierText != null)
            tierText.color = theme.textColorLight;
        if (costText != null)
            costText.color = theme.accentColor;
        if (attackText != null)
            attackText.color = theme.negativeColor;
        if (healthText != null)
            healthText.color = theme.positiveColor;

        // Re-apply visual state to update colors
        SetSelected(isSelected);
    }

    private void ApplyCardFrame(ThemeConfig theme, int tier)
    {
        if (cardFrame == null || theme == null) return;

        Sprite frame = theme.GetCardFrame(tier);
        if (frame != null)
            cardFrame.sprite = frame;
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
        if (tribeText != null)
        {
            TribeType primaryTribe = card.GetPrimaryTribe();
            if (primaryTribe != TribeType.None && ThemeManager.ActiveTheme != null)
            {
                tribeText.text = ThemeManager.GetTribeName(primaryTribe);
            }
            else
            {
                tribeText.text = card.tribe;
            }
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

        SetSelected(false);
    }

    public void SetCostVisible(bool visible, int cost = 3)
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(visible);
            if (visible) costText.text = $"{cost}g";
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBorderVisual();

        if (cardBackground != null)
            cardBackground.color = selected ? selectedColor : normalColor;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
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