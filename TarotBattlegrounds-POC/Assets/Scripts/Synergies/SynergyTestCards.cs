using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper class to create test cards with tribes for testing the synergy system.
/// Includes single-tribe and multi-tribe cards.
/// </summary>
public static class SynergyTestCards
{
    /// <summary>
    /// Create a full set of test cards covering all tribes and multi-tribe combinations.
    /// </summary>
    public static List<Card> CreateTestCards()
    {
        List<Card> cards = new List<Card>();

        // === SINGLE TRIBE CARDS (3 per tribe) ===

        // Pentacles (Economy)
        cards.Add(CreateCard("Gold Merchant", 1, 1, 2, new[] { TribeType.Pentacles }));
        cards.Add(CreateCard("Treasure Hunter", 2, 2, 3, new[] { TribeType.Pentacles }));
        cards.Add(CreateCard("Wealthy Noble", 3, 3, 4, new[] { TribeType.Pentacles }));

        // Cups (Healing)
        cards.Add(CreateCard("Healing Spring", 1, 1, 3, new[] { TribeType.Cups }));
        cards.Add(CreateCard("Water Priest", 2, 2, 4, new[] { TribeType.Cups }));
        cards.Add(CreateCard("Ocean Guardian", 3, 2, 6, new[] { TribeType.Cups }));

        // Swords (Aggro)
        cards.Add(CreateCard("Blade Initiate", 1, 2, 1, new[] { TribeType.Swords }));
        cards.Add(CreateCard("Sword Master", 2, 4, 2, new[] { TribeType.Swords }));
        cards.Add(CreateCard("Blade Storm", 3, 5, 3, new[] { TribeType.Swords }));

        // Wands (Buffs)
        cards.Add(CreateCard("Spark Mage", 1, 1, 2, new[] { TribeType.Wands }));
        cards.Add(CreateCard("Flame Enchanter", 2, 2, 3, new[] { TribeType.Wands }));
        cards.Add(CreateCard("Inferno Lord", 3, 3, 5, new[] { TribeType.Wands }));

        // === MULTI-TRIBE CARDS (4 combo cards) ===

        // Pentacles + Cups
        cards.Add(CreateCard("Healing Merchant", 2, 2, 3,
            new[] { TribeType.Pentacles, TribeType.Cups },
            "Dual tribe: Benefits from both Pentacles and Cups synergies"));

        // Cups + Wands
        cards.Add(CreateCard("Ember Healer", 2, 2, 4,
            new[] { TribeType.Cups, TribeType.Wands },
            "Dual tribe: Benefits from both Cups and Wands synergies"));

        // Swords + Pentacles
        cards.Add(CreateCard("Mercenary Captain", 3, 4, 3,
            new[] { TribeType.Swords, TribeType.Pentacles },
            "Dual tribe: Benefits from both Swords and Pentacles synergies"));

        // Wands + Swords
        cards.Add(CreateCard("Battle Mage", 3, 4, 4,
            new[] { TribeType.Wands, TribeType.Swords },
            "Dual tribe: Benefits from both Wands and Swords synergies"));

        // === TRIPLE TRIBE CARD (rare) ===
        cards.Add(CreateCard("Arcane Trinity", 4, 3, 5,
            new[] { TribeType.Cups, TribeType.Wands, TribeType.Swords },
            "Triple tribe: Benefits from Cups, Wands, and Swords synergies"));

        Debug.Log($"[SynergyTestCards] Created {cards.Count} test cards with tribes");
        return cards;
    }

    /// <summary>
    /// Create a single card with specified tribes.
    /// </summary>
    public static Card CreateCard(string name, int tier, int attack, int health, TribeType[] tribes, string ability = "")
    {
        Card card = ScriptableObject.CreateInstance<Card>();
        card.cardName = name;
        card.tier = tier;
        card.attack = attack;
        card.health = health;
        card.tribes = tribes;
        card.ability = ability;

        // Also set legacy tribe field to first tribe for backwards compatibility
        if (tribes != null && tribes.Length > 0 && tribes[0] != TribeType.None)
        {
            card.tribe = tribes[0].ToString();
        }

        return card;
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

        Debug.Log($"[SynergyTestCards] Added {testCards.Count * 3} tribe test cards to tavern pool");
    }

    /// <summary>
    /// Create a specific board composition for testing synergies.
    /// </summary>
    public static List<Card> CreateTestBoard_PentaclesCups()
    {
        // 2 Pentacles + 2 Cups = Both tier 2 synergies active + combo
        return new List<Card>
        {
            CreateCard("Gold Merchant", 1, 1, 2, new[] { TribeType.Pentacles }),
            CreateCard("Treasure Hunter", 2, 2, 3, new[] { TribeType.Pentacles }),
            CreateCard("Healing Spring", 1, 1, 3, new[] { TribeType.Cups }),
            CreateCard("Water Priest", 2, 2, 4, new[] { TribeType.Cups })
        };
    }

    /// <summary>
    /// Create a board with 4 Swords for tier 4 testing.
    /// </summary>
    public static List<Card> CreateTestBoard_SwordsTier4()
    {
        return new List<Card>
        {
            CreateCard("Blade Initiate", 1, 2, 1, new[] { TribeType.Swords }),
            CreateCard("Sword Master", 2, 4, 2, new[] { TribeType.Swords }),
            CreateCard("Blade Storm", 3, 5, 3, new[] { TribeType.Swords }),
            CreateCard("Mercenary Captain", 3, 4, 3, new[] { TribeType.Swords, TribeType.Pentacles })
        };
    }

    /// <summary>
    /// Create a board with 6 Wands for tier 6 testing.
    /// </summary>
    public static List<Card> CreateTestBoard_WandsTier6()
    {
        return new List<Card>
        {
            CreateCard("Spark Mage", 1, 1, 2, new[] { TribeType.Wands }),
            CreateCard("Spark Mage", 1, 1, 2, new[] { TribeType.Wands }),
            CreateCard("Flame Enchanter", 2, 2, 3, new[] { TribeType.Wands }),
            CreateCard("Flame Enchanter", 2, 2, 3, new[] { TribeType.Wands }),
            CreateCard("Inferno Lord", 3, 3, 5, new[] { TribeType.Wands }),
            CreateCard("Battle Mage", 3, 4, 4, new[] { TribeType.Wands, TribeType.Swords })
        };
    }

    /// <summary>
    /// Create a diverse board with all 4 tribes.
    /// </summary>
    public static List<Card> CreateTestBoard_AllTribes()
    {
        return new List<Card>
        {
            CreateCard("Gold Merchant", 1, 1, 2, new[] { TribeType.Pentacles }),
            CreateCard("Healing Spring", 1, 1, 3, new[] { TribeType.Cups }),
            CreateCard("Blade Initiate", 1, 2, 1, new[] { TribeType.Swords }),
            CreateCard("Spark Mage", 1, 1, 2, new[] { TribeType.Wands }),
            CreateCard("Healing Merchant", 2, 2, 3, new[] { TribeType.Pentacles, TribeType.Cups }),
            CreateCard("Battle Mage", 3, 4, 4, new[] { TribeType.Wands, TribeType.Swords })
        };
    }
}
