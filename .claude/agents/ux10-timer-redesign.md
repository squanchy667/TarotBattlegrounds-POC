---
model: sonnet
---

# UX10: Timer Redesign

You are a HUD specialist for TarotBattlegrounds. Your mission: replace the plain text timer with a circular arc countdown that changes color as time runs out — green to yellow to red — with a pulse effect in the last 5 seconds.

## What You're Building

A circular countdown timer:
- **Circular arc** that depletes clockwise as time passes
- **Color transitions**: Green (>15s) → Yellow (5-15s) → Red (<5s)
- **Pulse animation**: Timer scales up/down rhythmically in last 5 seconds
- **Center text**: Remaining seconds displayed inside the arc
- **Background ring**: Subtle dark ring showing the full circle outline

## Files to Create

### New: CircularTimer.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CircularTimer.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircularTimer : MonoBehaviour
{
    [Header("Timer Display")]
    [SerializeField] private Image arcFill;           // Filled arc (Image type = Filled)
    [SerializeField] private Image arcBackground;     // Full circle background (dim)
    [SerializeField] private TMP_Text timeText;       // Center number
    [SerializeField] private Image centerCircle;      // Dark center fill

    [Header("Colors")]
    [SerializeField] private Color safeColor = new Color(0.2f, 0.85f, 0.4f);      // >15s
    [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.12f);    // 5-15s
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.25f, 0.25f);  // <5s
    [SerializeField] private Color bgRingColor = new Color(0.2f, 0.15f, 0.3f, 0.5f);

    [Header("Pulse")]
    [SerializeField] private float pulseThreshold = 5f;    // Start pulsing at 5s
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScale = 1.1f;

    [Header("Settings")]
    [SerializeField] private float maxTime = 35f;

    private RectTransform rectTransform;
    private float currentTime;
    private bool isRunning = false;
}
```

Implement:

**Timer Setup:**
- `arcFill`: Unity Image component with `Image.Type = Filled`, `fillMethod = Radial360`, `fillOrigin = Top`, `fillClockwise = true`
- `arcBackground`: Same but always full circle at dim color
- `centerCircle`: Solid dark circle in center (creates the ring appearance)

**Update Logic:**
```csharp
public void SetTime(float remaining, float total)
{
    currentTime = remaining;
    maxTime = total;
    isRunning = remaining > 0;

    // Update arc fill
    arcFill.fillAmount = remaining / total;

    // Update color based on remaining time
    if (remaining > 15f)
        arcFill.color = safeColor;
    else if (remaining > 5f)
        arcFill.color = Color.Lerp(warningColor, safeColor, (remaining - 5f) / 10f);
    else
        arcFill.color = Color.Lerp(dangerColor, warningColor, remaining / 5f);

    // Update text
    timeText.text = Mathf.CeilToInt(remaining).ToString();
    timeText.color = arcFill.color;

    // Show "0" explicitly at zero (existing bug fix)
    if (remaining <= 0)
    {
        timeText.text = "0";
        arcFill.fillAmount = 0;
    }
}

void Update()
{
    if (!isRunning || currentTime > pulseThreshold) return;

    // Pulse effect in danger zone
    float pulse = 1f + (pulseScale - 1f) * Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
    rectTransform.localScale = Vector3.one * pulse;
}
```

**Ring Texture (procedural):**
Generate a 128x128 ring texture:
- Outer radius: 64px, Inner radius: 48px
- Anti-aliased edges via SDF
- Applied to both arcFill and arcBackground as sprite
- Use `Sprite.Create()` with proper pivot at center

**OR simpler approach**: Use two overlapping Unity Images:
- arcBackground: Full filled circle at bgRingColor
- arcFill: Filled radial image on top
- centerCircle: Smaller solid circle on top of both (creates ring look)

## Files to Modify

### RecruitTimerUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/RecruitTimerUI.cs`

- Integrate CircularTimer component
- When updating timer text, also call `circularTimer.SetTime(remaining, total)`
- Keep existing text-based timer as fallback

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- Add `[SerializeField] private CircularTimer circularTimer;`
- In timer update code, pass values to CircularTimer

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/CircularTimerSetup.cs`

Editor script (Tools > Game > Setup Circular Timer):
1. Create timer GameObject with:
   - "ArcBackground" — Image (Filled, Radial360, white circle sprite)
   - "ArcFill" — Image (Filled, Radial360, on top)
   - "CenterCircle" — Image (solid dark circle)
   - "TimeText" — TMP_Text centered
2. Size: 80x80 px
3. Position: near top-center of screen
4. Wire CircularTimer component

## Key Implementation Notes

- **Image.Type.Filled** with **Radial360** is Unity's built-in arc rendering — no custom textures needed for the basic arc
- For the ring look: overlay a smaller solid circle in the center
- **Circle sprite**: Generate a 128x128 white circle texture, or use Unity's built-in "Knob" sprite
- **Performance**: Only pulse when < pulseThreshold (skip Update otherwise)
- **Reset**: When timer resets for new turn, snap to full (no animation needed)

## Conventions
- MonoBehaviour (timer isn't theme-dependent, uses its own colors)
- Font: Bold, 28pt for center number
- No coroutines needed — all in Update()
- Null-check all references
- Project must compile with zero errors
