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
    private Player pendingPlayer;
    private List<Card> pendingCards;
    private ThemeConfig currentTheme;

    /// <summary>
    /// Pending discovery cards for network-triggered discovery.
    /// Set by host, read by NetworkGameBridge when client makes a choice.
    /// </summary>
    public static List<Card> PendingDiscoveryCards { get; set; }

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

        // Store pending discovery cards for network use
        PendingDiscoveryCards = cards;

        pendingPlayer = player;
        pendingCards = cards;

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
        if (pendingPlayer == null || pendingCards == null || index < 0 || index >= pendingCards.Count)
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
            Card chosen = pendingCards[index];
            pendingPlayer.AddDiscoveryCard(chosen);
            Debug.Log($"[DiscoveryUI] Player {pendingPlayer.playerId} discovered {chosen.cardName}");
        }

        // Clean up
        ClearChoices();
        pendingPlayer = null;
        pendingCards = null;
        PendingDiscoveryCards = null;

        if (discoveryPanel != null)
            discoveryPanel.SetActive(false);
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
