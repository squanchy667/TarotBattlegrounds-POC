using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shop UI with event-driven refresh support
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Shop Container")]
    [SerializeField] private Transform shopSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;
    
    [Header("Shop Info")]
    [SerializeField] private TMP_Text shopTierText;
    [SerializeField] private TMP_Text shopCountText;
    
    [Header("Empty Slot Display")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int maxShopSlots = 6;
    
    private List<GameObject> currentShopCards = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private int selectedCardIndex = -1;
    
    private Player cachedPlayer;
    
    private void Start()
    {
        // Subscribe to player events when available
        SubscribeToPlayerEvents();
        
        // Initial refresh with delay to ensure everything is initialized
        Invoke(nameof(RefreshShopDisplay), 0.1f);
    }
    
    private void SubscribeToPlayerEvents()
    {
        var player = GetActivePlayer();
        if (player != null && player != cachedPlayer)
        {
            // Unsubscribe from old player
            if (cachedPlayer != null)
            {
                cachedPlayer.OnShopRefreshed -= RefreshShopDisplay;
                cachedPlayer.OnTierChanged -= RefreshShopDisplay;
            }
            
            // Subscribe to new player
            player.OnShopRefreshed += RefreshShopDisplay;
            player.OnTierChanged += RefreshShopDisplay;
            cachedPlayer = player;
        }
    }
    
    public void RefreshShopDisplay()
    {
        // Update subscription if player changed
        SubscribeToPlayerEvents();
        
        ClearShopDisplay();
        
        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;
        
        // Update tier display
        if (shopTierText != null)
            shopTierText.text = $"Shop (Tier {activePlayer.currentTavernTier})";
        
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
            cardUI.SetCostVisible(true, 3 + card.buyCostModifier);
            
            // Gray out if player can't afford
            var player = GetActivePlayer();
            if (player != null && player.coins < 3 + card.buyCostModifier)
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
        
        // Update selection visuals
        for (int i = 0; i < currentShopCards.Count; i++)
        {
            CardDisplayUI cardUI = currentShopCards[i].GetComponent<CardDisplayUI>();
            if (cardUI != null)
            {
                cardUI.SetSelected(i == selectedCardIndex);
            }
        }
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
    
    private void OnDestroy()
    {
        if (cachedPlayer != null)
        {
            cachedPlayer.OnShopRefreshed -= RefreshShopDisplay;
            cachedPlayer.OnTierChanged -= RefreshShopDisplay;
        }
    }
}
