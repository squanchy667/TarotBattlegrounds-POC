using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/Card")]
public class Card : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    public int tier;

    [Header("Tribe System (Phase 4+)")]
    [Tooltip("Tribes this card belongs to (supports multi-tribe)")]
    public TribeType[] tribes = new TribeType[0];

    [Header("Legacy Tribe (Phase 1-3)")]
    [Tooltip("Legacy string tribe - kept for backwards compatibility")]
    public string tribe;

    [Header("Stats")]
    public int attack;
    public int health;

    [Header("Visuals")]
    public Sprite cardImage;  // Card artwork

    [Header("Legacy Ability (Phase 1-2)")]
    public string ability;
    public enum EffectType { NoEffect, Summoning, LastReading, Guardian, Aegis, Echo }
    public EffectType effectType;
    public string effectParameter;

    [Header("New Ability System (Phase 3+)")]
    public AbilityTrigger abilityTrigger = AbilityTrigger.None;
    public AbilityEffectType abilityEffect = AbilityEffectType.None;
    public int abilityValue = 0;

    // Ability effect types that can be configured in the inspector
    public enum AbilityEffectType
    {
        None,
        // Battlecry effects
        BuffAdjacentAttack,
        BuffAdjacentHealth,
        BuffAdjacentStats,
        BuffAllFriendlyAttack,
        BuffOtherFriendlyAttack,
        GainAegis,
        GainCoins,
        // Deathrattle effects
        DeathrattleBuffRandomFriendly,
        DeathrattleDamageRandomEnemy,
        DeathrattleDamageAllEnemies,
        // OnAttack effects
        OnAttackBuffSelf,
        OnAttackBonusDamage,
        OnAttackCleave,
        // Passive
        Taunt,

        // ====== Phase II Effects (T101-T104) ======

        // OnAllyDeath effects
        OnAllyDeathBuffSelf,            // This card gains +X/+X whenever an ally dies
        OnAllyDeathBuffRandom,          // Give a random ally +X/+X whenever an ally dies

        // OnAllySummoned effects
        OnAllySummonedBuffSummoned,     // Give the summoned card +X/+X
        OnAllySummonedBuffSelf,         // This card gains +X Attack per summon

        // OnSell effects
        OnSellGainCoins,                // Gain X extra coins on sell
        OnSellBuffAllRemaining,         // Give all remaining allies +X/+X on sell

        // Aura effects
        AuraBuffTribematesAttack,       // Aura: +X Attack to all friendly tribe members
        AuraBuffAdjacentStats,          // Aura: +X/+X to adjacent cards
        AuraBuffAllFriendlyAttack,      // Aura: +X Attack to all other friendly cards

        // ====== Phase II Effects (T105-T112) ======

        // Passive keyword effects
        Reborn,                         // Passive: Revive once with 1 HP after death
        Windfury,                       // Passive: Attack twice per combat turn
        Venomous,                       // Passive: Instantly kill any minion damaged by this

        // Summon Token effects
        SummonTokenOnDeath,             // Deathrattle: Summon a X/X token
        SummonTokenOnPlay,              // Battlecry: Summon a X/X token

        // OnAttack effects
        StealBuffOnAttack,              // OnAttack: Steal +X/+X from target

        // Passive effects
        GainArmor,                      // Passive: Reduce incoming damage by X (min 1)

        // Tribe buff effects
        BuffAllTribeOnPlay,             // Battlecry: Give all same-tribe +X/+X
        BuffAllTribeOnDeath,            // Deathrattle: Give all same-tribe +X/+X

        // Transform effects
        RandomTransformOnDeath,         // Deathrattle: Transform into a random card

        // Additional Battlecry effects
        BuffSelfHealth                  // Battlecry: Gain +X Health
    }

    [Header("Economy")]
    public int buyCostModifier = 0;
    public int sellValueModifier = 0;

    [Header("Golden")]
    [System.NonSerialized] public bool isGolden = false;

    [System.NonSerialized] public bool hasAegis;

    // Phase II passive keyword flags (T105-T107, T110)
    [System.NonSerialized] public bool hasReborn;
    [System.NonSerialized] public bool hasWindfury;
    [System.NonSerialized] public bool hasVenomous;
    [System.NonSerialized] public int armor;
    /// <summary>Synergy-granted cleave: damages adjacent enemies on attack.</summary>
    [System.NonSerialized] public bool hasCleave;
    /// <summary>Temporary attack bonus from OnAttackBonusDamage, cleared after each strike.</summary>
    [System.NonSerialized] public int tempBonusDamage;

    // Original stats for resetting when sold
    [System.NonSerialized] private int _baseAttack;
    [System.NonSerialized] private int _baseHealth;
    [System.NonSerialized] private bool _hasStoredBaseStats = false;

    /// <summary>
    /// Create and register the ability for this card instance.
    /// Call this when the card enters play.
    /// </summary>
    public void RegisterAbility()
    {
        if (abilityTrigger == AbilityTrigger.None && abilityEffect == AbilityEffectType.None)
            return;

        IAbility ability = CreateAbility();
        if (ability != null)
        {
            // If card's trigger differs from ability's default, wrap with override
            // e.g., Warlord Supreme: StartOfCombat trigger + BattlecryAbility buff
            if (abilityTrigger != AbilityTrigger.None && abilityTrigger != ability.Trigger)
            {
                ability = new TriggerOverrideAbility(ability, abilityTrigger);
            }
            AbilityManager.RegisterAbility(this, ability);
        }

        // T105-T107, T110: Set passive keyword flags
        switch (abilityEffect)
        {
            case AbilityEffectType.Reborn:
                hasReborn = true;
                break;
            case AbilityEffectType.Windfury:
                hasWindfury = true;
                break;
            case AbilityEffectType.Venomous:
                hasVenomous = true;
                break;
            case AbilityEffectType.GainArmor:
                armor = abilityValue;
                break;
        }
    }

    private IAbility CreateAbility()
    {
        switch (abilityEffect)
        {
            // Battlecry effects
            case AbilityEffectType.BuffAdjacentAttack:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentAttack, abilityValue);
            case AbilityEffectType.BuffAdjacentHealth:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentHealth, abilityValue);
            case AbilityEffectType.BuffAdjacentStats:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAdjacentStats, abilityValue);
            case AbilityEffectType.BuffAllFriendlyAttack:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffAllFriendlyAttack, abilityValue);
            case AbilityEffectType.BuffOtherFriendlyAttack:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffOtherFriendlyAttack, abilityValue);
            case AbilityEffectType.GainAegis:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.GainAegis, 0);
            case AbilityEffectType.GainCoins:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.GainCoins, abilityValue);

            // Deathrattle effects
            case AbilityEffectType.DeathrattleBuffRandomFriendly:
                return new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.BuffRandomFriendlyStats, abilityValue);
            case AbilityEffectType.DeathrattleDamageRandomEnemy:
                return new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.DealDamageToRandomEnemy, abilityValue);
            case AbilityEffectType.DeathrattleDamageAllEnemies:
                return new DeathrattleAbility(DeathrattleAbility.DeathrattleEffect.DealDamageToAllEnemies, abilityValue);

            // OnAttack effects
            case AbilityEffectType.OnAttackBuffSelf:
                return new OnAttackAbility(OnAttackAbility.OnAttackEffect.BuffSelfAttack, abilityValue);
            case AbilityEffectType.OnAttackBonusDamage:
                return new OnAttackAbility(OnAttackAbility.OnAttackEffect.DealBonusDamage, abilityValue);
            case AbilityEffectType.OnAttackCleave:
                return new OnAttackAbility(OnAttackAbility.OnAttackEffect.Cleave, abilityValue);

            // Passive
            case AbilityEffectType.Taunt:
                return new TauntAbility();

            // OnAllyDeath effects (T101)
            case AbilityEffectType.OnAllyDeathBuffSelf:
                return new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffSelfStats, abilityValue);
            case AbilityEffectType.OnAllyDeathBuffRandom:
                return new OnAllyDeathAbility(OnAllyDeathAbility.OnAllyDeathEffect.BuffRandomAllyStats, abilityValue);

            // OnAllySummoned effects (T102)
            case AbilityEffectType.OnAllySummonedBuffSummoned:
                return new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSummonedStats, abilityValue);
            case AbilityEffectType.OnAllySummonedBuffSelf:
                return new OnAllySummonedAbility(OnAllySummonedAbility.OnAllySummonedEffect.BuffSelfAttack, abilityValue);

            // OnSell effects (T103)
            case AbilityEffectType.OnSellGainCoins:
                return new OnSellAbility(OnSellAbility.OnSellEffect.GainCoins, abilityValue);
            case AbilityEffectType.OnSellBuffAllRemaining:
                return new OnSellAbility(OnSellAbility.OnSellEffect.BuffAllRemainingStats, abilityValue);

            // Aura effects (T104)
            case AbilityEffectType.AuraBuffTribematesAttack:
                // C8 fix: Pass card's primary tribe so aura only buffs same-tribe minions
                TribeType auraTribe = (tribes != null && tribes.Length > 0) ? tribes[0] : TribeType.None;
                return new AuraAbility(AuraAbility.AuraEffect.BuffTribematesAttack, abilityValue, auraTribe);
            case AbilityEffectType.AuraBuffAdjacentStats:
                return new AuraAbility(AuraAbility.AuraEffect.BuffAdjacentStats, abilityValue);
            case AbilityEffectType.AuraBuffAllFriendlyAttack:
                return new AuraAbility(AuraAbility.AuraEffect.BuffAllFriendlyAttack, abilityValue);

            // Passive keyword effects (T105-T107)
            case AbilityEffectType.Reborn:
                return new RebornAbility();
            case AbilityEffectType.Windfury:
                return new WindfuryAbility();
            case AbilityEffectType.Venomous:
                return new VenomousAbility();

            // Summon Token effects (T108)
            case AbilityEffectType.SummonTokenOnDeath:
                return new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnDeath, abilityValue);
            case AbilityEffectType.SummonTokenOnPlay:
                return new SummonTokenAbility(SummonTokenAbility.SummonTrigger.OnPlay, abilityValue);

            // StealBuff OnAttack (T109)
            case AbilityEffectType.StealBuffOnAttack:
                return new StealBuffAbility(abilityValue);

            // GainArmor passive (T110)
            case AbilityEffectType.GainArmor:
                return new GainArmorAbility(abilityValue);

            // BuffAllTribes (T111)
            case AbilityEffectType.BuffAllTribeOnPlay:
                return new BuffAllTribesAbility(AbilityTrigger.Battlecry, abilityValue);
            case AbilityEffectType.BuffAllTribeOnDeath:
                return new BuffAllTribesAbility(AbilityTrigger.Deathrattle, abilityValue);

            // RandomTransform (T112)
            case AbilityEffectType.RandomTransformOnDeath:
                return new RandomTransformAbility();

            // Additional Battlecry
            case AbilityEffectType.BuffSelfHealth:
                return new BattlecryAbility(BattlecryAbility.BattlecryEffect.BuffSelfHealth, abilityValue);

            default:
                return null;
        }
    }

    public Card Clone()
    {
        Card clone = ScriptableObject.CreateInstance<Card>();
        clone.cardName = this.cardName;
        clone.tier = this.tier;
        clone.tribe = this.tribe;
        // Clone tribes array (Phase 4+)
        if (this.tribes != null && this.tribes.Length > 0)
        {
            clone.tribes = new TribeType[this.tribes.Length];
            System.Array.Copy(this.tribes, clone.tribes, this.tribes.Length);
        }
        else
        {
            clone.tribes = new TribeType[0];
        }
        clone.attack = this.attack;
        clone.health = this.health;
        clone.cardImage = this.cardImage;
        clone.ability = this.ability;
        clone.buyCostModifier = this.buyCostModifier;
        clone.sellValueModifier = this.sellValueModifier;
        clone.effectType = this.effectType;
        clone.effectParameter = this.effectParameter;
        clone.hasAegis = this.hasAegis;
        clone.hasReborn = this.hasReborn;
        clone.hasWindfury = this.hasWindfury;
        clone.hasVenomous = this.hasVenomous;
        clone.hasCleave = this.hasCleave;
        clone.armor = this.armor;
        // BUG FIX: Do NOT copy isGolden! It should ONLY be set by CreateGoldenVersion()
        // clone.isGolden = this.isGolden; // REMOVED - was causing golden contamination
        clone.isGolden = false; // Always start as non-golden
        // New ability system fields
        clone.abilityTrigger = this.abilityTrigger;
        clone.abilityEffect = this.abilityEffect;
        clone.abilityValue = this.abilityValue;
        // Register abilities for cloned card (needed for combat simulation)
        clone.RegisterAbility();

        // Propagate original base stats from source card (not current buffed stats)
        if (this._hasStoredBaseStats)
        {
            clone._baseAttack = this._baseAttack;
            clone._baseHealth = this._baseHealth;
            clone._hasStoredBaseStats = true;
        }
        else
        {
            clone.StoreBaseStats();
        }

        return clone;
    }

    /// <summary>
    /// Create a golden version of this card with doubled stats.
    /// </summary>
    public static Card CreateGoldenVersion(Card baseCard)
    {
        Card golden = baseCard.Clone();
        golden.isGolden = true;
        // Use base (pre-buff) stats when available to avoid doubling aura/synergy buffs
        int baseAtk = baseCard._hasStoredBaseStats ? baseCard._baseAttack : baseCard.attack;
        int baseHp = baseCard._hasStoredBaseStats ? baseCard._baseHealth : baseCard.health;
        golden.attack = baseAtk * 2;
        golden.health = baseHp * 2;
        // Golden cards only double stats, not ability values
        // abilityValue is already correctly copied from Clone()

        // Re-store base stats for the golden version
        golden._baseAttack = golden.attack;
        golden._baseHealth = golden.health;
        golden._hasStoredBaseStats = true;

        Debug.Log($"[Card] Created golden {golden.cardName}: {golden.attack}/{golden.health} (ability value: {golden.abilityValue})");
        return golden;
    }

    /// <summary>
    /// Check if this card belongs to a specific tribe.
    /// Checks both new tribes array and legacy tribe string.
    /// </summary>
    public bool HasTribe(TribeType tribeType)
    {
        // Check new tribes array
        if (tribes != null && tribes.Length > 0)
        {
            foreach (var t in tribes)
            {
                if (t == tribeType) return true;
            }
        }

        // Fallback to legacy tribe string using ThemeManager
        if (!string.IsNullOrEmpty(tribe))
        {
            TribeType parsedTribe = ThemeManager.ParseTribeName(tribe);
            return parsedTribe == tribeType;
        }

        return false;
    }

    /// <summary>
    /// Get all tribes this card belongs to.
    /// </summary>
    public TribeType[] GetTribes()
    {
        return tribes ?? new TribeType[0];
    }

    /// <summary>
    /// Get the primary (first) tribe of this card.
    /// Returns TribeType.None if the card has no tribes.
    /// </summary>
    public TribeType GetPrimaryTribe()
    {
        // Check new tribes array first
        if (tribes != null && tribes.Length > 0 && tribes[0] != TribeType.None)
        {
            return tribes[0];
        }

        // Fallback to legacy tribe string using ThemeManager
        if (!string.IsNullOrEmpty(tribe))
        {
            return ThemeManager.ParseTribeName(tribe);
        }

        return TribeType.None;
    }

    public virtual void OnAttack(Card defender) { }
    public virtual void OnDeath() { }
    public virtual void OnSurvive() { }

    /// <summary>
    /// Store the current stats as base stats.
    /// Call this when the card is first acquired (bought/cloned).
    /// </summary>
    public void StoreBaseStats()
    {
        if (!_hasStoredBaseStats)
        {
            _baseAttack = attack;
            _baseHealth = health;
            _hasStoredBaseStats = true;
            Debug.Log($"[Card] {cardName} base stats stored: {_baseAttack}/{_baseHealth}");
        }
    }

    /// <summary>
    /// Reset the card to its original base stats.
    /// Call this when selling the card back to the pool.
    /// </summary>
    public void ResetToBaseStats()
    {
        if (_hasStoredBaseStats)
        {
            Debug.Log($"[Card] {cardName} reset from {attack}/{health} to base {_baseAttack}/{_baseHealth}");
            attack = _baseAttack;
            health = _baseHealth;
        }

        // Also reset any combat-related state
        hasAegis = false;
        hasReborn = false;
        hasWindfury = false;
        hasVenomous = false;
        hasCleave = false;
        armor = 0;
        tempBonusDamage = 0;
        isGolden = false; // Reset golden status when card returns to pool
        _hasStoredBaseStats = false;

        // Unregister abilities
        AbilityManager.UnregisterCard(this);
    }
}