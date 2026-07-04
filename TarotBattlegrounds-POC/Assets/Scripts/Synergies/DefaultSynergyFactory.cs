using UnityEngine;

/// <summary>
/// Pure data construction for default TribeSynergy ScriptableObjects (T209-T210). Extracted
/// from SynergyManager.EnsureDefaultSynergies as the sole low-risk extraction from the
/// SynergyManager god-class review (spec §4) — no state, no behavior change.
///
/// SynergyManager still owns which tribes are missing from the Inspector-assigned
/// `tribeSynergies` array and only calls <see cref="CreateDefault"/> for those, preserving the
/// original "only construct what's missing" behavior verbatim.
/// </summary>
public static class DefaultSynergyFactory
{
    /// <summary>
    /// Build the default TribeSynergy ScriptableObject for a tribe. Returns null for tribes
    /// with no hardcoded default (currently only TribeType.None).
    /// Moved verbatim from SynergyManager.EnsureDefaultSynergies.
    /// </summary>
    public static TribeSynergy CreateDefault(TribeType tribe)
    {
        switch (tribe)
        {
            case TribeType.Stars:
                return CreateStars();
            case TribeType.Coins:
                return CreateCoins();
            case TribeType.Pentacles:
                return CreatePentacles();
            case TribeType.Cups:
                return CreateCups();
            case TribeType.Swords:
                return CreateSwords();
            case TribeType.Wands:
                return CreateWands();
            default:
                return null;
        }
    }

    /// <summary>
    /// Stars synergy (T209): Scaling theme — grow stronger. Includes the T211 Stars+Swords combo.
    /// </summary>
    private static TribeSynergy CreateStars()
    {
        var stars = ScriptableObject.CreateInstance<TribeSynergy>();
        stars.tribe = TribeType.Stars;
        stars.tribeName = "Stars";
        stars.description = "Celestial beings that grow stronger with each fallen ally.";
        stars.themeColor = new Color(1f, 0.9f, 0.4f);
        stars.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffAttack, target = SynergyTarget.AllTribeMembers, value = 1, description = "(2) Stars get +1 Attack at combat start" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffStats, target = SynergyTarget.AllTribeMembers, value = 2, description = "(4) Stars get +2/+2 at combat start" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffStats, target = SynergyTarget.AllTribeMembers, value = 3, description = "(6) Stars get +3/+3 at combat start" }
        };
        // T211: Stars + Swords combo
        stars.comboTribe = TribeType.Swords;
        stars.comboThreshold = 2;
        stars.comboEffect = SynergyEffect.BuffAttack;
        stars.comboValue = 1;
        stars.comboDescription = "Stars+Swords: All allies gain +1 Attack";
        return stars;
    }

    /// <summary>
    /// Coins synergy (T210): Economy/Token theme. Includes the T212 Coins+Pentacles combo.
    /// </summary>
    private static TribeSynergy CreateCoins()
    {
        var coins = ScriptableObject.CreateInstance<TribeSynergy>();
        coins.tribe = TribeType.Coins;
        coins.tribeName = "Coins";
        coins.description = "Fortune-seekers who multiply wealth and spawn allies.";
        coins.themeColor = new Color(0.9f, 0.7f, 0.2f);
        coins.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.OnBuy, effect = SynergyEffect.BonusGold, target = SynergyTarget.Self, value = 1, description = "(2) Gain +1 gold when buying" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.Passive, effect = SynergyEffect.BuffHealth, target = SynergyTarget.AllTribeMembers, value = 2, description = "(4) Coins get +2 Health" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffStats, target = SynergyTarget.AllTribeMembers, value = 2, description = "(6) Coins get +2/+2 at combat start" }
        };
        // T212: Coins + Pentacles combo
        coins.comboTribe = TribeType.Pentacles;
        coins.comboThreshold = 2;
        coins.comboEffect = SynergyEffect.BonusGold;
        coins.comboValue = 1;
        coins.comboDescription = "Coins+Pentacles: Gain +1 gold at start of turn";
        return coins;
    }

    private static TribeSynergy CreatePentacles()
    {
        var pent = ScriptableObject.CreateInstance<TribeSynergy>();
        pent.tribe = TribeType.Pentacles;
        pent.tribeName = "Pentacles";
        pent.description = "Merchants and economists who profit from trade.";
        pent.themeColor = new Color(0.2f, 0.8f, 0.3f);
        pent.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.OnSell, effect = SynergyEffect.BonusGold, target = SynergyTarget.Self, value = 1, description = "(2) +1 gold on sell" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.OnSell, effect = SynergyEffect.BonusGold, target = SynergyTarget.Self, value = 2, description = "(4) +2 gold on sell" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.Passive, effect = SynergyEffect.ReduceCost, target = SynergyTarget.AllTribeMembers, value = 1, description = "(6) Pentacles cards cost 1 less" }
        };
        return pent;
    }

    private static TribeSynergy CreateCups()
    {
        var cups = ScriptableObject.CreateInstance<TribeSynergy>();
        cups.tribe = TribeType.Cups;
        cups.tribeName = "Cups";
        cups.description = "Healers and guardians who protect their allies.";
        cups.themeColor = new Color(0.3f, 0.5f, 1f);
        cups.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffHealth, target = SynergyTarget.AllTribeMembers, value = 1, description = "(2) Cups get +1 Health at combat start" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffHealth, target = SynergyTarget.AllTribeMembers, value = 2, description = "(4) Cups get +2 Health at combat start" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.Shield, target = SynergyTarget.Random, value = 1, description = "(6) A random Cup gains Aegis at combat start" }
        };
        return cups;
    }

    private static TribeSynergy CreateSwords()
    {
        var swords = ScriptableObject.CreateInstance<TribeSynergy>();
        swords.tribe = TribeType.Swords;
        swords.tribeName = "Swords";
        swords.description = "Warriors and assassins who deal devastating damage.";
        swords.themeColor = new Color(0.8f, 0.2f, 0.2f);
        swords.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffAttack, target = SynergyTarget.AllTribeMembers, value = 1, description = "(2) Swords get +1 Attack at combat start" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffAttack, target = SynergyTarget.AllTribeMembers, value = 2, description = "(4) Swords get +2 Attack at combat start" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffStats, target = SynergyTarget.AllTribeMembers, value = 2, description = "(6) Swords get +2/+2 at combat start" }
        };
        return swords;
    }

    private static TribeSynergy CreateWands()
    {
        var wands = ScriptableObject.CreateInstance<TribeSynergy>();
        wands.tribe = TribeType.Wands;
        wands.tribeName = "Wands";
        wands.description = "Mages and enchanters who buff their allies.";
        wands.themeColor = new Color(0.9f, 0.4f, 0.1f);
        wands.tiers = new SynergyTier[]
        {
            new SynergyTier { threshold = 2, trigger = SynergyTrigger.Passive, effect = SynergyEffect.BuffAttack, target = SynergyTarget.AllFriendly, value = 1, description = "(2) All friendlies get +1 Attack" },
            new SynergyTier { threshold = 4, trigger = SynergyTrigger.Passive, effect = SynergyEffect.BuffStats, target = SynergyTarget.AllTribeMembers, value = 1, description = "(4) Wands get +1/+1" },
            new SynergyTier { threshold = 6, trigger = SynergyTrigger.StartOfCombat, effect = SynergyEffect.BuffAttack, target = SynergyTarget.AllTribeMembers, value = 2, description = "(6) Wands get +2 Attack at combat start" }
        };
        return wands;
    }
}
