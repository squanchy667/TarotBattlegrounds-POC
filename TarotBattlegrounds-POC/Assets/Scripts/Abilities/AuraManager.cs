using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages active auras on a player's board.
/// Call RefreshAuras() whenever board state changes (card added, removed, combat death).
/// Removes all existing aura buffs then reapplies cleanly to prevent stacking.
/// </summary>
public static class AuraManager
{
    /// <summary>
    /// Reapply all auras on a board from scratch.
    /// </summary>
    public static void RefreshAuras(List<Card> board, Player owner)
    {
        if (board == null || board.Count == 0) return;

        // First pass: remove all currently active auras
        foreach (var card in board)
        {
            var auraAbilities = AbilityManager.GetAbilities(card)
                .OfType<AuraAbility>()
                .ToList();

            foreach (var aura in auraAbilities)
            {
                if (aura.IsActive)
                {
                    var removeCtx = new AbilityContext
                    {
                        SourceCard = card,
                        Owner = owner,
                        OwnerBoard = board
                    };
                    aura.RemoveAura(removeCtx);
                }
            }
        }

        // Second pass: reapply all auras on alive cards
        foreach (var card in board)
        {
            if (card.health <= 0) continue;

            var context = new AbilityContext
            {
                SourceCard = card,
                Owner = owner,
                OwnerBoard = board
            };
            AbilityManager.TriggerAbilities(AbilityTrigger.Aura, context);
        }
    }

    /// <summary>
    /// Remove all auras for a specific card (when it is sold or dies).
    /// </summary>
    public static void RemoveAurasForCard(Card card, List<Card> board, Player owner)
    {
        if (card == null) return;

        var auraAbilities = AbilityManager.GetAbilities(card)
            .OfType<AuraAbility>()
            .ToList();

        foreach (var aura in auraAbilities)
        {
            if (aura.IsActive)
            {
                var ctx = new AbilityContext
                {
                    SourceCard = card,
                    Owner = owner,
                    OwnerBoard = board
                };
                aura.RemoveAura(ctx);
            }
        }
    }
}
