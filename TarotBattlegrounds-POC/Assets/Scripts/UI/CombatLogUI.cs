using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI component for displaying combat log during battles
/// </summary>
public class CombatLogUI : MonoBehaviour
{
    [Header("Combat Log Panel")]
    [SerializeField] private GameObject combatLogPanel;
    [SerializeField] private Transform logEntriesContainer;
    [SerializeField] private GameObject logEntryPrefab;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Combat Info")]
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private TMP_Text combatStatusText;
    [SerializeField] private TMP_Text resultText;
    
    [Header("Settings")]
    [SerializeField] private int maxLogEntries = 50;
    [SerializeField] private float autoScrollDelay = 0.1f;
    
    [Header("Entry Colors")]
    [SerializeField] private Color attackColor = new Color(1f, 0.5f, 0.5f);
    [SerializeField] private Color counterattackColor = new Color(0.5f, 0.5f, 1f);
    [SerializeField] private Color deathColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color aegisColor = new Color(1f, 1f, 0.5f);
    [SerializeField] private Color echoColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private Color resultColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color defaultColor = Color.white;
    
    private List<GameObject> logEntries = new List<GameObject>();
    private string currentPlayer1;
    private string currentPlayer2;
    
    private void Awake()
    {
        // Hide panel initially
        if (combatLogPanel != null)
            combatLogPanel.SetActive(false);
    }
    
    /// <summary>
    /// Called when combat starts
    /// </summary>
    public void OnCombatStart(string player1, string player2)
    {
        currentPlayer1 = player1;
        currentPlayer2 = player2;
        
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
        if (combatStatusText != null)
            combatStatusText.text = "Combat finished!";
        
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            if (winner == "Tie")
            {
                resultText.text = "🤝 TIE - No damage dealt";
                resultText.color = defaultColor;
            }
            else
            {
                resultText.text = $"🏆 {winner} WINS!\n💥 {damage} damage dealt";
                resultText.color = resultColor;
            }
        }
    }
    
    /// <summary>
    /// Add a log entry to the combat display
    /// </summary>
    public void AddLogEntry(CombatLogEntry entry)
    {
        if (logEntryPrefab == null || logEntriesContainer == null)
        {
            Debug.Log($"[Combat Log] {entry.Message}");
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
                return $"{turnPrefix}{icon} <b>{entry.AttackerName}</b> attacks <b>{entry.DefenderName}</b> for <color=red>{entry.Damage}</color> dmg → {entry.RemainingHealth} HP";
                
            case CombatLogEntry.LogType.Counterattack:
                return $"{turnPrefix}{icon} <b>{entry.DefenderName}</b> counters <b>{entry.AttackerName}</b> for <color=blue>{entry.Damage}</color> dmg → {entry.RemainingHealth} HP";
                
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
}
