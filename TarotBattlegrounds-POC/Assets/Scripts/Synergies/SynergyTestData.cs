using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper class to create test TribeSynergy ScriptableObjects for testing.
/// Can be used at runtime or in editor to generate synergy definitions.
/// </summary>
public static class SynergyTestData
{
    /// <summary>
    /// Create all 4 tribe synergies with their tiered effects and combos.
    /// </summary>
    public static TribeSynergy[] CreateAllTribeSynergies()
    {
        return new TribeSynergy[]
        {
            CreatePentaclesSynergy(),
            CreateCupsSynergy(),
            CreateSwordsSynergy(),
            CreateWandsSynergy()
        };
    }

    /// <summary>
    /// Pentacles (Earth/Economy): Gold bonuses on sell, cost reduction at high tiers.
    /// Combo with Cups: +1 gold per turn
    /// </summary>
    public static TribeSynergy CreatePentaclesSynergy()
    {
        var synergy = ScriptableObject.CreateInstance<TribeSynergy>();
        synergy.tribe = TribeType.Pentacles;
        synergy.tribeName = "Pentacles";
        synergy.description = "The suit of Earth and material wealth. Pentacles grant economic advantages.";
        synergy.themeColor = new Color(0.8f, 0.6f, 0.2f); // Gold/brown

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.OnSell,
                effect = SynergyEffect.BonusGold,
                target = SynergyTarget.Self,
                value = 1,
                description = "(2) +1 gold when selling Pentacles cards"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.OnSell,
                effect = SynergyEffect.BonusGold,
                target = SynergyTarget.Self,
                value = 2,
                description = "(4) +2 gold when selling Pentacles cards"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.Passive,
                effect = SynergyEffect.ReduceCost,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = "(6) Pentacles cards cost 1 less to buy"
            }
        };

        // Combo with Cups
        synergy.comboTribe = TribeType.Cups;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BonusGold;
        synergy.comboValue = 1;
        synergy.comboDescription = "Pentacles + Cups (2 each): +1 gold at end of turn";

        return synergy;
    }

    /// <summary>
    /// Cups (Water/Healing): Heal and protect allies.
    /// Combo with Wands: Heals also buff attack
    /// </summary>
    public static TribeSynergy CreateCupsSynergy()
    {
        var synergy = ScriptableObject.CreateInstance<TribeSynergy>();
        synergy.tribe = TribeType.Cups;
        synergy.tribeName = "Cups";
        synergy.description = "The suit of Water and emotions. Cups restore health and grant protection.";
        synergy.themeColor = new Color(0.3f, 0.5f, 0.9f); // Blue

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.EndOfTurn,
                effect = SynergyEffect.HealFlat,
                target = SynergyTarget.Adjacent,
                value = 1,
                description = "(2) Heal adjacent cards for 1 at end of turn"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.EndOfTurn,
                effect = SynergyEffect.HealFlat,
                target = SynergyTarget.AllTribeMembers,
                value = 2,
                description = "(4) Heal all Cups for 2 at end of turn"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.Shield,
                target = SynergyTarget.AllFriendly,
                value = 1,
                description = "(6) All friendly cards gain Aegis at start of combat"
            }
        };

        // Combo with Wands
        synergy.comboTribe = TribeType.Wands;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BuffAttack;
        synergy.comboValue = 1;
        synergy.comboDescription = "Cups + Wands (2 each): Healing also grants +1 attack";

        return synergy;
    }

    /// <summary>
    /// Swords (Air/Aggro): Damage bonuses and cleave effects.
    /// Combo with Pentacles: Kills grant gold
    /// </summary>
    public static TribeSynergy CreateSwordsSynergy()
    {
        var synergy = ScriptableObject.CreateInstance<TribeSynergy>();
        synergy.tribe = TribeType.Swords;
        synergy.tribeName = "Swords";
        synergy.description = "The suit of Air and conflict. Swords deal devastating damage.";
        synergy.themeColor = new Color(0.7f, 0.7f, 0.8f); // Silver/steel

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BuffAttack,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = "(2) Swords cards gain +1 attack at start of combat"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BonusDamage,
                target = SynergyTarget.AllTribeMembers,
                value = 2,
                description = "(4) Swords cards deal +2 bonus damage"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.Passive,
                effect = SynergyEffect.Cleave,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = "(6) Swords attacks hit adjacent enemies"
            }
        };

        // Combo with Pentacles
        synergy.comboTribe = TribeType.Pentacles;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BonusGold;
        synergy.comboValue = 1;
        synergy.comboDescription = "Swords + Pentacles (2 each): Killing enemies grants +1 gold";

        return synergy;
    }

    /// <summary>
    /// Wands (Fire/Buffs): Stat increases for allies.
    /// Combo with Swords: Double attack buff effectiveness
    /// </summary>
    public static TribeSynergy CreateWandsSynergy()
    {
        var synergy = ScriptableObject.CreateInstance<TribeSynergy>();
        synergy.tribe = TribeType.Wands;
        synergy.tribeName = "Wands";
        synergy.description = "The suit of Fire and creation. Wands empower allies with stat buffs.";
        synergy.themeColor = new Color(0.9f, 0.4f, 0.2f); // Orange/fire

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.EndOfTurn,
                effect = SynergyEffect.BuffStats,
                target = SynergyTarget.Random,
                value = 1,
                description = "(2) Give a random friendly +1/+1 at end of turn"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.EndOfTurn,
                effect = SynergyEffect.BuffStats,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = "(4) Give all Wands +1/+1 at end of turn"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BuffAttack,
                target = SynergyTarget.AllFriendly,
                value = 2,
                description = "(6) All friendly cards gain +2 attack at start of combat"
            }
        };

        // Combo with Swords
        synergy.comboTribe = TribeType.Swords;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BuffAttack;
        synergy.comboValue = 1;
        synergy.comboDescription = "Wands + Swords (2 each): Attack buffs are doubled";

        return synergy;
    }

    /// <summary>
    /// Initialize SynergyManager with test synergies at runtime.
    /// Call this from a MonoBehaviour.Start() or similar.
    /// Auto-creates SynergyManager if it doesn't exist.
    /// </summary>
    public static void InitializeSynergyManager()
    {
        // Auto-create SynergyManager if it doesn't exist
        if (SynergyManager.Instance == null)
        {
            Debug.Log("[SynergyTestData] SynergyManager not found, creating one...");
            GameObject synergyManagerObj = new GameObject("SynergyManager");
            synergyManagerObj.AddComponent<SynergyManager>();
        }

        SynergyManager.Instance.tribeSynergies = CreateAllTribeSynergies();
        Debug.Log("[SynergyTestData] Initialized SynergyManager with 4 test tribe synergies");
    }
}
