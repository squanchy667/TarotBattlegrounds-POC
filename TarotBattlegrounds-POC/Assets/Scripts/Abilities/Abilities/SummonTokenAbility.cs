using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SummonToken ability (T108) - summons a token card on the board.
/// Can trigger as Deathrattle (OnDeath) or Battlecry (OnPlay).
/// Token is a simple X/X card with no abilities.
/// </summary>
public class SummonTokenAbility : AbilityBase
{
    private SummonTrigger _trigger;
    private int _value;
    private string _description;

    public override AbilityTrigger Trigger => _trigger == SummonTrigger.OnDeath
        ? AbilityTrigger.Deathrattle
        : AbilityTrigger.Battlecry;

    public override string Description => _description;

    public enum SummonTrigger
    {
        OnDeath,    // Deathrattle: summon token
        OnPlay      // Battlecry: summon token
    }

    public SummonTokenAbility(SummonTrigger trigger, int value)
    {
        _trigger = trigger;
        _value = value;
        _description = trigger == SummonTrigger.OnDeath
            ? $"Deathrattle: Summon a {_value}/{_value} token"
            : $"Battlecry: Summon a {_value}/{_value} token";
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        if (context.OwnerBoard == null) return;
        // Count only alive cards (dead cards may still be in list during deathrattle processing)
        int aliveCount = 0;
        foreach (var c in context.OwnerBoard)
            if (c.health > 0) aliveCount++;
        if (aliveCount >= 7)
        {
            Debug.Log($"[SummonToken] Board full, cannot summon token for {context.SourceCard?.cardName}");
            return;
        }

        Card token = CreateToken(context.SourceCard);
        int insertIndex = GetInsertIndex(context);
        context.OwnerBoard.Insert(insertIndex, token);

        Debug.Log($"[SummonToken] {context.SourceCard?.cardName} summons a {_value}/{_value} token at position {insertIndex}");
    }

    private Card CreateToken(Card source)
    {
        Card token = ScriptableObject.CreateInstance<Card>();
        token.cardName = $"{source?.cardName ?? "Unknown"} Token";
        token.tier = 1;
        token.attack = _value;
        token.health = _value;
        token.tribe = source?.tribe ?? "";
        if (source?.tribes != null && source.tribes.Length > 0)
        {
            token.tribes = new TribeType[source.tribes.Length];
            System.Array.Copy(source.tribes, token.tribes, source.tribes.Length);
        }
        else
        {
            token.tribes = new TribeType[0];
        }
        return token;
    }

    private int GetInsertIndex(AbilityContext context)
    {
        // Insert at the position of the source card (or end of board)
        int idx = context.OwnerBoard.IndexOf(context.SourceCard);
        return idx >= 0 ? idx : context.OwnerBoard.Count;
    }
}
