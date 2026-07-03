---
model: sonnet
---

# UX05: Card Frame Redesign

You are a card visual specialist for TarotBattlegrounds. Your mission: transform the single-layer colored border into a layered, premium card frame — outer border, inner frame, tribe accent stripe, and tier gem indicator. All procedural (no sprites required).

## What You're Building

A multi-layered card frame system:
1. **Outer border** — Tier-colored frame (2px, bold)
2. **Inner frame** — Slightly lighter inner border (1px, subtle)
3. **Tribe accent stripe** — Thin colored bar at bottom of card (tribe color)
4. **Tier indicator** — Small gem/diamond shapes at top-right showing tier (1-6 dots)
5. **Inner shadow** — Subtle darkening at edges for depth
6. **Smooth corners** — Rounded rectangle aesthetic

## Files to Modify

### Primary: CardFrameGenerator.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardFrameGenerator.cs`

Current structure (4 Image fields):
- `frameImage` — The border frame
- `backgroundImage` — Card background
- `goldenOverlay` — Golden tint for triples
- `tribeAccentBar` — Tribe color stripe

**Expand to support layered frame:**

Add new fields:
```csharp
[Header("Frame Layers")]
[SerializeField] private Image outerFrame;        // Tier-colored outer border
[SerializeField] private Image innerFrame;        // Subtle inner border
[SerializeField] private Image innerShadow;       // Edge darkening overlay
[SerializeField] private Image tierIndicator;     // Tier gem display

[Header("Frame Settings")]
[SerializeField] private float outerBorderWidth = 3f;
[SerializeField] private float innerBorderWidth = 1f;
[SerializeField] private float accentBarHeight = 6f;
```

**Enhance `ApplyCardVisuals(Card card)`:**
1. Apply outer frame color from tier (existing `ApplyRarityFrame`)
2. Apply inner frame — same color but 30% brighter
3. Apply tribe background (existing `ApplyTribeBackground`)
4. Apply tribe accent stripe at bottom (existing)
5. Apply inner shadow — gradient overlay that darkens edges by 15%
6. Apply tier indicator — generate small dots/gems texture
7. Apply golden overlay if golden (existing, but enhance glow)

**New method: `GenerateFrameTexture()`**
Generate a procedural rounded-rect frame texture with:
- Outer border at specified width and tier color
- Inner border at 1px and lighter color
- Fill area transparent (so card content shows through)
- Anti-aliased edges
- Cache per tier (6 textures total, generated once)

**New method: `GenerateTierGems(int tier)`**
Generate a small texture (64x16) with tier number of diamond shapes:
- Diamond shapes in tier color, arranged horizontally
- Background transparent
- Tier 6 = 6 small golden diamonds in a row

**Enhance golden overlay:**
- Instead of flat 25% alpha overlay, create a gradient that's brighter at edges
- This creates a "glowing frame" effect for golden cards

### Secondary: CardDisplayUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardDisplayUI.cs`

- In `ApplyCardFrame()`, if CardFrameGenerator exists on this object, delegate to it
- Add support for the new tier indicator display
- Ensure new frame layers don't overlap with text/stats

### New: Editor Setup Script
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/CardFrameSetup.cs`

Editor script (Tools > Game > Setup Card Frames) that:
1. Finds all CardDisplayUI / CardFrameGenerator in scene
2. Ensures all required Image children exist:
   - "OuterFrame" — stretched to fill, behind content
   - "InnerFrame" — stretched with padding, behind content
   - "InnerShadow" — stretched to fill, on top of background
   - "TierGems" — small area at top-right corner
3. Wires new references to CardFrameGenerator

## Implementation Notes

- **Procedural textures**: Generate at 128x128, use 9-slice `Sprite.Create()` with proper borders
- **Cache textures**: Use `static Dictionary<int, Texture2D>` keyed by tier
- **Anti-aliasing**: Use smoothstep for rounded corners (SDF-based)
- **Keep backward compat**: If `outerFrame` etc. are null, fall back to existing `frameImage` behavior
- **Performance**: Texture generation in Awake(), not Update()

## Conventions
- Don't change existing public API of CardFrameGenerator
- New fields: [SerializeField] private with [Header]
- Null-check all new Image references (graceful degradation)
- Wrap Editor scripts in `#if UNITY_EDITOR`
- Project must compile with zero errors
