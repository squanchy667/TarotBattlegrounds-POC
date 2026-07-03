---
model: sonnet
---

# UX17: Main Menu Redesign

You are a menu design specialist for TarotBattlegrounds. Your mission: transform the bare MainMenu scene into an atmospheric, polished game menu — title placement, mystical background, styled navigation buttons, difficulty selector, and subtle particle ambiance.

## What You're Building

Professional main menu layout:
```
+----------------------------------------+
|                                        |
|        [TAROT BATTLEGROUNDS]           |  ← Title (large, gold, centered)
|           ~ logo slot ~                |
|                                        |
|          [ PLAY SOLO ]                 |  ← Primary button (gold, large)
|          [ MULTIPLAYER ]               |  ← Secondary button (purple)
|          [ RANKED ]                    |  ← Secondary button (purple)
|                                        |
|    [Collection]  [Settings]  [Profile] |  ← Small icon buttons (bottom)
|                                        |
|  Difficulty: [Easy] [Medium] [Hard]    |  ← Toggle group (bottom-left)
|  Players:    [4] [6] [8]              |  ← Toggle group (bottom-left)
+----------------------------------------+
    (mystical particle dust throughout)
```

## Files to Modify

### Primary: MainMenuManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/MainMenuManager.cs`

Add new fields for visual enhancement:
```csharp
[Header("Title")]
[SerializeField] private TMP_Text titleText;
[SerializeField] private TMP_Text subtitleText;
[SerializeField] private Image logoImage;

[Header("Visual")]
[SerializeField] private Image menuBackground;
[SerializeField] private CanvasGroup contentGroup;  // For fade-in on load
```

Add fade-in animation on Start():
- Content starts at alpha 0, fades to 1 over 0.5s
- Title starts slightly above, slides down into position

Enhance button layout:
- Play Solo: large center button (TarotButton.Primary variant if UX04 exists)
- Multiplayer/Ranked: medium buttons below
- Collection/Settings/Profile: small buttons at bottom

### New: MainMenuVisual.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/MainMenuVisual.cs`

Handles menu-specific visuals:
- **Title animation**: Subtle float effect (sine wave Y offset, +-3px, slow)
- **Background**: Reuse BackgroundController (UX02) if available, or add own gradient
- **Difficulty selector**: Styled toggle group with highlight on selected
- **Player count selector**: Same styled toggle group

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/MainMenuSetup.cs`

Editor script (Tools > Game > Setup Main Menu) for MainMenu.unity:
1. Create layout hierarchy:
   - Background (gradient + particles)
   - TitleArea (top 30% — title text, subtitle, logo slot)
   - ButtonArea (center — 3 main buttons, vertically stacked)
   - BottomBar (bottom — small buttons + selectors)
2. Style all buttons with TarotButton or fallback styling
3. Wire MainMenuManager references

## Key Implementation Notes

- **Logo slot**: Empty Image with placeholder. User provides logo sprite later.
- **Title text**: "TAROT BATTLEGROUNDS" in gold, 48pt, bold. If no logo, this IS the logo.
- **Subtitle**: Optional tagline ("A Mystical Auto-Battler"), smaller, light gray
- **Background**: Use BackgroundController if exists, otherwise create own gradient
- **Particle dust**: Same as BackgroundController particles (golden specks)
- **Difficulty/Player count buttons**: When selected, button is brighter. Others are dim.
- **Scene**: This is for MainMenu.unity, NOT Game.unity

## Conventions
- Work in MainMenu scene (Assets/Scenes/MainMenu.unity)
- Don't break existing button OnClick wiring — only enhance visuals
- Fade-in: use CanvasGroup.alpha for clean fade
- All decorative elements: Raycast Target = false
- Keep existing auth flow (solo/multiplayer/ranked routing)
- Project must compile with zero errors
