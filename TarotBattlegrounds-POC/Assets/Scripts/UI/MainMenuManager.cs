using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Game Settings UI (Optional)")]
    [SerializeField] private TMP_Dropdown playerCountDropdown;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    [Header("Default Settings (used if UI not present)")]
    [SerializeField] private int defaultPlayerCount = 2;
    [SerializeField] private GameConfig.GameMode defaultGameMode = GameConfig.GameMode.HumanVsAI;
    [SerializeField] private AIDifficulty defaultDifficulty = AIDifficulty.Medium;

    private void Start()
    {
        Debug.Log("MainMenuManager initialized");

        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        else
            Debug.LogError("Play button not assigned!");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Setup dropdowns if present
        SetupDropdowns();

        // Load saved settings
        LoadSettings();
    }

    private void SetupDropdowns()
    {
        // Player count dropdown (2-4 players)
        if (playerCountDropdown != null)
        {
            playerCountDropdown.ClearOptions();
            playerCountDropdown.AddOptions(new System.Collections.Generic.List<string> { "2 Players", "3 Players", "4 Players" });
            playerCountDropdown.onValueChanged.AddListener(OnPlayerCountChanged);
        }

        // Game mode dropdown
        if (gameModeDropdown != null)
        {
            gameModeDropdown.ClearOptions();
            gameModeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Human vs AI", "AI vs AI (Spectate)", "Multiplayer (Online)" });
            gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
        }

        // Difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Medium", "Hard" });
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }
    }

    private void LoadSettings()
    {
        // Apply saved/default settings to UI
        if (playerCountDropdown != null)
            playerCountDropdown.value = GameConfig.PlayerCount - 2; // 2 players = index 0

        if (gameModeDropdown != null)
            gameModeDropdown.value = (int)GameConfig.CurrentGameMode;

        if (difficultyDropdown != null)
            difficultyDropdown.value = (int)GameConfig.DefaultAIDifficulty;
    }

    private void OnPlayerCountChanged(int index)
    {
        GameConfig.PlayerCount = index + 2; // index 0 = 2 players
        Debug.Log($"[MainMenu] Player count set to: {GameConfig.PlayerCount}");
    }

    private void OnGameModeChanged(int index)
    {
        GameConfig.CurrentGameMode = (GameConfig.GameMode)index;
        Debug.Log($"[MainMenu] Game mode set to: {GameConfig.CurrentGameMode}");
    }

    private void OnDifficultyChanged(int index)
    {
        GameConfig.DefaultAIDifficulty = (AIDifficulty)index;
        Debug.Log($"[MainMenu] AI difficulty set to: {GameConfig.DefaultAIDifficulty}");
    }

    private void OnPlayClicked()
    {
        // Apply default settings if dropdowns not present
        if (playerCountDropdown == null)
            GameConfig.PlayerCount = defaultPlayerCount;
        if (gameModeDropdown == null)
            GameConfig.CurrentGameMode = defaultGameMode;
        if (difficultyDropdown == null)
            GameConfig.DefaultAIDifficulty = defaultDifficulty;

        // If Multiplayer mode, load Lobby scene instead of Game scene
        if (GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer)
        {
            GameConfig.Save();
            Debug.Log("[MainMenu] Loading Lobby for multiplayer...");
            SceneManager.LoadScene("Lobby");
            return;
        }

        // Human is always player 1 (index 0) in HumanVsAI mode
        GameConfig.HumanPlayerIndex = 0;

        // Save and start
        GameConfig.Save();
        GameConfig.LogConfig();

        Debug.Log($"Starting game: {GameConfig.PlayerCount} players, {GameConfig.CurrentGameMode}, AI: {GameConfig.DefaultAIDifficulty}");
        SceneManager.LoadScene("Game");
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quitting...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
        if (playerCountDropdown != null)
            playerCountDropdown.onValueChanged.RemoveListener(OnPlayerCountChanged);
        if (gameModeDropdown != null)
            gameModeDropdown.onValueChanged.RemoveListener(OnGameModeChanged);
        if (difficultyDropdown != null)
            difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
    }
}
