using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable card data for network transmission.
/// Uses card name + tier as the unique identifier, plus mutable stats.
/// </summary>
[Serializable]
public struct NetworkCardData
{
    public string cardName;
    public int tier;
    public int attack;
    public int health;
    public bool isGolden;
    public bool hasAegis;
    public bool hasCleave;    // H6 fix: sync cleave flag for combat replay
    public bool hasReborn;    // H6 fix: sync reborn flag
    public bool hasWindfury;
    public bool hasVenomous;
    public int buyCostModifier;
    public int sellValueModifier;

    /// <summary>
    /// Create NetworkCardData from a Card instance.
    /// </summary>
    public static NetworkCardData FromCard(Card card)
    {
        return new NetworkCardData
        {
            cardName = card.cardName,
            tier = card.tier,
            attack = card.attack,
            health = card.health,
            isGolden = card.isGolden,
            hasAegis = card.hasAegis,
            hasCleave = card.hasCleave,
            hasReborn = card.hasReborn,
            hasWindfury = card.hasWindfury,
            hasVenomous = card.hasVenomous,
            buyCostModifier = card.buyCostModifier,
            sellValueModifier = card.sellValueModifier
        };
    }

    /// <summary>
    /// Reconstruct a Card from this network data using CardLookup templates.
    /// </summary>
    public Card ToCard()
    {
        Card template = CardLookup.FindTemplate(cardName, tier);
        if (template == null)
        {
            Debug.LogError($"[NetworkCardData] Template not found: '{cardName}' (Tier {tier}). CardLookup may not be initialized.");
            return null;
        }

        Card card = template.Clone();
        card.attack = attack;
        card.health = health;
        card.isGolden = isGolden;
        card.hasAegis = hasAegis;
        card.hasCleave = hasCleave;
        card.hasReborn = hasReborn;
        card.hasWindfury = hasWindfury;
        card.hasVenomous = hasVenomous;
        card.buyCostModifier = buyCostModifier;
        card.sellValueModifier = sellValueModifier;
        return card;
    }

    /// <summary>
    /// Convert a list of Cards to NetworkCardData array.
    /// </summary>
    public static NetworkCardData[] FromCardList(List<Card> cards)
    {
        if (cards == null) return new NetworkCardData[0];

        NetworkCardData[] data = new NetworkCardData[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            data[i] = FromCard(cards[i]);
        }
        return data;
    }

    /// <summary>
    /// Convert a NetworkCardData array to a list of Cards.
    /// </summary>
    public static List<Card> ToCardList(NetworkCardData[] data)
    {
        List<Card> cards = new List<Card>();
        if (data == null) return cards;

        foreach (var d in data)
        {
            Card card = d.ToCard();
            if (card != null)
                cards.Add(card);
        }
        return cards;
    }
}
