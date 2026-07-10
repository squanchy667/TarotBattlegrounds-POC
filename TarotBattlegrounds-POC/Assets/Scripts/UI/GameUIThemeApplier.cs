using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Owns theme state and application for GameUIManager: button/text theming, panel and
/// background art, and label copy. Extracted from GameUIManager as part
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
                owner.gameBackgroundImage.color = TarotBattlegrounds.UI.Tokens.Ash;
            }
        }

        // Re-update display to use themed labels
        owner.hud.UpdatePlayerDisplay();
        owner.hud.UpdatePhaseDisplay();
    }

    /// <summary>
    /// T750: chrome/text colors come from Tokens, not the theme — themes contribute
    /// copy (button labels) and background art only; tribe accents arrive via
    /// TribeTheme in T754 (DESIGN.md §9). Button visuals are owned by IgniteButton.
    /// The theme parameter remains for the label lookups above.
    /// </summary>
    private void ApplyThemeColors(ThemeConfig theme)
    {
        // Panel background — let StyledPanel handle it if present
        if (owner.mainStyledPanel == null && owner.mainPanelBackground != null)
            owner.mainPanelBackground.color = TarotBattlegrounds.UI.Tokens.CharredWood;

        // Text colors by role (DESIGN.md §3): key numbers BoneBright, labels Bone,
        // worth/cost BronzeBright.
        if (owner.phaseText != null) owner.phaseText.color = TarotBattlegrounds.UI.Tokens.BoneBright;
        if (owner.timerText != null) owner.timerText.color = TarotBattlegrounds.UI.Tokens.BronzeBright;
        if (owner.turnText != null) owner.turnText.color = TarotBattlegrounds.UI.Tokens.Bone;
        if (owner.playerNameText != null) owner.playerNameText.color = TarotBattlegrounds.UI.Tokens.BoneBright;

        if (owner.coinsText != null) owner.coinsText.color = TarotBattlegrounds.UI.Tokens.BronzeBright;
        if (owner.healthText != null) owner.healthText.color = TarotBattlegrounds.UI.Tokens.BoneBright;
        if (owner.tierText != null) owner.tierText.color = TarotBattlegrounds.UI.Tokens.Bone;
        if (owner.upgradeCostText != null) owner.upgradeCostText.color = TarotBattlegrounds.UI.Tokens.BronzeBright;
    }
}
