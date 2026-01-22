using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
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
    [SerializeField] private Button switchPlayerButton;
    
    [Header("References")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private HandUI handUI;
    [SerializeField] private BoardUI boardUI;
    
    // Combat log reference (NEW)
    [Header("Combat UI")]
    [SerializeField] private CombatLogUI combatLogUI;
    
    private int activePlayerIndex = 0;
    private Player currentPlayer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        SetupButtons();
        SubscribeToCurrentPlayer();
        
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
        switchPlayerButton?.onClick.AddListener(SwitchActivePlayer);
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
    
    // ====== REMOVED: Update() polling ======
    // We no longer poll every frame - events handle updates
    
    /// <summary>
    /// Call this when something external changes (like phase change from GameManager)
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateAllUI();
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
            phaseText.text = GameManager.Instance.CurrentPhase.ToString();
        if (turnText != null)
            turnText.text = $"Turn {GameManager.Instance.TurnNumber}";
    }
    
    private void UpdatePlayerDisplay()
    {
        var player = GetActivePlayer();
        if (player == null) return;
        
        if (playerNameText != null)
            playerNameText.text = $"Player {player.playerId}";
        if (coinsText != null)
            coinsText.text = $"Coins: {player.coins}";
        if (tierText != null)
            tierText.text = $"Tier: {player.currentTavernTier}";
        if (upgradeCostText != null)
            upgradeCostText.text = $"Upgrade: {player.GetUpgradeCost()}g";
        
        // Use player's Health property if available, otherwise fallback to GameManager
        if (healthText != null)
        {
            int health = player.Health; // Now using Player.Health property
            healthText.text = $"Health: {health}";
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
                buyCost = 3 + TavernManager.Instance.availableCards[player.playerId][shopIndex].buyCostModifier;
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
    }
    
    public void UpdateTimer(float time)
    {
        if (timerText != null)
            timerText.text = $"{Mathf.CeilToInt(time)}s";
    }
    
    private void ExecuteAction(string action)
    {
        var player = GetActivePlayer();
        if (player == null) return;
        
        switch (action)
        {
            case "Buy":
                if (shopUI != null)
                {
                    int selectedIndex = shopUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        player.BuyCard(selectedIndex);
                        // Events will handle UI refresh
                    }
                    else
                    {
                        Debug.Log("Select a card from the shop first!");
                    }
                }
                break;
                
            case "Sell":
                // Check board first, then hand
                int boardSellIndex = boardUI != null ? boardUI.GetSelectedCardIndex() : -1;
                int handSellIndex = handUI != null ? handUI.GetSelectedCardIndex() : -1;

                if (boardSellIndex >= 0)
                {
                    player.SellCard(boardSellIndex);
                    // Events will handle UI refresh
                }
                else if (handSellIndex >= 0)
                {
                    player.SellCardFromHand(handSellIndex);
                    // Events will handle UI refresh
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
                        // Events will handle UI refresh
                    }
                    else
                    {
                        Debug.Log("Select a card from your hand first!");
                    }
                }
                break;
                
            case "Refresh":
                player.RefreshTavernShop();
                // Events will handle UI refresh
                break;
                
            case "Upgrade":
                player.UpgradeTavern();
                // Events will handle UI refresh
                break;
        }
    }
    
    /// <summary>
    /// Switch to the next player and refresh all UI
    /// </summary>
    public void SwitchActivePlayer()
    {
        if (GameManager.Instance == null) return;
        
        int playerCount = GameManager.Instance.players.Count;
        activePlayerIndex = (activePlayerIndex + 1) % playerCount;
        
        Debug.Log($"Switched to Player {activePlayerIndex + 1}");
        
        // Resubscribe to new player's events
        SubscribeToCurrentPlayer();
        
        // Force refresh all UI panels for new player
        UpdateAllUI();
        
        // Notify the new player to fire all events (in case UI components are listening directly)
        currentPlayer?.NotifyAllStateChanged();
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
        }
        
        // Unsubscribe from combat events
        if (combatLogUI != null)
        {
            CombatManager.OnCombatLogEntry -= combatLogUI.AddLogEntry;
            CombatManager.OnCombatStart -= combatLogUI.OnCombatStart;
            CombatManager.OnCombatEnd -= combatLogUI.OnCombatEnd;
        }
        
        // Clean up button listeners
        buyButton?.onClick.RemoveAllListeners();
        sellButton?.onClick.RemoveAllListeners();
        playCardButton?.onClick.RemoveAllListeners();
        refreshButton?.onClick.RemoveAllListeners();
        upgradeButton?.onClick.RemoveAllListeners();
        switchPlayerButton?.onClick.RemoveAllListeners();
        
        if (Instance == this)
            Instance = null;
    }
    
    // Accessor methods for child UI components
    public ShopUI GetShopUI() => shopUI;
    public HandUI GetHandUI() => handUI;
    public BoardUI GetBoardUI() => boardUI;
    public CombatLogUI GetCombatLogUI() => combatLogUI;
}
