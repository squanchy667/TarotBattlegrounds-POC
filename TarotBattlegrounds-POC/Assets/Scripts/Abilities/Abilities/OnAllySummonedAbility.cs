using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OnAllySummoned ability - triggers when any friendly card is played or summoned to the board.
/// The newly summoned card is passed via context.TargetCard.
/// Fires for both shop purchases and token summons.
/// </summary>
public class OnAllySummonedAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.OnAllySummoned;

    private OnAllySummonedEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum OnAllySummonedEffect
    {
        BuffSummonedAttack,
        BuffSummonedHealth,
        BuffSummonedStats,
        BuffSelfAttack,
        BuffSelfStats,
        BuffAllAlliesAttack
    }

    public OnAllySummonedAbility(OnAllySummonedEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            OnAllySummonedEffect.BuffSummonedAttack  => $"Whenever a friendly minion is summoned, give it +{_value} Attack",
            OnAllySummonedEffect.BuffSummonedHealth  => $"Whenever a friendly minion is summoned, give it +{_value} Health",
            OnAllySummonedEffect.BuffSummonedStats   => $"Whenever a friendly minion is summoned, give it +{_value}/+{_value}",
            OnAllySummonedEffect.BuffSelfAttack      => $"Whenever a friendly minion is summoned, gain +{_value} Attack",
            OnAllySummonedEffect.BuffSelfStats       => $"Whenever a friendly minion is summoned, gain +{_value}/+{_value}",
            OnAllySummonedEffect.BuffAllAlliesAttack => $"Whenever a friendly minion is summoned, give all friendly minions +{_value} Attack",
            _ => "OnAllySummoned: Unknown effect"
        };
    }

    public override bool CanExecute(AbilityContext context)
    {
        if (!base.CanExecute(context)) return false;
        return context.SourceCard.health > 0;
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        switch (_effect)
        {
            case OnAllySummonedEffect.BuffSummonedAttack:
                if (context.TargetCard != null)
                    AbilityEffects.BuffAttack(context.TargetCard, _value);
                break;
            case OnAllySummonedEffect.BuffSummonedHealth:
                if (context.TargetCard != null)
                    AbilityEffects.BuffHealth(context.TargetCard, _value);
                break;
            case OnAllySummonedEffect.BuffSummonedStats:
                if (context.TargetCard != null)
                    AbilityEffects.BuffStats(context.TargetCard, _value, _value);
                break;
            case OnAllySummonedEffect.BuffSelfAttack:
                AbilityEffects.BuffAttack(context.SourceCard, _value);
                break;
            case OnAllySummonedEffect.BuffSelfStats:
                AbilityEffects.BuffStats(context.SourceCard, _value, _value);
                break;
            case OnAllySummonedEffect.BuffAllAlliesAttack:
                if (context.OwnerBoard != null)
                {
                    foreach (var card in context.OwnerBoard)
                    {
                        if (card.health > 0)
                            AbilityEffects.BuffAttack(card, _value);
                    }
                }
                break;
        }
    }
}
