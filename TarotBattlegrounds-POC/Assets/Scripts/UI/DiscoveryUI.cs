using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Discovery popup UI. Shows 3 card choices when a triple is formed.
/// The player picks one card which is added to their hand.
/// </summary>
public class DiscoveryUI : MonoBehaviour, IThemeable
{
    [Header("Panel")]
    [SerializeField] private GameObject discoveryPanel;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;

    [Header("Card Choices")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardDisplayPrefab;

    private List<GameObject> choiceCards = new List<GameObject>();
    private Dictionary<int, (Player player, List<Card> cards)> pendingDiscoveries = new Dictionary<int, (Player, List<Card>)>();
    private ThemeConfig currentTheme;

    /// <summary>
    /// Pending discovery cards keyed by player ID for network-triggered discovery.
    /// Set by host, read by NetworkGameBridge when client makes a choice.
    /// </summary>
    public static Dictionary<int, List<Card>> PendingDiscoveryByPlayer { get; set; } = new Dictionary<int, List<Card>>();

    private void Awake()
    {
        if (discoveryPanel != null)
            discoveryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;
        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
        UnsubscribeFromAllPlayers();
    }

    private void Start()
    {
        // Subscribe to all existing players' discovery events
        SubscribeToAllPlayers();
    }

    private void SubscribeToAllPlayers()
    {
        if (GameManager.Instance == null) return;
        foreach (var player in GameManager.Instance.players)
        {
            if (player != null)
                player.OnTripleDiscovery += ShowDiscovery;
        }
    }

    private void UnsubscribeFromAllPlayers()
    {
        if (GameManager.Instance == null) return;
        foreach (var player in GameManager.Instance.players)
        {
            if (player != null)
                player.OnTripleDiscovery -= ShowDiscovery;
        }
    }

    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        if (titleText != null)
            titleText.color = theme.accentColor;
    }

    private bool IsOnlineMode => GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer;

    private void ShowDiscovery(Player player, List<Card> cards)
    {
        if (discoveryPanel == null || cards == null || cards.Count == 0) return;

        // Only show discovery UI for human players
        if (!GameConfig.IsHumanPlayer(player.playerId - 1))
        {
            // AI auto-picks the first card
            player.AddDiscoveryCard(cards[0]);
            Debug.Log($"[DiscoveryUI] AI Player {player.playerId} auto-picked {cards[0].cardName}");
            return;
        }

        // Store pending discovery cards keyed by player ID — M2: per-player
        PendingDiscoveryByPlayer[player.playerId] = cards;
        pendingDiscoveries[player.playerId] = (player, cards);

#if PHOTON_UNITY_NETWORKING
        // In online mode, only show UI for local player
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            if (player.playerId - 1 != localSlot)
            {
                // Not our discovery, skip UI
                return;
            }
        }
#endif

        discoveryPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "Triple! Choose a Card:";

        ClearChoices();

        for (int i = 0; i < cards.Count; i++)
        {
            CreateChoiceCard(cards[i], i);
        }
    }

    /// <summary>
    /// Show discovery choices from a network trigger (client-side).
    /// </summary>
    public void ShowDiscoveryFromNetwork(Player player, List<Card> cards)
    {
        ShowDiscovery(player, cards);
    }

    private void CreateChoiceCard(Card card, int index)
    {
        if (cardDisplayPrefab == null || cardContainer == null) return;

        GameObject cardObj = Instantiate(cardDisplayPrefab, cardContainer);
        choiceCards.Add(cardObj);

        CardDisplayUI cardUI = cardObj.GetComponent<CardDisplayUI>();
        if (cardUI != null)
        {
            cardUI.Setup(card, index, OnChoiceClicked);
            cardUI.SetCostVisible(false);
        }
    }

    private void OnChoiceClicked(int index)
    {
        // M2: Determine which player's discovery this is
        int localPlayerId = GetLocalPlayerId();
        if (!pendingDiscoveries.ContainsKey(localPlayerId))
            return;

        var (player, cards) = pendingDiscoveries[localPlayerId];
        if (index < 0 || index >= cards.Count)
            return;

#if PHOTON_UNITY_NETWORKING
        // In online mode, route through network
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
        {
            NetworkGameBridge.Instance.RequestDiscoveryChoice(index);
            Debug.Log($"[DiscoveryUI] Sent discovery choice {index} via network");
        }
        else
#endif
        {
            Card chosen = cards[index];
            player.AddDiscoveryCard(chosen);
            Debug.Log($"[DiscoveryUI] Player {player.playerId} discovered {chosen.cardName}");
        }

        // Clean up this player's pending discovery
        ClearChoices();
        pendingDiscoveries.Remove(localPlayerId);
        PendingDiscoveryByPlayer.Remove(localPlayerId);

        if (discoveryPanel != null)
            discoveryPanel.SetActive(false);
    }

    /// <summary>
    /// Get the local player's ID (1-based) based on game mode.
    /// </summary>
    private int GetLocalPlayerId()
    {
#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode && NetworkGameBridge.Instance != null)
            return NetworkGameBridge.Instance.LocalPlayerSlot + 1;
#endif
        if (GameUIManager.Instance != null)
            return GameUIManager.Instance.GetActivePlayerIndex() + 1;
        return 1;
    }

    private void ClearChoices()
    {
        foreach (var card in choiceCards)
        {
            if (card != null) Destroy(card);
        }
        choiceCards.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeFromAllPlayers();
    }
}
