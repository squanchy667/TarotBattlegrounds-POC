using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OnAttack ability - triggers when this card attacks.
/// </summary>
public class OnAttackAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.OnAttack;

    private OnAttackEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum OnAttackEffect
    {
        BuffSelfAttack,             // Gain +X attack permanently
        BuffSelfHealth,             // Gain +X health permanently
        DealBonusDamage,            // Deal +X damage to target (this attack only)
        DealDamageToAdjacent,       // Deal X damage to adjacent enemies
        LifestealSelf,              // Heal self for X
        BuffRandomFriendlyAttack,   // Give random friendly +X attack
        Cleave                      // Also hit adjacent enemies for full damage
    }

    public OnAttackAbility(OnAttackEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            OnAttackEffect.BuffSelfAttack => $"On Attack: Gain +{_value} Attack",
            OnAttackEffect.BuffSelfHealth => $"On Attack: Gain +{_value} Health",
            OnAttackEffect.DealBonusDamage => $"On Attack: Deal +{_value} bonus damage",
            OnAttackEffect.DealDamageToAdjacent => $"On Attack: Deal {_value} damage to adjacent enemies",
            OnAttackEffect.LifestealSelf => $"On Attack: Restore {_value} Health",
            OnAttackEffect.BuffRandomFriendlyAttack => $"On Attack: Give a random friendly +{_value} Attack",
            OnAttackEffect.Cleave => "On Attack: Also damages adjacent enemies",
            _ => "On Attack: Unknown effect"
        };
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        switch (_effect)
        {
            case OnAttackEffect.BuffSelfAttack:
                AbilityEffects.BuffAttack(context.SourceCard, _value);
                break;
            case OnAttackEffect.BuffSelfHealth:
                AbilityEffects.BuffHealth(context.SourceCard, _value);
                break;
            case OnAttackEffect.DealBonusDamage:
                // Boost attack temporarily so damage calc uses the full combined value
                if (context.SourceCard != null)
                {
                    context.SourceCard.attack += _value;
                    context.SourceCard.tempBonusDamage += _value;
                    Debug.Log($"[OnAttack] {context.SourceCard.cardName} attack boosted by +{_value} for this strike (now {context.SourceCard.attack})");
                }
                break;
            case OnAttackEffect.DealDamageToAdjacent:
                DealDamageToAdjacent(context);
                break;
            case OnAttackEffect.LifestealSelf:
                AbilityEffects.BuffHealth(context.SourceCard, _value);
                Debug.Log($"[OnAttack] {context.SourceCard?.cardName} heals for {_value}");
                break;
            case OnAttackEffect.BuffRandomFriendlyAttack:
                BuffRandomFriendly(context);
                break;
            case OnAttackEffect.Cleave:
                DealCleave(context);
                break;
        }
    }

    private void DealDamageToAdjacent(AbilityContext context)
    {
        if (context.EnemyBoard == null || context.TargetCard == null) return;

        int targetIndex = context.EnemyBoard.IndexOf(context.TargetCard);
        if (targetIndex < 0) return;

        // Left of target
        if (targetIndex > 0)
        {
            Card left = context.EnemyBoard[targetIndex - 1];
            if (left.health > 0)
            {
                if (left.hasAegis)
                {
                    left.hasAegis = false;
                    Debug.Log($"[DamageAdjacent] {left.cardName}'s Aegis blocks adjacent damage");
                }
                else
                {
                    AbilityEffects.DealDamage(left, _value);
                }
            }
        }

        // Right of target
        if (targetIndex < context.EnemyBoard.Count - 1)
        {
            Card right = context.EnemyBoard[targetIndex + 1];
            if (right.health > 0)
            {
                if (right.hasAegis)
                {
                    right.hasAegis = false;
                    Debug.Log($"[DamageAdjacent] {right.cardName}'s Aegis blocks adjacent damage");
                }
                else
                {
                    AbilityEffects.DealDamage(right, _value);
                }
            }
        }
    }

    private void BuffRandomFriendly(AbilityContext context)
    {
        if (context.OwnerBoard == null) return;

        var targets = context.OwnerBoard.Where(c => c != context.SourceCard && c.health > 0).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        AbilityEffects.BuffAttack(target, _value);
    }

    private void DealCleave(AbilityContext context)
    {
        if (context.EnemyBoard == null || context.TargetCard == null || context.SourceCard == null) return;

        int targetIndex = context.EnemyBoard.IndexOf(context.TargetCard);
        if (targetIndex < 0) return;

        // Use configured value if > 0, otherwise full attack damage
        int cleaveDamage = _value > 0 ? _value : context.SourceCard.attack;

        // Left of target
        if (targetIndex > 0)
        {
            Card left = context.EnemyBoard[targetIndex - 1];
            if (left.health > 0)
            {
                if (left.hasAegis)
                {
                    left.hasAegis = false;
                    Debug.Log($"[Cleave] {left.cardName}'s Aegis blocks cleave damage");
                }
                else
                {
                    AbilityEffects.DealDamage(left, cleaveDamage);
                    Debug.Log($"[Cleave] {context.SourceCard.cardName} cleaves {left.cardName} for {cleaveDamage}");
                }
            }
        }

        // Right of target
        if (targetIndex < context.EnemyBoard.Count - 1)
        {
            Card right = context.EnemyBoard[targetIndex + 1];
            if (right.health > 0)
            {
                if (right.hasAegis)
                {
                    right.hasAegis = false;
                    Debug.Log($"[Cleave] {right.cardName}'s Aegis blocks cleave damage");
                }
                else
                {
                    AbilityEffects.DealDamage(right, cleaveDamage);
                    Debug.Log($"[Cleave] {context.SourceCard.cardName} cleaves {right.cardName} for {cleaveDamage}");
                }
            }
        }
    }
}
