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

    [Header("WO-09 Phase Env Stills (assign via GameEnvStillsSetup)")]
    [SerializeField] internal Sprite recruitEnvStill; // shop_bg.jpg
    [SerializeField] internal Sprite combatEnvStill;  // board_env.jpg
    private GameManager.GamePhase lastEnvPhase = (GameManager.GamePhase)(-1);

    private int activePlayerIndex = 0;
    private Player currentPlayer;

    // WO-04b: store combat static-event delegates so OnDestroy can -= even when
    // combatLogUI is a Unity fake-null (DisableDomainReload leaves statics alive).
    private System.Action<CombatLogEntry> combatLogEntryHandler;
    private System.Action<string, string> combatStartHandler;
    private System.Action<string, int> combatEndHandler;

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

        // Subscribe to combat events if available (stored handlers for reliable unsub — WO-04b)
        SubscribeCombatLogEvents();

        UpdateAllUI();

        // Recruit/combat env art (shop_bg / board_env) — force on boot
        ApplyPhaseEnvironmentBackground(
            GameManager.Instance != null ? GameManager.Instance.CurrentPhase : GameManager.GamePhase.Recruit,
            force: true);

        // MatchInfoHolder was often saved inactive → "i" button had no listeners
        if (matchInfoUI != null)
            matchInfoUI.EnsureWired();

        // GameOverUIRoot often saved inactive → never subscribed to OnGameOver → stuck after win
        var gameOver = FindObjectOfType<GameOverUI>(true);
        if (gameOver != null)
            gameOver.EnsureReady();

        // Top strip: timer / phase / turn clear of gear + match-info buttons
        PinTopHudChrome();

        // Synergy rows are built into the scene in editor but the runtime dictionary is empty
        if (synergyDisplay != null)
            synergyDisplay.EnsureInitialized();
    }

    /// <summary>
    /// WO-04b: pair CombatManager static += with stored delegates (not field-null-gated -=).
    /// </summary>
    private void SubscribeCombatLogEvents()
    {
        UnsubscribeCombatLogEvents();
        if (combatLogUI == null) return;

        combatLogEntryHandler = combatLogUI.AddLogEntry;
        combatStartHandler = combatLogUI.OnCombatStart;
        combatEndHandler = combatLogUI.OnCombatEnd;

        CombatManager.OnCombatLogEntry += combatLogEntryHandler;
        CombatManager.OnCombatStart += combatStartHandler;
        CombatManager.OnCombatEnd += combatEndHandler;
    }

    private void UnsubscribeCombatLogEvents()
    {
        if (combatLogEntryHandler != null)
        {
            CombatManager.OnCombatLogEntry -= combatLogEntryHandler;
            combatLogEntryHandler = null;
        }
        if (combatStartHandler != null)
        {
            CombatManager.OnCombatStart -= combatStartHandler;
            combatStartHandler = null;
        }
        if (combatEndHandler != null)
        {
            CombatManager.OnCombatEnd -= combatEndHandler;
            combatEndHandler = null;
        }
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
        // First-tick pin if BeginRecruitTimer was skipped (client path / late UI awake).
        if (!_timerPinned)
            EnsureTestabilityTimerVisible();
        hud.UpdateTimer(time);
    }

    /// <summary>Recruit phase start — reset circular total so long late-game timers fill correctly.</summary>
    public void BeginRecruitTimer(float totalSeconds)
    {
        EnsureTestabilityTimerVisible();
        hud.BeginRecruitTimer(totalSeconds);
    }

    private bool _timerPinned;
    private bool _topHudPinned;

    /// <summary>
    /// Guarantee a large top-center countdown is on-screen.
    /// Scene TimerText lived under PhasePanel with anchoredPosition.x=500 (fully off canvas).
    /// Always reparent + pin to SafeArea/Canvas top-center; create TMP if unwired.
    /// Styling runs once so HudPresenter can still flash red under 5s.
    /// </summary>
    public void EnsureTestabilityTimerVisible()
    {
        // Create timer first so PinTopHudChrome can place it
        if (timerText == null)
        {
            Transform host = GetTopHudHost();
            if (host == null) return;
            GameObject go = new GameObject("RecruitTimerText_Runtime");
            go.transform.SetParent(host, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            if (FontRefs.Instance != null && FontRefs.Instance.Display != null)
                tmp.font = FontRefs.Instance.Display;
            timerText = tmp;
            _topHudPinned = false; // allow re-layout with new timer
            Debug.Log("[GameUIManager] Created runtime recruit timer text (was unwired).");
        }

        PinTopHudChrome();

        if (timerText != null
            && GameManager.Instance != null
            && GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit
            && !timerText.gameObject.activeSelf)
            timerText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Layout top strip so turn/phase/timer never sit under gear (⚙) or match-info (i).
    /// Right ~200px reserved for those buttons.
    ///   [ Phase ]     [ TIMER ]     [ Turn N ]
    ///                 (center band)
    /// </summary>
    public void PinTopHudChrome()
    {
        if (_topHudPinned) return;
        Transform host = GetTopHudHost();
        if (host == null) return;

        // Timer — top center (leave headroom under right buttons)
        if (timerText != null)
        {
            PinTopLabel(timerText, host, new Vector2(0f, -10f), new Vector2(280f, 72f));
            StyleTopLabel(timerText, Tokens.TextDisplay, Tokens.BoneBright);
        }

        // Phase — left of center (not near right menus)
        if (phaseText != null)
        {
            PinTopLabel(phaseText, host, new Vector2(-200f, -18f), new Vector2(180f, 40f));
            StyleTopLabel(phaseText, Tokens.TextH2, Tokens.BronzeBright);
        }

        // Turn — right of center but well clear of ⚙/i (those sit at x≈-16..-100 from right edge)
        if (turnText != null)
        {
            PinTopLabel(turnText, host, new Vector2(200f, -18f), new Vector2(160f, 40f));
            StyleTopLabel(turnText, Tokens.TextH2, Tokens.BoneBright);
        }

        _topHudPinned = true;
        _timerPinned = timerText != null;
        Debug.Log("[GameUIManager] Pinned top HUD (phase/timer/turn) clear of corner menus");
    }

    private Transform GetTopHudHost()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;
        Transform host = canvas.transform;
        Transform safe = host.Find("SafeArea");
        return safe != null ? safe : host;
    }

    private static void PinTopLabel(TMP_Text tmp, Transform host, Vector2 anchoredPos, Vector2 size)
    {
        if (tmp == null || host == null) return;
        tmp.transform.SetParent(host, false);
        // Keep under SettingsRoot so gear stays clickable on top
        Transform settings = host.Find("SettingsRoot");
        if (settings != null)
            tmp.transform.SetSiblingIndex(Mathf.Max(0, settings.GetSiblingIndex()));
        else
            tmp.transform.SetAsLastSibling();

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        tmp.gameObject.SetActive(true);
    }

    private static void StyleTopLabel(TMP_Text tmp, float fontSize, Color color)
    {
        if (tmp == null) return;
        tmp.enableAutoSizing = false;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
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

        // Unsubscribe from combat events (handlers — do not gate on combatLogUI fake-null)
        UnsubscribeCombatLogEvents();

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
    /// Formerly auto-opened the match scoreboard after every fight.
    /// Playtest preference: on-demand only via the "i" button (popup redesign later).
    /// </summary>
    public void ShowMatchInfoAfterCombat()
    {
        // Intentionally no-op — use MatchInfoUI toggle for on-demand scoreboard.
    }

    /// <summary>
    /// T844: exclusive combat stage — hide ALL recruit chrome so the arena is alone.
    /// Called by CombatAnimator when the replay panel opens/closes; also used by GameOverUI.
    /// </summary>
    public void SetCombatPresentationMode(bool combatActive)
    {
        // Hide recruit clutter (shop window included — Ofek: replay was overlaying the shop)
        SetChromeActive(shopUI != null ? shopUI.gameObject : null, !combatActive);
        SetChromeActive(handUI != null ? handUI.gameObject : null, !combatActive);
        SetChromeActive(boardUI != null ? boardUI.gameObject : null, !combatActive);
        SetChromeActive(synergyDisplay != null ? synergyDisplay.gameObject : null, !combatActive);
        SetChromeActive(resourceBar != null ? resourceBar.gameObject : null, !combatActive);
        SetChromeActive(combatLogUI != null ? combatLogUI.gameObject : null, !combatActive);
        if (phaseBanner != null)
            SetChromeActive(phaseBanner.gameObject, !combatActive);

        // Named shop/action panels (scene hierarchy) — deep-find so nested Shop windows hide too
        Transform host = GetTopHudHost();
        if (host != null)
        {
            foreach (var name in new[]
                     {
                         "ActionsButtonsPanel", "ActionButtonsPanel", "ShopPanel", "ShopUI",
                         "HandPanel", "HandUI", "BoardPanel", "BoardUI", "PlayerInfoPanel",
                         "ShopWindow", "ShopArea", "RecruitRoot", "TavernPanel"
                     })
            {
                Transform t = FindDeepChild(host, name);
                if (t != null)
                    SetChromeActive(t.gameObject, !combatActive);
            }
        }

        // Also toggle individual action buttons if their panel wasn't found
        if (buyButton != null && buyButton.transform.parent != null)
            SetChromeActive(buyButton.transform.parent.gameObject, !combatActive);
        foreach (var btn in new[] { buyButton, sellButton, playCardButton, refreshButton, upgradeButton, freezeShopButton, endTurnButton })
        {
            if (btn != null)
                SetChromeActive(btn.gameObject, !combatActive);
        }

        if (timerText != null && combatActive)
            timerText.gameObject.SetActive(false);

        // T844/cosmetics: hide phase/turn during exclusive combat/game-over stages
        // (they were still reading "Combat / Turn N" behind the game-over panel)
        if (phaseText != null) phaseText.gameObject.SetActive(!combatActive);
        if (turnText != null) turnText.gameObject.SetActive(!combatActive);

        if (!combatActive)
        {
            ApplyPhaseEnvironmentBackground(
                GameManager.Instance != null ? GameManager.Instance.CurrentPhase : GameManager.GamePhase.Recruit,
                force: true);
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentPhase == GameManager.GamePhase.Recruit
                && timerText != null)
                timerText.gameObject.SetActive(true);
            if (phaseText != null) phaseText.gameObject.SetActive(true);
            if (turnText != null) turnText.gameObject.SetActive(true);
            hud?.RefreshSynergyDisplay();
        }
        else
        {
            ApplyPhaseEnvironmentBackground(GameManager.GamePhase.Combat, force: true);
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindDeepChild(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetChromeActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    // Accessor methods for child UI components
    public ShopUI GetShopUI() => shopUI;
    public HandUI GetHandUI() => handUI;
    public BoardUI GetBoardUI() => boardUI;
    public CombatLogUI GetCombatLogUI() => combatLogUI;

    /// <summary>
    /// WO-09 / in-game: recruit = shop_bg, combat = board_env.
    /// Always ensures a full-screen BG layer exists; loads stills if missing.
    /// </summary>
    public void ApplyPhaseEnvironmentBackground(GameManager.GamePhase phase, bool force = false)
    {
        EnsurePhaseBackgroundPipeline();

        if (!force && phase == lastEnvPhase) return;
        lastEnvPhase = phase;

        // Ofek: use combat board environment for BOTH recruit and combat (one consistent world bg)
        Sprite still = combatEnvStill;
        if (still == null)
            still = recruitEnvStill; // fallback if combat still unwired
        if (still == null)
            still = LoadEnvStillFallback(combat: true);
        if (still == null)
            still = LoadEnvStillFallback(combat: false);

        if (still == null)
        {
            Debug.LogWarning($"[GameUIManager] No env still for phase {phase} — check Art/Env sprites");
            return;
        }

        combatEnvStill = still;
        if (recruitEnvStill == null)
            recruitEnvStill = still;

        backgroundController.SetStillSprite(still);

        // Kill solid full-screen covers that sit above BG_Root (was hiding all env art)
        ClearSolidBackgroundCovers();
    }

    /// <summary>
    /// GameBackgroundImage (and similar) often paint a solid purple/ash fullscreen
    /// above BG_Root — disable so shop_bg / board_env can show.
    /// </summary>
    private void ClearSolidBackgroundCovers()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Explicit legacy field
        if (gameBackgroundImage != null)
        {
            gameBackgroundImage.enabled = false;
            gameBackgroundImage.raycastTarget = false;
            var c = gameBackgroundImage.color;
            c.a = 0f;
            gameBackgroundImage.color = c;
        }

        // Named cover under Canvas
        Transform gbi = canvas.transform.Find("GameBackgroundImage");
        if (gbi != null)
        {
            var img = gbi.GetComponent<Image>();
            if (img != null)
            {
                img.enabled = false;
                img.raycastTarget = false;
                var c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }

        if (mainPanelBackground != null && mainPanelBackground.color.a > 0.35f)
        {
            Color c = mainPanelBackground.color;
            c.a = 0.12f;
            mainPanelBackground.color = c;
            mainPanelBackground.raycastTarget = false;
        }

        // BG_Root must be first sibling under Canvas
        Transform bgRoot = canvas.transform.Find("BG_Root");
        if (bgRoot != null)
            bgRoot.SetAsFirstSibling();
    }

    /// <summary>Find/create BackgroundController + load default stills if inspector empty.</summary>
    private void EnsurePhaseBackgroundPipeline()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (backgroundController == null)
            backgroundController = canvas.GetComponent<BackgroundController>();
        if (backgroundController == null)
            backgroundController = canvas.GetComponentInChildren<BackgroundController>(true);
        if (backgroundController == null)
        {
            backgroundController = canvas.gameObject.AddComponent<BackgroundController>();
            Debug.Log("[GameUIManager] Added BackgroundController on Canvas");
        }

        backgroundController.EnsureBackgroundBaseLayer();

        if (recruitEnvStill == null)
            recruitEnvStill = LoadEnvStillFallback(combat: false);
        if (combatEnvStill == null)
            combatEnvStill = LoadEnvStillFallback(combat: true);
    }

    /// <summary>
    /// Runtime/editor load of env art. Prefers Resources/Env/ then Resources root names.
    /// Scene serialized refs should win when present.
    /// </summary>
    private static Sprite LoadEnvStillFallback(bool combat)
    {
        string[] keys = combat
            ? new[] { "Env/board_env", "board_env", "Env/Board/board_env" }
            : new[] { "Env/shop_bg", "shop_bg", "Env/Shop/shop_bg" };

        foreach (var key in keys)
        {
            var sp = Resources.Load<Sprite>(key);
            if (sp != null) return sp;
        }

#if UNITY_EDITOR
        string path = combat
            ? "Assets/Art/Env/Board/board_env.jpg"
            : "Assets/Art/Env/Shop/shop_bg.jpg";
        var editorSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (editorSp != null) return editorSp;
        foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) return s;
#endif
        return null;
    }
    public MatchInfoUI GetMatchInfoUI() => matchInfoUI;
    public SynergyDisplayPanel GetSynergyDisplay() => synergyDisplay;
}
