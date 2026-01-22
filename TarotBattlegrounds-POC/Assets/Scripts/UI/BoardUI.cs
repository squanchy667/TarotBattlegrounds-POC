using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Board UI with event-driven refresh support
/// </summary>
public class BoardUI : MonoBehaviour
{
    [Header("Board Container")]
    [SerializeField] private Transform boardSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;
    
    [Header("Board Info")]
    [SerializeField] private TMP_Text boardCountText;
    
    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxBoardSlots = 7;
    
    private List<GameObject> currentBoardCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        // Initial refresh with delay to ensure everything is initialized
        // Note: GameUIManager handles event subscriptions and calls RefreshBoardDisplay()
        Invoke(nameof(RefreshBoardDisplay), 0.1f);
    }

    public void RefreshBoardDisplay()
    {
        ClearBoardDisplay();
        
        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;
        
        if (boardCountText != null)
            boardCountText.text = $"Board: {activePlayer.board.Count}/{maxBoardSlots}";
        
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
        }
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
        selectedCardIndex = index;
        Debug.Log($"Selected board card at index {index}");
        
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
