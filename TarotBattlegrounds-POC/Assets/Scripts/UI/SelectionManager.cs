using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages card selection across all UI panels.
/// Clears selection when clicking on empty space.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private HandUI handUI;
    [SerializeField] private BoardUI boardUI;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[SelectionManager] Initialized");
    }

    private void Update()
    {
        // Check for click outside of cards
        if (Input.GetMouseButtonDown(0))
        {
            bool overCard = IsPointerOverCard();
            bool overButton = IsPointerOverButton();

            if (debugMode)
                Debug.Log($"[SelectionManager] Click detected - OverCard: {overCard}, OverButton: {overButton}");

            // Only clear if not clicking on a card or a button
            if (!overCard && !overButton)
            {
                if (debugMode)
                    Debug.Log("[SelectionManager] Clearing all selections");
                ClearAllSelections();
            }
        }
    }

    /// <summary>
    /// Check if the pointer is over any UI element with a card component.
    /// </summary>
    private bool IsPointerOverCard()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GameObject go = result.gameObject;

            // Check the object and all its parents for card components
            Transform current = go.transform;
            while (current != null)
            {
                if (current.GetComponent<CardDisplayUI>() != null ||
                    current.GetComponent<CardHoverHandler>() != null ||
                    current.GetComponent<ShopCardUI>() != null ||
                    current.GetComponent<HandCardUI>() != null ||
                    current.GetComponent<BoardCardUI>() != null)
                {
                    return true;
                }
                current = current.parent;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if pointer is over a button (don't clear selection when clicking buttons).
    /// </summary>
    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Check if it's a button (but not a card button)
            Button btn = result.gameObject.GetComponent<Button>();
            if (btn != null)
            {
                // If this button is part of a card, ignore it (handled by IsPointerOverCard)
                if (result.gameObject.GetComponentInParent<CardDisplayUI>() == null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Clear selections in all UI panels.
    /// </summary>
    public void ClearAllSelections()
    {
        if (shopUI != null)
            shopUI.ClearSelection();

        if (handUI != null)
            handUI.ClearSelection();

        if (boardUI != null)
            boardUI.ClearSelection();

        // Update button states
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.UpdateButtons();
    }

    /// <summary>
    /// Call this when a card is selected to clear other panel selections.
    /// </summary>
    public void OnCardSelectedInPanel(string panelName)
    {
        // Clear selections in other panels
        if (panelName != "Shop" && shopUI != null)
            shopUI.ClearSelection();

        if (panelName != "Hand" && handUI != null)
            handUI.ClearSelection();

        if (panelName != "Board" && boardUI != null)
            boardUI.ClearSelection();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
