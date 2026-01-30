# Multiplayer 1v1 - Known Bugs & TODO

**Date:** 2026-01-30
**Branch:** `tarot-skin`
**Status:** First successful 2-player session achieved. Both players connect as separate characters. Several gameplay bugs remain.

---

## Fixes Completed Today

1. **Lobby EventSystem missing** - Buttons and input fields in the Lobby scene were unresponsive. The scene already had an EventSystem but it wasn't being recognized; a duplicate was added then cleaned up.

2. **Both clients controlling same player (race condition)** - `GameUIManager.Start()` read `NetworkGameBridge.LocalPlayerSlot` before `NetworkGameSetup.AssignSlots()` had run (coroutine timing). Fixed by adding `OnNetworkSlotsAssigned()` callback after slot assignment.

3. **Client not in multiplayer mode** - `GameConfig.CurrentGameMode` was never set to `Multiplayer` on the client because `RoomManager.StartGame()` only runs on the host. Fixed by setting the game mode in `LobbyUI.Start()` since entering the Lobby inherently means multiplayer.

4. **Identical player names on ParrelSync clones** - Both editors showed "Player5441" because `GameConfig.PlayerName` uses `PlayerPrefs` which clones share. Fixed by detecting clone via `Application.dataPath.Contains("_clone_")` and generating a unique name.

5. **JoinRoom crash** - "Client not ready for operations" error when clicking a room entry while Photon was still transitioning servers. Fixed by adding `IsConnectedAndReady` guards to `CreateRoom`, `JoinRoom`, `JoinRandomRoom`.

6. **Switch Player button removed** - Debug/testing button was visible in the game. Removed the button GameObject from `Game.unity` and all code references from `GameUIManager.cs`.

---

## Open Bugs (TODO for next session)

### BUG 1: Tavern shop shows too few minions on turn start
- **Symptom:** At the beginning of a turn, the tavern sometimes only shows 2 minions instead of the expected full shop (3 for tier 1).
- **Expected:** Each turn should present a fresh, full tavern shop based on the player's current tier.
- **Where to look:**
  - `GameManager.cs` → `RecruitPhase()` — calls `player.RefreshShop(turnNumber)` for each player
  - `NetworkGameBridge.cs` → `BroadcastShopForPlayer()` — sends shop cards to the owning client
  - `TavernManager.cs` → shop generation logic, check if it's generating the correct number of cards per tier
  - Possible cause: shop is being broadcast before the host finishes generating it, or the client's shop display isn't refreshing after receiving the RPC

### BUG 2: Tavern upgrade cost not synced for Player 2
- **Symptom:** Player 2 (the non-host client) sees incorrect tavern upgrade cost — it wasn't decreased as expected.
- **Expected:** Upgrade cost should match what the host calculates and be synced via `BroadcastPlayerState`.
- **Where to look:**
  - `NetworkPlayerState` — check if `tavernTier` is being synced but upgrade cost is derived and not recalculated on the client
  - `Player.cs` → `GetUpgradeCost()` — may depend on local state that isn't synced
  - `GameManager.cs` → `ApplyNetworkPlayerState()` — verify that all fields needed for upgrade cost display are reconstructed
  - Possible cause: upgrade cost is derived from `currentTavernTier` but the tier value on the client might be stale or not updated before the UI reads it

### BUG 3: Player 2 buy action deducts coins but no minion appears
- **Symptom:** On the second turn, Player 2 clicked buy — coins were deducted but no minion was added to hand/board.
- **Expected:** BuyCard should add the minion to the player's hand and sync the updated state back.
- **Where to look:**
  - `NetworkGameBridge.cs` → `RPC_RequestBuyCard()` — runs on host, calls `player.BuyCard(shopIndex)`. Check if the shopIndex from the client matches the host's shop state
  - `Player.cs` → `BuyCard()` — may fail silently if hand is full or card index is out of range
  - Possible cause: client's shop display is out of sync with host's actual shop state, so the `shopIndex` sent in the RPC doesn't match. The client thinks it's buying card at index X but the host's shop has different cards at that index
  - Related to BUG 1 — if the shop display is wrong, click indices will be wrong too

---

## Architecture Notes (for context)

### How multiplayer works
- **Host-authoritative model**: only the MasterClient (host) runs the game loop and modifies game state
- **Client → Host**: RPCs send action requests (buy, sell, play, reroll, upgrade, end turn)
- **Host → All**: RPCs broadcast state updates (`BroadcastPlayerState`, `BroadcastShopForPlayer`, `BroadcastPhaseChange`)
- **Client UI**: receives state via RPCs and applies via `GameManager.ApplyNetworkPlayerState()`

### Key files
| File | Role |
|------|------|
| `NetworkGameSetup.cs` | Maps Photon ActorNumbers to game player slots on scene load |
| `NetworkGameBridge.cs` | All RPC routing (requests + broadcasts), PhotonView ID=1 |
| `GameManager.cs` | Game loop (host only), state management, combat |
| `GameUIManager.cs` | UI controller, locked to `activePlayerIndex` per client |
| `LobbyUI.cs` | Lobby connection/room management |
| `PhotonConnector.cs` | Photon connection lifecycle singleton |
| `RoomManager.cs` | Room create/join/start operations |

### How to test
1. Open **Lobby** scene in both the main editor and ParrelSync clone
2. Press Play in both
3. Editor 1: create a room → Editor 2: join it → Editor 1: Start Game
4. Both should load Game scene as separate players (slot 0 = host, slot 1 = client)

---

## Potential Root Cause Theory

All three open bugs may share a common root cause: **shop state desync between host and client**. The host generates the shop, but the client receives it asynchronously via RPC. If the client's shop display doesn't match the host's actual shop data (wrong cards, wrong count, or stale data), then:
- The shop appears short (BUG 1)
- Derived values like upgrade cost may be wrong (BUG 2)
- Buy actions send the wrong `shopIndex` to the host (BUG 3)

**Suggested investigation order:**
1. Add debug logging to `BroadcastShopForPlayer` and `RPC_SyncShopForPlayer` to verify card counts
2. Check if `TavernManager.SetShopFromNetwork()` properly replaces the shop or appends
3. Verify the shop UI refreshes after receiving the network shop data
4. Check if Player 2's initial shop is generated before or after `NetworkGameSetup` assigns slots
