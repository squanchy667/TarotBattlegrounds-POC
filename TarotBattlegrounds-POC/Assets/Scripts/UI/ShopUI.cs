using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("Shop Container")]
    [SerializeField] private Transform shopSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    [Header("Shop Info")]
    [SerializeField] private TMP_Text shopTierText;

    private List<GameObject> currentShopCards = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        Invoke(nameof(RefreshShopDisplay), 0.3f);
    }

    public void RefreshShopDisplay()
    {
        ClearShopDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        if (shopTierText != null)
            shopTierText.text = $"Shop (Tier {activePlayer.currentTavernTier})";

        if (TavernManager.Instance == null) return;
        if (!TavernManager.Instance.availableCards.ContainsKey(activePlayer.playerId)) return;

        List<Card> availableCards = TavernManager.Instance.availableCards[activePlayer.playerId];

        for (int i = 0; i < availableCards.Count; i++)
        {
            CreateShopCard(availableCards[i], i);
        }
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

    private Player GetActivePlayer()
    {
        if (GameUIManager.Instance == null) return null;
        return GameUIManager.Instance.GetActivePlayer();
    }
}