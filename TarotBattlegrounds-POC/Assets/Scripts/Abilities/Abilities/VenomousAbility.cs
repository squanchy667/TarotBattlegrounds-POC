using UnityEngine;

/// <summary>
/// Venomous ability (T107) - passive keyword that instantly kills any minion damaged by this card.
/// The actual instant-kill logic is handled in CombatManager's damage application.
/// This ability class sets the hasVenomous flag on registration.
/// </summary>
public class VenomousAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.None;
    public override string Description => "Venomous: Destroy any minion damaged by this";

    protected override void ExecuteEffect(AbilityContext context)
    {
        // Venomous is passive - no active effect to execute
        Debug.Log($"[Venomous] {context.SourceCard?.cardName} has Venomous (passive effect)");
    }

    /// <summary>
    /// Check if a card has Venomous via the ability system or passive flag.
    /// </summary>
    public static bool HasVenomous(Card card)
    {
        if (card == null) return false;
        if (card.hasVenomous) return true;

        var abilities = AbilityManager.GetAbilities(card);
        foreach (var ability in abilities)
        {
            if (ability is VenomousAbility)
                return true;
        }
        return false;
    }
}
