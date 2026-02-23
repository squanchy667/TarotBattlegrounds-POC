using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T407-T410: Core drag-and-drop system for card management.
    /// Enables dragging cards between shop, hand, and board.
    /// Attach to a Canvas-level GameObject in the scene.
    /// </summary>
    public class DragDropManager : MonoBehaviour
    {
        public static DragDropManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float dragThreshold = 10f;
        [SerializeField] private float dragAlpha = 0.7f;
        [SerializeField] private float snapSpeed = 15f;

        [Header("Visual Feedback")]
        [SerializeField] private Color validDropColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color invalidDropColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);

        // Drag state
        private bool isDragging;
        private DragSource dragSource;
        private int dragIndex;
        private Card dragCard;
        private GameObject dragGhost;
        private RectTransform dragGhostRect;
        private CanvasGroup dragGhostGroup;
        private Vector2 dragStartPos;
        private Canvas parentCanvas;

        // Drop zones
        private List<DropZone> dropZones = new List<DropZone>();

        public enum DragSource { None, Shop, Hand, Board }

        public event Action OnDragStarted;
        public event Action OnDragEnded;

        /// <summary>
        /// Registered drop zone that accepts cards from specific sources.
        /// </summary>
        public class DropZone
        {
            public RectTransform rect;
            public DragSource[] acceptsFrom;
            public Action<DragSource, int, Card, int> onDrop; // source, sourceIndex, card, dropSlot
            public Func<DragSource, Card, bool> canAccept;
            public string zoneName;
            public int slotIndex; // For board positioning
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            parentCanvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// Register a drop zone. Call from ShopUI, HandUI, BoardUI during Start().
        /// </summary>
        public void RegisterDropZone(DropZone zone)
        {
            if (zone != null && !dropZones.Contains(zone))
                dropZones.Add(zone);
        }

        public void UnregisterDropZone(DropZone zone)
        {
            dropZones.Remove(zone);
        }

        /// <summary>
        /// Called by card UI components when a drag starts.
        /// </summary>
        public void BeginDrag(DragSource source, int index, Card card, PointerEventData eventData)
        {
            if (!IsRecruitPhase()) return;

            dragSource = source;
            dragIndex = index;
            dragCard = card;
            dragStartPos = eventData.position;

            // Create ghost
            CreateDragGhost(card, eventData.position);
            isDragging = true;
            OnDragStarted?.Invoke();

            Debug.Log($"[DragDrop] Started drag: {source} index {index} ({card.cardName})");
        }

        /// <summary>
        /// Called during drag movement.
        /// </summary>
        public void UpdateDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Move ghost
            if (dragGhostRect != null)
                dragGhostRect.position = eventData.position;

            // Highlight valid drop zones
            DropZone hoveredZone = FindDropZone(eventData.position);
            UpdateDropZoneHighlights(hoveredZone);
        }

        /// <summary>
        /// Called when drag ends.
        /// </summary>
        public void EndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Check if we're over a valid drop zone
            DropZone targetZone = FindDropZone(eventData.position);

            if (targetZone != null && IsValidDrop(targetZone))
            {
                // Execute the drop
                targetZone.onDrop?.Invoke(dragSource, dragIndex, dragCard, targetZone.slotIndex);
                Debug.Log($"[DragDrop] Dropped {dragCard.cardName} from {dragSource} onto {targetZone.zoneName}");
            }
            else
            {
                Debug.Log($"[DragDrop] Cancelled drag (no valid drop zone)");
            }

            // Cleanup
            DestroyDragGhost();
            ClearDropZoneHighlights();
            isDragging = false;
            dragSource = DragSource.None;
            dragCard = null;
            OnDragEnded?.Invoke();
        }

        private void CreateDragGhost(Card card, Vector2 position)
        {
            DestroyDragGhost();

            dragGhost = new GameObject("DragGhost");
            dragGhost.transform.SetParent(transform, false);

            // Add RectTransform
            dragGhostRect = dragGhost.AddComponent<RectTransform>();
            dragGhostRect.sizeDelta = new Vector2(100f, 140f);
            dragGhostRect.position = position;

            // Add CanvasGroup for alpha
            dragGhostGroup = dragGhost.AddComponent<CanvasGroup>();
            dragGhostGroup.alpha = dragAlpha;
            dragGhostGroup.blocksRaycasts = false;
            dragGhostGroup.interactable = false;

            // Add visual
            var bg = dragGhost.AddComponent<Image>();
            TribeType tribe = card.GetPrimaryTribe();
            bg.color = TarotBattlegrounds.UI.CardFrameGenerator.GetTribeBackgroundColor(tribe);

            // Add card name text
            var textObj = new GameObject("Name");
            textObj.transform.SetParent(dragGhost.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5f, 5f);
            textRect.offsetMax = new Vector2(-5f, -5f);

            var tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = $"{card.cardName}\n{card.attack}/{card.health}";
            tmpText.fontSize = 12f;
            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
            tmpText.color = Color.white;
        }

        private void DestroyDragGhost()
        {
            if (dragGhost != null) Destroy(dragGhost);
            dragGhost = null;
            dragGhostRect = null;
            dragGhostGroup = null;
        }

        private DropZone FindDropZone(Vector2 screenPosition)
        {
            foreach (var zone in dropZones)
            {
                if (zone.rect == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(zone.rect, screenPosition, parentCanvas?.worldCamera))
                    return zone;
            }
            return null;
        }

        private bool IsValidDrop(DropZone zone)
        {
            if (zone.acceptsFrom != null)
            {
                bool accepted = false;
                foreach (var source in zone.acceptsFrom)
                {
                    if (source == dragSource) { accepted = true; break; }
                }
                if (!accepted) return false;
            }

            if (zone.canAccept != null)
                return zone.canAccept(dragSource, dragCard);

            return true;
        }

        private void UpdateDropZoneHighlights(DropZone hoveredZone)
        {
            // Could add highlight images to drop zones here
        }

        private void ClearDropZoneHighlights()
        {
            // Clear any drop zone visual highlights
        }

        private bool IsRecruitPhase()
        {
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        }

        public bool IsDragging => isDragging;
        public DragSource CurrentDragSource => dragSource;
        public Card CurrentDragCard => dragCard;

        private void OnDestroy()
        {
            DestroyDragGhost();
            if (Instance == this) Instance = null;
        }
    }
}
