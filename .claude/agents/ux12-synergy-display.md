---
model: sonnet
---

# UX12: Synergy Display Panel

You are a HUD specialist for TarotBattlegrounds. Your mission: create a visual synergy tracker showing all 6 tribes with filled/unfilled pip indicators for the 2/4/6 thresholds. Active synergies glow, inactive ones are dim.

## What You're Building

A compact side panel showing tribe synergy progress:
```
[Pentacles icon] ●●○ (2/6)  — active glow
[Cups icon]      ○○○ (0/6)  — dim
[Swords icon]    ●●●● (4/6) — active glow
[Wands icon]     ●○○ (1/6)  — dim
[Stars icon]     ●●○ (2/6)  — active glow
[Coins icon]     ○○○ (0/6)  — dim
```

Each row:
- Tribe color dot/icon (left)
- 3 threshold pips: ● filled (at 2), ● filled (at 4), ● filled (at 6)
- Count text (right)
- Active rows: glow effect, brighter colors
- Inactive rows: dim, gray tones

## Files to Create

### New: SynergyDisplayPanel.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/SynergyDisplayPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SynergyDisplayPanel : MonoBehaviour, IThemeable
{
    [Header("Panel")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Transform rowContainer;

    [Header("Row Prefab References")]
    [SerializeField] private GameObject synergyRowPrefab;

    [Header("Colors")]
    [SerializeField] private Color activeGlow = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color inactiveDim = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color pipFilled = new Color(1f, 1f, 1f);
    [SerializeField] private Color pipEmpty = new Color(0.3f, 0.3f, 0.3f);

    [Header("Settings")]
    [SerializeField] private float rowHeight = 28f;
    [SerializeField] private float pipSize = 10f;
    [SerializeField] private float iconSize = 20f;

    private Dictionary<TribeType, SynergyRow> rows = new Dictionary<TribeType, SynergyRow>();

    private class SynergyRow
    {
        public Image tribeIcon;
        public Image[] pips = new Image[3]; // 2, 4, 6 thresholds
        public TMP_Text countText;
        public Image rowBackground;
        public Image glowOverlay;
        public TribeType tribe;
        public int currentCount;
    }
}
```

Implement:

**Initialization:**
- Create 6 rows (one per TribeType: Pentacles, Cups, Swords, Wands, Stars, Coins)
- Each row: HorizontalLayoutGroup with icon + 3 pips + count text
- Generate tribe icon as small filled circle in tribe color (or use sprite if available)
- Pips: small circles (10px), filled = tribe color, empty = gray

**UpdateSynergies(Player player):**
```csharp
public void UpdateSynergies(Player player)
{
    // Count tribes on player's board
    var tribeCounts = new Dictionary<TribeType, int>();
    foreach (var card in player.board)
    {
        if (card == null) continue;
        var tribe = card.primaryTribe;
        if (!tribeCounts.ContainsKey(tribe)) tribeCounts[tribe] = 0;
        tribeCounts[tribe]++;
    }

    // Update each row
    foreach (var kvp in rows)
    {
        int count = tribeCounts.ContainsKey(kvp.Key) ? tribeCounts[kvp.Key] : 0;
        UpdateRow(kvp.Value, count);
    }
}

void UpdateRow(SynergyRow row, int count)
{
    row.currentCount = count;
    row.countText.text = $"{count}";

    // Update pips: threshold 2, 4, 6
    int[] thresholds = { 2, 4, 6 };
    for (int i = 0; i < 3; i++)
    {
        bool filled = count >= thresholds[i];
        Color tribeColor = ThemeManager.GetTribeColor(row.tribe);
        row.pips[i].color = filled ? tribeColor : pipEmpty;
    }

    // Active glow if any threshold met
    bool isActive = count >= 2;
    row.glowOverlay.gameObject.SetActive(isActive);
    row.rowBackground.color = isActive ?
        new Color(row.glowOverlay.color.r, row.glowOverlay.color.g, row.glowOverlay.color.b, 0.1f) :
        Color.clear;

    // Dim inactive rows
    float alpha = isActive ? 1f : 0.5f;
    row.tribeIcon.color = new Color(row.tribeIcon.color.r, row.tribeIcon.color.g, row.tribeIcon.color.b, alpha);
    row.countText.alpha = alpha;
}
```

**Icon Generation:**
- `GenerateTribeIcon(TribeType, int size)`: Small colored circle (or shape per tribe)
  - Pentacles: circle (coin-like)
  - Cups: inverted triangle (chalice)
  - Swords: vertical line with cross (sword)
  - Wands: vertical line (staff)
  - Stars: star shape (5 points)
  - Coins: circle with inner ring
- All procedural via Texture2D, 20x20 pixels

**Pip Generation:**
- Small circle texture (10x10) with soft edges
- Reusable across all rows

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- Add `[SerializeField] private SynergyDisplayPanel synergyDisplay;`
- After board changes, call `synergyDisplay?.UpdateSynergies(currentPlayer)`
- Subscribe to `player.OnBoardChanged` to trigger synergy refresh

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/SynergyDisplaySetup.cs`

Editor script (Tools > Game > Setup Synergy Display):
1. Create panel on LEFT side of screen (vertical strip)
2. Semi-transparent background (StyledPanel if available)
3. 6 rows, each with icon + pips + count
4. Wire to SynergyDisplayPanel
5. Wire to GameUIManager

## Conventions
- Implement IThemeable (tribe colors from theme)
- Subscribe to player.OnBoardChanged for live updates
- Panel position: left side of screen, vertically centered
- VerticalLayoutGroup for rows, HorizontalLayoutGroup per row
- All procedural textures cached as static
- Project must compile with zero errors
