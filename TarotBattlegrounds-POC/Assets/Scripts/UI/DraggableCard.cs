using UnityEngine;
using UnityEngine.EventSystems;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T408-T410: Makes a card UI element draggable.
    /// Attach to card GameObjects alongside CardDisplayUI, ShopCardUI, etc.
    /// Integrates with DragDropManager for the actual drag logic.
    /// </summary>
    public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private DragDropManager.DragSource source = DragDropManager.DragSource.Hand;

        private Card card;
        private int cardIndex;

        public void Setup(Card cardData, int index, DragDropManager.DragSource dragSource)
        {
            card = cardData;
            cardIndex = index;
            source = dragSource;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (DragDropManager.Instance == null) return;
            if (card == null) return;

            DragDropManager.Instance.BeginDrag(source, cardIndex, card, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (DragDropManager.Instance == null) return;
            DragDropManager.Instance.UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (DragDropManager.Instance == null) return;
            DragDropManager.Instance.EndDrag(eventData);
        }
    }
}
