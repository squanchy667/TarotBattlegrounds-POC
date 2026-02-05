# Clone Player Issues - Bug Analysis

**Date:** February 5, 2026
**Test Session:** 0502202620:59 (24,611 host lines + 28,673 clone lines)
**Reporter:** User (manual testing from clone editor)

---

## Summary

User reported 2 critical issues when playing from the clone (client):
1. **No abilities triggering**
2. **No discovery UI when forming golden minions**

After log analysis, I found:
- ✅ Abilities ARE triggering on the HOST
- ❌ Ability EFFECTS are NOT synced to clients
- ✅ Discovery events ARE triggered on the HOST
- ❌ Discovery UI is NOT shown to client players

---

## Bug 6: Abilities Not Visible to Clone Player

### Symptom
User reported: "no abilities" when playing from clone editor

### Investigation

**Host Log Evidence:**
```
Line 10411: [AbilityManager] Registered BattlecryAbility for Gold Collector
Line 10444: Player 2: Bought Gold Collector
Line 10544: [Ability] Gold Collector triggers BattlecryAbility: Battlecry: Gain Aegis
Line 10560: [AbilityEffect] Gold Collector gains Aegis
Line 10578: Player 2: Played Gold Collector to board position 2

Line 14104: [AbilityManager] Registered BattlecryAbility for Sword Captain
Line 14237: [Ability] Sword Captain triggers BattlecryAbility: Battlecry: Give all friendly minions +1 Attack
Line 14248: [AbilityEffect] Sword Captain gains +1 Attack (now 5)
Line 14329: Player 2: Played Sword Captain to board position 3
```

**Clone Log Evidence:**
```
Abilities registered: ✓ (lines 232, 527, 613, etc.)
Ability triggers: ✗ (0 found)
Ability effects: ✗ (0 found)
```

### Root Cause

**Abilities ARE working on the host**, but the effects are not synchronized to clients!

**Flow Analysis:**
1. Clone player clicks "Play Card"
2. RPC sent to host: `RequestPlayCard(handIndex, boardPos)`
3. Host receives RPC → calls `RPC_RequestPlayCard`
4. Host executes `player.PlayCard(handIndex, boardPos)` on Player 2
5. PlayCard triggers battlecry (line 427 in Player.cs):
   ```csharp
   var battlecryContext = AbilityManager.CreateBattlecryContext(card, this);
   AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, battlecryContext);
   ```
6. Ability executes on HOST → modifies card stats (Attack, Health, Aegis, etc.)
7. Host broadcasts player state: `BroadcastPlayerState(slot)`

**The Problem:**
`BroadcastPlayerState()` sends:
- Player stats (coins, tier, health)
- Hand cards
- Board cards
- Shop cards

**But card stats (Attack/Health) are embedded in NetworkCardData**, which IS included in the broadcast. So why doesn't the clone see the changes?

**Hypothesis:** Timing issue. The ability effects modify the card AFTER it's already on the board, but the broadcast might happen before all effects are applied, OR the effects are applied but not immediately reflected in the card's base stats.

**Alternate Hypothesis:** The card stats ARE broadcast, but the clone's UI doesn't update to show the changes.

### Testing Required
Need to verify:
1. Are card stats (Attack/Health) included in NetworkCardData?
2. Are stats updated BEFORE BroadcastPlayerState is called?
3. Does the clone receive the updated stats?
4. Does the clone's UI display the updated stats?

### Potential Fixes

**Option 1:** Add explicit RPC to broadcast ability effects
```csharp
// In AbilityBase.Execute or AbilityEffects
if (GameManager.Instance.IsOnlineMode && GameManager.Instance.IsHost)
{
    NetworkGameBridge.Instance.BroadcastAbilityEffect(cardId, effectType, value);
}
```

**Option 2:** Ensure BroadcastPlayerState is called AFTER all ability effects complete
```csharp
// In Player.PlayCard, after ability triggers:
AbilityManager.TriggerAbilities(AbilityTrigger.Battlecry, battlecryContext);

// Wait for effects to apply (might need coroutine or callback)
yield return null; // Or wait for ability completion

// Then broadcast
if (NetworkGameBridge.Instance != null)
    NetworkGameBridge.Instance.BroadcastPlayerState(playerId - 1);
```

**Option 3:** Trigger abilities on clients too (parallel execution)
```csharp
// In RPC_RequestPlayCard, broadcast a separate "PlayCard" event to all clients
photonView.RPC(nameof(RPC_PlayerPlayedCard), RpcTarget.All, slot, handIndex, boardPos);
// Clients execute PlayCard locally, triggering abilities on their own copies
```

---

## Bug 7: Discovery UI Not Showing for Clone Player

### Symptom
User reported: "no discovering a card when making golden at all" from clone editor

### Investigation

**Host Log Evidence:**
```
Line 6771: Player 2: Triple detected for Coin Apprentice! Creating golden version.
Line 6802: [TavernManager] Reserved 3 discovery cards from pool (pool size: 412)
Line 6818: Player 2: Triple discovery! Offering 3 tier 3 cards.
Line 6834: [DiscoveryUI/M2] Player 2 discovery stored, but not showing UI (localSlot=0)
```

**Clone Log Evidence:**
```
Triple detection messages: ✗ (0 found)
Discovery triggered messages: ✗ (0 found)
DiscoveryUI messages: ✗ (0 found)
```

### Root Cause

**Discovery IS triggered on the host**, but the UI is not shown to the clone player!

**Flow Analysis:**
1. Clone player forms a triple (buys 3rd copy of a card)
2. Triple detection happens on HOST (host-authoritative)
3. Host calls `CheckAndResolveTriples()` on Player 2 object
4. Golden minion created
5. Discovery triggered: `OnTripleDiscovery?.Invoke(this, discoveryCards)`
6. DiscoveryUI.ShowDiscovery() is called **on the host**
7. ShowDiscovery checks: "Is this the local player?" (line 110 in DiscoveryUI.cs)
   ```csharp
   int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
   if (player.playerId - 1 != localSlot)
   {
       // Not our discovery, skip UI
       return; // ← HOST RETURNS HERE because localSlot=0 (Player 1) but triple is for Player 2
   }
   ```
8. Clone never receives any message to show discovery UI!

**The Problem:**
- `OnTripleDiscovery` is a **local C# event**, not an RPC
- The event fires on the HOST's Player 2 object
- Only the HOST's DiscoveryUI is subscribed to that event
- The CLONE's DiscoveryUI is subscribed to the CLONE's Player 2 object (different instance)
- No RPC is sent to tell the clone to show discovery!

### Current Discovery Network Flow

Looking at the code, discovery choice IS networked:
1. Player clicks discovery choice
2. `DiscoveryUI.OnChoiceClicked()` calls `NetworkGameBridge.Instance.RequestDiscoveryChoice(index)`
3. RPC sent to host: `RPC_RequestDiscoveryChoice`
4. Host adds chosen card to player's hand

**But the INITIAL DISPLAY of discovery UI is not networked!**

### Fix Required

Add an RPC to show discovery UI on the client:

**In Player.cs CheckAndResolveTriples():**
```csharp
List<Card> discoveryCards = tavern.GetDiscoveryCards(discoveryTier, 3);
if (discoveryCards.Count > 0)
{
    _pendingDiscoveryCards = new List<Card>(discoveryCards);
    Debug.Log($"Player {playerId}: Triple discovery! Offering {discoveryCards.Count} tier {discoveryTier} cards.");

    // Trigger local event (for host's UI)
    OnTripleDiscovery?.Invoke(this, discoveryCards);

    // NEW: In online mode, send RPC to show discovery on client
#if PHOTON_UNITY_NETWORKING
    if (GameManager.Instance.IsOnlineMode && NetworkGameBridge.Instance != null)
    {
        NetworkGameBridge.Instance.BroadcastDiscoveryForPlayer(playerId - 1, discoveryCards);
    }
#endif
}
```

**In NetworkGameBridge.cs:**
```csharp
public void BroadcastDiscoveryForPlayer(int playerIndex, List<Card> cards)
{
    if (!IsHost) return;

    var cardDataArray = NetworkCardData.FromCardList(cards);
    var json = JsonConvert.SerializeObject(new DiscoverySyncData
    {
        playerIndex = playerIndex,
        discoveryCards = cardDataArray
    });

    photonView.RPC(nameof(RPC_ShowDiscovery), RpcTarget.Others, json);
}

[PunRPC]
private void RPC_ShowDiscovery(string json)
{
    DiscoverySyncData data = JsonConvert.DeserializeObject<DiscoverySyncData>(json);

    if (GameManager.Instance == null) return;

    Player player = GameManager.Instance.players[data.playerIndex];
    List<Card> cards = NetworkCardData.ToCardList(data.discoveryCards);

    // Store for later resolution
    DiscoveryUI.PendingDiscoveryByPlayer[player.playerId] = cards;

    // Show UI if this is the local player
    if (data.playerIndex == LocalPlayerSlot)
    {
        // Find DiscoveryUI and show
        var discoveryUI = FindObjectOfType<DiscoveryUI>();
        if (discoveryUI != null)
        {
            discoveryUI.ShowDiscoveryFromNetwork(player, cards);
        }
    }
}

[System.Serializable]
public class DiscoverySyncData
{
    public int playerIndex;
    public NetworkCardData[] discoveryCards;
}
```

---

## Summary of Fixes Needed

### Bug 6: Abilities Not Syncing
**Priority:** HIGH (P0) - Breaks core gameplay for clients

**Fix approach:**
1. Verify card stats are in NetworkCardData ✓
2. Add ability effect logging to track sync
3. Either:
   - Add explicit ability effect RPCs, OR
   - Ensure BroadcastPlayerState includes all stat changes, OR
   - Trigger abilities client-side (parallel execution)

### Bug 7: Discovery UI Not Showing
**Priority:** HIGH (P0) - Breaks golden minion mechanic for clients

**Fix approach:**
1. Add `BroadcastDiscoveryForPlayer()` RPC
2. Add `RPC_ShowDiscovery()` handler
3. Call from `CheckAndResolveTriples()` after local event

---

## Testing Verification

### For Bug 6 (Abilities):
1. Clone player buys and plays Sword Captain
2. **Expected:** All friendly minions gain +1 Attack (visible in clone's UI)
3. **Expected:** Clone log shows `[AbilityEffect] X gains +1 Attack`

### For Bug 7 (Discovery):
1. Clone player forms triple
2. **Expected:** Discovery UI appears on clone editor
3. **Expected:** Clone log shows `[DiscoveryUI/M2] Showing discovery for local player X`
4. **Expected:** Clone can click and choose a card

---

**Next Step:** Implement fixes for both bugs and re-test in ParrelSync.
