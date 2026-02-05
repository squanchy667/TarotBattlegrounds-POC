# Phase M - Multiplayer Bug Fixes: Comprehensive Test Sheet

**Test Date:** _________________
**Tester:** _________________
**Unity Version:** 2022.3.48f1
**Branch:** `tarot-skin`
**Commits Tested:** `0f2c377` (M5 fix), `bb5f8ea` (M3 fix), `6efec54` (original M1-M8)

---

## 🎯 Test Setup Requirements

### Prerequisites
1. ✅ Unity editor open with project loaded
2. ✅ ParrelSync clone created and functional
3. ✅ Both editors in Lobby scene
4. ✅ Console visible in both editors (clear logs before each test)
5. ✅ Note: Run tests in order (dependencies exist)

### Test Environment
- **Editor 1 (Host):** Main Unity editor
- **Editor 2 (Client):** ParrelSync clone
- **Game Mode:** 2-player multiplayer
- **Test Duration:** ~45-60 minutes for full suite

---

## 📋 Test Matrix Summary

| ID | Task | Status | Priority | Dependencies | Est. Time |
|----|------|--------|----------|--------------|-----------|
| M1 | SynergyManager per-player state | ⬜ | P0 | None | 5 min |
| M2 | DiscoveryUI per-player queue | ⬜ | P1 | None | 10 min |
| M3 | Shop pool card reservation | ⬜ | P0 | None | 10 min |
| M4 | Player 2 buy RPC sync | ⬜ | P0 | M3 | 10 min |
| M5 | Tavern upgrade cost reduction | ⬜ | P0 | None | 15 min |
| M6 | AbilityManager memory leak | ⬜ | P1 | None | 5 min |
| M7 | Combat log local filter | ⬜ | P2 | None | 5 min |
| M8 | RefreshShop coin setter | ⬜ | P2 | None | 5 min |

**Legend:** ⬜ Not Tested | ✅ PASS | ❌ FAIL | 🟡 PARTIAL | ⚠️ SKIP (dependency failed)

---

## 🧪 Test Cases

### M1: SynergyManager Per-Player State ⬜

**Bug:** Synergy calculations were using global state, causing Player 2's synergies to overwrite Player 1's

**Fix Location:** `SynergyManager.cs` - Per-player snapshot system

**How to Test:**
1. Both players: Press Play in Lobby scene
2. Host: Create room → Start Game
3. Client: Join room
4. **Turn 1:**
   - Host buys 3 Wands cards (e.g., Ace of Wands, Two of Wands, Three of Wands)
   - Client buys 3 Pentacles cards (e.g., Ace of Pentacles, Two of Pentacles, Three of Pentacles)
5. **Combat Phase:** Watch console logs for `[SynergyManager]` tags
6. **Post-Combat:** Check both players' boards still have their respective tribes

**Expected Result:**
- ✅ Host has Wands synergy active (+1/+1 per Wand)
- ✅ Client has Pentacles synergy active (+1/+1 per Pentacle)
- ✅ No synergy bonuses mixed between players
- ✅ Console logs show separate synergy calculations per player

**Console Tags to Watch:**
- `[SynergyManager] Creating snapshot for Player X`
- `[SynergyManager] Calculating synergies from snapshot`

**Pass Criteria:**
```
✅ Each player's synergies calculated independently
✅ No "global state overwrite" errors
✅ Synergy bonuses match each player's tribes
✅ Combat proceeds without synergy-related crashes
```

**Fail Criteria:**
```
❌ Player 2's synergies overwrite Player 1's
❌ Wrong tribes counted for either player
❌ Synergy bonuses don't apply correctly
❌ Errors: "ArgumentOutOfRangeException" in SynergyManager
```

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M2: DiscoveryUI Per-Player Queue ⬜

**Bug:** Triple discovery UI race condition - if both players form triples simultaneously, UI could show wrong player's choices

**Fix Location:** `DiscoveryUI.cs` - Per-player pending discovery dictionary (lines 23, 30, 102-103)

**How to Test:**
1. Continue from M1 test (or start fresh)
2. **Turn 2-3:** Each player should try to form a triple:
   - Buy 2 copies of same card (e.g., Ace of Wands)
   - Buy 3rd copy → Triple forms → Discovery UI appears
3. **Host forms triple:** UI should show for Host only
4. **Client forms triple:** UI should show for Client only
5. **Both form triple same turn:** Each sees their own discovery choices

**Expected Result:**
- ✅ Discovery UI shows only for the player who formed the triple
- ✅ In multiplayer, only local player sees the UI for their triple
- ✅ AI players auto-pick without showing UI
- ✅ No race condition if both players triple simultaneously

**Console Tags to Watch:**
- `[DiscoveryUI] AI Player X auto-picked...`
- `[DiscoveryUI] Sent discovery choice X via network`
- `[DiscoveryUI] Player X discovered...`

**Pass Criteria:**
```
✅ Host forms triple → Host sees discovery UI
✅ Client forms triple → Client sees discovery UI
✅ Other player doesn't see discovery UI
✅ Choice applies to correct player
✅ No "discovery stuck" or "wrong player picked"
```

**Fail Criteria:**
```
❌ Discovery UI appears for both players
❌ Wrong player receives the discovered card
❌ Discovery UI doesn't appear when triple forms
❌ UI stuck open, can't make choice
❌ Errors: "Key not found" in pendingDiscoveries
```

**Test Scenario A: Sequential Triples**
1. Host forms triple Turn 2 → Picks card X
2. Client forms triple Turn 3 → Picks card Y
3. Verify Host got X, Client got Y

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL

**Test Scenario B: Simultaneous Triples**
1. Both form triple same combat phase
2. Both see their own discovery UI
3. Both pick independently

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (hard to coordinate)

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M3: Shop Pool Card Reservation ⬜

**Bug:** Client received 2 cards instead of 3 due to CardLookup deserialization failure

**Fix Location:** `CardLookup.cs` - Now uses `TavernManager.masterCards` (commit `bb5f8ea`)

**How to Test:**
1. Start fresh game (both editors in Lobby)
2. Host: Create room → Start Game
3. Client: Join room
4. **Turn 1 Shop Check:**
   - Count cards in Host's shop (should be 3)
   - Count cards in Client's shop (should be 3)
5. **Watch Console:**
   - Look for `[Host/M3] Broadcasting shop for P0: 3 cards`
   - Look for `[Client/M3] Received shop for P0: 3 cards`
   - Look for `[CardLookup] Template not found:` (should NOT appear)

**Expected Result:**
- ✅ Host shop shows exactly 3 cards (Tier 1)
- ✅ Client shop shows exactly 3 cards (Tier 1)
- ✅ No NULL cards in shop
- ✅ CardLookup successfully deserializes all cards
- ✅ No "Template not found" errors

**Console Tags to Watch:**
- `[Host/M3] Broadcasting shop for PX: Y cards`
- `[Client/M3] Received shop for PX: Y cards`
- `[CardLookup] Initialized with Z templates from TavernManager.masterCards`
- `[TavernManager/M3] SetShopFromNetwork: Player X received Y cards`

**Pass Criteria:**
```
✅ Both players always see correct number of shop cards
✅ Shop card count matches tavern tier:
   - Tier 1: 3 cards
   - Tier 2: 4 cards
   - Tier 3: 4 cards
   - Tier 4: 5 cards
   - Tier 5: 5 cards
   - Tier 6: 6 cards
✅ No CardLookup deserialization errors
✅ All shop cards have valid names, stats, images
```

**Fail Criteria:**
```
❌ Client sees 2 cards instead of 3
❌ Any player sees NULL/blank cards in shop
❌ Console error: "Template not found: [card_name]"
❌ Shop card count doesn't match expected tier count
❌ Cards have missing names or stats
```

**Test Across Multiple Tiers:**
1. Turn 1 (Tier 1): Both see 3 cards ⬜
2. Upgrade to Tier 2: Both see 4 cards ⬜
3. Upgrade to Tier 3: Both see 4 cards ⬜
4. Upgrade to Tier 4: Both see 5 cards ⬜

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M4: Player 2 Buy RPC Sync ⬜

**Bug:** Player 2 clicks buy → coins deducted but no card added to hand/board

**Fix Location:** `NetworkGameBridge.cs` - `RPC_RequestBuyCard()` validation

**Dependencies:** ⚠️ **MUST PASS M3 FIRST** (shop sync required for correct buy)

**How to Test:**
1. ⚠️ **Verify M3 passed first**
2. Continue from M3 test (both players in game)
3. **Host Buy Test:**
   - Host: Click a shop card to buy
   - Verify: Card appears in host's hand/board
   - Verify: Host's coins reduced by card cost
   - Verify: Card removed from host's shop
4. **Client Buy Test:**
   - Client: Click a shop card to buy
   - Verify: Card appears in client's hand/board
   - Verify: Client's coins reduced by card cost
   - Verify: Card removed from client's shop
5. **Repeat** for multiple cards (at least 3 buys per player)

**Expected Result:**
- ✅ Client can successfully buy cards (same as host)
- ✅ Card appears in client's hand immediately
- ✅ Coins deducted correctly
- ✅ Card removed from shop display
- ✅ Host receives and processes client's buy RPC
- ✅ State synced back to client

**Console Tags to Watch:**
- `[Host] RPC_RequestBuyCard: sender=X, shopIndex=Y, shopSize=Z, coins=W`
- `[Host] Player X bought card: [card_name]`
- `[Client] Received player state update after buy`

**Pass Criteria:**
```
✅ Client can buy at least 3 cards successfully
✅ Each buy: card added, coins deducted, shop updated
✅ No "silent failures" (coins deducted but no card)
✅ No bounds errors (shopIndex out of range)
✅ State syncs correctly between host and client
```

**Fail Criteria:**
```
❌ Client click does nothing (no card, no coin change)
❌ Coins deducted but card not added
❌ Console error: "Invalid shopIndex X, shop has Y cards"
❌ Console error: "Invalid sender slot"
❌ Buy works for host but not client
```

**Detailed Buy Test Matrix:**

| Player | Card Position | Tier | Expected Cost | Card Appears? | Coins Deducted? | Result |
|--------|---------------|------|---------------|---------------|-----------------|--------|
| Host   | Shop Slot 0   | 1    | 3             | ⬜ YES / NO    | ⬜ YES / NO      | ⬜ PASS / FAIL |
| Host   | Shop Slot 1   | 1    | 3             | ⬜ YES / NO    | ⬜ YES / NO      | ⬜ PASS / FAIL |
| Client | Shop Slot 0   | 1    | 3             | ⬜ YES / NO    | ⬜ YES / NO      | ⬜ PASS / FAIL |
| Client | Shop Slot 1   | 1    | 3             | ⬜ YES / NO    | ⬜ YES / NO      | ⬜ PASS / FAIL |
| Client | Shop Slot 2   | 1    | 3             | ⬜ YES / NO    | ⬜ YES / NO      | ⬜ PASS / FAIL |

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (M3 failed)

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M5: Tavern Upgrade Cost Reduction ⬜

**Bug:** Cost reduction inconsistent between host/client, doesn't continue after upgrades

**Fix Location:** `Player.cs` (lines 98, 564-573, 583-595), `GameManager.cs` (lines 594-604)

**How to Test:**
1. Start fresh game (both editors in Lobby)
2. Host: Create room → Start Game
3. Client: Join room
4. **Turn 1:** Both check upgrade cost (should be 5)
5. **Turn 2:** Both check upgrade cost (should be 4) ← **CRITICAL CHECK**
6. **Turn 3:** Both check upgrade cost (should be 3)
7. **Host upgrades to Tier 2:** Cost resets to 8 for host
8. **Turn 4:** Host cost = 7, Client cost = 2
9. **Turn 5:** Host cost = 6, Client cost = 1
10. **Turn 6:** Host cost = 5, Client cost = 0

**Expected Result:**
- ✅ Both players see SAME cost at all times
- ✅ Cost reduces by 1 each turn for ALL players
- ✅ Cost resets to BASE when player upgrades
- ✅ Cost never goes below 0
- ✅ Strategic difference: rushing vs. waiting

**Console Tags to Watch:**
- `[Lifecycle Event] Player 1: Upgrade cost reduced to X`
- `[Lifecycle Event] Player 2: Upgrade cost reduced to X`
- `Player X: Upgraded to Tavern Tier Y... Next Upgrade Cost: Z`

**Pass Criteria:**
```
✅ Turn 2: Both see cost 4 (reduced from 5)
✅ Turn 3: Both see cost 3
✅ Cost reduces by 1 every turn for BOTH players
✅ After upgrade: cost resets to BASE for next tier
✅ Cost continues reducing after upgrade
✅ Cost never goes below 0
```

**Fail Criteria:**
```
❌ Turn 2: Host sees 4, Client sees 5 (DESYNC)
❌ Cost doesn't reduce each turn
❌ Cost doesn't reset after upgrade
❌ Cost goes negative
❌ Host and client show different costs
```

**Detailed Upgrade Cost Tracking:**

| Turn | Host Tier | Host Cost | Client Tier | Client Cost | Match? | Result |
|------|-----------|-----------|-------------|-------------|--------|--------|
| 1    | 1         | 5         | 1           | 5           | ⬜      | ⬜ PASS / FAIL |
| 2    | 1         | 4         | 1           | 4           | ⬜      | ⬜ PASS / FAIL |
| 3    | 1         | 3         | 1           | 3           | ⬜      | ⬜ PASS / FAIL |
| 4    | 2 (upgraded) | 8 (reset) | 1 | 2         | ⬜      | ⬜ PASS / FAIL |
| 5    | 2         | 7         | 1           | 1           | ⬜      | ⬜ PASS / FAIL |
| 6    | 2         | 6         | 1           | 0           | ⬜      | ⬜ PASS / FAIL |
| 7    | 2         | 5         | 1           | 0           | ⬜      | ⬜ PASS / FAIL |

**Edge Case Tests:**

**A. Multiple Upgrades:**
1. Player upgrades Tier 1→2 (cost resets to 8)
2. Wait 3 turns (cost becomes 5)
3. Player upgrades Tier 2→3 (cost resets to 11)
4. Cost should continue reducing from 11

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL

**B. Max Tier:**
1. Player reaches Tier 6
2. Upgrade cost should be 0 (can't upgrade further)

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (hard to reach)

**C. Strategic Play Difference:**
1. Player A: Rushes to Tier 3 by Turn 5 (pays 5+8+11 = 24 coins total)
2. Player B: Waits until Turn 10 (pays 1+1+4 = 6 coins total)
3. Verify Player B saved 18 coins but Player A had tier advantage

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (time-consuming)

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M6: AbilityManager Memory Leak ⬜

**Bug:** Ability registrations from previous games not cleaned up, causing memory leaks and stale triggers

**Fix Location:** `AbilityManager.cs` (line 116-119), `GameManager.cs` (lines 302, 716)

**How to Test:**
1. Start Game 1 (both editors)
2. Play at least 3 turns, buy some cards with abilities
3. **End Game** (one player reaches 0 HP or quit)
4. Check console for: `[AbilityManager] Cleared all ability registrations`
5. Start Game 2 (same room or new room)
6. Check console for: `[AbilityManager] Registered [AbilityType] for [CardName]`
7. Verify: No abilities from Game 1 trigger in Game 2

**Expected Result:**
- ✅ `AbilityManager.ClearAll()` called at game start (InitializePlayers)
- ✅ `AbilityManager.ClearAll()` called at game end (OnDestroy)
- ✅ No stale ability registrations between games
- ✅ Game 2 abilities register cleanly

**Console Tags to Watch:**
- `[AbilityManager] Registered X for Y` (new registrations)
- No errors about "card not found" or "null reference" in abilities

**Pass Criteria:**
```
✅ AbilityManager.ClearAll() called at game start
✅ AbilityManager.ClearAll() called at game cleanup
✅ Game 2 starts with empty ability dictionary
✅ No abilities from Game 1 trigger in Game 2
✅ No memory leak errors in console
```

**Fail Criteria:**
```
❌ ClearAll() not called between games
❌ Game 2 has stale abilities from Game 1
❌ Errors: "Ability triggered for destroyed card"
❌ Memory warnings in console
❌ Ability count grows across multiple games without clearing
```

**Multi-Game Test:**
1. Game 1: Buy "Ace of Wands" (has Echo ability)
2. Verify Echo triggers in combat
3. End Game 1 → Check ClearAll() called
4. Game 2: Start fresh
5. Buy "Two of Pentacles" (different ability)
6. Verify ONLY Two of Pentacles ability triggers, NOT Ace of Wands

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (time-consuming)

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M7: Combat Log Local Filter ⬜

**Bug:** Combat log shows events for ALL battles, not just local player's battle

**Fix Location:** `CombatLogUI.cs` (lines 103-108, 148, 179, 330-336)

**How to Test:**
1. Start game with 3+ players (e.g., 2 humans + 1 AI)
2. **Combat Phase:** Multiple battles happen simultaneously
   - Player 1 vs Player 2
   - Player 3 vs AI (not involving local player)
3. **Host (Player 1):** Should see ONLY "Player 1 vs Player 2" in combat log
4. **Client (Player 2):** Should see ONLY "Player 1 vs Player 2" in combat log
5. **Verify:** Neither sees "Player 3 vs AI" combat events

**Expected Result:**
- ✅ Combat log shows only local player's battle
- ✅ Other battles don't appear in log
- ✅ Console logs confirm filter: `[CombatLogUI] Skipping battle display — not local player's battle`

**Console Tags to Watch:**
- `[CombatLogUI.OnCombatStart] START - Player X vs Player Y`
- `[CombatLogUI] Skipping battle display — not local player's battle`
- `[CombatLogUI.AddLogEntry] Received: [message]`

**Pass Criteria:**
```
✅ Local player sees their own battle in combat log
✅ Local player does NOT see other players' battles
✅ Console confirms battles filtered correctly
✅ Combat log UI only shows/updates for local battle
```

**Fail Criteria:**
```
❌ Combat log shows all battles (no filter)
❌ Combat log shows wrong battle
❌ Combat log doesn't appear at all
❌ No console message about filtering
```

**Test Scenario:**
1. 4-player game: P1, P2, P3, P4
2. Combat pairings: P1 vs P3, P2 vs P4
3. P1 (host) should see: P1 vs P3 combat log
4. P2 (client) should see: P2 vs P4 combat log

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (need 3+ players)

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | ⚠️ SKIP (2-player only)

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

### M8: RefreshShop Coin Setter ⬜

**Bug:** `RefreshShop()` used direct field assignment `_coins += X` instead of property setter, breaking event system

**Fix Location:** `Player.cs` (line 606) - RefreshShop method

**How to Test:**
1. Start game (both editors)
2. Open UI inspector (if possible) or watch coin display
3. **Turn 1 Start:** Check coins = 3
4. **Turn 2 Start:** Check coins = 4
5. **Turn 3 Start:** Check coins = 5
6. Watch console for coin change events

**Expected Result:**
- ✅ `RefreshShop()` uses property setter: `coins = X`
- ✅ `OnCoinsChanged` event fires each turn
- ✅ UI updates correctly when coins change
- ✅ No direct `_coins` field assignment

**Console Tags to Watch:**
- `Player X: Tavern refreshed: ... Coins: Y -> Z`
- UI update logs (if any)

**Pass Criteria:**
```
✅ RefreshShop uses property setter (line 606: coins = ...)
✅ OnCoinsChanged event fires at turn start
✅ Coin UI updates automatically
✅ No direct _coins field manipulation
```

**Fail Criteria:**
```
❌ RefreshShop uses _coins -= 1 or _coins += 1
❌ OnCoinsChanged doesn't fire
❌ Coin UI doesn't update (stale value)
❌ Events broken for coin changes
```

**Code Inspection:**
1. Open `Player.cs`, line 606
2. Verify code says: `coins = Mathf.Min(3 + (gameTurn - 1), 10);`
3. NOT: `_coins = ...` or `_coins += ...`

**Result:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL

**Coin Progression Test:**

| Turn | Expected Coins | Actual Coins (Host) | Actual Coins (Client) | UI Updated? | Result |
|------|----------------|---------------------|-----------------------|-------------|--------|
| 1    | 3              | ⬜                   | ⬜                     | ⬜           | ⬜ PASS / FAIL |
| 2    | 4              | ⬜                   | ⬜                     | ⬜           | ⬜ PASS / FAIL |
| 3    | 5              | ⬜                   | ⬜                     | ⬜           | ⬜ PASS / FAIL |
| 4    | 6              | ⬜                   | ⬜                     | ⬜           | ⬜ PASS / FAIL |
| 10   | 10 (cap)       | ⬜                   | ⬜                     | ⬜           | ⬜ PASS / FAIL |

**Status:** ⬜ NOT TESTED | ✅ PASS | ❌ FAIL | 🟡 PARTIAL

**Notes:**
_________________________________________________________________
_________________________________________________________________

---

## 📊 Final Results Summary

### Test Completion Status

| Task | Result | Priority | Blocker? | Notes |
|------|--------|----------|----------|-------|
| M1 | ⬜ | P0 | YES | Synergy per-player |
| M2 | ⬜ | P1 | NO | Discovery queue |
| M3 | ⬜ | P0 | YES | Shop sync (CRITICAL) |
| M4 | ⬜ | P0 | YES | Buy RPC (depends on M3) |
| M5 | ⬜ | P0 | YES | Upgrade cost (CRITICAL) |
| M6 | ⬜ | P1 | NO | Memory cleanup |
| M7 | ⬜ | P2 | NO | Combat log filter |
| M8 | ⬜ | P2 | NO | Coin events |

**Pass Count:** _____ / 8
**Fail Count:** _____ / 8
**Skip Count:** _____ / 8

### Critical Issues (P0 - Must Fix Before Production)

**Issues Found:**
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

### Important Issues (P1 - Should Fix Soon)

**Issues Found:**
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

### Polish Issues (P2 - Nice to Have)

**Issues Found:**
_________________________________________________________________
_________________________________________________________________

---

## 🔧 Bug Report Template

If you find any failing tests, copy this template and fill it out:

### Bug Report: MX - [Task Name]

**Status:** ❌ FAIL
**Severity:** P0 / P1 / P2
**Date Found:** _____________

**Symptom:**
_________________________________________________________________
_________________________________________________________________

**Steps to Reproduce:**
1.
2.
3.

**Expected Behavior:**
_________________________________________________________________

**Actual Behavior:**
_________________________________________________________________

**Console Errors:**
```
[Paste console errors here]
```

**Screenshots:**
[Attach if helpful]

**Host Logs:**
```
[Paste relevant host console logs]
```

**Client Logs:**
```
[Paste relevant client console logs]
```

**Environment:**
- Unity: 2022.3.48f1
- Branch: tarot-skin
- Commit: 0f2c377
- Host/Client: [Which one had the issue?]

**Impact:**
_________________________________________________________________

**Suggested Fix:**
_________________________________________________________________

---

## ✅ When All Tests Pass

If all M1-M8 tests pass:

### Next Steps
1. ✅ Mark Sprint 12 as COMPLETE (8/8 tasks done)
2. ✅ Update PLAN.md progress to 100%
3. ✅ Commit test results to git
4. ✅ Move to Phase I: AWS Online Multiplayer (Sprint 13-15)

### Celebration Checklist
- ✅ All multiplayer bugs fixed
- ✅ 2-player ParrelSync testing successful
- ✅ Ready for production multiplayer testing
- ✅ No known critical issues

### Sprint 12 Metrics
- **Duration:** _____ days
- **Total Time:** _____ hours
- **Tasks Completed:** 8/8 (100%)
- **Test Pass Rate:** _____ / 8 (___%)
- **Bugs Found in Testing:** _____
- **Bugs Fixed:** _____

---

## ❌ If Tests Fail

If any P0 tasks fail (M1, M3, M4, M5):

### Immediate Action
1. ❌ **DO NOT PROCEED** to Phase I
2. ❌ **DO NOT MERGE** to develop branch
3. ✅ **REPORT FINDINGS** using bug report template above
4. ✅ **RE-RUN FAILING TEST** to confirm (not a fluke)
5. ✅ **ESCALATE** to orchestrator for fix

### Triage Process
1. Identify root cause from console logs
2. Determine if it's a regression (broke old fix) or new issue
3. Estimate fix complexity (< 1 hour, 1-3 hours, 3+ hours)
4. Prioritize by severity and dependencies

---

## 📝 Notes & Observations

**General Notes:**
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

**Performance Observations:**
_________________________________________________________________
_________________________________________________________________

**UI/UX Feedback:**
_________________________________________________________________
_________________________________________________________________

**Multiplayer Latency:**
_________________________________________________________________

**Other Issues (Non-M Tasks):**
_________________________________________________________________
_________________________________________________________________

---

## 🎓 Testing Tips

### Console Clarity
- Clear console logs before each test
- Use filter: `[M1]` `[M2]` etc. to focus on specific tasks
- Take screenshots of errors immediately

### ParrelSync Best Practices
- Keep both editors side-by-side on screen
- Use "Play Maximized" for better visibility
- If clone crashes, delete and re-create it

### Time Management
- Take breaks between tests (avoid fatigue)
- If stuck on a test for >10 minutes, mark as FAIL and move on
- Complete all tests first, then investigate failures

### Common Pitfalls
- ⚠️ Don't test M4 if M3 fails (dependency)
- ⚠️ Clear previous game state between tests
- ⚠️ Watch BOTH consoles (host and client)
- ⚠️ Some tests require 3+ players (M7) - skip if needed

---

**Test Sheet Version:** 1.0
**Last Updated:** February 5, 2026
**Created By:** Tarot Battlegrounds Orchestrator
**Branch:** `tarot-skin` (commit `0f2c377`)

**Sign-Off:**
- Tester: _________________ Date: _____________
- Reviewer: _________________ Date: _____________
- Orchestrator: _________________ Date: _____________

---

*"The cards reveal the truth. May your tests all pass."* 🃏✨
