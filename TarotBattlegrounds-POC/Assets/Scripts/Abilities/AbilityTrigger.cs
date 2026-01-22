/// <summary>
/// Defines when an ability triggers during gameplay.
/// Part of the generic ability framework for the core engine.
/// </summary>
public enum AbilityTrigger
{
    /// <summary>No ability trigger</summary>
    None,

    /// <summary>When played from hand to board</summary>
    Battlecry,

    /// <summary>When this card dies</summary>
    Deathrattle,

    /// <summary>When this card attacks</summary>
    OnAttack,

    /// <summary>When this card takes damage</summary>
    OnDamaged,

    /// <summary>Before combat begins</summary>
    StartOfCombat,

    /// <summary>At end of recruit phase</summary>
    EndOfTurn
}
