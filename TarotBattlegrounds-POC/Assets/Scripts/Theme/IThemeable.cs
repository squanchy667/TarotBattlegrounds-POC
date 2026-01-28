using UnityEngine;

/// <summary>
/// Interface for UI components that can be themed.
/// Implement this on any MonoBehaviour that should respond to theme changes.
/// </summary>
public interface IThemeable
{
    /// <summary>
    /// Apply the given theme configuration to this component.
    /// Called when theme changes or on initialization.
    /// </summary>
    void ApplyTheme(ThemeConfig theme);
}

/// <summary>
/// Base class for themeable UI components.
/// Automatically subscribes to theme changes and applies theme on start.
/// </summary>
public abstract class ThemeableUI : MonoBehaviour, IThemeable
{
    protected virtual void OnEnable()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += ApplyTheme;

        // Apply current theme
        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    protected virtual void OnDisable()
    {
        // Unsubscribe from theme changes
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply the theme to this component.
    /// Override in derived classes to customize theming behavior.
    /// </summary>
    public abstract void ApplyTheme(ThemeConfig theme);
}
