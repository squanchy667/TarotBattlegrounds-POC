using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TarotBattlegrounds.UI;

/// <summary>
/// T750: IgniteButton interaction model (DESIGN.md §7/§10.5) — ignite on press,
/// revert on release/exit, PC-only hover ignite, long-press event with click
/// suppression, tokenized visuals, and the no-scaling rule.
/// </summary>
[TestFixture]
public class IgniteButtonTests
{
    private GameObject buttonGO;
    private GameObject labelGO;
    private GameObject underlineGO;
    private Image background;
    private Button button;
    private IgniteButton ignite;
    private TextMeshProUGUI label;
    private Sprite idleSprite, ignitedSprite, pressedSprite;
    private bool originalReduceMotion;

    [SetUp]
    public void Setup()
    {
        originalReduceMotion = UiMotion.ReduceMotion;
        UiMotion.ReduceMotion = true; // instant transitions for deterministic asserts

        buttonGO = new GameObject("IgniteButton", typeof(RectTransform));
        background = buttonGO.AddComponent<Image>();
        button = buttonGO.AddComponent<Button>();
        ignite = buttonGO.AddComponent<IgniteButton>();

        labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        label = labelGO.AddComponent<TextMeshProUGUI>();

        underlineGO = new GameObject("Underline", typeof(RectTransform));
        underlineGO.transform.SetParent(buttonGO.transform, false);

        idleSprite = MakeSprite();
        ignitedSprite = MakeSprite();
        pressedSprite = MakeSprite();

        ignite.Bind(background, idleSprite, ignitedSprite, pressedSprite,
            label, IgniteButton.LabelTone.Bone,
            edge: null, edgeIdleTone: IgniteButton.EdgeTone.StoneHairline,
            underline: underlineGO);
    }

    [TearDown]
    public void Teardown()
    {
        UiMotion.ReduceMotion = originalReduceMotion;
        Object.DestroyImmediate(buttonGO);
    }

    private static Sprite MakeSprite()
    {
        var tex = new Texture2D(4, 4);
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    private static PointerEventData Pointer(int pointerId)
    {
        return new PointerEventData(EventSystem.current) { pointerId = pointerId };
    }

    // ─── Idle / bind ───

    [Test]
    public void Bind_AppliesIdleVisuals_AndForcesTransitionNone()
    {
        Assert.AreEqual(idleSprite, background.sprite);
        Assert.AreEqual(Tokens.Bone, label.color);
        Assert.IsFalse(underlineGO.activeSelf);
        Assert.AreEqual(Selectable.Transition.None, button.transition,
            "IgniteButton must neutralize baked Button transitions");
    }

    // ─── Press ignite ───

    [Test]
    public void PointerDown_Ignites_LabelUpOneBoneLevel_UnderlineOn()
    {
        ignite.OnPointerDown(Pointer(0));
        ignite.EvaluatePressFlash(Time.unscaledTime);

        Assert.IsTrue(ignite.IsIgnited);
        Assert.AreEqual(ignitedSprite, background.sprite);
        Assert.AreEqual(Tokens.BoneBright, label.color);
        Assert.IsTrue(underlineGO.activeSelf);
    }

    [Test]
    public void PressFlash_ShowsPressedSpriteThenIgnited_AtDurPress()
    {
        UiMotion.ReduceMotion = false;
        float t0 = Time.unscaledTime;

        ignite.OnPointerDown(Pointer(0));
        Assert.AreEqual(pressedSprite, background.sprite, "press flash first");

        ignite.EvaluatePressFlash(t0 + Tokens.DurPress * 0.5f);
        Assert.AreEqual(pressedSprite, background.sprite, "still flashing before DurPress");

        ignite.EvaluatePressFlash(t0 + Tokens.DurPress + 0.01f);
        Assert.AreEqual(ignitedSprite, background.sprite, "ignited after DurPress");
    }

    [Test]
    public void PointerUp_Touch_RevertsToIdle()
    {
        ignite.OnPointerDown(Pointer(0));
        ignite.EvaluatePressFlash(Time.unscaledTime);
        ignite.OnPointerUp(Pointer(0));

        Assert.IsFalse(ignite.IsIgnited);
        Assert.AreEqual(idleSprite, background.sprite);
        Assert.AreEqual(Tokens.Bone, label.color);
        Assert.IsFalse(underlineGO.activeSelf);
    }

    [Test]
    public void PointerExit_CancelsPressAndReverts()
    {
        ignite.OnPointerDown(Pointer(0));
        ignite.OnPointerExit(Pointer(0));

        Assert.IsFalse(ignite.IsIgnited);
        Assert.AreEqual(idleSprite, background.sprite);
    }

    // ─── Hover: PC only ───

    [Test]
    public void PointerEnter_Mouse_Ignites()
    {
        ignite.OnPointerEnter(Pointer(-1)); // mouse pointer ids are negative
        Assert.IsTrue(ignite.IsIgnited);
        Assert.AreEqual(Tokens.BoneBright, label.color);
    }

    [Test]
    public void PointerEnter_Touch_DoesNotIgnite()
    {
        ignite.OnPointerEnter(Pointer(0)); // touch pointer ids are 0+
        Assert.IsFalse(ignite.IsIgnited);
        Assert.AreEqual(idleSprite, background.sprite);
        Assert.AreEqual(Tokens.Bone, label.color);
    }

    [Test]
    public void PointerUp_WhileMouseHovered_StaysIgnited()
    {
        ignite.OnPointerEnter(Pointer(-1));
        ignite.OnPointerDown(Pointer(-1));
        ignite.OnPointerUp(Pointer(-1));

        Assert.IsTrue(ignite.IsIgnited, "mouse still over the button — hover ignite persists");
    }

    // ─── Long press ───

    [Test]
    public void LongPress_FiresOnceAtThreshold_AndSuppressesClick()
    {
        int fired = 0;
        ignite.onLongPress.AddListener(() => fired++);
        float t0 = Time.unscaledTime;

        ignite.OnPointerDown(Pointer(0));
        ignite.EvaluateLongPress(t0 + Tokens.LongPressTime - 0.05f);
        Assert.AreEqual(0, fired, "must not fire before the threshold");

        ignite.EvaluateLongPress(t0 + Tokens.LongPressTime + 0.05f);
        ignite.EvaluateLongPress(t0 + Tokens.LongPressTime + 0.10f);
        Assert.AreEqual(1, fired, "fires exactly once per gesture");

        var up = Pointer(0);
        up.eligibleForClick = true;
        ignite.OnPointerUp(up);
        Assert.IsFalse(up.eligibleForClick, "release after long-press must not click");
    }

    [Test]
    public void ShortPress_DoesNotSuppressClick()
    {
        ignite.OnPointerDown(Pointer(0));
        var up = Pointer(0);
        up.eligibleForClick = true;
        ignite.OnPointerUp(up);
        Assert.IsTrue(up.eligibleForClick);
    }

    // ─── No scaling, ever ───

    [Test]
    public void NoScaling_OnAnyState()
    {
        Assert.AreEqual(Vector3.one, buttonGO.transform.localScale);
        ignite.OnPointerEnter(Pointer(-1));
        Assert.AreEqual(Vector3.one, buttonGO.transform.localScale);
        ignite.OnPointerDown(Pointer(-1));
        Assert.AreEqual(Vector3.one, buttonGO.transform.localScale);
        ignite.TickTransitions(0.1f);
        Assert.AreEqual(Vector3.one, buttonGO.transform.localScale);
        ignite.OnPointerUp(Pointer(-1));
        ignite.OnPointerExit(Pointer(-1));
        Assert.AreEqual(Vector3.one, buttonGO.transform.localScale);
    }

    // ─── Selected state (toggle groups — DESIGN.md §7: selection is ignited) ───

    [Test]
    public void Selected_PersistsIgnitedLook_WithoutPointer()
    {
        ignite.Selected = true;
        Assert.IsTrue(ignite.IsIgnited);
        Assert.AreEqual(ignitedSprite, background.sprite);
        Assert.AreEqual(Tokens.BoneBright, label.color);

        ignite.Selected = false;
        Assert.IsFalse(ignite.IsIgnited);
        Assert.AreEqual(idleSprite, background.sprite);
        Assert.AreEqual(Tokens.Bone, label.color);
    }

    [Test]
    public void Selected_SurvivesPressAndRelease()
    {
        ignite.Selected = true;
        ignite.OnPointerDown(Pointer(0));
        ignite.EvaluatePressFlash(Time.unscaledTime);
        ignite.OnPointerUp(Pointer(0));

        Assert.IsTrue(ignite.IsIgnited, "selection persists after the gesture ends");
        Assert.AreEqual(ignitedSprite, background.sprite);
    }

    // ─── Disabled state ───

    [Test]
    public void Disabled_DimsLabel_AndIgnoresPress()
    {
        ignite.SetInteractable(false);
        Assert.AreEqual(Tokens.BoneDim, label.color);
        Assert.IsFalse(button.interactable);

        ignite.OnPointerDown(Pointer(0));
        Assert.IsFalse(ignite.IsIgnited);
        Assert.AreEqual(idleSprite, background.sprite);

        ignite.SetInteractable(true);
        Assert.AreEqual(Tokens.Bone, label.color);
    }
}

/// <summary>T750: FontRefs — the four design-system TMP assets, no string-loads.</summary>
[TestFixture]
public class FontRefsTests
{
    [Test]
    public void Instance_LoadsFromResources_WithAllFourFonts()
    {
        var refs = FontRefs.Instance;
        Assert.IsNotNull(refs, "Resources/FontRefs.asset must exist");
        Assert.IsNotNull(refs.Display, "Display (Cinzel-Bold SDF) not wired");
        Assert.IsNotNull(refs.Body, "Body (AlegreyaSans-Regular SDF) not wired");
        Assert.IsNotNull(refs.Label, "Label (AlegreyaSans-Medium SDF) not wired");
        Assert.IsNotNull(refs.Numeric, "Numeric (AlegreyaSans-Bold SDF) not wired");
    }
}
