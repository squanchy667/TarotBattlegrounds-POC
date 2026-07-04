> **Note:** Written 2026-01-17. Describes the original 8-player asynchronous concept.
> This was superseded by the shipped 2-8 player real-time Photon game.
> See the [docs repo](https://github.com/squanchy667/TarotBattlegrounds-docs) for current product docs.

# Tarot Battlegrounds

## Product Requirements Document (PRD)

---

## 1. Product Overview

**Product Name:** Tarot Battlegrounds  
**Genre:** Auto-battler / Strategy  
**Players:** 8-player asynchronous multiplayer  
**Platform:** TBD (PC / Mobile-first recommended)  

Tarot Battlegrounds is an auto-battler inspired by probability, sequencing, and symbolic tarot archetypes. Players ("Seers") build boards of Arcana Cards through a shared pool and engage in fully automated combat rounds until a single winner remains.

The product prioritizes:
- Strategic depth over mechanical dexterity
- High replayability
- Legible randomness
- Short-to-medium session length

---

## 2. Goals & Success Metrics

### 2.1. Product Goals
- Deliver a deep, skill-expressive auto-battler with minimal player friction
- Create a thematically coherent tarot-based reskin without altering proven mechanics
- Enable long-term balance, content expansion, and live-ops support

### 2.2. Success Metrics
- Average session length: 20–35 minutes
- Day-7 retention competitive with genre benchmarks
- Clear player understanding of combat outcomes (low confusion rate)
- High variance of viable endgame compositions

---

## 3. Non-Goals

- No real-time PvP inputs during combat
- No deck-building or pre-game loadouts
- No manual targeting or mid-combat decisions
- No hidden information beyond standard shop randomness

---

## 4. Core Game Loop

Each match proceeds in repeating rounds consisting of two phases:

1. **Divination Phase (Player-Controlled)**  
2. **Battle Phase (Fully Automatic)**

This loop repeats until one Seer remains.

---

## 5. Player Entity: Seer

Each player controls a Seer defined by the following attributes:

| Attribute | Description |
|---------|-------------|
| Fate (Health) | Reduced when losing combat; elimination at 0 |
| Mana | Spendable resource during Divination Phase |
| Arcana Circle Tier | Progression level (Tier I–VI) |
| Hero Power | Unique passive or active ability |
| Board | Up to 7 Arcana Cards |

---

## 6. Economy & Progression

### 6.1. Mana System
- Start at 3 mana
- Gain +1 mana per round
- Maximum 10 mana
- Unspent mana does not carry over

### 6.2. Arcana Circle (Shop)

Each Seer has a personal shop called the **Arcana Circle**.

| Action | Mana Cost |
|------|-----------|
| Channel Arcana Card | 3 |
| Reveal Spread (Refresh) | 1 |
| Bind Spread (Freeze) | 0 |
| Release Card (Sell) | +1 |
| Ascend Circle (Tier Up) | Variable |

Arcana Circle Tier determines the maximum Tier of cards that may appear.

---

## 7. Arcana Cards

### 7.1. Card Properties

Each Arcana Card contains:

- Name / Unique ID
- Circle Tier (I–VI)
- Suit / Archetype
- Power (Attack)
- Essence (Health)
- Keywords
- Triggered Effects

### 7.2. Suits

| Suit | Strategic Identity |
|------|-------------------|
| Major Arcana | Unique, high-impact effects |
| Swords | Aggression, burst, first-strike |
| Cups | Healing, regeneration, recursion |
| Wands | Buffs, scaling, spell-like effects |
| Pentacles | Economy and long-term scaling |

---

## 8. Keywords (Canonical)

| Keyword | Effect |
|--------|--------|
| Ward | Must be targeted first |
| Blessed | Negates first damage instance |
| Foresight | Attacks multiple times |
| Rebirth | Revives once at 1 Essence |
| Spellbound | Any damage destroys target |
| Omen | Triggers on death |

All keyword behavior is deterministic and globally consistent.

---

## 9. Fusion System (Triples)

- Three identical Arcana Cards automatically fuse
- Fusion produces a **Unified Arcana**:
  - Double Power and Essence
  - Upgraded triggered effects
- Fusion grants a **Vision Reward**:
  - Discover one Arcana Card from current Circle Tier or higher

Fusion is mandatory and resolves immediately.

---

## 10. Battle Phase

### 10.1. Setup
- Board state is locked at end of Divination Phase
- Card order (left → right) defines attack order

### 10.2. Opponent Assignment
- Players are paired randomly among living Seers
- If odd count, one player fights an **Echo** (ghost board)
- Echo matches deal no Fate damage

### 10.3. Combat Resolution

- Side with more cards attacks first (random if tied)
- Combat alternates between sides
- Left-most living card attacks
- Targeting prioritizes Ward; otherwise random

Damage is dealt simultaneously. All triggers fully resolve before the next attack.

### 10.4. Combat End

Combat ends when:
- One board has no living cards, or
- Both boards are destroyed simultaneously (tie)

---

## 11. Fate Damage

If a Seer wins combat:

```
Fate Damage = Sum of surviving cards’ Circle Tiers
            + Winner’s Arcana Circle Tier
```

- Ties deal 0 damage
- Echo matches deal 0 damage

---

## 12. Elimination & Endgame

- Seers reduced to 0 Fate are eliminated
- Their cards return to the shared pool
- Last remaining Seer wins the match

---

## 13. Design Principles

- High strategic depth from simple actions
- Variance that is explainable and observable
- No mechanical skill advantage
- Strong late-game decisiveness

---

## 14. Future Extensions (Out of Scope)

- Additional suits or keywords
- Ranked ladder / MMR
- Cosmetic progression
- PvE or solo modes
- Seasonal rotations

---

**Status:** Draft v1.0  
**Owner:** Product / Game Design  
**Last Updated:** TBD

