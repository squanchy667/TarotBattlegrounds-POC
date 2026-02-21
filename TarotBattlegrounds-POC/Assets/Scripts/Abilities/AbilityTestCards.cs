using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper class to create test cards with abilities for testing the ability system.
/// Use AbilityTestCards.CreateTestCards() to get a list of cards with various abilities.
/// </summary>
public static class AbilityTestCards
{
    /// <summary>
    /// Create a set of test cards demonstrating all ability types.
    /// </summary>
    public static List<Card> CreateTestCards()
    {
        List<Card> cards = new List<Card>();

        // === BATTLECRY CARDS ===

        // Card 1: Battlecry - Buff Adjacent Attack
        cards.Add(CreateCard(
            "Battle Commander",
            tier: 2,
            attack: 2,
            health: 3,
            tribe: "Wands",
            abilityEffect: Card.AbilityEffectType.BuffAdjacentAttack,
            abilityValue: 2
        ));

        // Card 2: Battlecry - Buff All Friendly Attack
        cards.Add(CreateCard(
            "War Drummer",
            tier: 3,
            attack: 1,
            health: 4,
            tribe: "Wands",
            abilityEffect: Card.AbilityEffectType.BuffAllFriendlyAttack,
            abilityValue: 1
        ));

        // Card 3: Battlecry - Gain Aegis
        cards.Add(CreateCard(
            "Shield Bearer",
            tier: 2,
            attack: 1,
            health: 5,
            tribe: "Pentacles",
            abilityEffect: Card.AbilityEffectType.GainAegis,
            abilityValue: 0
        ));

        // Card 4: Battlecry - Gain Coins
        cards.Add(CreateCard(
            "Gold Finder",
            tier: 1,
            attack: 1,
            health: 1,
            tribe: "Pentacles",
            abilityEffect: Card.AbilityEffectType.GainCoins,
            abilityValue: 2
        ));

        // === DEATHRATTLE CARDS ===

        // Card 5: Deathrattle - Buff Random Friendly
        cards.Add(CreateCard(
            "Dying Mentor",
            tier: 2,
            attack: 2,
            health: 2,
            tribe: "Cups",
            abilityEffect: Card.AbilityEffectType.DeathrattleBuffRandomFriendly,
            abilityValue: 2
        ));

        // Card 6: Deathrattle - Damage Random Enemy
        cards.Add(CreateCard(
            "Explosive Imp",
            tier: 2,
            attack: 3,
            health: 1,
            tribe: "Swords",
            abilityEffect: Card.AbilityEffectType.DeathrattleDamageRandomEnemy,
            abilityValue: 3
        ));

        // Card 7: Deathrattle - Damage All Enemies
        cards.Add(CreateCard(
            "Bomb Lobber",
            tier: 4,
            attack: 2,
            health: 4,
            tribe: "Swords",
            abilityEffect: Card.AbilityEffectType.DeathrattleDamageAllEnemies,
            abilityValue: 2
        ));

        // === ON ATTACK CARDS ===

        // Card 8: OnAttack - Buff Self Attack
        cards.Add(CreateCard(
            "Raging Berserker",
            tier: 3,
            attack: 2,
            health: 5,
            tribe: "Wands",
            abilityEffect: Card.AbilityEffectType.OnAttackBuffSelf,
            abilityValue: 1
        ));

        // Card 9: OnAttack - Cleave
        cards.Add(CreateCard(
            "Whirlwind Warrior",
            tier: 4,
            attack: 4,
            health: 4,
            tribe: "Swords",
            abilityEffect: Card.AbilityEffectType.OnAttackCleave,
            abilityValue: 0
        ));

        // === TAUNT CARDS ===

        // Card 10: Taunt
        cards.Add(CreateCard(
            "Stone Guardian",
            tier: 2,
            attack: 1,
            health: 6,
            tribe: "Pentacles",
            abilityEffect: Card.AbilityEffectType.Taunt,
            abilityValue: 0
        ));

        Debug.Log($"[AbilityTestCards] Created {cards.Count} test cards with abilities");
        return cards;
    }

    /// <summary>
    /// Create a single card with specified ability.
    /// </summary>
    public static Card CreateCard(
        string name,
        int tier,
        int attack,
        int health,
        string tribe,
        Card.AbilityEffectType abilityEffect,
        int abilityValue)
    {
        Card card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tier = tier;
        card.attack = attack;
        card.health = health;
        card.tribe = tribe;
        card.abilityEffect = abilityEffect;
        card.abilityValue = abilityValue;

        // Set the appropriate trigger based on effect type
        card.abilityTrigger = GetTriggerForEffect(abilityEffect);

        // Set description
        card.ability = GetAbilityDescription(abilityEffect, abilityValue);

        return card;
    }

    private static AbilityTrigger GetTriggerForEffect(Card.AbilityEffectType effect)
    {
        return effect switch
        {
            Card.AbilityEffectType.BuffAdjacentAttack => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.BuffAdjacentHealth => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.BuffAdjacentStats => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.BuffAllFriendlyAttack => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.GainAegis => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.GainCoins => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.DeathrattleBuffRandomFriendly => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.DeathrattleDamageRandomEnemy => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.DeathrattleDamageAllEnemies => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.OnAttackBuffSelf => AbilityTrigger.OnAttack,
            Card.AbilityEffectType.OnAttackBonusDamage => AbilityTrigger.OnAttack,
            Card.AbilityEffectType.OnAttackCleave => AbilityTrigger.OnAttack,
            Card.AbilityEffectType.Taunt => AbilityTrigger.None, // Passive
            // Phase II triggers
            Card.AbilityEffectType.OnAllyDeathBuffSelf => AbilityTrigger.OnAllyDeath,
            Card.AbilityEffectType.OnAllyDeathBuffRandom => AbilityTrigger.OnAllyDeath,
            Card.AbilityEffectType.OnAllySummonedBuffSummoned => AbilityTrigger.OnAllySummoned,
            Card.AbilityEffectType.OnAllySummonedBuffSelf => AbilityTrigger.OnAllySummoned,
            Card.AbilityEffectType.OnSellGainCoins => AbilityTrigger.OnSell,
            Card.AbilityEffectType.OnSellBuffAllRemaining => AbilityTrigger.OnSell,
            Card.AbilityEffectType.AuraBuffTribematesAttack => AbilityTrigger.Aura,
            Card.AbilityEffectType.AuraBuffAdjacentStats => AbilityTrigger.Aura,
            Card.AbilityEffectType.AuraBuffAllFriendlyAttack => AbilityTrigger.Aura,
            // Phase II T105-T112 triggers
            Card.AbilityEffectType.Reborn => AbilityTrigger.None,
            Card.AbilityEffectType.Windfury => AbilityTrigger.None,
            Card.AbilityEffectType.Venomous => AbilityTrigger.None,
            Card.AbilityEffectType.SummonTokenOnDeath => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.SummonTokenOnPlay => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.StealBuffOnAttack => AbilityTrigger.OnAttack,
            Card.AbilityEffectType.GainArmor => AbilityTrigger.None,
            Card.AbilityEffectType.BuffAllTribeOnPlay => AbilityTrigger.Battlecry,
            Card.AbilityEffectType.BuffAllTribeOnDeath => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.RandomTransformOnDeath => AbilityTrigger.Deathrattle,
            Card.AbilityEffectType.BuffSelfHealth => AbilityTrigger.Battlecry,
            _ => AbilityTrigger.None
        };
    }

    private static string GetAbilityDescription(Card.AbilityEffectType effect, int value)
    {
        return effect switch
        {
            Card.AbilityEffectType.BuffAdjacentAttack => $"Battlecry: Give adjacent cards +{value} Attack",
            Card.AbilityEffectType.BuffAdjacentHealth => $"Battlecry: Give adjacent cards +{value} Health",
            Card.AbilityEffectType.BuffAdjacentStats => $"Battlecry: Give adjacent cards +{value}/+{value}",
            Card.AbilityEffectType.BuffAllFriendlyAttack => $"Battlecry: Give all friendly cards +{value} Attack",
            Card.AbilityEffectType.GainAegis => "Battlecry: Gain Aegis",
            Card.AbilityEffectType.GainCoins => $"Battlecry: Gain {value} coin(s)",
            Card.AbilityEffectType.DeathrattleBuffRandomFriendly => $"Deathrattle: Give a random friendly +{value}/+{value}",
            Card.AbilityEffectType.DeathrattleDamageRandomEnemy => $"Deathrattle: Deal {value} damage to a random enemy",
            Card.AbilityEffectType.DeathrattleDamageAllEnemies => $"Deathrattle: Deal {value} damage to all enemies",
            Card.AbilityEffectType.OnAttackBuffSelf => $"On Attack: Gain +{value} Attack",
            Card.AbilityEffectType.OnAttackBonusDamage => $"On Attack: Deal +{value} bonus damage",
            Card.AbilityEffectType.OnAttackCleave => "On Attack: Also damages adjacent enemies",
            Card.AbilityEffectType.Taunt => "Taunt",
            // Phase II descriptions
            Card.AbilityEffectType.OnAllyDeathBuffSelf => $"Whenever a friendly minion dies, gain +{value}/+{value}",
            Card.AbilityEffectType.OnAllyDeathBuffRandom => $"Whenever a friendly minion dies, give a random friendly +{value}/+{value}",
            Card.AbilityEffectType.OnAllySummonedBuffSummoned => $"Whenever a friendly minion is summoned, give it +{value}/+{value}",
            Card.AbilityEffectType.OnAllySummonedBuffSelf => $"Whenever a friendly minion is summoned, gain +{value} Attack",
            Card.AbilityEffectType.OnSellGainCoins => $"Sell: Gain {value} extra coin(s)",
            Card.AbilityEffectType.OnSellBuffAllRemaining => $"Sell: Give all remaining allies +{value}/+{value}",
            Card.AbilityEffectType.AuraBuffTribematesAttack => $"Aura: Other friendly tribe members have +{value} Attack",
            Card.AbilityEffectType.AuraBuffAdjacentStats => $"Aura: Adjacent minions have +{value}/+{value}",
            Card.AbilityEffectType.AuraBuffAllFriendlyAttack => $"Aura: All other friendly minions have +{value} Attack",
            // Phase II T105-T112 descriptions
            Card.AbilityEffectType.Reborn => "Reborn: Revives with 1 Health after first death",
            Card.AbilityEffectType.Windfury => "Windfury: Attacks twice per turn",
            Card.AbilityEffectType.Venomous => "Venomous: Destroy any minion damaged by this",
            Card.AbilityEffectType.SummonTokenOnDeath => $"Deathrattle: Summon a {value}/{value} token",
            Card.AbilityEffectType.SummonTokenOnPlay => $"Battlecry: Summon a {value}/{value} token",
            Card.AbilityEffectType.StealBuffOnAttack => $"On Attack: Steal +{value}/+{value} from target",
            Card.AbilityEffectType.GainArmor => $"Armor {value}: Reduces incoming damage by {value}",
            Card.AbilityEffectType.BuffAllTribeOnPlay => $"Battlecry: Give all friendly same-tribe minions +{value}/+{value}",
            Card.AbilityEffectType.BuffAllTribeOnDeath => $"Deathrattle: Give all friendly same-tribe minions +{value}/+{value}",
            Card.AbilityEffectType.RandomTransformOnDeath => "Deathrattle: Transform into a random card",
            Card.AbilityEffectType.BuffSelfHealth => $"Battlecry: Gain +{value} Health",
            _ => ""
        };
    }

    /// <summary>
    /// Add test cards to a TavernManager's pool for testing.
    /// </summary>
    public static void AddTestCardsToPool(TavernManager tavern)
    {
        if (tavern == null) return;

        var testCards = CreateTestCards();
        foreach (var card in testCards)
        {
            // Add multiple copies of each test card
            for (int i = 0; i < 3; i++)
            {
                tavern.GetFullPool().Add(card.Clone());
            }
        }

        Debug.Log($"[AbilityTestCards] Added {testCards.Count * 3} test cards to tavern pool");
    }
}
