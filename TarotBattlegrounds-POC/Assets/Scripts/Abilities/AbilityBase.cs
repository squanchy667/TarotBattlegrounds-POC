using UnityEngine;

/// <summary>
/// Base class for abilities providing common functionality.
/// Inherit from this to create specific ability implementations.
/// </summary>
public abstract class AbilityBase : IAbility
{
    public abstract AbilityTrigger Trigger { get; }
    public abstract string Description { get; }

    /// <summary>
    /// Execute the ability. Override ExecuteEffect() to implement specific logic.
    /// </summary>
    public void Execute(AbilityContext context)
    {
        if (!CanExecute(context))
        {
            Debug.LogWarning($"Ability {GetType().Name} cannot execute - CanExecute returned false");
            return;
        }

        Debug.Log($"[Ability] {context.SourceCard?.cardName} triggers {GetType().Name}: {Description}");
        ExecuteEffect(context);
    }

    /// <summary>
    /// Override this to implement the specific ability effect.
    /// </summary>
    protected abstract void ExecuteEffect(AbilityContext context);

    /// <summary>
    /// Default implementation: ability can execute if source card exists.
    /// Override for abilities with additional requirements.
    /// </summary>
    public virtual bool CanExecute(AbilityContext context)
    {
        return context?.SourceCard != null;
    }
}

/// <summary>
/// Common stat modification effects that abilities can use.
/// </summary>
public static class AbilityEffects
{
    /// <summary>Buff a card's attack</summary>
    public static void BuffAttack(Card card, int amount)
    {
        if (card == null) return;
        card.attack += amount;
        Debug.Log($"[AbilityEffect] {card.cardName} gains +{amount} Attack (now {card.attack})");
    }

    /// <summary>Buff a card's health</summary>
    public static void BuffHealth(Card card, int amount)
    {
        if (card == null) return;
        card.health += amount;
        Debug.Log($"[AbilityEffect] {card.cardName} gains +{amount} Health (now {card.health})");
    }

    /// <summary>Buff both attack and health</summary>
    public static void BuffStats(Card card, int attack, int health)
    {
        BuffAttack(card, attack);
        BuffHealth(card, health);
    }

    /// <summary>Deal damage to a card</summary>
    public static void DealDamage(Card card, int damage)
    {
        if (card == null) return;
        card.health -= damage;
        Debug.Log($"[AbilityEffect] {card.cardName} takes {damage} damage (now {card.health} HP)");
    }

    /// <summary>Grant Aegis (divine shield) to a card</summary>
    public static void GrantAegis(Card card)
    {
        if (card == null) return;
        card.hasAegis = true;
        Debug.Log($"[AbilityEffect] {card.cardName} gains Aegis");
    }
}
