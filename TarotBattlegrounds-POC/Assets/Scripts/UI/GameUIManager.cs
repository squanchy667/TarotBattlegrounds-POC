using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main game UI manager with theming support.
/// </summary>
public class GameUIManager : MonoBehaviour, IThemeable
{
    public static GameUIManager Instance { get; private set; }

    [Header("Phase Display")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text turnText;

    [Header("Active Player Display")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text healthText;

    [Header("Action Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button playCardButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button freezeShopButton;

    [Header("Button Labels (for theming)")]
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private TMP_Text sellButtonText;
    [SerializeField] private TMP_Text playButtonText;
    [SerializeField] private TMP_Text refreshButtonText;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private TMP_Text endTurnButtonText;
    [SerializeField] private TMP_Text freezeShopButtonText;

    [Header("References")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private HandUI handUI;
    [SerializeField] private BoardUI boardUI;

    [Header("Combat UI")]
    [SerializeField] private CombatLogUI combatLogUI;

    [Header("Match Info")]
    [SerializeField] private MatchInfoUI matchInfoUI;

    [Header("Panel Backgrounds (optional)")]
    [SerializeField] private Image mainPanelBackground;

    [Header("Game Background")]
    [SerializeField] private Image gameBackgroundImage;

    private int activePlayerIndex = 0;
    private Player currentPlayer;
    private ThemeConfig currentTheme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += ApplyTheme;

        // Apply current theme if available
        if (ThemeManager.ActiveTheme != null)
        {
            ApplyTheme(ThemeManager.ActiveTheme);
        }
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    /// <summary>
    /// Apply theme to the game UI.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply button text from theme
        if (buyButtonText != null) buyButtonText.text = theme.buyButtonText;
        if (sellButtonText != null) sellButtonText.text = theme.sellButtonText;
        if (playButtonText != null) playButtonText.text = theme.playButtonText;
        if (refreshButtonText != null) refreshButtonText.text = theme.rerollButtonText;
        if (upgradeButtonText != null) upgradeButtonText.text = theme.upgradeButtonText;
        if (endTurnButtonText != null) endTurnButtonText.text = theme.endTurnButtonText;

        // Update freeze button text based on current state
        UpdateFreezeButtonText();

        // Apply colors to UI elements
        ApplyThemeColors(theme);

        // Apply background
        if (gameBackgroundImage != null)
        {
            if (theme.gameBackground != null)
            {
                gameBackgroundImage.sprite = theme.gameBackground;
                gameBackgroundImage.color = Color.white;
            }
            else
            {
                gameBackgroundImage.sprite = null;
                gameBackgroundImage.color = theme.gameBackgroundColor;
            }
        }

        // Re-update display to use themed labels
        UpdatePlayerDisplay();
        UpdatePhaseDisplay();
    }

    private void ApplyThemeColors(ThemeConfig theme)
    {
        // Apply primary color to buttons
        Color buttonColor = theme.primaryColor;
        ApplyButtonColor(buyButton, buttonColor);
        ApplyButtonColor(sellButton, buttonColor);
        ApplyButtonColor(playCardButton, buttonColor);
        ApplyButtonColor(refreshButton, buttonColor);
        ApplyButtonColor(upgradeButton, buttonColor);
        ApplyButtonColor(endTurnButton, buttonColor);
        ApplyButtonColor(freezeShopButton, buttonColor);

        // Apply panel background
        if (mainPanelBackground != null)
            mainPanelBackground.color = theme.secondaryColor;

        // Apply text colors
        Color lightText = theme.textColorLight;
        if (phaseText != null) phaseText.color = lightText;
        if (timerText != null) timerText.color = theme.accentColor;
        if (turnText != null) turnText.color = lightText;
        if (playerNameText != null) playerNameText.color = lightText;

        // Stats with semantic colors
        if (coinsText != null) coinsText.color = theme.accentColor;
        if (healthText != null) healthText.color = theme.positiveColor;
        if (tierText != null) tierText.color = lightText;
        if (upgradeCostText != null) upgradeCostText.color = theme.accentColor;
    }

    private void ApplyButtonColor(Button button, Color color)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color;
        button.colors = colors;
    }
    
    private void Start()
    {
        SetupButtons();

#if PHOTON_UNITY_NETWORKING
        // In online mode, lock to the local player's slot and hide switch button
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            if (localSlot >= 0)
            {
                activePlayerIndex = localSlot;
            }

        }
#endif

        SubscribeToCurrentPlayer();

        // Subscribe to board events for card positioning
        if (boardUI != null)
        {
            boardUI.OnEmptySlotSelected += OnBoardEmptySlotSelected;
            boardUI.OnBoardSwapRequested += OnBoardSwapRequested;
        }

        // Subscribe to combat events if available
        if (combatLogUI != null)
        {
            CombatManager.OnCombatLogEntry += combatLogUI.AddLogEntry;
            CombatManager.OnCombatStart += combatLogUI.OnCombatStart;
            CombatManager.OnCombatEnd += combatLogUI.OnCombatEnd;
        }

        UpdateAllUI();
    }
    
    private void SetupButtons()
    {
        buyButton?.onClick.AddListener(() => ExecuteAction("Buy"));
        sellButton?.onClick.AddListener(() => ExecuteAction("Sell"));
        playCardButton?.onClick.AddListener(() => ExecuteAction("Play"));
        refreshButton?.onClick.AddListener(() => ExecuteAction("Refresh"));
        upgradeButton?.onClick.AddListener(() => ExecuteAction("Upgrade"));
        endTurnButton?.onClick.AddListener(OnEndTurnClicked);
        freezeShopButton?.onClick.AddListener(OnFreezeShopClicked);
    }
    
    /// <summary>
    /// Subscribe to the current player's events for automatic UI updates
    /// </summary>
    private void SubscribeToCurrentPlayer()
    {
        // Unsubscribe from previous player
        if (currentPlayer != null)
        {
            currentPlayer.OnHandChanged -= OnPlayerHandChanged;
            currentPlayer.OnBoardChanged -= OnPlayerBoardChanged;
            currentPlayer.OnCoinsChanged -= OnPlayerCoinsChanged;
            currentPlayer.OnTierChanged -= OnPlayerTierChanged;
            currentPlayer.OnHealthChanged -= OnPlayerHealthChanged;
            currentPlayer.OnShopRefreshed -= OnPlayerShopRefreshed;
            currentPlayer.OnShopFreezeChanged -= OnPlayerShopFreezeChanged;
        }

        // Get new current player
        currentPlayer = GetActivePlayer();

        // Subscribe to new player's events
        if (currentPlayer != null)
        {
            currentPlayer.OnHandChanged += OnPlayerHandChanged;
            currentPlayer.OnBoardChanged += OnPlayerBoardChanged;
            currentPlayer.OnCoinsChanged += OnPlayerCoinsChanged;
            currentPlayer.OnTierChanged += OnPlayerTierChanged;
            currentPlayer.OnHealthChanged += OnPlayerHealthChanged;
            currentPlayer.OnShopRefreshed += OnPlayerShopRefreshed;
            currentPlayer.OnShopFreezeChanged += OnPlayerShopFreezeChanged;
        }
    }
    
    // ====== EVENT HANDLERS ======
    
    private void OnPlayerHandChanged()
    {
        handUI?.RefreshHandDisplay();
        UpdateButtonStates();
    }
    
    private void OnPlayerBoardChanged()
    {
        boardUI?.RefreshBoardDisplay();
        UpdateButtonStates();
    }
    
    private void OnPlayerCoinsChanged()
    {
        UpdatePlayerDisplay();
        UpdateButtonStates();
    }
    
    private void OnPlayerTierChanged()
    {
        UpdatePlayerDisplay();
        shopUI?.RefreshShopDisplay(); // Shop tier display updates
    }
    
    private void OnPlayerHealthChanged(int newHealth)
    {
        UpdatePlayerDisplay();
        
        // Check for victory/defeat
        if (newHealth <= 0)
        {
            Debug.Log($"Player {currentPlayer.playerId} has been defeated!");
            // Could trigger defeat UI here
        }
    }
    
    private void OnPlayerShopRefreshed()
    {
        shopUI?.RefreshShopDisplay();
    }

    private void OnPlayerShopFreezeChanged(bool frozen)
    {
        UpdateFreezeButtonText();
    }
    
    // ====== REMOVED: Update() polling ======
    // We no longer poll every frame - events handle updates
    
    /// <summary>
    /// Call this when something external changes (like phase change from GameManager)
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateAllUI();
    }

    /// <summary>
    /// Called by NetworkGameSetup after slot assignment completes.
    /// Re-syncs the UI to the correct local player slot.
    /// </summary>
    public void OnNetworkSlotsAssigned()
    {
#if PHOTON_UNITY_NETWORKING
        if (NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            if (localSlot >= 0)
            {
                activePlayerIndex = localSlot;
                Debug.Log($"[GameUIManager] Network slots assigned. Locked to player slot {localSlot}");
                SubscribeToCurrentPlayer();
                UpdateAllUI();
            }
        }
#endif
    }
    
    private void UpdateAllUI()
    {
        UpdatePhaseDisplay();
        UpdatePlayerDisplay();
        UpdateButtonStates();
        
        // Refresh all panels
        shopUI?.RefreshShopDisplay();
        handUI?.RefreshHandDisplay();
        boardUI?.RefreshBoardDisplay();
    }
    
    private void UpdatePhaseDisplay()
    {
        if (GameManager.Instance == null) return;

        if (phaseText != null)
        {
            // Use themed phase names if available
            if (currentTheme != null)
            {
                phaseText.text = GameManager.Instance.CurrentPhase == GameManager.GamePhase.Combat
                    ? currentTheme.combatPhaseTitle
                    : currentTheme.recruitPhaseTitle;
            }
            else
            {
                phaseText.text = GameManager.Instance.CurrentPhase.ToString();
            }
        }
        if (turnText != null)
            turnText.text = $"Turn {GameManager.Instance.TurnNumber}";
    }
    
    private void UpdatePlayerDisplay()
    {
        var player = GetActivePlayer();
        if (player == null) return;

        // Get themed labels or use defaults
        string coinsLabel = currentTheme != null ? currentTheme.coinsLabel : "Coins";
        string tierLabel = currentTheme != null ? currentTheme.tierLabel : "Tier";
        string healthLabel = currentTheme != null ? currentTheme.healthLabel : "Health";

        if (playerNameText != null)
            playerNameText.text = $"Player {player.playerId}";
        if (coinsText != null)
            coinsText.text = $"{coinsLabel}: {player.coins}";
        if (tierText != null)
            tierText.text = $"{tierLabel}: {player.currentTavernTier}";
        if (upgradeCostText != null)
        {
            if (player.currentTavernTier >= 6)
            {
                string maxText = currentTheme != null ? currentTheme.maxTierText : "MAX";
                upgradeCostText.text = $"Upgrade: {maxText}";
            }
            else
            {
                upgradeCostText.text = $"Upgrade: {player.GetUpgradeCost()}g";
            }
        }

        // Use player's Health property
        if (healthText != null)
        {
            int health = player.Health;
            healthText.text = $"{healthLabel}: {health}";
        }
    }
    
    /// <summary>
    /// Update button interactability based on current state
    /// </summary>
    private void UpdateButtonStates()
    {
        var player = GetActivePlayer();
        if (player == null) return;
        
        bool isRecruitPhase = GameManager.Instance != null && 
                             GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        
        // Buy button: enabled if recruit phase, have coins, and card selected
        if (buyButton != null)
        {
            int shopIndex = shopUI != null ? shopUI.GetSelectedCardIndex() : -1;
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
            buyButton.interactable = isRecruitPhase && shopIndex >= 0 && player.coins >= buyCost && player.hand.Count < 10;
        }
        
        // Sell button: enabled if recruit phase and board OR hand card selected
        if (sellButton != null)
        {
            int boardIndex = boardUI != null ? boardUI.GetSelectedCardIndex() : -1;
            int handIndex = handUI != null ? handUI.GetSelectedCardIndex() : -1;
            sellButton.interactable = isRecruitPhase && (boardIndex >= 0 || handIndex >= 0);
        }
        
        // Play button: enabled if recruit phase, hand card selected, and board not full
        if (playCardButton != null)
        {
            int handIndex = handUI != null ? handUI.GetSelectedCardIndex() : -1;
            playCardButton.interactable = isRecruitPhase && handIndex >= 0 && player.board.Count < 7;
        }
        
        // Refresh button: enabled if recruit phase and have 1+ coins
        if (refreshButton != null)
        {
            refreshButton.interactable = isRecruitPhase && player.coins >= 1;
        }
        
        // Upgrade button: enabled if recruit phase, have enough coins, and not max tier
        if (upgradeButton != null)
        {
            upgradeButton.interactable = isRecruitPhase &&
                                         player.coins >= player.GetUpgradeCost() &&
                                         player.currentTavernTier < 6;
        }

        // End Turn button: enabled during recruit phase if this player hasn't already readied
        if (endTurnButton != null)
        {
            bool alreadyReady = GameManager.Instance != null &&
                                GameManager.Instance.IsPlayerReady(activePlayerIndex);
            endTurnButton.interactable = isRecruitPhase && !alreadyReady;

            // Update button text to reflect state
            if (endTurnButtonText != null)
            {
                if (alreadyReady)
                {
                    endTurnButtonText.text = "Waiting...";
                }
                else if (currentTheme != null)
                {
                    endTurnButtonText.text = currentTheme.endTurnButtonText;
                }
                else
                {
                    endTurnButtonText.text = "End Turn";
                }
            }
        }

        // Freeze Shop button: enabled during recruit phase
        if (freezeShopButton != null)
        {
            freezeShopButton.interactable = isRecruitPhase;
        }
    }
    
    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int display = time <= 0f ? 0 : Mathf.CeilToInt(time);
            timerText.text = $"{display}s";
        }
    }
    
    private bool IsOnlineMode => GameManager.Instance != null && GameManager.Instance.IsOnlineMode;

    private void ExecuteAction(string action)
    {
        var player = GetActivePlayer();
        if (player == null) return;

#if PHOTON_UNITY_NETWORKING
        // In online mode, route through NetworkGameBridge
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            ExecuteNetworkAction(action, player);
            return;
        }
#endif

        // Offline mode: execute directly
        switch (action)
        {
            case "Buy":
                if (shopUI != null)
                {
                    int selectedIndex = shopUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        player.BuyCard(selectedIndex);
                    }
                    else
                    {
                        Debug.Log("Select a card from the shop first!");
                    }
                }
                break;

            case "Sell":
                int boardSellIndex = boardUI != null ? boardUI.GetSelectedCardIndex() : -1;
                int handSellIndex = handUI != null ? handUI.GetSelectedCardIndex() : -1;

                if (boardSellIndex >= 0)
                {
                    player.SellCard(boardSellIndex);
                }
                else if (handSellIndex >= 0)
                {
                    player.SellCardFromHand(handSellIndex);
                }
                else
                {
                    Debug.Log("Select a card from your board or hand first!");
                }
                break;

            case "Play":
                if (handUI != null)
                {
                    int selectedIndex = handUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        player.PlayCard(selectedIndex, player.board.Count);
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
    private void ExecuteNetworkAction(string action, Player player)
    {
        var bridge = NetworkGameBridge.Instance;

        switch (action)
        {
            case "Buy":
                if (shopUI != null)
                {
                    int selectedIndex = shopUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                        bridge.RequestBuyCard(selectedIndex);
                    else
                        Debug.Log("Select a card from the shop first!");
                }
                break;

            case "Sell":
                int boardSellIndex = boardUI != null ? boardUI.GetSelectedCardIndex() : -1;
                int handSellIndex = handUI != null ? handUI.GetSelectedCardIndex() : -1;

                if (boardSellIndex >= 0)
                    bridge.RequestSellBoardCard(boardSellIndex);
                else if (handSellIndex >= 0)
                    bridge.RequestSellHandCard(handSellIndex);
                else
                    Debug.Log("Select a card from your board or hand first!");
                break;

            case "Play":
                if (handUI != null)
                {
                    int selectedIndex = handUI.GetSelectedCardIndex();
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
    
    private void OnBoardEmptySlotSelected(int slotPosition)
    {
        if (handUI == null) return;
        int handIndex = handUI.GetSelectedCardIndex();
        if (handIndex < 0) return;

        var player = GetActivePlayer();
        if (player == null) return;

        bool isRecruitPhase = GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        if (!isRecruitPhase) return;

        if (player.board.Count < 7)
        {
#if PHOTON_UNITY_NETWORKING
            if (IsOnlineMode && NetworkGameBridge.Instance != null)
            {
                NetworkGameBridge.Instance.RequestPlayCard(handIndex, slotPosition);
            }
            else
#endif
            {
                player.PlayCard(handIndex, slotPosition);
            }
            handUI.ClearSelection();
            Debug.Log($"Played hand card {handIndex} to board slot {slotPosition}");
        }
    }

    private void OnBoardSwapRequested(int indexA, int indexB)
    {
        var player = GetActivePlayer();
        if (player == null) return;

        bool isRecruitPhase = GameManager.Instance != null &&
                              GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit;
        if (!isRecruitPhase) return;

#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestSwapBoardCards(indexA, indexB);
        }
        else
#endif
        {
            player.SwapBoardCards(indexA, indexB);
        }
    }

    private void OnEndTurnClicked()
    {
        if (GameManager.Instance == null) return;

#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestEndTurn();
        }
        else
#endif
        {
            int playerIndex = GetActivePlayerIndex();
            GameManager.Instance.PlayerReadyForCombat(playerIndex);
        }

        // Disable button and show waiting state
        if (endTurnButton != null)
            endTurnButton.interactable = false;
        if (endTurnButtonText != null)
            endTurnButtonText.text = "Waiting...";
    }

    private void OnFreezeShopClicked()
    {
#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestToggleFreeze();
        }
        else
#endif
        {
            var player = GetActivePlayer();
            if (player != null)
            {
                player.ToggleShopFreeze();
            }
        }
        UpdateFreezeButtonText();
    }

    private void UpdateFreezeButtonText()
    {
        if (freezeShopButtonText == null) return;

        var player = GetActivePlayer();
        bool frozen = player != null && player.ShopFrozen;

        if (currentTheme != null)
        {
            freezeShopButtonText.text = frozen ? currentTheme.unfreezeButtonText : currentTheme.freezeButtonText;
        }
        else
        {
            freezeShopButtonText.text = frozen ? "Unfreeze" : "Freeze";
        }
    }

    /// <summary>
    /// Switch to a specific player by index
    /// </summary>
    public void SwitchToPlayer(int playerIndex)
    {
        if (GameManager.Instance == null) return;
        if (playerIndex < 0 || playerIndex >= GameManager.Instance.players.Count) return;
        
        activePlayerIndex = playerIndex;
        
        Debug.Log($"Switched to Player {activePlayerIndex + 1}");
        
        SubscribeToCurrentPlayer();
        UpdateAllUI();
        currentPlayer?.NotifyAllStateChanged();
    }

    public void UpdateButtons()
    {
        UpdateButtonStates();
    }
    
    public Player GetActivePlayer()
    {
        if (GameManager.Instance == null) return null;
        if (GameManager.Instance.players == null) return null;
        if (activePlayerIndex >= GameManager.Instance.players.Count) return null;
        
        return GameManager.Instance.players[activePlayerIndex];
    }
    
    public int GetActivePlayerIndex() => activePlayerIndex;
    
    private void OnDestroy()
    {
        // Unsubscribe from player events
        if (currentPlayer != null)
        {
            currentPlayer.OnHandChanged -= OnPlayerHandChanged;
            currentPlayer.OnBoardChanged -= OnPlayerBoardChanged;
            currentPlayer.OnCoinsChanged -= OnPlayerCoinsChanged;
            currentPlayer.OnTierChanged -= OnPlayerTierChanged;
            currentPlayer.OnHealthChanged -= OnPlayerHealthChanged;
            currentPlayer.OnShopRefreshed -= OnPlayerShopRefreshed;
            currentPlayer.OnShopFreezeChanged -= OnPlayerShopFreezeChanged;
        }

        // Unsubscribe from board events
        if (boardUI != null)
        {
            boardUI.OnEmptySlotSelected -= OnBoardEmptySlotSelected;
            boardUI.OnBoardSwapRequested -= OnBoardSwapRequested;
        }

        // Unsubscribe from combat events
        if (combatLogUI != null)
        {
            CombatManager.OnCombatLogEntry -= combatLogUI.AddLogEntry;
            CombatManager.OnCombatStart -= combatLogUI.OnCombatStart;
            CombatManager.OnCombatEnd -= combatLogUI.OnCombatEnd;
        }

        // Unsubscribe from theme events
        ThemeManager.OnThemeChanged -= ApplyTheme;

        // Clean up button listeners
        buyButton?.onClick.RemoveAllListeners();
        sellButton?.onClick.RemoveAllListeners();
        playCardButton?.onClick.RemoveAllListeners();
        refreshButton?.onClick.RemoveAllListeners();
        upgradeButton?.onClick.RemoveAllListeners();
        endTurnButton?.onClick.RemoveAllListeners();
        freezeShopButton?.onClick.RemoveAllListeners();

        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// Auto-show match info panel briefly after combat ends.
    /// </summary>
    public void ShowMatchInfoAfterCombat()
    {
        if (matchInfoUI != null)
            matchInfoUI.AutoShowAfterCombat();
    }

    // Accessor methods for child UI components
    public ShopUI GetShopUI() => shopUI;
    public HandUI GetHandUI() => handUI;
    public BoardUI GetBoardUI() => boardUI;
    public CombatLogUI GetCombatLogUI() => combatLogUI;
    public MatchInfoUI GetMatchInfoUI() => matchInfoUI;
}
