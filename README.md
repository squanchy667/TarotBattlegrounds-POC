# Tarot Battlegrounds Project Summary

## Project Overview
- **Game Concept**: Tarot-themed auto-battler inspired by Hearthstone Battlegrounds. 4-player lobby (scalable to 6/8), recruit phase (buy/sell/position tarot cards), combat phase (auto-battles), last hero standing wins.
- **Tarot Integration**: Suits as tribes (Pentacles, Cups, Swords, Wands), Major Arcana as heroes with powers, tiers for cards (1-6), optional readings for random bonuses.
- **POC Scope**: Single-player prototype on PC (Unity 2023 LTS, C#), no trinkets, focus on core mechanics. Scalable to mobile/multiplayer.
- **Start Date**: July 22, 2025 (based on conversation).
- **Current Status**: Phase 3 (Combat System) in progress; Phase 2 (Card System) complete with coin progression reset (Turn 1:3/3, Turn 2:4/4, cap 10), shop size scaling (3-6), reroll logic (1 gold), buy/sell (3/1 gold, board limit 7), tier unlocking verified. Combat system placeholder with health 40 and damage cap 5 implemented.

## Tech Stack and Decisions
- **Engine**: Unity 2023 LTS (2D Core template, cross-platform for PC/mobile).
  - Decision: Chosen for rapid prototyping, multiplayer support (Mirror), and free cost for POC.
- **Networking**: Mirror (free open-source; Photon alternative if needed).
  - Decision: Budget-friendly, scalable for 4-8 players.
- **Backend**: Firebase free tier for future data (e.g., matchmaking).
  - Decision: Low-cost, deferred until multiplayer phase.
- **Code Structure**: Modular (GameManager for loop, TavernManager for shop, Card as ScriptableObject, CombatManager for battles).
  - Decision: Entity-Component-System inspired for easy expansion (e.g., effects, UI).
- **Git Repo**: TarotBattlegrounds-POC (private on GitHub, develop branch).
  - Decision: Version control for backups, milestones (e.g., phase commits).

Key Decisions Along the Way:
- Started with 4 players for POC, foundations for 6/8 (variable lobby size).
- No trinkets, but kept readings as optional (2 coins for random bonus).
- Tarot mechanics: Suits as tribes (synergies, e.g., Pentacles +1 coin on sell), tiers with Hearthstone-inspired copies (16 for Tier 1, etc.).
- Fixes: CS errors (e.g., CS0122 by public wrappers), Editor bugs (temp lists for serialization), coin progression reset, sell gold +1, shop size scaling.
- Tools: Used code_execution for prototyping (e.g., Python sims for coin/upgrade logic), web_search for inspiration (e.g., Hearthstone pool).

## Achievements So Far
- **Phase 1: Core Systems (Single-Player Prototype)**:
  - Unity project setup, Git repo.
  - GameManager.cs with Recruit/Combat phases, coroutine loop, AI placeholder.
  - Coin increment, basic logs.

- **Phase 2: Card System** (Complete):
  - Card ScriptableObject with name, tier, tribe, attack, health, ability.
  - Sample cards (AceOfPentacles Tier 1, KnightOfSwords Tier 4, added 3OfSwords Tier 2, 4OfWands Tier 3).
  - TavernManager with random generation, tier-based copies, buy/sell logic, coin system.
  - Integration with GameManager for refresh in Recruit.
  - Fixed "available cards - 0" by assigning masterCards.
  - Standardized buy/sell to 3/1 gold.
  - Implemented tavern tiers (1-6) with cumulative unlocking, upgrade costs (base 5 for Tier 2, reduce -1/turn not upgraded).
  - Added reroll (RefreshTavernShop, 1 gold, rerolls from current tier pool).
  - Shop size scales with tier (3 for Tier 1, 4 for Tier 2-3, 5 for Tier 4-5, 6 for Tier 6).
  - Coin progression resets to expected total each turn (Turn 1:3/3, Turn 2:4/4, cap 10).
  - Verified isolated mechanics (reroll changes shop, buy/sell board limit 7, tier unlocking in shop post-upgrade).

- **Phase 3: Combat System** (In Progress):
  - Implemented `CombatManager.SimulateBattle` with turn-based attacks, random first attacker ("ofek" or "jaya"), simultaneous damage, and counterattacks.
  - Added health (starting 40), damage cap (5 for first 3 turns, adjusted to tier-based in combat).
  - Integrated board from TavernManager, using cloned cards for simulation.
  - Developed tribe synergies placeholder (e.g., Pentacles +1 coin on sell, pending full implementation).
  - Fixed syntax errors (e.g., `CS1073`, `CS0103`) and crashes (infinite loops with `hasValidAttack` flag).
  - Resolved outcome discrepancies by updating logic to use final board states with `pName` and `aName`.
  - Made names generic with `pName` and `aName` parameters, passed from `CombatTester`.
  - Achieved working balance: 0% "ofek" wins, 52% "jaya" wins, 48% ties (50+ runs), attributed to board population (e.g., "AceOfPentacles" 1/3 survival).
  - Validated with 5-run logs, confirming consistent naming and turn tracking.

## Current Issues and Debugging Steps
- Resolved: "Available cards - 0" after first turn (pool regeneration fixed).
- Resolved: Coin increment bug (resets to expected per turn).
- Resolved: Sell gold not adding +1 (coins += value).
- Resolved: Shop size not scaling with tier (added GetShopSize).
- Resolved: Tier unlocking not visible (shop logs show Tier 2+ post-upgrade).
- Resolved: Health not defined (added int health = 40 in GameManager).
- Resolved: min not defined (used Mathf.Min for damage cap).
- Resolved: LINQ Select not found (added using System.Linq).
- Resolved: Combat outcome mismatches (updated to final board state logic).
- Resolved: Player tracking inconsistency (fixed turn logs with `isP ? pName : aName`).
- Other Known Bugs: ObjectDisposedException (Editor bug)—collapse Inspector during Play, or use non-serialized lists.
- Timeline: Phase 2 complete July 28, 2025; Phase 3 started July 29, 2025, ongoing as of July 31, 2025.

## Next Steps
- **Short-Term (Today)**:
  - Commit checkpoint: `git add .` > `git commit -m "Phase 3 progress: Combat system implemented, names generic"` > `git push origin develop`.
  - Add 3-5 new tarot cards (e.g., 4OfWands Tier 3: tribe "Wands", attack 4, health 4; 5OfCups Tier 3: tribe "Cups", attack 5, health 5, ability "Heal friendly for 2").

- **Medium-Term (Phase 3: Combat System)**:
  - Refine auto-battle logic (e.g., multi-minion support, taunt effects).
  - Implement tribe synergies fully (e.g., Pentacles +1 coin on sell).
  - Balance board population to address 0% "ofek" wins (e.g., adjust card stats or targeting).
  - Test with 100+ runs for statistical validation.

- **Long-Term**:
  - Phase 4: Polish/Balance (UI, animations, hero powers).
  - Phase 5: Multiplayer (Mirror networking, 4-8 lobbies).
  - Mobile port, API for xAI if needed.

- **Suggestions**: Update this file with new achievements/issues after each session. Use Trello or GitHub Issues for task tracking if needed. Ready for Phase 3 refinements now (02:37 PM +07, July 31, 2025)?

## Contributors
- Developed with assistance from Grok (xAI).