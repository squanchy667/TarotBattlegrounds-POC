using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns button interactability state for GameUIManager: IgniteButton-aware interactable
/// toggling, the per-frame button enable/disable logic (incl. synergy-discounted buy cost),
/// and UX13 button micro-feedback attachment. Extracted from GameUIManager as part of the UI
/// god-class refactor (spec §2.2 item 3, 2026-07-04 batch).
///
/// `SetButtonInteractable` and `UpdateButtonStates` are public because PlayerActionController
/// (OnEndTurnClicked) calls the former through `owner.buttonState`.
/// </summary>
public class UIButtonStateController
{
    private readonly GameUIManager owner;

    public UIButtonStateController(GameUIManager owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Set button interactable state, using IgniteButton.SetInteractable() if available
    /// for tokenized disabled visuals, otherwise falling back to standard Button.interactable.
    /// </summary>
    public void SetButtonInteractable(Button button, bool interactable)
    {
        if (button == null) return;

        TarotBattlegrounds.UI.IgniteButton ignite = button.GetComponent<TarotBattlegrounds.UI.IgniteButton>();
        if (ignite != null)
        {
            ignite.SetInteractable(interactable);
        }
        else
        {
            button.interactable = interactable;
        }
    }

    /// <summary>
    /// Update button interactability based on current state.
    /// Moved verbatim from GameUIManager.UpdateButtonStates (incl. the synergy-discounted
    /// buy-cost calculation).
    /// </summary>
    public void UpdateButtonStates()
    {
        var player = owner.GetActivePlayer();
        if (player == null) return;

        bool isRecruitPhase = GameManager.Instance != null &&
                             GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;

        // Buy button: enabled if recruit phase, have coins, and card selected
        if (owner.buyButton != null)
        {
            int shopIndex = owner.shopUI != null ? owner.shopUI.GetSelectedCardIndex() : -1;
            int buyCost = 3; // Default cost
            if (shopIndex >= 0 && TavernManager.Instance != null &&
                TavernManager.Instance.availableCards.ContainsKey(player.playerId) &&
                shopIndex < TavernManager.Instance.availableCards[player.playerId].Count)
            {
                Card shopCard = TavernManager.Instance.availableCards[player.playerId][shopIndex];
                buyCost = Mathf.Max(0, 3 + shopCard.buyCostModifier);
                if (SynergyManager.Instance != null)
                {
                    var snapshot = SynergyManager.Instance.CalculateSynergies(player.board);
                    int reduction = SynergyManager.Instance.GetCostReduction(shopCard, snapshot);
                    if (reduction > 0)
                        buyCost = Mathf.Max(1, buyCost - reduction);
                }
            }
            SetButtonInteractable(owner.buyButton, isRecruitPhase && shopIndex >= 0 && player.coins >= buyCost && player.hand.Count < 10);
        }

        // Sell button: enabled if recruit phase and board OR hand card selected
        if (owner.sellButton != null)
        {
            int boardIndex = owner.boardUI != null ? owner.boardUI.GetSelectedCardIndex() : -1;
            int handIndex = owner.handUI != null ? owner.handUI.GetSelectedCardIndex() : -1;
            SetButtonInteractable(owner.sellButton, isRecruitPhase && (boardIndex >= 0 || handIndex >= 0));
        }

        // Play button: enabled if recruit phase, hand card selected, and board not full
        if (owner.playCardButton != null)
        {
            int handIndex = owner.handUI != null ? owner.handUI.GetSelectedCardIndex() : -1;
            SetButtonInteractable(owner.playCardButton, isRecruitPhase && handIndex >= 0 && player.board.Count < 7);
        }

        // Refresh button: enabled if recruit phase and have 1+ coins
        if (owner.refreshButton != null)
        {
            SetButtonInteractable(owner.refreshButton, isRecruitPhase && player.coins >= 1);
        }

        // Upgrade button: enabled if recruit phase, have enough coins, and not max tier
        if (owner.upgradeButton != null)
        {
            SetButtonInteractable(owner.upgradeButton, isRecruitPhase &&
                                         player.coins >= player.GetUpgradeCost() &&
                                         player.currentTavernTier < 6);
        }

        // End Turn button: enabled during recruit phase if this player hasn't already readied
        if (owner.endTurnButton != null)
        {
            bool alreadyReady = GameManager.Instance != null &&
                                GameManager.Instance.IsPlayerReady(owner.GetActivePlayerIndex());
            SetButtonInteractable(owner.endTurnButton, isRecruitPhase && !alreadyReady);

            // Update button text to reflect state
            if (owner.endTurnButtonText != null)
            {
                if (alreadyReady)
                {
                    owner.endTurnButtonText.text = "Waiting...";
                }
                else if (owner.themeApplier.CurrentTheme != null)
                {
                    owner.endTurnButtonText.text = owner.themeApplier.CurrentTheme.endTurnButtonText;
                }
                else
                {
                    owner.endTurnButtonText.text = "End Turn";
                }
            }
        }

        // Freeze Shop button: enabled during recruit phase
        if (owner.freezeShopButton != null)
        {
            SetButtonInteractable(owner.freezeShopButton, isRecruitPhase);
        }
    }

    /// <summary>
    /// T750: Auto-attach IgniteButton to all child Button components that don't
    /// already have one, binding the design-system button sprites and label
    /// (DESIGN.md §10.5 — one interaction component everywhere, no scaling).
    /// Replaces the UX13 ButtonMicroFeedback attachment.
    /// </summary>
    public void AttachIgniteButtons()
    {
        var sprites = TarotBattlegrounds.UI.UiSprites.Instance;
        Button[] allButtons = owner.GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            if (btn.GetComponent<TarotBattlegrounds.UI.IgniteButton>() != null) continue;

            var ignite = btn.gameObject.AddComponent<TarotBattlegrounds.UI.IgniteButton>();
            var image = btn.GetComponent<UnityEngine.UI.Image>();
            var label = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (sprites != null && image != null)
            {
                TarotBattlegrounds.UI.UiSprites.ApplySliced(image, sprites.ButtonBronze);
                ignite.Bind(image, sprites.ButtonBronze, sprites.ButtonEmber,
                    sprites.ButtonBronzePressed, label);
            }
            else
            {
                ignite.Bind(null, null, null, null, label);
            }
        }
    }
}
