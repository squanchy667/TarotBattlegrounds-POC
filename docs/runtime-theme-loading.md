# Runtime Theme Loading

The game fetches `theme.json` from S3 at startup and applies all visual customization (colors, text, sprites) at runtime, allowing complete reskinning without rebuilding the game.

## Overview

```
S3: live/theme.json          RuntimeDataLoader          ThemeManager
    live/theme-assets/*.png      ↓ fetch JSON               ↓ apply config
                              BuildTheme()                SetTheme()
                                 ↓                          ↓
                              ThemeConfig              OnThemeChanged event
                                 ↓                          ↓
                          RuntimeThemeImageLoader       UI refreshes
                              ↓ download sprites
                          SetSpriteBySlotName()
                              ↓
                          NotifyThemeChanged()
```

## Data Flow

### 1. JSON Fetch (RuntimeDataLoader.cs)

On startup, `RuntimeDataLoader` fetches 4 files in parallel:
- `cards.json`
- `synergies.json`
- `config.json`
- `theme.json` (new)

Theme loading is **optional** — if `theme.json` is missing or fails to parse, the game continues with its built-in default theme. The other 3 files are required.

### 2. Theme Building (RuntimeDataLoader.BuildTheme)

`BuildTheme()` converts the JSON data into a `ThemeConfig` ScriptableObject:
- Hex color strings (e.g., `"#7c3aed"`) are parsed via `ColorUtility.TryParseHtmlString`
- UI text fields are mapped (e.g., JSON `buyButton` → ThemeConfig `buyButtonText`)
- Tribe data is mapped by key order: `Pentacles`=index 0, `Cups`=1, `Swords`=2, `Wands`=3

### 3. Theme Application (ThemeManager.ApplyRuntimeTheme)

Called from `CardPoolInitializer.InitializeSynergiesFromBestSource()` after data loads:
- Sets the new `ThemeConfig` as the active theme
- Fires `OnThemeChanged` so all UI updates immediately with colors and text
- Optionally kicks off sprite downloads via `RuntimeThemeImageLoader`

### 4. Progressive Sprite Loading (RuntimeThemeImageLoader.cs)

A singleton MonoBehaviour modeled after `RuntimeCardImageLoader`:
- Queues downloads for all non-null asset URLs (up to 14 sprites + 4 tribe icons)
- Concurrent download limit: 4 (configurable via `maxConcurrentDownloads`)
- On each download complete:
  - Creates a `Sprite` from the downloaded texture
  - Applies it to the `ThemeConfig` via `SetSpriteBySlotName()`
  - Fires `ThemeManager.NotifyThemeChanged()` so UI updates progressively
- Caches downloaded sprites (URL → Sprite dictionary)

## JSON Schema

`theme.json` matches the TypeScript `ThemeData` type from the DevZone shared module:

```json
{
  "gameName": "Tarot Battlegrounds",
  "tribes": {
    "Pentacles": {
      "name": "Pentacles",
      "description": "The suit of Earth and material wealth.",
      "color": "#d9a61f",
      "aliases": ["pentacle", "earth", "coins"],
      "iconUrl": "https://s3.../live/theme-assets/pentacles-icon.png"
    }
  },
  "colors": {
    "primary": "#7c3aed",
    "secondary": "#261e33",
    "accent": "#f59e0b",
    "positive": "#22c55e",
    "negative": "#ef4444",
    "textColorLight": "#ffffff",
    "textColorDark": "#1a1a26",
    "gameBackgroundColor": "#140f1f",
    "cardBackgroundColor": "#1f1a2e",
    "goldenCardColor": "#ffd933"
  },
  "uiText": {
    "shopTitle": "Tavern",
    "handTitle": "Hand",
    "boardTitle": "Battlefield",
    "buyButton": "Buy",
    "sellButton": "Sell",
    "coinsLabel": "Gold",
    "healthLabel": "Life",
    "tierLabel": "Tier",
    "playButton": "Deploy",
    "rerollButton": "Reroll",
    "upgradeButton": "Upgrade",
    "maxTierText": "MAX",
    "freezeButton": "Freeze",
    "unfreezeButton": "Unfreeze",
    "endTurnButton": "End Turn",
    "combatPhaseTitle": "Combat Phase",
    "recruitPhaseTitle": "Recruit Phase",
    "victoryText": "Victory!",
    "defeatText": "Defeat!",
    "tieText": "Draw!",
    "gameOverTitle": "Game Over",
    "playAgainText": "Play Again",
    "quitToMenuText": "Quit to Menu"
  },
  "assets": {
    "gameBackground": "https://s3.../live/theme-assets/bg.png",
    "cardFrameCommon": "https://s3.../live/theme-assets/frame-common.png",
    "cardFrameRare": null,
    "cardFrameEpic": null,
    "cardBack": null,
    "panelBackground": null,
    "buttonNormal": null,
    "buttonHighlighted": null,
    "buttonPressed": null,
    "buttonDisabled": null,
    "coinIcon": "https://s3.../live/theme-assets/coin.png",
    "healthIcon": null,
    "attackIcon": null,
    "shieldIcon": null
  }
}
```

All fields in `colors` (beyond the 5 required), all fields in `uiText` (beyond the 5 required), and the entire `assets` section are optional. Missing fields use the built-in ThemeConfig defaults.

## Files Modified/Created

| File | Change |
|------|--------|
| `Data/RuntimeDataLoader.cs` | Added theme.json parallel fetch, `RuntimeThemeData` classes, `BuildTheme()` |
| `Theme/ThemeManager.cs` | Added `ApplyRuntimeTheme()`, `NotifyThemeChanged()` |
| `Theme/ThemeConfig.cs` | Added `SetSpriteBySlotName()`, `SetTribeIcon()` |
| `Data/RuntimeThemeImageLoader.cs` | **New** — progressive sprite downloader |
| `Cards/CardPoolInitializer.cs` | Wired theme application into initialization flow |

## Scene Setup

To enable runtime theme loading, ensure your scene has:
1. `RuntimeDataLoader` — with a `DataConfig` that has `enableRuntimeLoading = true`
2. `ThemeManager` — auto-creates if missing (via `ThemeManager.EnsureExists()`)
3. `RuntimeThemeImageLoader` — add to any GameObject (optional, only needed for sprite assets)
4. `CardPoolInitializer` — triggers theme application after data loads

## Console Log Messages

Successful theme load shows:
```
[RuntimeDataLoader] Loaded theme 'Tarot Battlegrounds' from remote
[RuntimeDataLoader] Built ThemeConfig 'Tarot Battlegrounds' from runtime data
[CardPoolInitializer] Applied runtime theme from JSON
[ThemeManager] Applied runtime theme: Tarot Battlegrounds
[RuntimeThemeImageLoader] Queued 3 theme asset downloads
[RuntimeThemeImageLoader] Applied sprite: gameBackground
[RuntimeThemeImageLoader] Applied sprite: coinIcon
[RuntimeThemeImageLoader] Applied sprite: cardFrameCommon
```

Missing theme (non-fatal):
```
[RuntimeDataLoader] No theme.json found: 404 Not Found
```
