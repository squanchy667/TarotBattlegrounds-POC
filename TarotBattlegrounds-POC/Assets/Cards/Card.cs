using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Tarot/Card")]
public class Card : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    public int tier;
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
        return clone;
    }

    public virtual void OnAttack(Card defender) { }
    public virtual void OnDeath() { }
    public virtual void OnSurvive() { }
}