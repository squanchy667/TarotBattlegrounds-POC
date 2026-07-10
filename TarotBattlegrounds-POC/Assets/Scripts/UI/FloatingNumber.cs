using UnityEngine;
using TMPro;
using System;
using System.Collections;
using TarotBattlegrounds.UI;

/// <summary>
/// UX14: Individual floating combat number.
/// Displays damage/heal/buff text with scale-pop animation, upward float, and fade out.
/// Managed by FloatingNumberManager's object pool.
/// </summary>
public class FloatingNumber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float floatDuration = 0.8f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float floatDistance = 60f;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float overshootScale = 1.2f;
    [SerializeField] private float settleScale = 1.0f;
    [SerializeField] private int fontSize = (int)Tokens.TextBody;

    /// <summary>
    /// Predefined color constants for combat number types.
    /// </summary>
    public static readonly Color DamageColor = Tokens.Blood;
    public static readonly Color HealColor = Tokens.BronzeBright;
    public static readonly Color BuffColor = Tokens.BronzeBright;
    public static readonly Color AegisColor = Tokens.EtherBlue;
    public static readonly Color DeathColor = Tokens.Blood;

    /// <summary>
    /// Callback invoked when this floating number finishes its animation.
    /// Used by the pool to reclaim the instance.
    /// </summary>
    public Action<FloatingNumber> OnReturnToPool;

    private Coroutine activeCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        if (numberText == null)
            numberText = GetComponentInChildren<TMP_Text>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Initialize and play the floating number animation.
    /// </summary>
    /// <param name="text">Text to display (e.g., "-5", "+3", "AEGIS!")</param>
    /// <param name="color">Color of the text</param>
    /// <param name="screenPosition">Screen-space position to spawn at</param>
    public void Show(string text, Color color, Vector2 screenPosition)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        gameObject.SetActive(true);

        if (numberText != null)
        {
            numberText.text = text;
            numberText.color = color;
            numberText.fontSize = fontSize;
            numberText.fontStyle = FontStyles.Bold;
            numberText.alignment = TextAlignmentOptions.Center;
        }

        if (rectTransform != null)
            rectTransform.anchoredPosition = screenPosition;

        transform.localScale = Vector3.one * startScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        activeCoroutine = StartCoroutine(AnimateCoroutine());
    }

    /// <summary>
    /// Full animation sequence: scale pop (0.5 -> 1.2 -> 1.0), then float up with fade out.
    /// Total duration = popDuration + floatDuration.
    /// Fade begins in the last fadeDuration seconds of the float phase.
    /// </summary>
    private IEnumerator AnimateCoroutine()
    {
        // Phase 1: Scale pop (startScale -> overshootScale -> settleScale)
        float elapsed = 0f;
        float halfPop = popDuration * 0.5f;

        // Scale up to overshoot
        while (elapsed < halfPop)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfPop);
            float scale = Mathf.Lerp(startScale, overshootScale, t);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Scale back to settle
        elapsed = 0f;
        while (elapsed < halfPop)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfPop);
            float scale = Mathf.Lerp(overshootScale, settleScale, t);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one * settleScale;

        // Phase 2: Float up with fade out in last fadeDuration seconds
        elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * floatDistance;
        float fadeStartTime = floatDuration - fadeDuration;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / floatDuration);

            // Smooth float upward
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            // Fade out in last fadeDuration seconds
            if (elapsed >= fadeStartTime && canvasGroup != null)
            {
                float fadeT = Mathf.Clamp01((elapsed - fadeStartTime) / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        // Ensure fully faded
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        activeCoroutine = null;
        ReturnToPool();
    }

    /// <summary>
    /// Return this instance to the object pool.
    /// </summary>
    public void ReturnToPool()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        gameObject.SetActive(false);
        OnReturnToPool?.Invoke(this);
    }

    /// <summary>
    /// Build a FloatingNumber from code (no prefab needed).
    /// Creates a GameObject with TMP_Text and CanvasGroup.
    /// </summary>
    public static FloatingNumber CreateFromCode(Transform parent)
    {
        GameObject obj = new GameObject("FloatingNumber");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 50f);

        CanvasGroup cg = obj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = Tokens.TextBody;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = false;
        tmpText.overflowMode = TextOverflowModes.Overflow;
        tmpText.raycastTarget = false;

        FloatingNumber fn = obj.AddComponent<FloatingNumber>();
        fn.numberText = tmpText;
        fn.canvasGroup = cg;
        fn.rectTransform = rt;

        obj.SetActive(false);
        return fn;
    }
}
