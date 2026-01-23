/// <summary>
/// Defines what synergy effects do when activated.
/// </summary>
public enum SynergyEffect
{
    // Stat buffs
    BuffAttack,     // Increase attack stat
    BuffHealth,     // Increase health stat
    BuffStats,      // Increase both attack and health

    // Economy
    BonusGold,      // Gain extra gold
    ReduceCost,     // Reduce cost of cards

    // Combat - Offensive
    BonusDamage,    // Deal extra damage on attack
    Piercing,       // Damage ignores some defense/aegis
    Cleave,         // Damage adjacent enemies

    // Combat - Defensive
    HealFlat,       // Heal a flat amount
    HealPercent,    // Heal a percentage of max health
    Shield,         // Grant temporary damage absorption (aegis-like)

    // Utility
    ExtraCardDraw,  // Draw additional cards to shop
    Discover        // Choose from multiple options
}
