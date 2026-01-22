using UnityEngine;

/// <summary>
/// Taunt ability - forces enemies to attack this card first.
/// Note: The actual taunt logic is handled in CombatManager (Guardian check).
/// This ability class provides a standardized way to mark cards as having Taunt.
/// </summary>
public class TauntAbility : AbilityBase
{
    // Taunt doesn't have a trigger - it's a passive effect checked during combat
    public override AbilityTrigger Trigger => AbilityTrigger.None;

    public override string Description => "Taunt: Enemies must attack this card first";

    /// <summary>
    /// Check if a card has Taunt ability registered.
    /// </summary>
    public static bool HasTaunt(Card card)
    {
        var abilities = AbilityManager.GetAbilities(card);
        foreach (var ability in abilities)
        {
            if (ability is TauntAbility)
                return true;
        }

        // Also check legacy Guardian effect type
        return card.effectType == Card.EffectType.Guardian;
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        // Taunt is passive - no active effect to execute
        Debug.Log($"[Taunt] {context.SourceCard?.cardName} has Taunt (passive effect)");
    }
}
