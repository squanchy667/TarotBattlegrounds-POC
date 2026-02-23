using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T414: Read-only opponent board viewer.
    /// Allows the local player to peek at opponents' boards during recruit phase.
    /// Cycles through opponents with prev/next buttons.
    /// </summary>
    public class OpponentBoardViewer : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject viewerPanel;
        [SerializeField] private CanvasGroup panelGroup;

        [Header("Opponent Info")]
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text opponentHealthText;
        [SerializeField] private TMP_Text opponentTierText;
        [SerializeField] private TMP_Text boardCountText;

        [Header("Board Display")]
        [SerializeField] private Transform boardContainer;
        [SerializeField] private GameObject cardDisplayPrefab;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text pageText;

        private List<Player> opponents = new List<Player>();
        private int currentOpponentIndex;
        private List<GameObject> displayedCards = new List<GameObject>();

        private void Awake()
        {
            if (viewerPanel != null) viewerPanel.SetActive(false);
        }

        private void Start()
        {
            if (prevButton != null) prevButton.onClick.AddListener(() => Navigate(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Navigate(1));
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        /// <summary>
        /// Open the opponent viewer. Finds all opponents relative to the local player.
        /// </summary>
        public void Open()
        {
            if (GameManager.Instance == null) return;

            opponents.Clear();
            int localIndex = GameUIManager.Instance != null ? GameUIManager.Instance.GetActivePlayerIndex() : 0;

            for (int i = 0; i < GameManager.Instance.players.Count; i++)
            {
                if (i != localIndex && GameManager.Instance.players[i].Health > 0)
                    opponents.Add(GameManager.Instance.players[i]);
            }

            if (opponents.Count == 0) return;

            currentOpponentIndex = 0;
            if (viewerPanel != null) viewerPanel.SetActive(true);
            ShowCurrentOpponent();
        }

        public void Close()
        {
            if (viewerPanel != null) viewerPanel.SetActive(false);
            ClearDisplay();
        }

        private void Navigate(int direction)
        {
            if (opponents.Count == 0) return;
            currentOpponentIndex = (currentOpponentIndex + direction + opponents.Count) % opponents.Count;
            ShowCurrentOpponent();
        }

        private void ShowCurrentOpponent()
        {
            if (currentOpponentIndex >= opponents.Count) return;
            Player opponent = opponents[currentOpponentIndex];

            // Update info
            if (opponentNameText != null) opponentNameText.text = $"Player {opponent.playerId}";
            if (opponentHealthText != null) opponentHealthText.text = $"HP: {opponent.Health}";
            if (opponentTierText != null) opponentTierText.text = $"Tier {opponent.currentTavernTier}";
            if (boardCountText != null) boardCountText.text = $"Board: {opponent.board.Count}/7";
            if (pageText != null) pageText.text = $"{currentOpponentIndex + 1}/{opponents.Count}";

            // Navigation buttons
            if (prevButton != null) prevButton.interactable = opponents.Count > 1;
            if (nextButton != null) nextButton.interactable = opponents.Count > 1;

            // Display board
            ClearDisplay();
            foreach (var card in opponent.board)
            {
                CreateCardDisplay(card);
            }
        }

        private void CreateCardDisplay(Card card)
        {
            if (cardDisplayPrefab == null || boardContainer == null) return;

            GameObject cardObj = Instantiate(cardDisplayPrefab, boardContainer);
            displayedCards.Add(cardObj);

            // Try to set up as read-only (no click callback)
            var cardUI = cardObj.GetComponent<CardDisplayUI>();
            if (cardUI != null)
            {
                cardUI.Setup(card, -1, null); // null callback = read-only
            }

            // Add hover handler for tooltip
            var hover = cardObj.GetComponent<CardHoverHandler>();
            if (hover == null) hover = cardObj.AddComponent<CardHoverHandler>();
            hover.SetCard(card);
        }

        private void ClearDisplay()
        {
            foreach (var obj in displayedCards)
            {
                if (obj != null) Destroy(obj);
            }
            displayedCards.Clear();
        }

        public bool IsOpen => viewerPanel != null && viewerPanel.activeSelf;

        private void OnDestroy()
        {
            if (prevButton != null) prevButton.onClick.RemoveAllListeners();
            if (nextButton != null) nextButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}
