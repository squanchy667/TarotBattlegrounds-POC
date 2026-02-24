using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hand UI with event-driven refresh support and theming.
/// </summary>
public class HandUI : MonoBehaviour, IThemeable
{
    [Header("Hand Container")]
    [SerializeField] private Transform handSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    [Header("Hand Info")]
    [SerializeField] private TMP_Text handTitleText;
    [SerializeField] private TMP_Text handCountText;

    [Header("Panel Visuals")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private StyledPanel styledPanel;

    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxHandSlots = 10;

    private List<GameObject> currentHandCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;
    private ThemeConfig currentTheme;

    private void Start()
    {
        // Initial refresh with delay to ensure everything is initialized
        Invoke(nameof(RefreshHandDisplay), 0.1f);
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
    }

    /// <summary>
    /// Apply theme to the hand panel.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply hand title from theme
        if (handTitleText != null)
            handTitleText.text = theme.handTitle;

        // Apply colors — let StyledPanel handle background if present
        if (styledPanel == null && panelBackground != null)
            panelBackground.color = theme.secondaryColor;

        if (handCountText != null)
            handCountText.color = theme.textColorLight;
        if (handTitleText != null)
            handTitleText.color = theme.primaryColor;
    }

    public void RefreshHandDisplay()
    {
        ClearHandDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        // Get themed label
        string handLabel = currentTheme != null ? currentTheme.handTitle : "Hand";

        if (handCountText != null)
            handCountText.text = $"{handLabel}: {activePlayer.hand.Count}/{maxHandSlots}";
        
        for (int i = 0; i < activePlayer.hand.Count; i++)
        {
            CreateHandCard(activePlayer.hand[i], i);
        }
        
        CreateEmptySlots(activePlayer.hand.Count);
    }
    
    private void CreateHandCard(Card card, int index)
    {
        if (cardDisplayPrefab == null || handSlotsContainer == null) return;
        
        GameObject cardObj = Instantiate(cardDisplayPrefab, handSlotsContainer);
        currentHandCards.Add(cardObj);
        
        CardDisplayUI cardUI = cardObj.GetComponent<CardDisplayUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnCardClicked);
            cardUI.SetCostVisible(false);
        }
    }
    
    private void CreateEmptySlots(int filledSlots)
    {
        if (emptySlotPrefab == null) return;
        
        int emptyCount = Mathf.Min(3, maxHandSlots - filledSlots); // Show max 3 empty
        for (int i = 0; i < emptyCount; i++)
        {
            GameObject slot = Instantiate(emptySlotPrefab, handSlotsContainer);
            emptySlots.Add(slot);
        }
    }
    
    private void ClearHandDisplay()
    {
        foreach (GameObject card in currentHandCards)
        {
            if (card != null) Destroy(card);
        }
        currentHandCards.Clear();
        
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
        Debug.Log($"Selected hand card at index {index}");

        // Clear selections in other panels
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.OnCardSelectedInPanel("Hand");

        for (int i = 0; i < currentHandCards.Count; i++)
        {
            CardDisplayUI cardUI = currentHandCards[i].GetComponent<CardDisplayUI>();
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

    /// <summary>
    /// UX15: Get the RectTransform of the selected card for animation purposes.
    /// </summary>
    public RectTransform GetSelectedCardRect()
    {
        if (selectedCardIndex < 0 || selectedCardIndex >= currentHandCards.Count) return null;
        GameObject cardObj = currentHandCards[selectedCardIndex];
        return cardObj != null ? cardObj.GetComponent<RectTransform>() : null;
    }

    /// <summary>
    /// UX15: Get the RectTransform of the hand slot container for targeting animations.
    /// </summary>
    public RectTransform GetContainerRect()
    {
        return handSlotsContainer != null ? handSlotsContainer.GetComponent<RectTransform>() : null;
    }
    
    public void ClearSelection()
    {
        selectedCardIndex = -1;
        foreach (var card in currentHandCards)
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
