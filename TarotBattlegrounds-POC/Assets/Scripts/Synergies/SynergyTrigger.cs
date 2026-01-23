/// <summary>
/// Defines when synergy effects activate.
/// </summary>
public enum SynergyTrigger
{
    Passive,        // Always active while threshold is met
    StartOfCombat,  // Triggers at the start of combat phase
    EndOfCombat,    // Triggers at the end of combat phase
    OnSell,         // Triggers when a tribe member is sold
    OnBuy,          // Triggers when a tribe member is bought
    OnDeath,        // Triggers when a tribe member dies in combat
    EndOfTurn       // Triggers at the end of recruit phase
}
