---
description: Run N automated AI-vs-AI games for balance data. Takes number of games as argument (e.g., "100"). Collects tribe win rates, card pick rates, and hero power usage.
---

# Test Combat

Run automated AI-vs-AI battle simulations.

Number of games: $ARGUMENTS (e.g., "100")

## Step 1: Verify Test Infrastructure

Read test files:
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Tests/AIBattleTests.cs`
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/AI/` (AI decision-making)

Verify the AI battle test infrastructure exists and can run N-game batches.

## Step 2: Run Simulations

Delegate to the **tarot-test-agent** agent:
"Run $ARGUMENTS AI-vs-AI games using the AIBattleTests batch simulation framework. Collect: winner per game, tribe composition of winner, cards picked per tribe per tier, game length (rounds), hero power usage (if hero powers exist). Output raw data."

## Step 3: Analyze Results

Delegate to the **balance-auditor** agent:
"Analyze these $ARGUMENTS game simulation results. Calculate: tribe win rates, card pick rates per tier, average game length, hero power top-4 rates. Flag any violations of balance criteria (tribe >45%, card pick >20%, hero >55% top-4)."

## Step 4: Present Results

Show the user:

### Win Rates
| Tribe | Games Won | Win Rate | Assessment |
|-------|-----------|----------|------------|

### Card Pick Rates (Top 10 Most/Least Picked)
| Card | Tier | Pick Rate | Assessment |
|------|------|-----------|------------|

### Hero Power Usage (if applicable)
| Hero | Pick Rate | Top 4 Rate | Assessment |
|------|-----------|------------|------------|

### Game Health
- Average game length: X rounds
- Win rate variance: X%
- Longest/shortest game: X/Y rounds

### Flagged Issues
List any balance criteria violations.

## Step 5: Recommendations

Ask the user:
- "Run /balance-check for detailed tuning recommendations?"
- "Run additional simulations with specific constraints?"
- "Save results to docs?"
