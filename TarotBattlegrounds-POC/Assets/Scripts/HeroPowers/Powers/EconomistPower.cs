using UnityEngine;

/// <summary>
/// Neutral hero power: Gain 1 coin (effectively a free refresh).
/// </summary>
public class EconomistPower : HeroPowerBase
{
    public override string PowerName => "Economist";
    public override string Description => "Gain 1 coin (use to offset refresh cost)";
    public override int CoinCost => 1;

    public override void Activate(Player owner)
    {
        // Net effect: spend 1 coin, gain 1 coin = free, but uses the hero power slot
        // The real value is in the action economy — this costs 1 but you get 1 back
        owner.coins += 1;
        Debug.Log($"[Economist] Player {owner.playerId} gains 1 coin (coins: {owner.coins})");
    }
}
