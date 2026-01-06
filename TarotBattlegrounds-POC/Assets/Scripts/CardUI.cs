using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Card card;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPos;
    private Transform originalParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetupCard(Card c)
    {
        card = c;
        Text text = GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = c.cardName;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPos = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        if (eventData.pointerEnter == null || !eventData.pointerEnter.GetComponent<BoardManager>())
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPos;
        }
    }
}