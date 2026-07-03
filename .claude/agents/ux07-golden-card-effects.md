---
model: sonnet
---

# UX07: Golden Card Effects

You are a visual effects specialist for TarotBattlegrounds. Your mission: make golden (tripled) cards feel legendary — animated shimmer, pulsing gold border, and sparkle particles.

## What You're Building

Golden card visual effects:
1. **Animated shimmer** — Diagonal light sweep across the card surface (repeating every 3s)
2. **Pulsing gold border** — Frame border slowly pulses brightness (sine wave)
3. **Sparkle particles** — Small golden sparkles floating up from the card
4. **Gold tint enhancement** — Richer gold overlay than current flat 25% alpha

## Files to Create

### New: GoldenCardEffect.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GoldenCardEffect.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GoldenCardEffect : MonoBehaviour
{
    [Header("Shimmer")]
    [SerializeField] private Image shimmerOverlay;
    [SerializeField] private float shimmerInterval = 3f;
    [SerializeField] private float shimmerDuration = 0.8f;
    [SerializeField] private float shimmerWidth = 0.15f;  // Width of light band (0-1)

    [Header("Border Pulse")]
    [SerializeField] private Image borderImage;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinAlpha = 0.3f;
    [SerializeField] private float pulseMaxAlpha = 0.8f;

    [Header("Sparkles")]
    [SerializeField] private ParticleSystem sparkleParticles;
    [SerializeField] private int sparkleCount = 8;
    [SerializeField] private Color sparkleColor = new Color(1f, 0.9f, 0.3f, 0.8f);

    [Header("Gold Overlay")]
    [SerializeField] private Image goldOverlay;
    [SerializeField] private Color goldTint = new Color(1f, 0.82f, 0.12f, 0.15f);

    private bool isActive = false;
    private RectTransform rectTransform;
    private Material shimmerMaterial;
}
```

Implement:

**Shimmer Effect:**
- Use a `RawImage` or `Image` with a custom material that has a diagonal gradient mask
- Animate a `_ShimmerOffset` property from -0.3 to 1.3 over shimmerDuration
- The gradient: transparent → white (0.3 alpha) → transparent, moving diagonally
- If no custom shader available: use a UI `Image` with animated `rectTransform.anchoredPosition` moving a thin bright strip diagonally across the card
- Repeat every shimmerInterval

**Simpler Shimmer Alternative (no shader):**
```csharp
// Create a thin diagonal Image strip
// Animate its localPosition from bottom-left to top-right
// Use CanvasGroup for fade in/out at edges
IEnumerator ShimmerRoutine()
{
    while (isActive)
    {
        yield return new WaitForSeconds(shimmerInterval);
        // Animate shimmer strip from (-width, -height) to (width, height)
        // Duration: shimmerDuration
        // Alpha: fade in 20%, full 60%, fade out 20% of travel
    }
}
```

**Border Pulse:**
```csharp
void Update()
{
    if (!isActive || borderImage == null) return;
    float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
        (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
    var c = borderImage.color;
    c.a = alpha;
    borderImage.color = c;
}
```

**Sparkle Particles:**
- Small ParticleSystem as child of the card
- Particles: tiny (2-4px), golden, float upward slowly
- Emission: 2-3 per second (subtle, not overwhelming)
- Lifetime: 1-2 seconds
- Shape: rectangle matching card bounds
- Render: Billboard, additive blending

**Activation:**
- `SetActive(bool golden)` — Enable/disable all effects
- `SetCardRect(RectTransform)` — Size sparkle emission to card bounds
- Auto-detect: Check if parent has CardDisplayUI, get card.isGolden

## Files to Modify

### CardDisplayUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardDisplayUI.cs`

- In `Setup(Card)`: If card.isGolden, enable GoldenCardEffect (GetComponent or add)
- If not golden, disable GoldenCardEffect

### CardFrameGenerator.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardFrameGenerator.cs`

- In `ApplyGoldenOverlay(bool)`: Also activate GoldenCardEffect if component exists

## Key Implementation Notes

- **No custom shaders required** — use pure UI Image animation for shimmer
- **Performance**: Only active on golden cards (rare). ParticleSystem pool not needed.
- **Shimmer strip**: Use `Image` with white-to-transparent gradient texture, rotate 45 degrees, mask with card rect
- **Graceful degradation**: All effects null-check their references. If missing, silently skip.
- **Cleanup**: Stop all coroutines and particles in OnDisable()

## Conventions
- MonoBehaviour (not ThemeableUI — gold color is universal)
- Coroutines for shimmer timing, Update() for border pulse (smooth)
- All particle and overlay Images: Raycast Target = false
- New Images as children of the card, above content but below any tooltips
- Project must compile with zero errors
