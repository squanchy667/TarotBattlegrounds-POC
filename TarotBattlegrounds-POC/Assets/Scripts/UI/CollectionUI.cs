using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T417: Card collection viewer. Browse all cards with tribe/tier filters.
    /// Shows all cards in the CardDatabase with sorting and filtering options.
    /// </summary>
    public class CollectionUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject collectionPanel;

        [Header("Card Grid")]
        [SerializeField] private Transform cardGridContainer;
        [SerializeField] private GameObject cardDisplayPrefab;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown tribeFilter;
        [SerializeField] private TMP_Dropdown tierFilter;
        [SerializeField] private TMP_InputField searchField;

        [Header("Info")]
        [SerializeField] private TMP_Text cardCountText;
        [SerializeField] private TMP_Text selectedCardInfo;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        private List<Card> allCards;
        private List<Card> filteredCards;
        private List<GameObject> displayedCards = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (tribeFilter != null) tribeFilter.onValueChanged.AddListener(_ => ApplyFilters());
            if (tierFilter != null) tierFilter.onValueChanged.AddListener(_ => ApplyFilters());
            if (searchField != null) searchField.onValueChanged.AddListener(_ => ApplyFilters());

            SetupFilterDropdowns();
        }

        public void Open()
        {
            if (collectionPanel != null) collectionPanel.SetActive(true);

            allCards = CardDatabase.GenerateAllCards();
            filteredCards = new List<Card>(allCards);
            RefreshDisplay();
        }

        public void Close()
        {
            if (collectionPanel != null) collectionPanel.SetActive(false);
            ClearDisplay();
        }

        private void SetupFilterDropdowns()
        {
            if (tribeFilter != null)
            {
                tribeFilter.ClearOptions();
                var options = new List<string> { "All Tribes" };
                foreach (TribeType tribe in System.Enum.GetValues(typeof(TribeType)))
                {
                    if (tribe != TribeType.None) options.Add(tribe.ToString());
                }
                tribeFilter.AddOptions(options);
            }

            if (tierFilter != null)
            {
                tierFilter.ClearOptions();
                var options = new List<string> { "All Tiers" };
                for (int i = 1; i <= 6; i++) options.Add($"Tier {i}");
                tierFilter.AddOptions(options);
            }
        }

        private void ApplyFilters()
        {
            if (allCards == null) return;

            filteredCards = new List<Card>(allCards);

            // Tribe filter
            if (tribeFilter != null && tribeFilter.value > 0)
            {
                TribeType selectedTribe = (TribeType)System.Enum.GetValues(typeof(TribeType)).GetValue(tribeFilter.value);
                filteredCards = filteredCards.Where(c => c.HasTribe(selectedTribe)).ToList();
            }

            // Tier filter
            if (tierFilter != null && tierFilter.value > 0)
            {
                int tier = tierFilter.value;
                filteredCards = filteredCards.Where(c => c.tier == tier).ToList();
            }

            // Search filter
            if (searchField != null && !string.IsNullOrEmpty(searchField.text))
            {
                string search = searchField.text.ToLower();
                filteredCards = filteredCards.Where(c =>
                    c.cardName.ToLower().Contains(search) ||
                    (c.ability != null && c.ability.ToLower().Contains(search))
                ).ToList();
            }

            // Sort by tier, then name
            filteredCards = filteredCards.OrderBy(c => c.tier).ThenBy(c => c.cardName).ToList();

            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            ClearDisplay();

            if (cardCountText != null)
                cardCountText.text = $"{filteredCards.Count} / {(allCards != null ? allCards.Count : 0)} cards";

            foreach (var card in filteredCards)
            {
                CreateCardDisplay(card);
            }
        }

        private void CreateCardDisplay(Card card)
        {
            if (cardDisplayPrefab == null || cardGridContainer == null) return;

            GameObject cardObj = Instantiate(cardDisplayPrefab, cardGridContainer);
            displayedCards.Add(cardObj);

            var cardUI = cardObj.GetComponent<CardDisplayUI>();
            if (cardUI != null)
            {
                cardUI.Setup(card, -1, (idx) => OnCardSelected(card));
            }

            // Add hover for tooltip
            var hover = cardObj.GetComponent<CardHoverHandler>();
            if (hover == null) hover = cardObj.AddComponent<CardHoverHandler>();
            hover.SetCard(card);

            // Add frame generator
            var frameGen = cardObj.GetComponent<CardFrameGenerator>();
            if (frameGen == null) frameGen = cardObj.AddComponent<CardFrameGenerator>();
            frameGen.ApplyCardVisuals(card);
        }

        private void OnCardSelected(Card card)
        {
            if (selectedCardInfo == null) return;

            string tribes = "";
            if (card.tribes != null)
            {
                foreach (var t in card.tribes)
                    if (t != TribeType.None) tribes += (tribes.Length > 0 ? "/" : "") + t;
            }

            selectedCardInfo.text = $"<b>{card.cardName}</b>\n" +
                $"Tier {card.tier} | {card.attack}/{card.health} | {tribes}\n" +
                $"{card.ability}";
        }

        private void ClearDisplay()
        {
            foreach (var obj in displayedCards)
                if (obj != null) Destroy(obj);
            displayedCards.Clear();
        }

        public bool IsOpen => collectionPanel != null && collectionPanel.activeSelf;

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}
