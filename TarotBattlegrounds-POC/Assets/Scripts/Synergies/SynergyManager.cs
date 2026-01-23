using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages tribe synergies, counting tribe members and applying bonuses.
/// Attach to a GameObject in the scene and assign TribeSynergy ScriptableObjects.
/// </summary>
public class SynergyManager : MonoBehaviour
{
    public static SynergyManager Instance { get; private set; }

    [Header("Tribe Synergy Definitions")]
    [Tooltip("Assign all TribeSynergy ScriptableObjects here")]
    public TribeSynergy[] tribeSynergies;

    // Cache synergies by tribe type for quick lookup
    private Dictionary<TribeType, TribeSynergy> _synergyByTribe = new Dictionary<TribeType, TribeSynergy>();

    // Current tribe counts (updated when board changes)
    private Dictionary<TribeType, int> _tribeCounts = new Dictionary<TribeType, int>();

    // Active synergy tiers
    private Dictionary<TribeType, SynergyTier> _activeTiers = new Dictionary<TribeType, SynergyTier>();

    // Active cross-tribe combos
    private List<(TribeType, TribeType)> _activeCombos = new List<(TribeType, TribeType)>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSynergyCache();
    }

    private void InitializeSynergyCache()
    {
        _synergyByTribe.Clear();
        if (tribeSynergies == null) return;

        foreach (var synergy in tribeSynergies)
        {
            if (synergy != null && synergy.tribe != TribeType.None)
            {
                _synergyByTribe[synergy.tribe] = synergy;
            }
        }
        Debug.Log($"[SynergyManager] Initialized with {_synergyByTribe.Count} tribe synergies");
    }

    /// <summary>
    /// Count all tribes on a player's board and update active synergies.
    /// Call this whenever the board changes.
    /// </summary>
    public void UpdateTribeCounts(List<Card> board)
    {
        _tribeCounts.Clear();
        _activeTiers.Clear();
        _activeCombos.Clear();

        if (board == null || board.Count == 0)
        {
            Debug.Log("[SynergyManager] Board empty, no synergies active");
            return;
        }

        // Count tribes (cards can have multiple tribes)
        foreach (var card in board)
        {
            if (card.tribes == null) continue;
            foreach (var tribe in card.tribes)
            {
                if (tribe == TribeType.None) continue;
                if (!_tribeCounts.ContainsKey(tribe))
                    _tribeCounts[tribe] = 0;
                _tribeCounts[tribe]++;
            }
        }

        // Determine active tiers
        foreach (var kvp in _tribeCounts)
        {
            TribeType tribe = kvp.Key;
            int count = kvp.Value;

            if (_synergyByTribe.TryGetValue(tribe, out TribeSynergy synergy))
            {
                SynergyTier activeTier = synergy.GetActiveTier(count);
                if (activeTier != null)
                {
                    _activeTiers[tribe] = activeTier;
                    Debug.Log($"[SynergyManager] {tribe} tier active: {count} members, threshold {activeTier.threshold}");
                }
            }
        }

        // Check cross-tribe combos
        CheckCombos();

        LogActiveSynergies();
    }

    private void CheckCombos()
    {
        foreach (var synergy in _synergyByTribe.Values)
        {
            if (synergy.comboTribe == TribeType.None) continue;

            int thisTribeCount = GetTribeCount(synergy.tribe);
            int partnerCount = GetTribeCount(synergy.comboTribe);

            if (synergy.IsComboActive(thisTribeCount, partnerCount))
            {
                // Avoid duplicate combos (A+B and B+A)
                var combo = synergy.tribe < synergy.comboTribe
                    ? (synergy.tribe, synergy.comboTribe)
                    : (synergy.comboTribe, synergy.tribe);

                if (!_activeCombos.Contains(combo))
                {
                    _activeCombos.Add(combo);
                    Debug.Log($"[SynergyManager] Combo active: {synergy.tribe} + {synergy.comboTribe}");
                }
            }
        }
    }

    private void LogActiveSynergies()
    {
        if (_activeTiers.Count == 0 && _activeCombos.Count == 0)
        {
            Debug.Log("[SynergyManager] No synergies active");
            return;
        }

        string log = "[SynergyManager] Active synergies:\n";
        foreach (var kvp in _activeTiers)
        {
            log += $"  - {kvp.Key}: Tier {kvp.Value.threshold} ({kvp.Value.effect} +{kvp.Value.value})\n";
        }
        foreach (var combo in _activeCombos)
        {
            log += $"  - Combo: {combo.Item1} + {combo.Item2}\n";
        }
        Debug.Log(log.TrimEnd());
    }

    /// <summary>
    /// Get the count of a specific tribe on the board.
    /// </summary>
    public int GetTribeCount(TribeType tribe)
    {
        return _tribeCounts.TryGetValue(tribe, out int count) ? count : 0;
    }

    /// <summary>
    /// Get all current tribe counts.
    /// </summary>
    public Dictionary<TribeType, int> GetAllTribeCounts()
    {
        return new Dictionary<TribeType, int>(_tribeCounts);
    }

    /// <summary>
    /// Get the active tier for a tribe, or null if no tier is active.
    /// </summary>
    public SynergyTier GetActiveTier(TribeType tribe)
    {
        return _activeTiers.TryGetValue(tribe, out SynergyTier tier) ? tier : null;
    }

    /// <summary>
    /// Get all active tiers.
    /// </summary>
    public Dictionary<TribeType, SynergyTier> GetAllActiveTiers()
    {
        return new Dictionary<TribeType, SynergyTier>(_activeTiers);
    }

    /// <summary>
    /// Check if a specific combo is active.
    /// </summary>
    public bool IsComboActive(TribeType tribe1, TribeType tribe2)
    {
        var combo = tribe1 < tribe2 ? (tribe1, tribe2) : (tribe2, tribe1);
        return _activeCombos.Contains(combo);
    }

    /// <summary>
    /// Get all active combos.
    /// </summary>
    public List<(TribeType, TribeType)> GetActiveCombos()
    {
        return new List<(TribeType, TribeType)>(_activeCombos);
    }

    /// <summary>
    /// Get the TribeSynergy definition for a tribe.
    /// </summary>
    public TribeSynergy GetTribeSynergy(TribeType tribe)
    {
        return _synergyByTribe.TryGetValue(tribe, out TribeSynergy synergy) ? synergy : null;
    }

    /// <summary>
    /// Trigger all synergies that match a specific trigger type.
    /// </summary>
    public void TriggerSynergies(SynergyTrigger trigger, List<Card> board, Player owner)
    {
        Debug.Log($"[SynergyManager] Triggering {trigger} synergies");

        foreach (var kvp in _activeTiers)
        {
            TribeType tribe = kvp.Key;
            SynergyTier tier = kvp.Value;

            if (tier.trigger == trigger)
            {
                ApplySynergyEffect(tribe, tier, board, owner);
            }
        }

        // Apply combo effects (combos are typically passive or StartOfCombat)
        if (trigger == SynergyTrigger.Passive || trigger == SynergyTrigger.StartOfCombat)
        {
            ApplyComboEffects(board, owner);
        }
    }

    private void ApplySynergyEffect(TribeType tribe, SynergyTier tier, List<Card> board, Player owner)
    {
        List<Card> targets = GetTargets(tribe, tier.target, board);
        Debug.Log($"[SynergyManager] Applying {tribe} synergy: {tier.effect} +{tier.value} to {targets.Count} targets");

        foreach (var target in targets)
        {
            ApplyEffect(tier.effect, tier.value, target, owner);
        }
    }

    private void ApplyComboEffects(List<Card> board, Player owner)
    {
        foreach (var combo in _activeCombos)
        {
            TribeSynergy synergy1 = GetTribeSynergy(combo.Item1);
            TribeSynergy synergy2 = GetTribeSynergy(combo.Item2);

            // Apply combo from synergy1 if it references synergy2
            if (synergy1 != null && synergy1.comboTribe == combo.Item2)
            {
                Debug.Log($"[SynergyManager] Applying combo effect: {synergy1.comboEffect} +{synergy1.comboValue}");
                // Combo effects typically apply to all friendly
                foreach (var card in board)
                {
                    ApplyEffect(synergy1.comboEffect, synergy1.comboValue, card, owner);
                }
            }
        }
    }

    private List<Card> GetTargets(TribeType tribe, SynergyTarget targetType, List<Card> board)
    {
        switch (targetType)
        {
            case SynergyTarget.AllTribeMembers:
                return board.Where(c => c.HasTribe(tribe)).ToList();

            case SynergyTarget.AllFriendly:
                return new List<Card>(board);

            case SynergyTarget.Adjacent:
                // Get cards adjacent to any tribe member
                List<Card> adjacent = new List<Card>();
                for (int i = 0; i < board.Count; i++)
                {
                    if (board[i].HasTribe(tribe))
                    {
                        if (i > 0 && !adjacent.Contains(board[i - 1]))
                            adjacent.Add(board[i - 1]);
                        if (i < board.Count - 1 && !adjacent.Contains(board[i + 1]))
                            adjacent.Add(board[i + 1]);
                    }
                }
                return adjacent;

            case SynergyTarget.Random:
                if (board.Count == 0) return new List<Card>();
                return new List<Card> { board[Random.Range(0, board.Count)] };

            default:
                return new List<Card>();
        }
    }

    private void ApplyEffect(SynergyEffect effect, int value, Card target, Player owner)
    {
        switch (effect)
        {
            case SynergyEffect.BuffAttack:
                target.attack += value;
                Debug.Log($"[Synergy] {target.cardName} gains +{value} attack");
                break;

            case SynergyEffect.BuffHealth:
                target.health += value;
                Debug.Log($"[Synergy] {target.cardName} gains +{value} health");
                break;

            case SynergyEffect.BuffStats:
                target.attack += value;
                target.health += value;
                Debug.Log($"[Synergy] {target.cardName} gains +{value}/+{value}");
                break;

            case SynergyEffect.BonusGold:
                if (owner != null)
                {
                    owner.coins += value;
                    Debug.Log($"[Synergy] Player gains +{value} gold");
                }
                break;

            case SynergyEffect.Shield:
                target.hasAegis = true;
                Debug.Log($"[Synergy] {target.cardName} gains Aegis");
                break;

            case SynergyEffect.HealFlat:
                // Healing in this context means restoring health (capped at some max)
                target.health += value;
                Debug.Log($"[Synergy] {target.cardName} healed for {value}");
                break;

            // Additional effects can be implemented as needed
            default:
                Debug.Log($"[Synergy] Effect {effect} not yet implemented");
                break;
        }
    }

    /// <summary>
    /// Calculate bonus sell value from synergies (e.g., Pentacles).
    /// </summary>
    public int GetSellBonus(Card card)
    {
        int bonus = 0;
        foreach (var tribe in card.GetTribes())
        {
            var tier = GetActiveTier(tribe);
            if (tier != null && tier.trigger == SynergyTrigger.OnSell && tier.effect == SynergyEffect.BonusGold)
            {
                bonus += tier.value;
            }
        }
        return bonus;
    }

    /// <summary>
    /// Calculate cost reduction from synergies.
    /// </summary>
    public int GetCostReduction(Card card)
    {
        int reduction = 0;
        foreach (var tribe in card.GetTribes())
        {
            var tier = GetActiveTier(tribe);
            if (tier != null && tier.effect == SynergyEffect.ReduceCost)
            {
                reduction += tier.value;
            }
        }
        return reduction;
    }
}
