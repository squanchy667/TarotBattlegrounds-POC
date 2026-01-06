using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("Shop Container")]
    [SerializeField] private Transform shopSlotsContainer;
    [SerializeField] private GameObject shopCardPrefab;

    [Header("Shop Info")]
    [SerializeField] private TMP_Text shopTierText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollCostText;

    private List<GameObject> currentShopCards = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        rerollButton?.onClick.AddListener(OnRerollClicked);
        Invoke(nameof(RefreshShopDisplay), 0.3f);
    }

    // private void Update()
    // {
    //     RefreshShopDisplay();
    // }

    public void RefreshShopDisplay()
    {
        ClearShopDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        // Update shop tier text
        if (shopTierText != null)
            shopTierText.text = $"Shop (Tier {activePlayer.currentTavernTier})";

        // Update reroll cost
        if (rerollCostText != null)
            rerollCostText.text = "1g";

        // Get available cards for this player
        if (TavernManager.Instance == null) return;
        if (!TavernManager.Instance.availableCards.ContainsKey(activePlayer.playerId)) return;

        List<Card> availableCards = TavernManager.Instance.availableCards[activePlayer.playerId];

        // Create card displays
        for (int i = 0; i < availableCards.Count; i++)
        {
            CreateShopCard(availableCards[i], i);
        }
    }

    private void CreateShopCard(Card card, int index)
    {
        if (shopCardPrefab == null || shopSlotsContainer == null) return;

        GameObject cardObj = Instantiate(shopCardPrefab, shopSlotsContainer);
        currentShopCards.Add(cardObj);

        // Setup card display
        ShopCardUI cardUI = cardObj.GetComponent<ShopCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnCardClicked);
        }
    }

    private void ClearShopDisplay()
    {
        foreach (GameObject card in currentShopCards)
        {
            if (card != null) Destroy(card);
        }
        currentShopCards.Clear();
        selectedCardIndex = -1;
    }

    private void OnCardClicked(int index)
    {
        selectedCardIndex = index;
        Debug.Log($"Selected shop card at index {index}");
        
        // Highlight selected card
        for (int i = 0; i < currentShopCards.Count; i++)
        {
            ShopCardUI cardUI = currentShopCards[i].GetComponent<ShopCardUI>();
            if (cardUI != null)
            {
                cardUI.SetSelected(i == selectedCardIndex);
            }
        }
    }

    private void OnRerollClicked()
    {
        Player activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.RefreshTavernShop();
            RefreshShopDisplay();
        }
    }

    public void BuySelectedCard()
    {
        if (selectedCardIndex < 0) 
        {
            Debug.Log("No card selected to buy");
            return;
        }

        Player activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.BuyCard(selectedCardIndex);
            RefreshShopDisplay();
        }
    }

    public int GetSelectedCardIndex()
    {
        return selectedCardIndex;
    }

    private Player GetActivePlayer()
    {
        if (GameUIManager.Instance == null) return null;
        return GameUIManager.Instance.GetActivePlayer();
    }

    private void OnDestroy()
    {
        rerollButton?.onClick.RemoveAllListeners();
    }
}