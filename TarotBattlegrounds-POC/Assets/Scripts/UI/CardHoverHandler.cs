using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this component to any card UI element to enable card tooltips.
/// Requires a Card reference - will try to get it from CardDisplayUI, ShopCardUI, etc.
///
/// T728: routes by pointer type so both input styles work without conflicting:
///   - Mouse (pointerId &lt; 0)  -> hover (enter/exit), cursor-following tooltip (desktop, unchanged).
///   - Touch (pointerId &gt;= 0) -> tap-and-hold (down/up), fixed-anchor tooltip (mobile).
/// </summary>
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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
        // Mouse hover only (negative pointerId). Touch fires enter on press too — that path is
        // handled by OnPointerDown so touch gets tap-and-hold, not an instant hover tooltip.
        if (eventData.pointerId >= 0) return;

        Card hoverCard = GetCard();
        if (hoverCard != null && CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardHoverEnter(hoverCard);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId >= 0) return;

        if (CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardHoverExit();
        }
    }

    // T728: tap-and-hold for touch. pointerId >= 0 is a touch; mouse buttons are negative, so this
    // never hijacks a desktop click. The tooltip's showDelay doubles as the hold threshold, so a
    // quick tap (to buy/select) doesn't flash the tooltip.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerId < 0) return;

        Card pressCard = GetCard();
        if (pressCard != null && CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardPressStart(pressCard);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId < 0) return;

        if (CardTooltipUI.Instance != null)
        {
            CardTooltipUI.Instance.OnCardPressEnd();
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
