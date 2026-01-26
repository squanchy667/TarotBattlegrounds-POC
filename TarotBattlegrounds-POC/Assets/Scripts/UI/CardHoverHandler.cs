using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this component to any card UI element to enable hover tooltips.
/// Requires a Card reference - will try to get it from CardDisplayUI, ShopCardUI, etc.
/// </summary>
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Card card;

    // Cache references to different card UI types
    private CardDisplayUI cardDisplayUI;
    private ShopCardUI shopCardUI;
    private HandCardUI handCardUI;
    private BoardCardUI boardCardUI;

    private void Awake()
    {
        // Try to find card UI components
        cardDisplayUI = GetComponent<CardDisplayUI>();
        shopCardUI = GetComponent<ShopCardUI>();
        handCardUI = GetComponent<HandCardUI>();
        boardCardUI = GetComponent<BoardCardUI>();
    }

    /// <summary>
    /// Manually set the card (if not using one of the standard card UI components).
    /// </summary>
    public void SetCard(Card cardData)
    {
        card = cardData;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Card hoverCard = GetCard();
        if (hoverCard != null && CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardHoverEnter(hoverCard);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardHoverExit();
        }
    }

    private Card GetCard()
    {
        // Return manually set card first
        if (card != null) return card;

        // Try to get from various card UI components
        if (cardDisplayUI != null)
            return cardDisplayUI.GetCard();

        if (shopCardUI != null)
            return shopCardUI.GetCard();

        if (handCardUI != null)
            return handCardUI.GetCard();

        if (boardCardUI != null)
            return boardCardUI.GetCard();

        return null;
    }

    private void OnDisable()
    {
        // Hide tooltip when card is disabled
        if (CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardHoverExit();
        }
    }
}
