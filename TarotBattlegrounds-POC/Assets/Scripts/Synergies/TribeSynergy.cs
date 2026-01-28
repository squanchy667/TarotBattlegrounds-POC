using UnityEngine;

/// <summary>
/// Defines a tier threshold and its effect for a tribe synergy.
/// </summary>
[System.Serializable]
public class SynergyTier
{
    [Tooltip("Number of tribe members needed to activate this tier")]
    public int threshold = 2;

    [Tooltip("When this tier effect triggers")]
    public SynergyTrigger trigger = SynergyTrigger.Passive;

    [Tooltip("What effect this tier provides")]
    public SynergyEffect effect = SynergyEffect.BuffAttack;

    [Tooltip("Who receives the effect")]
    public SynergyTarget target = SynergyTarget.AllTribeMembers;

    [Tooltip("Value/magnitude of the effect")]
    public int value = 1;

    [Tooltip("Description shown to player")]
    public string description;
}

/// <summary>
/// ScriptableObject defining a tribe's synergy bonuses.
/// Supports tiered thresholds (2/4/6) and cross-tribe combos.
/// </summary>
[CreateAssetMenu(fileName = "NewTribeSynergy", menuName = "Game/Tribe Synergy")]
public class TribeSynergy : ScriptableObject
{
    [Header("Tribe Identity")]
    [Tooltip("The tribe this synergy belongs to")]
    public TribeType tribe;

    [Tooltip("Display name for this tribe")]
    public string tribeName;

    [Tooltip("Thematic description of the tribe")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("UI color for this tribe")]
    public Color themeColor = Color.white;

    [Tooltip("Icon for this tribe (optional)")]
    public Sprite tribeIcon;

    [Header("Synergy Tiers")]
    [Tooltip("Tiered bonuses at different thresholds (typically 2/4/6)")]
    public SynergyTier[] tiers;

    [Header("Cross-Tribe Combo")]
    [Tooltip("Partner tribe for combo bonus")]
    public TribeType comboTribe = TribeType.None;

    [Tooltip("Minimum count of EACH tribe to activate combo")]
    public int comboThreshold = 2;

    [Tooltip("Effect when combo is active")]
    public SynergyEffect comboEffect = SynergyEffect.BuffStats;

    [Tooltip("Value of combo effect")]
    public int comboValue = 1;

    [Tooltip("Description of combo bonus")]
    public string comboDescription;

    /// <summary>
    /// Get the highest tier that is currently active based on tribe count.
    /// </summary>
    /// <param name="tribeCount">Number of tribe members on board</param>
    /// <returns>The active tier, or null if no tier is met</returns>
    public SynergyTier GetActiveTier(int tribeCount)
    {
        SynergyTier activeTier = null;
        foreach (var tier in tiers)
        {
            if (tribeCount >= tier.threshold)
            {
                activeTier = tier;
            }
        }
        return activeTier;
    }

    /// <summary>
    /// Get all tiers that are currently active based on tribe count.
    /// </summary>
    /// <param name="tribeCount">Number of tribe members on board</param>
    /// <returns>Array of active tiers</returns>
    public SynergyTier[] GetAllActiveTiers(int tribeCount)
    {
        System.Collections.Generic.List<SynergyTier> activeTiers = new System.Collections.Generic.List<SynergyTier>();
        foreach (var tier in tiers)
        {
            if (tribeCount >= tier.threshold)
            {
                activeTiers.Add(tier);
            }
        }
        return activeTiers.ToArray();
    }

    /// <summary>
    /// Check if cross-tribe combo is active.
    /// </summary>
    /// <param name="thisTribeCount">Count of this tribe</param>
    /// <param name="partnerTribeCount">Count of partner tribe</param>
    /// <returns>True if combo is active</returns>
    public bool IsComboActive(int thisTribeCount, int partnerTribeCount)
    {
        if (comboTribe == TribeType.None) return false;
        return thisTribeCount >= comboThreshold && partnerTribeCount >= comboThreshold;
    }
}
