using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Aura ability - a continuous passive effect active while this card is on the board.
/// Auras are applied when the card enters the board and recalculated when board state changes.
/// AuraManager handles refresh (remove all, then reapply) to prevent stacking.
/// </summary>
public class AuraAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.Aura;

    private AuraEffect _effect;
    private int _value;
    private string _description;

    /// <summary>Whether this aura is currently applied to the board.</summary>
    public bool IsActive { get; private set; } = false;

    private Dictionary<Card, int> _appliedAttackBuffs = new Dictionary<Card, int>();
    private Dictionary<Card, int> _appliedHealthBuffs = new Dictionary<Card, int>();

    public override string Description => _description;

    public enum AuraEffect
    {
        BuffTribematesAttack,
        BuffTribematesHealth,
        BuffTribematesStats,
        BuffAdjacentAttack,
        BuffAdjacentStats,
        BuffAllFriendlyAttack
    }

    /// <summary>
    /// Optional tribe filter for BuffTribemates effects.
    /// TribeType.None means apply to all friendly cards.
    /// </summary>
    public TribeType TargetTribe { get; private set; }

    public AuraAbility(AuraEffect effect, int value, TribeType targetTribe = TribeType.None)
    {
        _effect = effect;
        _value = value;
        TargetTribe = targetTribe;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        string tribeLabel = TargetTribe != TribeType.None ? $"{TargetTribe} " : "friendly ";
        return _effect switch
        {
            AuraEffect.BuffTribematesAttack  => $"Aura: Other {tribeLabel}minions have +{_value} Attack",
            AuraEffect.BuffTribematesHealth  => $"Aura: Other {tribeLabel}minions have +{_value} Health",
            AuraEffect.BuffTribematesStats   => $"Aura: Other {tribeLabel}minions have +{_value}/+{_value}",
            AuraEffect.BuffAdjacentAttack    => $"Aura: Adjacent minions have +{_value} Attack",
            AuraEffect.BuffAdjacentStats     => $"Aura: Adjacent minions have +{_value}/+{_value}",
            AuraEffect.BuffAllFriendlyAttack => $"Aura: All other friendly minions have +{_value} Attack",
            _ => "Aura: Unknown effect"
        };
    }

    public override bool CanExecute(AbilityContext context)
    {
        return context?.SourceCard != null && context.SourceCard.health > 0;
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        if (IsActive)
        {
            Debug.LogWarning($"[Aura] {context.SourceCard?.cardName} aura already active - call RemoveAura() first");
            return;
        }

        var targets = GetAuraTargets(context);
        foreach (var target in targets)
        {
            int atkBuff = 0;
            int hpBuff = 0;

            switch (_effect)
            {
                case AuraEffect.BuffTribematesAttack:
                case AuraEffect.BuffAllFriendlyAttack:
                case AuraEffect.BuffAdjacentAttack:
                    atkBuff = _value;
                    break;
                case AuraEffect.BuffTribematesHealth:
                    hpBuff = _value;
                    break;
                case AuraEffect.BuffTribematesStats:
                case AuraEffect.BuffAdjacentStats:
                    atkBuff = _value;
                    hpBuff = _value;
                    break;
            }

            if (atkBuff != 0)
            {
                AbilityEffects.BuffAttack(target, atkBuff);
                _appliedAttackBuffs[target] = _appliedAttackBuffs.GetValueOrDefault(target, 0) + atkBuff;
            }
            if (hpBuff != 0)
            {
                AbilityEffects.BuffHealth(target, hpBuff);
                _appliedHealthBuffs[target] = _appliedHealthBuffs.GetValueOrDefault(target, 0) + hpBuff;
            }
        }

        IsActive = true;
    }

    /// <summary>
    /// Remove all buffs granted by this aura.
    /// </summary>
    public void RemoveAura(AbilityContext context)
    {
        if (!IsActive) return;

        foreach (var kvp in _appliedAttackBuffs)
        {
            if (kvp.Key != null && kvp.Key.health > 0)
                AbilityEffects.BuffAttack(kvp.Key, -kvp.Value);
        }
        foreach (var kvp in _appliedHealthBuffs)
        {
            if (kvp.Key != null && kvp.Key.health > 0)
                AbilityEffects.BuffHealth(kvp.Key, -kvp.Value);
        }

        _appliedAttackBuffs.Clear();
        _appliedHealthBuffs.Clear();
        IsActive = false;
    }

    private List<Card> GetAuraTargets(AbilityContext context)
    {
        var board = context.OwnerBoard;
        if (board == null) return new List<Card>();

        switch (_effect)
        {
            case AuraEffect.BuffAdjacentAttack:
            case AuraEffect.BuffAdjacentStats:
            {
                int idx = board.IndexOf(context.SourceCard);
                var adj = new List<Card>();
                if (idx > 0 && board[idx - 1].health > 0) adj.Add(board[idx - 1]);
                if (idx >= 0 && idx < board.Count - 1 && board[idx + 1].health > 0) adj.Add(board[idx + 1]);
                return adj;
            }

            case AuraEffect.BuffTribematesAttack:
            case AuraEffect.BuffTribematesHealth:
            case AuraEffect.BuffTribematesStats:
            {
                return board
                    .Where(c => c != context.SourceCard && c.health > 0
                        && (TargetTribe == TribeType.None || c.HasTribe(TargetTribe)))
                    .ToList();
            }

            case AuraEffect.BuffAllFriendlyAttack:
            default:
                return board.Where(c => c != context.SourceCard && c.health > 0).ToList();
        }
    }
}
