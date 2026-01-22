using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hand UI with event-driven refresh support
/// </summary>
public class HandUI : MonoBehaviour
{
    [Header("Hand Container")]
    [SerializeField] private Transform handSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;
    
    [Header("Hand Info")]
    [SerializeField] private TMP_Text handCountText;
    
    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxHandSlots = 10;
    
    private List<GameObject> currentHandCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        // Initial refresh with delay to ensure everything is initialized
        // Note: GameUIManager handles event subscriptions and calls RefreshHandDisplay()
        Invoke(nameof(RefreshHandDisplay), 0.1f);
    }

    public void RefreshHandDisplay()
    {
        ClearHandDisplay();
        
        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;
        
        if (handCountText != null)
            handCountText.text = $"Hand: {activePlayer.hand.Count}/{maxHandSlots}";
        
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
