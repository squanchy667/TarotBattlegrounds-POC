using UnityEngine;

/// <summary>
/// Pentacles hero power: Gain 1 extra coin this turn.
/// </summary>
public class MidasTouchPower : HeroPowerBase
{
    public override string PowerName => "The Empress";
    public override string Description => "Gain 1 coin (once per turn)";
    public override int CoinCost => 0;

    public override void Activate(Player owner)
    {
        owner.coins += 1;
        Debug.Log($"[Midas Touch] Player {owner.playerId} gains 1 coin (coins: {owner.coins})");
    }
}
