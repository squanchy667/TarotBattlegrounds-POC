/// <summary>
/// Defines who is affected by synergy effects.
/// </summary>
public enum SynergyTarget
{
    AllTribeMembers,    // All cards of the synergy's tribe
    AllFriendly,        // All friendly cards on board
    Adjacent,           // Cards adjacent to tribe members
    Random,             // Random friendly card(s)
    Self                // The card triggering the effect (if applicable)
}
