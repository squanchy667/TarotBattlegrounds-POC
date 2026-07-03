---
model: sonnet
---

# UX14: Floating Combat Numbers

You are a combat feedback specialist for TarotBattlegrounds. Your mission: add floating damage/heal/buff numbers that pop up during combat — red numbers for damage, green for healing, gold for buffs, with scale-in animation and upward float.

## What You're Building

Pool-based floating number system:
1. **Damage numbers**: Red, float up from damaged card, "-5" style
2. **Heal numbers**: Green, float up, "+3" style
3. **Buff numbers**: Gold, float up, "+2 ATK" or "+1/+1" style
4. **Pop-in animation**: Start at 0.5x scale, pop to 1.2x, settle to 1.0x
5. **Float up**: Rise 60px over 1 second, fade out in last 0.3s

## Files to Create

### New: FloatingNumberManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/FloatingNumberManager.cs`

Singleton with object pool:
```csharp
public class FloatingNumberManager : MonoBehaviour
{
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private int poolSize = 20;

    public static FloatingNumberManager Instance;

    public void ShowDamage(Vector3 worldPos, int amount);
    public void ShowHeal(Vector3 worldPos, int amount);
    public void ShowBuff(Vector3 worldPos, string text);
    public void ShowAegisPop(Vector3 worldPos);  // "AEGIS!" text
    public void ShowDeath(Vector3 worldPos);      // skull or "DEAD" text
}
```

### New: FloatingNumber.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/UI/FloatingNumber.cs`

Individual floating number:
- TMP_Text with CanvasGroup
- Animation: scale pop (0.5 → 1.2 → 1.0 over 0.2s) then float up (60px over 0.8s) with fade out (last 0.3s)
- Colors: damage=red (0.95, 0.25, 0.25), heal=green (0.2, 0.85, 0.4), buff=gold (1, 0.82, 0.12), aegis=cyan (0.3, 0.8, 1)
- Bold, 24pt font
- Pool: `ReturnToPool()` after animation complete

## Files to Modify

### CombatManager.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/CombatManager.cs`

- After damage dealt: `FloatingNumberManager.Instance?.ShowDamage(cardPosition, damage)`
- After heal: `ShowHeal()`
- After aegis pop: `ShowAegisPop()`
- After death: `ShowDeath()`
- Keep backward compat: null-check Instance

### CombatCardVisual.cs
`/Users/ofek/Projects/Claude/BattleNet/TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Combat/Animator/CombatCardVisual.cs`

- Expose position for floating numbers: `public Vector3 GetWorldPosition()`
- After FlashDamage/FlashBuff: trigger floating number at card position

## Conventions
- Object pool pattern (pre-instantiate, reuse)
- Singleton with Instance field
- All animation in coroutines (lifetime-bound, auto-return to pool)
- World-to-screen position conversion for UI overlay
- Project must compile with zero errors
