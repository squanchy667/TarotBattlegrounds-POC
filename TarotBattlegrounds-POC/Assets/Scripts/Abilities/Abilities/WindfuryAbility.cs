using UnityEngine;

/// <summary>
/// Windfury ability (T106) - passive keyword that allows a card to attack twice per combat turn.
/// The actual double-attack logic is handled in CombatManager's attack loop.
/// This ability class sets the hasWindfury flag on registration.
/// </summary>
public class WindfuryAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.None;
    public override string Description => "Windfury: Attacks twice per turn";

    protected override void ExecuteEffect(AbilityContext context)
    {
        // Windfury is passive - no active effect to execute
        Debug.Log($"[Windfury] {context.SourceCard?.cardName} has Windfury (passive effect)");
    }

    /// <summary>
    /// Check if a card has Windfury via the ability system or passive flag.
    /// </summary>
    public static bool HasWindfury(Card card)
    {
        if (card == null) return false;
        if (card.hasWindfury) return true;

        var abilities = AbilityManager.GetAbilities(card);
        foreach (var ability in abilities)
        {
            if (ability is WindfuryAbility)
                return true;
        }
        return false;
    }
}
