using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T417/UX20: Card collection viewer. Browse all cards with tribe/tier filters.
    /// Shows all cards in the CardDatabase with sorting and filtering options.
    /// UX20 enhancements: tribe filter tabs, tier star buttons, card pop-in animation,
    /// detail panel, and enhanced search bar.
    /// </summary>
    public class CollectionUI : MonoBehaviour, IThemeable
    {
        [Header("Panel")]
        [SerializeField] private GameObject collectionPanel;

        [Header("Card Grid")]
        [SerializeField] private Transform cardGridContainer;
        [SerializeField] private GameObject cardDisplayPrefab;

        [Header("Filters (Legacy Dropdowns)")]
        [SerializeField] private TMP_Dropdown tribeFilter;
        [SerializeField] private TMP_Dropdown tierFilter;
        [SerializeField] private TMP_InputField searchField;

        [Header("UX20: Tribe Filter Tabs")]
        [SerializeField] private Transform tribeFilterContainer;

        [Header("UX20: Tier Filter Buttons")]
        [SerializeField] private Transform tierFilterContainer;

        [Header("UX20: Search Bar")]
        [SerializeField] private TMP_InputField searchInput;

        [Header("UX20: Card Grid Animation")]
        [SerializeField] private GridLayoutGroup cardGrid;
        [SerializeField] private float cardPopDelay = 0.03f;

        [Header("UX20: Detail Panel")]
        [SerializeField] private RectTransform detailPanel;
        [SerializeField] private CardDisplayUI detailCard;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailAbilities;
        [SerializeField] private TMP_Text detailLore;

        [Header("Info")]
        [SerializeField] private TMP_Text cardCountText;
        [SerializeField] private TMP_Text selectedCardInfo;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        private List<Card> allCards;
        private List<Card> filteredCards;
        private List<GameObject> displayedCards = new List<GameObject>();

        // UX20: Filter state
        private TribeType selectedTribeFilter = TribeType.None; // None = "All"
        private int selectedTierFilter = 0; // 0 = "All"
        private List<Button> tribeButtons = new List<Button>();
        private List<Button> tierButtons = new List<Button>();
        private Coroutine popInCoroutine;

        // UX20: Tribe colors for filter buttons
        private static readonly Dictionary<TribeType, Color> TribeColors = new Dictionary<TribeType, Color>
        {
            { TribeType.None,      new Color(0.5f, 0.5f, 0.5f) },   // "All" - gray
            { TribeType.Pentacles, new Color(0.82f, 0.68f, 0.15f) }, // Gold
            { TribeType.Cups,      new Color(0.25f, 0.45f, 0.95f) }, // Blue
            { TribeType.Swords,    new Color(0.78f, 0.78f, 0.90f) }, // Silver
            { TribeType.Wands,     new Color(0.92f, 0.45f, 0.12f) }, // Orange
            { TribeType.Stars,     new Color(0.55f, 0.75f, 1.0f) },  // Light blue
            { TribeType.Coins,     new Color(0.90f, 0.80f, 0.20f) }, // Yellow gold
        };

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            // Legacy dropdown listeners
            if (tribeFilter != null) tribeFilter.onValueChanged.AddListener(_ => ApplyFilters());
            if (tierFilter != null) tierFilter.onValueChanged.AddListener(_ => ApplyFilters());
            if (searchField != null) searchField.onValueChanged.AddListener(_ => ApplyFilters());

            // UX20: Search input listener (new field)
            if (searchInput != null) searchInput.onValueChanged.AddListener(_ => ApplyFilters());

            SetupFilterDropdowns();

            // UX20: Build tribe and tier filter button rows
            BuildTribeFilterButtons();
            BuildTierFilterButtons();

            // UX20: Hide detail panel initially
            if (detailPanel != null)
                detailPanel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ThemeManager.OnThemeChanged += ApplyTheme;
            if (ThemeManager.ActiveTheme != null)
                ApplyTheme(ThemeManager.ActiveTheme);
        }

        private void OnDisable()
        {
            ThemeManager.OnThemeChanged -= ApplyTheme;
        }

        /// <summary>
        /// IThemeable: Apply theme colors to tribe filter buttons from ThemeManager colors.
        /// </summary>
        public void ApplyTheme(ThemeConfig theme)
        {
            if (theme == null) return;

            // Update tribe button colors from theme
            for (int i = 0; i < tribeButtons.Count; i++)
            {
                if (tribeButtons[i] == null) continue;

                TribeType tribe;
                if (i == 0)
                    tribe = TribeType.None; // "All" button
                else
                    tribe = (TribeType)i;

                Color tribeColor = (tribe == TribeType.None)
                    ? theme.primaryColor
                    : theme.GetTribeColor(tribe);

                bool isActive = (tribe == selectedTribeFilter);
                ApplyFilterButtonStyle(tribeButtons[i], tribeColor, isActive);
            }

            // Update detail panel colors
            if (detailName != null)
                detailName.color = theme.accentColor;
            if (detailAbilities != null)
                detailAbilities.color = theme.textColorLight;
            if (detailLore != null)
                detailLore.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        public void Open()
        {
            if (collectionPanel != null) collectionPanel.SetActive(true);

            allCards = CardDatabase.GenerateAllCards();
            filteredCards = new List<Card>(allCards);

            // UX20: Reset filters on open
            selectedTribeFilter = TribeType.None;
            selectedTierFilter = 0;
            if (searchInput != null) searchInput.text = "";
            if (searchField != null) searchField.text = "";

            UpdateTribeButtonVisuals();
            UpdateTierButtonVisuals();

            RefreshDisplay();
        }

        public void Close()
        {
            if (collectionPanel != null) collectionPanel.SetActive(false);

            // UX20: Hide detail panel
            if (detailPanel != null)
                detailPanel.gameObject.SetActive(false);

            // UX20: Stop pop-in coroutine
            if (popInCoroutine != null)
            {
                StopCoroutine(popInCoroutine);
                popInCoroutine = null;
            }

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

        // ===================== UX20: TRIBE FILTER BUTTONS =====================

        /// <summary>
        /// UX20: Build tribe filter button row — "All" + 6 tribe buttons.
        /// Each button is colored per tribe and toggles the tribe filter.
        /// </summary>
        private void BuildTribeFilterButtons()
        {
            if (tribeFilterContainer == null) return;

            tribeButtons.Clear();

            // "All" button
            Button allBtn = CreateFilterButton(tribeFilterContainer, "All",
                TribeColors[TribeType.None], () => OnTribeFilterClicked(TribeType.None));
            tribeButtons.Add(allBtn);

            // One button per tribe
            foreach (TribeType tribe in System.Enum.GetValues(typeof(TribeType)))
            {
                if (tribe == TribeType.None) continue;

                string label = tribe.ToString();
                if (ThemeManager.ActiveTheme != null)
                    label = ThemeManager.GetTribeName(tribe);

                Color color = TribeColors.ContainsKey(tribe)
                    ? TribeColors[tribe]
                    : Color.gray;

                TribeType capturedTribe = tribe; // Capture for closure
                Button btn = CreateFilterButton(tribeFilterContainer, label,
                    color, () => OnTribeFilterClicked(capturedTribe));
                tribeButtons.Add(btn);
            }

            UpdateTribeButtonVisuals();
        }

        private void OnTribeFilterClicked(TribeType tribe)
        {
            selectedTribeFilter = tribe;
            UpdateTribeButtonVisuals();
            ApplyFilters();
        }

        private void UpdateTribeButtonVisuals()
        {
            for (int i = 0; i < tribeButtons.Count; i++)
            {
                if (tribeButtons[i] == null) continue;

                TribeType tribe;
                if (i == 0)
                    tribe = TribeType.None;
                else
                    tribe = (TribeType)i;

                Color baseColor = TribeColors.ContainsKey(tribe) ? TribeColors[tribe] : Color.gray;

                // Use theme color if available
                if (ThemeManager.ActiveTheme != null && tribe != TribeType.None)
                    baseColor = ThemeManager.GetTribeColor(tribe);

                bool isActive = (tribe == selectedTribeFilter);
                ApplyFilterButtonStyle(tribeButtons[i], baseColor, isActive);
            }
        }

        // ===================== UX20: TIER FILTER BUTTONS =====================

        /// <summary>
        /// UX20: Build tier filter button row — "All" + tier 1-6 buttons.
        /// </summary>
        private void BuildTierFilterButtons()
        {
            if (tierFilterContainer == null) return;

            tierButtons.Clear();

            // "All" button
            Button allBtn = CreateFilterButton(tierFilterContainer, "All",
                new Color(0.5f, 0.5f, 0.5f), () => OnTierFilterClicked(0));
            tierButtons.Add(allBtn);

            // Tier 1-6 buttons with star indicators
            Color[] tierColors = new Color[]
            {
                new Color(0.6f, 0.6f, 0.6f),  // T1: Gray
                new Color(0.4f, 0.7f, 0.4f),  // T2: Green
                new Color(0.3f, 0.5f, 0.9f),  // T3: Blue
                new Color(0.6f, 0.3f, 0.8f),  // T4: Purple
                new Color(0.9f, 0.6f, 0.1f),  // T5: Orange
                new Color(1.0f, 0.8f, 0.2f),  // T6: Gold
            };

            for (int t = 1; t <= 6; t++)
            {
                int tier = t; // Capture for closure
                string stars = new string('\u2605', tier); // Unicode filled star
                Color color = tierColors[t - 1];
                Button btn = CreateFilterButton(tierFilterContainer, stars,
                    color, () => OnTierFilterClicked(tier));
                tierButtons.Add(btn);
            }

            UpdateTierButtonVisuals();
        }

        private void OnTierFilterClicked(int tier)
        {
            selectedTierFilter = tier;
            UpdateTierButtonVisuals();
            ApplyFilters();
        }

        private void UpdateTierButtonVisuals()
        {
            Color[] tierColors = new Color[]
            {
                new Color(0.5f, 0.5f, 0.5f),  // All
                new Color(0.6f, 0.6f, 0.6f),  // T1
                new Color(0.4f, 0.7f, 0.4f),  // T2
                new Color(0.3f, 0.5f, 0.9f),  // T3
                new Color(0.6f, 0.3f, 0.8f),  // T4
                new Color(0.9f, 0.6f, 0.1f),  // T5
                new Color(1.0f, 0.8f, 0.2f),  // T6
            };

            for (int i = 0; i < tierButtons.Count; i++)
            {
                if (tierButtons[i] == null) continue;
                bool isActive = (i == selectedTierFilter);
                Color baseColor = (i < tierColors.Length) ? tierColors[i] : Color.gray;
                ApplyFilterButtonStyle(tierButtons[i], baseColor, isActive);
            }
        }

        // ===================== SHARED FILTER BUTTON HELPERS =====================

        /// <summary>
        /// UX20: Create a small filter toggle button with label and color.
        /// </summary>
        private Button CreateFilterButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(label + "_FilterBtn");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90f, 36f);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            // Layout element for HorizontalLayoutGroup
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = 70f;
            le.preferredWidth = 90f;
            le.minHeight = 36f;

            // Text child
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            return btn;
        }

        /// <summary>
        /// UX20: Apply active/inactive visual style to a filter button.
        /// Active = full opacity, colored background.
        /// Inactive = 0.4 opacity, gray background.
        /// </summary>
        private void ApplyFilterButtonStyle(Button button, Color activeColor, bool isActive)
        {
            if (button == null) return;

            Image img = button.GetComponent<Image>();
            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg == null) cg = button.gameObject.AddComponent<CanvasGroup>();

            if (isActive)
            {
                if (img != null) img.color = activeColor;
                cg.alpha = 1f;
            }
            else
            {
                if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                cg.alpha = 0.4f;
            }
        }

        // ===================== FILTER LOGIC =====================

        private void ApplyFilters()
        {
            if (allCards == null) return;

            filteredCards = new List<Card>(allCards);

            // UX20: Tribe filter (button-based)
            if (selectedTribeFilter != TribeType.None)
            {
                filteredCards = filteredCards.Where(c => c.HasTribe(selectedTribeFilter)).ToList();
            }
            // Legacy dropdown tribe filter (fallback)
            else if (tribeFilter != null && tribeFilter.value > 0)
            {
                TribeType selectedTribe = (TribeType)System.Enum.GetValues(typeof(TribeType)).GetValue(tribeFilter.value);
                filteredCards = filteredCards.Where(c => c.HasTribe(selectedTribe)).ToList();
            }

            // UX20: Tier filter (button-based)
            if (selectedTierFilter > 0)
            {
                filteredCards = filteredCards.Where(c => c.tier == selectedTierFilter).ToList();
            }
            // Legacy dropdown tier filter (fallback)
            else if (tierFilter != null && tierFilter.value > 0)
            {
                int tier = tierFilter.value;
                filteredCards = filteredCards.Where(c => c.tier == tier).ToList();
            }

            // UX20: Search filter — check both searchInput (new) and searchField (legacy)
            string searchText = "";
            if (searchInput != null && !string.IsNullOrEmpty(searchInput.text))
                searchText = searchInput.text;
            else if (searchField != null && !string.IsNullOrEmpty(searchField.text))
                searchText = searchField.text;

            if (!string.IsNullOrEmpty(searchText))
            {
                string search = searchText.ToLower();
                filteredCards = filteredCards.Where(c =>
                    c.cardName.ToLower().Contains(search) ||
                    (c.ability != null && c.ability.ToLower().Contains(search))
                ).ToList();
            }

            // Sort by tier, then name
            filteredCards = filteredCards.OrderBy(c => c.tier).ThenBy(c => c.cardName).ToList();

            RefreshDisplay();
        }

        // ===================== DISPLAY =====================

        private void RefreshDisplay()
        {
            ClearDisplay();

            if (cardCountText != null)
                cardCountText.text = $"{filteredCards.Count} / {(allCards != null ? allCards.Count : 0)} cards";

            // UX20: Stop any running pop-in coroutine
            if (popInCoroutine != null)
            {
                StopCoroutine(popInCoroutine);
                popInCoroutine = null;
            }

            List<GameObject> newCards = new List<GameObject>();
            foreach (var card in filteredCards)
            {
                GameObject cardObj = CreateCardDisplay(card);
                if (cardObj != null)
                    newCards.Add(cardObj);
            }

            // UX20: Start pop-in animation
            if (newCards.Count > 0)
            {
                popInCoroutine = StartCoroutine(PopInCards(newCards));
            }
        }

        /// <summary>
        /// UX20: Staggered pop-in animation — each card scales from 0 to 1 with delay.
        /// </summary>
        private IEnumerator PopInCards(List<GameObject> cards)
        {
            // Set all cards to scale 0 immediately
            foreach (var cardObj in cards)
            {
                if (cardObj != null)
                    cardObj.transform.localScale = Vector3.zero;
            }

            // Pop in each card with stagger
            foreach (var cardObj in cards)
            {
                if (cardObj == null) continue;

                StartCoroutine(ScalePopIn(cardObj.transform, 0.2f));
                yield return new WaitForSeconds(cardPopDelay);
            }

            popInCoroutine = null;
        }

        /// <summary>
        /// UX20: Smooth scale-up from 0 to 1 with overshoot easing.
        /// </summary>
        private IEnumerator ScalePopIn(Transform target, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot ease-out: slightly bounces past 1.0
                float scale = 1f + 0.1f * Mathf.Sin(t * Mathf.PI) * (1f - t);
                scale *= t; // Combine with linear ramp for 0-to-1
                if (target != null)
                    target.localScale = Vector3.one * Mathf.Clamp(scale, 0f, 1.15f);
                yield return null;
            }

            if (target != null)
                target.localScale = Vector3.one;
        }

        private GameObject CreateCardDisplay(Card card)
        {
            if (cardDisplayPrefab == null || cardGridContainer == null) return null;

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

            // UX20: Add click handler for detail panel
            Button cardButton = cardObj.GetComponent<Button>();
            if (cardButton == null)
                cardButton = cardObj.AddComponent<Button>();

            // Wire click to show detail (use a local capture)
            Card capturedCard = card;
            cardButton.onClick.AddListener(() => ShowDetailPanel(capturedCard));

            return cardObj;
        }

        // ===================== UX20: DETAIL PANEL =====================

        /// <summary>
        /// UX20: Show the detail panel with enlarged card view and full stats.
        /// </summary>
        private void ShowDetailPanel(Card card)
        {
            if (card == null) return;

            // Update legacy selected card info
            OnCardSelected(card);

            // UX20: Show detail panel if available
            if (detailPanel != null)
                detailPanel.gameObject.SetActive(true);

            // UX20: Set up detail card display
            if (detailCard != null)
            {
                detailCard.Setup(card, -1, null);
            }

            // UX20: Detail name
            if (detailName != null)
            {
                string tribeSuffix = "";
                if (card.tribes != null)
                {
                    foreach (var t in card.tribes)
                    {
                        if (t != TribeType.None)
                        {
                            string name = ThemeManager.ActiveTheme != null
                                ? ThemeManager.GetTribeName(t)
                                : t.ToString();
                            tribeSuffix += (tribeSuffix.Length > 0 ? " / " : "") + name;
                        }
                    }
                }

                detailName.text = $"<b>{card.cardName}</b>\n" +
                    $"<size=70%>Tier {card.tier} | {card.attack}/{card.health}" +
                    (tribeSuffix.Length > 0 ? $" | {tribeSuffix}" : "") + "</size>";
            }

            // UX20: Detail abilities — full ability description
            if (detailAbilities != null)
            {
                string abilityText = "";

                // Show trigger + effect
                if (card.abilityTrigger != AbilityTrigger.None)
                {
                    abilityText += $"<b>{FormatTriggerDisplay(card.abilityTrigger)}</b>\n";
                }

                if (card.abilityEffect != Card.AbilityEffectType.None)
                {
                    abilityText += FormatEffectDisplay(card.abilityEffect, card.abilityValue) + "\n";
                }

                // Show keywords
                List<string> keywords = new List<string>();
                if (card.abilityEffect == Card.AbilityEffectType.Taunt) keywords.Add("Guardian");
                if (card.abilityEffect == Card.AbilityEffectType.Reborn) keywords.Add("Reborn");
                if (card.abilityEffect == Card.AbilityEffectType.Windfury) keywords.Add("Windfury");
                if (card.abilityEffect == Card.AbilityEffectType.Venomous) keywords.Add("Venomous");
                if (card.abilityEffect == Card.AbilityEffectType.GainArmor) keywords.Add($"Armor {card.abilityValue}");

                if (keywords.Count > 0)
                    abilityText += "<color=#FFD700>" + string.Join(", ", keywords) + "</color>\n";

                // Legacy effect
                if (card.effectType != Card.EffectType.NoEffect)
                {
                    abilityText += $"<i>{card.effectType}</i>\n";
                }

                detailAbilities.text = abilityText.TrimEnd('\n');
            }

            // UX20: Detail lore — use the ability description string as lore flavor
            if (detailLore != null)
            {
                detailLore.text = !string.IsNullOrEmpty(card.ability)
                    ? $"<i>\"{card.ability}\"</i>"
                    : "";
            }
        }

        /// <summary>
        /// UX20: Format ability trigger for detail display.
        /// </summary>
        private string FormatTriggerDisplay(AbilityTrigger trigger)
        {
            switch (trigger)
            {
                case AbilityTrigger.Battlecry:      return "<color=#FFD700>Battlecry</color>";
                case AbilityTrigger.Deathrattle:     return "<color=#8B4513>Deathrattle</color>";
                case AbilityTrigger.OnAttack:        return "<color=#FF4500>On Attack</color>";
                case AbilityTrigger.OnDamaged:       return "<color=#DC143C>On Damaged</color>";
                case AbilityTrigger.StartOfCombat:   return "<color=#4169E1>Start of Combat</color>";
                case AbilityTrigger.EndOfTurn:       return "<color=#9370DB>End of Turn</color>";
                case AbilityTrigger.OnAllyDeath:     return "<color=#696969>On Ally Death</color>";
                case AbilityTrigger.OnAllySummoned:   return "<color=#32CD32>On Ally Summoned</color>";
                case AbilityTrigger.OnSell:          return "<color=#DAA520>On Sell</color>";
                case AbilityTrigger.Aura:            return "<color=#00CED1>Aura</color>";
                default: return trigger.ToString();
            }
        }

        /// <summary>
        /// UX20: Format ability effect for detail panel (longer descriptions than card face).
        /// </summary>
        private string FormatEffectDisplay(Card.AbilityEffectType effect, int value)
        {
            switch (effect)
            {
                case Card.AbilityEffectType.BuffAdjacentAttack:       return $"Give adjacent minions +{value} Attack";
                case Card.AbilityEffectType.BuffAdjacentHealth:       return $"Give adjacent minions +{value} Health";
                case Card.AbilityEffectType.BuffAdjacentStats:        return $"Give adjacent minions +{value}/+{value}";
                case Card.AbilityEffectType.BuffAllFriendlyAttack:    return $"Give all friendly minions +{value} Attack";
                case Card.AbilityEffectType.BuffOtherFriendlyAttack:  return $"Give all other friendlies +{value} Attack";
                case Card.AbilityEffectType.GainAegis:                return "Gain Aegis (block one attack)";
                case Card.AbilityEffectType.GainCoins:                return $"Gain {value} gold";
                case Card.AbilityEffectType.DeathrattleBuffRandomFriendly: return $"Give a random friendly +{value}/+{value}";
                case Card.AbilityEffectType.DeathrattleDamageRandomEnemy:  return $"Deal {value} damage to a random enemy";
                case Card.AbilityEffectType.DeathrattleDamageAllEnemies:   return $"Deal {value} damage to all enemies";
                case Card.AbilityEffectType.OnAttackBuffSelf:         return $"Gain +{value} Attack permanently";
                case Card.AbilityEffectType.OnAttackBonusDamage:      return $"Deal +{value} extra damage";
                case Card.AbilityEffectType.OnAttackCleave:           return $"Deal {value} damage to adjacent enemies";
                case Card.AbilityEffectType.Taunt:                    return "Must be attacked first";
                case Card.AbilityEffectType.Reborn:                   return "Returns with 1 Health after dying";
                case Card.AbilityEffectType.Windfury:                 return "Attacks twice each combat";
                case Card.AbilityEffectType.Venomous:                 return "Instantly destroys any minion it damages";
                case Card.AbilityEffectType.GainArmor:                return $"Absorbs {value} damage before Health";
                case Card.AbilityEffectType.OnAllyDeathBuffSelf:      return $"Gain +{value}/+{value} when an ally dies";
                case Card.AbilityEffectType.OnAllyDeathBuffRandom:    return $"Give a random ally +{value}/+{value} when an ally dies";
                case Card.AbilityEffectType.OnAllySummonedBuffSelf:   return $"Gain +{value}/+{value} when an ally is summoned";
                case Card.AbilityEffectType.OnAllySummonedBuffSummoned: return $"Give summoned ally +{value}/+{value}";
                case Card.AbilityEffectType.OnSellGainCoins:          return $"Gain {value} extra gold when sold";
                case Card.AbilityEffectType.OnSellBuffAllRemaining:   return $"Give all remaining allies +{value}/+{value}";
                case Card.AbilityEffectType.AuraBuffTribematesAttack: return $"Tribemates have +{value} Attack";
                case Card.AbilityEffectType.AuraBuffAdjacentStats:    return $"Adjacent allies have +{value}/+{value}";
                case Card.AbilityEffectType.AuraBuffAllFriendlyAttack: return $"All other allies have +{value} Attack";
                case Card.AbilityEffectType.SummonTokenOnDeath:       return $"Summon a {value}/{value} token on death";
                case Card.AbilityEffectType.SummonTokenOnPlay:        return $"Summon a {value}/{value} token when played";
                case Card.AbilityEffectType.StealBuffOnAttack:        return $"Steal +{value}/+{value} from target";
                case Card.AbilityEffectType.BuffAllTribeOnPlay:       return $"Give all tribemates +{value}/+{value}";
                case Card.AbilityEffectType.BuffAllTribeOnDeath:      return $"Give all tribemates +{value}/+{value} on death";
                case Card.AbilityEffectType.RandomTransformOnDeath:   return "Transform into a random card on death";
                case Card.AbilityEffectType.BuffSelfHealth:           return $"Gain +{value} Health";
                default: return effect.ToString();
            }
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

            // UX20: Clean up dynamically created filter buttons
            foreach (var btn in tribeButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();
            foreach (var btn in tierButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}
