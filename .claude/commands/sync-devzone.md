---
description: Sync DevZone schemas and enums with Unity code changes. Compares Unity C# enums with DevZone TypeScript enums and updates mismatches. Pass "dry-run" to preview changes without applying.
---

# Sync DevZone

Synchronize DevZone editor schemas with Unity game code.

Mode: $ARGUMENTS (optional — "dry-run" to preview only)

## Step 1: Read Unity Enums

Read the source-of-truth enums from Unity:
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Synergies/TribeType.cs` → TribeType enum values
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Abilities/AbilityTrigger.cs` → AbilityTrigger enum values
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Cards/Card.cs` → AbilityEffectType enum values

## Step 2: Read DevZone Enums

Read current DevZone enums:
- `tarot-devzone/shared/enums.ts` → TribeType, AbilityTrigger, AbilityEffectType

## Step 3: Compare

Generate a diff showing:
- Values in Unity but NOT in DevZone (need to add)
- Values in DevZone but NOT in Unity (stale, flag for review)
- Order mismatches (warning only)

## Step 4: If Dry Run

If $ARGUMENTS contains "dry-run":
- Present the diff to the user
- List all changes that WOULD be made
- STOP here (do not modify files)

## Step 5: Apply Changes

Delegate to the **devzone-sync-engineer** agent:
"Update DevZone shared files to match Unity enums. Add these new values: [list from diff]. Update these files:
1. shared/enums.ts — Add new enum values
2. shared/schemas.ts — Update Zod validation
3. shared/types.ts — Update TypeScript types
4. client/src/components/cards/CardForm.tsx — Update dropdowns
5. client/src/components/synergies/SynergyEditor.tsx — Update tribe options"

## Step 6: Verify

Run typecheck:
```bash
cd tarot-devzone && npm run typecheck
```

If typecheck fails, fix the issues and re-run.

## Step 7: Report

Present to user:
- Enum values added (count per enum)
- Files modified (list)
- Typecheck result (pass/fail)
- Any stale values flagged for review
