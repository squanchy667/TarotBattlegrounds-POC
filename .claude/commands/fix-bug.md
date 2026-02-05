---
description: Run the full debug, fix, test pipeline for a bug. Orchestrates 3 specialized agents in a loop until the fix is verified.
---

# Bug Fix Pipeline

You are orchestrating a 3-agent pipeline to fix a bug in Tarot Battlegrounds.

The bug to investigate: $ARGUMENTS

## Pipeline Steps

Execute these steps IN ORDER. After each agent completes, review its output before proceeding to the next step.

### Phase 1: Diagnose

Delegate to the **debugger** agent:
"Investigate this bug in the Tarot Battlegrounds codebase: $ARGUMENTS"

Wait for the diagnosis. Review it. If the diagnosis is unclear or inconclusive, ask the debugger to dig deeper before proceeding.

### Phase 2: Fix

Take the debugger's diagnosis and delegate to the **fixer** agent:
"Apply a fix based on this diagnosis: [include the debugger's full output]"

Wait for the fix. Review the changes.

### Phase 3: Verify

Take both the diagnosis and the fix, and delegate to the **tester** agent:
"Verify this fix: [include the original bug, the diagnosis, and the fix details]"

Wait for the test results.

### Phase 4: Evaluate

Based on the tester's output:

- If PASS: Report success. Summarize the bug, root cause, fix, and tests.
- If FAIL or NEEDS MORE WORK: Go back to Phase 1 with the NEW information from the tester (what failed and why). Repeat the loop.
- Maximum 3 loops. If still failing after 3 attempts, report what was tried and recommend manual investigation.

## Output Format

After the pipeline completes, provide a final summary with these fields:

- Bug: the original description
- Root Cause: from the debugger
- Fix: from the fixer, including files changed
- Tests: from the tester, pass or fail
- Loops: how many iterations were needed
- Status: FIXED or NEEDS MANUAL REVIEW

