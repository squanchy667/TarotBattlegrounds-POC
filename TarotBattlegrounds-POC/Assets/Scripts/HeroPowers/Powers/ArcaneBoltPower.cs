using System.Linq;
using UnityEngine;

/// <summary>
/// Wands hero power: Deal 3 damage to a random enemy minion at combat start.
/// </summary>
public class ArcaneBoltPower : HeroPowerBase
{
    public override string PowerName => "Arcane Bolt";
    public override string Description => "At combat start, deal 3 damage to a random enemy";
    public override int CoinCost => 2;

    public override void Activate(Player owner)
    {
        // Give all friendly minions +1 Attack as a combat prep buff
        var targets = owner.board.Where(c => c.health > 0).ToList();
        if (targets.Count == 0)
        {
            Debug.Log("[Arcane Bolt] No minions to buff");
            return;
        }
        foreach (var card in targets)
            card.attack += 1;
        Debug.Log($"[Arcane Bolt] All {targets.Count} friendly minions gain +1 Attack");
    }

    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.board.Any(c => c.health > 0);
    }
}
