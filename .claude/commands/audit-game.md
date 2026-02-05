---
description: Run a full game logic audit comparing code against Hearthstone Battlegrounds rules. Produces a prioritized bug list. Optionally auto-fix with the fix-bug pipeline.
---

# Game Logic Audit

You are running a comprehensive audit of Tarot Battlegrounds.

$ARGUMENTS

## Step 1: Run the Audit

Delegate to the **game-auditor** agent:
"Audit the entire Tarot Battlegrounds codebase. Check every system against the expected game rules. Produce a numbered bug list."

Wait for the full audit report.

## Step 2: Present the Report

Show the user the complete bug list organized by severity (CRITICAL, HIGH, MEDIUM, LOW).

## Step 3: Ask About Auto-Fix

After presenting the report, ask the user:
"I found X bugs. Would you like me to run /fix-bug on any of them? You can say:
- 'fix all critical' to fix all CRITICAL bugs
- 'fix 1, 3, 5' to fix specific bugs by number
- 'fix all' to fix everything (this will take a while)
- 'none' to just keep the report"

Then for each bug the user selects, run the fix-bug pipeline (debugger, fixer, tester) on that specific bug description.

