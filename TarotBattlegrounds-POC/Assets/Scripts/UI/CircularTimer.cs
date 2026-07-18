using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// UX10: Circular arc countdown timer with color transitions and pulse effect.
/// Displays a depleting arc (green > yellow > red) with center text showing remaining seconds.
/// </summary>
public class CircularTimer : MonoBehaviour
{
    [Header("Timer Display")]
    [SerializeField] private Image arcFill;           // Filled arc (Image type = Filled)
    [SerializeField] private Image arcBackground;     // Full circle background (dim)
    [SerializeField] private TMP_Text timeText;       // Center number
    [SerializeField] private Image centerCircle;      // Dark center fill

    [Header("Colors")]
    [SerializeField] private Color safeColor = Tokens.Ember;      // >15s
    [SerializeField] private Color warningColor = Tokens.BronzeBright;    // 5-15s
    [SerializeField] private Color dangerColor = Tokens.Blood;  // <5s
    [SerializeField] private Color bgRingColor = Tokens.WithAlpha(Tokens.StoneEdge, 0.5f);

    [Header("Pulse")]
    [SerializeField] private float pulseThreshold = 5f;    // Start pulsing at 5s
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScale = 1.1f;

    [Header("Settings")]
    [SerializeField] private float maxTime = 35f;

    private RectTransform rectTransform;
    private float currentTime;
    private bool isRunning = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Configure arcFill as a radial filled image
        if (arcFill != null)
        {
            arcFill.type = Image.Type.Filled;
            arcFill.fillMethod = Image.FillMethod.Radial360;
            arcFill.fillOrigin = (int)Image.Origin360.Top;
            arcFill.fillClockwise = true;
            arcFill.raycastTarget = false;
        }

        // Configure arcBackground as full circle
        if (arcBackground != null)
        {
            arcBackground.type = Image.Type.Filled;
            arcBackground.fillMethod = Image.FillMethod.Radial360;
            arcBackground.fillOrigin = (int)Image.Origin360.Top;
            arcBackground.fillClockwise = true;
            arcBackground.fillAmount = 1f;
            arcBackground.color = bgRingColor;
            arcBackground.raycastTarget = false;
        }

        // Center circle creates the ring appearance
        if (centerCircle != null)
        {
            centerCircle.raycastTarget = false;
        }
    }

    /// <summary>
    /// Generate a white circle texture at runtime for use as sprite on the arc images.
    /// Uses SDF for anti-aliased edges.
    /// </summary>
    public static Sprite GenerateCircleSprite(int size = 128)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                // SDF anti-aliasing: smooth edge over 1 pixel
                float alpha = Mathf.Clamp01(radius - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Set the timer display. Called each frame by the timer controller.
    /// </summary>
    /// <param name="remaining">Seconds remaining</param>
    /// <param name="total">Total seconds for this phase</param>
    public void SetTime(float remaining, float total)
    {
        currentTime = remaining;
        maxTime = total;
        isRunning = remaining > 0;

        // Update arc fill amount
        if (arcFill != null)
        {
            arcFill.fillAmount = total > 0f ? remaining / total : 0f;

            // Update color based on remaining time
            if (remaining > 15f)
            {
                arcFill.color = safeColor;
            }
            else if (remaining > 5f)
            {
                arcFill.color = Color.Lerp(warningColor, safeColor, (remaining - 5f) / 10f);
            }
            else
            {
                arcFill.color = Color.Lerp(dangerColor, warningColor, remaining / 5f);
            }
        }

        // Update center text
        if (timeText != null)
        {
            if (remaining <= 0f)
            {
                timeText.text = "0";
            }
            else
            {
                timeText.text = Mathf.CeilToInt(remaining).ToString();
            }

            // Text color matches arc color
            if (arcFill != null)
            {
                timeText.color = arcFill.color;
            }
        }

        // Ensure arc shows 0 fill at zero
        if (remaining <= 0f && arcFill != null)
        {
            arcFill.fillAmount = 0f;
        }

        // Reset scale when not in pulse zone
        if (remaining > pulseThreshold && rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        if (!isRunning || currentTime > pulseThreshold) return;

        if (rectTransform == null) return;

        // Pulse effect in danger zone
        float pulse = 1f + (pulseScale - 1f) * Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
        rectTransform.localScale = Vector3.one * pulse;
    }

    /// <summary>
    /// Hide the timer display (e.g., during combat phase).
    /// </summary>
    public void Hide()
    {
        isRunning = false;

        if (arcFill != null)
            arcFill.fillAmount = 0f;

        if (timeText != null)
            timeText.text = "";

        gameObject.SetActive(false);

        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
    }
}
