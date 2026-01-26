using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central card database that generates the full 30-card pool.
/// 5 cards per tier (1-6), with distributed abilities and tribes.
///
/// Design Philosophy:
/// - Tier 1-2: Simple stats, basic effects
/// - Tier 3-4: More complex abilities, synergy enablers
/// - Tier 5-6: Powerful finishers, multi-tribe cards
///
/// Tribe Distribution (roughly equal):
/// - Pentacles (Economy): 7-8 cards
/// - Cups (Healing): 7-8 cards
/// - Swords (Aggro): 7-8 cards
/// - Wands (Buffs): 7-8 cards
/// </summary>
public static class CardDatabase
{
    /// <summary>
    /// Generate the complete 30-card pool.
    /// </summary>
    public static List<Card> GenerateAllCards()
    {
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
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 3,
            "Battlecry: Gain 3 gold."));

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
            AbilityTrigger.Battlecry, Card.AbilityEffectType.GainCoins, 4,
            "Battlecry: Gain 4 gold."));

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

        Debug.Log($"[CardDatabase] Generated {cards.Count} cards across 6 tiers");
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

        // Set legacy tribe field for backwards compatibility
        if (tribes != null && tribes.Length > 0 && tribes[0] != TribeType.None)
        {
            card.tribe = tribes[0].ToString();
        }

        return card;
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
