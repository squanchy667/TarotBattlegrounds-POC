using System.Linq;
using UnityEngine;

/// <summary>
/// Cups hero power: Restore a random friendly minion to full health.
/// </summary>
public class HealerPower : HeroPowerBase
{
    public override string PowerName => "Healer";
    public override string Description => "Restore a random friendly minion to full health";
    public override int CoinCost => 1;

    public override void Activate(Player owner)
    {
        // Flat +3 health to a random friendly minion (since Card lacks maxHealth tracking)
        var targets = owner.board.Where(c => c.health > 0).ToList();
        if (targets.Count == 0)
        {
            Debug.Log("[Healer] No minions to heal");
            return;
        }

        Card target = targets[Random.Range(0, targets.Count)];
        target.health += 3;
        Debug.Log($"[Healer] {target.cardName} gains +3 Health (now {target.health})");
    }
    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.board.Count > 0;
    }
}
