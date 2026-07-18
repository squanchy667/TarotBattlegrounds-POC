using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// T702: SettingsUI (T418) shipped as a complete class with zero scene presence — no panel,
/// no button, no wired references. This setup script surgically ADDS the settings entry
/// point to the existing MainMenu scene (it does not regenerate the scene like the ux17
/// MainMenuSetup does): a gear button in the top-right + a hidden settings panel, with
/// SettingsUI's and MainMenuManager's serialized fields wired via SerializedObject.
/// Idempotent: re-running replaces the previously generated objects.
/// Run: Tools > Game > Setup Settings Panel (or -executeMethod SettingsPanelSetup.Setup).
/// </summary>
public static class SettingsPanelSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string RootName = "SettingsRoot";

    private static readonly Color LabelColor = Tokens.Bone;

    [MenuItem("Tools/Game/Setup Settings Panel")]
    public static void Setup()
    {
        string report = SetupMainMenuSettingsCore(save: true);
        Debug.Log(report);
        EditorUtility.DisplayDialog("Settings Panel", report, "OK");
    }

    /// <summary>Dialog-free core for MCP. Returns report string.</summary>
    public static string SetupMainMenuSettingsCore(bool save = true)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var menuManager = Object.FindObjectOfType<MainMenuManager>(true);
        var canvas = Object.FindObjectOfType<Canvas>(true);
        if (menuManager == null || canvas == null)
            return "ABORT: missing MainMenuManager or Canvas";

        // Idempotency: drop any previous run's objects.
        var existing = canvas.transform.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        Button gearButton = CreateGearButton(root.transform);
        (GameObject panel, SettingsUI settings) = CreatePanel(root.transform, gearButton);

        var mm = new SerializedObject(menuManager);
        mm.FindProperty("settingsButton").objectReferenceValue = gearButton;
        mm.FindProperty("settingsUI").objectReferenceValue = settings;
        mm.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (save) EditorSceneManager.SaveScene(scene);
        return "OK Settings gear + panel on MainMenu";
    }

    /// <summary>Add settings gear + panel to Game scene (in-play access).</summary>
    public static string SetupGameSettingsCore(bool save = true)
    {
        const string gamePath = "Assets/Scenes/Game.unity";
        var scene = EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Single);
        var canvas = Object.FindObjectOfType<Canvas>(true);
        if (canvas == null) return "ABORT: no Canvas in Game";

        Transform safe = canvas.transform.Find("SafeArea");
        Transform parent = safe != null ? safe : canvas.transform;

        var existing = parent.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.transform.SetAsLastSibling();

        Button gearButton = CreateGearButton(root.transform);
        (GameObject panel, SettingsUI settings) = CreatePanel(root.transform, gearButton);

        // Runtime wiring is owned by SettingsUI.Awake (editor AddListener does not serialize).
        EditorSceneManager.MarkSceneDirty(scene);
        if (save) EditorSceneManager.SaveScene(scene);
        return "OK Settings gear + panel on Game scene";
    }

    private static Button CreateGearButton(Transform parent)
    {
        GameObject btnObj = EditorUiFactory.CreateButton(parent, "SettingsButton", "⚙",
            72f, 72f, labelFontSize: (int)Tokens.TextH2, addLayoutElement: false);
        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        // Top-right corner; MatchInfo "i" sits to the left (see MatchInfoPanelSetup).
        rt.anchoredPosition = new Vector2(-16f, -16f);
        rt.sizeDelta = new Vector2(72f, 72f); // T715: comfortably above 44pt touch minimum
        return btnObj.GetComponent<Button>();
    }

    private static (GameObject, SettingsUI) CreatePanel(Transform parent, Button openButton = null)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "SettingsPanel");

        // Dim backdrop
        Image panelImg = panel.GetComponent<Image>();
        if (panelImg == null) panelImg = panel.AddComponent<Image>();
        panelImg.color = Tokens.WithAlpha(Tokens.Ash, 0.72f);
        panelImg.raycastTarget = true;

        GameObject box = EditorUiFactory.CreateCenteredContainer(panel.transform, "SettingsBox",
            680f, 760f);
        Image boxImg = box.GetComponent<Image>();
        if (boxImg != null)
        {
            if (UiSprites.Instance != null && UiSprites.Instance.PanelCharred != null)
                UiSprites.ApplySliced(boxImg, UiSprites.Instance.PanelCharred);
            else
                boxImg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.96f);
        }
        var layout = box.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, (int)Tokens.Space3, (int)Tokens.Space3);
        layout.spacing = 14f;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        EditorUiFactory.CreateText(box.transform, "Title", "Settings", (int)Tokens.TextH2,
            FontStyles.Bold, Tokens.BoneBright, TextAlignmentOptions.Center);

        // Host SettingsUI on the active parent (SettingsRoot), NOT the inactive panel.
        // Inactive GameObjects never run Awake/Start, so the gear would never wire.
        var existingOnPanel = panel.GetComponent<SettingsUI>();
        if (existingOnPanel != null) Object.DestroyImmediate(existingOnPanel);
        var settings = parent.GetComponent<SettingsUI>();
        if (settings == null) settings = parent.gameObject.AddComponent<SettingsUI>();
        var so = new SerializedObject(settings);
        so.FindProperty("settingsPanel").objectReferenceValue = panel;
        if (openButton != null)
            so.FindProperty("openButton").objectReferenceValue = openButton;
        else
        {
            var gearT = parent.Find("SettingsButton");
            if (gearT != null)
                so.FindProperty("openButton").objectReferenceValue = gearT.GetComponent<Button>();
        }

        WireSlider(so, box.transform, "Master Volume", "masterVolumeSlider", "masterVolumeText", 1f);
        WireSlider(so, box.transform, "Music Volume", "musicVolumeSlider", "musicVolumeText", 0.7f);
        WireSlider(so, box.transform, "SFX Volume", "sfxVolumeSlider", "sfxVolumeText", 0.8f);
        WireToggle(so, box.transform, "Combat VFX", "vfxToggle", true);
        WireToggle(so, box.transform, "Auto End Turn", "autoEndTurnToggle", false);
        WireSlider(so, box.transform, "Combat Speed", "combatSpeedSlider", "combatSpeedText", 1f, 0.5f, 2f);

        // Quality dropdown (row built like the sliders, control from the shared factory).
        GameObject qRow = EditorUiFactory.CreateHorizontalRow(box.transform, "QualityRow", 12f);
        CreateSettingsLabel(qRow.transform, "Quality");
        GameObject dd = EditorUiFactory.CreateDropdown(qRow.transform, "QualityDropdown",
            240f, 48f, "Medium", (int)Tokens.TextCaption);
        so.FindProperty("qualityDropdown").objectReferenceValue = dd.GetComponent<TMP_Dropdown>();

        // Fullscreen toggle is desktop-only; SettingsUI.Start hides this row on mobile.
        WireToggle(so, box.transform, "Fullscreen", "fullscreenToggle", true);

        GameObject closeObj = EditorUiFactory.CreateButton(box.transform, "CloseButton", "Close",
            220f, Tokens.MinTouchTarget, labelFontSize: (int)Tokens.TextLabel);
        so.FindProperty("closeButton").objectReferenceValue = closeObj.GetComponent<Button>();
        // Ignite close if available
        if (closeObj.GetComponent<IgniteButton>() == null)
            closeObj.AddComponent<IgniteButton>();

        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        return (panel, settings);
    }

    /// <summary>
    /// Label for a settings row. CreateText defaults to width 0 + word wrap, which under
    /// HorizontalLayoutGroup collapses into a single-character-wide vertical stack.
    /// Force a fixed horizontal width and disable wrapping so "Master Volume" reads left-to-right.
    /// </summary>
    private static GameObject CreateSettingsLabel(Transform parent, string text, float width = 200f)
    {
        GameObject labelObj = EditorUiFactory.CreateText(parent, "Label", text, (int)Tokens.TextBody,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Left);
        var tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        float h = Tokens.TextBody + 12f;
        var rt = labelObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, h);

        var le = labelObj.GetComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
        le.minHeight = h;
        le.preferredHeight = h;
        return labelObj;
    }

    private static GameObject CreateSettingsValueText(Transform parent, string text, float width = 64f)
    {
        GameObject valueObj = EditorUiFactory.CreateText(parent, "Value", text, (int)Tokens.TextCaption,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Right);
        var tmp = valueObj.GetComponent<TextMeshProUGUI>();
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        float h = Tokens.TextCaption + 12f;
        var rt = valueObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, h);

        var le = valueObj.GetComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
        le.minHeight = h;
        le.preferredHeight = h;
        return valueObj;
    }

    private static void WireSlider(SerializedObject so, Transform parent, string label,
        string sliderField, string textField, float defaultValue, float min = 0f, float max = 1f)
    {
        GameObject row = EditorUiFactory.CreateHorizontalRow(parent, label.Replace(" ", "") + "Row", 12f);
        CreateSettingsLabel(row.transform, label);

        GameObject sliderObj = DefaultControls.CreateSlider(UiResources());
        sliderObj.name = label.Replace(" ", "") + "Slider";
        sliderObj.transform.SetParent(row.transform, false);
        var le = sliderObj.AddComponent<LayoutElement>();
        le.minWidth = 220f;
        le.preferredWidth = 280f;
        le.flexibleWidth = 1f;
        le.minHeight = 44f; // touch target
        le.preferredHeight = 44f;
        var slider = sliderObj.GetComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = defaultValue;

        // Combat Speed uses 0.5–2.0; show 1 decimal "x" suffix instead of percent.
        string valueStr = (max > 1.01f)
            ? defaultValue.ToString("0.0") + "x"
            : Mathf.RoundToInt(defaultValue * 100f) + "%";
        GameObject valueText = CreateSettingsValueText(row.transform, valueStr);

        so.FindProperty(sliderField).objectReferenceValue = slider;
        so.FindProperty(textField).objectReferenceValue = valueText.GetComponent<TMP_Text>();
    }

    private static void WireToggle(SerializedObject so, Transform parent, string label,
        string toggleField, bool defaultValue)
    {
        GameObject row = EditorUiFactory.CreateHorizontalRow(parent, label.Replace(" ", "") + "Row", 12f);
        CreateSettingsLabel(row.transform, label);

        GameObject toggleObj = DefaultControls.CreateToggle(UiResources());
        toggleObj.name = label.Replace(" ", "") + "Toggle";
        toggleObj.transform.SetParent(row.transform, false);
        var le = toggleObj.AddComponent<LayoutElement>();
        le.minWidth = 56f;
        le.preferredWidth = 56f;
        le.flexibleWidth = 0f;
        le.minHeight = 44f;
        le.preferredHeight = 44f;
        var toggle = toggleObj.GetComponent<Toggle>();
        toggle.isOn = defaultValue;
        var builtinLabel = toggleObj.transform.Find("Label");
        if (builtinLabel != null) Object.DestroyImmediate(builtinLabel.gameObject); // row has its own

        so.FindProperty(toggleField).objectReferenceValue = toggle;
    }

    private static DefaultControls.Resources UiResources()
    {
        // Standard built-in UI sprites (same set the editor's GameObject > UI menu uses).
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
