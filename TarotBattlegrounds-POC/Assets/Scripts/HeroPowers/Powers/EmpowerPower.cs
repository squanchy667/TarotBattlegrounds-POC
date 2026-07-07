using UnityEngine;

/// <summary>
/// Wands hero power: Give all friendly minions +1 Health.
/// </summary>
public class EmpowerPower : HeroPowerBase
{
    public override string PowerName => "The Magician";
    public override string Description => "Give all friendly minions +1 Health";
    public override int CoinCost => 2;

    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.board.Count > 0;
    }

    public override void Activate(Player owner)
    {
        int buffed = 0;
        foreach (var card in owner.board)
        {
            if (card.health > 0)
            {
                card.health += 1;
                buffed++;
            }
        }
        Debug.Log($"[Empower] Gave +1 Health to {buffed} minions");
    }
}
