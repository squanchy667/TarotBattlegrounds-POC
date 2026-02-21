using UnityEngine;

/// <summary>
/// GainArmor ability (T110) - passive keyword that reduces incoming damage.
/// When this card takes damage, the damage is reduced by the armor value (minimum 1 damage).
/// The actual damage reduction is handled in CombatManager.
/// This ability sets the armor value on the card.
/// </summary>
public class GainArmorAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.None;

    private int _armorValue;
    private string _description;

    public override string Description => _description;

    public GainArmorAbility(int armorValue)
    {
        _armorValue = armorValue;
        _description = $"Armor {_armorValue}: Reduces incoming damage by {_armorValue}";
    }

    protected override void ExecuteEffect(AbilityContext context)
    {
        // Armor is passive - no active effect to execute
        Debug.Log($"[Armor] {context.SourceCard?.cardName} has Armor {_armorValue} (passive effect)");
    }

    /// <summary>
    /// Get the armor value for a card. Returns 0 if no armor.
    /// </summary>
    public static int GetArmor(Card card)
    {
        if (card == null) return 0;
        if (card.armor > 0) return card.armor;

        var abilities = AbilityManager.GetAbilities(card);
        foreach (var ability in abilities)
        {
            if (ability is GainArmorAbility armorAbility)
                return armorAbility._armorValue;
        }
        return 0;
    }

    /// <summary>
    /// Apply armor damage reduction. Returns the reduced damage amount.
    /// </summary>
    public static int ApplyArmor(Card card, int incomingDamage)
    {
        int armor = GetArmor(card);
        if (armor <= 0) return incomingDamage;

        int reduced = Mathf.Max(1, incomingDamage - armor);
        if (reduced < incomingDamage)
            Debug.Log($"[Armor] {card.cardName}'s armor reduces damage from {incomingDamage} to {reduced}");
        return reduced;
    }
}
