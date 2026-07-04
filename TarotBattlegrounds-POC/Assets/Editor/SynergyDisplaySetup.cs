#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script to set up the Synergy Display Panel in the Game scene.
/// Creates a compact left-side panel showing tribe synergy progress.
/// Run via menu: Tools/Game/Setup Synergy Display
/// </summary>
public class SynergyDisplaySetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Synergy Display")]
    public static void SetupSynergyDisplay()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Game"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not Game. Open Game scene first?\n\n" +
                "(This will save the current scene.)",
                "Open Game", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }

        if (!EditorUtility.DisplayDialog("Setup Synergy Display",
            "This will create:\n" +
            "  - SynergyDisplayPanel on the LEFT side\n" +
            "  - 6 tribe rows with icons, pips, and counts\n" +
            "  - Semi-transparent background panel\n\n" +
            "And wire references on GameUIManager.\nContinue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();

        // Remove old synergy panel if it exists
        RemoveOld(canvas.transform);

        // Create the synergy display panel
        GameObject panelObj = CreateSynergyPanel(canvas.transform);

        // Wire to GameUIManager
        GameUIManager guiManager = Object.FindObjectOfType<GameUIManager>();
        if (guiManager != null)
        {
            Undo.RegisterCompleteObjectUndo(guiManager, "Wire SynergyDisplayPanel");
            SerializedObject so = new SerializedObject(guiManager);
            SerializedProperty prop = so.FindProperty("synergyDisplay");
            if (prop != null)
            {
                prop.objectReferenceValue = panelObj.GetComponent<SynergyDisplayPanel>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(guiManager);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Synergy Display Setup Complete",
            "Created synergy display panel on the left side.\n" +
            "Save the scene to keep changes.", "OK");
    }

    private static void RemoveOld(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("SynergyDisplayPanel");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        // Also remove any orphaned SynergyDisplayPanel components
        foreach (var panel in Object.FindObjectsOfType<SynergyDisplayPanel>())
            Undo.DestroyObjectImmediate(panel.gameObject);
    }

    private static GameObject CreateSynergyPanel(Transform canvasTransform)
    {
        // Root panel — left side of screen, vertically centered
        GameObject panelObj = new GameObject("SynergyDisplayPanel");
        panelObj.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();

        // Anchor to left side, vertically centered
        panelRect.anchorMin = new Vector2(0f, 0.3f);
        panelRect.anchorMax = new Vector2(0f, 0.7f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(8f, 0f);
        panelRect.sizeDelta = new Vector2(140f, 0f); // Width fixed, height from anchors

        // Semi-transparent background
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.05f, 0.14f, 0.75f);
        panelBg.raycastTarget = false;

        // Add StyledPanel if available for nicer look
        StyledPanel styledPanel = panelObj.AddComponent<StyledPanel>();

        // Wire StyledPanel fields via SerializedObject
        SerializedObject styledSO = new SerializedObject(styledPanel);
        SerializedProperty bgProp = styledSO.FindProperty("panelBackground");
        if (bgProp != null)
        {
            bgProp.objectReferenceValue = panelBg;
        }
        SerializedProperty headerProp = styledSO.FindProperty("showHeader");
        if (headerProp != null)
        {
            headerProp.boolValue = false;
        }
        SerializedProperty bgAlphaProp = styledSO.FindProperty("backgroundAlpha");
        if (bgAlphaProp != null)
        {
            bgAlphaProp.floatValue = 0.75f;
        }
        SerializedProperty cornerProp = styledSO.FindProperty("cornerRadius");
        if (cornerProp != null)
        {
            cornerProp.floatValue = 8f;
        }
        styledSO.ApplyModifiedProperties();

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(0f, 20f);

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Synergies";
        titleTMP.fontSize = 12;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.84f, 0f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.enableWordWrapping = false;
        titleTMP.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) titleTMP.font = font;

        // Row container with VerticalLayoutGroup
        GameObject rowContainer = new GameObject("RowContainer");
        rowContainer.transform.SetParent(panelObj.transform, false);
        RectTransform rowContainerRect = rowContainer.AddComponent<RectTransform>();
        rowContainerRect.anchorMin = new Vector2(0f, 0f);
        rowContainerRect.anchorMax = new Vector2(1f, 1f);
        rowContainerRect.offsetMin = new Vector2(4f, 4f);
        rowContainerRect.offsetMax = new Vector2(-4f, -24f); // Leave room for title

        VerticalLayoutGroup vlg = rowContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Add SynergyDisplayPanel component
        SynergyDisplayPanel synergyPanel = panelObj.AddComponent<SynergyDisplayPanel>();

        // Wire serialized fields via SerializedObject
        SerializedObject so = new SerializedObject(synergyPanel);
        so.FindProperty("panelRect").objectReferenceValue = panelRect;
        so.FindProperty("panelBackground").objectReferenceValue = panelBg;
        so.FindProperty("rowContainer").objectReferenceValue = rowContainer.transform;
        so.ApplyModifiedProperties();

        // Initialize the panel (creates 6 tribe rows procedurally)
        synergyPanel.Initialize();

        Undo.RegisterCreatedObjectUndo(panelObj, "Create SynergyDisplayPanel");

        return panelObj;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return font;
    }
}
#endif
