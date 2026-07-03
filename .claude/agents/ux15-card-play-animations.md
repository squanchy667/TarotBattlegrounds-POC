---
model: haiku
---

# UX15: Shop Refresh & Card Play Animations

You are an animation specialist for TarotBattlegrounds. Your mission: polish the existing UIAnimator with better card transitions — cards fan in on shop refresh, card flies from hand to board on play, sell card shrinks with a coin burst effect.

## What You're Building

Enhanced animations in the existing UIAnimator system:

1. **Shop refresh**: Cards cascade in with slight rotation wobble + scale-from-zero
2. **Card play (hand→board)**: Card flies to board slot with arc trajectory
3. **Card sell**: Card shrinks to zero + brief coin particle burst
4. **Card buy**: Card slides from shop to hand with subtle glow trail

## Files to Modify

### Primary: UIAnimator.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/UIAnimator.cs`

Existing methods to enhance:
- `AnimateShopRefresh(Transform shopContainer)` — currently slides from right. Add: start at 0 scale, pop to 1.1x then 1.0x, slight random rotation (-5 to +5 degrees) that settles to 0
- `PunchScale()` — works fine, reuse for buy/sell

New methods to add:
```csharp
public void AnimateCardPlay(RectTransform card, RectTransform targetSlot, System.Action onComplete)
// Arc trajectory from hand position to board position, 0.4s

public void AnimateCardSell(RectTransform card, System.Action onComplete)
// Shrink to zero (0.3s) with rotation spin (180 degrees)

public void AnimateCardBuy(RectTransform card, RectTransform handSlot, System.Action onComplete)
// Slide from shop to hand (0.3s) with brief glow

public void AnimateCoinBurst(Vector3 position, int coinCount)
// Spawn coinCount small gold circles that fly outward and fade (0.5s)
```

**Arc trajectory** for card play:
```csharp
// Bezier curve: start → control point (higher Y) → end
Vector3 controlPoint = (start + end) / 2 + Vector3.up * arcHeight;
Vector3 pos = Mathf.Pow(1-t, 2) * start + 2 * (1-t) * t * controlPoint + t * t * end;
```

## Files to Modify (Integration)

### GameUIManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/GameUIManager.cs`

- On card play action: call UIAnimator.AnimateCardPlay() before executing game logic
- On card sell: call UIAnimator.AnimateCardSell()
- On card buy: call UIAnimator.AnimateCardBuy()

## Conventions
- Extend existing UIAnimator (don't create new class)
- All animations use coroutines
- onComplete callbacks for chaining with game logic
- Keep existing AnimateShopRefresh API, just enhance the internal routine
- Project must compile with zero errors
