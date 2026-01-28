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
        Taunt
    }

    [Header("Economy")]
    public int buyCostModifier = 0;
    public int sellValueModifier = 0;

    [System.NonSerialized] public bool hasAegis;

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
            AbilityManager.RegisterAbility(this, ability);
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
                return new OnAttackAbility(OnAttackAbility.OnAttackEffect.Cleave, 0);

            // Passive
            case AbilityEffectType.Taunt:
                return new TauntAbility();

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
        // New ability system fields
        clone.abilityTrigger = this.abilityTrigger;
        clone.abilityEffect = this.abilityEffect;
        clone.abilityValue = this.abilityValue;
        // Register abilities for cloned card (needed for combat simulation)
        clone.RegisterAbility();

        // Store base stats for the clone
        clone.StoreBaseStats();

        return clone;
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
        _hasStoredBaseStats = false;

        // Unregister abilities
        AbilityManager.UnregisterCard(this);
    }
}