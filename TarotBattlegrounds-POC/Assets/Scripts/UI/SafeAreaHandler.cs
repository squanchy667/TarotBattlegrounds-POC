using UnityEngine;

/// <summary>
/// Handles safe area adjustments for notched displays and various screen sizes.
/// Attach to the main Canvas or a child panel that should respect safe area boundaries.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    [Header("Settings")]
    [Tooltip("Apply safe area to all edges")]
    [SerializeField] private bool applyToAllEdges = true;

    [Tooltip("Only apply to specific edges when applyToAllEdges is false")]
    [SerializeField] private bool applyToTop = true;
    [SerializeField] private bool applyToBottom = true;
    [SerializeField] private bool applyToLeft = true;
    [SerializeField] private bool applyToRight = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        // Check if safe area has changed (orientation change, etc.)
        if (SafeAreaChanged())
        {
            ApplySafeArea();
        }
    }

    private bool SafeAreaChanged()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        bool changed = safeArea != lastSafeArea ||
                       screenSize != lastScreenSize ||
                       orientation != lastOrientation;

        if (changed)
        {
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastOrientation = orientation;
        }

        return changed;
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        if (showDebugInfo)
        {
            Debug.Log($"[SafeAreaHandler] Screen: {Screen.width}x{Screen.height}, SafeArea: {safeArea}");
        }

        // Convert safe area to anchor values (0-1 range)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Apply selective edge handling
        if (!applyToAllEdges)
        {
            if (!applyToLeft) anchorMin.x = 0f;
            if (!applyToBottom) anchorMin.y = 0f;
            if (!applyToRight) anchorMax.x = 1f;
            if (!applyToTop) anchorMax.y = 1f;
        }

        // Apply to RectTransform
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        if (showDebugInfo)
        {
            Debug.Log($"[SafeAreaHandler] Applied anchors: min={anchorMin}, max={anchorMax}");
        }
    }

    /// <summary>
    /// Force refresh the safe area (call after resolution changes)
    /// </summary>
    public void RefreshSafeArea()
    {
        lastSafeArea = Rect.zero;
        ApplySafeArea();
    }

    /// <summary>
    /// Get the current safe area in screen coordinates
    /// </summary>
    public static Rect GetSafeArea()
    {
        return Screen.safeArea;
    }

    /// <summary>
    /// Check if the device has a notch or cutout
    /// </summary>
    public static bool HasNotch()
    {
        Rect safeArea = Screen.safeArea;
        return safeArea.x > 0 ||
               safeArea.y > 0 ||
               safeArea.width < Screen.width ||
               safeArea.height < Screen.height;
    }
}
