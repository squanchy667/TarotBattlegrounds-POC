using UnityEngine;
using UnityEngine.UI;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T402-T404: Generates procedural card frames with rarity borders,
    /// tribe-colored backgrounds, and golden overlays.
    /// Attach alongside CardDisplayUI for automatic visual enhancement.
    /// </summary>
    public class CardFrameGenerator : MonoBehaviour
    {
        [Header("Frame References")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image goldenOverlay;
        [SerializeField] private Image tribeAccentBar;

        [Header("Frame Width")]
        [SerializeField] private float frameBorderWidth = 4f;

        // Tier-based rarity colors
        private static readonly Color[] TierFrameColors = new Color[]
        {
            new Color(0.6f, 0.6f, 0.6f, 1f),    // Tier 1: Gray (Common)
            new Color(0.2f, 0.7f, 0.3f, 1f),     // Tier 2: Green (Uncommon)
            new Color(0.2f, 0.4f, 0.9f, 1f),     // Tier 3: Blue (Rare)
            new Color(0.6f, 0.2f, 0.8f, 1f),     // Tier 4: Purple (Epic)
            new Color(1f, 0.6f, 0.1f, 1f),        // Tier 5: Orange (Legendary)
            new Color(1f, 0.85f, 0.2f, 1f)        // Tier 6: Gold (Mythic)
        };

        // Tribe background tints (subtle, darkened)
        public static Color GetTribeBackgroundColor(TribeType tribe)
        {
            switch (tribe)
            {
                case TribeType.Pentacles: return new Color(0.15f, 0.25f, 0.12f, 1f);
                case TribeType.Cups:      return new Color(0.12f, 0.18f, 0.3f, 1f);
                case TribeType.Swords:    return new Color(0.3f, 0.12f, 0.12f, 1f);
                case TribeType.Wands:     return new Color(0.3f, 0.18f, 0.08f, 1f);
                case TribeType.Stars:     return new Color(0.25f, 0.22f, 0.1f, 1f);
                case TribeType.Coins:     return new Color(0.28f, 0.22f, 0.08f, 1f);
                default:                  return new Color(0.15f, 0.15f, 0.18f, 1f);
            }
        }

        // Tribe accent colors (vibrant, for accent bar)
        public static Color GetTribeAccentColor(TribeType tribe)
        {
            switch (tribe)
            {
                case TribeType.Pentacles: return new Color(0.2f, 0.8f, 0.3f, 1f);
                case TribeType.Cups:      return new Color(0.3f, 0.5f, 1f, 1f);
                case TribeType.Swords:    return new Color(0.9f, 0.2f, 0.2f, 1f);
                case TribeType.Wands:     return new Color(0.95f, 0.5f, 0.1f, 1f);
                case TribeType.Stars:     return new Color(1f, 0.9f, 0.3f, 1f);
                case TribeType.Coins:     return new Color(0.9f, 0.7f, 0.2f, 1f);
                default:                  return new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        /// <summary>
        /// Apply full card frame visuals based on card data.
        /// </summary>
        public void ApplyCardVisuals(Card card)
        {
            if (card == null) return;

            // T402: Rarity border based on tier
            ApplyRarityFrame(card.tier);

            // T403: Tribe-colored background
            TribeType primaryTribe = card.GetPrimaryTribe();
            ApplyTribeBackground(primaryTribe);

            // T404: Golden overlay for triples
            ApplyGoldenOverlay(card.isGolden);
        }

        private void ApplyRarityFrame(int tier)
        {
            if (frameImage == null) return;

            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            frameImage.color = TierFrameColors[index];
        }

        private void ApplyTribeBackground(TribeType tribe)
        {
            if (backgroundImage != null)
                backgroundImage.color = GetTribeBackgroundColor(tribe);

            if (tribeAccentBar != null)
            {
                tribeAccentBar.color = GetTribeAccentColor(tribe);
                tribeAccentBar.gameObject.SetActive(tribe != TribeType.None);
            }
        }

        private void ApplyGoldenOverlay(bool isGolden)
        {
            if (goldenOverlay == null) return;

            goldenOverlay.gameObject.SetActive(isGolden);
            if (isGolden)
            {
                goldenOverlay.color = new Color(1f, 0.85f, 0f, 0.25f);
            }
        }

        /// <summary>
        /// Get frame color for a given tier (used by other UI systems).
        /// </summary>
        public static Color GetTierFrameColor(int tier)
        {
            int index = Mathf.Clamp(tier - 1, 0, TierFrameColors.Length - 1);
            return TierFrameColors[index];
        }
    }
}
