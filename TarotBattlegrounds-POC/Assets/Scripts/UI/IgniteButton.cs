using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// The single interaction component for every tappable (DESIGN.md §7/§10.5).
    /// Press ignites within Tokens.DurPress, release acts, holding past
    /// Tokens.LongPressTime fires onLongPress (tooltip/detail) and suppresses the
    /// click, hover-ignites on PC pointers only. No scaling on any state.
    ///
    /// Coexists with a sibling Button (kept for onClick wiring/interactable) but
    /// forces its Transition to None — IgniteButton owns all visual state, so
    /// baked transition settings in existing scenes are neutralized without
    /// scene regeneration.
    ///
    /// All visuals resolve from Tokens at runtime (tones are serialized as enums,
    /// never Colors) so scenes hold no baked color values.
    /// </summary>
    [DisallowMultipleComponent]
    public class IgniteButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum LabelTone { BoneDim, Bone, BoneBright }
        public enum EdgeTone { StoneHairline, Bronze }

        [Header("Background (optional — sprite-swap buttons)")]
        [SerializeField] private Image background;
        [SerializeField] private Sprite idleSprite;      // button_bronze
        [SerializeField] private Sprite ignitedSprite;   // button_ember
        [SerializeField] private Sprite pressedSprite;   // button_bronze_pressed (press flash)

        [Header("Label (optional — tone shifts up one bone level on ignite)")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private LabelTone idleTone = LabelTone.Bone;

        [Header("Extras (optional)")]
        [SerializeField] private Graphic edge;           // border graphic; Ember when ignited
        [SerializeField] private EdgeTone edgeIdleTone = EdgeTone.StoneHairline;
        [SerializeField] private GameObject underline;   // underline_ember, menu rows

        [Header("Events")]
        public UnityEvent onLongPress = new UnityEvent();

        private Button button;
        private bool initialized;
        private bool pressed;
        private bool hovered;          // PC pointer only
        private bool selected;         // toggle/selection state — ignited look persists
        private bool longPressFired;
        private bool pressFlashDone;
        private float pressStartTime;

        // Color transitions (label/edge). t >= 1 means settled.
        private Color labelFrom, labelTo;
        private float labelT = 1f, labelDur;
        private Color edgeFrom, edgeTo;
        private float edgeT = 1f, edgeDur;

        public bool IsIgnited => pressed || hovered || selected;
        public bool LongPressFired => longPressFired;

        /// <summary>Selection is one of the ignited states (DESIGN.md §7) — e.g. toggle groups.</summary>
        public bool Selected
        {
            get => selected;
            set
            {
                EnsureInit();
                if (selected == value) return;
                selected = value;
                ApplyState(instant: false, duration: UiMotion.Dur(Tokens.DurFast));
            }
        }

        private bool Interactable => button == null || button.interactable;

        // ─── Lifecycle ───

        private void Awake() => EnsureInit();

        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            longPressFired = false;
            if (initialized) ApplyState(instant: true);
        }

        private void Update()
        {
            EvaluateLongPress(Time.unscaledTime);
            EvaluatePressFlash(Time.unscaledTime);
            TickTransitions(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Lazy init so behavior is identical in Play mode and EditMode tests
        /// (Awake does not run on AddComponent in EditMode).
        /// </summary>
        private void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            button = GetComponent<Button>();
            if (button != null) button.transition = Selectable.Transition.None;
            ApplyState(instant: true);
        }

        // ─── Runtime binding (Photon/off-factory construction paths) ───

        public void Bind(
            Image background, Sprite idleSprite, Sprite ignitedSprite, Sprite pressedSprite,
            TMP_Text label, LabelTone idleTone = LabelTone.Bone,
            Graphic edge = null, EdgeTone edgeIdleTone = EdgeTone.StoneHairline,
            GameObject underline = null)
        {
            this.background = background;
            this.idleSprite = idleSprite;
            this.ignitedSprite = ignitedSprite;
            this.pressedSprite = pressedSprite;
            this.label = label;
            this.idleTone = idleTone;
            this.edge = edge;
            this.edgeIdleTone = edgeIdleTone;
            this.underline = underline;
            EnsureInit();
            ApplyState(instant: true);
        }

        /// <summary>Interactable passthrough with tokenized disabled visuals.</summary>
        public void SetInteractable(bool interactable)
        {
            EnsureInit();
            if (button != null) button.interactable = interactable;
            if (!interactable)
            {
                pressed = false;
                hovered = false;
                longPressFired = false;
            }
            ApplyState(instant: true);
        }

        // ─── Pointer handlers ───

        public void OnPointerDown(PointerEventData eventData)
        {
            EnsureInit();
            if (!Interactable) return;
            pressed = true;
            longPressFired = false;
            pressFlashDone = false;
            pressStartTime = Time.unscaledTime;
            ApplyState(instant: false, duration: UiMotion.Dur(Tokens.DurPress));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EnsureInit();
            bool wasLongPress = longPressFired;
            pressed = false;
            longPressFired = false;
            // Long-press consumed this gesture — release must not also click.
            if (wasLongPress && eventData != null) eventData.eligibleForClick = false;
            ApplyState(instant: false, duration: UiMotion.Dur(Tokens.DurPress));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureInit();
            // Hover ignite is a PC-only press preview — touch has no hover.
            if (!IsMousePointer(eventData) || !Interactable) return;
            hovered = true;
            ApplyState(instant: false, duration: UiMotion.Dur(Tokens.DurFast));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EnsureInit();
            hovered = false;
            pressed = false;
            longPressFired = false;
            ApplyState(instant: false, duration: UiMotion.Dur(Tokens.DurFast));
        }

        private static bool IsMousePointer(PointerEventData eventData)
        {
            // Mouse/pen pointer ids are negative; touches are 0+.
            return eventData != null && eventData.pointerId < 0;
        }

        // ─── Long press / press flash (internal for EditMode tests) ───

        internal void EvaluateLongPress(float now)
        {
            if (!pressed || longPressFired) return;
            if (now - pressStartTime < Tokens.LongPressTime) return;
            longPressFired = true;
            onLongPress.Invoke();
        }

        internal void EvaluatePressFlash(float now)
        {
            if (!pressed || pressFlashDone) return;
            if (background == null || ignitedSprite == null) { pressFlashDone = true; return; }
            if (UiMotion.ReduceMotion || pressedSprite == null || now - pressStartTime >= Tokens.DurPress)
            {
                background.sprite = ignitedSprite;
                pressFlashDone = true;
            }
        }

        // ─── Visual state ───

        private void ApplyState(bool instant, float duration = 0f)
        {
            bool ignited = IsIgnited && Interactable;

            if (background != null)
            {
                if (!ignited) background.sprite = idleSprite != null ? idleSprite : background.sprite;
                else if (pressed && pressedSprite != null && !pressFlashDone) background.sprite = pressedSprite;
                else if (ignitedSprite != null) background.sprite = ignitedSprite;
            }

            SetLabelTarget(ResolveLabelColor(ignited), instant ? 0f : duration);
            SetEdgeTarget(ignited ? Tokens.Ember : ResolveEdgeIdleColor(), instant ? 0f : duration);
            if (underline != null) underline.SetActive(ignited);
        }

        private Color ResolveLabelColor(bool ignited)
        {
            if (!Interactable) return Tokens.BoneDim;
            LabelTone tone = ignited ? Raise(idleTone) : idleTone;
            switch (tone)
            {
                case LabelTone.BoneDim: return Tokens.BoneDim;
                case LabelTone.BoneBright: return Tokens.BoneBright;
                default: return Tokens.Bone;
            }
        }

        private static LabelTone Raise(LabelTone tone) =>
            tone == LabelTone.BoneDim ? LabelTone.Bone : LabelTone.BoneBright;

        private Color ResolveEdgeIdleColor() =>
            edgeIdleTone == EdgeTone.Bronze ? Tokens.Bronze : Tokens.StoneHairline;

        private void SetLabelTarget(Color to, float duration)
        {
            if (label == null) return;
            if (duration <= 0f) { label.color = to; labelT = 1f; return; }
            labelFrom = label.color;
            labelTo = to;
            labelDur = duration;
            labelT = 0f;
        }

        private void SetEdgeTarget(Color to, float duration)
        {
            if (edge == null) return;
            if (duration <= 0f) { edge.color = to; edgeT = 1f; return; }
            edgeFrom = edge.color;
            edgeTo = to;
            edgeDur = duration;
            edgeT = 0f;
        }

        internal void TickTransitions(float dt)
        {
            if (label != null && labelT < 1f)
            {
                labelT = Mathf.Min(1f, labelT + dt / labelDur);
                label.color = Color.Lerp(labelFrom, labelTo, EaseOut(labelT));
            }
            if (edge != null && edgeT < 1f)
            {
                edgeT = Mathf.Min(1f, edgeT + dt / edgeDur);
                edge.color = Color.Lerp(edgeFrom, edgeTo, EaseOut(edgeT));
            }
        }

        // Ease-out entrances (DESIGN.md §7 — no bounce, no elastic).
        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
