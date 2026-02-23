using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T005: Matchmaking queue UI. Shows "Finding match...", queue stats, cancel button.
    /// </summary>
    public class MatchmakingUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject matchmakingPanel;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text waitTimeText;
        [SerializeField] private TMP_Text queueCountText;

        [Header("Buttons")]
        [SerializeField] private Button cancelButton;

        [Header("Animation")]
        [SerializeField] private GameObject loadingSpinner;

        private void Start()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
            if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.OnStateChanged -= OnStateChanged;
        }

        public void Open()
        {
            if (matchmakingPanel != null) matchmakingPanel.SetActive(true);

            EnsureMatchmakingManager();
            MatchmakingManager.Instance.JoinQueue();
        }

        public void Close()
        {
            if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
        }

        private void Update()
        {
            if (matchmakingPanel == null || !matchmakingPanel.activeSelf) return;
            if (MatchmakingManager.Instance == null) return;

            if (MatchmakingManager.Instance.State == MatchmakingManager.QueueState.Waiting)
            {
                int wait = MatchmakingManager.Instance.WaitTimeSeconds;
                if (waitTimeText != null)
                    waitTimeText.text = $"Wait time: {wait / 60}:{wait % 60:D2}";

                if (queueCountText != null)
                    queueCountText.text = $"Players in queue: {MatchmakingManager.Instance.PlayersInQueue}";
            }
        }

        private void OnStateChanged(MatchmakingManager.QueueState state)
        {
            switch (state)
            {
                case MatchmakingManager.QueueState.Joining:
                    SetStatus("Joining queue...");
                    SetSpinner(true);
                    break;

                case MatchmakingManager.QueueState.Waiting:
                    SetStatus("Finding match...");
                    SetSpinner(true);
                    break;

                case MatchmakingManager.QueueState.Matched:
                    SetStatus("Match found! Loading...");
                    SetSpinner(false);
                    if (cancelButton != null) cancelButton.interactable = false;
                    break;

                case MatchmakingManager.QueueState.Error:
                    SetStatus($"Error: {MatchmakingManager.Instance.ErrorMessage}");
                    SetSpinner(false);
                    break;

                case MatchmakingManager.QueueState.Idle:
                    Close();
                    break;
            }
        }

        private void OnCancelClicked()
        {
            MatchmakingManager.Instance?.CancelQueue();
            Close();
        }

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void SetSpinner(bool active)
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(active);
        }

        private void EnsureMatchmakingManager()
        {
            if (MatchmakingManager.Instance == null)
            {
                var go = new GameObject("MatchmakingManager");
                go.AddComponent<MatchmakingManager>();
            }
        }

        public bool IsOpen => matchmakingPanel != null && matchmakingPanel.activeSelf;

        private void OnDestroy()
        {
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        }
    }
}
