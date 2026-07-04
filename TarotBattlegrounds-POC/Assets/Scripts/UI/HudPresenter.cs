using UnityEngine;

/// <summary>
/// Owns HUD presentation state for GameUIManager: phase/turn display + UX11 banner state,
/// player stat display, the UX10 circular timer, synergy panel refresh, and freeze-button
/// text. Extracted from GameUIManager as part of the UI god-class refactor (spec §2.2 item 2,
/// 2026-07-04 batch).
///
/// Takes the sibling GameUIThemeApplier directly via constructor (rather than reaching through
/// `owner.themeApplier`) since every method here reads the active theme.
/// </summary>
public class HudPresenter
{
    private readonly GameUIManager owner;
    private readonly GameUIThemeApplier theme;

    private float circularTimerTotal = 35f; // UX10: Tracked total time for circular timer
    private GameManager.GamePhase lastBannerPhase = (GameManager.GamePhase)(-1); // UX11: Track phase for banner triggers
    private int lastBannerTurn = -1; // UX11: Track turn for badge updates

    public HudPresenter(GameUIManager owner, GameUIThemeApplier theme)
    {
        this.owner = owner;
        this.theme = theme;
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.UpdatePhaseDisplay. `currentTheme` reads now go
    /// through `theme.CurrentTheme` (spec §2.6 reader migration).
    /// </summary>
    public void UpdatePhaseDisplay()
    {
        if (GameManager.Instance == null) return;

        GameManager.GamePhase currentPhase = GameManager.Instance.CurrentPhase;
        int currentTurn = GameManager.Instance.TurnNumber;

        if (owner.phaseText != null)
        {
            // Use themed phase names if available
            if (theme.CurrentTheme != null)
            {
                owner.phaseText.text = currentPhase == GameManager.GamePhase.Combat
                    ? theme.CurrentTheme.combatPhaseTitle
                    : theme.CurrentTheme.recruitPhaseTitle;
            }
            else
            {
                owner.phaseText.text = currentPhase.ToString();
            }
        }
        if (owner.turnText != null)
            owner.turnText.text = $"Turn {currentTurn}";

        // UX11: Show phase banner on phase change
        if (owner.phaseBanner != null && currentPhase != lastBannerPhase)
        {
            bool isCombat = currentPhase == GameManager.GamePhase.Combat;
            string bannerLabel;
            if (theme.CurrentTheme != null)
            {
                bannerLabel = isCombat ? theme.CurrentTheme.combatPhaseTitle : theme.CurrentTheme.recruitPhaseTitle;
            }
            else
            {
                bannerLabel = isCombat ? "COMBAT!" : "RECRUIT PHASE";
            }
            owner.phaseBanner.ShowPhaseBanner(bannerLabel, isCombat);
            lastBannerPhase = currentPhase;
        }

        // UX11: Update turn badge on turn change
        if (owner.phaseBanner != null && currentTurn != lastBannerTurn)
        {
            owner.phaseBanner.UpdateTurn(currentTurn);
            lastBannerTurn = currentTurn;
        }
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.UpdatePlayerDisplay.
    /// </summary>
    public void UpdatePlayerDisplay()
    {
        var player = owner.GetActivePlayer();
        if (player == null) return;

        if (owner.playerNameText != null)
            owner.playerNameText.text = $"Player {player.playerId}";

        // UX09: Route resource display through ResourceBar if available
        if (owner.resourceBar != null)
        {
            owner.resourceBar.UpdateCoins(player.coins, Player.MAX_COINS);
            owner.resourceBar.UpdateHealth(player.Health);
            owner.resourceBar.UpdateTier(player.currentTavernTier, player.GetUpgradeCost());
        }

        // Backward compat: still update legacy text fields when ResourceBar is not wired
        // Get themed labels or use defaults
        string coinsLabel = theme.CurrentTheme != null ? theme.CurrentTheme.coinsLabel : "Coins";
        string tierLabel = theme.CurrentTheme != null ? theme.CurrentTheme.tierLabel : "Tier";
        string healthLabel = theme.CurrentTheme != null ? theme.CurrentTheme.healthLabel : "Health";

        if (owner.coinsText != null)
            owner.coinsText.text = $"{coinsLabel}: {player.coins}";
        if (owner.tierText != null)
            owner.tierText.text = $"{tierLabel}: {player.currentTavernTier}";
        if (owner.upgradeCostText != null)
        {
            if (player.currentTavernTier >= 6)
            {
                string maxText = theme.CurrentTheme != null ? theme.CurrentTheme.maxTierText : "MAX";
                owner.upgradeCostText.text = $"Upgrade: {maxText}";
            }
            else
            {
                owner.upgradeCostText.text = $"Upgrade: {player.GetUpgradeCost()}g";
            }
        }

        // Use player's Health property
        if (owner.healthText != null)
        {
            int health = player.Health;
            owner.healthText.text = $"{healthLabel}: {health}";
        }
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.UpdateTimer. Facade's public UpdateTimer(float)
    /// delegates here; owns `circularTimerTotal` (spec §2.2 item 2).
    /// </summary>
    public void UpdateTimer(float time)
    {
        if (owner.timerText != null)
        {
            int display = time <= 0f ? 0 : Mathf.CeilToInt(time);
            owner.timerText.text = $"{display}s";
        }

        // UX10: Track the max time seen as the total (first call each phase has the full value)
        if (time > circularTimerTotal)
            circularTimerTotal = time;

        // UX10: Update circular timer
        if (owner.circularTimer != null)
        {
            owner.circularTimer.SetTime(time, circularTimerTotal);
        }
    }

    /// <summary>
    /// Refresh the synergy display panel with current player's board state.
    /// Moved verbatim from GameUIManager.RefreshSynergyDisplay.
    /// </summary>
    public void RefreshSynergyDisplay()
    {
        if (owner.synergyDisplay == null) return;
        var player = owner.GetActivePlayer();
        if (player != null)
            owner.synergyDisplay.UpdateSynergies(player);
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.UpdateFreezeButtonText. `currentTheme` reads now go
    /// through `theme.CurrentTheme` (spec §2.6 reader migration) — this is the method the
    /// spec's §2.2 item 1 calls out as "the theme half"; GameUIThemeApplier.ApplyTheme calls
    /// out to it via `owner.hud.UpdateFreezeButtonText()` rather than owning the body itself
    /// (see the spec-mismatch note in the final report).
    /// </summary>
    public void UpdateFreezeButtonText()
    {
        if (owner.freezeShopButtonText == null) return;

        var player = owner.GetActivePlayer();
        bool frozen = player != null && player.ShopFrozen;

        if (theme.CurrentTheme != null)
        {
            owner.freezeShopButtonText.text = frozen ? theme.CurrentTheme.unfreezeButtonText : theme.CurrentTheme.freezeButtonText;
        }
        else
        {
            owner.freezeShopButtonText.text = frozen ? "Unfreeze" : "Freeze";
        }
    }
}
