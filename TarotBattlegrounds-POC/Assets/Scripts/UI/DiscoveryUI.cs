using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// Discovery popup UI. Shows 3 card choices when a triple is formed.
/// The player picks one card which is added to their hand.
/// UX19: Enhanced with backdrop, mystical frame, fan layout, stagger entrance, hover, and selection flash.
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

    [Header("UX19: Visual Enhancement")]
    [SerializeField] private Image backdrop;
    [SerializeField] private Image mysticalFrame;
    [SerializeField] private TMP_Text discoverBanner;
    [SerializeField] private CanvasGroup popupGroup;

    [Header("UX19: Card Fan")]
    [SerializeField] private float fanAngle = 10f;           // Degrees of tilt
    [SerializeField] private float cardSpacing = 160f;
    [SerializeField] private float staggerDelay = Tokens.DurFast;
    [SerializeField] private float cardScaleSpeed = Tokens.DurBase;

    [Header("UX19: Colors")]
    [SerializeField] private Color backdropColor = Tokens.WithAlpha(Tokens.Ash, 0.7f);
    [SerializeField] private Color bannerColor = Tokens.BronzeBright;
    [SerializeField] private Color selectionFlash = Tokens.WithAlpha(Tokens.Ember, 0.8f);

    private List<GameObject> choiceCards = new List<GameObject>();
    private Dictionary<int, (Player player, List<Card> cards)> pendingDiscoveries = new Dictionary<int, (Player, List<Card>)>();
    private ThemeConfig currentTheme;
    private Coroutine showRoutine;
    private Coroutine selectionRoutine;
    private bool isAnimating;

    /// <summary>
    /// Pending discovery cards keyed by player ID for network-triggered discovery.
    /// Set by host, read by NetworkGameBridge when client makes a choice.
    /// </summary>
    public static Dictionary<int, List<Card>> PendingDiscoveryByPlayer { get; set; } = new Dictionary<int, List<Card>>();

    private Coroutine subscribeRoutine;
    private readonly HashSet<Player> _subscribedPlayers = new HashSet<Player>();

    private void Awake()
    {
        // T842: root must stay active (so Start/coroutines run); only the panel child starts hidden.
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
        // T842: GameManager.Start may create/spawn players after DiscoveryUI.Start.
        // Subscribe immediately for whoever exists, then keep trying until the roster is ready.
        SubscribeToAllPlayers();
        if (subscribeRoutine != null) StopCoroutine(subscribeRoutine);
        subscribeRoutine = StartCoroutine(SubscribeWhenPlayersReady());
    }

    /// <summary>
    /// T842: re-subscribe as players appear (spawned AI seats, late InitializePlayers).
    /// </summary>
    private IEnumerator SubscribeWhenPlayersReady()
    {
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            SubscribeToAllPlayers();
            if (GameManager.Instance != null
                && GameManager.Instance.players != null
                && GameManager.Instance.players.Count > 0
                && _subscribedPlayers.Count >= GameManager.Instance.players.Count)
            {
                // One more frame in case late spawns append after Count first settles
                yield return null;
                SubscribeToAllPlayers();
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        subscribeRoutine = null;
    }

    private void SubscribeToAllPlayers()
    {
        if (GameManager.Instance == null || GameManager.Instance.players == null) return;
        foreach (var player in GameManager.Instance.players)
        {
            if (player == null) continue;
            if (_subscribedPlayers.Contains(player)) continue;
            player.OnTripleDiscovery += ShowDiscovery;
            _subscribedPlayers.Add(player);
            Debug.Log($"[DiscoveryUI] Subscribed to Player {player.playerId} OnTripleDiscovery");
        }
    }

    private void UnsubscribeFromAllPlayers()
    {
        foreach (var player in _subscribedPlayers)
        {
            if (player != null)
                player.OnTripleDiscovery -= ShowDiscovery;
        }
        _subscribedPlayers.Clear();
        if (GameManager.Instance == null || GameManager.Instance.players == null) return;
        // Belt-and-suspenders for any player we never tracked
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

        // UX19: Theme the discover banner
        if (discoverBanner != null)
            discoverBanner.color = theme.accentColor;

        // UX19: Theme the mystical frame border
        if (mysticalFrame != null)
            mysticalFrame.color = new Color(theme.accentColor.r, theme.accentColor.g, theme.accentColor.b, 0.6f);
    }

    private bool IsOnlineMode => GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer;

    private void ShowDiscovery(Player player, List<Card> cards)
    {
        if (discoveryPanel == null || cards == null || cards.Count == 0)
        {
            Debug.LogWarning($"[DiscoveryUI] Cannot show discovery: panel={discoveryPanel != null}, cards={cards != null}, count={cards?.Count ?? 0}");
            return;
        }

        // Only show discovery UI for human players
        if (!GameConfig.IsHumanPlayer(player.playerId - 1))
        {
            // AI auto-picks the first card; AddDiscoveryCard returns unchosen reserved pool cards.
            player.AddDiscoveryCard(cards[0]);
            Debug.Log($"[DiscoveryUI] AI Player {player.playerId} auto-picked {cards[0].cardName} (unchosen returned to pool)");
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
                // Not our discovery, skip UI (stored for network resolution)
                Debug.Log($"[DiscoveryUI/M2] Player {player.playerId} discovery stored, but not showing UI (localSlot={localSlot})");
                return;
            }
            Debug.Log($"[DiscoveryUI/M2] Showing discovery for local player {player.playerId} (slot {localSlot})");
        }
#endif

        // BUG FIX M2: Always clear previous choices before showing new discovery
        // This ensures UI is fresh even if panel was already active
        ClearChoices();

        // Stop any in-progress animation
        if (showRoutine != null) StopCoroutine(showRoutine);
        if (selectionRoutine != null) StopCoroutine(selectionRoutine);
        isAnimating = false;

        // Ensure panel is active and visible
        discoveryPanel.SetActive(false); // Force deactivate first
        discoveryPanel.SetActive(true);  // Then reactivate to trigger UI refresh

        if (titleText != null)
            titleText.text = "Triple! Choose a Card:";

        for (int i = 0; i < cards.Count; i++)
        {
            CreateChoiceCard(cards[i], i);
        }

        // UX19: Animated entrance flow
        showRoutine = StartCoroutine(AnimatedShowRoutine());

        Debug.Log($"[DiscoveryUI/M2] Displayed {cards.Count} discovery choices for Player {player.playerId}");
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
        // UX19: Prevent clicks during animation
        if (isAnimating) return;

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

        // UX19: Animated selection flash + close
        selectionRoutine = StartCoroutine(AnimatedSelectionRoutine(index, localPlayerId));
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

    // ====== UX19: Animated Discovery Flow ======

    /// <summary>
    /// UX19: Animated show routine. Fades backdrop, slides banner, staggers cards in fan layout.
    /// </summary>
    private IEnumerator AnimatedShowRoutine()
    {
        isAnimating = true;

        // Step 1: Set initial states
        if (backdrop != null)
        {
            backdrop.gameObject.SetActive(true);
            backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0f);
        }

        if (discoverBanner != null)
        {
            discoverBanner.gameObject.SetActive(true);
            RectTransform bannerRect = discoverBanner.GetComponent<RectTransform>();
            if (bannerRect != null)
                bannerRect.anchoredPosition = new Vector2(bannerRect.anchoredPosition.x, 80f); // Start above
        }

        if (popupGroup != null)
            popupGroup.alpha = 0f;

        if (mysticalFrame != null)
        {
            mysticalFrame.gameObject.SetActive(true);
            Color frameColor = mysticalFrame.color;
            mysticalFrame.color = new Color(frameColor.r, frameColor.g, frameColor.b, 0f);
        }

        // Hide all cards initially (scale to zero)
        for (int i = 0; i < choiceCards.Count; i++)
        {
            if (choiceCards[i] != null)
                choiceCards[i].transform.localScale = Vector3.zero;
        }

        // Step 2: Fade in backdrop (0.2s)
        float elapsed = 0f;
        float backdropDuration = Tokens.DurBase;
        while (elapsed < backdropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / backdropDuration);
            if (backdrop != null)
                backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, backdropColor.a * t);
            if (popupGroup != null)
                popupGroup.alpha = t;
            if (mysticalFrame != null)
            {
                Color fc = mysticalFrame.color;
                mysticalFrame.color = new Color(fc.r, fc.g, fc.b, 0.6f * t);
            }
            yield return null;
        }

        if (backdrop != null)
            backdrop.color = backdropColor;
        if (popupGroup != null)
            popupGroup.alpha = 1f;

        // Step 3: Banner slides down (0.3s)
        if (discoverBanner != null)
        {
            RectTransform bannerRect = discoverBanner.GetComponent<RectTransform>();
            if (bannerRect != null)
            {
                float bannerStartY = 80f;
                float bannerEndY = bannerRect.anchoredPosition.y > 40f ? 0f : bannerRect.anchoredPosition.y;
                // Temporarily store original position and use 0 as target
                float targetY = 0f;
                elapsed = 0f;
                float bannerDuration = Tokens.DurBase;
                while (elapsed < bannerDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / bannerDuration));
                    bannerRect.anchoredPosition = new Vector2(bannerRect.anchoredPosition.x, Mathf.Lerp(bannerStartY, targetY, t));
                    yield return null;
                }
                bannerRect.anchoredPosition = new Vector2(bannerRect.anchoredPosition.x, targetY);
            }
        }

        // Step 4: Layout cards in fan pattern and stagger entrance
        LayoutCardsInFan();

        for (int i = 0; i < choiceCards.Count; i++)
        {
            if (choiceCards[i] != null)
            {
                StartCoroutine(ScaleCardIn(choiceCards[i].transform));
                yield return new WaitForSeconds(staggerDelay);
            }
        }

        // Wait for last card to finish scaling
        yield return new WaitForSeconds(cardScaleSpeed);

        isAnimating = false;
    }

    /// <summary>
    /// UX19: Layout cards in fan pattern with tilt and spacing.
    /// </summary>
    private void LayoutCardsInFan()
    {
        int count = choiceCards.Count;
        if (count == 0) return;

        float[] rotations;
        float[] xOffsets;

        if (count == 3)
        {
            rotations = new float[] { -fanAngle, 0f, fanAngle };
            xOffsets = new float[] { -cardSpacing, 0f, cardSpacing };
        }
        else if (count == 2)
        {
            rotations = new float[] { -fanAngle * 0.5f, fanAngle * 0.5f };
            xOffsets = new float[] { -cardSpacing * 0.5f, cardSpacing * 0.5f };
        }
        else
        {
            rotations = new float[] { 0f };
            xOffsets = new float[] { 0f };
        }

        for (int i = 0; i < count && i < rotations.Length; i++)
        {
            RectTransform rt = choiceCards[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(xOffsets[i], 0);
                rt.localRotation = Quaternion.Euler(0, 0, rotations[i]);
            }
        }
    }

    /// <summary>
    /// UX19: Scale a card from zero to full size with overshoot bounce.
    /// </summary>
    private IEnumerator ScaleCardIn(Transform cardTransform)
    {
        if (cardTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < cardScaleSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cardScaleSpeed);

            // Ease-out back curve for overshoot bounce
            float s = 1.70158f;
            float scale = 1f + (s + 1f) * Mathf.Pow(t - 1f, 3f) + s * Mathf.Pow(t - 1f, 2f);
            scale = Mathf.Max(0f, scale);

            cardTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        cardTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// UX19: Animated selection — flash chosen card white, fade + shrink others, close popup.
    /// </summary>
    private IEnumerator AnimatedSelectionRoutine(int chosenIndex, int localPlayerId)
    {
        isAnimating = true;

        // Flash the chosen card white
        if (chosenIndex >= 0 && chosenIndex < choiceCards.Count && choiceCards[chosenIndex] != null)
        {
            Image chosenBg = choiceCards[chosenIndex].GetComponentInChildren<Image>();
            if (chosenBg != null)
            {
                Color originalColor = chosenBg.color;
                chosenBg.color = selectionFlash;

                // Also scale up the chosen card slightly
                RectTransform chosenRect = choiceCards[chosenIndex].GetComponent<RectTransform>();
                if (chosenRect != null)
                    chosenRect.localScale = Vector3.one * 1.1f;

                // Flash duration
                yield return new WaitForSeconds(Tokens.DurFast);

                // Restore color
                chosenBg.color = originalColor;
            }
        }

        // Fade + shrink non-chosen cards
        float fadeDuration = Tokens.DurBase;
        float elapsed = 0f;

        // Cache initial states for non-chosen cards
        List<CanvasGroup> fadeGroups = new List<CanvasGroup>();
        for (int i = 0; i < choiceCards.Count; i++)
        {
            if (i == chosenIndex || choiceCards[i] == null) continue;
            CanvasGroup cg = choiceCards[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = choiceCards[i].AddComponent<CanvasGroup>();
            fadeGroups.Add(cg);
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade + shrink non-chosen
            foreach (var cg in fadeGroups)
            {
                if (cg != null)
                {
                    cg.alpha = 1f - t;
                    cg.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
                }
            }

            yield return null;
        }

        // Brief hold to let player see their choice
        yield return new WaitForSeconds(Tokens.DurFast);

        // Fade out the entire popup
        float popupFadeDuration = Tokens.DurBase;
        elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popupFadeDuration);

            if (popupGroup != null)
                popupGroup.alpha = 1f - t;
            if (backdrop != null)
            {
                Color bc = backdrop.color;
                backdrop.color = new Color(bc.r, bc.g, bc.b, backdropColor.a * (1f - t));
            }

            yield return null;
        }

        // Clean up
        ClearChoices();
        pendingDiscoveries.Remove(localPlayerId);
        PendingDiscoveryByPlayer.Remove(localPlayerId);

        if (discoveryPanel != null)
            discoveryPanel.SetActive(false);

        isAnimating = false;
    }

    /// <summary>
    /// UX19: Handle hover highlight — card lifts up, scales, and glows.
    /// Called from card EventTrigger or pointer events.
    /// </summary>
    public void OnCardHoverEnter(int index)
    {
        if (isAnimating) return;
        if (index < 0 || index >= choiceCards.Count || choiceCards[index] == null) return;

        RectTransform rt = choiceCards[index].GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one * 1.1f;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 15f); // Lift up
        }
    }

    /// <summary>
    /// UX19: Handle hover exit — card returns to fan position.
    /// </summary>
    public void OnCardHoverExit(int index)
    {
        if (isAnimating) return;
        if (index < 0 || index >= choiceCards.Count || choiceCards[index] == null) return;

        RectTransform rt = choiceCards[index].GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            // Restore fan Y position (0)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 0f);
        }
    }

    private void OnDestroy()
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        if (selectionRoutine != null) StopCoroutine(selectionRoutine);
        if (subscribeRoutine != null) StopCoroutine(subscribeRoutine);
        UnsubscribeFromAllPlayers();
    }

    /// <summary>T842 / AutoPlaytest v2: panel is showing choices for the local player.</summary>
    public bool IsShowing => discoveryPanel != null && discoveryPanel.activeSelf;
}
