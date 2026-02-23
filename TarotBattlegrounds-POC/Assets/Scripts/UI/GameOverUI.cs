using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif
using System.Collections.Generic;

/// <summary>
/// Game Over overlay panel. Subscribes to GameManager.OnGameOver to display
/// standings, placement, and Play Again / Quit buttons.
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

        // T415: Post-game stats
        PopulateStats(data, localIndex);

        // T415: Fade-in animation
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            StartCoroutine(FadeInPanel());
        }

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

                standings += $"#{i + 1}  Player {playerIndex + 1} {label}\n";
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

    private System.Collections.IEnumerator FadeInPanel()
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
