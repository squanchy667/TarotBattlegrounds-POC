---
model: sonnet
---

# UX02: Game Background

You are a visual atmosphere specialist for TarotBattlegrounds. Your mission: replace the flat solid-color background with a rich, layered procedural background that creates a mystical tavern atmosphere.

## What You're Building

A multi-layered background system:
1. **Base gradient** — Dark radial gradient (deep purple center → near-black edges)
2. **Vignette overlay** — Darkened edges to draw focus to center
3. **Ambient particle dust** — Slow-floating mystical particles (golden specks)
4. **Optional image slot** — For user-provided background art (falls back to procedural)

## Files to Create

### New: BackgroundController.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/BackgroundController.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;

public class BackgroundController : MonoBehaviour, IThemeable
{
    [Header("Layers")]
    [SerializeField] private Image backgroundBase;        // Solid base
    [SerializeField] private Image gradientOverlay;       // Radial gradient
    [SerializeField] private Image vignetteOverlay;       // Edge darkening
    [SerializeField] private ParticleSystem ambientDust;  // Floating particles

    [Header("Settings")]
    [SerializeField] private bool useProceduralBackground = true;
    [SerializeField] private Sprite customBackgroundSprite;

    [Header("Gradient")]
    [SerializeField] private Color gradientCenter = new Color(0.08f, 0.05f, 0.14f);
    [SerializeField] private Color gradientEdge = new Color(0.02f, 0.01f, 0.05f);

    [Header("Particles")]
    [SerializeField] private Color dustColor = new Color(1f, 0.82f, 0.12f, 0.15f);
    [SerializeField] private int dustCount = 40;
    [SerializeField] private float dustSpeed = 8f;
    [SerializeField] private float dustSize = 3f;
}
```

Implement:
- `Start()`: Create gradient texture procedurally (512x512 radial gradient), apply to gradientOverlay
- `SetupParticles()`: Configure particle system — slow upward drift with slight horizontal sway, long lifetime (8-12s), small soft circles, very low alpha (0.1-0.2)
- `ApplyTheme(ThemeConfig)`: Read gameBackgroundColor for base, derive gradient from it
- Support `customBackgroundSprite` override: if set, use that instead of procedural gradient
- Vignette: Create a procedural texture with transparent center → black edges (alpha 0-0.6)

### New: Editor Setup Script
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/BackgroundSetup.cs`

Editor script (Tools > Game > Setup Background) that:
1. Finds/creates Canvas in Game scene
2. Adds background layers as first children (behind all UI):
   - "BG_Base" — fullscreen Image, Raycast Target = false
   - "BG_Gradient" — fullscreen Image, Raycast Target = false
   - "BG_Vignette" — fullscreen Image, Raycast Target = false
   - "BG_Particles" — ParticleSystem (World Space, in front of BG but behind UI)
3. Adds BackgroundController component, wires all references
4. Also set up for MainMenu scene

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- The existing `gameBackgroundImage` field should be integrated with BackgroundController
- In `ApplyTheme()`, if BackgroundController exists, delegate background theming to it

## Key Patterns

- Use `Texture2D` + `SetPixels` for procedural gradient generation at runtime
- Cache generated textures (create once in Start, don't regenerate per frame)
- ParticleSystem: use `Simulation Space = World`, `Render Mode = Billboard`
- All Images: `Raycast Target = false` (background shouldn't block clicks)
- Wrap Editor script in `#if UNITY_EDITOR`
- Implement IThemeable interface for theme integration

## Particle Settings Guide
```
Shape: Box (screen width x screen height)
Emission: Rate over Time = dustCount / lifetime
Start Lifetime: 8-12 (random between two)
Start Speed: dustSpeed (very slow, ~5-10)
Start Size: dustSize (2-4)
Start Color: dustColor (gold, very low alpha)
Gravity Modifier: -0.02 (slight upward drift)
Noise: Strength 0.5, Frequency 0.3 (gentle sway)
Renderer: Default-Particle material, additive blending
```

## Conventions
- Inherit from MonoBehaviour, implement IThemeable
- Subscribe to ThemeManager.OnThemeChanged in OnEnable, unsubscribe in OnDisable
- All new UI elements: Raycast Target = false for backgrounds
- Use [Header] attributes to organize Inspector fields
- Project must compile with zero errors
