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

    [Header("T416: Additional Menu Buttons")]
    [SerializeField] private Button collectionButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private TarotBattlegrounds.UI.CollectionUI collectionUI;
    [SerializeField] private TarotBattlegrounds.UI.SettingsUI settingsUI;

    [Header("T003/T005: Auth & Matchmaking")]
    [SerializeField] private TarotBattlegrounds.UI.AuthUI authUI;
    [SerializeField] private TarotBattlegrounds.UI.MatchmakingUI matchmakingUI;
    [SerializeField] private Button rankedButton;
    [SerializeField] private TMP_Text playerInfoText;

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
        if (collectionButton != null) collectionButton.onClick.AddListener(OnCollectionClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (rankedButton != null) rankedButton.onClick.AddListener(OnRankedClicked);

        // Auth UI callback
        if (authUI != null)
            authUI.OnAuthenticated += OnPlayerAuthenticated;

        // Update player info display
        UpdatePlayerInfo();

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
        // Require auth before entering multiplayer
        if (GameAuthManager.Instance == null || !GameAuthManager.Instance.IsAuthenticated)
        {
            pendingMultiplayerAction = PendingAction.Lobby;
            if (authUI != null) authUI.Open();
            return;
        }

        GameConfig.CurrentGameMode = GameConfig.GameMode.Multiplayer;
        GameConfig.Save();
        Debug.Log("[MainMenu] Loading Lobby for multiplayer...");
        SceneManager.LoadScene("Lobby");
    }

    private void OnRankedClicked()
    {
        // Require auth before ranked matchmaking
        if (GameAuthManager.Instance == null || !GameAuthManager.Instance.IsAuthenticated)
        {
            pendingMultiplayerAction = PendingAction.Ranked;
            if (authUI != null) authUI.Open();
            return;
        }

        if (matchmakingUI != null) matchmakingUI.Open();
    }

    private enum PendingAction { None, Lobby, Ranked }
    private PendingAction pendingMultiplayerAction = PendingAction.None;

    private void OnPlayerAuthenticated()
    {
        UpdatePlayerInfo();
        var action = pendingMultiplayerAction;
        pendingMultiplayerAction = PendingAction.None;

        switch (action)
        {
            case PendingAction.Lobby:
                OnMultiplayerClicked();
                break;
            case PendingAction.Ranked:
                OnRankedClicked();
                break;
        }
    }

    private void UpdatePlayerInfo()
    {
        if (playerInfoText == null) return;
        if (GameAuthManager.Instance != null && GameAuthManager.Instance.IsAuthenticated)
        {
            playerInfoText.text = $"{GameAuthManager.Instance.DisplayName} (Rating: {GameAuthManager.Instance.Rating})";
        }
        else
        {
            playerInfoText.text = "";
        }
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

    private void OnCollectionClicked()
    {
        if (collectionUI != null) collectionUI.Open();
    }

    private void OnSettingsClicked()
    {
        if (settingsUI != null) settingsUI.Open();
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
        if (collectionButton != null) collectionButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
        if (rankedButton != null) rankedButton.onClick.RemoveAllListeners();
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        if (players4Button != null) players4Button.onClick.RemoveAllListeners();
        if (players6Button != null) players6Button.onClick.RemoveAllListeners();
        if (players8Button != null) players8Button.onClick.RemoveAllListeners();
        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
        if (authUI != null) authUI.OnAuthenticated -= OnPlayerAuthenticated;
    }
}
