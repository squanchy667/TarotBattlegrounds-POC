---
model: sonnet
---

# UX16: Combat Arena Visual

You are a combat scene specialist for TarotBattlegrounds. Your mission: enhance the combat replay view with a battlefield feel — center divider, team labels, positional slot indicators, and attack trail lines.

## What You're Building

Combat arena visual enhancements:
1. **Center divider**: Glowing horizontal line separating attacker/defender boards
2. **Team labels**: "YOUR BOARD" / "OPPONENT" with colored backgrounds
3. **Slot indicators**: Subtle numbered positions (1-7) below each card slot
4. **Attack trail**: Brief line/arc drawn from attacker to defender during attack animation
5. **Arena background**: Darker gradient behind combat area, distinct from recruit phase

## Files to Create

### New: CombatArenaVisual.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Combat/Animator/CombatArenaVisual.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CombatArenaVisual : MonoBehaviour
{
    [Header("Divider")]
    [SerializeField] private Image dividerLine;
    [SerializeField] private float dividerGlowSpeed = 1.5f;
    [SerializeField] private Color dividerColor = new Color(0.55f, 0.3f, 0.75f, 0.6f);

    [Header("Team Labels")]
    [SerializeField] private TMP_Text attackerLabel;
    [SerializeField] private TMP_Text defenderLabel;
    [SerializeField] private Image attackerLabelBg;
    [SerializeField] private Image defenderLabelBg;

    [Header("Attack Trail")]
    [SerializeField] private Image attackTrailLine;
    [SerializeField] private float trailDuration = 0.3f;
    [SerializeField] private Color trailColor = new Color(1f, 0.4f, 0.2f, 0.8f);

    [Header("Arena Background")]
    [SerializeField] private Image arenaBackground;
    [SerializeField] private Color arenaColor = new Color(0.03f, 0.02f, 0.06f, 0.9f);

    public void ShowArena(string attackerName, string defenderName);
    public void HideArena();
    public void ShowAttackTrail(Vector3 from, Vector3 to);
}
```

Implement:
- **Divider**: Thin Image (2px height, full width) with pulsing alpha via sine wave
- **Attack trail**: Temporarily show a line Image stretched and rotated between attacker/defender positions, fade out over trailDuration
- **Arena BG**: Semi-transparent dark overlay that activates during combat
- **Labels**: "YOUR BOARD" top, "OPPONENT" bottom (or vice versa)

## Files to Modify

### CombatAnimator.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Combat/Animator/CombatAnimator.cs`

- Add `[SerializeField] private CombatArenaVisual arenaVisual;`
- On combat start: `arenaVisual?.ShowArena(attackerName, defenderName)`
- On each attack: `arenaVisual?.ShowAttackTrail(attackerPos, defenderPos)`
- On combat end: `arenaVisual?.HideArena()`

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/CombatArenaSetup.cs`

Create arena visual elements in Game scene combat panel.

## Conventions
- All overlays: Raycast Target = false
- Attack trail: rotate Image to point from→to, stretch width to distance
- Line rendering via stretched thin Image (simpler than LineRenderer for UI)
- Project must compile with zero errors
