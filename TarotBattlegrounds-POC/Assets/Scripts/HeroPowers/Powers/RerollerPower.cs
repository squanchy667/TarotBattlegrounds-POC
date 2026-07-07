using UnityEngine;

/// <summary>
/// Neutral hero power: Refresh the shop for free (costs 1 coin to activate,
/// but saves 1 coin on refresh = net free reroll).
/// </summary>
public class RerollerPower : HeroPowerBase
{
    public override string PowerName => "The Fool";
    public override string Description => "Refresh your shop (costs 1, saves the 1-coin refresh)";
    public override int CoinCost => 1;

    public override void Activate(Player owner)
    {
        if (TavernManager.Instance == null)
        {
            Debug.LogWarning("[Reroller] No TavernManager available");
            return;
        }

        // Refresh shop without spending the normal 1-coin refresh cost
        TavernManager.Instance.RefreshPlayerShop(owner.playerId, owner.currentTavernTier);
        Debug.Log($"[Reroller] Player {owner.playerId} got a free shop refresh");
    }
}
