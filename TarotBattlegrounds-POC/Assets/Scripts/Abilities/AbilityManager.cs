using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ability registration and triggering for cards.
/// Call TriggerAbilities() at appropriate times (play, combat, death, etc.)
/// </summary>
public static class AbilityManager
{
    // Maps card instances to their abilities
    private static Dictionary<Card, List<IAbility>> _cardAbilities = new Dictionary<Card, List<IAbility>>();

    /// <summary>
    /// Register an ability for a card. Skips if card already has abilities registered.
    /// </summary>
    public static void RegisterAbility(Card card, IAbility ability)
    {
        if (card == null || ability == null) return;

        // Check if this card already has abilities registered (prevent duplicates)
        if (_cardAbilities.ContainsKey(card) && _cardAbilities[card].Count > 0)
        {
            Debug.Log($"[AbilityManager] {card.cardName} already has abilities registered, skipping duplicate");
            return;
        }

        if (!_cardAbilities.ContainsKey(card))
        {
            _cardAbilities[card] = new List<IAbility>();
        }

        _cardAbilities[card].Add(ability);
        Debug.Log($"[AbilityManager] Registered {ability.GetType().Name} for {card.cardName}");
    }

    /// <summary>
    /// Remove all abilities for a card (call when card is destroyed/sold).
    /// </summary>
    public static void UnregisterCard(Card card)
    {
        if (card != null && _cardAbilities.ContainsKey(card))
        {
            _cardAbilities.Remove(card);
        }
    }

    /// <summary>
    /// Get all abilities for a card.
    /// </summary>
    public static List<IAbility> GetAbilities(Card card)
    {
        if (card != null && _cardAbilities.ContainsKey(card))
        {
            return _cardAbilities[card];
        }
        return new List<IAbility>();
    }

    /// <summary>
    /// Trigger all abilities of a specific type for a card.
    /// </summary>
    public static void TriggerAbilities(AbilityTrigger trigger, AbilityContext context)
    {
        if (context?.SourceCard == null) return;

        var abilities = GetAbilities(context.SourceCard);
        foreach (var ability in abilities)
        {
            if (ability.Trigger == trigger && ability.CanExecute(context))
            {
                ability.Execute(context);
            }
        }
    }

    /// <summary>
    /// Trigger abilities for all cards on a board.
    /// </summary>
    public static void TriggerAbilitiesForBoard(AbilityTrigger trigger, List<Card> board, Player owner, List<Card> enemyBoard = null)
    {
        if (board == null) return;

        foreach (var card in board)
        {
            var context = new AbilityContext
            {
                SourceCard = card,
                Owner = owner,
                OwnerBoard = board,
                EnemyBoard = enemyBoard
            };
            TriggerAbilities(trigger, context);
        }
    }

    /// <summary>
    /// Create a Battlecry ability context for when a card is played.
    /// </summary>
    public static AbilityContext CreateBattlecryContext(Card card, Player owner)
    {
        return new AbilityContext
        {
            SourceCard = card,
            Owner = owner,
            OwnerBoard = owner?.board
        };
    }

    /// <summary>
    /// Clear all registered abilities. Call when starting a new game.
    /// </summary>
    public static void ClearAll()
    {
        _cardAbilities.Clear();
    }
}
