using UnityEngine;

/// <summary>
/// Neutral hero power: Add a random card of your current tavern tier to your hand.
/// Scales with the game — early on gives T1 cards, late game gives T5-T6 cards.
/// </summary>
public class RecruiterPower : HeroPowerBase
{
    public override string PowerName => "Recruiter";
    public override string Description => "Add a random card of your tavern tier to your hand";
    public override int CoinCost => 2;

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
        int targetTier = owner.currentTavernTier;
        var tierCards = new System.Collections.Generic.List<Card>();
        foreach (var card in pool)
        {
            if (card.tier == targetTier)
                tierCards.Add(card);
        }

        // Fallback to any available card at or below current tier
        if (tierCards.Count == 0)
        {
            foreach (var card in pool)
            {
                if (card.tier <= targetTier)
                    tierCards.Add(card);
            }
        }

        if (tierCards.Count == 0)
        {
            Debug.Log($"[Recruiter] No tier {targetTier} cards in pool");
            return;
        }

        Card picked = tierCards[Random.Range(0, tierCards.Count)];
        Card copy = picked.Clone();
        owner.hand.Add(copy);
        TavernManager.Instance.RemoveCardFromPool(picked);
        Debug.Log($"[Recruiter] Added {copy.cardName} (Tier {copy.tier}) to hand");
    }
}
