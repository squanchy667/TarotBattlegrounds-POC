using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TarotBattlegrounds.UI;

/// <summary>
/// Animated phase transition banner and turn counter badge.
/// Shows "RECRUIT PHASE" or "COMBAT!" text that slides in from the side,
/// holds briefly, then fades out. Also displays a styled turn counter.
/// </summary>
public class PhaseBanner : MonoBehaviour, IThemeable
{
    [Header("Banner")]
    [SerializeField] private RectTransform bannerRect;
    [SerializeField] private Image bannerBackground;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private TMP_Text bannerShadowText;
    [SerializeField] private CanvasGroup bannerGroup;

    [Header("Turn Badge")]
    [SerializeField] private Image turnBadge;
    [SerializeField] private TMP_Text turnText;

    [Header("Phase Colors")]
    [SerializeField] private Color recruitColor = Tokens.BronzeBright;
    [SerializeField] private Color combatColor = Tokens.Blood;
    [SerializeField] private Color bannerBgColor = Tokens.WithAlpha(Tokens.Ash, 0.85f);

    [Header("Timing")]
    [SerializeField] private float slideInDuration = Tokens.DurBase;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = Tokens.DurSlow;
    [SerializeField] private float offscreenOffset = 800f;

    private Coroutine currentBanner;
    private Coroutine currentTurnPulse;

    // Roman numeral lookup arrays
    private static readonly int[] romanValues = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    private static readonly string[] romanSymbols = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    private void OnEnable()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += ApplyTheme;

        // Apply current theme if available
        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply theme colors to the phase banner.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Use theme accent color for recruit (gold), keep combat red
        recruitColor = theme.accentColor;
    }

    /// <summary>
    /// Show the phase banner with slide-in animation.
    /// </summary>
    /// <param name="phaseText">Text to display (e.g., "RECRUIT PHASE", "COMBAT!")</param>
    /// <param name="isCombat">True for combat phase (red), false for recruit phase (gold)</param>
    public void ShowPhaseBanner(string phaseText, bool isCombat)
    {
        if (bannerRect == null || bannerText == null || bannerGroup == null) return;

        if (currentBanner != null) StopCoroutine(currentBanner);
        currentBanner = StartCoroutine(BannerRoutine(phaseText, isCombat));
    }

    private IEnumerator BannerRoutine(string text, bool isCombat)
    {
        // Setup text
        if (bannerText != null)
        {
            bannerText.text = text;
            bannerText.color = isCombat ? combatColor : recruitColor;
        }

        if (bannerShadowText != null)
        {
            bannerShadowText.text = text;
            bannerShadowText.color = Tokens.WithAlpha(Tokens.Ash, 0.5f);
        }

        // Setup background
        if (bannerBackground != null)
        {
            bannerBackground.color = bannerBgColor;
        }

        bannerGroup.alpha = 1f;

        // Position offscreen right
        bannerRect.anchoredPosition = new Vector2(offscreenOffset, 0);

        // Slide in (ease-out quadratic)
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            float t = elapsed / slideInDuration;
            t = 1f - (1f - t) * (1f - t); // Ease-out quad
            bannerRect.anchoredPosition = new Vector2(Mathf.Lerp(offscreenOffset, 0, t), 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bannerRect.anchoredPosition = Vector2.zero;

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            bannerGroup.alpha = 1f - (elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        bannerGroup.alpha = 0f;

        currentBanner = null;
    }

    /// <summary>
    /// Update the turn counter badge with the current turn number.
    /// Shows turn number in Roman numerals with a brief scale pulse.
    /// </summary>
    /// <param name="turnNumber">Current turn number</param>
    public void UpdateTurn(int turnNumber)
    {
        // Single source of turn text: GameUIManager.turnText (top HUD).
        // Hiding the badge avoids "Turn 5" + "Turn V" stacking on combat/recruit.
        if (turnBadge != null)
            turnBadge.gameObject.SetActive(false);
        if (turnText != null)
            turnText.gameObject.SetActive(false);
    }

    private IEnumerator TurnPulseRoutine()
    {
        if (turnBadge == null) yield break;

        RectTransform badgeRect = turnBadge.rectTransform;
        float pulseDuration = Tokens.DurBase;
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            float t = elapsed / pulseDuration;
            float scale = Mathf.Lerp(1.15f, 1.0f, t);
            badgeRect.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        badgeRect.localScale = Vector3.one;

        currentTurnPulse = null;
    }

    /// <summary>
    /// Convert an integer to its Roman numeral representation.
    /// </summary>
    private static string ToRoman(int number)
    {
        if (number <= 0 || number > 3999) return number.ToString();

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        for (int i = 0; i < romanValues.Length; i++)
        {
            while (number >= romanValues[i])
            {
                result.Append(romanSymbols[i]);
                number -= romanValues[i];
            }
        }
        return result.ToString();
    }
}
