# Tarot Battlegrounds

> A tarot-themed auto-battler inspired by Hearthstone Battlegrounds

[![Unity](https://img.shields.io/badge/Unity-2022.3.48f1-black)]()
[![Networking](https://img.shields.io/badge/Networking-Photon%20PUN%202-blue)]()
[![License](https://img.shields.io/badge/License-Private-red)]()

---

> ## 📖 Documentation lives in [TarotBattlegrounds-docs](https://github.com/squanchy667/TarotBattlegrounds-docs)
>
> This README is a **quickstart only**. For architecture, roadmap, known issues, changelog,
> and everything else, see the docs repo — it is the canonical source of truth for this project.

---

## 🎮 What is This?

**Tarot Battlegrounds** is a **2-8 player** real-time auto-battler where players:
- **Recruit** tarot cards from a mystical tavern
- **Build** synergistic boards across 6 tribal factions
- **Battle** opponents in automated combat
- **Survive** to be the last hero standing

**Current Status**: Production audit complete (2026-02-24) — full 7-phase feature set shipped
(online infra, hero powers, 110-card pool, combat VFX, UI overhaul, ranked/MMR, 8-player scale).
UX overhaul + audit remediation in progress as of 2026-07.

---

## 📚 Documentation

**Full documentation available at:**
**[TarotBattlegrounds-docs](https://github.com/squanchy667/TarotBattlegrounds-docs)**

### Quick Links
- 🏗️ [Architecture Overview](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/architecture.md)
- 🎯 [Roadmap](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/product/roadmap/)
- 🔧 [Setup Guide](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/setup-guide.md)
- 🐛 [Known Issues](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/resources/known-issues.md)
- 📝 [Changelog](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/resources/changelog.md)

---

## 🚀 Quick Start

### Prerequisites
- Unity **2022.3.48f1**
- Git

### Installation
```bash
git clone https://github.com/squanchy667/TarotBattlegrounds-POC.git
cd TarotBattlegrounds-POC
```

### Run the Game
1. Open project in Unity Hub (`TarotBattlegrounds-POC/` is the Unity project directory)
2. Open `Assets/Scenes/MainMenu.unity`
3. Press Play ▶️
4. From the main menu, host or join a lobby (`Lobby.unity`) or start a local game (`Game.unity`)

---

## 🏗️ Tech Stack

- **Engine**: Unity 2022.3.48f1 (2D Core)
- **Language**: C# (.NET)
- **UI**: uGUI (Canvas) + TextMesh Pro
- **Networking**: Photon PUN 2, host-authoritative
- **Hosting**: AWS S3 + CloudFront (WebGL build) — live at [dui22oafwco41.cloudfront.net](https://dui22oafwco41.cloudfront.net)
- **Backend**: DevZone serverless (separate `tarot-devzone` repo) — game auth, matchmaking, ranked/MMR/seasons
- **Version Control**: Git + GitHub

> Note: Mirror networking and Firebase were early candidates but were **never used in shipped
> code**. The unused Mirror package has since been removed from this repo.

---

## 🎴 Game Content

- **110 cards** across **6 tribes**: Pentacles, Cups, Swords, Wands, Stars, Coins
- **12 hero powers**
- **16 ability types**
- **415 EditMode tests**

---

## 📁 File Structure

```
TarotBattlegrounds-POC/          # Unity project directory
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.unity       # Title screen / entry point
│   │   ├── Lobby.unity          # Photon lobby (host/join)
│   │   └── Game.unity           # Main gameplay
│   ├── Scripts/                 # Core, Cards, Combat, UI, Networking, Auth, etc.
│   └── Editor/                  # Scene/asset setup tooling
AWS/
├── PLAN.md                      # AWS infrastructure plan
└── scripts/                     # deploy-webgl.sh, redeploy.sh, validate-deployment.sh
tools/                            # Log analysis utilities
```

---

## 🤝 Contributing

This is currently a private project. For documentation updates or suggestions:
1. Check the [Documentation Repo](https://github.com/squanchy667/TarotBattlegrounds-docs)
2. Review [Coding Standards](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/developer/coding-standards.md)
3. See the [Roadmap](https://github.com/squanchy667/TarotBattlegrounds-docs/blob/main/product/roadmap/)

---

## 📄 License

Private project - All rights reserved

---

## 👥 Contributors

- **squanchy667** - Project Lead & Developer
- **Theylon** - Contributor
- **AI Assistants**: Claude (Anthropic), Grok (xAI)

---

> *"Divine your path to victory"* 🃏✨
