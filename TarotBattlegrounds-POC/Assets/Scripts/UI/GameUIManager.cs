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

    private int activePlayerIndex = 0;

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

    private void Update()
    {
        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        UpdatePhaseDisplay();
        UpdatePlayerDisplay();
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

        if (healthText != null && GameManager.Instance != null)
            healthText.text = $"Health: {GameManager.Instance.GetPlayerHealth(activePlayerIndex)}";
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
                        shopUI.RefreshShopDisplay();
                        handUI?.RefreshHandDisplay();
                    }
                    else
                        Debug.Log("Select a card from the shop first!");
                }
                else
                {
                    player.BuyCard(0);
                }
                break;
            case "Sell":
                if (boardUI != null)
                {
                    int selectedIndex = boardUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        player.SellCard(selectedIndex);
                        boardUI.RefreshBoardDisplay();
                    }
                    else
                        Debug.Log("Select a card from your board first!");
                }
                else
                {
                    player.SellCard(0);
                }
                break;
            case "Play":
                if (handUI != null)
                {
                    int selectedIndex = handUI.GetSelectedCardIndex();
                    if (selectedIndex >= 0)
                    {
                        player.PlayCard(selectedIndex, player.board.Count);
                        handUI.RefreshHandDisplay();
                        boardUI?.RefreshBoardDisplay();
                    }
                    else
                        Debug.Log("Select a card from your hand first!");
                }
                else
                {
                    player.PlayCard(0, player.board.Count);
                }
                break;
            case "Refresh":
                player.RefreshTavernShop();
                break;
            case "Upgrade":
                player.UpgradeTavern();
                break;
        }
        UpdateAllUI();
    }

    private void SwitchActivePlayer()
    {
        if (GameManager.Instance == null) return;
        
        int playerCount = GameManager.Instance.players.Count;
        activePlayerIndex = (activePlayerIndex + 1) % playerCount;
        
        Debug.Log($"Switched to Player {activePlayerIndex + 1}");
        
        // Refresh all UI panels
        shopUI?.RefreshShopDisplay();
        handUI?.RefreshHandDisplay();
        boardUI?.RefreshBoardDisplay();
        
        UpdateAllUI();
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
        buyButton?.onClick.RemoveAllListeners();
        sellButton?.onClick.RemoveAllListeners();
        playCardButton?.onClick.RemoveAllListeners();
        refreshButton?.onClick.RemoveAllListeners();
        upgradeButton?.onClick.RemoveAllListeners();
        switchPlayerButton?.onClick.RemoveAllListeners();

        if (Instance == this) Instance = null;
    }
        public ShopUI GetShopUI()
    {
        return shopUI;
    }

    public HandUI GetHandUI()
    {
        return handUI;
    }
    
    public BoardUI GetBoardUI()
    {
        return boardUI;
    }

}