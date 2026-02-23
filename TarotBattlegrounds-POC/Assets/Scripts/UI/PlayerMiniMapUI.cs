using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T608: Mini-map showing all player health/status in 8-player games.
    /// Displays a grid of player cards with health bars and elimination status.
    /// </summary>
    public class PlayerMiniMapUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject miniMapPanel;
        [SerializeField] private Transform playerCardContainer;
        [SerializeField] private GameObject playerCardPrefab;

        [Header("Layout")]
        [SerializeField] private int maxColumns = 4;

        private List<PlayerCardEntry> entries = new List<PlayerCardEntry>();

        private class PlayerCardEntry
        {
            public GameObject gameObject;
            public TMP_Text nameText;
            public TMP_Text healthText;
            public TMP_Text tierText;
            public Slider healthBar;
            public Image background;
            public int playerIndex;
        }

        private void Start()
        {
            if (miniMapPanel != null) miniMapPanel.SetActive(false);
        }

        /// <summary>Initialize the mini-map for a given number of players.</summary>
        public void Initialize(int playerCount)
        {
            Clear();
            if (playerCardPrefab == null || playerCardContainer == null) return;

            for (int i = 0; i < playerCount; i++)
            {
                var go = Instantiate(playerCardPrefab, playerCardContainer);
                var entry = new PlayerCardEntry
                {
                    gameObject = go,
                    playerIndex = i,
                    nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>(),
                    healthText = go.transform.Find("HealthText")?.GetComponent<TMP_Text>(),
                    tierText = go.transform.Find("TierText")?.GetComponent<TMP_Text>(),
                    healthBar = go.GetComponentInChildren<Slider>(),
                    background = go.GetComponent<Image>(),
                };
                entries.Add(entry);
            }

            if (miniMapPanel != null) miniMapPanel.SetActive(true);
        }

        /// <summary>Update a player's display.</summary>
        public void UpdatePlayer(int playerIndex, string name, int health, int maxHealth, int tavernTier, bool isEliminated, bool isLocal)
        {
            if (playerIndex < 0 || playerIndex >= entries.Count) return;
            var entry = entries[playerIndex];

            if (entry.nameText != null)
            {
                entry.nameText.text = isLocal ? $"{name} (You)" : name;
                entry.nameText.fontStyle = isLocal ? FontStyles.Bold : FontStyles.Normal;
            }

            if (entry.healthText != null)
                entry.healthText.text = isEliminated ? "ELIMINATED" : $"{health}/{maxHealth}";

            if (entry.tierText != null)
                entry.tierText.text = $"T{tavernTier}";

            if (entry.healthBar != null)
            {
                entry.healthBar.maxValue = maxHealth;
                entry.healthBar.value = health;
            }

            if (entry.background != null)
            {
                if (isEliminated)
                    entry.background.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                else if (isLocal)
                    entry.background.color = new Color(0.2f, 0.4f, 0.8f, 0.3f);
                else
                    entry.background.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            }
        }

        /// <summary>Refresh all players from GameManager state.</summary>
        public void RefreshFromGameState()
        {
            if (GameManager.Instance == null) return;

            int localIndex = GameConfig.HumanPlayerIndex;
#if PHOTON_UNITY_NETWORKING
            if (GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer &&
                NetworkGameBridge.Instance != null)
                localIndex = NetworkGameBridge.Instance.LocalPlayerSlot;
#endif

            int startingHealth = EightPlayerManager.Instance != null
                ? EightPlayerManager.Instance.GetStartingHealth(GameManager.Instance.players.Count)
                : 40;

            for (int i = 0; i < GameManager.Instance.players.Count && i < entries.Count; i++)
            {
                var player = GameManager.Instance.players[i];
                string name = $"Player {i + 1}";

#if PHOTON_UNITY_NETWORKING
                if (GameConfig.CurrentGameMode == GameConfig.GameMode.Multiplayer &&
                    NetworkGameBridge.Instance != null &&
                    NetworkGameBridge.Instance.IsNetworkPlayerSlot(i))
                {
                    int actorNum = NetworkGameBridge.Instance.SlotToActor[i];
                    foreach (var pp in Photon.Pun.PhotonNetwork.PlayerList)
                    {
                        if (pp.ActorNumber == actorNum)
                        {
                            name = PhotonConnector.GetPlayerDisplayName(pp);
                            break;
                        }
                    }
                }
#endif

                UpdatePlayer(i, name, player.health, startingHealth,
                    player.currentTavernTier, !player.isAlive, i == localIndex);
            }
        }

        private void Clear()
        {
            foreach (var entry in entries)
                if (entry.gameObject != null) Destroy(entry.gameObject);
            entries.Clear();
        }

        public bool IsOpen => miniMapPanel != null && miniMapPanel.activeSelf;

        private void OnDestroy() => Clear();
    }
}
