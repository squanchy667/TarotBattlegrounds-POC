using System.Linq;
using UnityEngine;

/// <summary>
/// Swords passive hero power: At combat start, your Swords minions get +1 Attack.
/// </summary>
public class WarChiefPower : HeroPowerBase
{
    public override string PowerName => "The Emperor";
    public override string Description => "Passive: Your Swords minions have +1 Attack at combat start";
    public override bool IsPassive => true;

    public override void Activate(Player owner) { }

    public override void OnCombatStart(Player owner, Player opponent)
    {
        int buffed = 0;
        foreach (var card in owner.board)
        {
            if (card.health > 0 && card.HasTribe(TribeType.Swords))
            {
                card.attack += 1;
                buffed++;
            }
        }
        if (buffed > 0)
            Debug.Log($"[War Chief] Buffed {buffed} Swords minions with +1 Attack");
    }
}
