using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// UI component for displaying combat log during battles with theming support.
/// </summary>
public class CombatLogUI : MonoBehaviour, IThemeable
{
    [Header("Combat Log Panel")]
    [SerializeField] private GameObject combatLogPanel;
    [SerializeField] private Transform logEntriesContainer;
    [SerializeField] private GameObject logEntryPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Combat Info")]
    [SerializeField] private TMP_Text combatTitleText;
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private TMP_Text combatStatusText;
    [SerializeField] private TMP_Text resultText;

    [Header("Panel Visuals")]
    [SerializeField] private Image panelBackground;

    [Header("Settings")]
    [SerializeField] private int maxLogEntries = 50;

    [Header("Entry Colors (overridden by theme)")]
    [SerializeField] private Color attackColor = Tokens.Blood;
    [SerializeField] private Color counterattackColor = Tokens.Ember;
    [SerializeField] private Color deathColor = Tokens.BoneDim;
    [SerializeField] private Color aegisColor = Tokens.BronzeBright;
    [SerializeField] private Color echoColor = Tokens.Ember;
    [SerializeField] private Color resultColor = Tokens.BronzeBright;
    [SerializeField] private Color defaultColor = Tokens.BoneBright;

    private List<GameObject> logEntries = new List<GameObject>();
    private string currentPlayer1;
    private string currentPlayer2;
    private bool isLocalPlayerBattle;
    private ThemeConfig currentTheme;

    private void Awake()
    {
        // Hide panel initially
        if (combatLogPanel != null)
            combatLogPanel.SetActive(false);
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
    }

    /// <summary>
    /// Apply theme to combat log UI.
    /// </summary>
    public void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;
        currentTheme = theme;

        // Apply title from theme
        if (combatTitleText != null)
            combatTitleText.text = theme.combatPhaseTitle;

        // Apply colors
        if (panelBackground != null)
            panelBackground.color = theme.secondaryColor;

        // Use theme colors for combat entries
        attackColor = theme.negativeColor;
        resultColor = theme.accentColor;
        echoColor = theme.positiveColor;
        defaultColor = theme.textColorLight;

        // Update text colors
        if (combatTitleText != null) combatTitleText.color = theme.primaryColor;
        if (combatStatusText != null) combatStatusText.color = theme.textColorLight;
        if (player1NameText != null) player1NameText.color = theme.textColorLight;
        if (player2NameText != null) player2NameText.color = theme.textColorLight;
    }
    
    /// <summary>
    /// Called when combat starts
    /// </summary>
    public void OnCombatStart(string player1, string player2)
    {
        Debug.Log($"[CombatLogUI.OnCombatStart] START - {player1} vs {player2}");
        currentPlayer1 = player1;
        currentPlayer2 = player2;

        // M7: Check if this battle involves the local player
        isLocalPlayerBattle = IsLocalPlayerInBattle(player1, player2);
        if (!isLocalPlayerBattle)
        {
            Debug.Log($"[CombatLogUI] Skipping battle display — not local player's battle");
            return;
        }

        // Show panel
        if (combatLogPanel != null)
            combatLogPanel.SetActive(true);

        // Clear previous entries
        ClearLog();
        
        // Update player names
        if (player1NameText != null)
            player1NameText.text = player1;
        if (player2NameText != null)
            player2NameText.text = player2;
        
        // Set status
        if (combatStatusText != null)
            combatStatusText.text = "Combat in progress...";
        
        if (resultText != null)
        {
            resultText.text = "";
            resultText.gameObject.SetActive(false);
        }
        
        // Add start entry
        AddLogEntry(new CombatLogEntry
        {
            Type = CombatLogEntry.LogType.TurnStart,
            Message = $"⚔️ COMBAT BEGINS: {player1} vs {player2}",
            TurnNumber = 0
        });
    }
    
    /// <summary>
    /// Called when combat ends
    /// </summary>
    public void OnCombatEnd(string winner, int damage)
    {
        // M7: Skip end display for battles not involving the local player
        if (!isLocalPlayerBattle) return;

        if (combatStatusText != null)
            combatStatusText.text = "Combat finished!";

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            if (winner == "Tie")
            {
                string tieText = currentTheme != null ? currentTheme.tieText : "Tie!";
                resultText.text = $"🤝 {tieText} - No damage dealt";
                resultText.color = defaultColor;
            }
            else
            {
                string victoryText = currentTheme != null ? currentTheme.victoryText : "Victory!";
                resultText.text = $"🏆 {winner} {victoryText}\n💥 {damage} damage dealt";
                resultText.color = resultColor;
            }
        }
    }
    
    /// <summary>
    /// Add a log entry to the combat display
    /// </summary>
    public void AddLogEntry(CombatLogEntry entry)
    {
        Debug.Log($"[CombatLogUI.AddLogEntry] Received: {entry.Message} (Turn {entry.TurnNumber}, Type: {entry.Type})");

        // M7: Skip log entries for battles not involving the local player
        if (!isLocalPlayerBattle) return;

        if (logEntryPrefab == null || logEntriesContainer == null)
        {
            Debug.Log($"[Combat Log] FALLBACK - prefab or container null: {entry.Message}");
            return;
        }
        
        // Create entry object
        GameObject entryObj = Instantiate(logEntryPrefab, logEntriesContainer);
        logEntries.Add(entryObj);
        
        // Get text component
        TMP_Text entryText = entryObj.GetComponent<TMP_Text>();
        if (entryText == null)
            entryText = entryObj.GetComponentInChildren<TMP_Text>();
        
        if (entryText != null)
        {
            entryText.text = FormatLogEntry(entry);
            entryText.color = GetEntryColor(entry.Type);
            entryText.fontSize = Tokens.TextCaption;
            entryText.enableAutoSizing = true;
            entryText.fontSizeMin = Tokens.TextCaption;
            entryText.fontSizeMax = Tokens.TextCaption;
        }
        
        // Enforce max entries
        while (logEntries.Count > maxLogEntries)
        {
            GameObject oldEntry = logEntries[0];
            logEntries.RemoveAt(0);
            Destroy(oldEntry);
        }
        
        // Auto-scroll to bottom
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Format log entry with icons and details
    /// </summary>
    private string FormatLogEntry(CombatLogEntry entry)
    {
        string icon = GetEntryIcon(entry.Type);
        string turnPrefix = entry.TurnNumber > 0 ? $"[T{entry.TurnNumber}] " : "";
        
        switch (entry.Type)
        {
            case CombatLogEntry.LogType.Attack:
                return $"{turnPrefix}{icon} <b>{entry.AttackerName}</b> attacks <b>{entry.DefenderName}</b> for <color=#{ColorUtility.ToHtmlStringRGB(Tokens.Blood)}>{entry.Damage}</color> dmg → {entry.RemainingHealth} HP";
                
            case CombatLogEntry.LogType.Counterattack:
                return $"{turnPrefix}{icon} <b>{entry.DefenderName}</b> counters <b>{entry.AttackerName}</b> for <color=#{ColorUtility.ToHtmlStringRGB(Tokens.Ember)}>{entry.Damage}</color> dmg → {entry.RemainingHealth} HP";
                
            case CombatLogEntry.LogType.CardDeath:
                return $"{turnPrefix}{icon} <b>{entry.DefenderName}</b> is destroyed! ({entry.DefenderOwner})";
                
            case CombatLogEntry.LogType.AegisBlock:
                return $"{turnPrefix}{icon} <b>{entry.DefenderName}</b>'s Aegis blocks the attack!";
                
            case CombatLogEntry.LogType.EchoTrigger:
                return $"{turnPrefix}{icon} <b>{entry.AttackerName}</b>'s Echo: +{entry.Damage} ATK to <b>{entry.DefenderName}</b>";
                
            case CombatLogEntry.LogType.GuardianTaunt:
                return $"{turnPrefix}{icon} <b>{entry.DefenderName}</b> (Guardian) forces attack!";
                
            case CombatLogEntry.LogType.BattleResult:
                return $"\n{icon} {entry.Message}";
                
            default:
                return $"{turnPrefix}{icon} {entry.Message}";
        }
    }
    
    /// <summary>
    /// Get icon for log entry type
    /// </summary>
    private string GetEntryIcon(CombatLogEntry.LogType type)
    {
        switch (type)
        {
            case CombatLogEntry.LogType.Attack: return "⚔️";
            case CombatLogEntry.LogType.Counterattack: return "🔄";
            case CombatLogEntry.LogType.CardDeath: return "💀";
            case CombatLogEntry.LogType.AegisBlock: return "🛡️";
            case CombatLogEntry.LogType.EchoTrigger: return "📢";
            case CombatLogEntry.LogType.GuardianTaunt: return "🎯";
            case CombatLogEntry.LogType.TurnStart: return "▶️";
            case CombatLogEntry.LogType.BattleResult: return "🏆";
            default: return "•";
        }
    }
    
    /// <summary>
    /// Get color for log entry type
    /// </summary>
    private Color GetEntryColor(CombatLogEntry.LogType type)
    {
        switch (type)
        {
            case CombatLogEntry.LogType.Attack: return attackColor;
            case CombatLogEntry.LogType.Counterattack: return counterattackColor;
            case CombatLogEntry.LogType.CardDeath: return deathColor;
            case CombatLogEntry.LogType.AegisBlock: return aegisColor;
            case CombatLogEntry.LogType.EchoTrigger: return echoColor;
            case CombatLogEntry.LogType.BattleResult: return resultColor;
            default: return defaultColor;
        }
    }
    
    /// <summary>
    /// Clear all log entries
    /// </summary>
    public void ClearLog()
    {
        Debug.Log($"[CombatLogUI.ClearLog] Clearing {logEntries.Count} entries");
        foreach (var entry in logEntries)
        {
            if (entry != null)
                Destroy(entry);
        }
        logEntries.Clear();
    }
    
    /// <summary>
    /// Show/hide the combat log panel
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (combatLogPanel != null)
            combatLogPanel.SetActive(visible);
    }
    
    /// <summary>
    /// Toggle combat log visibility
    /// </summary>
    public void ToggleVisibility()
    {
        if (combatLogPanel != null)
            combatLogPanel.SetActive(!combatLogPanel.activeSelf);
    }

    /// <summary>
    /// Check if either combatant is the local player.
    /// M7: Used to filter combat log to only show local player's battle.
    /// </summary>
    private bool IsLocalPlayerInBattle(string player1, string player2)
    {
        string localPlayerName = GetLocalPlayerName();
        if (string.IsNullOrEmpty(localPlayerName))
            return true; // Fallback: show all battles if we can't determine local player

        return player1.Contains(localPlayerName) || player2.Contains(localPlayerName);
    }

    private string GetLocalPlayerName()
    {
#if PHOTON_UNITY_NETWORKING
        if (GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer
            && NetworkGameBridge.Instance != null)
        {
            int localSlot = NetworkGameBridge.Instance.LocalPlayerSlot;
            if (localSlot >= 0)
                return $"Player {localSlot + 1}";
        }
#endif
        if (GameUIManager.Instance != null)
        {
            int activeIndex = GameUIManager.Instance.GetActivePlayerIndex();
            return $"Player {activeIndex + 1}";
        }
        return null;
    }
}
