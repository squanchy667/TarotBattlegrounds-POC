using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central card database that generates the full 100+ card pool.
/// Cards distributed across tiers 1-6 with 6 tribes and Phase II abilities.
///
/// Supports runtime data loading: if RuntimeDataLoader has fetched cards.json,
/// GenerateAllCards() returns those cards instead of built-in definitions.
///
/// Design Philosophy:
/// - Tier 1-2: Simple stats, basic effects, entry-level tribe cards
/// - Tier 3-4: Complex abilities, synergy enablers, scaling cards
/// - Tier 5-6: Powerful finishers, multi-tribe cards, game-ending effects
///
/// Tribe Distribution (6 tribes, ~15-22 cards each):
/// - Pentacles (Economy): Gold bonuses, sell effects, armor
/// - Cups (Healing): Health buffs, Aegis, Guardian, Reborn
/// - Swords (Aggro): Damage, Venomous, Windfury, Cleave, StealBuff
/// - Wands (Buffs): Stat buffs, tokens, Deathrattle damage, Reborn
/// - Stars (Scaling): OnAllyDeath growth, tribe buffs, Reborn, Windfury (Phase III)
/// - Coins (Tokens/Economy): Token summons, sell bonuses, OnAllySummoned (Phase III)
/// </summary>
public static class CardDatabase
{
    /// <summary>Whether cards are sourced from runtime JSON data instead of built-in definitions.</summary>
    public static bool IsUsingRuntimeData { get; private set; }

    /// <summary>
    /// Generate the complete card pool.
    /// If RuntimeDataLoader has loaded cards, uses those; otherwise falls back to built-in definitions.
    /// </summary>
    public static List<Card> GenerateAllCards()
    {
        // Check for runtime-loaded data first
        if (RuntimeDataLoader.Instance != null && RuntimeDataLoader.Instance.IsLoaded && RuntimeDataLoader.Instance.Cards != null)
        {
            var runtimeCards = RuntimeDataLoader.Instance.BuildCards();
            if (runtimeCards != null && runtimeCards.Count > 0)
            {
                IsUsingRuntimeData = true;
                Debug.Log($"[CardDatabase] Using {runtimeCards.Count} runtime-loaded cards");
                return runtimeCards;
            }
        }

        IsUsingRuntimeData = false;
        List<Card> cards = new List<Card>();

        // === TIER 1: Basic Units (5 cards) - No abilities ===
        cards.Add(CreateCard("Coin Apprentice", 1, 1, 2,
            new[] { TribeType.Pentacles },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "A young merchant learning the trade."));

        cards.Add(CreateCard("Spring Sprite", 1, 1, 3,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "A tiny water spirit."));

        cards.Add(CreateCard("Dagger Initiate", 1, 2, 1,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "Quick but fragile."));

        cards.Add(CreateCard("Spark Wisp", 1, 2, 2,
            new[] { TribeType.Wands },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "A floating ember."));

        cards.Add(CreateCard("Tarot Seeker", 1, 1, 2,
            new[] { TribeType.Pentacles, TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "Seeks knowledge of the cards. (Dual tribe)"));

        // === TIER 2: Early Game Enablers (5 cards) ===
        cards.Add(CreateCard("Gold Collector", 2, 2, 3,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 1,
            "Battlecry: Gain 1 gold."));

        cards.Add(CreateCard("Healing Wave", 2, 1, 4,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainAegis, 1,
            "Battlecry: Gain Aegis."));

        cards.Add(CreateCard("Blade Squire", 2, 3, 2,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBonusDamage, 1,
            "OnAttack: Deal 1 extra damage."));

        cards.Add(CreateCard("Flame Enchanter", 2, 2, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentAttack, 1,
            "Battlecry: Give adjacent minions +1 Attack."));

        cards.Add(CreateCard("River Guard", 2, 2, 4,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian - Must be attacked first.",
            Card.EffectType.Guardian));

        // === TIER 3: Mid-Game Power (5 cards) ===
        cards.Add(CreateCard("Treasure Master", 3, 3, 4,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleBuffRandomFriendly, 2,
            "Deathrattle: Give a random friendly minion +2/+2."));

        cards.Add(CreateCard("Tidal Priest", 3, 2, 5,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainAegis, 1,
            "Battlecry: Gain Aegis."));

        cards.Add(CreateCard("Sword Captain", 3, 4, 3,
            new[] { TribeType.Swords },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllFriendlyAttack, 1,
            "Battlecry: Give all friendly minions +1 Attack."));

        cards.Add(CreateCard("Inferno Mage", 3, 3, 4,
            new[] { TribeType.Wands },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleDamageAllEnemies, 2,
            "Deathrattle: Deal 2 damage to all enemies."));

        cards.Add(CreateCard("Mercenary", 3, 4, 4,
            new[] { TribeType.Swords, TribeType.Pentacles },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBuffSelf, 1,
            "OnAttack: Gain +1 Attack. (Dual tribe)"));

        // === TIER 4: Late-Game Synergies (5 cards) ===
        cards.Add(CreateCard("Wealthy Baron", 4, 3, 5,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 2,
            "Battlecry: Gain 2 gold."));

        cards.Add(CreateCard("Ocean Guardian", 4, 3, 7,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian - Must be attacked first.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Blade Master", 4, 6, 4,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackCleave, 2,
            "OnAttack: Deal 2 damage to adjacent enemies."));

        cards.Add(CreateCard("Phoenix Caller", 4, 4, 5,
            new[] { TribeType.Wands },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleBuffRandomFriendly, 3,
            "Deathrattle: Give a random friendly minion +3/+3."));

        cards.Add(CreateCard("Ember Healer", 4, 3, 6,
            new[] { TribeType.Cups, TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentStats, 1,
            "Battlecry: Give adjacent minions +1/+1. (Dual tribe)"));

        // === TIER 5: Power Spikes (5 cards) ===
        cards.Add(CreateCard("Dragon Hoarder", 5, 5, 6,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 1,
            "Battlecry: Give all Pentacles +1/+1."));

        cards.Add(CreateCard("Tsunami Lord", 5, 4, 8,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainAegis, 1,
            "Battlecry: Gain Aegis. Guardian.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Blade Storm", 5, 7, 5,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBonusDamage, 2,
            "OnAttack: Deal 2 extra damage."));

        cards.Add(CreateCard("Archmage", 5, 5, 6,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllFriendlyAttack, 2,
            "Battlecry: Give all friendly minions +2 Attack."));

        cards.Add(CreateCard("Battle Mage", 5, 6, 6,
            new[] { TribeType.Wands, TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBuffSelf, 2,
            "OnAttack: Gain +2 Attack permanently. (Dual tribe)"));

        // === TIER 6: Finishers (5 cards) ===
        cards.Add(CreateCard("Golden Emperor", 6, 6, 8,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 2,
            "Battlecry: Give all Pentacles +2/+2."));

        cards.Add(CreateCard("Leviathan", 6, 5, 12,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian - Massive health pool.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Doom Blade", 6, 10, 6,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackCleave, 3,
            "OnAttack: Deal 3 damage to adjacent enemies."));

        cards.Add(CreateCard("Inferno Dragon", 6, 7, 7,
            new[] { TribeType.Wands },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleDamageAllEnemies, 4,
            "Deathrattle: Deal 4 damage to all enemies."));

        cards.Add(CreateCard("Arcane Trinity", 6, 6, 8,
            new[] { TribeType.Cups, TribeType.Wands, TribeType.Swords },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentStats, 2,
            "Battlecry: Give adjacent minions +2/+2. (Triple tribe)"));

        // === ORIGINAL CARDS (5 user-created cards) ===
        cards.Add(CreateCard("Spark of Inspiration", 1, 1, 2,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentAttack, 1,
            "Battlecry: Give adjacent minions +1 Attack."));

        cards.Add(CreateCard("Intuitive Novice", 1, 2, 2,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "Predicts and reduces incoming damage.",
            Card.EffectType.Aegis));

        cards.Add(CreateCard("Impulsive Apprentice", 1, 2, 1,
            new[] { TribeType.Wands },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "Quick attack starter."));

        cards.Add(CreateCard("Flame Dancer", 2, 3, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllFriendlyAttack, 1,
            "Battlecry: Give all friendly minions +1 Attack."));

        cards.Add(CreateCard("Blazing Knight", 3, 4, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBuffSelf, 2,
            "OnAttack: Gain +2 Attack."));

        // === PHASE III: STARS TRIBE (T203) - 10 cards, Tier 1-3 ===
        // Stars theme: Scaling — grow stronger each combat round
        cards.Add(CreateCard("Starlight Acolyte", 1, 1, 2,
            new[] { TribeType.Stars },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "A student of the celestial arts."));

        cards.Add(CreateCard("Shooting Star", 1, 2, 1,
            new[] { TribeType.Stars },
            AbilityTrigger.None, Card.AbilityEffectType.Reborn, 0,
            "Reborn. Returns once after falling."));

        cards.Add(CreateCard("Constellation Weaver", 2, 2, 3,
            new[] { TribeType.Stars },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffSelf, 1,
            "OnAllyDeath: Gain +1/+1."));

        cards.Add(CreateCard("Nova Burst", 2, 3, 2,
            new[] { TribeType.Stars },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleDamageRandomEnemy, 3,
            "Deathrattle: Deal 3 damage to a random enemy."));

        cards.Add(CreateCard("Celestial Guardian", 2, 2, 4,
            new[] { TribeType.Stars },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian — blocks for the heavens.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Star Forger", 3, 3, 4,
            new[] { TribeType.Stars },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 1,
            "Battlecry: Give all Stars +1/+1."));

        cards.Add(CreateCard("Astral Warden", 4, 4, 5,
            new[] { TribeType.Stars },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffTribematesAttack, 1,
            "Aura: Stars have +1 Attack."));

        cards.Add(CreateCard("Supernova Mage", 3, 3, 3,
            new[] { TribeType.Stars },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleDamageAllEnemies, 2,
            "Deathrattle: Deal 2 damage to all enemies."));

        cards.Add(CreateCard("Comet Rider", 3, 5, 3,
            new[] { TribeType.Stars },
            AbilityTrigger.None, Card.AbilityEffectType.Windfury, 0,
            "Windfury. Attacks twice."));

        cards.Add(CreateCard("Polaris, the North Star", 3, 2, 5,
            new[] { TribeType.Stars, TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainAegis, 1,
            "Battlecry: Gain Aegis. (Dual tribe)"));

        // === PHASE III: STARS HIGH-TIER (T214) - 5 cards, Tier 4-6 ===
        cards.Add(CreateCard("Stellar Guardian", 4, 4, 6,
            new[] { TribeType.Stars },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian. A celestial protector.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Nebula Titan", 4, 5, 6,
            new[] { TribeType.Stars },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffSelf, 2,
            "OnAllyDeath: Gain +2/+2. Grows with sacrifice."));

        cards.Add(CreateCard("Galaxy Weaver", 5, 4, 7,
            new[] { TribeType.Stars },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.BuffAllTribeOnDeath, 2,
            "Deathrattle: Give all Stars +2/+2."));

        cards.Add(CreateCard("Cosmic Shaper", 5, 5, 5,
            new[] { TribeType.Stars },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 2,
            "Battlecry: Give all Stars +2/+2."));

        cards.Add(CreateCard("Cosmic Arbiter", 6, 7, 8,
            new[] { TribeType.Stars, TribeType.Swords },
            AbilityTrigger.StartOfCombat, Card.AbilityEffectType.BuffAllFriendlyAttack, 2,
            "StartOfCombat: All allies gain +2 Attack. (Dual tribe)"));

        // === PHASE III: COINS TRIBE (T204) - 10 cards, Tier 1-3 ===
        // Coins theme: Economy/Tokens — summon tokens, gain gold, sell bonuses
        cards.Add(CreateCard("Lucky Penny", 1, 1, 2,
            new[] { TribeType.Coins },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellGainCoins, 1,
            "OnSell: Gain 1 extra coin."));

        cards.Add(CreateCard("Copper Golem", 1, 2, 2,
            new[] { TribeType.Coins },
            AbilityTrigger.None, Card.AbilityEffectType.None, 0,
            "A small construct of copper coins."));

        cards.Add(CreateCard("Mint Maker", 2, 1, 3,
            new[] { TribeType.Coins },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.SummonTokenOnPlay, 1,
            "Battlecry: Summon a 1/1 Coin Token."));

        cards.Add(CreateCard("Gold Digger", 2, 3, 2,
            new[] { TribeType.Coins },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 1,
            "Battlecry: Gain 1 coin."));

        cards.Add(CreateCard("Toll Collector", 2, 2, 3,
            new[] { TribeType.Coins },
            AbilityTrigger.OnAllySummoned, Card.AbilityEffectType.OnAllySummonedBuffSelf, 1,
            "OnAllySummoned: Gain +1 Attack."));

        cards.Add(CreateCard("Silver Sentinel", 3, 3, 5,
            new[] { TribeType.Coins },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.SummonTokenOnDeath, 2,
            "Deathrattle: Summon a 2/2 Coin Golem."));

        cards.Add(CreateCard("Fortune Teller", 3, 2, 4,
            new[] { TribeType.Coins },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellBuffAllRemaining, 1,
            "OnSell: Give all remaining allies +1/+1."));

        cards.Add(CreateCard("Jackpot Jester", 3, 4, 3,
            new[] { TribeType.Coins },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 2,
            "Battlecry: Gain 2 coins."));

        cards.Add(CreateCard("Banker Baron", 3, 3, 4,
            new[] { TribeType.Coins },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffTribematesAttack, 1,
            "Aura: Coins have +1 Attack."));

        cards.Add(CreateCard("Golden Opportunist", 3, 4, 4,
            new[] { TribeType.Coins, TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 1,
            "Battlecry: Gain 1 coin. (Dual tribe)"));

        // === PHASE III: COINS HIGH-TIER (T214) - 5 cards, Tier 4-6 ===
        cards.Add(CreateCard("Coin Mint", 4, 3, 5,
            new[] { TribeType.Coins },
            AbilityTrigger.OnAllySummoned, Card.AbilityEffectType.OnAllySummonedBuffSelf, 1,
            "OnAllySummoned: Gain +1/+1. Token synergy engine."));

        cards.Add(CreateCard("Treasure Wyrm", 4, 4, 6,
            new[] { TribeType.Coins },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.SummonTokenOnDeath, 3,
            "Deathrattle: Summon a 3/3 Treasure Hoard."));

        cards.Add(CreateCard("Platinum Dragon", 5, 5, 6,
            new[] { TribeType.Coins },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.SummonTokenOnDeath, 2,
            "Deathrattle: Summon two 2/2 Coin Drakes."));

        cards.Add(CreateCard("Minting Press", 5, 3, 7,
            new[] { TribeType.Coins },
            AbilityTrigger.OnAllySummoned, Card.AbilityEffectType.OnAllySummonedBuffSelf, 2,
            "OnAllySummoned: Gain +2/+2. Prints value."));

        cards.Add(CreateCard("Grand Vault", 6, 6, 9,
            new[] { TribeType.Coins },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellBuffAllRemaining, 2,
            "OnSell: Give all remaining allies +2/+2."));

        // === PHASE III: SWORDS EXPANSION (T205) - 15 cards, Tier 1-5 ===
        cards.Add(CreateCard("Razor Scout", 3, 3, 2,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.Venomous, 0,
            "Venomous. Small but deadly."));

        cards.Add(CreateCard("Parry Trainee", 1, 1, 3,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.GainArmor, 1,
            "Armor 1. Trained to deflect."));

        cards.Add(CreateCard("Duelist", 2, 3, 2,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.StealBuffOnAttack, 1,
            "OnAttack: Steal +1/+1 from target."));

        cards.Add(CreateCard("War Drummer", 2, 2, 3,
            new[] { TribeType.Swords },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 1,
            "Battlecry: Give all Swords +1/+1."));

        cards.Add(CreateCard("Charging Knight", 2, 2, 2,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.Windfury, 0,
            "Windfury. Charges twice."));

        cards.Add(CreateCard("Blade Dancer", 3, 4, 3,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackBonusDamage, 2,
            "OnAttack: Deal 2 extra damage."));

        cards.Add(CreateCard("Venomous Assassin", 4, 3, 4,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.Venomous, 0,
            "Venomous. Stealth strikes."));

        cards.Add(CreateCard("Steel Commander", 3, 3, 5,
            new[] { TribeType.Swords },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffTribematesAttack, 1,
            "Aura: Swords have +1 Attack."));

        cards.Add(CreateCard("Blade Revenant", 4, 5, 4,
            new[] { TribeType.Swords },
            AbilityTrigger.None, Card.AbilityEffectType.Reborn, 0,
            "Reborn. Rises to fight again."));

        cards.Add(CreateCard("Warmonger", 4, 6, 5,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffSelf, 2,
            "OnAllyDeath: Gain +2/+2."));

        cards.Add(CreateCard("Sword Saint", 4, 5, 5,
            new[] { TribeType.Swords },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.BuffAllTribeOnDeath, 2,
            "Deathrattle: Give all Swords +2/+2."));

        cards.Add(CreateCard("Armored Berserker", 4, 7, 3,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.OnAttackCleave, 2,
            "OnAttack: Cleave for 2 damage."));

        cards.Add(CreateCard("Chaos Swordsman", 5, 6, 6,
            new[] { TribeType.Swords },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.RandomTransformOnDeath, 0,
            "Deathrattle: Transform into a random card."));

        cards.Add(CreateCard("Blade Sovereign", 5, 6, 5,
            new[] { TribeType.Swords },
            AbilityTrigger.OnAttack, Card.AbilityEffectType.StealBuffOnAttack, 2,
            "OnAttack: Steal +2/+2 from target."));

        cards.Add(CreateCard("Warlord Supreme", 5, 7, 7,
            new[] { TribeType.Swords },
            AbilityTrigger.StartOfCombat, Card.AbilityEffectType.BuffAllFriendlyAttack, 1,
            "StartOfCombat: All friendlies gain +1 Attack."));

        // === PHASE III: CUPS EXPANSION (T206) - 15 cards, Tier 1-5 ===
        cards.Add(CreateCard("Dewdrop Fairy", 1, 1, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffSelfHealth, 1,
            "Battlecry: Gain +1 Health."));

        cards.Add(CreateCard("Puddle Sprite", 1, 1, 2,
            new[] { TribeType.Cups },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleBuffRandomFriendly, 1,
            "Deathrattle: Give a friendly +1/+1."));

        cards.Add(CreateCard("Wellspring Dancer", 2, 2, 4,
            new[] { TribeType.Cups },
            AbilityTrigger.OnAllySummoned, Card.AbilityEffectType.OnAllySummonedBuffSummoned, 1,
            "OnAllySummoned: Give summoned +1/+1."));

        cards.Add(CreateCard("Tidal Shaman", 2, 2, 3,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentHealth, 2,
            "Battlecry: Give adjacent minions +2 Health."));

        cards.Add(CreateCard("Aegis Bearer", 2, 1, 4,
            new[] { TribeType.Cups, TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainAegis, 1,
            "Battlecry: Gain Aegis. (Dual tribe)"));

        cards.Add(CreateCard("Coral Bulwark", 3, 2, 6,
            new[] { TribeType.Cups, TribeType.Pentacles },
            AbilityTrigger.None, Card.AbilityEffectType.GainArmor, 2,
            "Armor 2. A living reef wall. (Dual tribe)",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Healing Totem", 3, 2, 5,
            new[] { TribeType.Cups },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffAdjacentStats, 1,
            "Aura: Adjacent allies get +1/+1."));

        cards.Add(CreateCard("Moonwell Priestess", 3, 3, 5,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 1,
            "Battlecry: Give all Cups +1/+1."));

        cards.Add(CreateCard("Deep Sea Revenant", 4, 3, 6,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.Reborn, 0,
            "Reborn. The depths reclaim their own."));

        cards.Add(CreateCard("Hydra Matriarch", 4, 4, 7,
            new[] { TribeType.Cups },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.SummonTokenOnDeath, 3,
            "Deathrattle: Summon a 3/3 Hydra Head."));

        cards.Add(CreateCard("Cascade Champion", 4, 4, 6,
            new[] { TribeType.Cups },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffRandom, 2,
            "OnAllyDeath: Give a random ally +2/+2."));

        cards.Add(CreateCard("Torrent Elemental", 4, 5, 5,
            new[] { TribeType.Cups },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.BuffAllTribeOnDeath, 1,
            "Deathrattle: Give all Cups +1/+1."));

        cards.Add(CreateCard("Abyssal Leviathan", 5, 4, 10,
            new[] { TribeType.Cups },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian. An ancient ocean titan.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Ocean Oracle", 5, 4, 7,
            new[] { TribeType.Cups },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAdjacentStats, 2,
            "Battlecry: Give adjacent minions +2/+2."));

        cards.Add(CreateCard("Maelstrom Caller", 5, 5, 8,
            new[] { TribeType.Cups },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffSelf, 2,
            "OnAllyDeath: Gain +2/+2. A storm grows."));

        // === PHASE III: PENTACLES EXPANSION (T207) - 10 cards, Tier 1-4 ===
        cards.Add(CreateCard("Scrap Collector", 1, 1, 2,
            new[] { TribeType.Pentacles },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellGainCoins, 1,
            "OnSell: Gain 1 extra coin."));

        cards.Add(CreateCard("Gem Hoarder", 2, 2, 3,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleBuffRandomFriendly, 1,
            "Deathrattle: Give a friendly +1/+1."));

        cards.Add(CreateCard("Vault Guardian", 2, 2, 4,
            new[] { TribeType.Pentacles },
            AbilityTrigger.None, Card.AbilityEffectType.GainArmor, 1,
            "Armor 1. Protects the treasury."));

        cards.Add(CreateCard("Market Hustler", 2, 3, 2,
            new[] { TribeType.Pentacles },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellBuffAllRemaining, 1,
            "OnSell: Give all remaining allies +1/+1."));

        cards.Add(CreateCard("Investment Banker", 3, 3, 4,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffTribematesAttack, 1,
            "Aura: Pentacles have +1 Attack."));

        cards.Add(CreateCard("Midas Apprentice", 3, 4, 3,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 2,
            "Battlecry: Gain 2 coins."));

        cards.Add(CreateCard("Diamond Golem", 3, 4, 5,
            new[] { TribeType.Pentacles },
            AbilityTrigger.None, Card.AbilityEffectType.Taunt, 0,
            "Guardian. Hard as diamond.",
            Card.EffectType.Guardian));

        cards.Add(CreateCard("Treasury Titan", 4, 4, 6,
            new[] { TribeType.Pentacles },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.BuffAllTribeOnPlay, 1,
            "Battlecry: Give all Pentacles +1/+1."));

        cards.Add(CreateCard("Grand Appraiser", 4, 3, 5,
            new[] { TribeType.Pentacles },
            AbilityTrigger.OnSell, Card.AbilityEffectType.OnSellGainCoins, 2,
            "OnSell: Gain 2 extra coins."));

        cards.Add(CreateCard("Kingpin", 4, 5, 6,
            new[] { TribeType.Pentacles },
            AbilityTrigger.OnAllyDeath, Card.AbilityEffectType.OnAllyDeathBuffSelf, 1,
            "OnAllyDeath: Gain +1/+1. Profits from loss."));

        // === PHASE III: WANDS EXPANSION (T208) - 5 cards, Tier 2-5 ===
        cards.Add(CreateCard("Ember Weaver", 2, 2, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.Battlecry, Card.AbilityEffectType.SummonTokenOnPlay, 1,
            "Battlecry: Summon a 1/1 Ember."));

        cards.Add(CreateCard("Pyromancer Adept", 3, 3, 3,
            new[] { TribeType.Wands },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.DeathrattleDamageRandomEnemy, 3,
            "Deathrattle: Deal 3 damage to a random enemy."));

        cards.Add(CreateCard("Wildfire Shaman", 4, 4, 5,
            new[] { TribeType.Wands },
            AbilityTrigger.Aura, Card.AbilityEffectType.AuraBuffAllFriendlyAttack, 1,
            "Aura: All other friendlies have +1 Attack."));

        cards.Add(CreateCard("Magma Lord", 5, 6, 7,
            new[] { TribeType.Wands },
            AbilityTrigger.Deathrattle, Card.AbilityEffectType.BuffAllTribeOnDeath, 2,
            "Deathrattle: Give all Wands +2/+2."));

        cards.Add(CreateCard("Elemental Phoenix", 5, 5, 5,
            new[] { TribeType.Wands },
            AbilityTrigger.None, Card.AbilityEffectType.Reborn, 0,
            "Reborn. Rises from the ashes."));

        Debug.Log($"[CardDatabase] Generated {cards.Count} built-in cards across 6 tiers (6 tribes)");
        return cards;
    }

    /// <summary>
    /// Create a card with full configuration.
    /// </summary>
    private static Card CreateCard(
        string name, int tier, int attack, int health,
        TribeType[] tribes,
        AbilityTrigger abilityTrigger, Card.AbilityEffectType abilityEffect, int abilityValue,
        string description,
        Card.EffectType effectType = Card.EffectType.NoEffect,
        string effectParameter = "")
    {
        Card card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tier = tier;
        card.attack = attack;
        card.health = health;
        card.tribes = tribes;
        card.ability = description;

        // New ability system
        card.abilityTrigger = abilityTrigger;
        card.abilityEffect = abilityEffect;
        card.abilityValue = abilityValue;

        // Legacy effect system
        card.effectType = effectType;
        card.effectParameter = effectParameter;
        // T766 / TA-10: keyword effect types must set their runtime flags on the template too
        if (effectType == Card.EffectType.Aegis)
            card.hasAegis = true;

        // Set legacy tribe field for backwards compatibility
        if (tribes != null && tribes.Length > 0 && tribes[0] != TribeType.None)
        {
            card.tribe = tribes[0].ToString();
        }

        card.cardImage = LoadCardArt(name);

        return card;
    }

    // Card art lives at Assets/Resources/CardArt/<cardName>.png — the filename must match
    // cardName exactly. Cards with no art file get a null sprite, which CardDisplayUI already
    // handles by drawing a StoneEdge placeholder, so a missing image is never an error.
    //
    // Public because RuntimeDataLoader.BuildCards() must use the same lookup: when runtime
    // data loads, it REPLACES this pool entirely, and cards built there would otherwise have
    // no art at all.
    private static readonly Dictionary<string, Sprite> _artCache = new Dictionary<string, Sprite>();

    public static Sprite LoadCardArt(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return null;

        if (_artCache.TryGetValue(cardName, out Sprite cached)) return cached;

        Sprite sprite = Resources.Load<Sprite>($"CardArt/{cardName}");
        _artCache[cardName] = sprite;
        return sprite;
    }

    /// <summary>
    /// Get cards filtered by tier.
    /// </summary>
    public static List<Card> GetCardsByTier(int tier)
    {
        List<Card> all = GenerateAllCards();
        List<Card> filtered = new List<Card>();

        foreach (var card in all)
        {
            if (card.tier == tier)
                filtered.Add(card);
        }

        return filtered;
    }

    /// <summary>
    /// Get cards filtered by tribe.
    /// </summary>
    public static List<Card> GetCardsByTribe(TribeType tribe)
    {
        List<Card> all = GenerateAllCards();
        List<Card> filtered = new List<Card>();

        foreach (var card in all)
        {
            if (card.HasTribe(tribe))
                filtered.Add(card);
        }

        return filtered;
    }

    /// <summary>
    /// Print a summary of the card pool for debugging.
    /// </summary>
    public static void PrintCardPoolSummary()
    {
        var cards = GenerateAllCards();

        Debug.Log("=== CARD POOL SUMMARY ===");

        // Count by tier
        for (int tier = 1; tier <= 6; tier++)
        {
            int count = 0;
            foreach (var c in cards) if (c.tier == tier) count++;
            Debug.Log($"Tier {tier}: {count} cards");
        }

        // Count by tribe
        foreach (TribeType tribe in System.Enum.GetValues(typeof(TribeType)))
        {
            if (tribe == TribeType.None) continue;
            int count = 0;
            foreach (var c in cards) if (c.HasTribe(tribe)) count++;
            Debug.Log($"{tribe}: {count} cards");
        }

        // Count abilities
        int withAbility = 0;
        int withBattlecry = 0;
        int withDeathrattle = 0;
        int withOnAttack = 0;
        int multiTribe = 0;

        foreach (var card in cards)
        {
            if (card.abilityTrigger != AbilityTrigger.None) withAbility++;
            if (card.abilityTrigger == AbilityTrigger.Battlecry) withBattlecry++;
            if (card.abilityTrigger == AbilityTrigger.Deathrattle) withDeathrattle++;
            if (card.abilityTrigger == AbilityTrigger.OnAttack) withOnAttack++;
            if (card.tribes != null && card.tribes.Length > 1) multiTribe++;
        }

        Debug.Log($"Cards with abilities: {withAbility}");
        Debug.Log($"  Battlecry: {withBattlecry}");
        Debug.Log($"  Deathrattle: {withDeathrattle}");
        Debug.Log($"  OnAttack: {withOnAttack}");
        Debug.Log($"Multi-tribe cards: {multiTribe}");
        Debug.Log("=========================");
    }
}
