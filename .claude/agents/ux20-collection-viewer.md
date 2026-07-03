---
model: haiku
---

# UX20: Collection Viewer Layout

You are a collection browser specialist. Your mission: polish the Collection UI — card grid with tribe filter tabs, card detail panel on select, tier filter as star buttons, smooth scroll with card pop-in animation.

## What You're Building

Polished collection browser:
1. **Tribe filter tabs**: 6 tribe-colored buttons + "All" tab at top
2. **Tier filter**: Star/gem buttons (1-6) + "All" option
3. **Card grid**: ScrollRect with GridLayoutGroup, cards pop-in on scroll
4. **Detail panel**: Right side panel showing selected card enlarged with full stats
5. **Search bar**: Text input for filtering by card name

## Files to Modify

### Primary: CollectionUI.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/CollectionUI.cs`

Enhance existing collection viewer:

Add fields:
```csharp
[Header("Filters")]
[SerializeField] private Transform tribeFilterContainer;
[SerializeField] private Transform tierFilterContainer;
[SerializeField] private TMP_InputField searchInput;

[Header("Card Grid")]
[SerializeField] private GridLayoutGroup cardGrid;
[SerializeField] private float cardPopDelay = 0.03f;

[Header("Detail Panel")]
[SerializeField] private RectTransform detailPanel;
[SerializeField] private CardDisplayUI detailCard;
[SerializeField] private TMP_Text detailName;
[SerializeField] private TMP_Text detailAbilities;
[SerializeField] private TMP_Text detailLore;
```

Enhance:
- **Tribe filters**: Row of 7 small buttons ("All" + 6 tribes), each in tribe color. Active = bright, inactive = dim. Toggle filter on click.
- **Tier filters**: Row of 7 small buttons ("All" + tiers 1-6). Active = bright, inactive = dim.
- **Card pop-in**: When grid refreshes, cards scale from 0 to 1 with slight stagger (0.03s each)
- **Detail panel**: When card clicked in grid, show enlarged view on right with full ability descriptions

**Filter logic** (existing, enhance visual feedback):
- Active filter button: full opacity, tribe-colored background
- Inactive filter button: 0.4 opacity, gray background
- Multiple filters combine (tribe AND tier AND search)

### New: Editor Setup
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Editor/CollectionSetup.cs`

Setup collection UI hierarchy in MainMenu scene.

## Conventions
- Work in MainMenu scene
- Reuse CardDisplayUI for grid cells (existing prefab)
- Filters: HorizontalLayoutGroup with toggle buttons
- Card grid: GridLayoutGroup with ScrollRect
- Detail panel: right 30% of screen, dark background
- Project must compile with zero errors
