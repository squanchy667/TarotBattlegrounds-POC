using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Owns theme state and application for GameUIManager: button/text theming, panel and
/// background colors, and TarotButton propagation. Extracted from GameUIManager as part
/// of the UI god-class refactor (spec §2.2 item 1, 2026-07-04 batch).
///
/// Holds `currentTheme` (exposed read-only via <see cref="CurrentTheme"/>) so other helpers
/// (HudPresenter, UIButtonStateController) can read the active theme without GameUIManager
/// itself owning theme state.
/// </summary>
public class GameUIThemeApplier
{
    private readonly GameUIManager owner;
    private ThemeConfig currentTheme;

    public ThemeConfig CurrentTheme => currentTheme;

    public GameUIThemeApplier(GameUIManager owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Apply theme to the game UI. Moved verbatim from GameUIManager.ApplyTheme.
    /// The freeze-button text update and the post-theme display refresh now route through
    /// HudPresenter (owner.hud) since that helper owns those methods (spec §2.6).
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply button text from theme
        if (owner.buyButtonText != null) owner.buyButtonText.text = theme.buyButtonText;
        if (owner.sellButtonText != null) owner.sellButtonText.text = theme.sellButtonText;
        if (owner.playButtonText != null) owner.playButtonText.text = theme.playButtonText;
        if (owner.refreshButtonText != null) owner.refreshButtonText.text = theme.rerollButtonText;
        if (owner.upgradeButtonText != null) owner.upgradeButtonText.text = theme.upgradeButtonText;
        if (owner.endTurnButtonText != null) owner.endTurnButtonText.text = theme.endTurnButtonText;

        // Update freeze button text based on current state
        owner.hud.UpdateFreezeButtonText();

        // Apply colors to UI elements
        ApplyThemeColors(theme);

        // Apply background: delegate to BackgroundController if available, otherwise fallback
        if (owner.backgroundController != null)
        {
            // BackgroundController handles its own theming via IThemeable subscription
            // but we also forward here for safety in case of initialization order
            owner.backgroundController.ApplyTheme(theme);
        }
        else if (owner.gameBackgroundImage != null)
        {
            if (theme.gameBackground != null)
            {
                owner.gameBackgroundImage.sprite = theme.gameBackground;
                owner.gameBackgroundImage.color = Color.white;
            }
            else
            {
                owner.gameBackgroundImage.sprite = null;
                owner.gameBackgroundImage.color = theme.gameBackgroundColor;
            }
        }

        // Apply theme to all TarotButton children
        ApplyThemeToTarotButtons(theme);

        // Re-update display to use themed labels
        owner.hud.UpdatePlayerDisplay();
        owner.hud.UpdatePhaseDisplay();
    }

    /// <summary>
    /// Propagate theme to all TarotButton components found in children.
    /// Moved verbatim from GameUIManager.ApplyThemeToTarotButtons. GetComponentsInChildren is
    /// inherited from Component/MonoBehaviour, so this plain class calls it through `owner`.
    /// </summary>
    private void ApplyThemeToTarotButtons(ThemeConfig theme)
    {
        TarotButton[] tarotButtons = owner.GetComponentsInChildren<TarotButton>(true);
        foreach (TarotButton tb in tarotButtons)
        {
            tb.ApplyTheme(theme);
        }
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.ApplyThemeColors.
    /// </summary>
    private void ApplyThemeColors(ThemeConfig theme)
    {
        // Apply primary color to buttons
        Color buttonColor = theme.primaryColor;
        ApplyButtonColor(owner.buyButton, buttonColor);
        ApplyButtonColor(owner.sellButton, buttonColor);
        ApplyButtonColor(owner.playCardButton, buttonColor);
        ApplyButtonColor(owner.refreshButton, buttonColor);
        ApplyButtonColor(owner.upgradeButton, buttonColor);
        ApplyButtonColor(owner.endTurnButton, buttonColor);
        ApplyButtonColor(owner.freezeShopButton, buttonColor);

        // Apply panel background — let StyledPanel handle it if present
        if (owner.mainStyledPanel == null && owner.mainPanelBackground != null)
            owner.mainPanelBackground.color = theme.secondaryColor;

        // Apply text colors
        Color lightText = theme.textColorLight;
        if (owner.phaseText != null) owner.phaseText.color = lightText;
        if (owner.timerText != null) owner.timerText.color = theme.accentColor;
        if (owner.turnText != null) owner.turnText.color = lightText;
        if (owner.playerNameText != null) owner.playerNameText.color = lightText;

        // Stats with semantic colors
        if (owner.coinsText != null) owner.coinsText.color = theme.accentColor;
        if (owner.healthText != null) owner.healthText.color = theme.positiveColor;
        if (owner.tierText != null) owner.tierText.color = lightText;
        if (owner.upgradeCostText != null) owner.upgradeCostText.color = theme.accentColor;
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.ApplyButtonColor.
    /// </summary>
    private void ApplyButtonColor(Button button, Color color)
    {
        if (button == null) return;

        // Skip color block changes if TarotButton handles visuals
        if (button.GetComponent<TarotButton>() != null) return;

        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color;
        button.colors = colors;
    }
}
