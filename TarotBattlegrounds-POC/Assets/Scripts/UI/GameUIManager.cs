using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// Main game UI manager with theming support. Facade over four extracted plain-C# helpers —
/// GameUIThemeApplier, HudPresenter, UIButtonStateController, PlayerActionController — as part
/// of the UI god-class refactor (spec §2, 2026-07-04 batch). Helpers are constructed in Awake
/// (after the singleton guard) and hold an `internal` back-reference to this facade so
/// cross-helper composition (e.g. PlayerActionController calling into HudPresenter /
/// UIButtonStateController) resolves through `owner.hud` / `owner.buttonState` at call time.
/// </summary>
public class GameUIManager : MonoBehaviour, IThemeable
{
    public static GameUIManager Instance { get; private set; }

    [Header("Phase Display")]
    [SerializeField] internal TMP_Text phaseText;
    [SerializeField] internal TMP_Text timerText;
    [SerializeField] internal TMP_Text turnText;

    [Header("Active Player Display")]
    [SerializeField] internal TMP_Text playerNameText;
    [SerializeField] internal TMP_Text coinsText;
    [SerializeField] internal TMP_Text tierText;
    [SerializeField] internal TMP_Text upgradeCostText;
    [SerializeField] internal TMP_Text healthText;

    [Header("Action Buttons")]
    [SerializeField] internal Button buyButton;
    [SerializeField] internal Button sellButton;
    [SerializeField] internal Button playCardButton;
    [SerializeField] internal Button refreshButton;
    [SerializeField] internal Button upgradeButton;
    [SerializeField] internal Button endTurnButton;
    [SerializeField] internal Button freezeShopButton;

    [Header("Button Labels (for theming)")]
    [SerializeField] internal TMP_Text buyButtonText;
    [SerializeField] internal TMP_Text sellButtonText;
    [SerializeField] internal TMP_Text playButtonText;
    [SerializeField] internal TMP_Text refreshButtonText;
    [SerializeField] internal TMP_Text upgradeButtonText;
    [SerializeField] internal TMP_Text endTurnButtonText;
    [SerializeField] internal TMP_Text freezeShopButtonText;

    [Header("References")]
    [SerializeField] internal ShopUI shopUI;
    [SerializeField] internal HandUI handUI;
    [SerializeField] internal BoardUI boardUI;

    [Header("Combat UI")]
    [SerializeField] internal CombatLogUI combatLogUI;

    [Header("Resource Bar (UX09)")]
    [SerializeField] internal ResourceBar resourceBar;

    [Header("Match Info")]
    [SerializeField] internal MatchInfoUI matchInfoUI;

    [Header("Circular Timer (UX10)")]
    [SerializeField] internal CircularTimer circularTimer;

    [Header("Phase Banner (UX11)")]
    [SerializeField] internal PhaseBanner phaseBanner;

    [Header("Synergy Display (UX12)")]
    [SerializeField] internal SynergyDisplayPanel synergyDisplay;

    [Header("Panel Backgrounds (optional)")]
    [SerializeField] internal Image mainPanelBackground;
    [SerializeField] internal StyledPanel mainStyledPanel;

    [Header("Game Background")]
    [SerializeField] internal Image gameBackgroundImage;
    [SerializeField] internal BackgroundController backgroundController;

    private int activePlayerIndex = 0;
    private Player currentPlayer;

    // Extracted helpers (constructed in Awake, after the singleton guard). Internal so
    // sibling helpers can reach each other through the owner reference they hold.
    internal GameUIThemeApplier themeApplier;
    internal HudPresenter hud;
    internal UIButtonStateController buttonState;
    internal PlayerActionController actions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        themeApplier = new GameUIThemeApplier(this);
        buttonState = new UIButtonStateController(this);
        hud = new HudPresenter(this, themeApplier);
        actions = new PlayerActionController(this);
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
    /// Apply theme to the game UI. Delegates to GameUIThemeApplier — this method stays on the
    /// facade to satisfy the IThemeable contract and the ThemeManager.OnThemeChanged
    /// subscription (spec §2.2 item 1).
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        themeApplier.ApplyTheme(theme);
    }

    private void Start()
    {
        SetupButtons();
        buttonState.AttachIgniteButtons();

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
            boardUI.OnEmptySlotSelected += actions.OnBoardEmptySlotSelected;
            boardUI.OnBoardSwapRequested += actions.OnBoardSwapRequested;
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
        buyButton?.onClick.AddListener(() => actions.ExecuteAction("Buy"));
        sellButton?.onClick.AddListener(() => actions.ExecuteAction("Sell"));
        playCardButton?.onClick.AddListener(() => actions.ExecuteAction("Play"));
        refreshButton?.onClick.AddListener(() => actions.ExecuteAction("Refresh"));
        upgradeButton?.onClick.AddListener(() => actions.ExecuteAction("Upgrade"));
        endTurnButton?.onClick.AddListener(() => actions.OnEndTurnClicked());
        freezeShopButton?.onClick.AddListener(() => actions.OnFreezeShopClicked());
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
        buttonState.UpdateButtonStates();
    }

    private void OnPlayerBoardChanged()
    {
        boardUI?.RefreshBoardDisplay();
        buttonState.UpdateButtonStates();
        hud.RefreshSynergyDisplay();
    }

    private void OnPlayerCoinsChanged()
    {
        hud.UpdatePlayerDisplay();
        buttonState.UpdateButtonStates();
    }

    private void OnPlayerTierChanged()
    {
        hud.UpdatePlayerDisplay();
        shopUI?.RefreshShopDisplay(); // Shop tier display updates
    }

    private void OnPlayerHealthChanged(int newHealth)
    {
        hud.UpdatePlayerDisplay();

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
        hud.UpdateFreezeButtonText();
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
        hud.UpdatePhaseDisplay();
        hud.UpdatePlayerDisplay();
        buttonState.UpdateButtonStates();

        // Refresh all panels
        shopUI?.RefreshShopDisplay();
        handUI?.RefreshHandDisplay();
        boardUI?.RefreshBoardDisplay();
        hud.RefreshSynergyDisplay();
    }

    public void UpdateTimer(float time)
    {
        hud.UpdateTimer(time);
    }

    internal bool IsOnlineMode => GameManager.Instance != null && GameManager.Instance.IsOnlineMode;

    /// <summary>
    /// Switch to a specific player by index
    /// </summary>
    public void SwitchToPlayer(int playerIndex)
    {
        if (GameManager.Instance == null) return;
        if (playerIndex < 0 || playerIndex >= GameManager.Instance.players.Count) return;

        activePlayerIndex = playerIndex;

        // UX09: Reset resource bar cached values so switching players doesn't trigger flash animations
        if (resourceBar != null)
            resourceBar.ResetCachedValues();

        Debug.Log($"Switched to Player {activePlayerIndex + 1}");

        SubscribeToCurrentPlayer();
        UpdateAllUI();
        currentPlayer?.NotifyAllStateChanged();
    }

    public void UpdateButtons()
    {
        buttonState.UpdateButtonStates();
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
            boardUI.OnEmptySlotSelected -= actions.OnBoardEmptySlotSelected;
            boardUI.OnBoardSwapRequested -= actions.OnBoardSwapRequested;
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
    public SynergyDisplayPanel GetSynergyDisplay() => synergyDisplay;
}
