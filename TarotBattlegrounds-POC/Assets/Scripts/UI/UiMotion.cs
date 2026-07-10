using UnityEngine;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// Reduce-motion preference (DESIGN.md §7 / review checklist §11).
    /// Honored by IgniteButton and any UI transition; the Settings toggle
    /// lands with T753. When on, all UI transitions resolve instantly.
    /// </summary>
    public static class UiMotion
    {
        private const string PrefKey = "ui.reduce_motion";
        private static bool? cached;

        public static bool ReduceMotion
        {
            get
            {
                if (!cached.HasValue) cached = PlayerPrefs.GetInt(PrefKey, 0) == 1;
                return cached.Value;
            }
            set
            {
                cached = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Duration with reduce-motion applied — 0 means "set instantly".</summary>
        public static float Dur(float duration) => ReduceMotion ? 0f : duration;
    }
}
