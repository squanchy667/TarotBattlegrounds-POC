using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static lookup table mapping "cardName_tier" to Card templates.
/// Used for reconstructing cards from network data.
/// </summary>
public static class CardLookup
{
    private static Dictionary<string, Card> _templates = new Dictionary<string, Card>();
    private static bool _initialized = false;

    /// <summary>
    /// Build the lookup from CardDatabase. Call once at game start.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        _templates.Clear();
        List<Card> allCards = CardDatabase.GenerateAllCards();

        foreach (Card card in allCards)
        {
            string key = MakeKey(card.cardName, card.tier);
            if (!_templates.ContainsKey(key))
            {
                _templates[key] = card;
            }
        }

        _initialized = true;
        Debug.Log($"[CardLookup] Initialized with {_templates.Count} templates.");
    }

    /// <summary>
    /// Find a card template by name and tier. Returns null if not found.
    /// </summary>
    public static Card FindTemplate(string cardName, int tier)
    {
        if (!_initialized)
            Initialize();

        string key = MakeKey(cardName, tier);
        if (_templates.TryGetValue(key, out Card template))
        {
            return template;
        }

        Debug.LogWarning($"[CardLookup] Template not found: {key}");
        return null;
    }

    private static string MakeKey(string cardName, int tier)
    {
        return $"{cardName}_{tier}";
    }

    /// <summary>
    /// Force re-initialization (e.g., when returning to lobby).
    /// </summary>
    public static void Reset()
    {
        _templates.Clear();
        _initialized = false;
    }
}
