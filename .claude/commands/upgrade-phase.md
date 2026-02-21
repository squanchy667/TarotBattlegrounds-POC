---
description: Execute a full upgrade phase (I through VII). Runs batch execution with quality gates between batches. Takes phase number as argument.
---

# Upgrade Phase

Execute upgrade phase $ARGUMENTS for Tarot Battlegrounds.

## Pre-flight

1. Read `TarotBattlegrounds-docs/PLAN.md` and `TarotBattlegrounds-docs/TASK_BOARD.md`
2. Identify all tasks for Phase $ARGUMENTS and their dependencies
3. Verify prerequisite phases are complete:
   - Phase I: No dependencies
   - Phase II: No dependencies
   - Phase III: Requires Phase II complete
   - Phase IV: No dependencies
   - Phase V: Requires Phase IV complete
   - Phase VI: Requires Phase I complete
   - Phase VII: Requires Phases I, III, IV complete
4. If prerequisites not met, STOP and report which phases need completion

## Batch Plan

Present the batch execution plan to the user:

**Phase I** (3 batches, 7 tasks — extends existing DevZone AWS stack):
- I-B1: T001-T002 → network-engineer (PlayersTable + auth endpoints)
- I-B2: T003-T005 → unity-game-developer, network-engineer (Unity auth + matchmaking)
- I-B3: T006-T007 → unity-game-developer, tarot-test-agent (Photon integration + testing)

**Phase II** (4 batches, 18 tasks):
- II-B1: T101-T104 → ability-engineer
- II-B2: T105-T112 → ability-engineer, unity-game-developer
- II-B3: T113-T116 → hero-power-engineer, ui-engineer
- II-B4: T117-T118 → ability-engineer, devzone-sync-engineer

**Phase III** (4 batches, 16 tasks):
- III-B1: T201-T202 → card-content-designer, devzone-sync-engineer
- III-B2: T203-T208 → card-content-designer, unity-game-developer
- III-B3: T209-T214 → balance-auditor, card-content-designer
- III-B4: T215-T216 → balance-auditor, game-designer

**Phase IV** (5 batches, 20 tasks):
- IV-B1: T301-T303 → combat-vfx-engineer
- IV-B2: T304-T307 → combat-vfx-engineer
- IV-B3: T308-T311 → combat-vfx-engineer
- IV-B4: T312-T315 → sfx-engineer
- IV-B5: T316-T320 → combat-vfx-engineer, network-engineer

**Phase V** (4 batches, 18 tasks):
- V-B1: T401-T406 → ui-engineer
- V-B2: T407-T410 → ui-engineer
- V-B3: T411-T413 → ui-engineer
- V-B4: T414-T418 → ui-engineer

**Phase VI** (3 batches, 15 tasks):
- VI-B1: T501-T503 → ranked-backend-engineer
- VI-B2: T504-T509 → ranked-backend-engineer, ui-engineer
- VI-B3: T510-T515 → ranked-backend-engineer, tarot-test-agent

**Phase VII** (4 batches, 20 tasks):
- VII-B1: T601-T606 → network-engineer, unity-game-developer
- VII-B2: T607-T610 → ui-engineer
- VII-B3: T611-T613 → network-engineer
- VII-B4: T614-T620 → balance-auditor, tester

## Execution

For each batch:

1. **Dispatch agents** listed for this batch with their task assignments
2. **Centralized git commit** after agents complete:
   ```
   [Phase X] TXXX-TXXX: Brief description of batch work
   ```
3. **Update TASK_BOARD.md** — mark completed tasks as DONE

## Quality Gate (After All Batches Complete)

1. Dispatch **tarot-game-auditor** for full audit — score must be >= 7.0/10
2. Dispatch **tarot-test-agent** to run all test suites — no regressions
3. Dispatch **balance-auditor** for balance check — no tribe > 45% win rate
4. Publish updated data via DevZone → verify game loads correctly from S3

If quality gate fails:
- Present issues to user
- Ask: fix now (create hotfix tasks) or proceed to next phase?

## Completion Report

After phase completes, output:
- Tasks completed (count and IDs)
- Files created/modified
- Quality gate scores
- Balance summary (if applicable)
- Next recommended phase
