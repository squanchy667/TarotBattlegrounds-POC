---
model: sonnet
---

# UX06: Card Typography & Layout

You are a card layout specialist for TarotBattlegrounds. Your mission: rework the CardDisplayUI layout for a proper card game feel — ATK bottom-left, HP bottom-right (like Hearthstone), name banner at top, cost gem top-left, tribe badge bottom-center.

## What You're Building

Professional card layout with clear visual hierarchy:
```
+---------------------------+
| [3] Card Name             |  ← Cost gem (top-left), Name banner
|                           |
|      [Card Artwork]       |  ← Centered artwork area
|                           |
|      [Ability Text]       |  ← Small ability description
|                           |
|  [2]    Pentacles    [5]  |  ← ATK (bottom-left), Tribe (center), HP (bottom-right)
+---------------------------+
```

## Files to Modify

### Primary: CardDisplayUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CardDisplayUI.cs`

Current layout is basic text stacking. Redesign the `Setup()` and display methods:

**Add new fields:**
```csharp
[Header("Layout - Stat Badges")]
[SerializeField] private Image attackBadge;        // Red circle behind ATK number
[SerializeField] private Image healthBadge;        // Green circle behind HP number
[SerializeField] private Image costBadge;          // Blue/gold circle behind cost

[Header("Layout - Sections")]
[SerializeField] private RectTransform nameBanner;     // Top strip for name
[SerializeField] private RectTransform artworkArea;    // Center area
[SerializeField] private RectTransform abilityArea;    // Below artwork
[SerializeField] private RectTransform statBar;        // Bottom bar

[Header("Typography")]
[SerializeField] private float nameSize = 16f;
[SerializeField] private float statSize = 22f;
[SerializeField] private float costSize = 18f;
[SerializeField] private float abilitySize = 11f;
[SerializeField] private TMP_Text abilityText;
```

**Enhance stat display:**
- ATK and HP: Bold numbers inside colored circular badges
- ATK badge: dark red circle (0.7, 0.15, 0.15) with sword-like decoration
- HP badge: dark green circle (0.15, 0.55, 0.15) with heart-like shape
- Cost badge: blue-gold circle at top-left corner, slightly overlapping frame
- Numbers: white, bold, large (22pt), centered in badges

**Enhance name display:**
- Name banner: semi-transparent dark strip at top of card
- Name text: centered, white, bold, 16pt
- Golden cards: name in gold color with "* " prefix (existing)

**Add ability text:**
- Below artwork area, above stat bar
- Small font (11pt), italic, muted color (light gray)
- Show first ability's trigger type + short description
- Truncate with "..." if too long

**Enhance tribe display:**
- Bottom-center between ATK and HP
- Show tribe name in tribe color
- Small font (12pt), all-caps

### New: StatBadge.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/StatBadge.cs`

Small component for stat circles:
```csharp
public class StatBadge : MonoBehaviour
{
    [SerializeField] private Image badgeBackground;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Color badgeColor;

    public void SetValue(int value, bool isBuff = false);
    // isBuff=true → show in bright green to indicate buffed stat
}
```

Generate circular badge texture procedurally (32x32 circle with slight shadow).

**Buffed stat indication:**
- If current ATK > base ATK: show ATK in bright green instead of white
- If current HP > base HP: show HP in bright green
- If current HP < base HP (damaged): show HP in red

### New: Editor Setup Script
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/CardLayoutSetup.cs`

Editor script (Tools > Game > Setup Card Layout) that restructures CardDisplayUI prefab:
1. Create layout zones (name banner, artwork area, stat bar)
2. Create stat badges (ATK, HP, Cost) with circular images
3. Position elements using RectTransform anchors:
   - Name banner: top, stretch horizontal, 24px height
   - Artwork area: center, stretch both, with margins
   - Stat bar: bottom, stretch horizontal, 28px height
   - ATK badge: bottom-left corner, 28x28
   - HP badge: bottom-right corner, 28x28
   - Cost badge: top-left corner, 24x24
4. Wire all references to CardDisplayUI

## Key Implementation Notes

- **Don't break existing Setup() API** — same `Setup(Card, int, Action<int>)` signature
- **Graceful degradation**: If new fields are null, fall back to existing flat layout
- **Badge textures**: Generate once, cache as static Texture2D
- **Buffed stats**: Compare card.attack vs card._baseAttack (need to expose or access base stats)
- **Font sizes**: Use the existing min font size fields as floors
- **Ability text**: Get from `card.abilities` list, format as "{TriggerType}: {Description}"

## Conventions
- Don't change public method signatures
- New fields: [SerializeField] private with [Header]
- Text: always TMP_Text (never legacy UnityEngine.UI.Text)
- Colors from ThemeConfig where possible
- Wrap Editor scripts in `#if UNITY_EDITOR`
- Project must compile with zero errors
