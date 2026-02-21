using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Battlecry ability - triggers when card is played from hand to board.
/// Supports various effects like buffing adjacent cards, dealing damage, etc.
/// </summary>
public class BattlecryAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.Battlecry;

    private BattlecryEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum BattlecryEffect
    {
        BuffAdjacentAttack,         // +X attack to adjacent cards
        BuffAdjacentHealth,         // +X health to adjacent cards
        BuffAdjacentStats,          // +X/+X to adjacent cards
        BuffAllFriendlyAttack,      // +X attack to all friendly cards (includes self)
        BuffAllFriendlyHealth,      // +X health to all friendly cards (includes self)
        BuffOtherFriendlyAttack,    // +X attack to all other friendly cards (excludes self)
        BuffOtherFriendlyHealth,    // +X health to all other friendly cards (excludes self)
        DealDamageToRandom,         // Deal X damage to random enemy (for combat start)
        GainAegis,                  // This card gains Aegis
        DrawCard,                   // Draw X cards (placeholder for future)
        GainCoins,                  // Gain X coins
        BuffSelfHealth              // This card gains +X Health
    }

    public BattlecryAbility(BattlecryEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            BattlecryEffect.BuffAdjacentAttack => $"Battlecry: Give adjacent cards +{_value} Attack",
            BattlecryEffect.BuffAdjacentHealth => $"Battlecry: Give adjacent cards +{_value} Health",
            BattlecryEffect.BuffAdjacentStats => $"Battlecry: Give adjacent cards +{_value}/+{_value}",
            BattlecryEffect.BuffAllFriendlyAttack => $"Battlecry: Give all friendly minions +{_value} Attack",
            BattlecryEffect.BuffAllFriendlyHealth => $"Battlecry: Give all friendly minions +{_value} Health",
            BattlecryEffect.BuffOtherFriendlyAttack => $"Battlecry: Give all other friendly minions +{_value} Attack",
            BattlecryEffect.BuffOtherFriendlyHealth => $"Battlecry: Give all other friendly minions +{_value} Health",
            BattlecryEffect.DealDamageToRandom => $"Battlecry: Deal {_value} damage to a random enemy",
            BattlecryEffect.GainAegis => "Battlecry: Gain Aegis",
            BattlecryEffect.DrawCard => $"Battlecry: Draw {_value} card(s)",
            BattlecryEffect.GainCoins => $"Battlecry: Gain {_value} coin(s)",
            BattlecryEffect.BuffSelfHealth => $"Battlecry: Gain +{_value} Health",
            _ => "Battlecry: Unknown effect"
        };
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        switch (_effect)
        {
            case BattlecryEffect.BuffAdjacentAttack:
                BuffAdjacent(context, _value, 0);
                break;
            case BattlecryEffect.BuffAdjacentHealth:
                BuffAdjacent(context, 0, _value);
                break;
            case BattlecryEffect.BuffAdjacentStats:
                BuffAdjacent(context, _value, _value);
                break;
            case BattlecryEffect.BuffAllFriendlyAttack:
                BuffAllFriendly(context, _value, 0, includeSelf: true);
                break;
            case BattlecryEffect.BuffAllFriendlyHealth:
                BuffAllFriendly(context, 0, _value, includeSelf: true);
                break;
            case BattlecryEffect.BuffOtherFriendlyAttack:
                BuffAllFriendly(context, _value, 0, includeSelf: false);
                break;
            case BattlecryEffect.BuffOtherFriendlyHealth:
                BuffAllFriendly(context, 0, _value, includeSelf: false);
                break;
            case BattlecryEffect.DealDamageToRandom:
                // This would be used at start of combat, not on play
                Debug.Log($"[Battlecry] {context.SourceCard.cardName} will deal {_value} damage at combat start");
                break;
            case BattlecryEffect.GainAegis:
                AbilityEffects.GrantAegis(context.SourceCard);
                break;
            case BattlecryEffect.GainCoins:
                if (context.Owner != null)
                {
                    context.Owner.coins += _value;
                    Debug.Log($"[Battlecry] {context.SourceCard.cardName} grants {_value} coins");
                }
                break;
            case BattlecryEffect.BuffSelfHealth:
                AbilityEffects.BuffHealth(context.SourceCard, _value);
                break;
        }
    }

    private void BuffAdjacent(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;

        int index = context.OwnerBoard.IndexOf(context.SourceCard);
        if (index < 0) return;

        // Left neighbor
        if (index > 0)
        {
            Card left = context.OwnerBoard[index - 1];
            if (attack > 0) AbilityEffects.BuffAttack(left, attack);
            if (health > 0) AbilityEffects.BuffHealth(left, health);
        }

        // Right neighbor
        if (index < context.OwnerBoard.Count - 1)
        {
            Card right = context.OwnerBoard[index + 1];
            if (attack > 0) AbilityEffects.BuffAttack(right, attack);
            if (health > 0) AbilityEffects.BuffHealth(right, health);
        }
    }

    private void BuffAllFriendly(AbilityContext context, int attack, int health, bool includeSelf)
    {
        if (context.OwnerBoard == null) return;

        foreach (Card card in context.OwnerBoard)
        {
            if (!includeSelf && card == context.SourceCard) continue;
            if (attack > 0) AbilityEffects.BuffAttack(card, attack);
            if (health > 0) AbilityEffects.BuffHealth(card, health);
        }
    }
}
