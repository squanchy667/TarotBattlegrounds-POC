using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI difficulty levels affecting decision quality and timing.
/// </summary>
public enum AIDifficulty
{
    Easy,   // Makes suboptimal choices, slower upgrades
    Medium, // Balanced play, standard decisions
    Hard    // Optimized choices, aggressive synergy building
}

/// <summary>
/// Controls AI player decision-making during recruit phase.
/// Attach to an AI player's GameObject alongside Player component.
/// </summary>
public class AIController : MonoBehaviour
{
    [Header("Configuration")]
    public AIDifficulty difficulty = AIDifficulty.Medium;

    [Header("References")]
    [SerializeField] private Player player;

    // Decision weights by difficulty
    private static readonly Dictionary<AIDifficulty, float> UpgradeThreshold = new Dictionary<AIDifficulty, float>
    {
        { AIDifficulty.Easy, 0.3f },   // Rarely upgrades early
        { AIDifficulty.Medium, 0.5f }, // Balanced upgrade timing
        { AIDifficulty.Hard, 0.7f }    // Aggressive upgrades
    };

    private static readonly Dictionary<AIDifficulty, float> RerollChance = new Dictionary<AIDifficulty, float>
    {
        { AIDifficulty.Easy, 0.1f },   // Rarely rerolls
        { AIDifficulty.Medium, 0.3f }, // Sometimes rerolls
        { AIDifficulty.Hard, 0.5f }    // Strategic rerolls
    };

    private TavernManager tavern;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>();
    }

    private void Start()
    {
        tavern = TavernManager.Instance;
        if (player == null)
        {
            Debug.LogError($"[AIController] No Player component found on {gameObject.name}!");
        }
    }

    /// <summary>
    /// Initialize the AI controller with required references.
    /// Call this after adding the component dynamically.
    /// </summary>
    public void Initialize(Player playerRef)
    {
        player = playerRef;
        tavern = TavernManager.Instance;
        Debug.Log($"[AIController] Initialized for Player {player.playerId}");
    }

    /// <summary>
    /// Execute full AI turn. Call this during recruit phase.
    /// </summary>
    public void ExecuteTurn()
    {
        if (player == null || tavern == null)
        {
            Debug.LogError("[AIController] Cannot execute turn - missing references");
            return;
        }

        Debug.Log($"[AI Player {player.playerId}] Starting turn (Difficulty: {difficulty})");

        // AI decision order:
        // 1. Consider upgrading tavern tier
        // 2. Buy cards from shop
        // 3. Play cards from hand to board
        // 4. Consider selling weak cards
        // 5. Consider rerolling if coins left
        // 6. Optimize board positioning

        DecideUpgrade();
        BuyCards();
        PlayCardsToBoard();
        SellWeakCards();
        ConsiderReroll();
        OptimizePositioning();

        Debug.Log($"[AI Player {player.playerId}] Turn complete. Board: {player.board.Count}, Hand: {player.hand.Count}, Coins: {player.coins}");
    }

    /// <summary>
    /// Decide whether to upgrade tavern tier.
    /// </summary>
    private void DecideUpgrade()
    {
        int upgradeCost = player.GetUpgradeCost();
        int currentTier = player.currentTavernTier;

        if (currentTier >= 6) return; // Max tier

        // Calculate upgrade priority based on difficulty
        float priority = CalculateUpgradePriority(upgradeCost, currentTier);
        float threshold = UpgradeThreshold[difficulty];

        // Hard AI: Upgrade if it's efficient (cost reduced significantly)
        // Medium AI: Upgrade at standard curve
        // Easy AI: Sometimes misses upgrade windows

        bool shouldUpgrade = priority >= threshold && player.coins >= upgradeCost;

        // Easy AI has random chance to skip upgrades
        if (difficulty == AIDifficulty.Easy && Random.value < 0.3f)
            shouldUpgrade = false;

        if (shouldUpgrade)
        {
            player.UpgradeTavern();
            Debug.Log($"[AI Player {player.playerId}] Upgraded to tier {player.currentTavernTier}");
        }
    }

    private float CalculateUpgradePriority(int cost, int currentTier)
    {
        // Priority increases when:
        // - Cost is low (discounted over turns)
        // - We have excess coins
        // - Board is reasonably full

        float costFactor = 1f - (cost / 10f); // Lower cost = higher priority
        float coinFactor = player.coins >= cost + 3 ? 0.3f : 0f; // Excess coins
        float boardFactor = player.board.Count >= 4 ? 0.2f : 0f; // Board is developing

        return Mathf.Clamp01(costFactor + coinFactor + boardFactor);
    }

    /// <summary>
    /// Buy cards from the shop based on evaluation.
    /// </summary>
    private void BuyCards()
    {
        if (!tavern.availableCards.ContainsKey(player.playerId)) return;

        var shopCards = tavern.availableCards[player.playerId];

        while (player.coins >= 1 && player.hand.Count < 10 && shopCards.Count > 0)
        {
            // Evaluate all cards and pick the best
            int bestIndex = -1;
            float bestScore = -1f;

            for (int i = 0; i < shopCards.Count; i++)
            {
                float score = EvaluateCard(shopCards[i]);

                // Apply difficulty modifiers
                if (difficulty == AIDifficulty.Easy)
                    score += Random.Range(-2f, 2f); // Add randomness
                else if (difficulty == AIDifficulty.Hard)
                    score *= 1.2f; // Better at identifying good cards

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            // Only buy if card meets minimum threshold
            float minScore = difficulty == AIDifficulty.Easy ? 0f : 3f;

            if (bestIndex >= 0 && bestScore >= minScore)
            {
                Card card = shopCards[bestIndex];

                // Calculate cost with synergy reduction (centralized in SynergyManager)
                int cost = SynergyManager.Instance != null
                    ? SynergyManager.Instance.GetEffectiveCost(card, player.board)
                    : 3 + card.buyCostModifier;

                if (player.coins >= cost)
                {
                    Debug.Log($"[AI Player {player.playerId}] Buying {card.cardName} (Score: {bestScore:F1})");
                    player.BuyCard(bestIndex);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break; // No good cards to buy
            }
        }
    }

    /// <summary>
    /// Evaluate a card's value for purchasing.
    /// T215: Updated for Phase II abilities and Phase III tribes.
    /// </summary>
    private float EvaluateCard(Card card)
    {
        float score = 0f;

        // Base stats value
        score += card.attack * 1.0f;
        score += card.health * 0.8f;

        // Tier bonus (higher tier = generally better)
        score += card.tier * 1.5f;

        // Synergy bonus - check if card matches existing board tribes
        if (card.tribes != null && card.tribes.Length > 0)
        {
            foreach (var tribe in card.tribes)
            {
                if (tribe == TribeType.None) continue;

                int existingCount = CountTribeOnBoard(tribe);
                if (existingCount > 0)
                {
                    // Synergy potential - more valuable if close to threshold
                    if (existingCount == 1) score += 2f; // Will hit tier 2
                    else if (existingCount == 3) score += 3f; // Will hit tier 4
                    else if (existingCount == 5) score += 4f; // Will hit tier 6
                    else score += 1f; // Some synergy value
                }

                // Cross-tribe combo awareness (Hard AI only)
                if (difficulty == AIDifficulty.Hard)
                {
                    // Stars+Swords combo
                    if ((tribe == TribeType.Stars && CountTribeOnBoard(TribeType.Swords) >= 2) ||
                        (tribe == TribeType.Swords && CountTribeOnBoard(TribeType.Stars) >= 2))
                        score += 1.5f;

                    // Coins+Pentacles combo
                    if ((tribe == TribeType.Coins && CountTribeOnBoard(TribeType.Pentacles) >= 2) ||
                        (tribe == TribeType.Pentacles && CountTribeOnBoard(TribeType.Coins) >= 2))
                        score += 1.5f;
                }
            }

            // Multi-tribe cards are valuable
            if (card.tribes.Length > 1)
                score += 2f;
        }

        // Phase II ability evaluation (T215)
        score += EvaluateAbility(card);

        // Effect bonus (legacy system)
        if (card.effectType != Card.EffectType.NoEffect)
            score += 1.5f;

        return score;
    }

    /// <summary>
    /// Evaluate a card's ability value. Phase II abilities are scored by combat impact.
    /// </summary>
    private float EvaluateAbility(Card card)
    {
        if (card.abilityTrigger == AbilityTrigger.None && card.abilityEffect == Card.AbilityEffectType.None)
            return 0f;

        float bonus = 0f;

        // High-value keyword abilities
        switch (card.abilityEffect)
        {
            case Card.AbilityEffectType.Reborn:
                bonus += 3f; // Effectively doubles the card
                break;
            case Card.AbilityEffectType.Windfury:
                bonus += 2.5f + card.attack * 0.5f; // Scales with attack
                break;
            case Card.AbilityEffectType.Venomous:
                bonus += 3f; // Instant kill is extremely strong
                break;
            case Card.AbilityEffectType.Taunt:
                bonus += 1.5f;
                break;
            case Card.AbilityEffectType.GainArmor:
                bonus += card.abilityValue * 1f;
                break;

            // Buff abilities
            case Card.AbilityEffectType.BuffAllTribeOnPlay:
            case Card.AbilityEffectType.BuffAllTribeOnDeath:
                bonus += 2f + card.abilityValue * CountTribeOnBoard(card.tribes[0]) * 0.5f;
                break;
            case Card.AbilityEffectType.BuffAllFriendlyAttack:
            case Card.AbilityEffectType.BuffAdjacentStats:
            case Card.AbilityEffectType.BuffAdjacentAttack:
            case Card.AbilityEffectType.BuffAdjacentHealth:
                bonus += 2f + card.abilityValue * 0.5f;
                break;
            case Card.AbilityEffectType.BuffOtherFriendlyAttack:
                bonus += 2f + card.abilityValue * (player.board.Count * 0.3f);
                break;

            // Economy abilities (more valuable early game)
            case Card.AbilityEffectType.GainCoins:
            case Card.AbilityEffectType.OnSellGainCoins:
                bonus += 1.5f + card.abilityValue * 0.5f;
                break;
            case Card.AbilityEffectType.OnSellBuffAllRemaining:
                bonus += 2f + card.abilityValue * player.board.Count * 0.3f;
                break;

            // Scaling abilities
            case Card.AbilityEffectType.OnAllyDeathBuffSelf:
            case Card.AbilityEffectType.OnAllyDeathBuffRandom:
                bonus += 2.5f + card.abilityValue * 0.5f;
                break;
            case Card.AbilityEffectType.OnAllySummonedBuffSelf:
            case Card.AbilityEffectType.OnAllySummonedBuffSummoned:
                bonus += 2f;
                break;
            case Card.AbilityEffectType.OnAttackBuffSelf:
            case Card.AbilityEffectType.StealBuffOnAttack:
                bonus += 2f + card.abilityValue * 0.5f;
                break;

            // Deathrattle effects
            case Card.AbilityEffectType.DeathrattleDamageAllEnemies:
                bonus += 2f + card.abilityValue * 0.8f;
                break;
            case Card.AbilityEffectType.DeathrattleDamageRandomEnemy:
                bonus += 1.5f + card.abilityValue * 0.5f;
                break;
            case Card.AbilityEffectType.DeathrattleBuffRandomFriendly:
                bonus += 2f + card.abilityValue * 0.5f;
                break;
            case Card.AbilityEffectType.SummonTokenOnDeath:
            case Card.AbilityEffectType.SummonTokenOnPlay:
                bonus += 2f + card.abilityValue * 0.8f;
                break;
            case Card.AbilityEffectType.RandomTransformOnDeath:
                bonus += 1.5f; // Unreliable but fun
                break;

            // Aura abilities
            case Card.AbilityEffectType.AuraBuffTribematesAttack:
            case Card.AbilityEffectType.AuraBuffAdjacentStats:
            case Card.AbilityEffectType.AuraBuffAllFriendlyAttack:
                bonus += 2.5f + card.abilityValue * 0.5f;
                break;

            // Combat triggers
            case Card.AbilityEffectType.OnAttackBonusDamage:
            case Card.AbilityEffectType.OnAttackCleave:
                bonus += 2f + card.abilityValue * 0.5f;
                break;

            default:
                if (card.abilityTrigger != AbilityTrigger.None)
                    bonus += 1.5f; // Generic ability bonus
                break;
        }

        return bonus;
    }

    private int CountTribeOnBoard(TribeType tribe)
    {
        int count = 0;
        foreach (var card in player.board)
        {
            if (card.HasTribe(tribe))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Play cards from hand to board.
    /// </summary>
    private void PlayCardsToBoard()
    {
        while (player.hand.Count > 0 && player.board.Count < 7)
        {
            // Find best card to play
            int bestIndex = -1;
            float bestScore = -1f;

            for (int i = 0; i < player.hand.Count; i++)
            {
                float score = EvaluateCard(player.hand[i]);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                Card card = player.hand[bestIndex];
                int position = DetermineBestPosition(card);
                Debug.Log($"[AI Player {player.playerId}] Playing {card.cardName} to position {position}");
                player.PlayCard(bestIndex, position);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Determine the best board position for a card.
    /// </summary>
    private int DetermineBestPosition(Card card)
    {
        if (player.board.Count == 0) return 0;

        // Position strategy based on card properties:
        // - High attack, low health = place in back (right side)
        // - Guardian/Taunt = place in front (left side, position 0)
        // - Buff cards = place next to targets

        // Taunt goes to front
        if (card.effectType == Card.EffectType.Guardian)
        {
            return 0;
        }

        // Glass cannons go to back
        if (card.attack > card.health * 2)
        {
            return player.board.Count;
        }

        // Buff/support cards go in middle to maximize adjacency
        if (card.tribes != null && (card.HasTribe(TribeType.Wands) || card.HasTribe(TribeType.Cups) || card.HasTribe(TribeType.Stars)))
        {
            return player.board.Count / 2;
        }

        // Default: add to end
        return player.board.Count;
    }

    /// <summary>
    /// Sell weak cards if board is full and better options exist.
    /// </summary>
    private void SellWeakCards()
    {
        // Only sell if board is full and we have cards in hand we want to play
        if (player.board.Count < 7 || player.hand.Count == 0) return;

        // Find weakest card on board
        int worstIndex = -1;
        float worstScore = float.MaxValue;

        for (int i = 0; i < player.board.Count; i++)
        {
            float score = EvaluateCard(player.board[i]);
            if (score < worstScore)
            {
                worstScore = score;
                worstIndex = i;
            }
        }

        // Find best card in hand
        float bestHandScore = -1f;
        for (int i = 0; i < player.hand.Count; i++)
        {
            float score = EvaluateCard(player.hand[i]);
            if (score > bestHandScore)
                bestHandScore = score;
        }

        // Sell if hand card is significantly better
        float threshold = difficulty == AIDifficulty.Hard ? 2f : 4f;

        if (bestHandScore > worstScore + threshold && worstIndex >= 0)
        {
            Card toSell = player.board[worstIndex];
            Debug.Log($"[AI Player {player.playerId}] Selling weak card {toSell.cardName} (Score: {worstScore:F1})");
            player.SellCard(worstIndex);
        }
    }

    /// <summary>
    /// Consider rerolling shop if coins available and shop is weak.
    /// </summary>
    private void ConsiderReroll()
    {
        if (player.coins < 2) return; // Need coin for reroll + potential buy

        if (!tavern.availableCards.ContainsKey(player.playerId)) return;
        var shopCards = tavern.availableCards[player.playerId];

        // Evaluate current shop quality
        float avgShopScore = 0f;
        foreach (var card in shopCards)
        {
            avgShopScore += EvaluateCard(card);
        }
        avgShopScore = shopCards.Count > 0 ? avgShopScore / shopCards.Count : 0f;

        // Reroll if shop is weak and we can afford it
        float rerollThreshold = 4f + (player.currentTavernTier * 0.5f);
        bool shouldReroll = avgShopScore < rerollThreshold && Random.value < RerollChance[difficulty];

        if (shouldReroll && player.hand.Count < 10) // Don't reroll if hand is full
        {
            Debug.Log($"[AI Player {player.playerId}] Rerolling shop (Avg score: {avgShopScore:F1})");
            player.RefreshTavernShop();

            // Try to buy from new shop
            BuyCards();
        }
    }

    /// <summary>
    /// Optimize board positioning for combat.
    /// </summary>
    private void OptimizePositioning()
    {
        if (difficulty == AIDifficulty.Easy) return; // Easy AI doesn't reposition
        if (player.board.Count < 2) return;

        // Simple optimization: ensure Guardians/Taunts are at front
        for (int i = 1; i < player.board.Count; i++)
        {
            Card card = player.board[i];
            if (card.effectType == Card.EffectType.Guardian || card.abilityEffect == Card.AbilityEffectType.Taunt)
            {
                // Move to front by swapping
                Card temp = player.board[0];
                player.board[0] = card;
                player.board[i] = temp;
                Debug.Log($"[AI Player {player.playerId}] Moved Guardian {card.cardName} to front");
            }
        }
        player.NotifyBoardChanged();

        // Hard AI: Additional positioning logic
        if (difficulty == AIDifficulty.Hard)
        {
            // Move high-health units to front, glass cannons to back
            player.board.Sort((a, b) =>
            {
                // Guardians/Taunt always first (check both legacy and new ability system)
                bool aGuardian = a.effectType == Card.EffectType.Guardian || a.abilityEffect == Card.AbilityEffectType.Taunt;
                bool bGuardian = b.effectType == Card.EffectType.Guardian || b.abilityEffect == Card.AbilityEffectType.Taunt;
                if (aGuardian && !bGuardian) return -1;
                if (bGuardian && !aGuardian) return 1;

                // Then by health (tankier units front)
                return b.health.CompareTo(a.health);
            });
            player.NotifyBoardChanged();
        }
    }

    /// <summary>
    /// Get a summary of the AI's current state for debugging.
    /// </summary>
    public string GetAISummary()
    {
        if (player == null) return "No player assigned";

        string tribes = "";
        var tribeCounts = new Dictionary<TribeType, int>();
        foreach (var card in player.board)
        {
            foreach (var tribe in card.GetTribes())
            {
                if (!tribeCounts.ContainsKey(tribe)) tribeCounts[tribe] = 0;
                tribeCounts[tribe]++;
            }
        }
        foreach (var kvp in tribeCounts)
        {
            tribes += $"{kvp.Key}:{kvp.Value} ";
        }

        return $"AI Player {player.playerId} ({difficulty}) - Tier:{player.currentTavernTier} Board:{player.board.Count} Tribes:[{tribes.Trim()}]";
    }
}
