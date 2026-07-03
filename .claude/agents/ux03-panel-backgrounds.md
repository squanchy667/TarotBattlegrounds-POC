---
model: sonnet
---

# UX03: Panel Backgrounds

You are a UI panel specialist for TarotBattlegrounds. Your mission: add semi-transparent, rounded-corner panel backgrounds with subtle borders to the Shop, Hand, Board, and Info panels — turning flat invisible containers into visually distinct areas.

## What You're Building

A reusable `StyledPanel` component that gives any panel:
1. **Semi-transparent dark background** (dark purple, alpha ~0.7)
2. **Rounded corners** (procedural rounded-rect texture)
3. **Subtle glowing border** (1-2px, accent-colored with low opacity)
4. **Optional header bar** (slightly lighter strip at top for title)
5. **Theme-aware** — colors from ThemeConfig

## Files to Create

### New: StyledPanel.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/StyledPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

public class StyledPanel : MonoBehaviour, IThemeable
{
    [Header("Panel Style")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image panelBorder;
    [SerializeField] private Image headerBar;

    [Header("Settings")]
    [SerializeField] private float backgroundAlpha = 0.7f;
    [SerializeField] private float borderAlpha = 0.3f;
    [SerializeField] private float cornerRadius = 12f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private bool showHeader = true;
    [SerializeField] private float headerHeight = 32f;

    [Header("Colors (overridden by theme)")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.05f, 0.14f, 0.7f);
    [SerializeField] private Color borderColor = new Color(0.55f, 0.3f, 0.75f, 0.3f);
    [SerializeField] private Color headerColor = new Color(0.12f, 0.08f, 0.20f, 0.8f);
}
```

Implement:
- `Awake()`: Generate rounded-rect textures procedurally (background + border outline)
- `GenerateRoundedRect(int width, int height, float radius, Color fill, Color border, float borderWidth)`: Creates a `Texture2D` with smooth rounded corners using SDF (signed distance field) for anti-aliased edges
- `ApplyTheme(ThemeConfig)`: Derive panel colors from theme:
  - Background = `theme.secondaryColor` with `backgroundAlpha`
  - Border = `theme.primaryColor` with `borderAlpha`
  - Header = slightly lighter than background
- `SetHeaderVisible(bool)`: Show/hide header bar
- Static utility `CreateRoundedRectSprite(...)` for reuse across the project

### New: Editor Setup Script
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/PanelBackgroundSetup.cs`

Editor script (Tools > Game > Setup Panel Backgrounds) that:
1. Finds ShopUI, HandUI, BoardUI, GameUIManager in Game scene
2. For each panel, inserts background layers as first children:
   - "PanelBG" — Image with StyledPanel, stretched to fill parent
   - "PanelBorder" — Image outline on top of BG
   - "PanelHeader" — Image at top for title area
3. Wires references to StyledPanel component
4. Sets Raycast Target = false on all background images
5. Preserves existing child layout (content goes on top of background)

## Files to Modify

### ShopUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/ShopUI.cs`

- Has existing `panelBackground` Image field — integrate with StyledPanel
- In `ApplyTheme()`, if StyledPanel exists on the panel, let it handle background

### HandUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/HandUI.cs`

Same integration as ShopUI.

### BoardUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/BoardUI.cs`

Same integration as ShopUI.

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- Apply StyledPanel to the main info panel (`mainPanelBackground`)
- In `ApplyTheme()`, update any StyledPanel references

## Rounded-Rect Generation Algorithm

```csharp
// For each pixel (x, y) in texture:
// 1. Calculate distance to nearest rounded-rect edge using SDF
// 2. If inside fill area: fill color
// 3. If in border zone (within borderWidth of edge): border color
// 4. If outside: transparent
// 5. Anti-alias by smoothstep on the distance

float RoundedRectSDF(Vector2 point, Vector2 halfSize, float radius)
{
    Vector2 d = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfSize + Vector2.one * radius;
    return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude - radius;
}
```

Generate textures at 256x256 resolution, use `Sprite.Create()` with proper border for 9-slice scaling. This ensures panels scale to any size without distortion.

## Conventions
- Implement IThemeable, subscribe in OnEnable/OnDisable
- Generated textures: cache in static dictionary to avoid regeneration
- All background Images: Raycast Target = false
- Use [Header] attributes for Inspector organization
- StyledPanel should work standalone (doesn't require ShopUI/HandUI/BoardUI)
- Wrap Editor scripts in `#if UNITY_EDITOR`
- Project must compile with zero errors
