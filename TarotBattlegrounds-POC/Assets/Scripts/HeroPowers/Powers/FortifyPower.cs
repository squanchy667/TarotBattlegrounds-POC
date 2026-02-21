using System.Linq;
using UnityEngine;

/// <summary>
/// Pentacles hero power: Give a random friendly minion Aegis.
/// </summary>
public class FortifyPower : HeroPowerBase
{
    public override string PowerName => "Fortify";
    public override string Description => "Give a random friendly minion Aegis";
    public override int CoinCost => 2;

    public override bool CanActivate(Player owner)
    {
        if (!base.CanActivate(owner)) return false;
        // Must have at least one minion without Aegis
        return owner.board.Any(c => c.health > 0 && !c.hasAegis);
    }

    public override void Activate(Player owner)
    {
        var targets = owner.board.Where(c => c.health > 0 && !c.hasAegis).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        target.hasAegis = true;
        Debug.Log($"[Fortify] {target.cardName} gains Aegis");
    }
}
