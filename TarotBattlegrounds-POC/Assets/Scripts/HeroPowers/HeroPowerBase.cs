using UnityEngine;

/// <summary>
/// Base class for all hero powers (T113).
/// Each player selects one hero power at game start.
/// Active powers cost coins and can be used once per turn.
/// Passive powers trigger automatically.
/// </summary>
public abstract class HeroPowerBase
{
    public abstract string PowerName { get; }
    public abstract string Description { get; }
    public virtual int CoinCost => 2;
    public virtual bool IsPassive => false;

    /// <summary>True if this power was used this recruit phase.</summary>
    public bool UsedThisTurn { get; set; }

    /// <summary>Execute the hero power effect.</summary>
    public abstract void Activate(Player owner);

    /// <summary>Check if the hero power can be activated.</summary>
    public virtual bool CanActivate(Player owner)
    {
        if (owner == null) return false;
        if (IsPassive) return false; // Passive powers don't activate manually
        if (UsedThisTurn) return false;
        if (owner.coins < CoinCost) return false;
        return true;
    }

    /// <summary>Reset for new recruit phase.</summary>
    public void ResetForNewTurn()
    {
        UsedThisTurn = false;
    }

    /// <summary>Called at start of combat for passive powers.</summary>
    public virtual void OnCombatStart(Player owner) { }

    /// <summary>Called at start of recruit phase for passive powers.</summary>
    public virtual void OnRecruitStart(Player owner) { }
}
