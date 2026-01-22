using System.Collections.Generic;

/// <summary>
/// Context passed to abilities when they execute.
/// Contains all information an ability might need to resolve its effect.
/// </summary>
public class AbilityContext
{
    /// <summary>The card that has this ability</summary>
    public Card SourceCard { get; set; }

    /// <summary>The player who owns the source card</summary>
    public Player Owner { get; set; }

    /// <summary>Target card (for targeted abilities like OnAttack)</summary>
    public Card TargetCard { get; set; }

    /// <summary>Owner's board state</summary>
    public List<Card> OwnerBoard { get; set; }

    /// <summary>Enemy board state (during combat)</summary>
    public List<Card> EnemyBoard { get; set; }

    /// <summary>Damage dealt (for OnDamaged trigger)</summary>
    public int DamageDealt { get; set; }
}

/// <summary>
/// Interface for all card abilities in the game.
/// Abilities are triggered at specific times (defined by AbilityTrigger)
/// and execute effects that modify game state.
/// </summary>
public interface IAbility
{
    /// <summary>When this ability triggers</summary>
    AbilityTrigger Trigger { get; }

    /// <summary>Human-readable description of what this ability does</summary>
    string Description { get; }

    /// <summary>
    /// Execute the ability effect.
    /// </summary>
    /// <param name="context">Context containing source card, owner, targets, and board state</param>
    void Execute(AbilityContext context);

    /// <summary>
    /// Check if this ability can execute given the current context.
    /// </summary>
    /// <param name="context">Context to validate against</param>
    /// <returns>True if ability can execute</returns>
    bool CanExecute(AbilityContext context);
}
