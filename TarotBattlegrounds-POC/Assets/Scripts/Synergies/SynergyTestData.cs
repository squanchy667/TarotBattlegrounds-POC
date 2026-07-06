using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper class to create TribeSynergy ScriptableObjects.
/// Uses ThemeManager for theme-specific names and colors.
/// </summary>
public static class SynergyTestData
{
    /// <summary>
    /// Create all 4 tribe synergies with their tiered effects and combos.
    /// </summary>
    public static TribeSynergy[] CreateAllTribeSynergies()
    {
        // Ensure ThemeManager exists
        ThemeManager.EnsureExists();

        return new TribeSynergy[]
        {
            CreateTribeSynergy(TribeType.Pentacles),
            CreateTribeSynergy(TribeType.Cups),
            CreateTribeSynergy(TribeType.Swords),
            CreateTribeSynergy(TribeType.Wands)
        };
    }

    /// <summary>
    /// Create a synergy for a specific tribe type using theme configuration.
    /// </summary>
    public static TribeSynergy CreateTribeSynergy(TribeType tribe)
    {
        var synergy = ScriptableObject.CreateInstance<TribeSynergy>();
        synergy.tribe = tribe;
        synergy.tribeName = ThemeManager.GetTribeName(tribe);
        synergy.description = ThemeManager.GetTribeDescription(tribe);
        synergy.themeColor = ThemeManager.GetTribeColor(tribe);

        // Configure tiers and combos based on tribe type
        switch (tribe)
        {
            case TribeType.Pentacles:
                ConfigureTribe1_Economy(synergy);
                break;
            case TribeType.Cups:
                ConfigureTribe2_Healing(synergy);
                break;
            case TribeType.Swords:
                ConfigureTribe3_Aggro(synergy);
                break;
            case TribeType.Wands:
                ConfigureTribe4_Buffs(synergy);
                break;
        }

        return synergy;
    }

    /// <summary>
    /// Tribe 1 (Pentacles/Economy): Gold bonuses on sell, cost reduction at high tiers.
    /// </summary>
    private static void ConfigureTribe1_Economy(TribeSynergy synergy)
    {
        string tribeName = synergy.tribeName;

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.OnSell,
                effect = SynergyEffect.BonusGold,
                target = SynergyTarget.Self,
                value = 1,
                description = $"(2) +1 gold when selling {tribeName} cards"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.OnSell,
                effect = SynergyEffect.BonusGold,
                target = SynergyTarget.Self,
                value = 2,
                description = $"(4) +2 gold when selling {tribeName} cards"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.Passive,
                effect = SynergyEffect.ReduceCost,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = $"(6) {tribeName} cards cost 1 less to buy"
            }
        };

        // Combo with Tribe 2 (Cups)
        string comboTribeName = ThemeManager.GetTribeName(TribeType.Cups);
        synergy.comboTribe = TribeType.Cups;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BonusGold;
        synergy.comboValue = 1;
        synergy.comboDescription = $"{tribeName} + {comboTribeName} (2 each): +1 gold at end of turn";
    }

    /// <summary>
    /// Tribe 2 (Cups/Healing): Heal and protect allies.
    /// </summary>
    private static void ConfigureTribe2_Healing(TribeSynergy synergy)
    {
        string tribeName = synergy.tribeName;

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
                description = $"(4) Heal all {tribeName} for 2 at end of turn"
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

        // Combo with Tribe 4 (Wands)
        string comboTribeName = ThemeManager.GetTribeName(TribeType.Wands);
        synergy.comboTribe = TribeType.Wands;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BuffAttack;
        synergy.comboValue = 1;
        synergy.comboDescription = $"{tribeName} + {comboTribeName} (2 each): Healing also grants +1 attack";
    }

    /// <summary>
    /// Tribe 3 (Swords/Aggro): Damage bonuses and cleave effects.
    /// </summary>
    private static void ConfigureTribe3_Aggro(TribeSynergy synergy)
    {
        string tribeName = synergy.tribeName;

        synergy.tiers = new SynergyTier[]
        {
            new SynergyTier
            {
                threshold = 2,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BuffAttack,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = $"(2) {tribeName} cards gain +1 attack at start of combat"
            },
            new SynergyTier
            {
                threshold = 4,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BonusDamage,
                target = SynergyTarget.AllTribeMembers,
                value = 2,
                description = $"(4) {tribeName} cards deal +2 bonus damage"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.Passive,
                effect = SynergyEffect.Cleave,
                target = SynergyTarget.AllTribeMembers,
                value = 1,
                description = $"(6) {tribeName} attacks hit adjacent enemies"
            }
        };

        // Combo with Tribe 1 (Pentacles)
        string comboTribeName = ThemeManager.GetTribeName(TribeType.Pentacles);
        synergy.comboTribe = TribeType.Pentacles;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BonusGold;
        synergy.comboValue = 1;
        synergy.comboDescription = $"{tribeName} + {comboTribeName} (2 each): Killing enemies grants +1 gold";
    }

    /// <summary>
    /// Tribe 4 (Wands/Buffs): Stat increases for allies.
    /// </summary>
    private static void ConfigureTribe4_Buffs(TribeSynergy synergy)
    {
        string tribeName = synergy.tribeName;

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
                description = $"(4) Give all {tribeName} +1/+1 at end of turn"
            },
            new SynergyTier
            {
                threshold = 6,
                trigger = SynergyTrigger.StartOfCombat,
                effect = SynergyEffect.BuffAttack,
                target = SynergyTarget.AllFriendly,
                value = 2,
                description = "(6) Wands get +2 Attack at combat start"
            }
        };

        // Combo with Tribe 3 (Swords)
        string comboTribeName = ThemeManager.GetTribeName(TribeType.Swords);
        synergy.comboTribe = TribeType.Swords;
        synergy.comboThreshold = 2;
        synergy.comboEffect = SynergyEffect.BuffAttack;
        synergy.comboValue = 1;
        synergy.comboDescription = $"{tribeName} + {comboTribeName} (2 each): Attack buffs are doubled";
    }

    /// <summary>
    /// Initialize SynergyManager with synergies at runtime.
    /// Auto-creates SynergyManager and ThemeManager if they don't exist.
    /// </summary>
    public static void InitializeSynergyManager()
    {
        // Ensure ThemeManager exists first
        ThemeManager.EnsureExists();

        // Auto-create SynergyManager if it doesn't exist
        if (SynergyManager.Instance == null)
        {
            Debug.Log("[SynergyTestData] SynergyManager not found, creating one...");
            GameObject synergyManagerObj = new GameObject("SynergyManager");
            synergyManagerObj.AddComponent<SynergyManager>();
        }

        SynergyManager.Instance.tribeSynergies = CreateAllTribeSynergies();
        Debug.Log("[SynergyTestData] Initialized SynergyManager with 4 tribe synergies");
    }
}
