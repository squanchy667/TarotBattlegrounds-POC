# Tarot Battlegrounds - Master Project Plan

**Version:** 2.0
**Last Updated:** February 5, 2026
**Branch:** `tarot-skin`
**Main Branch:** `develop`
**Status:** Phase M - Multiplayer Bug Fixes (Sprint 12)

---

## 🎯 Project Overview

**Tarot Battlegrounds** is an 8-player auto-battler inspired by Hearthstone Battlegrounds, featuring:
- Tarot-themed cards with 4 tribal synergies (Pentacles, Cups, Swords, Wands)
- Automated combat system with strategic deck building
- Multiplayer support via Photon PUN
- 6-tier progression system with triple/golden mechanics

**Current State:** Local gameplay complete. Multiplayer functional but has 3 critical bugs blocking full 2-player experience.

---

## 📊 Project Health Dashboard

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  SYSTEM HEALTH: 8.05/10  ✅ GOOD                        ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃  Test Pass Rate:       107/124 (86.3%)  ✅             ┃
┃  Open Bugs:            3 multiplayer     ⚠️             ┃
┃  Regression Risk:      LOW               ✅             ┃
┃  Ready for Phase I:    After Phase M     🟡             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

### Component Scores (Weighted Average: 8.05/10)

| Component | Score | Weight | Status |
|-----------|-------|--------|--------|
| Rules & Mechanics | 8/10 | 20% | ✅ Good |
| Combat Balance | 9/10 | 25% | ✅ Excellent |
| Economy | 7/10 | 15% | ⚠️ Needs Work |
| Synergy System | 10/10 | 15% | ⭐ Perfect |
| Multiplayer | 6/10 | 20% | ⚠️ Needs Work |
| Regression | 9/10 | 5% | ✅ Excellent |

---

## 🗓️ Current Sprint: Sprint 12 - Phase M (Multiplayer Bug Fixes)

**Goal:** Fix 8 multiplayer bugs to enable stable 2-player online matches
**Duration:** 3-5 days (~17 hours of work)
**Start Date:** February 5, 2026
**Target Completion:** February 10, 2026

### Sprint 12 Task List

| ID | Task | Priority | Status | Effort | Files | Dependencies |
|----|------|----------|--------|--------|-------|--------------|
| M1 | SynergyManager per-player state | P0 | ✅ DONE | 0h | SynergyManager.cs | None |
| M3 | Shop pool card reservation | P0 | 🔴 TODO | 4h | TavernManager.cs | None |
| M4 | Player 2 buy RPC sync | P0 | 🔴 TODO | 3h | NetworkGameBridge.cs, Player.cs | M3 |
| M2 | DiscoveryUI per-player queue | P1 | 🟡 TODO | 2h | DiscoveryUI.cs | None |
| M5 | Tavern upgrade state sync | P1 | 🟡 TODO | 2h | NetworkPlayerState, Player.cs | None |
| M6 | AbilityManager memory leak | P1 | 🟡 TODO | 3h | AbilityManager.cs | None |
| M8 | RefreshShop coin setter | P2 | 🟢 TODO | 1h | TavernManager.cs, Player.cs | None |
| M7 | Combat log local filter | P2 | 🟢 TODO | 2h | CombatLogUI.cs, CombatManager.cs | None |

**Legend:**
- 🔴 P0 = Critical (blocks testing)
- 🟡 P1 = Important (affects UX)
- 🟢 P2 = Nice to have (polish)

### Execution Order (Dependency-Aware)

**Day 1: Critical Path**
1. ✅ M1: Already complete (per-player synergy snapshots)
2. 🔴 M3: Fix shop pool reservation (4h)
3. 🔴 M4: Fix Player 2 buy RPC (3h) - depends on M3
4. ✅ Verify with ParrelSync 2-player test

**Day 2: Important Fixes**
5. 🟡 M2: Fix DiscoveryUI race condition (2h)
6. 🟡 M5: Fix upgrade cost sync (2h)
7. 🟡 M6: Fix AbilityManager cleanup (3h)

**Day 3: Polish & Testing**
8. 🟢 M8: Fix RefreshShop setter (1h)
9. 🟢 M7: Fix combat log filter (2h)
10. ✅ Full multiplayer regression test (2h)
11. ✅ Create Phase M test suite (3h)

---

## 🐛 Known Issues (Active)

### CRITICAL (P0) - Blocks Multiplayer Testing

#### BUG 1: Shop State Desync 🔴
**Task:** M3
**Symptom:** Tavern shows 2 cards instead of 3 at turn start for Player 2
**Root Cause:** Client shop display doesn't match host's actual shop state
**Impact:** Wrong `shopIndex` sent in buy RPCs → causes BUG 3
**Files:**
- `TavernManager.cs` → `SetShopFromNetwork()`, shop generation
- `NetworkGameBridge.cs` → `BroadcastShopForPlayer()`, `RPC_SyncShopForPlayer()`

**Investigation Steps:**
1. Add debug logging to `BroadcastShopForPlayer()`:
   ```csharp
   Debug.Log($"[Host] Broadcasting shop for P{playerIndex}: {shopCards.Length} cards");
   ```
2. Add debug logging to `RPC_SyncShopForPlayer()`:
   ```csharp
   Debug.Log($"[Client] Received shop for P{playerIndex}: {cardDataArray.Length} cards");
   ```
3. Check if `SetShopFromNetwork()` does REPLACE (correct) vs APPEND (bug)

**Expected Fix:**
```csharp
// TavernManager.cs - SetShopFromNetwork()
public void SetShopFromNetwork(int playerIndex, List<Card> cards)
{
    if (!availableCards.ContainsKey(playerIndex))
        availableCards[playerIndex] = new List<Card>();

    availableCards[playerIndex].Clear(); // ← Ensure this exists!
    availableCards[playerIndex].AddRange(cards);
}
```

**Test Verification:**
- ParrelSync 2-player test: both players see same shop card count
- Unit test: `ShopState_ConsistentAcrossNetwork()`

---

#### BUG 3: Player 2 Buy Action Fails 🔴
**Task:** M4
**Symptom:** Player 2 clicks buy → coins deducted but no card added to hand/board
**Root Cause:** Caused by BUG 1 - client sends wrong `shopIndex` to host
**Impact:** Player 2 cannot buy cards, game is unplayable
**Files:**
- `NetworkGameBridge.cs` → `RPC_RequestBuyCard()` (host-side validation)
- `Player.cs` → `BuyCard()` (may fail silently)

**Investigation Steps:**
1. Add debug logging to `RPC_RequestBuyCard()` on host:
   ```csharp
   Debug.Log($"[Host] RPC_RequestBuyCard: sender={senderSlot}, shopIndex={shopIndex}, " +
             $"shopSize={player.GetShop().Count}, coins={player.coins}");
   ```
2. Check if shopIndex is out of bounds
3. Check if `Player.BuyCard()` has validation that returns early without error

**Expected Fix:**
```csharp
// NetworkGameBridge.cs - RPC_RequestBuyCard()
[PunRPC]
private void RPC_RequestBuyCard(int shopIndex, PhotonMessageInfo info)
{
    int senderSlot = GetSenderSlot(info);
    if (senderSlot < 0 || senderSlot >= GameManager.Instance.players.Count)
    {
        Debug.LogError($"[Host] Invalid sender slot: {senderSlot}");
        return;
    }

    Player player = GameManager.Instance.players[senderSlot];
    List<Card> shop = TavernManager.Instance.GetShopForPlayer(senderSlot);

    // Validate shopIndex against actual shop size
    if (shopIndex < 0 || shopIndex >= shop.Count)
    {
        Debug.LogError($"[Host] Invalid shopIndex {shopIndex}, shop has {shop.Count} cards");
        return;
    }

    player.BuyCard(shopIndex);
    BroadcastPlayerState(senderSlot);
}
```

**Dependencies:** Must fix BUG 1 (M3) first to ensure shop state is synced

**Test Verification:**
- ParrelSync 2-player test: Player 2 can successfully buy cards
- Unit test: `NetworkBuy_PlayerTwoCanBuyCards()`

---

### IMPORTANT (P1) - Affects UX

#### BUG 2: Tavern Upgrade Cost Not Synced 🟡
**Task:** M5
**Symptom:** Player 2 (non-host) sees incorrect tavern upgrade cost
**Root Cause:** `NetworkPlayerState.upgradeCost` exists but client may not use it
**Impact:** Confusing UX, player might think they can/can't afford upgrade
**Files:**
- `NetworkPlayerState.cs` → line 17 has `upgradeCost` field
- `Player.cs` → `GetUpgradeCost()` needs to check `SyncedUpgradeCost`
- `GameManager.cs` → `ApplyNetworkPlayerState()` needs to set `SyncedUpgradeCost`

**Expected Fix:**
```csharp
// Player.cs - GetUpgradeCost()
public int GetUpgradeCost()
{
    // In multiplayer, use synced value from host if available
    if (GameManager.Instance.IsOnlineMode && !GameManager.Instance.IsHost)
    {
        if (SyncedUpgradeCost >= 0)
            return SyncedUpgradeCost;
    }

    // Local calculation (host or offline mode)
    if (currentTavernTier >= 6)
        return 0; // Already max tier

    int targetTier = currentTavernTier + 1;
    if (!baseUpgradeCosts.ContainsKey(targetTier))
        return 999;

    int baseCost = baseUpgradeCosts[targetTier];
    int turnsSinceTier = tierTurnCounter.ContainsKey(currentTavernTier)
        ? tierTurnCounter[currentTavernTier] : 0;

    return Mathf.Max(0, baseCost - turnsSinceTier);
}
```

**Test Verification:**
- ParrelSync 2-player test: both players see same upgrade cost
- Unit test: `UpgradeCost_SyncedToAllClients()`

---

### POLISH (P2) - Code Quality

#### Issue: Combat Log Shows All Players 🟢
**Task:** M7
**Symptom:** Combat log shows events for all matches, not just local player
**Expected:** Each player sees only their own combat events
**Files:**
- `CombatLogUI.cs` → filter by local player
- `CombatManager.cs` → tag events with player index

---

#### Issue: RefreshShop Uses Direct Assignment 🟢
**Task:** M8
**Symptom:** `TavernManager.RefreshShop()` does `player.coins -= 1` instead of using property setter
**Expected:** Use `player.coins = player.coins - 1` to trigger events
**Files:**
- `TavernManager.cs` → `RefreshShop()` method

---

## 📋 Phase I: AWS Online Multiplayer (Next Sprint)

**Status:** 🔜 Planned (after Phase M complete)
**Goal:** Deploy to AWS infrastructure for online matchmaking
**Duration:** 10-15 days (~80 hours of work)

### Phase I Task Groups

#### Sprint 13: Auth Infrastructure (I1-I5)
- I1: AWS Cognito user authentication
- I2: Player profile service (DynamoDB)
- I3: Session token management
- I4: Account linking (guest → registered)
- I5: Security audit (OWASP checklist)

#### Sprint 14: Real-time Networking (I6-I8)
- I6: Photon Cloud configuration
- I7: Region selection and latency testing
- I8: Connection resilience (reconnect logic)

#### Sprint 15: Matchmaking & Testing (I9-I13)
- I9: Matchmaking queue (Lambda + SQS)
- I10: ELO/MMR system (placeholder)
- I11: Lobby system redesign
- I12: Anti-cheat validation (server-side)
- I13: Load testing (100 concurrent players)

---

## 🏗️ Architecture Overview

### Core Systems

```
GameManager (Singleton)
├── Game loop & phase management
├── Player state tracking (health, elimination)
├── AI controller management
└── Matchmaking history

TavernManager (Singleton)
├── Master card pool (30 cards, 5 per tier)
├── Per-player shop state
├── Card reservation system
└── Shop generation logic

CombatManager (Static)
├── Battle simulation
├── Damage calculation
├── Ability trigger coordination
└── Combat logging

SynergyManager (Singleton)
├── Tribe counting (Pentacles, Cups, Swords, Wands)
├── Threshold activation (2/4/6)
├── Per-player synergy snapshots (M1 compliant)
└── Cross-tribe combo tracking

NetworkGameBridge (Singleton)
├── RPC routing (client → host requests)
├── State broadcasting (host → all clients)
├── Slot assignment (ActorNumber ↔ PlayerIndex)
└── Event coordination
```

### Multiplayer Architecture

**Model:** Host-authoritative
**Flow:**
1. Client sends action request via RPC (e.g., `RequestBuyCard`)
2. Host validates and executes action
3. Host broadcasts updated state via RPC (e.g., `BroadcastPlayerState`)
4. All clients apply state update via `GameManager.ApplyNetworkPlayerState()`

**Key Design Decisions:**
- ✅ Per-player synergy calculation (no global state)
- ✅ Card pool reservation system (prevents duplicates)
- ✅ NetworkPlayerState includes all UI-relevant fields
- ✅ Shop state fully synced per player
- ✅ Discovery queue per player (no race conditions)

---

## 🧪 Test Coverage

### Test Suites (124 tests, 86.3% pass rate)

| Suite | Tests | Pass | Fail | Coverage |
|-------|-------|------|------|----------|
| **AIBattleTests** | 15 | 15 | 0 | 100% ⭐ |
| **SynergyTests** | 43 | 43 | 0 | 100% ⭐ |
| **BugFixVerificationTests** | 30 | 26 | 4 | 86.7% ✅ |
| **CombatTests** | 8 | 7 | 1 | 87.5% ✅ |
| **EconomyTests** | 15 | 11 | 4 | 73.3% ⚠️ |
| **EdgeCaseTests** | 8 | 4 | 4 | 50.0% ⚠️ |
| **CardSystemTests** | 6 | 2 | 4 | 33.3% ⚠️ |

### Test Coverage Gaps (Phase M Tests Needed)

| Test | Validates | Priority | Status |
|------|-----------|----------|--------|
| `ShopState_ConsistentAcrossNetwork()` | M3 - Shop sync | P0 | 🔴 TODO |
| `NetworkBuy_PlayerTwoCanBuyCards()` | M4 - Buy RPC | P0 | 🔴 TODO |
| `SynergyCalculation_PerPlayer()` | M1 - No global state | P0 | 🔴 TODO |
| `DiscoveryQueue_NoConcurrency()` | M2 - Race condition | P1 | 🟡 TODO |
| `UpgradeCost_SyncedToAllClients()` | M5 - Cost sync | P1 | 🟡 TODO |
| `AbilityManager_CleanupBetweenGames()` | M6 - Memory leak | P1 | 🟡 TODO |
| `CombatLog_LocalPlayerFilter()` | M7 - Log filter | P2 | 🟢 TODO |
| `RefreshShop_UsesPropertySetter()` | M8 - Coin events | P2 | 🟢 TODO |

**Total Effort for Test Suite:** ~14 hours

### Test Infrastructure Issues

**Problem:** 17 failing tests all show:
```
[Error] Player 1: Cannot buy card, TavernManager not found!
```

**Root Cause:** Test fixtures don't initialize TavernManager singleton before calling Player methods

**Solution:** Create `TestHelper.SetupGameEnvironment()` utility:
```csharp
// Assets/Scripts/Tests/Editor/TestHelper.cs
public static class TestHelper
{
    public static (TavernManager, Player, SynergyManager) SetupGameEnvironment()
    {
        // Create and initialize TavernManager
        var tavernObj = new GameObject("TavernManager");
        var tavern = tavernObj.AddComponent<TavernManager>();
        tavern.masterCards = CreateTestCardPool();
        tavern.ResetPool();

        // Create and initialize SynergyManager
        var synergyObj = new GameObject("SynergyManager");
        var synergy = synergyObj.AddComponent<SynergyManager>();
        synergy.tribeSynergies = SynergyTestData.CreateAllTribeSynergies();
        synergy.InitializeSynergyCache();

        // Create Player with dependencies
        var playerObj = new GameObject("TestPlayer");
        var player = playerObj.AddComponent<Player>();
        player.playerId = 1;
        player.coins = 10;
        player.currentTavernTier = 1;

        return (tavern, player, synergy);
    }
}
```

**Expected Impact:** Should fix all 17 failing tests, bringing pass rate to ~95%+

---

## 📜 Recent Changes (Last 5 Commits)

### Commit: `5de9b21` - Fix 2 bugs from audit round 10
**Date:** February 2, 2026
**Files:** SynergyManager.cs, TavernManager.cs, ShopUI.cs
**Changes:**
- Fixed shop synergy cost calculation
- Fixed combat damage counting (filter alive cards only)
**Tests:** 107/124 passing

### Commit: `6a63991` - Fix 6 bugs from audit consensus
**Date:** February 1, 2026
**Files:** CombatManager.cs, Player.cs, TavernManager.cs, ShopUI.cs, GameManager.cs, LobbyManager.cs
**Changes:**
- Fixed damage tier calculation
- Fixed golden card sell value
- Fixed discovery reservation
- Fixed shop freeze logic
- Fixed base stats preservation
- Fixed disconnect cleanup
**Tests:** 107/124 passing

### Commit: `3042960` - Fix 3 bugs from audit round 9
**Date:** January 31, 2026
**Files:** Player.cs, SynergyManager.cs, AIController.cs
**Changes:**
- Fixed DRY cost reduction logic
- Fixed tier counter tracking
- Removed excessive debug logs
**Tests:** Synergy 43/43, AI 15/15 passing

### Commit: `5f3684b` - Fix 2 bugs from audit round 9
**Date:** January 31, 2026
**Files:** CombatManager.cs, TavernManager.cs
**Changes:**
- Fixed golden flag persistence on sell
- Fixed damage calculation formula
**Tests:** Combat 7/8 passing

### Commit: `a39db49` - Fix 2 bugs from audit round 7
**Date:** January 30, 2026
**Files:** Player.cs, SynergyManager.cs
**Changes:**
- Fixed coin cap enforcement
- Fixed BuffAllFriendly includes self
**Tests:** Synergy 43/43 passing

---

## 🎯 Success Criteria

### Phase M Completion Checklist

- [ ] All 8 Phase M tasks (M1-M8) complete
- [ ] 3 open multiplayer bugs resolved
- [ ] 8 new Phase M tests written and passing
- [ ] Test pass rate ≥ 95% (118/124 tests)
- [ ] 2-player ParrelSync test successful (full game completion)
- [ ] All changes committed with proper format
- [ ] MULTIPLAYER_BUGS.md updated with resolutions
- [ ] Ready to proceed to Phase I (AWS infrastructure)

### Phase I Completion Checklist (Future)

- [ ] AWS Cognito authentication working
- [ ] Player profiles stored in DynamoDB
- [ ] Photon Cloud configured with regions
- [ ] Matchmaking queue functional
- [ ] 100 concurrent players load test passed
- [ ] Anti-cheat validation on server
- [ ] Deployment pipeline to AWS
- [ ] Monitoring and alerting setup

---

## 📝 Open Questions & Decisions

### Design Decisions (Resolved)

**Q:** Should synergy calculation be global or per-player?
**A:** ✅ Per-player (M1 architecture). Each player has independent synergy state via `SynergySnapshot`.

**Q:** Should shop state be synced on every action or batched?
**A:** ✅ Synced on every state-changing action via `BroadcastPlayerState()` and separate `BroadcastShopForPlayer()`.

**Q:** Should upgrade cost be calculated client-side or host-authoritative?
**A:** ✅ Host-authoritative (M5). Host calculates and syncs via `NetworkPlayerState.upgradeCost`.

### Open Questions (Pending)

**Q:** Should we add reconnect logic in Phase M or defer to Phase I?
**A:** 🔜 Defer to Phase I (I8 task). Phase M focuses on core multiplayer stability.

**Q:** Should combat log be filtered on send (host) or receive (client)?
**A:** 🔜 TBD in M7 implementation. Likely filter on receive for simplicity.

**Q:** Should we support spectator mode in Phase I?
**A:** 🔜 Defer to Phase J (post-MVP). Focus on player experience first.

---

## 🔄 Development Workflow

### Bug Fix Protocol

1. **Identify:** Find bug via audit, testing, or user report
2. **Document:** Add to PLAN.md Known Issues section with severity
3. **Investigate:** Add debug logging, reproduce in ParrelSync
4. **Fix:** Implement fix following coding standards
5. **Test:** Write unit test, verify with ParrelSync
6. **Commit:** Use format `[Sprint X] Task ID: Brief description`
7. **Update Docs:** Update PLAN.md, MULTIPLAYER_BUGS.md, changelog

### Commit Format

```
[Sprint X] Task ID: Brief description
- Detail 1
- Detail 2
- Test verification
- Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

Example:
```
[Sprint 12] M4: Fix Player 2 buy RPC sync
- Added debug logging to RPC_RequestBuyCard
- Fixed SetShopFromNetwork to clear before adding
- Added shopIndex validation on host
- Verified with ParrelSync 2-player test
- Test: NetworkBuy_PlayerTwoCanBuyCards PASSED
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

### Testing Protocol

**Before Every Commit:**
1. Run relevant unit tests (`Unity -runTests -testPlatform EditMode`)
2. Check for regressions (compare test results)
3. Manual testing in Unity Editor (if UI changes)

**Before Every PR:**
1. Run full test suite (all 124+ tests)
2. ParrelSync 2-player test (for multiplayer changes)
3. Code review by AI agent or human
4. Update PLAN.md with task status changes

---

## 📚 Key Files Reference

### Core Game Systems
- `Assets/Scripts/GameManager.cs` - Game loop, phase management
- `Assets/Scripts/Player.cs` - Player state, economy, actions
- `Assets/Scripts/TavernManager.cs` - Shop system, card pool
- `Assets/Scripts/CombatManager.cs` - Battle simulation
- `Assets/Scripts/Synergies/SynergyManager.cs` - Tribe synergies

### Multiplayer Systems
- `Assets/Scripts/Network/NetworkGameBridge.cs` - RPC routing
- `Assets/Scripts/Network/NetworkPlayerState.cs` - State serialization
- `Assets/Scripts/Network/NetworkGameSetup.cs` - Slot assignment
- `Assets/Scripts/Network/NetworkCardData.cs` - Card serialization

### UI Systems
- `Assets/Scripts/UI/GameUIManager.cs` - Main UI controller
- `Assets/Scripts/UI/ShopUI.cs` - Shop display and interaction
- `Assets/Scripts/UI/BoardUI.cs` - Board display
- `Assets/Scripts/UI/HandUI.cs` - Hand display
- `Assets/Scripts/UI/DiscoveryUI.cs` - Discovery choice panel

### Test Suites
- `Assets/Scripts/Tests/Editor/SynergyTests.cs` - 43 synergy tests
- `Assets/Scripts/Tests/Editor/AIBattleTests.cs` - 15 AI tests
- `Assets/Scripts/Tests/Editor/BugFixVerificationTests.cs` - 30 fix verification tests
- `Assets/Scripts/Tests/Editor/CombatTests.cs` - 8 combat tests
- `Assets/Scripts/Tests/Editor/EconomyTests.cs` - 15 economy tests

### Documentation (External Repo)
- `TarotBattlegrounds-docs/` - Full documentation repository
- Reference: `README.md` links to docs repo

---

## 🚀 Next Steps (Immediate)

### Today (February 5, 2026)
1. ✅ Create PLAN.md (this file)
2. 🔴 Fix M3: Shop pool card reservation (4h)
3. 🔴 Fix M4: Player 2 buy RPC sync (3h)
4. ✅ Test with ParrelSync (30min)
5. ✅ Commit changes with proper format

### Tomorrow (February 6, 2026)
6. 🟡 Fix M2: DiscoveryUI per-player queue (2h)
7. 🟡 Fix M5: Tavern upgrade state sync (2h)
8. 🟡 Fix M6: AbilityManager memory leak (3h)
9. ✅ Write 4 Phase M tests (2h)

### Day After (February 7, 2026)
10. 🟢 Fix M8: RefreshShop coin setter (1h)
11. 🟢 Fix M7: Combat log local filter (2h)
12. ✅ Write remaining 4 Phase M tests (2h)
13. ✅ Full regression test (2h)
14. ✅ Update all documentation
15. ✅ Merge to develop, prepare for Phase I

---

## 📞 Contact & Resources

**Project Lead:** squanchy667
**Contributors:** Theylon, Claude (Anthropic), Grok (xAI)
**Unity Version:** 2023 LTS
**Test Framework:** NUnit 3.5.0.0
**Networking:** Photon PUN 2

**Repositories:**
- Code: `TarotBattlegrounds-POC` (this repo)
- Docs: `TarotBattlegrounds-docs` (external)

**Documentation Links:**
- Architecture: `TarotBattlegrounds-docs/developer/architecture.md`
- Combat System: `TarotBattlegrounds-docs/developer/combat-system.md`
- Tavern System: `TarotBattlegrounds-docs/developer/tavern-system.md`
- Known Issues: `TarotBattlegrounds-docs/resources/known-issues.md`
- Changelog: `TarotBattlegrounds-docs/resources/changelog.md`

---

**PLAN.md Version 2.0**
**Source of Truth:** This document supersedes all conflicting information
**Last Audit:** February 5, 2026 (Full Pipeline Audit - Score: 8.05/10)
**Next Review:** February 10, 2026 (after Phase M completion)

---

*"Divine your path to victory"* 🃏✨
