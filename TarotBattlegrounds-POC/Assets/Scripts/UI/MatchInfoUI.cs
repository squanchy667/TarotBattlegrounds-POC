using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fullscreen match info scoreboard for testing.
/// Press "i" button to show. Click anywhere outside the panel to dismiss.
/// </summary>
public class MatchInfoUI : MonoBehaviour, IThemeable
{
    [Header("Panel")]
    [SerializeField] private GameObject infoPanel;

    [Header("Dismiss Overlay")]
    [SerializeField] private Button dismissOverlay;

    [Header("Text Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text playerStatusText;
    [SerializeField] private TMP_Text lastRoundText;
    [SerializeField] private TMP_Text boardsText;
    [SerializeField] private TMP_Text historyText;

    [Header("Toggle Button")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;

    private ThemeConfig currentTheme;
    private bool isVisible;
    private bool _wired;

    private void Awake()
    {
        // Host must stay active so Awake/Start can wire the "i" toggle.
        // Only the content panel + overlay hide; the holder itself stays on.
        if (infoPanel != null)
            infoPanel.SetActive(false);
        if (dismissOverlay != null)
            dismissOverlay.gameObject.SetActive(false);
        isVisible = false;
        EnsureWired();
    }

    private void OnEnable()
    {
        ThemeManager.OnThemeChanged += ApplyTheme;
        if (ThemeManager.ActiveTheme != null)
            ApplyTheme(ThemeManager.ActiveTheme);
        EnsureWired();
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    private void Start()
    {
        EnsureWired();
    }

    /// <summary>
    /// Wire toggle/dismiss listeners. Safe to call when this component was left inactive
    /// in the scene (Start never ran → "i" button did nothing).
    /// </summary>
    public void EnsureWired()
    {
        // If someone deactivated the whole holder, bring it back so children can show.
        // SetActive may run Awake/OnEnable which also calls EnsureWired — re-check _wired after.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (_wired) return;

        if (toggleButton == null)
        {
            // Fallback: find the always-visible top-right "i" button
            var all = FindObjectsOfType<Button>(true);
            foreach (var b in all)
            {
                if (b != null && b.name == "MatchInfoToggleButton")
                {
                    toggleButton = b;
                    break;
                }
            }
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePanel);
            toggleButton.onClick.AddListener(TogglePanel);
        }
        if (dismissOverlay != null)
        {
            dismissOverlay.onClick.RemoveListener(HidePanel);
            dismissOverlay.onClick.AddListener(HidePanel);
        }
        _wired = toggleButton != null;
    }

    public void TogglePanel()
    {
        if (isVisible) HidePanel();
        else ShowPanel();
    }

    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        if (titleText != null) titleText.color = theme.accentColor;
        if (turnText != null) turnText.color = theme.textColorLight;
        if (playerStatusText != null) playerStatusText.color = theme.textColorLight;
        if (lastRoundText != null) lastRoundText.color = theme.textColorLight;
        if (boardsText != null) boardsText.color = theme.textColorLight;
        if (historyText != null) historyText.color = theme.textColorLight;
    }

    public void ShowPanel()
    {
        // Holder may have been saved inactive — force on before showing children.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        isVisible = true;
        if (dismissOverlay != null)
            dismissOverlay.gameObject.SetActive(true);
        if (infoPanel != null)
            infoPanel.SetActive(true);
        RefreshDisplay();
    }

    public void HidePanel()
    {
        isVisible = false;
        if (infoPanel != null)
            infoPanel.SetActive(false);
        if (dismissOverlay != null)
            dismissOverlay.gameObject.SetActive(false);
        // Keep this host active so the "i" button stays wired next click.
    }

    public void AutoShowAfterCombat()
    {
        ShowPanel();
    }

    public void RefreshDisplay()
    {
        if (!isVisible) return;

        if (turnText != null && GameManager.Instance != null)
        {
            string phase = GameManager.Instance.CurrentPhase.ToString();
            turnText.text = $"Turn {GameManager.Instance.TurnNumber}  |  Phase: {phase}  |  Alive: {CountAlive()}";
        }

        if (playerStatusText != null)
            playerStatusText.text = BuildPlayerStatusText();

        if (boardsText != null)
            boardsText.text = BuildBoardsText();

        if (lastRoundText != null)
            lastRoundText.text = BuildLastRoundText();

        if (historyText != null)
            historyText.text = BuildHistoryText();
    }

    private int CountAlive()
    {
        if (GameManager.Instance == null) return 0;
        int count = 0;
        for (int i = 0; i < GameManager.Instance.playerCount; i++)
            if (GameManager.Instance.GetPlayerHealth(i) > 0) count++;
        return count;
    }

    private string BuildPlayerStatusText()
    {
        if (MatchTracker.Instance == null || GameManager.Instance == null) return "No data";

        var gm = GameManager.Instance;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < gm.playerCount && i < gm.players.Count; i++)
        {
            var player = gm.players[i];
            if (player == null) continue;

            int hp = gm.GetPlayerHealth(i);
            bool isHuman = GameConfig.IsHumanPlayer(i);
            string tag = isHuman ? "(You)" : "(AI)";
            string status = hp > 0 ? $"HP: {hp}" : "ELIMINATED";

            sb.AppendLine($"  P{i + 1} {tag}  {status}  |  Tier {player.currentTavernTier}  |  Coins: {player.coins}  |  Hand: {player.hand.Count}  |  Board: {player.board.Count}/7");
        }
        return sb.ToString().TrimEnd();
    }

    private string BuildBoardsText()
    {
        if (GameManager.Instance == null) return "";

        var gm = GameManager.Instance;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < gm.playerCount && i < gm.players.Count; i++)
        {
            var player = gm.players[i];
            if (player == null || gm.GetPlayerHealth(i) <= 0) continue;

            sb.Append($"  P{i + 1} Board: ");
            if (player.board.Count == 0)
            {
                sb.AppendLine("(empty)");
            }
            else
            {
                var cards = new List<string>();
                foreach (var card in player.board)
                {
                    string golden = card.isGolden ? "*" : "";
                    cards.Add($"{golden}{card.cardName} {card.attack}/{card.health}");
                }
                sb.AppendLine(string.Join(", ", cards));
            }
        }
        return sb.ToString().TrimEnd();
    }

    private string BuildLastRoundText()
    {
        if (MatchTracker.Instance == null) return "";

        var lastRound = MatchTracker.Instance.GetLastRound();
        if (lastRound == null) return "No battles yet";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"-- Last Round (Turn {lastRound.turnNumber}) --");
        foreach (var b in lastRound.battles)
        {
            string p1 = $"P{b.p1Index + 1}";
            string p2 = $"P{b.p2Index + 1}";
            if (b.winnerIndex == -1)
                sb.AppendLine($"  {p1} vs {p2}: Tie ({b.damage} dmg each)  |  {p1}={b.p1HpAfter}hp  {p2}={b.p2HpAfter}hp");
            else if (b.winnerIndex == b.p1Index)
                sb.AppendLine($"  {p1} vs {p2}: {p1} won ({b.damage} dmg)  |  {p1}={b.p1HpAfter}hp  {p2}={b.p2HpAfter}hp");
            else
                sb.AppendLine($"  {p1} vs {p2}: {p2} won ({b.damage} dmg)  |  {p1}={b.p1HpAfter}hp  {p2}={b.p2HpAfter}hp");
        }
        return sb.ToString().TrimEnd();
    }

    private string BuildHistoryText()
    {
        if (MatchTracker.Instance == null) return "";

        var history = MatchTracker.Instance.roundHistory;
        if (history.Count <= 1) return ""; // Last round already shown above

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- Full History --");

        // Show all rounds except the last (already shown above), newest first
        for (int r = history.Count - 2; r >= 0; r--)
        {
            var round = history[r];
            sb.Append($"  Turn {round.turnNumber}: ");
            var results = new List<string>();
            foreach (var b in round.battles)
            {
                string p1 = $"P{b.p1Index + 1}";
                string p2 = $"P{b.p2Index + 1}";
                if (b.winnerIndex == -1)
                    results.Add($"{p1}={p2} tie");
                else if (b.winnerIndex == b.p1Index)
                    results.Add($"{p1}>{p2} ({b.damage}dmg)");
                else
                    results.Add($"{p2}>{p1} ({b.damage}dmg)");
            }
            sb.AppendLine(string.Join("  |  ", results));
        }
        return sb.ToString().TrimEnd();
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveAllListeners();
        if (dismissOverlay != null)
            dismissOverlay.onClick.RemoveAllListeners();
    }
}
