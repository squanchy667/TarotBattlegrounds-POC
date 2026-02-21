using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// BuffAllTribes ability (T111) - buffs all friendly cards of the same tribe as this card.
/// Can trigger as Battlecry (on play) or Deathrattle (on death).
/// Gives +X/+X to all other friendly cards sharing at least one tribe.
/// </summary>
public class BuffAllTribesAbility : AbilityBase
{
    private AbilityTrigger _trigger;
    private int _value;
    private string _description;

    public override AbilityTrigger Trigger => _trigger;
    public override string Description => _description;

    public BuffAllTribesAbility(AbilityTrigger trigger, int value)
    {
        _trigger = trigger;
        _value = value;
        string prefix = trigger == AbilityTrigger.Battlecry ? "Battlecry" : "Deathrattle";
        _description = $"{prefix}: Give all friendly same-tribe minions +{_value}/+{_value}";
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        if (context.OwnerBoard == null || context.SourceCard == null) return;

        TribeType[] sourceTribes = context.SourceCard.GetTribes();
        if (sourceTribes == null || sourceTribes.Length == 0)
        {
            // Fallback to legacy tribe
            if (!string.IsNullOrEmpty(context.SourceCard.tribe))
            {
                TribeType parsed = ThemeManager.ParseTribeName(context.SourceCard.tribe);
                if (parsed != TribeType.None)
                    sourceTribes = new[] { parsed };
            }
        }

        if (sourceTribes == null || sourceTribes.Length == 0)
        {
            Debug.Log($"[BuffAllTribes] {context.SourceCard.cardName} has no tribe, skipping");
            return;
        }

        int buffedCount = 0;
        foreach (Card card in context.OwnerBoard)
        {
            if (card == context.SourceCard || card.health <= 0) continue;

            // Check if card shares any tribe with source
            bool sharesTribe = false;
            foreach (var tribe in sourceTribes)
            {
                if (tribe != TribeType.None && card.HasTribe(tribe))
                {
                    sharesTribe = true;
                    break;
                }
            }

            if (sharesTribe)
            {
                AbilityEffects.BuffStats(card, _value, _value);
                buffedCount++;
            }
        }

        Debug.Log($"[BuffAllTribes] {context.SourceCard.cardName} buffed {buffedCount} tribmates by +{_value}/+{_value}");
    }
}
