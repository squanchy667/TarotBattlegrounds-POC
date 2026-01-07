using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HandUI : MonoBehaviour
{
    [Header("Hand Container")]
    [SerializeField] private Transform handSlotsContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    [Header("Hand Info")]
    [SerializeField] private TMP_Text handCountText;

    private List<GameObject> currentHandCards = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        Invoke(nameof(RefreshHandDisplay), 0.3f);
    }

    public void RefreshHandDisplay()
    {
        ClearHandDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        if (handCountText != null)
            handCountText.text = $"Hand: {activePlayer.hand.Count}/10";

        for (int i = 0; i < activePlayer.hand.Count; i++)
        {
            CreateHandCard(activePlayer.hand[i], i);
        }
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
            cardUI.SetCostVisible(false); // No cost in hand
        }
    }

    private void ClearHandDisplay()
    {
        foreach (GameObject card in currentHandCards)
        {
            if (card != null) Destroy(card);
        }
        currentHandCards.Clear();
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