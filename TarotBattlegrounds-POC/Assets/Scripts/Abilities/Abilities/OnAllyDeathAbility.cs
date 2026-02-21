using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OnAllyDeath ability - triggers when any friendly card (not self) dies on the same board.
/// The dying card is passed via context.TargetCard.
/// </summary>
public class OnAllyDeathAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.OnAllyDeath;

    private OnAllyDeathEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum OnAllyDeathEffect
    {
        BuffRandomAllyAttack,
        BuffRandomAllyHealth,
        BuffRandomAllyStats,
        BuffSelfAttack,
        BuffSelfHealth,
        BuffSelfStats
    }

    public OnAllyDeathAbility(OnAllyDeathEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            OnAllyDeathEffect.BuffRandomAllyAttack => $"Whenever a friendly minion dies, give a random friendly minion +{_value} Attack",
            OnAllyDeathEffect.BuffRandomAllyHealth => $"Whenever a friendly minion dies, give a random friendly minion +{_value} Health",
            OnAllyDeathEffect.BuffRandomAllyStats  => $"Whenever a friendly minion dies, give a random friendly minion +{_value}/+{_value}",
            OnAllyDeathEffect.BuffSelfAttack       => $"Whenever a friendly minion dies, gain +{_value} Attack",
            OnAllyDeathEffect.BuffSelfHealth       => $"Whenever a friendly minion dies, gain +{_value} Health",
            OnAllyDeathEffect.BuffSelfStats        => $"Whenever a friendly minion dies, gain +{_value}/+{_value}",
            _ => "OnAllyDeath: Unknown effect"
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
            case OnAllyDeathEffect.BuffRandomAllyAttack:
                BuffRandomSurvivor(context, _value, 0);
                break;
            case OnAllyDeathEffect.BuffRandomAllyHealth:
                BuffRandomSurvivor(context, 0, _value);
                break;
            case OnAllyDeathEffect.BuffRandomAllyStats:
                BuffRandomSurvivor(context, _value, _value);
                break;
            case OnAllyDeathEffect.BuffSelfAttack:
                AbilityEffects.BuffAttack(context.SourceCard, _value);
                break;
            case OnAllyDeathEffect.BuffSelfHealth:
                AbilityEffects.BuffHealth(context.SourceCard, _value);
                break;
            case OnAllyDeathEffect.BuffSelfStats:
                AbilityEffects.BuffStats(context.SourceCard, _value, _value);
                break;
        }
    }

    private void BuffRandomSurvivor(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;
        var candidates = context.OwnerBoard
            .Where(c => c != context.SourceCard && c != context.TargetCard && c.health > 0)
            .ToList();
        if (candidates.Count == 0) return;
        Card target = candidates[Random.Range(0, candidates.Count)];
        if (attack > 0) AbilityEffects.BuffAttack(target, attack);
        if (health > 0) AbilityEffects.BuffHealth(target, health);
    }
}
