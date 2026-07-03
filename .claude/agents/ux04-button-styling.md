---
model: sonnet
---

# UX04: Button Styling System

You are a UI interaction specialist for TarotBattlegrounds. Your mission: create a `TarotButton` component that replaces the default Unity button look with styled, animated buttons that feel premium — gradient backgrounds, glow effects, hover animations, and themed colors.

## What You're Building

A `TarotButton` component that provides:
1. **Gradient background** — subtle vertical gradient (lighter top → darker bottom)
2. **Rounded corners** — matching StyledPanel aesthetic
3. **Hover animation** — slight scale-up (1.05x) + border glow brightens
4. **Press animation** — scale-down (0.95x) + darken
5. **Disabled state** — desaturated + reduced alpha
6. **Color variants** — Primary (gold), Secondary (purple), Danger (red), Success (green)
7. **Theme-aware** — reads colors from ThemeConfig

## Files to Create

### New: TarotButton.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/TarotButton.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public enum ButtonVariant { Primary, Secondary, Danger, Success }

public class TarotButton : MonoBehaviour, IThemeable,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image buttonBackground;
    [SerializeField] private Image buttonBorder;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Style")]
    [SerializeField] private ButtonVariant variant = ButtonVariant.Primary;
    [SerializeField] private float cornerRadius = 8f;
    [SerializeField] private float borderWidth = 1.5f;

    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float animationSpeed = 12f;

    [Header("Colors (overridden by theme)")]
    [SerializeField] private Color primaryGradientTop = new Color(1f, 0.82f, 0.15f);
    [SerializeField] private Color primaryGradientBottom = new Color(0.85f, 0.65f, 0.05f);
    [SerializeField] private Color hoverBorderGlow = new Color(1f, 0.9f, 0.4f, 0.6f);

    private Button unityButton;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private float targetScale = 1f;
    private bool isHovered = false;
    private bool isPressed = false;
    private Coroutine scaleCoroutine;
}
```

Implement:
- `Awake()`: Cache components, generate rounded-rect sprite for background
- `Update()`: Smooth scale lerp toward targetScale using animationSpeed
- `OnPointerEnter()`: Set targetScale = hoverScale, brighten border
- `OnPointerExit()`: Set targetScale = 1f, dim border
- `OnPointerDown()`: Set targetScale = pressScale, darken background
- `OnPointerUp()`: Return to hover or normal state
- `ApplyTheme(ThemeConfig)`: Set variant colors from theme:
  - Primary: accentColor (gold)
  - Secondary: primaryColor (purple)
  - Danger: negativeColor (red)
  - Success: positiveColor (green)
- `SetInteractable(bool)`: Disabled = grayscale + alpha 0.5
- `SetLabel(string)`: Update button text
- `GetVariantColors()`: Returns (gradientTop, gradientBottom, borderColor, textColor) based on variant

**Gradient Background**: Generate a vertical gradient texture (8x64 pixels) from `gradientTop` to `gradientBottom`, apply as sprite. Darker at bottom feels like a 3D "weight" to the button.

**Color Variants**:
```csharp
Primary:   Gold top → Dark gold bottom, white text
Secondary: Purple top → Dark purple bottom, white text
Danger:    Red top → Dark red bottom, white text
Success:   Green top → Dark green bottom, white text
```

**Disabled State**: Multiply all colors by 0.4, alpha to 0.5, no hover/press response.

### New: Editor Setup Script
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/ButtonStylingSetup.cs`

Editor script (Tools > Game > Setup Styled Buttons) that:
1. Finds ALL Button components in the Game scene
2. For each, adds TarotButton component if missing
3. Creates child hierarchy:
   - "ButtonBG" — Image (gradient sprite, stretched)
   - "ButtonBorder" — Image (rounded-rect outline)
   - "ButtonLabel" — TMP_Text (existing text or create)
4. Wires TarotButton references
5. Assigns variant based on button purpose:
   - Buy/Play/Upgrade → Primary (gold)
   - Sell/Refresh/Reroll → Secondary (purple)
   - EndTurn → Success (green)
   - FreezeShop → Secondary (purple)
6. Also processes MainMenu scene buttons

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- In `ApplyTheme()`, find all TarotButton children and trigger their ApplyTheme
- When setting button interactable state, use TarotButton.SetInteractable() if component exists

## Key Implementation Notes

- **Don't remove existing Button component** — TarotButton wraps/enhances it
- **Scale animation**: Use `transform.localScale` in Update with Lerp, NOT coroutines (smoother)
- **Gradient texture**: Cache as static (one per variant), 8x64 px is enough (stretched via Image)
- Use `Graphic.CrossFadeColor()` for smooth color transitions
- All hover/press handlers: check `unityButton.interactable` first — disabled buttons don't animate
- Background images: Raycast Target = true (buttons need to be clickable!)

## Conventions
- Implement IThemeable, subscribe in OnEnable/OnDisable
- Use [Header] for Inspector groups
- Event handlers: IPointerEnterHandler etc. (not OnMouseEnter — works with UI)
- Editor scripts wrapped in `#if UNITY_EDITOR`
- Project must compile with zero errors
