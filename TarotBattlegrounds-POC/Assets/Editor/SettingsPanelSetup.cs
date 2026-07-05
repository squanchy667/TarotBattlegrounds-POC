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

    private static readonly Color LabelColor = new Color(0.92f, 0.89f, 0.82f, 1f);

    [MenuItem("Tools/Game/Setup Settings Panel")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var menuManager = Object.FindObjectOfType<MainMenuManager>(true);
        var canvas = Object.FindObjectOfType<Canvas>(true);
        if (menuManager == null || canvas == null)
        {
            Debug.LogError($"[SettingsPanelSetup] Missing {(menuManager == null ? "MainMenuManager" : "Canvas")} in {ScenePath} — aborting.");
            return;
        }

        // Idempotency: drop any previous run's objects.
        var existing = canvas.transform.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        Button gearButton = CreateGearButton(root.transform);
        (GameObject panel, SettingsUI settings) = CreatePanel(root.transform);

        // Wire MainMenuManager.settingsButton / settingsUI (private [SerializeField]).
        var mm = new SerializedObject(menuManager);
        mm.FindProperty("settingsButton").objectReferenceValue = gearButton;
        mm.FindProperty("settingsUI").objectReferenceValue = settings;
        mm.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SettingsPanelSetup] Settings gear + panel added to MainMenu and wired.");
    }

    private static Button CreateGearButton(Transform parent)
    {
        GameObject btnObj = EditorUiFactory.CreateButton(parent, "SettingsButton", "⚙",
            72f, 72f, labelFontSize: 36, addLayoutElement: false);
        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(72f, 72f); // T715: comfortably above 44pt touch minimum
        return btnObj.GetComponent<Button>();
    }

    private static (GameObject, SettingsUI) CreatePanel(Transform parent)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "SettingsPanel");

        GameObject box = EditorUiFactory.CreateCenteredContainer(panel.transform, "SettingsBox",
            680f, 760f);
        var layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 24, 24);
        layout.spacing = 14f;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        EditorUiFactory.CreateText(box.transform, "Title", "Settings", 34,
            FontStyles.Bold, LabelColor, TextAlignmentOptions.Center);

        var settings = panel.AddComponent<SettingsUI>();
        var so = new SerializedObject(settings);
        so.FindProperty("settingsPanel").objectReferenceValue = panel;

        WireSlider(so, box.transform, "Master Volume", "masterVolumeSlider", "masterVolumeText", 1f);
        WireSlider(so, box.transform, "Music Volume", "musicVolumeSlider", "musicVolumeText", 0.7f);
        WireSlider(so, box.transform, "SFX Volume", "sfxVolumeSlider", "sfxVolumeText", 0.8f);
        WireToggle(so, box.transform, "Combat VFX", "vfxToggle", true);
        WireToggle(so, box.transform, "Auto End Turn", "autoEndTurnToggle", false);
        WireSlider(so, box.transform, "Combat Speed", "combatSpeedSlider", "combatSpeedText", 1f, 0.5f, 2f);

        // Quality dropdown (row built like the sliders, control from the shared factory).
        GameObject qRow = EditorUiFactory.CreateHorizontalRow(box.transform, "QualityRow", 12f);
        EditorUiFactory.CreateText(qRow.transform, "Label", "Quality", 22,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Left);
        GameObject dd = EditorUiFactory.CreateDropdown(qRow.transform, "QualityDropdown",
            240f, 48f, "Medium", 18);
        so.FindProperty("qualityDropdown").objectReferenceValue = dd.GetComponent<TMP_Dropdown>();

        // Fullscreen toggle is desktop-only; SettingsUI.Start hides this row on mobile.
        WireToggle(so, box.transform, "Fullscreen", "fullscreenToggle", true);

        GameObject closeObj = EditorUiFactory.CreateButton(box.transform, "CloseButton", "Close",
            220f, 56f, labelFontSize: 22);
        so.FindProperty("closeButton").objectReferenceValue = closeObj.GetComponent<Button>();

        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        return (panel, settings);
    }

    private static void WireSlider(SerializedObject so, Transform parent, string label,
        string sliderField, string textField, float defaultValue, float min = 0f, float max = 1f)
    {
        GameObject row = EditorUiFactory.CreateHorizontalRow(parent, label.Replace(" ", "") + "Row", 12f);
        EditorUiFactory.CreateText(row.transform, "Label", label, 22,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Left);

        GameObject sliderObj = DefaultControls.CreateSlider(UiResources());
        sliderObj.name = label.Replace(" ", "") + "Slider";
        sliderObj.transform.SetParent(row.transform, false);
        var le = sliderObj.AddComponent<LayoutElement>();
        le.minWidth = 260f; le.minHeight = 44f; // touch target
        var slider = sliderObj.GetComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = defaultValue;

        GameObject valueText = EditorUiFactory.CreateText(row.transform, "Value",
            Mathf.RoundToInt(defaultValue * 100f) + "%", 20,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Right);

        so.FindProperty(sliderField).objectReferenceValue = slider;
        so.FindProperty(textField).objectReferenceValue = valueText.GetComponent<TMP_Text>();
    }

    private static void WireToggle(SerializedObject so, Transform parent, string label,
        string toggleField, bool defaultValue)
    {
        GameObject row = EditorUiFactory.CreateHorizontalRow(parent, label.Replace(" ", "") + "Row", 12f);
        EditorUiFactory.CreateText(row.transform, "Label", label, 22,
            FontStyles.Normal, LabelColor, TextAlignmentOptions.Left);

        GameObject toggleObj = DefaultControls.CreateToggle(UiResources());
        toggleObj.name = label.Replace(" ", "") + "Toggle";
        toggleObj.transform.SetParent(row.transform, false);
        var le = toggleObj.AddComponent<LayoutElement>();
        le.minWidth = 56f; le.minHeight = 44f;
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
