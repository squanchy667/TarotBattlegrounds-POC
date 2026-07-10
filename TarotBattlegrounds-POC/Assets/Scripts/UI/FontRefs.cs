using TMPro;
using UnityEngine;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// The four TMP font assets of the design system (DESIGN.md §4), exposed as
    /// typed references so runtime code never string-loads fonts. The single
    /// asset lives at Resources/FontRefs (same idiom as SFXConfig/DataConfig).
    /// </summary>
    [CreateAssetMenu(fileName = "FontRefs", menuName = "TarotBattlegrounds/Font Refs")]
    public class FontRefs : ScriptableObject
    {
        [Tooltip("Cinzel-Bold SDF — titles, arcana numerals, Display/Hero sizes only")]
        [SerializeField] private TMP_FontAsset display;

        [Tooltip("AlegreyaSans-Regular SDF — body text")]
        [SerializeField] private TMP_FontAsset body;

        [Tooltip("AlegreyaSans-Medium SDF — labels and button text (caps, +4% tracking)")]
        [SerializeField] private TMP_FontAsset label;

        [Tooltip("AlegreyaSans-Bold SDF — stats, costs, timers, damage numbers")]
        [SerializeField] private TMP_FontAsset numeric;

        public TMP_FontAsset Display => display;
        public TMP_FontAsset Body => body;
        public TMP_FontAsset Label => label;
        public TMP_FontAsset Numeric => numeric;

        private static FontRefs instance;

        /// <summary>Loaded once from Resources/FontRefs; null only if the asset is missing.</summary>
        public static FontRefs Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<FontRefs>("FontRefs");
                return instance;
            }
        }
    }
}
