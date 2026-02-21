---
description: Run a balance analysis on the current card pool. Optionally focus on a specific area (tribes, heroes, economy). Produces a detailed report with tuning recommendations.
---

# Balance Check

Run a balance analysis on Tarot Battlegrounds.

Focus area: $ARGUMENTS (optional — e.g., "tribes", "heroes", "economy", or blank for full analysis)

## Step 1: Read Game Data

Read all relevant game data files:
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Cards/CardDatabase.cs`
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Synergies/TribeSynergy.cs`
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Heroes/HeroPowerDatabase.cs` (if exists)
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/AI/` (AI evaluation logic)

## Step 2: Statistical Analysis

Delegate to the **balance-auditor** agent:
"Analyze the current Tarot Battlegrounds card pool for balance. Focus: $ARGUMENTS. Check tribe distributions, stat budgets, ability power levels, synergy achievability, and hero power balance. Produce a report with specific tuning recommendations."

## Step 3: Design Intent Check

Optionally delegate to the **game-designer** agent:
"Review the balance auditor's findings against the design intent. Flag any recommended changes that would conflict with the game's design vision."

## Step 4: Present Report

Show the user the full balance report:
- Overall health score (X/10)
- Per-tribe win rates and assessment
- Outlier cards (overpowered or underpowered)
- Hero power balance (if applicable)
- Specific tuning recommendations (prioritized)

## Step 5: Action Items

Ask the user:
- "Apply recommended changes now?" → Generate code changes
- "Run simulation first?" → Trigger /test-combat for data
- "Save report only?" → Write to `TarotBattlegrounds-docs/resources/balance-reports/`
