using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BoardUI : MonoBehaviour
{
    [Header("Board Container")]
    [SerializeField] private Transform boardSlotsContainer;
    [SerializeField] private GameObject boardCardPrefab;

    [Header("Board Info")]
    [SerializeField] private TMP_Text boardCountText;

    private List<GameObject> currentBoardCards = new List<GameObject>();
    private int selectedCardIndex = -1;

    private void Start()
    {
        Invoke(nameof(RefreshBoardDisplay), 0.3f);
    }

    public void RefreshBoardDisplay()
    {
        ClearBoardDisplay();

        Player activePlayer = GetActivePlayer();
        if (activePlayer == null) return;

        // Update board count
        if (boardCountText != null)
            boardCountText.text = $"Board: {activePlayer.board.Count}/7";

        // Create card displays
        for (int i = 0; i < activePlayer.board.Count; i++)
        {
            CreateBoardCard(activePlayer.board[i], i);
        }
    }

    private void CreateBoardCard(Card card, int index)
    {
        if (boardCardPrefab == null || boardSlotsContainer == null) return;

        GameObject cardObj = Instantiate(boardCardPrefab, boardSlotsContainer);
        currentBoardCards.Add(cardObj);

        BoardCardUI cardUI = cardObj.GetComponent<BoardCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnCardClicked);
        }
    }

    private void ClearBoardDisplay()
    {
        foreach (GameObject card in currentBoardCards)
        {
            if (card != null) Destroy(card);
        }
        currentBoardCards.Clear();
        selectedCardIndex = -1;
    }

    private void OnCardClicked(int index)
    {
        selectedCardIndex = index;
        Debug.Log($"Selected board card at index {index}");

        for (int i = 0; i < currentBoardCards.Count; i++)
        {
            BoardCardUI cardUI = currentBoardCards[i].GetComponent<BoardCardUI>();
            if (cardUI != null)
            {
                cardUI.SetSelected(i == selectedCardIndex);
            }
        }
    }

    public void SellSelectedCard()
    {
        if (selectedCardIndex < 0)
        {
            Debug.Log("No board card selected to sell");
            return;
        }

        Player activePlayer = GetActivePlayer();
        if (activePlayer != null)
        {
            activePlayer.SellCard(selectedCardIndex);
            RefreshBoardDisplay();
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