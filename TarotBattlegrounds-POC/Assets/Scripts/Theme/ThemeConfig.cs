using UnityEngine;

/// <summary>
/// Configuration for game theming. All theme-specific strings, colors, and assets
/// are defined here to allow complete reskinning without code changes.
/// </summary>
[CreateAssetMenu(fileName = "ThemeConfig", menuName = "Game/Theme Config")]
public class ThemeConfig : ScriptableObject
{
    [Header("Game Identity")]
    [Tooltip("Name of the game/theme (e.g., 'Tarot Battlegrounds', 'Robot Wars')")]
    public string gameName = "Auto Battler";

    [Tooltip("Short identifier for menus and paths")]
    public string themeId = "AutoBattler";

    [Header("Tribe Configuration")]
    [Tooltip("Configure all 4 tribes - order matches TribeType enum (index 1-4)")]
    public TribeThemeData[] tribes = new TribeThemeData[4];

    [Header("UI Text")]
    public string shopTitle = "Shop";
    public string handTitle = "Hand";
    public string boardTitle = "Board";
    public string coinsLabel = "Coins";
    public string healthLabel = "Health";
    public string tierLabel = "Tier";
    public string buyButtonText = "Buy";
    public string sellButtonText = "Sell";
    public string playButtonText = "Play";
    public string rerollButtonText = "Reroll";
    public string upgradeButtonText = "Upgrade";
    public string endTurnButtonText = "End Turn";

    [Header("Combat Text")]
    public string combatPhaseTitle = "Combat Phase";
    public string recruitPhaseTitle = "Recruit Phase";
    public string victoryText = "Victory!";
    public string defeatText = "Defeat!";
    public string tieText = "Tie!";

    /// <summary>
    /// Get theme data for a specific tribe type.
    /// </summary>
    public TribeThemeData GetTribeData(TribeType tribe)
    {
        if (tribe == TribeType.None) return null;
        int index = (int)tribe - 1; // TribeType enum starts at 1
        if (index >= 0 && index < tribes.Length)
            return tribes[index];
        return null;
    }

    /// <summary>
    /// Get the display name for a tribe.
    /// </summary>
    public string GetTribeName(TribeType tribe)
    {
        var data = GetTribeData(tribe);
        return data != null ? data.tribeName : tribe.ToString();
    }

    /// <summary>
    /// Get the description for a tribe.
    /// </summary>
    public string GetTribeDescription(TribeType tribe)
    {
        var data = GetTribeData(tribe);
        return data != null ? data.description : "";
    }

    /// <summary>
    /// Get the theme color for a tribe.
    /// </summary>
    public Color GetTribeColor(TribeType tribe)
    {
        var data = GetTribeData(tribe);
        return data != null ? data.themeColor : Color.gray;
    }

    /// <summary>
    /// Parse a tribe name string to TribeType enum.
    /// Supports both enum names and theme-specific names.
    /// </summary>
    public TribeType ParseTribeName(string tribeName)
    {
        if (string.IsNullOrEmpty(tribeName)) return TribeType.None;

        string normalized = tribeName.Trim().ToLower();

        // Check each tribe's configured name and aliases
        for (int i = 0; i < tribes.Length; i++)
        {
            if (tribes[i] == null) continue;

            // Check main name
            if (tribes[i].tribeName.ToLower() == normalized)
                return (TribeType)(i + 1);

            // Check aliases
            if (tribes[i].aliases != null)
            {
                foreach (var alias in tribes[i].aliases)
                {
                    if (alias.ToLower() == normalized)
                        return (TribeType)(i + 1);
                }
            }
        }

        // Fallback to enum parsing
        if (System.Enum.TryParse<TribeType>(tribeName, true, out TribeType result))
            return result;

        return TribeType.None;
    }

    /// <summary>
    /// Get valid tribe combination strings for CSV import validation.
    /// </summary>
    public string[] GetValidTribeCombinations()
    {
        var combinations = new System.Collections.Generic.List<string>();

        // Single tribes
        foreach (var tribe in tribes)
        {
            if (tribe != null && !string.IsNullOrEmpty(tribe.tribeName))
                combinations.Add(tribe.tribeName);
        }

        // Dual tribe combinations
        for (int i = 0; i < tribes.Length; i++)
        {
            for (int j = i + 1; j < tribes.Length; j++)
            {
                if (tribes[i] != null && tribes[j] != null)
                {
                    combinations.Add($"{tribes[i].tribeName}/{tribes[j].tribeName}");
                    combinations.Add($"{tribes[j].tribeName}/{tribes[i].tribeName}");
                }
            }
        }

        // Special values
        combinations.Add("All Suits");
        combinations.Add("Neutral");
        combinations.Add("");

        return combinations.ToArray();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Create a default Tarot theme configuration.
    /// </summary>
    [UnityEditor.MenuItem("Game/Create Default Tarot Theme")]
    public static void CreateDefaultTarotTheme()
    {
        var config = CreateInstance<ThemeConfig>();
        config.gameName = "Tarot Battlegrounds";
        config.themeId = "Tarot";

        config.tribes = new TribeThemeData[]
        {
            new TribeThemeData
            {
                tribeName = "Pentacles",
                description = "The suit of Earth and material wealth. Grants economic advantages.",
                themeColor = new Color(0.8f, 0.6f, 0.2f),
                aliases = new string[] { "pentacle", "earth", "coins" }
            },
            new TribeThemeData
            {
                tribeName = "Cups",
                description = "The suit of Water and emotions. Restores health and grants protection.",
                themeColor = new Color(0.3f, 0.5f, 0.9f),
                aliases = new string[] { "cup", "water", "chalice" }
            },
            new TribeThemeData
            {
                tribeName = "Swords",
                description = "The suit of Air and conflict. Deals devastating damage.",
                themeColor = new Color(0.7f, 0.7f, 0.8f),
                aliases = new string[] { "sword", "air", "blade" }
            },
            new TribeThemeData
            {
                tribeName = "Wands",
                description = "The suit of Fire and creation. Empowers allies with stat buffs.",
                themeColor = new Color(0.9f, 0.4f, 0.2f),
                aliases = new string[] { "wand", "fire", "staff" }
            }
        };

        string path = "Assets/Data/TarotTheme.asset";
        UnityEditor.AssetDatabase.CreateAsset(config, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.Selection.activeObject = config;
        Debug.Log($"[ThemeConfig] Created default Tarot theme at {path}");
    }
#endif
}

/// <summary>
/// Theme data for a single tribe/faction.
/// </summary>
[System.Serializable]
public class TribeThemeData
{
    [Tooltip("Display name for this tribe (e.g., 'Pentacles', 'Orcs', 'Robots')")]
    public string tribeName;

    [Tooltip("Flavor description for this tribe")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("Theme color for UI elements")]
    public Color themeColor = Color.white;

    [Tooltip("Alternative names that map to this tribe (for CSV import, etc.)")]
    public string[] aliases;

    [Tooltip("Icon/sprite for this tribe (optional)")]
    public Sprite tribeIcon;
}
