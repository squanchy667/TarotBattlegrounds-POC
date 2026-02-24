using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private Color goldMedal = new Color(1f, 0.82f, 0.12f);
    [SerializeField] private Color silverMedal = new Color(0.78f, 0.78f, 0.85f);
    [SerializeField] private Color bronzeMedal = new Color(0.8f, 0.5f, 0.2f);
    [SerializeField] private Color defaultMedal = new Color(0.5f, 0.5f, 0.5f);

    [Header("UX18: Dark Overlay")]
    [SerializeField] private Image darkOverlay;

    [Header("UX18: Animation")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float animDuration = 0.4f;

    [Header("UX18: Standings Row Container")]
    [SerializeField] private Transform standingsRowContainer;
    [SerializeField] private Color standingsHighlightColor = new Color(1f, 0.82f, 0.12f, 0.15f);

    private ThemeConfig currentTheme;

    private void Awake()
    {
        // Start hidden
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += ShowGameOver;
        ThemeManager.OnThemeChanged += ApplyTheme;

        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= ShowGameOver;
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    private void Start()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(OnQuitToMenuClicked);
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

    private void ShowGameOver(GameOverData data)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // UX18: Show dark overlay behind the panel
        if (darkOverlay != null)
        {
            darkOverlay.gameObject.SetActive(true);
            darkOverlay.raycastTarget = false;
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

        // Placement text
        if (placementText != null)
        {
            if (localPlacement == 1)
            {
                string victoryText = currentTheme != null ? currentTheme.victoryText : "Victory!";
                placementText.text = victoryText;
                placementText.color = currentTheme != null ? currentTheme.accentColor : Color.yellow;
            }
            else if (localPlacement > 0)
            {
                placementText.text = $"You finished #{localPlacement}";
                placementText.color = currentTheme != null ? currentTheme.textColorLight : Color.white;
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
            string standings = $"Turns Played: {data.totalTurns}\n\n";
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

    private IEnumerator FadeInPanel()
    {
        float duration = 0.4f;
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
                placementNumber.color = Color.white;
            }
            else
            {
                placementNumber.text = "-";
                placementNumber.color = Color.white;
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
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveAllListeners();
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.RemoveAllListeners();
    }
}
