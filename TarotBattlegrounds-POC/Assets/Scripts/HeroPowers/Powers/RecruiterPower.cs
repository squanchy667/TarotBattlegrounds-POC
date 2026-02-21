using UnityEngine;

/// <summary>
/// Neutral hero power: Add a random Tier 1 card to your hand.
/// </summary>
public class RecruiterPower : HeroPowerBase
{
    public override string PowerName => "Recruiter";
    public override string Description => "Add a random Tier 1 card to your hand";
    public override int CoinCost => 3;

    public override bool CanActivate(Player owner)
    {
        return base.CanActivate(owner) && owner.hand.Count < 10;
    }

    public override void Activate(Player owner)
    {
        if (TavernManager.Instance == null)
        {
            Debug.LogWarning("[Recruiter] No TavernManager available");
            return;
        }

        var pool = TavernManager.Instance.GetFullPool();
        var tier1Cards = new System.Collections.Generic.List<Card>();
        foreach (var card in pool)
        {
            if (card.tier <= 1)
                tier1Cards.Add(card);
        }

        if (tier1Cards.Count == 0)
        {
            Debug.Log("[Recruiter] No Tier 1 cards in pool");
            return;
        }

        Card picked = tier1Cards[Random.Range(0, tier1Cards.Count)];
        Card copy = picked.Clone();
        owner.hand.Add(copy);
        pool.Remove(picked); // Remove from pool (card is reserved)
        Debug.Log($"[Recruiter] Added {copy.cardName} to hand");
    }
}
