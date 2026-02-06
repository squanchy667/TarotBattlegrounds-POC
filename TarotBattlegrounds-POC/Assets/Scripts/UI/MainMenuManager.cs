using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button quitButton;

    [Header("Solo Panel")]
    [SerializeField] private GameObject soloPanel;
    [SerializeField] private Button players4Button;
    [SerializeField] private Button players6Button;
    [SerializeField] private Button players8Button;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    private static readonly int[] PlayerOptions = { 4, 6, 8 };
    private int selectedPlayerCount = 4;
    private Button[] playerCountButtons;

    private void Start()
    {
        Debug.Log("MainMenuManager initialized");

        playerCountButtons = new Button[] { players4Button, players6Button, players8Button };

        // Main panel buttons
        if (soloButton != null) soloButton.onClick.AddListener(OnSoloClicked);
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Solo panel buttons
        if (players4Button != null) players4Button.onClick.AddListener(() => SelectPlayerCount(0));
        if (players6Button != null) players6Button.onClick.AddListener(() => SelectPlayerCount(1));
        if (players8Button != null) players8Button.onClick.AddListener(() => SelectPlayerCount(2));
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        // Setup difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "Easy", "Medium", "Hard" });
            difficultyDropdown.value = (int)GameConfig.DefaultAIDifficulty;
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }

        // Show main panel by default
        ShowMainPanel();

        // Default selection
        SelectPlayerCount(0);
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (soloPanel != null) soloPanel.SetActive(false);
    }

    private void ShowSoloPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (soloPanel != null) soloPanel.SetActive(true);
    }

    private void SelectPlayerCount(int index)
    {
        selectedPlayerCount = PlayerOptions[index];

        // Update button visuals — highlight the selected one
        for (int i = 0; i < playerCountButtons.Length; i++)
        {
            if (playerCountButtons[i] == null) continue;
            var colors = playerCountButtons[i].colors;
            colors.normalColor = (i == index) ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
            playerCountButtons[i].colors = colors;
        }

        Debug.Log($"[MainMenu] Selected {selectedPlayerCount} players");
    }

    private void OnSoloClicked()
    {
        ShowSoloPanel();
    }

    private void OnMultiplayerClicked()
    {
        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        GameConfig.Save();
        Debug.Log("[MainMenu] Loading Lobby for multiplayer...");
        SceneManager.LoadScene("Lobby");
    }

    private void OnPlayClicked()
    {
        GameConfig.PlayerCount = selectedPlayerCount;
        GameConfig.CurrentGameMode = GameConfig.GameMode.HumanVsAI;
        GameConfig.HumanPlayerIndex = 0;

        if (difficultyDropdown != null)
            GameConfig.DefaultAIDifficulty = (AIDifficulty)difficultyDropdown.value;

        GameConfig.Save();
        GameConfig.LogConfig();

        Debug.Log($"Starting solo game: {GameConfig.PlayerCount} players, AI: {GameConfig.DefaultAIDifficulty}");
        SceneManager.LoadScene("Game");
    }

    private void OnBackClicked()
    {
        ShowMainPanel();
    }

    private void OnDifficultyChanged(int index)
    {
        GameConfig.DefaultAIDifficulty = (AIDifficulty)index;
        Debug.Log($"[MainMenu] AI difficulty set to: {GameConfig.DefaultAIDifficulty}");
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
        if (soloButton != null) soloButton.onClick.RemoveListener(OnSoloClicked);
        if (multiplayerButton != null) multiplayerButton.onClick.RemoveListener(OnMultiplayerClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        if (players4Button != null) players4Button.onClick.RemoveAllListeners();
        if (players6Button != null) players6Button.onClick.RemoveAllListeners();
        if (players8Button != null) players8Button.onClick.RemoveAllListeners();
        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
    }
}
