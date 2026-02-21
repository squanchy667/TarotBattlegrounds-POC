using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// RandomTransform ability (T112) - deathrattle that replaces the dead card
/// with a random card from the tavern pool (same or lower tier).
/// The new card appears at the same board position.
/// </summary>
public class RandomTransformAbility : AbilityBase
{
    public override AbilityTrigger Trigger => AbilityTrigger.Deathrattle;
    public override string Description => "Deathrattle: Transform into a random card";

    protected override void ExecuteEffect(AbilityContext context)
    {
        if (context.OwnerBoard == null || context.SourceCard == null) return;
        // Count only alive cards (dead cards may still be in list during deathrattle processing)
        int aliveCount = 0;
        foreach (var c in context.OwnerBoard)
            if (c.health > 0) aliveCount++;
        if (aliveCount >= 7)
        {
            Debug.Log($"[RandomTransform] Board full, cannot transform for {context.SourceCard.cardName}");
            return;
        }

        // Get a random card from the tavern pool
        Card randomCard = GetRandomCard(context.SourceCard.tier);
        if (randomCard == null)
        {
            Debug.Log($"[RandomTransform] No cards available for transform");
            return;
        }

        // Insert at the source card's position (or end of board)
        int insertIndex = context.OwnerBoard.IndexOf(context.SourceCard);
        if (insertIndex < 0) insertIndex = context.OwnerBoard.Count;

        Card newCard = randomCard.Clone();
        context.OwnerBoard.Insert(insertIndex, newCard);

        Debug.Log($"[RandomTransform] {context.SourceCard.cardName} transforms into {newCard.cardName} ({newCard.attack}/{newCard.health})");
    }

    private Card GetRandomCard(int maxTier)
    {
        // Try to get from tavern pool
        if (TavernManager.Instance != null)
        {
            var pool = TavernManager.Instance.GetFullPool();
            var candidates = pool.Where(c => c.tier <= maxTier).ToList();
            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];
        }

        // Fallback: create a simple token if no pool available
        Card token = ScriptableObject.CreateInstance<Card>();
        token.cardName = "Transformed Card";
        token.tier = 1;
        token.attack = 2;
        token.health = 2;
        token.tribes = new TribeType[0];
        return token;
    }
}
