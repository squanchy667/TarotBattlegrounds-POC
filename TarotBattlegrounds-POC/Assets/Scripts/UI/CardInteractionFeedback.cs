using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TarotBattlegrounds.UI;

/// <summary>
/// UX08: Card hover and selection polish — smooth scale-up with shadow on hover,
/// animated glowing outline on selection, and ice-blue shimmer for frozen cards.
/// Attach alongside CardDisplayUI (or ShopCardUI/HandCardUI/BoardCardUI).
/// </summary>
public class CardInteractionFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverYOffset = 10f;
    [SerializeField] private float hoverSpeed = 10f;
    [SerializeField] private Image dropShadow;

    [Header("Selection Glow")]
    [SerializeField] private Image selectionGlow;
    [SerializeField] private Color selectedColor = Tokens.WithAlpha(Tokens.Ember, 0.6f);
    [SerializeField] private Color playableColor = Tokens.WithAlpha(Tokens.Ember, 0.5f); // design system has no green; "ready" maps to Ember
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMinAlpha = 0.2f;
    [SerializeField] private float glowMaxAlpha = 0.7f;

    [Header("Frozen")]
    [SerializeField] private Image frozenOverlay;
    [SerializeField] private Color frozenColor = Tokens.WithAlpha(Tokens.Ember, 0.2f); // no dedicated frozen/status token; interaction-highlight fallback
    [SerializeField] private float frozenShimmerSpeed = 0.8f;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private float targetYOffset = 0f;
    private bool isHovered = false;
    private bool isSelected = false;
    private bool isPlayable = false;
    private bool isFrozen = false;
    private int originalSiblingIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Capture original position and scale in Start (after layout has settled)
        if (rectTransform != null)
            originalPosition = rectTransform.anchoredPosition;
        originalScale = transform.localScale;

        // Initialize overlays to disabled state
        if (dropShadow != null)
        {
            dropShadow.raycastTarget = false;
            var c = dropShadow.color;
            c.a = 0f;
            dropShadow.color = c;
        }

        if (selectionGlow != null)
        {
            selectionGlow.raycastTarget = false;
            selectionGlow.gameObject.SetActive(false);
        }

        if (frozenOverlay != null)
        {
            frozenOverlay.raycastTarget = false;
            frozenOverlay.color = frozenColor;
            frozenOverlay.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Smooth scale
        float currentScale = Mathf.Lerp(transform.localScale.x / originalScale.x, targetScale, Time.deltaTime * hoverSpeed);
        transform.localScale = originalScale * currentScale;

        // Smooth Y offset
        if (rectTransform != null)
        {
            float currentYOffset = rectTransform.anchoredPosition.y - originalPosition.y;
            float newYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * hoverSpeed);
            rectTransform.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y + newYOffset);
        }

        // Shadow: visible when hovered, fade with offset
        if (dropShadow != null)
        {
            var c = dropShadow.color;
            c.a = Mathf.Lerp(c.a, isHovered ? 0.4f : 0f, Time.deltaTime * hoverSpeed);
            dropShadow.color = c;
        }

        // Selection glow pulse
        if (isSelected && selectionGlow != null)
        {
            float pulse = Mathf.Lerp(glowMinAlpha, glowMaxAlpha,
                (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
            var c = selectionGlow.color;
            c.a = pulse;
            selectionGlow.color = c;
        }

        // Frozen shimmer (subtle alpha wave)
        if (isFrozen && frozenOverlay != null)
        {
            float shimmer = Mathf.Lerp(0.1f, 0.25f,
                (Mathf.Sin(Time.time * frozenShimmerSpeed) + 1f) * 0.5f);
            var c = frozenOverlay.color;
            c.a = shimmer;
            frozenOverlay.color = c;
        }
    }

    // ===== Pointer Events =====

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = hoverScale;
        targetYOffset = hoverYOffset;

        // Bring to front so hovered card draws above neighbors
        originalSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
        targetYOffset = 0f;

        // Restore original draw order
        transform.SetSiblingIndex(originalSiblingIndex);
    }

    // ===== Public API =====

    /// <summary>
    /// Toggle selection glow. Uses selectedColor (gold) by default,
    /// or playableColor (green) if SetPlayable(true) was called.
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionGlow != null)
        {
            selectionGlow.gameObject.SetActive(selected);
            if (selected)
            {
                Color glowColor = isPlayable ? playableColor : selectedColor;
                selectionGlow.color = glowColor;
            }
        }
    }

    /// <summary>
    /// Toggle frozen overlay with ice-blue shimmer effect.
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (frozenOverlay != null)
        {
            frozenOverlay.gameObject.SetActive(frozen);
            if (frozen)
            {
                frozenOverlay.color = frozenColor;
            }
        }
    }

    /// <summary>
    /// Switch glow color between gold (selected) and green (playable).
    /// Call before or after SetSelected — the color updates on next frame.
    /// </summary>
    public void SetPlayable(bool playable)
    {
        isPlayable = playable;

        // Update glow color immediately if already selected
        if (isSelected && selectionGlow != null)
        {
            Color glowColor = isPlayable ? playableColor : selectedColor;
            var c = selectionGlow.color;
            selectionGlow.color = Tokens.WithAlpha(glowColor, c.a);
        }
    }

    /// <summary>
    /// Re-capture original position. Call after layout changes (e.g., card moved in hand).
    /// </summary>
    public void RefreshOriginalPosition()
    {
        if (rectTransform != null)
            originalPosition = rectTransform.anchoredPosition;
    }
}
