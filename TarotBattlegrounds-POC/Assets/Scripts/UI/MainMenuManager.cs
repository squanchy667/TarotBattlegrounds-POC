using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    [Header("Game Settings")]
    [SerializeField] private int playerCount = 2;

    private void Start()
    {
        Debug.Log("MainMenuManager initialized");
        
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        else
            Debug.LogError("Play button not assigned!");
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        PlayerPrefs.SetInt("PlayerCount", playerCount);
        PlayerPrefs.Save();
        
        Debug.Log($"Starting game with {playerCount} players...");
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
    }
}