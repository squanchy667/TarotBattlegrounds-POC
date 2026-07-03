---
model: haiku
---

# UX18: Game Over Screen Polish

You are a results screen specialist. Your mission: polish the Game Over panel — placement display with medal colors (gold/silver/bronze), standings list, animated entry, and styled Play Again / Quit buttons.

## What You're Building

Polished game over overlay:
1. **Placement badge**: Large "1st" / "2nd" / "3rd" etc. with medal color (gold/silver/bronze/gray)
2. **Standings list**: All players ranked with health/turn info
3. **Animated entry**: Panel scales up from 0.8x to 1.0x with fade-in (0.4s)
4. **Styled buttons**: Play Again (gold/primary) and Quit (secondary/gray)

## Files to Modify

### Primary: GameOverUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameOverUI.cs`

Add fields:
```csharp
[Header("Placement")]
[SerializeField] private Image placementBadge;
[SerializeField] private TMP_Text placementNumber;
[SerializeField] private Color goldMedal = new Color(1f, 0.82f, 0.12f);
[SerializeField] private Color silverMedal = new Color(0.78f, 0.78f, 0.85f);
[SerializeField] private Color bronzeMedal = new Color(0.8f, 0.5f, 0.2f);

[Header("Animation")]
[SerializeField] private CanvasGroup panelGroup;
[SerializeField] private float animDuration = 0.4f;
```

Enhance `Show()` method:
- Set placementBadge color based on placement (1=gold, 2=silver, 3=bronze, 4+=gray)
- Animate: start at scale 0.8, alpha 0, lerp to scale 1.0, alpha 1.0 over animDuration
- Placement number: "1st", "2nd", "3rd", "4th"... with ordinal suffix
- Panel background: dark semi-transparent with border (reuse StyledPanel if available)

## Conventions
- Overlay: high sort order, dark background behind panel (blocks game view)
- Medal badge: circular Image (64x64) with number on top
- Standings: simple vertical list, each row: rank + name + health
- Project must compile with zero errors
