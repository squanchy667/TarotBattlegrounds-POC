using UnityEngine;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// Design tokens — the single source of truth for all visual values.
    /// Implements DESIGN.md. If a value isn't here, it doesn't ship.
    /// Register: Dark Tribal Realism.
    /// RULE: no UI script may contain a color/size/duration literal — reference Tokens.
    /// </summary>
    public static class Tokens
    {
        // ─── Surfaces (dark materials — warm blacks, never pure #000) ───
        public static readonly Color Ash         = Hex("#0F0C09"); // app background, deepest layer
        public static readonly Color CharredWood = Hex("#1A1410"); // panels, card back-plates, slabs
        public static readonly Color Umber       = Hex("#28201A"); // elevated: hover rows, modals, tooltips
        public static readonly Color StoneEdge   = Hex("#3A2F26"); // borders/dividers (hairline at 40% alpha)

        // ─── Bone (text hierarchy) ───
        public static readonly Color BoneBright  = Hex("#EAE0CE"); // primary text, titles, key numbers
        public static readonly Color Bone        = Hex("#C9BBA4"); // body text, labels
        public static readonly Color BoneDim     = Hex("#8E8271"); // secondary, disabled, captions

        // ─── Bronze (structure, worth, chrome) ───
        public static readonly Color Bronze       = Hex("#9A7134"); // frame edges, sigil linework, tiers
        public static readonly Color BronzeBright = Hex("#D3A55A"); // highlights, selected frames, gold text

        // ─── Blood (danger, cost, aggression) ───
        public static readonly Color BloodDeep = Hex("#571219"); // fills, damage backgrounds
        public static readonly Color Blood     = Hex("#9E1B26"); // damage numbers, destructive actions

        // ─── Ember (interaction light: hover / focus / selection) ───
        public static readonly Color Ember = Hex("#D96C2B");

        // ─── Ethereal (MAGIC ONLY — never chrome, buttons, or plain text.
        //     Budget: ≤ 1 visible use per screen outside combat.) ───
        public static readonly Color EtherBlue   = Hex("#5FA8D8"); // spells, enchanted, arcane rarity
        public static readonly Color EtherViolet = Hex("#8B6FC9"); // mythic arcana, rune glow

        // ─── Derived (common alpha variants) ───
        public static Color StoneHairline => WithAlpha(StoneEdge, 0.40f);
        public static Color SigilIdle     => WithAlpha(Bronze, 0.35f);   // engraved linework at rest
        public static Color RimLight      => WithAlpha(BronzeBright, 0.30f); // 1px lit edge (depth)

        // ─── Type scale (ratio ≈ 1.25, base 24 — mobile-first legibility).
        //     All values at 1920×1080 reference resolution (CanvasScaler match 0.5).
        //     Floor: nothing below TextCaption anywhere. ───
        public const float TextCaption = 18f;
        public const float TextBody    = 24f;
        public const float TextLabel   = 24f;  // Medium weight, +4% tracking, caps on buttons
        public const float TextH3      = 30f;
        public const float TextH2      = 37f;
        public const float TextH1      = 47f;
        public const float TextDisplay = 59f;  // Cinzel only, never below 30
        public const float TextHero    = 73f;

        // ─── Touch ergonomics (reference res) ───
        public const float MinTouchTarget = 88f;   // raycast hit area, every tappable
        public const float MenuRowHeight  = 96f;   // full-width tappable menu rows
        public const float SlotHeight     = 112f;  // lobby player slots
        public const float MinTouchGap    = 16f;   // spacing between adjacent tappables
        public const float LongPressTime  = 0.4f;  // tooltip / detail on hold

        public const float TrackingDisplay = 0.06f; // Cinzel titles
        public const float TrackingLabel   = 0.04f; // button labels

        // ─── Spacing (unit 8 — these five steps ONLY) ───
        public const float Space1 = 8f;
        public const float Space2 = 16f;
        public const float Space3 = 24f;
        public const float Space4 = 40f;
        public const float Space5 = 64f;

        // ─── Shape ───
        public const float Radius     = 0f;   // hewn, not rounded — always 0
        public const float NotchSize  = 12f;  // 45° corner-cut, TL + BR only (in 9-slice sprites)
        public const float BorderThin = 1f;   // StoneEdge default
        public const float BorderFrame = 2f;  // Bronze on important frames; Ember when ignited

        // ─── Motion ───
        public const float DurPress = 0.08f; // touch/click ignite — mandatory, instant
        public const float DurFast  = 0.12f; // PC hover ignite (preview of press)
        public const float DurBase  = 0.20f; // panel/state changes
        public const float DurSlow  = 0.45f; // screen transitions ONLY
        // Easing: ease-out entrances, ease-in exits. No bounce/elastic anywhere.
        // Hover NEVER gates anything — mobile has no hover; long-press is the
        // touch path for tooltips/detail (see LongPressTime).

        // ─── Texture ───
        public const float TextureOpacityMin = 0.04f;
        public const float TextureOpacityMax = 0.08f;
        public const float MenuVignette      = 0.15f;

        // ─── Helpers ───
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c)
                ? c
                : Color.magenta; // loud failure — magenta means a broken token
        }
    }
}
