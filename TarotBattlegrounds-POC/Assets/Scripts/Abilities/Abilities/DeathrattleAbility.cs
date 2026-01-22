using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Deathrattle ability - triggers when the card dies.
/// </summary>
public class DeathrattleAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.Deathrattle;

    private DeathrattleEffect _effect;
    private int _value;
    private string _description;

    public override string Description => _description;

    public enum DeathrattleEffect
    {
        BuffRandomFriendlyAttack,   // +X attack to random friendly
        BuffRandomFriendlyHealth,   // +X health to random friendly
        BuffRandomFriendlyStats,    // +X/+X to random friendly
        BuffAllFriendlyAttack,      // +X attack to all friendly
        BuffAllFriendlyHealth,      // +X health to all friendly
        DealDamageToRandomEnemy,    // Deal X damage to random enemy
        DealDamageToAllEnemies,     // Deal X damage to all enemies
        SummonToken,                // Summon a X/X token (simplified)
        GainCoins                   // Owner gains X coins
    }

    public DeathrattleAbility(DeathrattleEffect effect, int value)
    {
        _effect = effect;
        _value = value;
        _description = GenerateDescription();
    }

    private string GenerateDescription()
    {
        return _effect switch
        {
            DeathrattleEffect.BuffRandomFriendlyAttack => $"Deathrattle: Give a random friendly +{_value} Attack",
            DeathrattleEffect.BuffRandomFriendlyHealth => $"Deathrattle: Give a random friendly +{_value} Health",
            DeathrattleEffect.BuffRandomFriendlyStats => $"Deathrattle: Give a random friendly +{_value}/+{_value}",
            DeathrattleEffect.BuffAllFriendlyAttack => $"Deathrattle: Give all friendly cards +{_value} Attack",
            DeathrattleEffect.BuffAllFriendlyHealth => $"Deathrattle: Give all friendly cards +{_value} Health",
            DeathrattleEffect.DealDamageToRandomEnemy => $"Deathrattle: Deal {_value} damage to a random enemy",
            DeathrattleEffect.DealDamageToAllEnemies => $"Deathrattle: Deal {_value} damage to all enemies",
            DeathrattleEffect.SummonToken => $"Deathrattle: Summon a {_value}/{_value} token",
            DeathrattleEffect.GainCoins => $"Deathrattle: Gain {_value} coin(s)",
            _ => "Deathrattle: Unknown effect"
        };
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        switch (_effect)
        {
            case DeathrattleEffect.BuffRandomFriendlyAttack:
                BuffRandomFriendly(context, _value, 0);
                break;
            case DeathrattleEffect.BuffRandomFriendlyHealth:
                BuffRandomFriendly(context, 0, _value);
                break;
            case DeathrattleEffect.BuffRandomFriendlyStats:
                BuffRandomFriendly(context, _value, _value);
                break;
            case DeathrattleEffect.BuffAllFriendlyAttack:
                BuffAllFriendly(context, _value, 0);
                break;
            case DeathrattleEffect.BuffAllFriendlyHealth:
                BuffAllFriendly(context, 0, _value);
                break;
            case DeathrattleEffect.DealDamageToRandomEnemy:
                DealDamageToRandom(context);
                break;
            case DeathrattleEffect.DealDamageToAllEnemies:
                DealDamageToAll(context);
                break;
            case DeathrattleEffect.SummonToken:
                // Simplified: just log for now, full implementation would add card to board
                Debug.Log($"[Deathrattle] {context.SourceCard?.cardName} would summon a {_value}/{_value} token");
                break;
            case DeathrattleEffect.GainCoins:
                if (context.Owner != null)
                {
                    context.Owner.coins += _value;
                    Debug.Log($"[Deathrattle] {context.SourceCard?.cardName} grants {_value} coins");
                }
                break;
        }
    }

    private void BuffRandomFriendly(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;

        var targets = context.OwnerBoard.Where(c => c != context.SourceCard && c.health > 0).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        if (attack > 0) AbilityEffects.BuffAttack(target, attack);
        if (health > 0) AbilityEffects.BuffHealth(target, health);
    }

    private void BuffAllFriendly(AbilityContext context, int attack, int health)
    {
        if (context.OwnerBoard == null) return;

        foreach (Card card in context.OwnerBoard)
        {
            if (card != context.SourceCard && card.health > 0)
            {
                if (attack > 0) AbilityEffects.BuffAttack(card, attack);
                if (health > 0) AbilityEffects.BuffHealth(card, health);
            }
        }
    }

    private void DealDamageToRandom(AbilityContext context)
    {
        if (context.EnemyBoard == null) return;

        var targets = context.EnemyBoard.Where(c => c.health > 0).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        AbilityEffects.DealDamage(target, _value);
    }

    private void DealDamageToAll(AbilityContext context)
    {
        if (context.EnemyBoard == null) return;

        foreach (Card card in context.EnemyBoard.Where(c => c.health > 0))
        {
            AbilityEffects.DealDamage(card, _value);
        }
    }
}
