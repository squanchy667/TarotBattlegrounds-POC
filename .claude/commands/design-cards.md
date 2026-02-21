---
description: Card content design pipeline. Takes tribe, tier, and count as arguments (e.g., "Stars 3 5" for 5 tier-3 Stars cards).
---

# Design Cards

Design new cards for Tarot Battlegrounds.

Arguments: $ARGUMENTS (format: "tribe tier count" — e.g., "Stars 3 5")

## Step 1: Read Current State

Read these files to understand the existing card pool:
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Cards/CardDatabase.cs`
- `tarot-devzone/shared/enums.ts` (available tribes, abilities)
- `TarotBattlegrounds-docs/PLAN.md` (design principles)

Count existing cards for the requested tribe and tier to avoid overlap.

## Step 2: Design Cards

Delegate to the **card-content-designer** agent:
"Design [count] tier-[tier] [tribe] cards for Tarot Battlegrounds. Follow tarot naming conventions and the stat budget for the tier. Each card needs: name, attack, health, tribe(s), ability (trigger + effect), and flavor text."

## Step 3: Balance Review

Take the card designs and delegate to the **game-designer** agent:
"Review these card designs for balance. Check stat budgets match tier expectations, abilities are appropriately powered, and cards are distinct from existing pool."

## Step 4: Present to User

Show the user all designed cards in a table:
| Name | Tier | Atk/HP | Tribe | Ability | Flavor |
|------|------|--------|-------|---------|--------|

Ask for approval, modifications, or rejection.

## Step 5: Generate Code

On approval, produce:
1. **Unity code** — `CreateCard()` calls for `CardDatabase.cs`
2. **DevZone JSON** — card entries for `cards.json`

If new tribes or abilities were used that don't exist yet, delegate to **devzone-sync-engineer** to update shared enums.

## Step 6: Summary

Output: cards designed, code generated, any sync actions needed.
