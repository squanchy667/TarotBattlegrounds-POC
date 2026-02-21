using System.Linq;
using UnityEngine;

/// <summary>
/// Neutral passive hero power: At combat start, a random friendly minion gains +2/+2.
/// </summary>
public class TacticianPower : HeroPowerBase
{
    public override string PowerName => "Tactician";
    public override string Description => "Passive: A random friendly minion starts combat with +2/+2";
    public override bool IsPassive => true;

    public override void Activate(Player owner) { }

    public override void OnCombatStart(Player owner)
    {
        var targets = owner.board.Where(c => c.health > 0).ToList();
        if (targets.Count == 0) return;

        Card target = targets[Random.Range(0, targets.Count)];
        target.attack += 2;
        target.health += 2;
        Debug.Log($"[Tactician] {target.cardName} gains +2/+2 at combat start ({target.attack}/{target.health})");
    }
}
