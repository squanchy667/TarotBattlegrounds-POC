using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OnSell ability - triggers when THIS card is sold from board or hand.
/// OwnerBoard reflects the board state BEFORE this card is removed.
/// </summary>
public class OnSellAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.OnSell;

    private OnSellEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum OnSellEffect
    {
        GainCoins,
        BuffAllRemainingAttack,
        BuffAllRemainingHealth,
        BuffAllRemainingStats,
        BuffRandomRemainingStats
    }

    public OnSellAbility(OnSellEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            OnSellEffect.GainCoins               => $"Sell: Gain {_value} extra coin(s)",
            OnSellEffect.BuffAllRemainingAttack  => $"Sell: Give all remaining friendly minions +{_value} Attack",
            OnSellEffect.BuffAllRemainingHealth  => $"Sell: Give all remaining friendly minions +{_value} Health",
            OnSellEffect.BuffAllRemainingStats   => $"Sell: Give all remaining friendly minions +{_value}/+{_value}",
            OnSellEffect.BuffRandomRemainingStats => $"Sell: Give a random remaining friendly minion +{_value}/+{_value}",
            _ => "OnSell: Unknown effect"
        };
    }

    /// <summary>
    /// OnSell always executes - the card is being sold, no health check needed.
    /// </summary>
    public override bool CanExecute(AbilityContext context)
    {
        return context?.SourceCard != null;
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        switch (_effect)
        {
            case OnSellEffect.GainCoins:
                if (context.Owner != null)
                {
                    context.Owner.coins += _value;
                    Debug.Log($"[OnSell] {context.SourceCard?.cardName} sell bonus: +{_value} coin(s)");
                }
                break;
            case OnSellEffect.BuffAllRemainingAttack:
                BuffRemaining(context, _value, 0);
                break;
            case OnSellEffect.BuffAllRemainingHealth:
                BuffRemaining(context, 0, _value);
                break;
            case OnSellEffect.BuffAllRemainingStats:
                BuffRemaining(context, _value, _value);
                break;
            case OnSellEffect.BuffRandomRemainingStats:
                BuffRandomRemaining(context, _value, _value);
                break;
        }
    }

    private void BuffRemaining(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;
        foreach (var card in context.OwnerBoard)
        {
            if (card == context.SourceCard) continue;
            if (attack > 0) AbilityEffects.BuffAttack(card, attack);
            if (health > 0) AbilityEffects.BuffHealth(card, health);
        }
    }

    private void BuffRandomRemaining(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;
        var candidates = context.OwnerBoard.Where(c => c != context.SourceCard).ToList();
        if (candidates.Count == 0) return;
        Card target = candidates[Random.Range(0, candidates.Count)];
        if (attack > 0) AbilityEffects.BuffAttack(target, attack);
        if (health > 0) AbilityEffects.BuffHealth(target, health);
    }
}
