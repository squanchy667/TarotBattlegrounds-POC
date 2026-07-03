---
model: sonnet
---

# UX19: Discovery Popup Design

You are a popup/modal specialist for TarotBattlegrounds. Your mission: make the triple-fusion discovery popup feel rewarding — 3 cards fan out with glow, mystical frame around the popup, "CHOOSE ONE" banner, and selection highlight.

## What You're Building

Rewarding discovery experience:
1. **Backdrop**: Dark overlay (alpha 0.7) behind popup
2. **Mystical frame**: Ornate border around the popup area (procedural)
3. **"DISCOVER" banner**: Gold text banner at top, slides in
4. **Card fan**: 3 cards arranged in slight fan pattern (tilted -10, 0, +10 degrees)
5. **Card entrance**: Cards scale from 0 to 1.0 with stagger (0.15s between each)
6. **Hover highlight**: Hovered card glows and lifts up
7. **Selection flash**: Chosen card flashes bright, others fade out, popup closes

## Files to Modify

### Primary: DiscoveryUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/DiscoveryUI.cs`

Add fields:
```csharp
[Header("Visual Enhancement")]
[SerializeField] private Image backdrop;
[SerializeField] private Image mysticalFrame;
[SerializeField] private TMP_Text discoverBanner;
[SerializeField] private CanvasGroup popupGroup;

[Header("Card Fan")]
[SerializeField] private float fanAngle = 10f;           // Degrees of tilt
[SerializeField] private float cardSpacing = 160f;
[SerializeField] private float staggerDelay = 0.15f;
[SerializeField] private float cardScaleSpeed = 0.3f;

[Header("Colors")]
[SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.7f);
[SerializeField] private Color bannerColor = new Color(1f, 0.82f, 0.12f);
[SerializeField] private Color selectionFlash = new Color(1f, 1f, 1f, 0.8f);
```

Enhance show flow:
1. Backdrop fades in (0.2s)
2. Banner slides down from above (0.3s)
3. Cards appear one by one with scale-from-zero (stagger 0.15s each)
4. Cards tilted: left card -fanAngle, center 0, right +fanAngle
5. On hover: card scales to 1.1x, lifts Y by 15px, border glows
6. On select: flash white, other cards shrink + fade, popup closes after 0.5s

**Fan layout:**
```csharp
void LayoutCards()
{
    float[] rotations = { -fanAngle, 0f, fanAngle };
    float[] xOffsets = { -cardSpacing, 0f, cardSpacing };
    for (int i = 0; i < 3; i++)
    {
        var rt = cardSlots[i].GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(xOffsets[i], 0);
        rt.localRotation = Quaternion.Euler(0, 0, rotations[i]);
    }
}
```

**Mystical frame**: Procedural rounded-rect with double border and corner flourishes (inner border + outer glow).

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/DiscoverySetup.cs`

Setup the discovery popup visual hierarchy.

## Conventions
- Backdrop blocks all clicks behind it (Raycast Target = true)
- Card slots: reuse existing CardDisplayUI prefab
- All animations in coroutines
- Stagger with WaitForSeconds between each card
- Project must compile with zero errors
