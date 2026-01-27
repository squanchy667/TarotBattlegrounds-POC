#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class TooltipPrefabGenerator : EditorWindow
{
    [MenuItem("Tools/Tarot BG/Generate Tooltip UI")]
    public static void GenerateTooltipUI()
    {
        // Find the Canvas in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in scene. Please create a Canvas first.", "OK");
            return;
        }

        // Check if tooltip already exists
        CardTooltipUI existingTooltip = FindObjectOfType<CardTooltipUI>();
        if (existingTooltip != null)
        {
            if (!EditorUtility.DisplayDialog("Tooltip Exists",
                "A CardTooltipUI already exists in the scene. Delete it and create a new one?",
                "Yes, Replace", "Cancel"))
            {
                return;
            }
            DestroyImmediate(existingTooltip.gameObject);
        }

        // Create the tooltip structure
        GameObject tooltipRoot = CreateTooltipStructure(canvas.transform);

        // Select the created object
        Selection.activeGameObject = tooltipRoot;

        Debug.Log("[TooltipPrefabGenerator] Tooltip UI created successfully!");
        EditorUtility.DisplayDialog("Success", "Tooltip UI created! Check the CardTooltipUI component and assign any missing references.", "OK");
    }

    private static GameObject CreateTooltipStructure(Transform canvasTransform)
    {
        // Root object with CardTooltipUI
        GameObject root = new GameObject("CardTooltipUI");
        root.transform.SetParent(canvasTransform, false);
        CardTooltipUI tooltipScript = root.AddComponent<CardTooltipUI>();

        // Tooltip Panel (the actual visible panel)
        GameObject panel = CreatePanel(root.transform);

        // Get references via SerializedObject
        SerializedObject so = new SerializedObject(tooltipScript);
        so.FindProperty("tooltipPanel").objectReferenceValue = panel;
        so.FindProperty("tooltipRect").objectReferenceValue = panel.GetComponent<RectTransform>();

        // Header Section
        GameObject header = CreateHeader(panel.transform, so);

        // Divider 1
        CreateDivider(panel.transform, "Divider1");

        // Stats Row
        CreateStatsRow(panel.transform, so);

        // Divider 2
        CreateDivider(panel.transform, "Divider2");

        // Ability Section
        CreateAbilitySection(panel.transform, so);

        // Legacy Effect Section
        CreateLegacySection(panel.transform, so);

        // Apply serialized changes
        so.ApplyModifiedProperties();

        return root;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("TooltipPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.pivot = new Vector2(0, 1); // Top-left pivot for positioning
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);

        // Background image
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f); // Dark background

        // Outline for border effect
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        outline.effectDistance = new Vector2(2, -2);

        // Vertical layout
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 12);
        vlg.spacing = 6;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Content size fitter
        ContentSizeFitter csf = panel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Set min width via layout element
        LayoutElement le = panel.AddComponent<LayoutElement>();
        le.minWidth = 260;
        le.preferredWidth = 280;

        return panel;
    }

    private static GameObject CreateHeader(Transform parent, SerializedObject so)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);

        RectTransform rect = header.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Card Name
        GameObject nameObj = CreateTextObject(header.transform, "CardNameText", "Card Name", 18, FontStyles.Bold, Color.white);
        LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;
        so.FindProperty("cardNameText").objectReferenceValue = nameObj.GetComponent<TMP_Text>();

        // Tier
        GameObject tierObj = CreateTextObject(header.transform, "TierText", "Tier 1", 13, FontStyles.Normal, new Color(1f, 0.84f, 0f)); // Gold
        so.FindProperty("tierText").objectReferenceValue = tierObj.GetComponent<TMP_Text>();

        return header;
    }

    private static void CreateStatsRow(Transform parent, SerializedObject so)
    {
        GameObject row = new GameObject("StatsRow");
        row.transform.SetParent(parent, false);

        RectTransform rect = row.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Stats
        GameObject statsObj = CreateTextObject(row.transform, "StatsText", "ATK: 3  |  HP: 4", 14, FontStyles.Normal, Color.white);
        so.FindProperty("statsText").objectReferenceValue = statsObj.GetComponent<TMP_Text>();

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(row.transform, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        // Tribes
        GameObject tribesObj = CreateTextObject(row.transform, "TribesText", "Pentacles", 13, FontStyles.Italic, new Color(0.31f, 0.8f, 0.77f)); // Teal
        so.FindProperty("tribesText").objectReferenceValue = tribesObj.GetComponent<TMP_Text>();
    }

    private static void CreateDivider(Transform parent, string name)
    {
        GameObject divider = new GameObject(name);
        divider.transform.SetParent(parent, false);

        RectTransform rect = divider.AddComponent<RectTransform>();

        Image img = divider.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.45f, 1f);

        LayoutElement le = divider.AddComponent<LayoutElement>();
        le.minHeight = 1;
        le.preferredHeight = 1;
        le.flexibleWidth = 1;
    }

    private static void CreateAbilitySection(Transform parent, SerializedObject so)
    {
        GameObject section = new GameObject("AbilitySection");
        section.transform.SetParent(parent, false);

        RectTransform rect = section.AddComponent<RectTransform>();

        VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Ability Trigger (e.g., "BATTLECRY")
        GameObject triggerObj = CreateTextObject(section.transform, "AbilityTriggerText", "<color=#FFD700>BATTLECRY</color>", 14, FontStyles.Bold, Color.white);
        so.FindProperty("abilityTriggerText").objectReferenceValue = triggerObj.GetComponent<TMP_Text>();

        // Ability Effect
        GameObject effectObj = CreateTextObject(section.transform, "AbilityEffectText", "Give adjacent minions +2 Attack", 13, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        so.FindProperty("abilityEffectText").objectReferenceValue = effectObj.GetComponent<TMP_Text>();

        // Ability Description (flavor text)
        GameObject descObj = CreateTextObject(section.transform, "AbilityDescriptionText", "\"The dawn brings strength.\"", 12, FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f));
        so.FindProperty("abilityDescriptionText").objectReferenceValue = descObj.GetComponent<TMP_Text>();

        so.FindProperty("abilitySection").objectReferenceValue = section;
    }

    private static void CreateLegacySection(Transform parent, SerializedObject so)
    {
        GameObject section = new GameObject("LegacyEffectSection");
        section.transform.SetParent(parent, false);

        RectTransform rect = section.AddComponent<RectTransform>();

        VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Legacy Effect Text
        GameObject effectObj = CreateTextObject(section.transform, "LegacyEffectText", "<color=#4169E1>GUARDIAN</color>\nMust be attacked first", 13, FontStyles.Normal, Color.white);
        so.FindProperty("legacyEffectText").objectReferenceValue = effectObj.GetComponent<TMP_Text>();

        so.FindProperty("legacyEffectSection").objectReferenceValue = section;
    }

    private static GameObject CreateTextObject(Transform parent, string name, string defaultText, int fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;

        // Try to find a font asset
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            // Try default TMP font
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
        if (font != null)
            tmp.font = font;

        return obj;
    }
}
#endif
