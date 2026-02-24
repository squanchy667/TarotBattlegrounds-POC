using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Board UI with event-driven refresh support and theming.
/// </summary>
public class BoardUI : MonoBehaviour, IThemeable
{
    [Header("Board Container")]
    [SerializeField] private Transform boardSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    [Header("Board Info")]
    [SerializeField] private TMP_Text boardTitleText;
    [SerializeField] private TMP_Text boardCountText;

    [Header("Panel Visuals")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private StyledPanel styledPanel;

    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxBoardSlots = 7;

    /// <summary>
    /// Fired when an empty board slot is clicked. Parameter is the slot position index.
    /// </summary>
    public event Action<int> OnEmptySlotSelected;

    /// <summary>
    /// Fired when a board card is clicked while another board card is already selected (swap request).
    /// Parameters: (indexA, indexB)
    /// </summary>
    public event Action<int, int> OnBoardSwapRequested;

    private List<GameObject> currentBoardCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;
    private ThemeConfig currentTheme;

    private void Start()
    {
        // Initial refresh with delay to ensure everything is initialized
        Invoke(nameof(RefreshBoardDisplay), 0.1f);
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
    /// Apply theme to the board panel.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply board title from theme
        if (boardTitleText != null)
            boardTitleText.text = theme.boardTitle;

        // Apply colors — let StyledPanel handle background if present
        if (styledPanel == null && panelBackground != null)
            panelBackground.color = theme.secondaryColor;

        if (boardCountText != null)
            boardCountText.color = theme.textColorLight;
        if (boardTitleText != null)
            boardTitleText.color = theme.primaryColor;
    }

    public void RefreshBoardDisplay()
    {
        ClearBoardDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        // Get themed label
        string boardLabel = currentTheme != null ? currentTheme.boardTitle : "Board";

        if (boardCountText != null)
            boardCountText.text = $"{boardLabel}: {activePlayer.board.Count}/{maxBoardSlots}";
        
        for (int i = 0; i < activePlayer.board.Count; i++)
        {
            CreateBoardCard(activePlayer.board[i], i);
        }
        
        CreateEmptySlots(activePlayer.board.Count);
    }
    
    private void CreateBoardCard(Card card, int index)
    {
        if (cardDisplayPrefab == null || boardSlotsContainer == null) return;
        
        GameObject cardObj = Instantiate(cardDisplayPrefab, boardSlotsContainer);
        currentBoardCards.Add(cardObj);
        
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

        int emptyCount = maxBoardSlots - filledSlots;
        for (int i = 0; i < emptyCount; i++)
        {
            GameObject slot = Instantiate(emptySlotPrefab, boardSlotsContainer);
            emptySlots.Add(slot);

            // Add click handler to empty slot
            int slotPosition = filledSlots + i;
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton == null)
                slotButton = slot.AddComponent<Button>();

            slotButton.onClick.AddListener(() => OnEmptySlotClicked(slotPosition));
        }
    }

    private void OnEmptySlotClicked(int slotPosition)
    {
        Debug.Log($"Empty board slot clicked at position {slotPosition}");
        OnEmptySlotSelected?.Invoke(slotPosition);
    }
    
    private void ClearBoardDisplay()
    {
        foreach (GameObject card in currentBoardCards)
        {
            if (card != null) Destroy(card);
        }
        currentBoardCards.Clear();
        
        foreach (GameObject slot in emptySlots)
        {
            if (slot != null) Destroy(slot);
        }
        emptySlots.Clear();
        
        selectedCardIndex = -1;
    }
    
    private void OnCardClicked(int index)
    {
        // If a board card is already selected and we click another board card → swap
        if (selectedCardIndex >= 0 && selectedCardIndex != index)
        {
            int prevIndex = selectedCardIndex;
            Debug.Log($"Board swap requested: {prevIndex} <-> {index}");
            OnBoardSwapRequested?.Invoke(prevIndex, index);
            selectedCardIndex = -1;

            // Clear visual selection
            for (int i = 0; i < currentBoardCards.Count; i++)
            {
                CardDisplayUI cardUI = currentBoardCards[i].GetComponent<CardDisplayUI>();
                if (cardUI != null)
                    cardUI.SetSelected(false);
            }
            return;
        }

        selectedCardIndex = index;
        Debug.Log($"Selected board card at index {index}");

        // Clear selections in other panels
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.OnCardSelectedInPanel("Board");

        for (int i = 0; i < currentBoardCards.Count; i++)
        {
            CardDisplayUI cardUI = currentBoardCards[i].GetComponent<CardDisplayUI>();
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
        if (selectedCardIndex < 0 || selectedCardIndex >= currentBoardCards.Count) return null;
        GameObject cardObj = currentBoardCards[selectedCardIndex];
        return cardObj != null ? cardObj.GetComponent<RectTransform>() : null;
    }

    /// <summary>
    /// UX15: Get the RectTransform of the board slot container for targeting animations.
    /// </summary>
    public RectTransform GetContainerRect()
    {
        return boardSlotsContainer != null ? boardSlotsContainer.GetComponent<RectTransform>() : null;
    }
    
    public void ClearSelection()
    {
        selectedCardIndex = -1;
        foreach (var card in currentBoardCards)
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
