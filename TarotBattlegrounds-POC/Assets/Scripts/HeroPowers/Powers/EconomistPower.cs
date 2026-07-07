using UnityEngine;

/// <summary>
/// Neutral hero power: Gain 2 coins for 1 (net +1 gold per turn).
/// </summary>
public class EconomistPower : HeroPowerBase
{
    public override string PowerName => "Wheel of Fortune";
    public override string Description => "Gain 2 coins";
    public override int CoinCost => 1;

    public override void Activate(Player owner)
    {
        owner.coins += 2;
        Debug.Log($"[Economist] Player {owner.playerId} gains 2 coins (coins: {owner.coins})");
    }
}
