---
model: sonnet
---

# UX01: Theme Palette Overhaul

You are a color/theming specialist for TarotBattlegrounds. Your mission: transform the default ThemeConfig into a rich, mystical tarot palette that feels premium and atmospheric.

## What You're Changing

The current palette is functional but flat. You'll create a deep, immersive mystical color scheme with:
- Deep midnight purples and navy blues as base
- Rich gold and amber accents
- Jewel tones for tribes (emerald, sapphire, ruby, amber, celestial blue, antique gold)
- High contrast for readability
- Warm glow feel throughout

## Files to Modify

### Primary: ThemeConfig.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Theme/ThemeConfig.cs`

Update ALL default color values in the field declarations:

**Current → New values to set:**

```csharp
// Background & Base
gameBackgroundColor: (0.1, 0.08, 0.15) → (0.05, 0.03, 0.10)  // Deeper midnight
secondaryColor: (0.2, 0.2, 0.3) → (0.12, 0.08, 0.18)          // Rich dark purple
cardBackgroundColor: (0.15, 0.15, 0.2) → (0.10, 0.07, 0.16)    // Dark card base

// Primary & Accent
primaryColor: (0.6, 0.4, 0.8) → (0.55, 0.3, 0.75)             // Deeper mystical purple
accentColor: (1, 0.8, 0.2) → (1.0, 0.78, 0.15)                // Richer gold

// Feedback
positiveColor: (0.3, 0.8, 0.3) → (0.2, 0.85, 0.4)             // Brighter emerald
negativeColor: (0.9, 0.3, 0.3) → (0.95, 0.25, 0.25)           // Crisper red

// Golden cards
goldenCardColor: (1, 0.85, 0.2, 1) → (1.0, 0.82, 0.12, 1.0)  // Deeper gold
```

**Tribe colors (in CreateDefaultTarotTheme or TribeThemeData defaults):**
```csharp
Pentacles: (0.85, 0.65, 0.2)  → (0.82, 0.68, 0.15)   // Antique gold
Cups:      (0.3, 0.5, 0.9)    → (0.25, 0.45, 0.95)    // Deep sapphire
Swords:    (0.75, 0.75, 0.85)  → (0.78, 0.78, 0.90)    // Bright silver
Wands:     (0.9, 0.4, 0.2)    → (0.92, 0.45, 0.12)    // Burning amber
Stars:     (0.6, 0.8, 1.0)    → (0.55, 0.75, 1.0)     // Celestial blue
Coins:     (0.95, 0.85, 0.3)  → (0.90, 0.80, 0.20)    // Rich coin gold
```

### Secondary: CardFrameGenerator.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardFrameGenerator.cs`

Update the static tier frame colors and tribe background colors:

**Tier Frame Colors** (make more vibrant):
```csharp
Tier 1: (0.6, 0.6, 0.6)   → (0.55, 0.55, 0.60)   // Cool gray
Tier 2: (0.2, 0.7, 0.3)   → (0.15, 0.75, 0.30)   // Rich green
Tier 3: (0.2, 0.4, 0.9)   → (0.20, 0.45, 0.95)   // Vivid blue
Tier 4: (0.6, 0.2, 0.8)   → (0.65, 0.18, 0.85)   // Royal purple
Tier 5: (1, 0.6, 0.1)     → (1.0, 0.55, 0.05)    // Blazing orange
Tier 6: (1, 0.85, 0.2)    → (1.0, 0.82, 0.10)    // Legendary gold
```

**Tribe Background Colors** (deeper, moodier):
```csharp
Pentacles: (0.15, 0.25, 0.12) → (0.12, 0.18, 0.08)
Cups:      (0.12, 0.18, 0.3)  → (0.08, 0.12, 0.25)
Swords:    (0.3, 0.12, 0.12)  → (0.22, 0.08, 0.10)
Wands:     (0.3, 0.18, 0.08)  → (0.25, 0.12, 0.05)
Stars:     (0.25, 0.22, 0.1)  → (0.18, 0.18, 0.08)
Coins:     (0.28, 0.22, 0.08) → (0.22, 0.18, 0.05)
Default:   (0.15, 0.15, 0.18) → (0.10, 0.08, 0.14)
```

### Tertiary: CardDisplayUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardDisplayUI.cs`

Update the hardcoded fallback colors:
- `normalColor` default: (0.2, 0.2, 0.2) → (0.10, 0.07, 0.16)
- `selectedColor` default: (0.3, 0.5, 0.3) → (0.55, 0.3, 0.75)
- `frozenBorderColor` static: (0.3, 0.6, 1, 1) → (0.3, 0.55, 1.0, 1.0)

Also update ShopCardUI, BoardCardUI, HandCardUI default colors:
- `ShopCardUI.cs` normalColor/selectedColor
- `BoardCardUI.cs` normalColor/selectedColor
- `HandCardUI.cs` normalColor/selectedColor

These are at:
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/ShopCardUI.cs`
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/BoardCardUI.cs`
- `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/HandCardUI.cs`

## Conventions
- All colors use Unity `Color` (0-1 floats, not 0-255)
- Use `new Color(r, g, b)` for RGB, `new Color(r, g, b, a)` for RGBA
- Don't change any method signatures, field names, or class structure
- Only modify color values and string defaults
- Test: Project should compile with zero errors after changes
