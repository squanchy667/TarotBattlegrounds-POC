---
model: sonnet
---

# UX11: Phase & Turn Banner

You are a HUD specialist for TarotBattlegrounds. Your mission: create an animated phase transition banner — "RECRUIT PHASE" and "COMBAT!" text that slides in from the side, holds briefly, then fades out. Plus a styled turn counter.

## What You're Building

1. **Phase Banner**: Full-width banner that animates on phase change:
   - Slides in from right (0.3s)
   - Holds in center (1.2s)
   - Fades out (0.4s)
   - Different colors per phase: Recruit = gold, Combat = red
   - Large bold text with subtle text shadow

2. **Turn Counter Badge**: Small styled badge showing current turn:
   - "Turn V" (Roman numerals) or "Round 5"
   - Shield/banner shape background
   - Updates smoothly (brief pulse on change)

## Files to Create

### New: PhaseBanner.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/PhaseBanner.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PhaseBanner : MonoBehaviour
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
    [SerializeField] private Color recruitColor = new Color(1f, 0.82f, 0.12f);
    [SerializeField] private Color combatColor = new Color(0.95f, 0.25f, 0.25f);
    [SerializeField] private Color bannerBgColor = new Color(0.05f, 0.03f, 0.10f, 0.85f);

    [Header("Timing")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float offscreenOffset = 800f;

    private Coroutine currentBanner;
}
```

Implement:

**ShowPhaseBanner(string text, bool isCombat):**
```csharp
public void ShowPhaseBanner(string phaseText, bool isCombat)
{
    if (currentBanner != null) StopCoroutine(currentBanner);
    currentBanner = StartCoroutine(BannerRoutine(phaseText, isCombat));
}

IEnumerator BannerRoutine(string text, bool isCombat)
{
    // Setup
    bannerText.text = text;
    bannerShadowText.text = text;
    bannerText.color = isCombat ? combatColor : recruitColor;
    bannerShadowText.color = new Color(0, 0, 0, 0.5f);
    bannerGroup.alpha = 1f;

    // Position offscreen right
    bannerRect.anchoredPosition = new Vector2(offscreenOffset, 0);

    // Slide in (ease-out)
    float elapsed = 0;
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
    elapsed = 0;
    while (elapsed < fadeOutDuration)
    {
        bannerGroup.alpha = 1f - (elapsed / fadeOutDuration);
        elapsed += Time.deltaTime;
        yield return null;
    }
    bannerGroup.alpha = 0f;
}
```

**UpdateTurn(int turnNumber):**
- Set turnText to "Turn {Roman numeral}" or "Round {number}"
- Brief scale punch on turnBadge (1.15x → 1.0x over 0.3s)
- Turn badge: dark rounded-rect background with gold border

**Text Shadow:**
- Second TMP_Text behind main text, offset by (2, -2) pixels, black with 0.5 alpha
- Creates a cheap but effective drop shadow effect

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- Add `[SerializeField] private PhaseBanner phaseBanner;`
- When phase changes to Recruit: `phaseBanner?.ShowPhaseBanner("RECRUIT PHASE", false)`
- When phase changes to Combat: `phaseBanner?.ShowPhaseBanner("COMBAT!", true)`
- When turn updates: `phaseBanner?.UpdateTurn(turnNumber)`
- Keep existing `phaseText` and `turnText` as fallback

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/PhaseBannerSetup.cs`

Editor script (Tools > Game > Setup Phase Banner):
1. Create banner overlay at center of screen:
   - CanvasGroup for fade control
   - Full-width semi-transparent background strip (80px height)
   - Main text (48pt, bold, centered)
   - Shadow text (same, offset by (2,-2), black)
2. Create turn badge at top-right:
   - Small rounded-rect (120x36)
   - TMP_Text inside (18pt, bold)
3. Wire PhaseBanner component

## Conventions
- Banner overlays everything (high sibling index or separate Canvas with higher sort order)
- Banner background: Raycast Target = false (don't block gameplay)
- Use CanvasGroup for alpha control (affects all children)
- StopCoroutine before starting new one (prevent overlapping banners)
- Project must compile with zero errors
