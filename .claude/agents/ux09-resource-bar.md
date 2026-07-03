---
model: sonnet
---

# UX09: Resource Bar Redesign

You are a HUD specialist for TarotBattlegrounds. Your mission: redesign the coin/health/tier display from plain text into styled containers with procedural icons — coin (yellow circle), health (heart shape), tier (Roman numeral badge).

## What You're Building

A horizontal resource bar with 3 styled segments:
```
[ (coin) 7/10 ]  [ (heart) 32 ]  [ Tier III - 5g to upgrade ]
```

Each segment:
- Procedural icon (generated circle/heart/badge texture)
- Value text (bold, white)
- Container with dark rounded-rect background
- Icon colored by type (gold, red, purple)
- Value animates on change (brief flash + scale punch)

## Files to Create

### New: ResourceBar.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/ResourceBar.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResourceBar : MonoBehaviour, IThemeable
{
    [Header("Coin Display")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image coinContainer;

    [Header("Health Display")]
    [SerializeField] private Image healthIcon;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthContainer;

    [Header("Tier Display")]
    [SerializeField] private Image tierIcon;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Image tierContainer;

    [Header("Colors")]
    [SerializeField] private Color coinColor = new Color(1f, 0.82f, 0.12f);
    [SerializeField] private Color healthColor = new Color(0.9f, 0.2f, 0.25f);
    [SerializeField] private Color tierColor = new Color(0.55f, 0.3f, 0.75f);
    [SerializeField] private Color containerColor = new Color(0.08f, 0.05f, 0.14f, 0.8f);

    [Header("Animation")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float punchScale = 1.2f;

    private int lastCoinValue = -1;
    private int lastHealthValue = -1;
    private int lastTierValue = -1;
}
```

Implement:

**Icon Generation (static, called once):**
- `GenerateCoinIcon()`: 32x32 circle with inner ring detail (2-color, gold)
- `GenerateHeartIcon()`: 32x32 heart shape (two circles + triangle, red)
- `GenerateTierBadge()`: 32x32 shield/diamond shape (purple)
- All generated via `Texture2D` + `SetPixels` with SDF-based shapes

**Value Updates:**
- `UpdateCoins(int current, int max)`: Display "7/10", flash if changed
- `UpdateHealth(int current)`: Display "32", flash red on damage, green on heal
- `UpdateTier(int tier, int upgradeCost)`: Display "Tier III" with Roman numerals, show cost or "MAX"

**Change Animation:**
```csharp
IEnumerator FlashValue(TMP_Text text, RectTransform container, Color flashColor)
{
    // 1. Brief color flash (text turns flashColor for 0.15s)
    // 2. Scale punch (container scales to 1.2x then back, 0.3s total)
    // 3. Return to normal
    var originalColor = text.color;
    text.color = flashColor;
    float elapsed = 0;
    while (elapsed < flashDuration)
    {
        float t = elapsed / flashDuration;
        float scale = 1f + (punchScale - 1f) * Mathf.Sin(t * Mathf.PI);
        container.localScale = Vector3.one * scale;
        elapsed += Time.deltaTime;
        yield return null;
    }
    container.localScale = Vector3.one;
    text.color = originalColor;
}
```

**Roman Numeral Conversion:**
```csharp
string ToRoman(int tier) => tier switch
{
    1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", _ => tier.ToString()
};
```

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- Replace direct text updates to `coinsText`, `tierText`, `healthText`, `upgradeCostText` with ResourceBar calls
- Add `[SerializeField] private ResourceBar resourceBar;` field
- In update methods: `resourceBar?.UpdateCoins(coins, maxCoins)` etc.
- Keep backward compat: if resourceBar is null, use existing text fields

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/ResourceBarSetup.cs`

Editor script (Tools > Game > Setup Resource Bar):
1. Create horizontal layout group for resource bar
2. Create 3 containers (coin, health, tier) with:
   - Rounded-rect background image
   - Icon Image (32x32) on left
   - Value TMP_Text on right
3. Wire to ResourceBar component
4. Wire ResourceBar to GameUIManager

## Conventions
- Implement IThemeable (coin/health/tier colors from theme)
- Procedural icons: generate once in static initializer, cache
- All containers: HorizontalLayoutGroup for icon+text alignment
- Padding: 8px inside containers, 12px between containers
- Font: Bold for values, Regular for labels
- Project must compile with zero errors
