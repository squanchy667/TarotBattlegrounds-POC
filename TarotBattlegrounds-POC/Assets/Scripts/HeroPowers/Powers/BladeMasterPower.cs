using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Swords hero power: Deal 1 damage to a random enemy minion (combat start)
/// and give a random friendly minion +1 Attack.
/// </summary>
public class BladeMasterPower : HeroPowerBase
{
    public override string PowerName => "The Chariot";
    public override string Description => "Give a random friendly minion +1 Attack";
    public override int CoinCost => 2;

    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.board.Any(c => c.health > 0);
    }

    public override void Activate(Player owner)
    {
        var targets = owner.board.Where(c => c.health > 0).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        target.attack += 1;
        Debug.Log($"[Blade Master] {target.cardName} gains +1 Attack (now {target.attack})");
    }
}
