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

    [Header("Color Palette")]
    [Tooltip("Primary UI color (buttons, highlights)")]
    public Color primaryColor = new Color(0.6f, 0.4f, 0.8f); // Purple
    [Tooltip("Secondary UI color (backgrounds, panels)")]
    public Color secondaryColor = new Color(0.2f, 0.2f, 0.3f); // Dark blue-gray
    [Tooltip("Accent color (important elements, warnings)")]
    public Color accentColor = new Color(1f, 0.8f, 0.2f); // Gold
    [Tooltip("Text color on dark backgrounds")]
    public Color textColorLight = Color.white;
    [Tooltip("Text color on light backgrounds")]
    public Color textColorDark = new Color(0.1f, 0.1f, 0.1f);
    [Tooltip("Positive feedback color (health, buffs)")]
    public Color positiveColor = new Color(0.3f, 0.8f, 0.3f); // Green
    [Tooltip("Negative feedback color (damage, debuffs)")]
    public Color negativeColor = new Color(0.9f, 0.3f, 0.3f); // Red

    [Header("Card Visuals")]
    [Tooltip("Card frame sprite for tier 1-2 cards")]
    public Sprite cardFrameCommon;
    [Tooltip("Card frame sprite for tier 3-4 cards")]
    public Sprite cardFrameRare;
    [Tooltip("Card frame sprite for tier 5-6 cards")]
    public Sprite cardFrameEpic;
    [Tooltip("Card back sprite")]
    public Sprite cardBack;
    [Tooltip("Default card background color")]
    public Color cardBackgroundColor = new Color(0.15f, 0.15f, 0.2f);

    [Header("UI Panels")]
    [Tooltip("Panel background sprite")]
    public Sprite panelBackground;
    [Tooltip("Button normal sprite")]
    public Sprite buttonNormal;
    [Tooltip("Button highlighted sprite")]
    public Sprite buttonHighlighted;
    [Tooltip("Button pressed sprite")]
    public Sprite buttonPressed;
    [Tooltip("Button disabled sprite")]
    public Sprite buttonDisabled;

    [Header("Icons")]
    [Tooltip("Coin/gold icon")]
    public Sprite coinIcon;
    [Tooltip("Health/heart icon")]
    public Sprite healthIcon;
    [Tooltip("Attack/sword icon")]
    public Sprite attackIcon;
    [Tooltip("Shield/aegis icon")]
    public Sprite shieldIcon;

    [Header("Fonts (Optional)")]
    [Tooltip("Main title font")]
    public TMPro.TMP_FontAsset titleFont;
    [Tooltip("Body text font")]
    public TMPro.TMP_FontAsset bodyFont;
    [Tooltip("Stats/numbers font")]
    public TMPro.TMP_FontAsset statsFont;

    /// <summary>
    /// Get the appropriate card frame based on tier.
    /// </summary>
    public Sprite GetCardFrame(int tier)
    {
        if (tier <= 2) return cardFrameCommon;
        if (tier <= 4) return cardFrameRare;
        return cardFrameEpic;
    }

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

        // Tarot-specific UI text
        config.shopTitle = "Tavern";
        config.handTitle = "Hand";
        config.boardTitle = "Battlefield";
        config.coinsLabel = "Gold";
        config.healthLabel = "Life";
        config.tierLabel = "Tier";
        config.buyButtonText = "Buy";
        config.sellButtonText = "Sell";
        config.playButtonText = "Deploy";
        config.rerollButtonText = "Reroll";
        config.upgradeButtonText = "Upgrade";
        config.endTurnButtonText = "End Turn";
        config.combatPhaseTitle = "Combat Phase";
        config.recruitPhaseTitle = "Recruit Phase";
        config.victoryText = "Victory!";
        config.defeatText = "Defeat!";
        config.tieText = "Draw!";

        // Tarot color palette (mystical purple/gold theme)
        config.primaryColor = new Color(0.6f, 0.4f, 0.8f);      // Purple
        config.secondaryColor = new Color(0.15f, 0.12f, 0.2f);  // Dark purple
        config.accentColor = new Color(1f, 0.8f, 0.2f);         // Gold
        config.textColorLight = Color.white;
        config.textColorDark = new Color(0.1f, 0.1f, 0.15f);
        config.positiveColor = new Color(0.3f, 0.8f, 0.3f);     // Green
        config.negativeColor = new Color(0.9f, 0.3f, 0.3f);     // Red
        config.cardBackgroundColor = new Color(0.12f, 0.1f, 0.18f);

        config.tribes = new TribeThemeData[]
        {
            new TribeThemeData
            {
                tribeName = "Pentacles",
                description = "The suit of Earth and material wealth. Grants economic advantages.",
                themeColor = new Color(0.85f, 0.65f, 0.2f),  // Gold
                aliases = new string[] { "pentacle", "earth", "coins" }
            },
            new TribeThemeData
            {
                tribeName = "Cups",
                description = "The suit of Water and emotions. Restores health and grants protection.",
                themeColor = new Color(0.3f, 0.5f, 0.9f),    // Blue
                aliases = new string[] { "cup", "water", "chalice" }
            },
            new TribeThemeData
            {
                tribeName = "Swords",
                description = "The suit of Air and conflict. Deals devastating damage.",
                themeColor = new Color(0.75f, 0.75f, 0.85f), // Silver
                aliases = new string[] { "sword", "air", "blade" }
            },
            new TribeThemeData
            {
                tribeName = "Wands",
                description = "The suit of Fire and creation. Empowers allies with stat buffs.",
                themeColor = new Color(0.9f, 0.4f, 0.2f),    // Orange/Fire
                aliases = new string[] { "wand", "fire", "staff" }
            }
        };

        // Ensure Resources folder exists
        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Save as DefaultTheme in Resources for auto-loading
        string resourcesPath = "Assets/Resources/DefaultTheme.asset";
        UnityEditor.AssetDatabase.CreateAsset(config, resourcesPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.Selection.activeObject = config;
        Debug.Log($"[ThemeConfig] Created default Tarot theme at {resourcesPath}");
    }

    /// <summary>
    /// Create a debug/generic theme for testing.
    /// </summary>
    [UnityEditor.MenuItem("Game/Create Debug Theme")]
    public static void CreateDebugTheme()
    {
        var config = CreateInstance<ThemeConfig>();
        config.gameName = "Auto Battler";
        config.themeId = "Debug";

        // Generic UI text
        config.shopTitle = "Shop";
        config.handTitle = "Hand";
        config.boardTitle = "Board";
        config.coinsLabel = "Coins";
        config.healthLabel = "HP";
        config.tierLabel = "Tier";
        config.buyButtonText = "Buy";
        config.sellButtonText = "Sell";
        config.playButtonText = "Play";
        config.rerollButtonText = "Reroll";
        config.upgradeButtonText = "Upgrade";
        config.endTurnButtonText = "End Turn";
        config.combatPhaseTitle = "Combat";
        config.recruitPhaseTitle = "Recruit";

        // Debug color palette (high contrast for testing)
        config.primaryColor = new Color(0.2f, 0.6f, 0.9f);     // Blue
        config.secondaryColor = new Color(0.15f, 0.15f, 0.2f); // Dark gray
        config.accentColor = new Color(1f, 0.9f, 0.2f);        // Yellow
        config.textColorLight = Color.white;
        config.textColorDark = Color.black;
        config.positiveColor = Color.green;
        config.negativeColor = Color.red;
        config.cardBackgroundColor = new Color(0.2f, 0.2f, 0.25f);

        // Generic tribe names
        config.tribes = new TribeThemeData[]
        {
            new TribeThemeData
            {
                tribeName = "Tribe A",
                description = "First tribe type",
                themeColor = Color.yellow,
                aliases = new string[] { "a", "tribea" }
            },
            new TribeThemeData
            {
                tribeName = "Tribe B",
                description = "Second tribe type",
                themeColor = Color.blue,
                aliases = new string[] { "b", "tribeb" }
            },
            new TribeThemeData
            {
                tribeName = "Tribe C",
                description = "Third tribe type",
                themeColor = Color.gray,
                aliases = new string[] { "c", "tribec" }
            },
            new TribeThemeData
            {
                tribeName = "Tribe D",
                description = "Fourth tribe type",
                themeColor = new Color(1f, 0.5f, 0f),
                aliases = new string[] { "d", "tribed" }
            }
        };

        // Ensure Data folder exists
        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Data"))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Data");
        }

        string path = "Assets/Data/DebugTheme.asset";
        UnityEditor.AssetDatabase.CreateAsset(config, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.Selection.activeObject = config;
        Debug.Log($"[ThemeConfig] Created debug theme at {path}");
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
