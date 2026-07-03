---
model: haiku
---

# UX13: Button Hover/Press Micro-Animations

You are a micro-interaction specialist. Your mission: add hover bounce (1.05x), press squish (0.95x), click ripple, and disabled shake to all game buttons via the existing TarotButton system (UX04) or as a standalone lightweight component.

## What You're Building

Micro-interactions for buttons:
1. **Hover**: Scale to 1.05x over 0.1s (ease-out)
2. **Press**: Scale to 0.95x instantly, bounce back on release
3. **Click ripple**: Brief circular expand from click point (0.3s, then fade)
4. **Invalid click shake**: Horizontal shake (3 oscillations, 0.3s) when clicking disabled button

## Files to Create

### New: ButtonMicroFeedback.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/ButtonMicroFeedback.cs`

A lightweight component that adds to any Button:
- Implements IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
- All animation in Update() with Lerp (smooth, no allocations)
- Shake via `Mathf.Sin(Time.time * shakeFrequency) * shakeAmplitude * decay`
- Ripple: child Image that scales from 0 to 2x with fading alpha
- Auto-attaches if Button component exists

Detect disabled clicks: `IPointerClickHandler.OnPointerClick` checks `button.interactable`, if false triggers shake.

## Files to Modify

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- In Start() or Awake(): Find all Button children, add ButtonMicroFeedback if not present

## Conventions
- Lightweight MonoBehaviour, no coroutines, all in Update()
- No allocations in Update loop
- Null-check Button component
- Works independently of TarotButton (UX04) — can coexist
- Project must compile with zero errors
