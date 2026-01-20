# Tarot Battlegrounds

> A tarot-themed auto-battler inspired by Hearthstone Battlegrounds

[![Phase 4](https://img.shields.io/badge/Phase-4%20UI%20System-blue)]()
[![Unity](https://img.shields.io/badge/Unity-2023%20LTS-black)]()
[![License](https://img.shields.io/badge/License-Private-red)]()

---

## 🎮 What is This?

**Tarot Battlegrounds** is a 4-8 player auto-battler where players:
- **Recruit** tarot cards from a mystical tavern
- **Build** synergistic boards with tribal mechanics (Pentacles, Cups, Swords, Wands)
- **Battle** opponents in automated combat
- **Survive** to be the last hero standing

**Current Status**: Phase 4 - UI System (Step 6 in progress)

---

## 📚 Documentation

**Full documentation available at:**  
**[TarotBattlegrounds-docs](https://github.com/squanchy667/TarotBattlegrounds-docs)**

### Quick Links
- 🏗️ [Architecture Overview](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/architecture.md)
- 🎯 [Current Roadmap (Phase 4)](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/product/roadmap/phase-4-ui.md)
- 🔧 [Setup Guide](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/setup-guide.md)
- 🐛 [Known Issues](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/resources/known-issues.md)
- 📝 [Changelog](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/resources/changelog.md)

---

## 🚀 Quick Start

### Prerequisites
- Unity 2023 LTS
- Git

### Installation
```bash
git clone https://github.com/squanchy667/TarotBattlegrounds-POC.git
cd TarotBattlegrounds-POC
```

### Run the Game
1. Open project in Unity Hub
2. Open `Scenes/MainMenu.unity`
3. Press Play ▶️
4. Click **PLAY** button
5. Use UI buttons to buy cards, play to board, battle!

---

## 🎯 Current Phase: UI System

### ✅ Completed (Steps 1-5)
- Main Menu scene with Play/Quit buttons
- Game UI framework (replaced OnGUI debug system)
- Shop UI - Browse and buy cards
- Hand UI - Manage purchased cards
- Board UI - Deploy cards for battle
- Player info panel (coins, tier, health)
- Action buttons (Buy, Sell, Play, Refresh, Upgrade, Switch Player)

### 🔄 In Progress (Step 6)
- **Player switching UI refresh** - All panels update when switching between Player 1 and Player 2

### 📋 Next Up
- Card asset system with artwork
- Full gameplay loop testing
- UI polish and animations

---

## 🏗️ Architecture

### Core Systems
- **GameManager** - Game loop, phase management
- **TavernManager** - Card pool, shop generation, tier system
- **Player** - Player state, coins, hand, board
- **CombatManager** - Auto-battle simulation

### Card System
- ScriptableObject-based cards
- 6 tier progression system
- 4 tribal synergies (Pentacles, Cups, Swords, Wands)
- Card effects: Guardian (taunt), Aegis (shield), Echo (deathrattle)

### Combat System
- Turn-based auto-battles
- Random first attacker
- Counterattack mechanics
- Health system (40 HP start)
- Damage calculation with caps

**For detailed architecture, see [Documentation](https://github.com/squanchy667/TarotBattlegrounds-docs)**

---

## 📊 Project Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Core Systems (GameManager, loop) |
| Phase 2 | ✅ Complete | Card System (ScriptableObjects, shop) |
| Phase 3 | ✅ Complete | Combat System (auto-battles) |
| **Phase 4** | 🔄 **In Progress** | **UI System (Steps 1-5 done, Step 6 pending)** |
| Phase 5 | ⏳ Planned | Polish & Balance |
| Phase 6 | ⏳ Planned | Multiplayer (Mirror networking) |

---

## 🎴 Game Features

### Recruit Phase
- **Buy Cards** (3 gold) - Add to your hand
- **Sell Cards** (1 gold) - Get gold refund
- **Reroll Shop** (1 gold) - Refresh available cards
- **Upgrade Tier** (5 gold, decreasing) - Unlock stronger cards
- **Play Cards** - Deploy from hand to board (max 7)

### Combat Phase
- Automated battles between player boards
- Turn-based attacks with counterattacks
- Card abilities trigger during combat
- Damage dealt to losing player's health
- Last hero standing wins!

### Tarot Theme
- **Suits as Tribes**: Pentacles (wealth), Cups (healing), Swords (aggression), Wands (energy)
- **Major Arcana as Heroes**: Special powers (Phase 5 feature)
- **Card Tiers**: 1-6 progressive power scaling
- **Mystical Shop**: The Tavern - your source of power

---

## 🛠️ Tech Stack

- **Engine**: Unity 2023 LTS (2D Core)
- **Language**: C# (.NET)
- **UI**: uGUI (Canvas) + TextMesh Pro
- **Networking** (Future): Mirror
- **Backend** (Future): Firebase (free tier)
- **Version Control**: Git + GitHub

---

## 📁 File Structure

```
Assets/
├── Cards/
│   └── Card.cs                    # ScriptableObject definition
├── Scenes/
│   ├── MainMenu.unity             # Title screen
│   └── Game.unity                 # Main gameplay
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs         # Game loop
│   │   ├── TavernManager.cs       # Shop system
│   │   ├── Player.cs              # Player state
│   │   └── CombatManager.cs       # Battle logic
│   └── UI/
│       ├── MainMenuManager.cs
│       ├── GameUIManager.cs
│       ├── ShopUI.cs
│       ├── HandUI.cs
│       └── BoardUI.cs
├── Prefabs/UI/
│   ├── ShopCard.prefab
│   ├── HandCard.prefab
│   └── BoardCard.prefab
└── Resources/Cards/              # Card assets
```

---

## 🐛 Known Issues

- Player switch doesn't refresh UI panels (Step 6 fix pending)
- Cards use placeholder colors, no artwork yet
- Combat phase has no visualization (works in background)
- 7 dark slots visible from old BoardPanel (cleanup needed)

**See full issue tracker:** [Known Issues](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/resources/known-issues.md)

---

## 🤝 Contributing

This is currently a private POC. For documentation updates or suggestions:
1. Check the [Documentation Repo](https://github.com/squanchy667/TarotBattlegrounds-docs)
2. Review [Coding Standards](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/coding-standards.md)
3. See [Current Roadmap](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/product/roadmap/)

---

## 📄 License

Private project - All rights reserved

---

## 👥 Contributors

- **squanchy667** - Project Lead & Developer
- **Theylon** - Contributor
- **AI Assistants**: Claude (Anthropic), Grok (xAI)

---

## 📞 Links

- **Documentation**: [TarotBattlegrounds-docs](https://github.com/squanchy667/TarotBattlegrounds-docs)
- **Main Branch**: `develop`
- **Unity Version**: 2023 LTS

---

## 🔄 Recent Updates

**January 2026** - Phase 4 UI System
- ✅ Completed Steps 1-5 (Main Menu → Shop/Hand/Board UI)
- 🔄 Step 6 in progress (Player switching refresh)

**July 2025** - Phase 3 Combat System
- ✅ Auto-battle simulation complete
- ✅ Card effects implemented
- ✅ Balance testing (50+ runs)

**July 2025** - Phase 2 Card System
- ✅ ScriptableObject architecture
- ✅ Shop mechanics (buy/sell/reroll)
- ✅ Tier system with unlocking

---

**Last Updated**: January 20, 2026

---

## 🎯 Vision

Build a compelling tarot-themed auto-battler that combines:
- Strategic deck building
- Mystical tarot aesthetics
- Fast-paced auto-combat
- Social multiplayer lobbies

**Final Goal**: 4-8 player online multiplayer with mobile support

---

> *"Divine your path to victory"* 🃏✨
