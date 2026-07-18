using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif
using System.Collections;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// Game Over overlay panel. Subscribes to GameManager.OnGameOver to display
/// standings, placement, and Play Again / Quit buttons.
/// UX18: Enhanced with placement badge (medal colors), standings list,
/// animated panel entry (scale + fade), and styled buttons.
/// </summary>
public class GameOverUI : MonoBehaviour, IThemeable
{
    [Header("Panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Text Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text placementText;
    [SerializeField] private TMP_Text standingsText;

    [Header("T415: Post-Game Stats")]
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private TMP_Text playAgainButtonText;
    [SerializeField] private TMP_Text quitToMenuButtonText;

    [Header("UX18: Placement Badge")]
    [SerializeField] private Image placementBadge;
    [SerializeField] private TMP_Text placementNumber;
    [SerializeField] private Color goldMedal = Tokens.BronzeBright;
    [SerializeField] private Color silverMedal = Tokens.Bone;
    [SerializeField] private Color bronzeMedal = Tokens.Bronze;
    [SerializeField] private Color defaultMedal = Tokens.BoneDim;

    [Header("UX18: Dark Overlay")]
    [SerializeField] private Image darkOverlay;

    [Header("WO-09 Result Stills (R9 — stills only, no video this cycle)")]
    [SerializeField] private Image resultStillImage;   // full-bleed behind panel chrome
    [SerializeField] private Sprite victoryStill;      // victory.jpg
    [SerializeField] private Sprite loseStill;         // lose.jpg

    [Header("UX18: Animation")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float animDuration = Tokens.DurSlow;

    [Header("UX18: Standings Row Container")]
    [SerializeField] private Transform standingsRowContainer;
    [SerializeField] private Color standingsHighlightColor = Tokens.WithAlpha(Tokens.BronzeBright, 0.15f);

    private ThemeConfig currentTheme;
    private bool _subscribed;

    private void Awake()
    {
        // Host must stay active so OnEnable/Start run and OnGameOver is subscribed.
        // Only the panel content starts hidden.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (darkOverlay != null)
            darkOverlay.gameObject.SetActive(false);
        EnsureSubscribed();
    }

    private void OnEnable()
    {
        EnsureSubscribed();
        ThemeManager.OnThemeChanged += ApplyTheme;

        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
    }

    private void OnDisable()
    {
        // Keep game-over subscription even if this GO is toggled; only drop on destroy.
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    private void Start()
    {
        EnsureSubscribed();
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }
        if (quitToMenuButton != null)
        {
            quitToMenuButton.onClick.RemoveListener(OnQuitToMenuClicked);
            quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
        }
    }

    /// <summary>
    /// Scene often saved GameOverUIRoot inactive → never subscribed → win never shows.
    /// Call from GameUIManager.Start as a belt-and-suspenders activate.
    /// </summary>
    public void EnsureReady()
    {
        // Walk up and force every parent active so the panel can display
        Transform t = transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
        EnsureSubscribed();
        if (gameOverPanel != null && !IsShowing)
            gameOverPanel.SetActive(false);
    }

    public bool IsShowing => gameOverPanel != null && gameOverPanel.activeSelf;

    private void EnsureSubscribed()
    {
        if (_subscribed) return;
        GameManager.OnGameOver -= ShowGameOver;
        GameManager.OnGameOver += ShowGameOver;
        _subscribed = true;
    }

    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        if (titleText != null)
            titleText.color = theme.accentColor;

        if (placementText != null)
            placementText.color = theme.textColorLight;

        if (standingsText != null)
            standingsText.color = theme.textColorLight;

        if (playAgainButtonText != null)
            playAgainButtonText.text = theme.playAgainText;

        if (quitToMenuButtonText != null)
            quitToMenuButtonText.text = theme.quitToMenuText;
    }

    private bool IsOnlineMode => GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer;

    private void ApplyResultStill(bool victory)
    {
        if (resultStillImage == null) return;
        Sprite still = victory ? victoryStill : loseStill;
        if (still == null)
        {
            resultStillImage.enabled = false;
            return;
        }
        resultStillImage.sprite = still;
        resultStillImage.color = Color.white;
        resultStillImage.enabled = true;
        resultStillImage.gameObject.SetActive(true);
        resultStillImage.raycastTarget = false;
    }

    private void ShowGameOver(GameOverData data)
    {
        // Force entire hierarchy on (root often saved inactive; combat UI may cover us)
        EnsureReady();

        // Tear down combat presentation so game-over is the only full-screen UI (T840).
        // CombatResultBanner has its own root (not under GameUIManager chrome).
        if (CombatResultBanner.Instance != null)
            CombatResultBanner.Instance.Hide();
        // combatActive=true hides shop/hand/board (and related recruit chrome).
        // combatActive=false was wrong here — it re-showed shop chrome behind the panel.
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.SetCombatPresentationMode(true);
        // Belt-and-suspenders: A1b finds ShopUI/HandUI/BoardUI via FindObjectOfType
        // and requires their roots inactive even if GameUIManager refs were unbound.
        HideRecruitChromeFallback();
        if (TarotBattlegrounds.Combat.Animator.CombatAnimator.Instance != null)
        {
            // Best-effort: hide combat panel if still up after last fight
            var anim = TarotBattlegrounds.Combat.Animator.CombatAnimator.Instance;
            // Skip remaining if still playing
            if (anim.IsPlaying)
                anim.SkipReplay();
        }

        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverUI] gameOverPanel is null — cannot show end screen");
            return;
        }

        // Bring to front, center, full size
        gameOverPanel.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
        var panelRt = gameOverPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panelRt.localScale = Vector3.one;
            panelRt.anchoredPosition = Vector2.zero;
        }
        if (panelRect != null)
        {
            // Content card: center on screen, not hanging half off-canvas
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            if (panelRect.sizeDelta.x < 400f || panelRect.sizeDelta.y < 300f)
                panelRect.sizeDelta = new Vector2(720f, 560f);
            panelRect.localScale = Vector3.one;
        }

        gameOverPanel.SetActive(true);
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        // UX18: Show dark overlay behind the panel — must block clicks to combat underneath
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.raycastTarget = true;
            var ovRt = darkOverlay.rectTransform;
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.offsetMin = ovRt.offsetMax = Vector2.zero;
        }

        // Title
        string title = currentTheme != null ? currentTheme.gameOverTitle : "Game Over";
        if (titleText != null)
            titleText.text = title;

        // Determine local player placement
#if PHOTON_UNITY_NETWORKING
        int localIndex = IsOnlineMode && NetworkGameBridge.Instance != null
            ? NetworkGameBridge.Instance.LocalPlayerSlot
            : GameConfig.HumanPlayerIndex;
#else
        int localIndex = GameConfig.HumanPlayerIndex;
#endif

        int localPlacement = -1;
        for (int i = 0; i < data.standings.Count; i++)
        {
            if (data.standings[i] == localIndex)
            {
                localPlacement = i + 1;
                break;
            }
        }

        // Placement text + WO-09 result still (victory vs lose; no video this cycle)
        bool isVictory = localPlacement == 1;
        ApplyResultStill(isVictory);

        if (placementText != null)
        {
            if (isVictory)
            {
                string victoryText = currentTheme != null ? currentTheme.victoryText : "Victory!";
                placementText.text = victoryText;
                placementText.color = currentTheme != null ? currentTheme.accentColor : Tokens.BronzeBright;
            }
            else if (localPlacement > 0)
            {
                placementText.text = $"You finished #{localPlacement}";
                placementText.color = currentTheme != null ? currentTheme.textColorLight : Tokens.BoneBright;
            }
            else
            {
                placementText.text = "Game Complete";
            }
        }

        // UX18: Placement badge with medal color
        UpdatePlacementBadge(localPlacement);

        // T415: Post-game stats
        PopulateStats(data, localIndex);

        // UX18: Animated entry (scale + fade) — replaces original fade-in
        StartCoroutine(AnimatedPanelEntry());

        // Standings list
        if (standingsText != null)
        {
            string standings = $"Turns Played: {data.totalTurns}\n";
            // T732: "what beat me" recap — surfaced right under the turn count.
            string defeatRecap = BuildDefeatRecap(localIndex, data);
            if (!string.IsNullOrEmpty(defeatRecap))
                standings += defeatRecap + "\n";
            standings += "\n";
            for (int i = 0; i < data.standings.Count; i++)
            {
                int playerIndex = data.standings[i];
                bool isHuman = GameConfig.IsHumanPlayer(playerIndex);

                string label;
#if PHOTON_UNITY_NETWORKING
                // In online mode, show Photon nicknames
                if (IsOnlineMode && NetworkGameBridge.Instance != null)
                {
                    if (NetworkGameBridge.Instance.IsNetworkPlayerSlot(playerIndex))
                    {
                        int actorNum = NetworkGameBridge.Instance.SlotToActor[playerIndex];
                        var photonPlayer = FindPhotonPlayerByActor(actorNum);
                        string nick = photonPlayer != null ? photonPlayer.NickName : $"Player {playerIndex + 1}";
                        bool isLocal = photonPlayer != null && photonPlayer.IsLocal;
                        label = isLocal ? $"{nick} (You)" : nick;
                    }
                    else
                    {
                        label = "(AI)";
                    }
                }
                else
#endif
                {
                    label = isHuman ? "(You)" : "(AI)";
                }

                // UX18: Color-coded rank numbers in standings
                string rankColor = GetMedalHexColor(i + 1);
                standings += $"<color={rankColor}>#{i + 1}</color>  Player {playerIndex + 1} {label}\n";
            }
            standingsText.text = standings;
        }
    }

    /// <summary>
    /// T840: ensure ShopUI / HandUI / BoardUI roots are inactive even when
    /// GameUIManager serialized refs are missing or unbound.
    /// </summary>
    private static void HideRecruitChromeFallback()
    {
        var shop = Object.FindObjectOfType<ShopUI>(true);
        if (shop != null && shop.gameObject.activeSelf)
            shop.gameObject.SetActive(false);
        var hand = Object.FindObjectOfType<HandUI>(true);
        if (hand != null && hand.gameObject.activeSelf)
            hand.gameObject.SetActive(false);
        var board = Object.FindObjectOfType<BoardUI>(true);
        if (board != null && board.gameObject.activeSelf)
            board.gameObject.SetActive(false);
    }

#if PHOTON_UNITY_NETWORKING
    private Photon.Realtime.Player FindPhotonPlayerByActor(int actorNumber)
    {
        if (!PhotonNetwork.IsConnected) return null;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
                return p;
        }
        return null;
    }
#endif

    /// <summary>
    /// T415: Show post-game statistics for the local player.
    /// </summary>
    private void PopulateStats(GameOverData data, int localIndex)
    {
        if (statsText == null) return;

        if (GameManager.Instance == null || localIndex < 0 || localIndex >= GameManager.Instance.players.Count)
        {
            statsText.text = "";
            return;
        }

        var player = GameManager.Instance.players[localIndex];

        // Count tribes on final board
        var tribeCounts = new System.Collections.Generic.Dictionary<TribeType, int>();
        foreach (var card in player.board)
        {
            if (card.tribes == null) continue;
            foreach (var tribe in card.tribes)
            {
                if (tribe == TribeType.None) continue;
                if (!tribeCounts.ContainsKey(tribe)) tribeCounts[tribe] = 0;
                tribeCounts[tribe]++;
            }
        }

        string tribeStr = "";
        foreach (var kvp in tribeCounts)
            tribeStr += $"  {kvp.Key}: {kvp.Value}\n";

        // Compute total board stats
        int totalAtk = 0, totalHp = 0;
        foreach (var card in player.board)
        {
            totalAtk += card.attack;
            totalHp += card.health;
        }

        statsText.text =
            $"<b>Your Stats</b>\n" +
            $"Final Board: {player.board.Count}/7 cards\n" +
            $"Total Stats: {totalAtk} ATK / {totalHp} HP\n" +
            $"Tavern Tier: {player.currentTavernTier}\n" +
            $"\n<b>Tribe Distribution</b>\n" +
            (string.IsNullOrEmpty(tribeStr) ? "  No tribes\n" : tribeStr) +
            $"\nTurns: {data.totalTurns}";
    }

    /// <summary>
    /// T732: Build a one-line "what beat me" recap for a defeated local player,
    /// sourced from MatchTracker's recorded battles. Returns "" for the winner or
    /// when no fatal battle can be found (e.g. disconnect elimination).
    /// </summary>
    private string BuildDefeatRecap(int localIndex, GameOverData data)
    {
        // The winner has no "what beat me".
        if (data.winnerPlayerIndex == localIndex) return "";
        if (MatchTracker.Instance == null) return "";

        var history = MatchTracker.Instance.roundHistory;
        // Scan from the most recent round backward for the battle in which the local
        // player dropped to <= 0 HP — that fight is what knocked them out.
        for (int r = history.Count - 1; r >= 0; r--)
        {
            foreach (var b in history[r].battles)
            {
                bool localIsP1 = b.p1Index == localIndex;
                bool localIsP2 = b.p2Index == localIndex;
                if (!localIsP1 && !localIsP2) continue;

                int localHpAfter = localIsP1 ? b.p1HpAfter : b.p2HpAfter;
                if (localHpAfter > 0) continue; // survived this fight — not the fatal one

                int oppIndex = localIsP1 ? b.p2Index : b.p1Index;
                string oppName = $"Player {oppIndex + 1}" + (GameConfig.IsHumanPlayer(oppIndex) ? "" : " (AI)");
                return $"<color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>Defeated by {oppName} — {b.damage} dmg on turn {history[r].turnNumber}</color>";
            }
        }
        return "";
    }

    private IEnumerator FadeInPanel()
    {
        float duration = Tokens.DurSlow;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// UX18: Animated panel entry — scales from 0.8x to 1.0x with simultaneous alpha fade 0 to 1.
    /// Uses SmoothStep for an ease-in-out feel. Falls back to the basic FadeInPanel if panelRect is not assigned.
    /// </summary>
    private IEnumerator AnimatedPanelEntry()
    {
        // Set initial state
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        if (panelRect != null)
            panelRect.localScale = Vector3.one * 0.8f;

        // If neither panelRect nor panelCanvasGroup are available, nothing to animate
        if (panelRect == null && panelCanvasGroup == null)
            yield break;

        // If only panelCanvasGroup is available (no panelRect), fall back to basic fade
        if (panelRect == null)
        {
            yield return FadeInPanel();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);

            // SmoothStep for easing
            float smooth = t * t * (3f - 2f * t);

            // Scale: 0.8 -> 1.0
            float scale = Mathf.Lerp(0.8f, 1f, smooth);
            panelRect.localScale = Vector3.one * scale;

            // Alpha: 0 -> 1
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = smooth;

            yield return null;
        }

        // Ensure final state
        panelRect.localScale = Vector3.one;
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// UX18: Update the placement badge image and number text based on player placement.
    /// 1st = gold, 2nd = silver, 3rd = bronze, 4th+ = gray.
    /// </summary>
    private void UpdatePlacementBadge(int placement)
    {
        // Set badge color
        if (placementBadge != null)
        {
            Color badgeColor = GetMedalColor(placement);
            placementBadge.color = badgeColor;
            placementBadge.raycastTarget = false;
        }

        // Set placement number with ordinal suffix
        if (placementNumber != null)
        {
            if (placement > 0)
            {
                placementNumber.text = GetOrdinalString(placement);
                placementNumber.color = Tokens.BoneBright;
            }
            else
            {
                placementNumber.text = "-";
                placementNumber.color = Tokens.BoneBright;
            }
        }
    }

    /// <summary>
    /// UX18: Get the medal color for a given placement (1-based).
    /// </summary>
    private Color GetMedalColor(int placement)
    {
        switch (placement)
        {
            case 1: return goldMedal;
            case 2: return silverMedal;
            case 3: return bronzeMedal;
            default: return defaultMedal;
        }
    }

    /// <summary>
    /// UX18: Get a hex color string for TMP rich text based on placement.
    /// </summary>
    private string GetMedalHexColor(int placement)
    {
        Color c = GetMedalColor(placement);
        return $"#{ColorUtility.ToHtmlStringRGB(c)}";
    }

    /// <summary>
    /// UX18: Convert placement number to ordinal string (1st, 2nd, 3rd, 4th...).
    /// </summary>
    private static string GetOrdinalString(int number)
    {
        if (number <= 0) return number.ToString();

        int remainder = number % 100;

        // Handle special cases for 11th, 12th, 13th
        if (remainder >= 11 && remainder <= 13)
            return $"{number}th";

        switch (number % 10)
        {
            case 1: return $"{number}st";
            case 2: return $"{number}nd";
            case 3: return $"{number}rd";
            default: return $"{number}th";
        }
    }

    private void OnPlayAgainClicked()
    {
#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode)
        {
            // Leave room and return to lobby
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("Lobby");
        }
        else
#endif
        {
            // Reload the game scene
            SceneManager.LoadScene("Game");
        }
    }

    private void OnQuitToMenuClicked()
    {
#if PHOTON_UNITY_NETWORKING
        if (IsOnlineMode && PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
#endif
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (_subscribed)
        {
            GameManager.OnGameOver -= ShowGameOver;
            _subscribed = false;
        }
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveAllListeners();
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.RemoveAllListeners();
    }
}
