using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HandUI : MonoBehaviour
{
    [Header("Hand Container")]
    [SerializeField] private Transform handSlotsContainer;
    [SerializeField] private GameObject handCardPrefab;

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

        // Update hand count
        if (handCountText != null)
            handCountText.text = $"Hand: {activePlayer.hand.Count}/10";

        // Create card displays
        for (int i = 0; i < activePlayer.hand.Count; i++)
        {
            CreateHandCard(activePlayer.hand[i], i);
        }
    }

    private void CreateHandCard(Card card, int index)
    {
        if (handCardPrefab == null || handSlotsContainer == null) return;

        GameObject cardObj = Instantiate(handCardPrefab, handSlotsContainer);
        currentHandCards.Add(cardObj);

        HandCardUI cardUI = cardObj.GetComponent<HandCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnCardClicked);
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
            HandCardUI cardUI = currentHandCards[i].GetComponent<HandCardUI>();
            if (cardUI != null)
            {
                cardUI.SetSelected(i == selectedCardIndex);
            }
        }
    }

    public void PlaySelectedCard()
    {
        if (selectedCardIndex < 0)
        {
            Debug.Log("No hand card selected to play");
            return;
        }

        Player activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.PlayCard(selectedCardIndex, activePlayer.board.Count);
            RefreshHandDisplay();
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