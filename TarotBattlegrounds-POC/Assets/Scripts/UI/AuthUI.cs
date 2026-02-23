using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T003: Login/Register/Guest UI panel for MainMenu.
    /// Manages authentication flow before entering multiplayer.
    /// </summary>
    public class AuthUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject authPanel;

        [Header("Login Tab")]
        [SerializeField] private GameObject loginTab;
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button switchToRegisterButton;

        [Header("Register Tab")]
        [SerializeField] private GameObject registerTab;
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerNameInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button switchToLoginButton;

        [Header("Guest")]
        [SerializeField] private Button guestButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;

        public event System.Action OnAuthenticated;

        private void Start()
        {
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
            if (guestButton != null) guestButton.onClick.AddListener(OnGuestClicked);
            if (switchToRegisterButton != null) switchToRegisterButton.onClick.AddListener(ShowRegisterTab);
            if (switchToLoginButton != null) switchToLoginButton.onClick.AddListener(ShowLoginTab);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            if (authPanel != null) authPanel.SetActive(false);
        }

        public void Open()
        {
            // If already authenticated, skip
            if (GameAuthManager.Instance != null && GameAuthManager.Instance.IsAuthenticated)
            {
                OnAuthenticated?.Invoke();
                return;
            }

            if (authPanel != null) authPanel.SetActive(true);
            ShowLoginTab();
            SetStatus("");
        }

        public void Close()
        {
            if (authPanel != null) authPanel.SetActive(false);
        }

        private void ShowLoginTab()
        {
            if (loginTab != null) loginTab.SetActive(true);
            if (registerTab != null) registerTab.SetActive(false);
        }

        private void ShowRegisterTab()
        {
            if (loginTab != null) loginTab.SetActive(false);
            if (registerTab != null) registerTab.SetActive(true);
        }

        private void OnLoginClicked()
        {
            string email = loginEmailInput?.text ?? "";
            string password = loginPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetStatus("Email and password required");
                return;
            }

            SetStatus("Logging in...");
            SetButtonsInteractable(false);

            EnsureAuthManager();
            GameAuthManager.Instance.Login(email, password, (success, error) =>
            {
                SetButtonsInteractable(true);
                if (success)
                {
                    SetStatus("Login successful!");
                    Close();
                    OnAuthenticated?.Invoke();
                }
                else
                {
                    SetStatus($"Login failed: {error}");
                }
            });
        }

        private void OnRegisterClicked()
        {
            string email = registerEmailInput?.text ?? "";
            string password = registerPasswordInput?.text ?? "";
            string displayName = registerNameInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(displayName))
            {
                SetStatus("All fields required");
                return;
            }

            SetStatus("Creating account...");
            SetButtonsInteractable(false);

            EnsureAuthManager();
            GameAuthManager.Instance.Register(email, password, displayName, (success, error) =>
            {
                SetButtonsInteractable(true);
                if (success)
                {
                    SetStatus("Account created!");
                    Close();
                    OnAuthenticated?.Invoke();
                }
                else
                {
                    SetStatus($"Registration failed: {error}");
                }
            });
        }

        private void OnGuestClicked()
        {
            SetStatus("Joining as guest...");
            SetButtonsInteractable(false);

            EnsureAuthManager();
            GameAuthManager.Instance.LoginAsGuest((success, error) =>
            {
                SetButtonsInteractable(true);
                if (success)
                {
                    SetStatus("Welcome, guest!");
                    Close();
                    OnAuthenticated?.Invoke();
                }
                else
                {
                    SetStatus($"Guest login failed: {error}");
                }
            });
        }

        private void EnsureAuthManager()
        {
            if (GameAuthManager.Instance == null)
            {
                var go = new GameObject("GameAuthManager");
                go.AddComponent<GameAuthManager>();
            }
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (loginButton != null) loginButton.interactable = interactable;
            if (registerButton != null) registerButton.interactable = interactable;
            if (guestButton != null) guestButton.interactable = interactable;
        }

        public bool IsOpen => authPanel != null && authPanel.activeSelf;

        private void OnDestroy()
        {
            if (loginButton != null) loginButton.onClick.RemoveAllListeners();
            if (registerButton != null) registerButton.onClick.RemoveAllListeners();
            if (guestButton != null) guestButton.onClick.RemoveAllListeners();
            if (switchToRegisterButton != null) switchToRegisterButton.onClick.RemoveAllListeners();
            if (switchToLoginButton != null) switchToLoginButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}
