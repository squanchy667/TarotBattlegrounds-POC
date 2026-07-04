using UnityEngine;
using TarotBattlegrounds.UI;

/// <summary>
/// Owns action execution for GameUIManager: offline card buy/sell/play/refresh/upgrade
/// (incl. UX15 buy/sell/play animations), online action routing through NetworkGameBridge,
/// board empty-slot/swap requests, end-turn/freeze-shop click handling, and UX15 animation
/// reparenting. Extracted from GameUIManager as part of the UI god-class refactor (spec §2.2
/// item 4, 2026-07-04 batch) — the largest single win (~280 lines).
///
/// Reaches into the sibling helpers via `owner.buttonState` / `owner.hud` since this class is
/// constructed last (after those fields are already populated in GameUIManager.Awake).
/// </summary>
public class PlayerActionController
{
    private readonly GameUIManager owner;

    public PlayerActionController(GameUIManager owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.ExecuteAction.
    /// </summary>
    public void ExecuteAction(string action)
    {
        var player = owner.GetActivePlayer();
        if (player == null) return;

#if PHOTON_UNITY_NETWORKING
        // In online mode, route through NetworkGameBridge
        if (owner.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            ExecuteNetworkAction(action, player);
            return;
        }
#endif

        // Offline mode: execute directly
        switch (action)
        {
            case "Buy":
                if (owner.shopUI != null)
                {
                    int selectedIndex = owner.shopUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        // UX15: Animate card buy (shop → hand) if UIAnimator is available
                        RectTransform shopCardRect = owner.shopUI.GetSelectedCardRect();
                        RectTransform handTarget = owner.handUI != null ? owner.handUI.GetContainerRect() : null;

                        if (UIAnimator.Instance != null && shopCardRect != null && handTarget != null)
                        {
                            // Reparent card to canvas overlay so it survives shop refresh
                            RectTransform animCard = ReparentForAnimation(shopCardRect);
                            if (animCard != null)
                            {
                                player.BuyCard(selectedIndex);
                                UIAnimator.Instance.AnimateCardBuy(animCard, handTarget, () =>
                                {
                                    if (animCard != null) Object.Destroy(animCard.gameObject);
                                });
                            }
                            else
                            {
                                player.BuyCard(selectedIndex);
                            }
                        }
                        else
                        {
                            player.BuyCard(selectedIndex);
                        }
                    }
                    else
                    {
                        Debug.Log("Select a card from the shop first!");
                    }
                }
                break;

            case "Sell":
                int boardSellIndex = owner.boardUI != null ? owner.boardUI.GetSelectedCardIndex() : -1;
                int handSellIndex = owner.handUI != null ? owner.handUI.GetSelectedCardIndex() : -1;

                if (boardSellIndex >= 0)
                {
                    // UX15: Animate card sell from board
                    RectTransform boardCardRect = owner.boardUI != null ? owner.boardUI.GetSelectedCardRect() : null;
                    if (UIAnimator.Instance != null && boardCardRect != null)
                    {
                        RectTransform animCard = ReparentForAnimation(boardCardRect);
                        if (animCard != null)
                        {
                            int capturedIndex = boardSellIndex;
                            UIAnimator.Instance.AnimateCardSell(animCard, () =>
                            {
                                player.SellCard(capturedIndex);
                                if (animCard != null) Object.Destroy(animCard.gameObject);
                            });
                        }
                        else
                        {
                            player.SellCard(boardSellIndex);
                        }
                    }
                    else
                    {
                        player.SellCard(boardSellIndex);
                    }
                }
                else if (handSellIndex >= 0)
                {
                    // UX15: Animate card sell from hand
                    RectTransform handCardRect = owner.handUI != null ? owner.handUI.GetSelectedCardRect() : null;
                    if (UIAnimator.Instance != null && handCardRect != null)
                    {
                        RectTransform animCard = ReparentForAnimation(handCardRect);
                        if (animCard != null)
                        {
                            int capturedIndex = handSellIndex;
                            UIAnimator.Instance.AnimateCardSell(animCard, () =>
                            {
                                player.SellCardFromHand(capturedIndex);
                                if (animCard != null) Object.Destroy(animCard.gameObject);
                            });
                        }
                        else
                        {
                            player.SellCardFromHand(handSellIndex);
                        }
                    }
                    else
                    {
                        player.SellCardFromHand(handSellIndex);
                    }
                }
                else
                {
                    Debug.Log("Select a card from your board or hand first!");
                }
                break;

            case "Play":
                if (owner.handUI != null)
                {
                    int selectedIndex = owner.handUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        // UX15: Animate card play (hand → board)
                        RectTransform handCardRect = owner.handUI.GetSelectedCardRect();
                        RectTransform boardTarget = owner.boardUI != null ? owner.boardUI.GetContainerRect() : null;

                        if (UIAnimator.Instance != null && handCardRect != null && boardTarget != null)
                        {
                            RectTransform animCard = ReparentForAnimation(handCardRect);
                            if (animCard != null)
                            {
                                int capturedIndex = selectedIndex;
                                int boardCount = player.board.Count;
                                UIAnimator.Instance.AnimateCardPlay(animCard, boardTarget, () =>
                                {
                                    player.PlayCard(capturedIndex, boardCount);
                                    if (animCard != null) Object.Destroy(animCard.gameObject);
                                });
                            }
                            else
                            {
                                player.PlayCard(selectedIndex, player.board.Count);
                            }
                        }
                        else
                        {
                            player.PlayCard(selectedIndex, player.board.Count);
                        }
                    }
                    else
                    {
                        Debug.Log("Select a card from your hand first!");
                    }
                }
                break;

            case "Refresh":
                player.RefreshTavernShop();
                break;

            case "Upgrade":
                player.UpgradeTavern();
                break;
        }
    }

#if PHOTON_UNITY_NETWORKING
    /// <summary>
    /// Moved verbatim from GameUIManager.ExecuteNetworkAction.
    /// </summary>
    private void ExecuteNetworkAction(string action, Player player)
    {
        var bridge = NetworkGameBridge.Instance;

        switch (action)
        {
            case "Buy":
                if (owner.shopUI != null)
                {
                    int selectedIndex = owner.shopUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                        bridge.RequestBuyCard(selectedIndex);
                    else
                        Debug.Log("Select a card from the shop first!");
                }
                break;

            case "Sell":
                int boardSellIndex = owner.boardUI != null ? owner.boardUI.GetSelectedCardIndex() : -1;
                int handSellIndex = owner.handUI != null ? owner.handUI.GetSelectedCardIndex() : -1;

                if (boardSellIndex >= 0)
                    bridge.RequestSellBoardCard(boardSellIndex);
                else if (handSellIndex >= 0)
                    bridge.RequestSellHandCard(handSellIndex);
                else
                    Debug.Log("Select a card from your board or hand first!");
                break;

            case "Play":
                if (owner.handUI != null)
                {
                    int selectedIndex = owner.handUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                        bridge.RequestPlayCard(selectedIndex, player.board.Count);
                    else
                        Debug.Log("Select a card from your hand first!");
                }
                break;

            case "Refresh":
                bridge.RequestRerollShop();
                break;

            case "Upgrade":
                bridge.RequestUpgradeTavern();
                break;
        }
    }
#endif

    /// <summary>
    /// Moved verbatim from GameUIManager.OnBoardEmptySlotSelected. Public — subscribed as a
    /// delegate to boardUI.OnEmptySlotSelected from GameUIManager.Start().
    /// </summary>
    public void OnBoardEmptySlotSelected(int slotPosition)
    {
        if (owner.handUI == null) return;
        int handIndex = owner.handUI.GetSelectedCardIndex();
        if (handIndex < 0) return;

        var player = owner.GetActivePlayer();
        if (player == null) return;

        bool isRecruitPhase = GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        if (!isRecruitPhase) return;

        if (player.board.Count < 7)
        {
#if PHOTON_UNITY_NETWORKING
            if (owner.IsOnlineMode && NetworkGameBridge.Instance != null)
            {
                NetworkGameBridge.Instance.RequestPlayCard(handIndex, slotPosition);
            }
            else
#endif
            {
                player.PlayCard(handIndex, slotPosition);
            }
            owner.handUI.ClearSelection();
            Debug.Log($"Played hand card {handIndex} to board slot {slotPosition}");
        }
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.OnBoardSwapRequested. Public — subscribed as a
    /// delegate to boardUI.OnBoardSwapRequested from GameUIManager.Start().
    /// </summary>
    public void OnBoardSwapRequested(int indexA, int indexB)
    {
        var player = owner.GetActivePlayer();
        if (player == null) return;

        bool isRecruitPhase = GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        if (!isRecruitPhase) return;

#if PHOTON_UNITY_NETWORKING
        if (owner.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestSwapBoardCards(indexA, indexB);
        }
        else
#endif
        {
            player.SwapBoardCards(indexA, indexB);
        }
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.OnEndTurnClicked. Public — wired from a facade click
    /// lambda in GameUIManager.SetupButtons().
    /// </summary>
    public void OnEndTurnClicked()
    {
        if (GameManager.Instance == null) return;

#if PHOTON_UNITY_NETWORKING
        if (owner.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestEndTurn();
        }
        else
#endif
        {
            int playerIndex = owner.GetActivePlayerIndex();
            GameManager.Instance.PlayerReadyForCombat(playerIndex);
        }

        // Disable button and show waiting state
        owner.buttonState.SetButtonInteractable(owner.endTurnButton, false);
        if (owner.endTurnButtonText != null)
            owner.endTurnButtonText.text = "Waiting...";
    }

    /// <summary>
    /// Moved verbatim from GameUIManager.OnFreezeShopClicked. Public — wired from a facade
    /// click lambda in GameUIManager.SetupButtons().
    /// </summary>
    public void OnFreezeShopClicked()
    {
#if PHOTON_UNITY_NETWORKING
        if (owner.IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestToggleFreeze();
        }
        else
#endif
        {
            var player = owner.GetActivePlayer();
            if (player != null)
            {
                player.ToggleShopFreeze();
            }
        }
        owner.hud.UpdateFreezeButtonText();
    }

    /// <summary>
    /// UX15: Reparent a card RectTransform to the root canvas so it survives panel refreshes
    /// during animation. Returns the reparented RectTransform, or null if no canvas is found.
    /// Moved verbatim from GameUIManager.ReparentForAnimation. `FindObjectOfType` is a static
    /// UnityEngine.Object member, so it is qualified here (this class is not a Component).
    /// </summary>
    private RectTransform ReparentForAnimation(RectTransform cardRect)
    {
        if (cardRect == null) return null;

        Canvas rootCanvas = cardRect.GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = Object.FindObjectOfType<Canvas>();
        if (rootCanvas == null) return null;

        // Preserve world position when reparenting
        cardRect.SetParent(rootCanvas.transform, true);

        // Ensure it renders on top
        cardRect.SetAsLastSibling();

        // Disable raycasting on the animated card so it doesn't block UI
        CanvasGroup group = cardRect.GetComponent<CanvasGroup>();
        if (group == null) group = cardRect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        return cardRect;
    }
}
