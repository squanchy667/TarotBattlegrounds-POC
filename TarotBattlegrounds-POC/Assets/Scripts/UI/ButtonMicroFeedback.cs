using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UX13: Lightweight micro-interaction component for buttons.
/// Adds hover bounce, press squish, click ripple, and disabled-click shake.
/// Attach to any GameObject with a Button component.
/// Works independently of TarotButton (UX04) — can coexist.
/// All animation runs in Update() with Lerp — no coroutines, no allocations.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonMicroFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.1f;

    [Header("Press Settings")]
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float pressBounceSpeed = 16f;

    [Header("Ripple Settings")]
    [SerializeField] private float rippleDuration = 0.3f;
    [SerializeField] private float rippleMaxScale = 2f;
    [SerializeField] private Color rippleColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeAmplitude = 6f;
    [SerializeField] private float shakeFrequency = 40f;

    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalLocalPosition;

    // State tracking
    private bool isHovered;
    private bool isPressed;
    private float targetScale = 1f;
    private float currentScaleFactor = 1f;

    // Shake state
    private bool isShaking;
    private float shakeTimer;

    // Ripple state
    private bool isRippling;
    private float rippleTimer;
    private RectTransform rippleRect;
    private Image rippleImage;
    private Color rippleBaseColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        originalLocalPosition = transform.localPosition;

        // Create ripple child object
        CreateRippleChild();
    }

    /// <summary>
    /// Creates a child Image used for the click ripple effect.
    /// The ripple is hidden by default and activated on click.
    /// </summary>
    private void CreateRippleChild()
    {
        GameObject rippleObj = new GameObject("RippleEffect");
        rippleObj.transform.SetParent(transform, false);

        rippleRect = rippleObj.AddComponent<RectTransform>();
        rippleRect.anchorMin = new Vector2(0.5f, 0.5f);
        rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
        rippleRect.pivot = new Vector2(0.5f, 0.5f);
        rippleRect.sizeDelta = new Vector2(40f, 40f);
        rippleRect.anchoredPosition = Vector2.zero;
        rippleRect.localScale = Vector3.zero;

        rippleImage = rippleObj.AddComponent<Image>();
        rippleImage.color = rippleColor;
        rippleImage.raycastTarget = false;

        // Use a simple white sprite — the color tint provides the visual
        rippleImage.sprite = null;

        rippleObj.SetActive(false);
    }

    private void Update()
    {
        UpdateScale();
        UpdateShake();
        UpdateRipple();
    }

    // ========== SCALE ANIMATION ==========

    private void UpdateScale()
    {
        // Smooth lerp toward target scale using ease-out feel
        float lerpSpeed = isPressed ? pressBounceSpeed : (1f / Mathf.Max(hoverDuration, 0.01f));
        currentScaleFactor = Mathf.Lerp(currentScaleFactor, targetScale, Time.unscaledDeltaTime * lerpSpeed);

        // Only apply scale if not shaking (shake offsets position, not scale — but
        // we still update scale during shake for consistency)
        transform.localScale = originalScale * currentScaleFactor;
    }

    // ========== SHAKE ANIMATION ==========

    private void UpdateShake()
    {
        if (!isShaking) return;

        shakeTimer -= Time.unscaledDeltaTime;

        if (shakeTimer <= 0f)
        {
            // Shake finished — restore position
            isShaking = false;
            transform.localPosition = originalLocalPosition;
            return;
        }

        // Decay factor: 1 at start, 0 at end
        float decay = shakeTimer / shakeDuration;

        // Horizontal oscillation via sine wave
        float offset = Mathf.Sin(Time.unscaledTime * shakeFrequency) * shakeAmplitude * decay;
        transform.localPosition = originalLocalPosition + new Vector3(offset, 0f, 0f);
    }

    /// <summary>
    /// Start the invalid-click shake animation (3 oscillations over 0.3s).
    /// </summary>
    private void TriggerShake()
    {
        // Capture current position as base (in case layout shifted)
        originalLocalPosition = transform.localPosition;
        isShaking = true;
        shakeTimer = shakeDuration;
    }

    // ========== RIPPLE ANIMATION ==========

    private void UpdateRipple()
    {
        if (!isRippling) return;

        rippleTimer -= Time.unscaledDeltaTime;

        if (rippleTimer <= 0f)
        {
            // Ripple finished
            isRippling = false;
            if (rippleRect != null)
            {
                rippleRect.gameObject.SetActive(false);
            }
            return;
        }

        // Progress: 0 at start, 1 at end
        float progress = 1f - (rippleTimer / rippleDuration);

        // Scale: 0 -> rippleMaxScale
        float scale = Mathf.Lerp(0f, rippleMaxScale, progress);
        if (rippleRect != null)
        {
            rippleRect.localScale = new Vector3(scale, scale, 1f);
        }

        // Alpha: fade out
        if (rippleImage != null)
        {
            float alpha = Mathf.Lerp(rippleBaseColor.a, 0f, progress);
            rippleImage.color = new Color(rippleBaseColor.r, rippleBaseColor.g, rippleBaseColor.b, alpha);
        }
    }

    /// <summary>
    /// Trigger a click ripple effect expanding from the click position.
    /// </summary>
    private void TriggerRipple(Vector2 localClickPos)
    {
        if (rippleRect == null || rippleImage == null) return;

        rippleRect.gameObject.SetActive(true);
        rippleRect.anchoredPosition = localClickPos;
        rippleRect.localScale = Vector3.zero;

        rippleBaseColor = rippleColor;
        rippleImage.color = rippleBaseColor;

        isRippling = true;
        rippleTimer = rippleDuration;
    }

    // ========== POINTER EVENT HANDLERS ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isHovered = true;
        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsButtonInteractable()) return;

        isPressed = true;
        targetScale = pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isHovered && IsButtonInteractable())
        {
            // Return to hover state
            targetScale = hoverScale;
        }
        else
        {
            targetScale = 1f;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null) return;

        if (!button.interactable)
        {
            // Disabled button clicked — trigger shake feedback
            TriggerShake();
            return;
        }

        // Interactable click — trigger ripple from click position
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        TriggerRipple(localPoint);
    }

    // ========== HELPERS ==========

    private bool IsButtonInteractable()
    {
        return button != null && button.interactable;
    }

    /// <summary>
    /// Reset all animation state. Useful when recycling or pooling buttons.
    /// </summary>
    public void ResetAnimationState()
    {
        isHovered = false;
        isPressed = false;
        isShaking = false;
        isRippling = false;
        targetScale = 1f;
        currentScaleFactor = 1f;
        transform.localScale = originalScale;
        transform.localPosition = originalLocalPosition;

        if (rippleRect != null)
        {
            rippleRect.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Clean up animation state when disabled
        isHovered = false;
        isPressed = false;
        isShaking = false;
        isRippling = false;
        targetScale = 1f;
        currentScaleFactor = 1f;

        // Restore transforms
        transform.localScale = originalScale;
        transform.localPosition = originalLocalPosition;

        if (rippleRect != null)
        {
            rippleRect.gameObject.SetActive(false);
        }
    }
}
