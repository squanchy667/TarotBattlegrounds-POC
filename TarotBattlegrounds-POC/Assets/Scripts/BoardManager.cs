using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour, IDropHandler
{
    [SerializeField] private Transform[] boardSlots = new Transform[7]; // Assign 7 UI Panels
    [SerializeField] private GameObject cardUIPrefab; // UI Image with CardUI
    private int playerId; // Set by GameManager

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    public void PlaceCardToBoard(Card card, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 7) return;
        Player player = GameManager.Instance.players.Find(p => p.playerId == playerId);
        if (player == null || player.board.Count >= 7) return;

        int handIndex = player.hand.FindIndex(c => c.cardName == card.cardName && c.tier == card.tier);
        if (handIndex >= 0)
        {
            player.PlayCard(handIndex, slotIndex);
            GameObject cardUI = Instantiate(cardUIPrefab, boardSlots[slotIndex]);
            cardUI.GetComponent<CardUI>().SetupCard(card);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            CardUI cardUI = eventData.pointerDrag.GetComponent<CardUI>();
            if (cardUI != null && cardUI.card != null)
            {
                int slotIndex = GetNearestSlot(eventData.position);
                PlaceCardToBoard(cardUI.card, slotIndex);
                Destroy(eventData.pointerDrag);
            }
        }
    }

    private int GetNearestSlot(Vector2 pos)
    {
        float minDist = float.MaxValue;
        int slotIndex = 0;
        for (int i = 0; i < boardSlots.Length; i++)
        {
            float dist = Vector2.Distance(pos, boardSlots[i].GetComponent<RectTransform>().position);
            if (dist < minDist)
            {
                minDist = dist;
                slotIndex = i;
            }
        }
        return slotIndex;
    }

    public void ClearBoard()
    {
        foreach (Transform slot in boardSlots)
        {
            foreach (Transform child in slot)
            {
                Destroy(child.gameObject);
            }
        }
    }
}