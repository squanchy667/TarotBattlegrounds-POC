using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shop UI with event-driven refresh support and theming.
/// </summary>
public class ShopUI : MonoBehaviour, IThemeable
{
    [Header("Shop Container")]
    [SerializeField] private Transform shopSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    [Header("Shop Info")]
    [SerializeField] private TMP_Text shopTitleText;
    [SerializeField] private TMP_Text shopTierText;
    [SerializeField] private TMP_Text shopCountText;

    [Header("Panel Visuals")]
    [SerializeField] private Image panelBackground;

    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxShopSlots = 6;

    private List<GameObject> currentShopCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;
    private ThemeConfig currentTheme;
    private Player subscribedPlayer;

    private void Start()
    {
        // Initial refresh with delay to ensure everything is initialized
        Invoke(nameof(RefreshShopDisplay), 0.1f);
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;
        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
        UnsubscribeFromPlayer();
    }

    private void SubscribeToPlayer(Player player)
    {
        if (player == subscribedPlayer) return;
        UnsubscribeFromPlayer();
        subscribedPlayer = player;
        if (subscribedPlayer != null)
            subscribedPlayer.OnShopFreezeChanged += OnShopFreezeChanged;
    }

    private void UnsubscribeFromPlayer()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnShopFreezeChanged -= OnShopFreezeChanged;
            subscribedPlayer = null;
        }
    }

    private void OnShopFreezeChanged(bool frozen)
    {
        foreach (var cardObj in currentShopCards)
        {
            if (cardObj == null) continue;
            var cardUI = cardObj.GetComponent<CardDisplayUI>();
            if (cardUI != null)
                cardUI.SetFrozen(frozen);
        }
    }

    /// <summary>
    /// Apply theme to the shop panel.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply shop title from theme
        if (shopTitleText != null)
            shopTitleText.text = theme.shopTitle;

        // Apply colors
        if (panelBackground != null)
            panelBackground.color = theme.secondaryColor;

        if (shopTierText != null)
            shopTierText.color = theme.textColorLight;
        if (shopCountText != null)
            shopCountText.color = theme.textColorLight;
        if (shopTitleText != null)
            shopTitleText.color = theme.primaryColor;
    }

    public void RefreshShopDisplay()
    {
        ClearShopDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        SubscribeToPlayer(activePlayer);

        // Get themed labels
        string shopLabel = currentTheme != null ? currentTheme.shopTitle : "Shop";
        string tierLabel = currentTheme != null ? currentTheme.tierLabel : "Tier";

        // Update tier display
        if (shopTierText != null)
            shopTierText.text = $"{shopLabel} ({tierLabel} {activePlayer.currentTavernTier})";

        if (TavernManager.Instance == null) return;
        if (!TavernManager.Instance.availableCards.ContainsKey(activePlayer.playerId)) return;

        List<Card> availableCards = TavernManager.Instance.availableCards[activePlayer.playerId];

        // Update count display
        if (shopCountText != null)
            shopCountText.text = $"{availableCards.Count} cards";

        // Create card displays
        for (int i = 0; i < availableCards.Count; i++)
        {
            CreateShopCard(availableCards[i], i);
        }

        // Apply frozen border if shop is frozen
        if (activePlayer.ShopFrozen)
        {
            foreach (var cardObj in currentShopCards)
            {
                var cardUI = cardObj.GetComponent<CardDisplayUI>();
                if (cardUI != null)
                    cardUI.SetFrozen(true);
            }
        }

        // Create empty slots to fill remaining space
        CreateEmptySlots(availableCards.Count);
    }
    
    private void CreateShopCard(Card card, int index)
    {
        if (cardDisplayPrefab == null || shopSlotsContainer == null) return;
        
        GameObject cardObj = Instantiate(cardDisplayPrefab, shopSlotsContainer);
        currentShopCards.Add(cardObj);
        
        CardDisplayUI cardUI = cardObj.GetComponent<CardDisplayUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnCardClicked);
            int displayCost = Mathf.Max(0, 3 + card.buyCostModifier);
            // Apply synergy cost reduction for display
            var player = GetActivePlayer();
            if (player != null && SynergyManager.Instance != null)
            {
                var snapshot = SynergyManager.Instance.CalculateSynergies(player.board);
                int reduction = SynergyManager.Instance.GetCostReduction(card, snapshot);
                if (reduction > 0)
                    displayCost = Mathf.Max(1, displayCost - reduction);
            }
            cardUI.SetCostVisible(true, displayCost);

            // Gray out if player can't afford
            if (player != null && player.coins < displayCost)
            {
                // Could add visual feedback for unaffordable cards
            }
        }
    }
    
    private void CreateEmptySlots(int filledSlots)
    {
        if (emptySlotPrefab == null) return;
        
        int emptyCount = maxShopSlots - filledSlots;
        for (int i = 0; i < emptyCount; i++)
        {
            GameObject slot = Instantiate(emptySlotPrefab, shopSlotsContainer);
            emptySlots.Add(slot);
        }
    }
    
    private void ClearShopDisplay()
    {
        foreach (GameObject card in currentShopCards)
        {
            if (card != null) Destroy(card);
        }
        currentShopCards.Clear();
        
        foreach (GameObject slot in emptySlots)
        {
            if (slot != null) Destroy(slot);
        }
        emptySlots.Clear();
        
        selectedCardIndex = -1;
    }
    
    private void OnCardClicked(int index)
    {
        selectedCardIndex = index;
        Debug.Log($"Selected shop card at index {index}");

        // Clear selections in other panels
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.OnCardSelectedInPanel("Shop");

        // Update selection visuals
        for (int i = 0; i < currentShopCards.Count; i++)
        {
            CardDisplayUI cardUI = currentShopCards[i].GetComponent<CardDisplayUI>();
            if (cardUI != null)
            {
                cardUI.SetSelected(i == selectedCardIndex);
            }
        }
        GameUIManager.Instance?.UpdateButtons();
    }
    
    public int GetSelectedCardIndex()
    {
        return selectedCardIndex;
    }
    
    public void ClearSelection()
    {
        selectedCardIndex = -1;
        foreach (var card in currentShopCards)
        {
            var cardUI = card.GetComponent<CardDisplayUI>();
            if (cardUI != null)
                cardUI.SetSelected(false);
        }
    }
    
    private Player GetActivePlayer()
    {
        if (GameUIManager.Instance == null) return null;
        return GameUIManager.Instance.GetActivePlayer();
    }
    
}
