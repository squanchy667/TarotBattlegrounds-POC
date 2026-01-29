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

    private Card card;
    private int index;
    private Action<int> onClickCallback;
    private Button cardButton;
    private ThemeConfig currentTheme;

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

        // Re-apply selection state to update colors
        SetSelected(selectionBorder != null && selectionBorder.gameObject.activeSelf);
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

    public void Setup(Card cardData, int cardIndex, Action<int> onClick)
    {
        card = cardData;
        index = cardIndex;
        onClickCallback = onClick;

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
        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);

        if (cardBackground != null)
            cardBackground.color = selected ? selectedColor : normalColor;
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