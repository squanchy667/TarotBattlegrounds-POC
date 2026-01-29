# Online Multiplayer Setup Guide (Photon PUN 2)

All networking code has been written. Follow these steps in Unity to wire everything up.

---

## Step 1: Install Photon PUN 2

1. Open Unity Editor
2. Go to **Window > Asset Store** (or open Unity Asset Store in browser)
3. Search for **"PUN 2 - FREE"** by Exit Games
4. Import the package into your project
5. When prompted, enter your **Photon App ID** (get one at https://dashboard.photonengine.com)
   - Create a free account if needed (free tier = 20 CCU)
   - Create a new **Photon PUN** app
   - Copy the App ID
6. The Photon Setup Wizard will open — paste your App ID and click **Setup Project**
7. This creates `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`

## Step 2: Install Newtonsoft JSON (if not already present)

The network layer uses `Newtonsoft.Json` for serialization. If not already installed:

1. Go to **Window > Package Manager**
2. Click **+ > Add package by name**
3. Enter: `com.unity.nuget.newtonsoft-json`
4. Click **Add**

## Step 3: Create the Lobby Scene

1. **File > New Scene** (Basic)
2. Save as `Assets/Scenes/Lobby.unity`
3. In the scene, create the following hierarchy:

```
Lobby Scene
├── Main Camera
├── EventSystem
├── Canvas (Screen Space - Overlay)
│   ├── ConnectionPanel (Panel)
│   │   ├── ConnectionStatusText (TMP_Text) — "Connecting..."
│   │   └── PlayerNameInput (TMP_InputField)
│   │
│   ├── RoomBrowserPanel (Panel)
│   │   ├── RoomNameInput (TMP_InputField) — placeholder "Room Name"
│   │   ├── MaxPlayersDropdown (TMP_Dropdown)
│   │   ├── CreateRoomButton (Button) — "Create Room"
│   │   ├── JoinRandomButton (Button) — "Quick Join"
│   │   ├── BackToMenuButton (Button) — "Back"
│   │   ├── RoomListContainer (empty Transform, vertical layout)
│   │   └── NoRoomsText (TMP_Text) — "No rooms available"
│   │
│   └── RoomInteriorPanel (Panel)
│       ├── RoomTitleText (TMP_Text)
│       ├── PlayerListText (TMP_Text)
│       ├── StartGameButton (Button) — "Start Game"
│       ├── LeaveRoomButton (Button) — "Leave Room"
│       └── RoomStatusText (TMP_Text)
│
└── LobbyUI (empty GameObject)
    └── [Add Component: LobbyUI.cs]
```

4. Wire the LobbyUI serialized fields in the Inspector to the corresponding UI elements
5. **Optional:** Create a `RoomListEntry` prefab (a Button with a TMP_Text child) and assign it to `roomListEntryPrefab`

## Step 4: Add Scenes to Build Settings

1. **File > Build Settings**
2. Add scenes in this order:
   - `Assets/Scenes/MainMenu.unity` (index 0)
   - `Assets/Scenes/Lobby.unity` (index 1)
   - `Assets/Scenes/Game.unity` (index 2)

## Step 5: Set Up Network Objects in the Game Scene

Open `Assets/Scenes/Game.unity` and add:

### 5a. NetworkGameBridge

1. Create an empty GameObject named **"NetworkGameBridge"**
2. Add component: `NetworkGameBridge.cs`
3. Add component: `PhotonView` (added automatically via RequireComponent)
4. Set the PhotonView **View ID = 1** (important — must be a scene-scoped view)
5. Set Ownership Transfer: Fixed
6. Observed Components: None needed (we use RPCs, not serialization)

### 5b. NetworkGameSetup

1. Create an empty GameObject named **"NetworkGameSetup"**
2. Add component: `NetworkGameSetup.cs`
3. This object self-destructs in offline mode, so it's safe to leave in the scene

## Step 6: Verify Offline Mode Still Works

1. Open MainMenu scene
2. Select **"Human vs AI"** from the game mode dropdown
3. Press Play — should work identically to before (all network code is gated behind `GameConfig.CurrentGameMode == Multiplayer`)

## Step 7: Test Multiplayer

### Option A: Two Builds
1. Build the project (**File > Build and Run**)
2. Run one instance from the build, one from the Editor
3. Both select **"Multiplayer (Online)"** from main menu
4. One creates a room, the other joins
5. Host clicks Start

### Option B: ParrelSync (Recommended for Editor testing)
1. Install ParrelSync from https://github.com/VeriorPies/ParrelSync
2. **ParrelSync > Clones Manager > Create New Clone**
3. Open the clone project in a second Unity Editor
4. Play both editors simultaneously

---

## Architecture Reference

### Scene Flow
```
MainMenu → "Multiplayer" → Lobby → Room (create/join) → Game (synced) → GameOver → Lobby
MainMenu → "Human vs AI" → Game (offline, unchanged)
```

### Authority Model
- **MasterClient (host)** is authoritative: runs game loop, card pool, combat, phase transitions
- **Clients** send action requests via RPC → host validates → host broadcasts state
- All network code gated behind `GameConfig.CurrentGameMode == Multiplayer`

### Network Files

| File | Purpose |
|------|---------|
| `Network/PhotonConnector.cs` | Photon connection lifecycle singleton |
| `Network/RoomManager.cs` | Room create/join/leave, room list caching |
| `Network/NetworkGameBridge.cs` | Core RPC hub: 10 client→host + 7 host→all RPCs |
| `Network/NetworkGameSetup.cs` | Maps Photon actors → game slots on scene load |
| `Network/NetworkCardData.cs` | Serializable card data for JSON transmission |
| `Network/NetworkPlayerState.cs` | Serializable full player state for sync |
| `Network/CardLookup.cs` | Card template dictionary for reconstruction |
| `UI/LobbyUI.cs` | Lobby scene UI controller |

### Modified Files

| File | What Changed |
|------|-------------|
| `GameConfig.cs` | `PlayerName`, `SetMultiplayerHumanSlots()`, `IsHumanPlayer()` updated |
| `GameManager.cs` | `IsOnlineMode`/`IsHost` props, host/client branching, network broadcasts, `ApplyNetworkPlayerState()` |
| `Player.cs` | `ShopFrozen` setter made public |
| `TavernManager.cs` | `allCards` now instance field, `SetShopFromNetwork()`, offline-only `DontDestroyOnLoad` |
| `GameUIManager.cs` | Actions route through `NetworkGameBridge` in online mode, locked to local slot |
| `MainMenuManager.cs` | "Multiplayer (Online)" option loads Lobby scene |
| `GameOverUI.cs` | Shows Photon nicknames, Play Again → Lobby |
| `DiscoveryUI.cs` | Discovery choices routed via network |

### RPCs

**Client → Host (10):**
- `RPC_RequestBuyCard(shopIndex)`
- `RPC_RequestSellBoardCard(boardIndex)`
- `RPC_RequestSellHandCard(handIndex)`
- `RPC_RequestPlayCard(handIndex, boardPos)`
- `RPC_RequestUpgradeTavern()`
- `RPC_RequestRerollShop()`
- `RPC_RequestToggleFreeze()`
- `RPC_RequestEndTurn()`
- `RPC_RequestSwapBoardCards(indexA, indexB)`
- `RPC_RequestDiscoveryChoice(choiceIndex)`

**Host → All (7):**
- `RPC_SyncPlayerState(json)` — full player state
- `RPC_SyncShopForPlayer(json)` — shop cards for specific player
- `RPC_PhaseChanged(phase, turn, timer)`
- `RPC_TimerUpdate(remaining)` — ~1s intervals
- `RPC_CombatResult(json)`
- `RPC_CombatLog(json)`
- `RPC_GameOver(json)`

### Disconnection Handling
- **Player disconnects mid-game:** slot converts to AI, game continues
- **Host disconnects:** clients see "Host disconnected", return to Lobby
- **AI fills empty slots:** any room slot without a Photon player gets `AIController` on host

---

## Verification Checklist

- [ ] Install PUN 2, configure App ID
- [ ] Install Newtonsoft JSON package
- [ ] Create Lobby scene with LobbyUI wired up
- [ ] Add MainMenu, Lobby, Game to Build Settings
- [ ] Add NetworkGameBridge (PhotonView ID=1) to Game scene
- [ ] Add NetworkGameSetup to Game scene
- [ ] Test offline: Human vs AI works identically
- [ ] Test online: two instances connect, create/join room, start game
- [ ] Test recruit actions: buy, sell, play, reroll, upgrade, freeze sync correctly
- [ ] Test End Turn from both players → combat runs → health updates
- [ ] Test game over → standings shown on both → Play Again returns to lobby
- [ ] Test 3-player: 2 humans + 1 AI fills correctly
- [ ] Test disconnect: mid-game leave → slot becomes AI
