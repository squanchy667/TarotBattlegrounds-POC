using UnityEngine;

/// <summary>
/// Reborn ability (T105) - passive keyword that revives the card with 1 HP after death.
/// The actual revival logic is handled in CombatManager.ProcessDeaths().
/// This ability class sets the hasReborn flag on registration.
/// </summary>
public class RebornAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.None;
    public override string Description => "Reborn: Revives with 1 Health after first death";

    protected override void ExecuteEffect(AbilityContext context)
    {
        // Reborn is passive - no active effect to execute
        Debug.Log($"[Reborn] {context.SourceCard?.cardName} has Reborn (passive effect)");
    }

    /// <summary>
    /// Check if a card has Reborn via the ability system or passive flag.
    /// </summary>
    public static bool HasReborn(Card card)
    {
        if (card == null) return false;
        if (card.hasReborn) return true;

        var abilities = AbilityManager.GetAbilities(card);
        foreach (var ability in abilities)
        {
            if (ability is RebornAbility)
                return true;
        }
        return false;
    }
}
