---
model: sonnet
---

# UX08: Card Hover & Selection Polish

You are a card interaction specialist for TarotBattlegrounds. Your mission: enhance the hover and selection feedback — smooth scale-up with shadow on hover, animated glowing outline on selection, and ice-blue shimmer for frozen cards.

## What You're Building

Three interaction states with smooth transitions:
1. **Hover**: Card scales up (1.08x), rises slightly (Y offset), gains drop shadow
2. **Selection**: Animated pulsing glow border (gold for selected, green for playable)
3. **Frozen**: Ice-blue border with subtle frost shimmer effect

## Files to Create

### New: CardInteractionFeedback.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardInteractionFeedback.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.12f, 0.6f);
    [SerializeField] private Color playableColor = new Color(0.3f, 0.85f, 0.3f, 0.5f);
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMinAlpha = 0.2f;
    [SerializeField] private float glowMaxAlpha = 0.7f;

    [Header("Frozen")]
    [SerializeField] private Image frozenOverlay;
    [SerializeField] private Color frozenColor = new Color(0.3f, 0.55f, 1f, 0.2f);
    [SerializeField] private float frozenShimmerSpeed = 0.8f;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private float targetYOffset = 0f;
    private bool isHovered = false;
    private bool isSelected = false;
    private bool isFrozen = false;
    private CanvasGroup shadowGroup;
}
```

Implement:

**Hover Effect:**
```csharp
void Update()
{
    // Smooth scale
    float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, Time.deltaTime * hoverSpeed);
    transform.localScale = originalScale * currentScale;

    // Smooth Y offset
    float currentY = Mathf.Lerp(rectTransform.anchoredPosition.y - originalPosition.y, targetYOffset,
        Time.deltaTime * hoverSpeed);
    rectTransform.anchoredPosition = originalPosition + Vector3.up * currentY;

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
```

**OnPointerEnter/Exit:**
- Enter: `targetScale = hoverScale`, `targetYOffset = hoverYOffset`, `isHovered = true`
- Exit: `targetScale = 1f`, `targetYOffset = 0f`, `isHovered = false`
- Set sibling index to top on hover (so hovered card draws above neighbors)
- Restore sibling index on exit

**Selection API:**
- `SetSelected(bool selected)`: Toggle selection glow
- `SetFrozen(bool frozen)`: Toggle frozen overlay
- `SetPlayable(bool playable)`: Switch glow color to green (can play this card)

**Drop Shadow:**
- Slightly larger than card, offset down-right by (3, -3) pixels
- Color: black with 0.4 alpha when hovered, 0 when not
- Generate as slightly larger rounded-rect Image behind the card

## Files to Modify

### CardDisplayUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardDisplayUI.cs`

- In `SetSelected(bool)`: Also trigger CardInteractionFeedback.SetSelected() if component exists
- In `SetFrozen(bool)`: Also trigger CardInteractionFeedback.SetFrozen() if component exists
- Remove the basic color-swap selection logic (delegate to CardInteractionFeedback)
- Keep backward compat: if CardInteractionFeedback not present, use old color swap

### ShopCardUI.cs, HandCardUI.cs, BoardCardUI.cs
Same pattern: integrate CardInteractionFeedback where selection/frozen states are managed.

Located at:
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/ShopCardUI.cs`
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/HandCardUI.cs`
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/BoardCardUI.cs`

## Key Implementation Notes

- **Sibling index trick**: `transform.SetAsLastSibling()` on hover makes card draw on top
- **Original position**: Capture in `Awake()` or `Start()`, NOT in `OnPointerEnter`
- **Drop shadow texture**: Generate a rounded-rect with Gaussian blur (or just soft edges via SDF)
- **Performance**: All animation in Update() with Lerp (no coroutines, no allocations)
- **Frozen overlay**: Semi-transparent ice-blue Image, same size as card, Raycast Target = false
- **Selection glow**: Slightly larger than card (2-3px padding), soft edges, Raycast Target = false

## Conventions
- MonoBehaviour, not ThemeableUI (interaction feedback is theme-independent)
- All overlays: Raycast Target = false
- Smooth everything with Lerp in Update (never snap)
- Null-check all optional Image references
- Project must compile with zero errors
