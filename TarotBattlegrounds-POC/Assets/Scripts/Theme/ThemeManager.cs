using UnityEngine;
using System;

/// <summary>
/// Singleton manager for game theming. Provides access to the active theme configuration.
/// Attach to a GameObject in the scene or let it auto-create.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [Header("Active Theme")]
    [Tooltip("The current theme configuration. Assign in Inspector or load at runtime.")]
    [SerializeField] private ThemeConfig _activeTheme;

    /// <summary>
    /// Event fired when theme changes (for UI refresh).
    /// </summary>
    public static event Action<ThemeConfig> OnThemeChanged;

    /// <summary>
    /// Get the active theme configuration.
    /// </summary>
    public static ThemeConfig ActiveTheme
    {
        get
        {
            if (Instance == null || Instance._activeTheme == null)
            {
                // Try to load default theme from Resources
                var defaultTheme = Resources.Load<ThemeConfig>("DefaultTheme");
                if (defaultTheme != null && Instance != null)
                {
                    Instance._activeTheme = defaultTheme;
                }
                else if (Instance != null)
                {
                    // Create a runtime default if no theme found
                    Instance._activeTheme = CreateDefaultTheme();
                }
            }
            return Instance?._activeTheme;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load default theme if none assigned
        if (_activeTheme == null)
        {
            _activeTheme = Resources.Load<ThemeConfig>("DefaultTheme");
            if (_activeTheme == null)
            {
                _activeTheme = CreateDefaultTheme();
            }
        }

        Debug.Log($"[ThemeManager] Initialized with theme: {_activeTheme?.gameName ?? "None"}");
    }

    /// <summary>
    /// Set a new active theme.
    /// </summary>
    public void SetTheme(ThemeConfig newTheme)
    {
        if (newTheme == null)
        {
            Debug.LogWarning("[ThemeManager] Cannot set null theme");
            return;
        }

        _activeTheme = newTheme;
        Debug.Log($"[ThemeManager] Theme changed to: {newTheme.gameName}");
        OnThemeChanged?.Invoke(newTheme);
    }

    /// <summary>
    /// Get display name for a tribe.
    /// </summary>
    public static string GetTribeName(TribeType tribe)
    {
        return ActiveTheme?.GetTribeName(tribe) ?? tribe.ToString();
    }

    /// <summary>
    /// Get description for a tribe.
    /// </summary>
    public static string GetTribeDescription(TribeType tribe)
    {
        return ActiveTheme?.GetTribeDescription(tribe) ?? "";
    }

    /// <summary>
    /// Get theme color for a tribe.
    /// </summary>
    public static Color GetTribeColor(TribeType tribe)
    {
        return ActiveTheme?.GetTribeColor(tribe) ?? Color.gray;
    }

    /// <summary>
    /// Parse a tribe name string using the active theme.
    /// </summary>
    public static TribeType ParseTribeName(string tribeName)
    {
        return ActiveTheme?.ParseTribeName(tribeName) ?? TribeType.None;
    }

    /// <summary>
    /// Ensure ThemeManager exists in the scene.
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance == null)
        {
            var go = new GameObject("ThemeManager");
            go.AddComponent<ThemeManager>();
        }
    }

    /// <summary>
    /// Create a default theme configuration at runtime.
    /// </summary>
    private static ThemeConfig CreateDefaultTheme()
    {
        var config = ScriptableObject.CreateInstance<ThemeConfig>();
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

        Debug.Log("[ThemeManager] Created default Tarot theme at runtime");
        return config;
    }
}
