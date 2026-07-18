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
    /// Whether this card will still Reborn on its next death.
    /// Consumable: only <see cref="Card.hasReborn"/> counts.
    /// Do NOT treat a leftover <see cref="RebornAbility"/> registration as active —
    /// ProcessDeaths clears the flag after one revive; if we also keyed off the
    /// ability type, Shooting Star etc. would reborn forever (infinite combat).
    /// </summary>
    public static bool HasReborn(Card card)
    {
        if (card == null) return false;
        return card.hasReborn;
    }

    /// <summary>
    /// Spend the reborn keyword after a successful revive (flag + ability entry).
    /// </summary>
    public static void ConsumeReborn(Card card)
    {
        if (card == null) return;
        card.hasReborn = false;
        AbilityManager.UnregisterAbilityOfType(card, typeof(RebornAbility));
    }
}
