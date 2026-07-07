using UnityEngine;

/// <summary>
/// Cups hero power: Take 2 damage and gain 1 coin.
/// </summary>
public class LifeTapPower : HeroPowerBase
{
    public override string PowerName => "The Hanged Man";
    public override string Description => "Take 2 damage, gain 1 coin";
    public override int CoinCost => 0;

    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.Health > 2; // Don't kill yourself
    }

    public override void Activate(Player owner)
    {
        owner.Health -= 2;
        owner.coins += 1;
        Debug.Log($"[Life Tap] Player {owner.playerId} takes 2 damage (HP: {owner.Health}), gains 1 coin (coins: {owner.coins})");
    }
}
