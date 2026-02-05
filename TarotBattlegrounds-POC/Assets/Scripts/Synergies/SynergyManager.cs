using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages tribe synergies, counting tribe members and applying bonuses.
/// Attach to a GameObject in the scene and assign TribeSynergy ScriptableObjects.
///
/// MULTIPLAYER PATTERN (M1 FIX):
/// Always use CalculateSynergies(board) to get a per-player snapshot, then pass
/// that snapshot to methods like TriggerSynergies(), GetSellBonus(), GetCostReduction().
///
/// Example:
///   var snapshot = SynergyManager.Instance.CalculateSynergies(player.board);
///   SynergyManager.Instance.TriggerSynergies(trigger, board, player, snapshot);
///   int bonus = SynergyManager.Instance.GetSellBonus(card, snapshot);
///
/// DO NOT use the deprecated global state methods (UpdateTribeCounts, GetTribeCount, etc.)
/// as they cause bugs in multiplayer when multiple players' synergies overwrite each other.
/// </summary>
public class SynergyManager : MonoBehaviour
{
    /// <summary>
    /// Immutable snapshot of synergy state for a specific board configuration.
    /// Allows per-player synergy calculations without mutating singleton state.
    /// </summary>
    public class SynergySnapshot
    {
        public Dictionary<TribeType, int> tribeCounts = new Dictionary<TribeType, int>();
        public Dictionary<TribeType, SynergyTier> activeTiers = new Dictionary<TribeType, SynergyTier>();
        public List<(TribeType, TribeType)> activeCombos = new List<(TribeType, TribeType)>();
    }

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

    /// <summary>
    /// Initialize or reinitialize the synergy cache. Call after setting tribeSynergies.
    /// </summary>
    public void InitializeSynergyCache()
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
    /// Parse legacy tribe string to TribeType enum.
    /// Uses ThemeManager for theme-agnostic parsing.
    /// </summary>
    private TribeType ParseLegacyTribe(string tribeName)
    {
        if (string.IsNullOrEmpty(tribeName)) return TribeType.None;

        // Use ThemeManager for parsing (supports aliases from theme config)
        return ThemeManager.ParseTribeName(tribeName);
    }

    /// <summary>
    /// Count all tribes on a player's board and update active synergies.
    /// [OBSOLETE] This method mutates global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board) instead to get a per-player snapshot.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board) instead for per-player synergy calculations", true)]
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

        Debug.Log($"[SynergyManager] UpdateTribeCounts called with {board.Count} cards, {_synergyByTribe.Count} synergies loaded");

        // Count tribes (cards can have multiple tribes)
        foreach (var card in board)
        {
            // Check new tribes array first
            if (card.tribes != null && card.tribes.Length > 0)
            {
                foreach (var tribe in card.tribes)
                {
                    if (tribe == TribeType.None) continue;
                    if (!_tribeCounts.ContainsKey(tribe))
                        _tribeCounts[tribe] = 0;
                    _tribeCounts[tribe]++;
                    Debug.Log($"[SynergyManager] {card.cardName} has tribe (array): {tribe}");
                }
            }
            // Fallback to legacy tribe string
            else if (!string.IsNullOrEmpty(card.tribe))
            {
                TribeType legacyTribe = ParseLegacyTribe(card.tribe);
                if (legacyTribe != TribeType.None)
                {
                    if (!_tribeCounts.ContainsKey(legacyTribe))
                        _tribeCounts[legacyTribe] = 0;
                    _tribeCounts[legacyTribe]++;
                    Debug.Log($"[SynergyManager] {card.cardName} has tribe (legacy): {card.tribe} -> {legacyTribe}");
                }
                else
                {
                    Debug.Log($"[SynergyManager] {card.cardName} has unknown legacy tribe: '{card.tribe}'");
                }
            }
            else
            {
                Debug.Log($"[SynergyManager] {card.cardName} has no tribe");
            }
        }

        // Log final tribe counts
        foreach (var kvp in _tribeCounts)
        {
            Debug.Log($"[SynergyManager] Tribe count: {kvp.Key} = {kvp.Value}");
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

    /// <summary>
    /// Calculate synergies for a board and return a snapshot without mutating singleton state.
    /// Use this for per-player synergy calculations in multiplayer.
    /// </summary>
    public SynergySnapshot CalculateSynergies(List<Card> board)
    {
        var snapshot = new SynergySnapshot();

        if (board == null || board.Count == 0)
            return snapshot;

        // Count tribes
        foreach (var card in board)
        {
            if (card.tribes != null && card.tribes.Length > 0)
            {
                foreach (var tribe in card.tribes)
                {
                    if (tribe == TribeType.None) continue;
                    if (!snapshot.tribeCounts.ContainsKey(tribe))
                        snapshot.tribeCounts[tribe] = 0;
                    snapshot.tribeCounts[tribe]++;
                }
            }
            else if (!string.IsNullOrEmpty(card.tribe))
            {
                TribeType legacyTribe = ParseLegacyTribe(card.tribe);
                if (legacyTribe != TribeType.None)
                {
                    if (!snapshot.tribeCounts.ContainsKey(legacyTribe))
                        snapshot.tribeCounts[legacyTribe] = 0;
                    snapshot.tribeCounts[legacyTribe]++;
                }
            }
        }

        // Determine active tiers
        foreach (var kvp in snapshot.tribeCounts)
        {
            if (_synergyByTribe.TryGetValue(kvp.Key, out TribeSynergy synergy))
            {
                SynergyTier activeTier = synergy.GetActiveTier(kvp.Value);
                if (activeTier != null)
                    snapshot.activeTiers[kvp.Key] = activeTier;
            }
        }

        // Check combos
        foreach (var synergy in _synergyByTribe.Values)
        {
            if (synergy.comboTribe == TribeType.None) continue;
            int thisTribeCount = snapshot.tribeCounts.TryGetValue(synergy.tribe, out int tc) ? tc : 0;
            int partnerCount = snapshot.tribeCounts.TryGetValue(synergy.comboTribe, out int pc) ? pc : 0;

            if (synergy.IsComboActive(thisTribeCount, partnerCount))
            {
                var combo = synergy.tribe < synergy.comboTribe
                    ? (synergy.tribe, synergy.comboTribe)
                    : (synergy.comboTribe, synergy.tribe);
                if (!snapshot.activeCombos.Contains(combo))
                    snapshot.activeCombos.Add(combo);
            }
        }

        return snapshot;
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
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board) to get a snapshot, then check snapshot.tribeCounts.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).tribeCounts instead", true)]
    public int GetTribeCount(TribeType tribe)
    {
        return _tribeCounts.TryGetValue(tribe, out int count) ? count : 0;
    }

    /// <summary>
    /// Get all current tribe counts.
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board).tribeCounts instead.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).tribeCounts instead", true)]
    public Dictionary<TribeType, int> GetAllTribeCounts()
    {
        return new Dictionary<TribeType, int>(_tribeCounts);
    }

    /// <summary>
    /// Get the active tier for a tribe, or null if no tier is active.
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board).activeTiers instead.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).activeTiers instead", true)]
    public SynergyTier GetActiveTier(TribeType tribe)
    {
        return _activeTiers.TryGetValue(tribe, out SynergyTier tier) ? tier : null;
    }

    /// <summary>
    /// Get all active tiers.
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board).activeTiers instead.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).activeTiers instead", true)]
    public Dictionary<TribeType, SynergyTier> GetAllActiveTiers()
    {
        return new Dictionary<TribeType, SynergyTier>(_activeTiers);
    }

    /// <summary>
    /// Check if a specific combo is active.
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board).activeCombos instead.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).activeCombos instead", true)]
    public bool IsComboActive(TribeType tribe1, TribeType tribe2)
    {
        var combo = tribe1 < tribe2 ? (tribe1, tribe2) : (tribe2, tribe1);
        return _activeCombos.Contains(combo);
    }

    /// <summary>
    /// Get all active combos.
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use CalculateSynergies(board).activeCombos instead.
    /// </summary>
    [System.Obsolete("Use CalculateSynergies(board).activeCombos instead", true)]
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
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use TriggerSynergies(trigger, board, owner, snapshot) with a pre-computed snapshot instead.
    /// </summary>
    [System.Obsolete("Use TriggerSynergies(trigger, board, owner, CalculateSynergies(board)) instead", true)]
    public void TriggerSynergies(SynergyTrigger trigger, List<Card> board, Player owner)
    {
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";
        Debug.Log($"[SynergyManager] === {playerName}: Triggering {trigger} synergies ===");

        foreach (var kvp in _activeTiers)
        {
            TribeType tribe = kvp.Key;
            SynergyTier tier = kvp.Value;

            if (tier.trigger == trigger)
            {
                Debug.Log($"[SynergyManager] {playerName}: {tribe} Tier {tier.threshold} - {tier.description}");
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
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";

        if (targets.Count == 0)
        {
            Debug.Log($"[SynergyManager] {playerName}: {tribe} synergy has no valid targets (target type: {tier.target})");
            return;
        }

        string targetNames = string.Join(", ", targets.Select(t => t.cardName));
        Debug.Log($"[SynergyManager] {playerName}: Applying {tribe} synergy ({tier.effect} +{tier.value}) to {targets.Count} target(s): [{targetNames}]");

        foreach (var target in targets)
        {
            ApplyEffect(tier.effect, tier.value, target, owner);
        }
    }

    private void ApplyComboEffects(List<Card> board, Player owner)
    {
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";

        foreach (var combo in _activeCombos)
        {
            TribeSynergy synergy1 = GetTribeSynergy(combo.Item1);
            TribeSynergy synergy2 = GetTribeSynergy(combo.Item2);

            // Apply combo from synergy1 if it references synergy2
            if (synergy1 != null && synergy1.comboTribe == combo.Item2)
            {
                Debug.Log($"[SynergyManager] {playerName}: COMBO {combo.Item1}+{combo.Item2} - {synergy1.comboDescription}");
                Debug.Log($"[SynergyManager] {playerName}: Applying combo ({synergy1.comboEffect} +{synergy1.comboValue}) to all {board.Count} cards");
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
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";
        int oldAtk = target.attack;
        int oldHp = target.health;

        switch (effect)
        {
            case SynergyEffect.BuffAttack:
                target.attack += value;
                Debug.Log($"[Synergy] {playerName}: {target.cardName} gains +{value} attack ({oldAtk} -> {target.attack})");
                break;

            case SynergyEffect.BuffHealth:
                target.health += value;
                Debug.Log($"[Synergy] {playerName}: {target.cardName} gains +{value} health ({oldHp} -> {target.health})");
                break;

            case SynergyEffect.BuffStats:
                target.attack += value;
                target.health += value;
                Debug.Log($"[Synergy] {playerName}: {target.cardName} gains +{value}/+{value} ({oldAtk}/{oldHp} -> {target.attack}/{target.health})");
                break;

            case SynergyEffect.BonusGold:
                if (owner != null)
                {
                    int oldCoins = owner.coins;
                    owner.coins += value;
                    Debug.Log($"[Synergy] {playerName}: gains +{value} gold ({oldCoins} -> {owner.coins})");
                }
                break;

            case SynergyEffect.Shield:
                target.hasAegis = true;
                Debug.Log($"[Synergy] {playerName}: {target.cardName} gains Aegis (shield)");
                break;

            case SynergyEffect.HealFlat:
                // Healing in this context means restoring health (capped at some max)
                target.health += value;
                Debug.Log($"[Synergy] {playerName}: {target.cardName} healed for {value} ({oldHp} -> {target.health})");
                break;

            // Additional effects can be implemented as needed
            default:
                Debug.Log($"[Synergy] {playerName}: Effect {effect} not yet implemented");
                break;
        }
    }

    /// <summary>
    /// Calculate bonus sell value from synergies (e.g., economy-focused tribes).
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use GetSellBonus(card, snapshot) with a pre-computed snapshot instead.
    /// </summary>
    [System.Obsolete("Use GetSellBonus(card, CalculateSynergies(board)) instead", true)]
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
    /// [OBSOLETE] This method uses global singleton state and causes bugs in multiplayer.
    /// Use GetCostReduction(card, snapshot) with a pre-computed snapshot instead.
    /// </summary>
    [System.Obsolete("Use GetCostReduction(card, CalculateSynergies(board)) instead", true)]
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

    // ================================================================
    // Per-player snapshot overloads (M1 fix)
    // ================================================================

    /// <summary>
    /// Trigger synergies using a pre-computed snapshot (per-player safe).
    /// </summary>
    public void TriggerSynergies(SynergyTrigger trigger, List<Card> board, Player owner, SynergySnapshot snapshot)
    {
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";

        foreach (var kvp in snapshot.activeTiers)
        {
            TribeType tribe = kvp.Key;
            SynergyTier tier = kvp.Value;

            if (tier.trigger == trigger)
            {
                ApplySynergyEffect(tribe, tier, board, owner);
            }
        }

        if (trigger == SynergyTrigger.Passive || trigger == SynergyTrigger.StartOfCombat)
        {
            ApplyComboEffects(board, owner, snapshot);
        }
    }

    /// <summary>
    /// Calculate sell bonus using a pre-computed snapshot (per-player safe).
    /// </summary>
    public int GetSellBonus(Card card, SynergySnapshot snapshot)
    {
        int bonus = 0;
        foreach (var tribe in card.GetTribes())
        {
            if (snapshot.activeTiers.TryGetValue(tribe, out SynergyTier tier))
            {
                if (tier.trigger == SynergyTrigger.OnSell && tier.effect == SynergyEffect.BonusGold)
                    bonus += tier.value;
            }
        }
        return bonus;
    }

    /// <summary>
    /// Calculate cost reduction using a pre-computed snapshot (per-player safe).
    /// </summary>
    public int GetCostReduction(Card card, SynergySnapshot snapshot)
    {
        int reduction = 0;
        foreach (var tribe in card.GetTribes())
        {
            if (snapshot.activeTiers.TryGetValue(tribe, out SynergyTier tier))
            {
                if (tier.effect == SynergyEffect.ReduceCost)
                    reduction += tier.value;
            }
        }
        return reduction;
    }

    private void ApplyComboEffects(List<Card> board, Player owner, SynergySnapshot snapshot)
    {
        string playerName = owner != null ? $"Player {owner.playerId}" : "Unknown";

        foreach (var combo in snapshot.activeCombos)
        {
            TribeSynergy synergy1 = GetTribeSynergy(combo.Item1);
            TribeSynergy synergy2 = GetTribeSynergy(combo.Item2);

            if (synergy1 != null && synergy1.comboTribe == combo.Item2)
            {
                foreach (var card in board)
                {
                    ApplyEffect(synergy1.comboEffect, synergy1.comboValue, card, owner);
                }
            }
        }
    }

    /// <summary>
    /// Calculate the effective buy cost of a card, including base cost and synergy reduction.
    /// Centralizes cost calculation to avoid DRY violation across Player.cs, ShopUI.cs, AIController.cs.
    /// </summary>
    public int GetEffectiveCost(Card card, List<Card> board)
    {
        // Base cost calculation
        int baseCost = Mathf.Max(0, 3 + card.buyCostModifier);

        // Apply synergy cost reduction
        var snapshot = CalculateSynergies(board);
        int reduction = GetCostReduction(card, snapshot);
        if (reduction > 0)
        {
            return Mathf.Max(1, baseCost - reduction);
        }

        return baseCost;
    }
}
