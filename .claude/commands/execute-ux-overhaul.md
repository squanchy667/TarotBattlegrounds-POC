# UX Visual Overhaul — Orchestrator Prompt

You are orchestrating a 20-task UI/UX visual overhaul for TarotBattlegrounds, a Unity auto-battler game. Each task has a dedicated agent definition in `/Users/ofek/Projects/Claude/BattleNet/.claude/agents/ux{01-20}-*.md`.

## Project Location
- **Code repo**: `/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/`
- **Scripts**: `Assets/Scripts/`
- **UI scripts**: `Assets/Scripts/UI/`
- **Theme**: `Assets/Scripts/Theme/`
- **Editor scripts**: `Assets/Editor/`
- **Agent definitions**: `/Users/ofek/Projects/Claude/BattleNet/.claude/agents/ux*.md`

## Critical Rules

1. **Read the agent file first** — Before executing any task, read the full agent `.md` file. It contains exact file paths, field names, color values, and implementation details.
2. **One batch at a time** — Do NOT start the next batch until the current one compiles cleanly.
3. **Compile check after each batch** — Unity uses C#. All files must have valid syntax. Check for missing usings, unresolved types, mismatched braces.
4. **No file conflicts within a batch** — Tasks in the same batch touch different files. If two tasks modify the same file, run them sequentially.
5. **New files need .meta** — Unity auto-generates .meta files. Just create the .cs files; Unity handles the rest.
6. **IThemeable pattern** — Any new UI component that uses theme colors must implement `IThemeable` and subscribe to `ThemeManager.OnThemeChanged` in `OnEnable`, unsubscribe in `OnDisable`.
7. **Editor scripts** — Wrap in `#if UNITY_EDITOR` and `using UnityEditor`. Place in `Assets/Editor/`.
8. **Backward compatibility** — Always null-check new [SerializeField] references. Existing features must not break if new components aren't wired in the Inspector yet.

## Execution Plan

### Phase 1: Foundation (Batch 1)
These establish the visual base. Everything else builds on these.

**Step 1a — Run alone (others depend on its colors):**
- `ux01-theme-palette` — Rewrite all color values across ThemeConfig, CardFrameGenerator, CardDisplayUI, ShopCardUI, BoardCardUI, HandCardUI

**Step 1b — Run in parallel (after UX01 is done):**
- `ux02-game-background` — New BackgroundController.cs + Editor setup
- `ux03-panel-backgrounds` — New StyledPanel.cs + modify ShopUI/HandUI/BoardUI/GameUIManager
- `ux04-button-styling` — New TarotButton.cs + Editor setup

**Compile check. Fix any errors before proceeding.**

---

### Phase 2: Cards (Batch 2)
The core visual — cards are what players look at most.

**Run sequentially (they share CardDisplayUI.cs and CardFrameGenerator.cs):**
1. `ux05-card-frame-redesign` — Layered frame: outer/inner border, tier gems, inner shadow
2. `ux06-card-typography` — Stat badges (ATK/HP circles), name banner, ability text, layout zones
3. `ux07-golden-card-effects` — Shimmer animation, pulsing gold border, sparkle particles
4. `ux08-card-hover-selection` — Hover scale+shadow, selection glow pulse, frozen shimmer

**Compile check. Fix any errors before proceeding.**

---

### Phase 3: HUD (Batch 3)
All independent — different UI areas, no file conflicts.

**Run in parallel:**
- `ux09-resource-bar` — Coin/health/tier display with procedural icons + change animations
- `ux10-timer-redesign` — Circular arc countdown with color transitions + danger pulse
- `ux11-phase-banner` — Animated "RECRUIT PHASE" / "COMBAT!" slide-in banner + turn badge
- `ux12-synergy-display` — Side panel with 6 tribe rows, pip indicators, active glow

**Compile check. Fix any errors before proceeding.**

---

### Phase 4: Feedback & Polish (Batch 4)
Animations and micro-interactions.

**Run in parallel (UX15 modifies UIAnimator.cs alone, others create new files):**
- `ux13-button-animations` — Hover bounce, press squish, disabled shake
- `ux14-floating-combat-numbers` — Damage/heal/buff popup numbers during combat
- `ux15-card-play-animations` — Card fly hand→board, sell shrink, buy slide, coin burst
- `ux16-combat-arena` — Battlefield divider, team labels, attack trails

**Compile check. Fix any errors before proceeding.**

---

### Phase 5: Screens & Menus (Batch 5)
Each is a separate scene/panel — fully independent.

**Run in parallel:**
- `ux17-main-menu` — MainMenu.unity visual redesign: title, layout, buttons, atmosphere
- `ux18-game-over-screen` — Placement medals, animated entry, styled buttons
- `ux19-discovery-popup` — Card fan layout, staggered entrance, selection flash
- `ux20-collection-viewer` — Card grid, tribe/tier filters, detail panel, search

**Compile check. Final validation.**

---

## How to Execute Each Task

For each task agent:

```
1. Read the agent definition:
   Read /Users/ofek/Projects/Claude/BattleNet/.claude/agents/ux{XX}-{name}.md

2. Read every file listed under "Files to Modify" in the agent definition

3. Create new files listed under "Files to Create"

4. Modify existing files as specified

5. Verify: ensure all files have correct using statements, no unresolved types,
   matching braces, and IThemeable properly implemented where required
```

## Validation Checklist

After ALL 5 phases are complete, verify each item:

### Phase 1: Foundation
- [ ] UX01: ThemeConfig.cs has new mystical color palette (deep purples, rich gold)
- [ ] UX01: CardFrameGenerator.cs tier colors updated (vibrant jewel tones)
- [ ] UX01: CardDisplayUI/ShopCardUI/BoardCardUI/HandCardUI colors updated
- [ ] UX02: BackgroundController.cs exists in Assets/Scripts/UI/
- [ ] UX02: BackgroundSetup.cs exists in Assets/Editor/
- [ ] UX02: Procedural gradient + vignette + particle dust implemented
- [ ] UX03: StyledPanel.cs exists in Assets/Scripts/UI/
- [ ] UX03: PanelBackgroundSetup.cs exists in Assets/Editor/
- [ ] UX03: Rounded-rect generation with SDF anti-aliasing implemented
- [ ] UX03: ShopUI/HandUI/BoardUI integrate StyledPanel
- [ ] UX04: TarotButton.cs exists in Assets/Scripts/UI/
- [ ] UX04: ButtonStylingSetup.cs exists in Assets/Editor/
- [ ] UX04: 4 button variants (Primary/Secondary/Danger/Success)
- [ ] UX04: Hover scale (1.05x) + press scale (0.95x) + disabled state
- [ ] **COMPILE CHECK**: Zero errors after Phase 1

### Phase 2: Cards
- [ ] UX05: CardFrameGenerator.cs has outerFrame, innerFrame, innerShadow, tierIndicator fields
- [ ] UX05: GenerateFrameTexture() creates layered rounded-rect frame
- [ ] UX05: GenerateTierGems() creates tier diamond indicators
- [ ] UX05: CardFrameSetup.cs exists in Assets/Editor/
- [ ] UX06: CardDisplayUI.cs has attackBadge, healthBadge, costBadge fields
- [ ] UX06: StatBadge.cs exists in Assets/Scripts/UI/
- [ ] UX06: CardLayoutSetup.cs exists in Assets/Editor/
- [ ] UX06: ATK bottom-left, HP bottom-right, cost top-left layout
- [ ] UX07: GoldenCardEffect.cs exists in Assets/Scripts/UI/
- [ ] UX07: Shimmer animation (diagonal light sweep)
- [ ] UX07: Border pulse (sine wave alpha)
- [ ] UX07: Sparkle particles (golden, upward drift)
- [ ] UX08: CardInteractionFeedback.cs exists in Assets/Scripts/UI/
- [ ] UX08: Hover scale (1.08x) + Y offset + drop shadow
- [ ] UX08: Selection glow pulse + frozen shimmer
- [ ] **COMPILE CHECK**: Zero errors after Phase 2

### Phase 3: HUD
- [ ] UX09: ResourceBar.cs exists in Assets/Scripts/UI/
- [ ] UX09: ResourceBarSetup.cs exists in Assets/Editor/
- [ ] UX09: Procedural coin/heart/tier icons generated
- [ ] UX09: Value change flash + scale punch animation
- [ ] UX09: Roman numeral tier display
- [ ] UX10: CircularTimer.cs exists in Assets/Scripts/UI/
- [ ] UX10: CircularTimerSetup.cs exists in Assets/Editor/
- [ ] UX10: Radial fill arc with green→yellow→red color transition
- [ ] UX10: Pulse effect in last 5 seconds
- [ ] UX11: PhaseBanner.cs exists in Assets/Scripts/UI/
- [ ] UX11: PhaseBannerSetup.cs exists in Assets/Editor/
- [ ] UX11: Slide-in + hold + fade-out banner animation
- [ ] UX11: Turn badge with Roman numerals
- [ ] UX12: SynergyDisplayPanel.cs exists in Assets/Scripts/UI/
- [ ] UX12: SynergyDisplaySetup.cs exists in Assets/Editor/
- [ ] UX12: 6 tribe rows with 3 threshold pips each
- [ ] UX12: Active synergy glow, inactive dim
- [ ] **COMPILE CHECK**: Zero errors after Phase 3

### Phase 4: Feedback & Polish
- [ ] UX13: ButtonMicroFeedback.cs exists in Assets/Scripts/UI/
- [ ] UX13: Hover bounce + press squish + disabled shake
- [ ] UX14: FloatingNumberManager.cs exists in Assets/Scripts/UI/
- [ ] UX14: FloatingNumber.cs exists in Assets/Scripts/UI/
- [ ] UX14: Pool-based damage/heal/buff/aegis/death popups
- [ ] UX15: UIAnimator.cs has AnimateCardPlay, AnimateCardSell, AnimateCardBuy, AnimateCoinBurst
- [ ] UX15: Bezier arc trajectory for card play
- [ ] UX16: CombatArenaVisual.cs exists in Assets/Scripts/Combat/Animator/
- [ ] UX16: CombatArenaSetup.cs exists in Assets/Editor/
- [ ] UX16: Center divider + team labels + attack trail
- [ ] **COMPILE CHECK**: Zero errors after Phase 4

### Phase 5: Screens
- [ ] UX17: MainMenuVisual.cs exists in Assets/Scripts/UI/
- [ ] UX17: MainMenuSetup.cs exists in Assets/Editor/
- [ ] UX17: Title + styled buttons + fade-in animation
- [ ] UX18: GameOverUI.cs has placement badges with medal colors (gold/silver/bronze)
- [ ] UX18: Animated panel entry (scale + fade)
- [ ] UX19: DiscoveryUI.cs has card fan layout + staggered entrance + selection flash
- [ ] UX19: DiscoverySetup.cs exists in Assets/Editor/
- [ ] UX20: CollectionUI.cs has tribe/tier filter tabs + detail panel + search
- [ ] UX20: CollectionSetup.cs exists in Assets/Editor/
- [ ] **COMPILE CHECK**: Zero errors after Phase 5

### Final Validation
- [ ] All 20 UX agent tasks completed
- [ ] Zero compile errors across entire project
- [ ] No existing gameplay features broken (backward compat via null-checks)
- [ ] All new components implement IThemeable where they use theme colors
- [ ] All Editor setup scripts are in Assets/Editor/ wrapped in #if UNITY_EDITOR
- [ ] All background/overlay Images have Raycast Target = false
- [ ] All button Images have Raycast Target = true
- [ ] Total new files created: ~20 scripts + ~10 Editor scripts

## Notes for the Orchestrator
- If a compile error occurs mid-batch, fix it before continuing. Common issues:
  - Missing `using UnityEngine.UI;` or `using TMPro;`
  - Type not found → check assembly (TarotBattlegrounds.asmdef covers Assets/Scripts/)
  - IThemeable not found → needs to be in TarotBattlegrounds assembly scope
- The user will wire components in Unity Inspector after code is generated. Editor setup scripts help but aren't required for compilation.
- Assets (sprites, images) will be provided by the user later. All code should gracefully handle null sprites.
