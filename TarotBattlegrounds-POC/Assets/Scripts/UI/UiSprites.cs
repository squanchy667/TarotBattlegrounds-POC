using UnityEngine;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// Design-system UI sprites (DESIGN.md §5/§10.4) as typed references for
    /// runtime-constructed UI (IgniteButton auto-attach, Photon lobby paths) —
    /// no string-loads. Single asset at Resources/UiSprites, same idiom as FontRefs.
    /// Editor factories may also use it so sprite wiring stays in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSprites", menuName = "TarotBattlegrounds/Ui Sprites")]
    public class UiSprites : ScriptableObject
    {
        [Header("Buttons (9-sliced, notched)")]
        [SerializeField] private Sprite buttonBronze;
        [SerializeField] private Sprite buttonEmber;
        [SerializeField] private Sprite buttonBronzePressed;
        [SerializeField] private Sprite buttonBlood;

        [Header("Panels (9-sliced)")]
        [SerializeField] private Sprite panelCharred;
        [SerializeField] private Sprite panelCharredBorderless;
        [SerializeField] private Sprite panelUmber;

        [Header("Lobby slots (9-sliced)")]
        [SerializeField] private Sprite slotEmpty;
        [SerializeField] private Sprite slotFilled;
        [SerializeField] private Sprite slotReady;

        [Header("Accents")]
        [SerializeField] private Sprite underlineEmber;
        [SerializeField] private Sprite dividerStone;

        [Header("WO-07 Card anatomy (DESIGN §10.4)")]
        [SerializeField] private Sprite cardFrameT1Stone;
        [SerializeField] private Sprite cardFrameT2Bronze;
        [SerializeField] private Sprite cardFrameT3Gold;
        [SerializeField] private Sprite cardFrameT4Ether;
        [SerializeField] private Sprite cardFrameT5Mythic;
        [SerializeField] private Sprite cardNameplate;
        [SerializeField] private Sprite chipAttack;
        [SerializeField] private Sprite chipHealth;
        [SerializeField] private Sprite chipCost;

        public Sprite ButtonBronze => buttonBronze;
        public Sprite ButtonEmber => buttonEmber;
        public Sprite ButtonBronzePressed => buttonBronzePressed;
        public Sprite ButtonBlood => buttonBlood;
        public Sprite PanelCharred => panelCharred;
        public Sprite PanelCharredBorderless => panelCharredBorderless;
        public Sprite PanelUmber => panelUmber;
        public Sprite SlotEmpty => slotEmpty;
        public Sprite SlotFilled => slotFilled;
        public Sprite SlotReady => slotReady;
        public Sprite UnderlineEmber => underlineEmber;
        public Sprite DividerStone => dividerStone;
        public Sprite CardNameplate => cardNameplate;
        public Sprite ChipAttack => chipAttack;
        public Sprite ChipHealth => chipHealth;
        public Sprite ChipCost => chipCost;

        /// <summary>Tier 1–5 kit frames; null if unwired or tier out of range. Tier ≥6 → null (ESCALATE).</summary>
        public Sprite GetCardFrame(int tier)
        {
            switch (tier)
            {
                case 1: return cardFrameT1Stone;
                case 2: return cardFrameT2Bronze;
                case 3: return cardFrameT3Gold;
                case 4: return cardFrameT4Ether;
                case 5: return cardFrameT5Mythic;
                default: return null;
            }
        }

        private static UiSprites instance;

        /// <summary>Loaded once from Resources/UiSprites; null only if the asset is missing.</summary>
        public static UiSprites Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<UiSprites>("UiSprites");
                return instance;
            }
        }

        /// <summary>
        /// Standard Image setup for the 9-sliced kit sprites: Sliced + PPU multiplier 2
        /// so the 48px borders render at their intended physical size (DESIGN.md §10.4).
        /// </summary>
        public static void ApplySliced(UnityEngine.UI.Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = Color.white;
        }
    }
}
