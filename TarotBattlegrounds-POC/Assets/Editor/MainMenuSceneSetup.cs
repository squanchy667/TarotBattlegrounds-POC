#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script to set up the MainMenu scene with Solo/Multiplayer panel navigation.
/// Run via menu: Tools/Game/Setup Main Menu UI
/// </summary>
public class MainMenuSceneSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Main Menu UI")]
    public static void SetupMainMenuUI()
    {
        // Ensure we're in the MainMenu scene
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("MainMenu"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not MainMenu. Open MainMenu scene first?\n\n" +
                "(This will save the current scene.)",
                "Open MainMenu", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        if (!EditorUtility.DisplayDialog("Setup Main Menu UI",
            "This will create MainPanel (Solo/Multiplayer/Quit) and " +
            "SoloPanel (4/6/8 players + difficulty + Play/Back) in the MainMenu scene " +
            "and wire all references on MainMenuManager.\n\nContinue?",
            "Create", "Cancel"))
            return;

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = EditorUiFactory.CreateCanvas(withBackground: true);
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Find or create MainMenuManager
        MainMenuManager manager = Object.FindObjectOfType<MainMenuManager>();
        if (manager == null)
        {
            manager = canvas.gameObject.AddComponent<MainMenuManager>();
        }

        Undo.RegisterCompleteObjectUndo(manager, "Setup Main Menu UI");
        SerializedObject so = new SerializedObject(manager);

        // Remove old UI children that conflict
        RemoveOldPanels(canvas.transform);

        // Create panels
        GameObject mainPanel = CreateMainPanel(canvas.transform, so);
        GameObject soloPanel = CreateSoloPanel(canvas.transform, so);

        // Solo panel starts inactive
        soloPanel.SetActive(false);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[MainMenuSceneSetup] Main menu UI setup complete!");
        EditorUtility.DisplayDialog("Main Menu Setup Complete",
            "Created:\n" +
            "  - MainPanel (Solo / Multiplayer / Quit)\n" +
            "  - SoloPanel (4/6/8 players + Difficulty + Play/Back)\n\n" +
            "All references wired on MainMenuManager.\n" +
            "Save the scene to keep changes.", "OK");
    }

    // ===================== CLEANUP =====================

    private static void RemoveOldPanels(Transform canvasTransform)
    {
        string[] oldNames = { "MainPanel", "SoloPanel" };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[MainMenuSceneSetup] Removed old {name}");
            }
        }
    }

    // ===================== CANVAS =====================

    // ===================== MAIN PANEL =====================

    private static GameObject CreateMainPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "MainPanel");
        so.FindProperty("mainPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = EditorUiFactory.CreateCenteredContainer(panel.transform, "Content", 500, 450);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 25;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(50, 50, 40, 40);

        // Title
        EditorUiFactory.CreateText(content.transform, "TitleText", "Tarot Battlegrounds", 42,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Subtitle
        EditorUiFactory.CreateText(content.transform, "SubtitleText", "Auto Battler", 20,
            FontStyles.Italic, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(content.transform, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.minHeight = 20;

        // Solo button
        GameObject soloBtn = EditorUiFactory.CreateButton(content.transform, "SoloButton", "Solo", 300, 60, labelFontSize: 22);
        so.FindProperty("soloButton").objectReferenceValue = soloBtn.GetComponent<Button>();
        SetButtonColor(soloBtn, new Color(0.2f, 0.45f, 0.2f));

        // Multiplayer button
        GameObject mpBtn = EditorUiFactory.CreateButton(content.transform, "MultiplayerButton", "Multiplayer", 300, 60, labelFontSize: 22);
        so.FindProperty("multiplayerButton").objectReferenceValue = mpBtn.GetComponent<Button>();
        SetButtonColor(mpBtn, new Color(0.2f, 0.3f, 0.5f));

        // Quit button
        GameObject quitBtn = EditorUiFactory.CreateButton(content.transform, "QuitButton", "Quit", 300, 60, labelFontSize: 22);
        so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        SetButtonColor(quitBtn, new Color(0.45f, 0.2f, 0.2f));

        return panel;
    }

    // ===================== SOLO PANEL =====================

    private static GameObject CreateSoloPanel(Transform parent, SerializedObject so)
    {
        GameObject panel = EditorUiFactory.CreateFullscreenPanel(parent, "SoloPanel");
        so.FindProperty("soloPanel").objectReferenceValue = panel;

        // Centered content container
        GameObject content = EditorUiFactory.CreateCenteredContainer(panel.transform, "Content", 550, 500);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(40, 40, 30, 30);

        // Title
        EditorUiFactory.CreateText(content.transform, "SoloTitle", "Solo Game", 32,
            FontStyles.Bold, new Color(1f, 0.84f, 0f), TextAlignmentOptions.Center);

        // Player count label
        EditorUiFactory.CreateText(content.transform, "PlayerCountLabel", "Number of Players", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        // Player count buttons row
        GameObject countRow = EditorUiFactory.CreateHorizontalRow(content.transform, "PlayerCountRow", 20, minHeight: 60f);

        GameObject btn4 = EditorUiFactory.CreateButton(countRow.transform, "Players4Button", "4 Players", 150, 55, labelFontSize: 22);
        so.FindProperty("players4Button").objectReferenceValue = btn4.GetComponent<Button>();
        SetButtonColor(btn4, new Color(0.4f, 0.8f, 0.4f)); // Green = default selected

        GameObject btn6 = EditorUiFactory.CreateButton(countRow.transform, "Players6Button", "6 Players", 150, 55, labelFontSize: 22);
        so.FindProperty("players6Button").objectReferenceValue = btn6.GetComponent<Button>();

        GameObject btn8 = EditorUiFactory.CreateButton(countRow.transform, "Players8Button", "8 Players", 150, 55, labelFontSize: 22);
        so.FindProperty("players8Button").objectReferenceValue = btn8.GetComponent<Button>();

        // Difficulty label
        EditorUiFactory.CreateText(content.transform, "DifficultyLabel", "AI Difficulty", 20,
            FontStyles.Normal, Color.white, TextAlignmentOptions.Center);

        // Difficulty dropdown
        GameObject diffDropdown = EditorUiFactory.CreateDropdown(content.transform, "DifficultyDropdown", 250, 45);
        so.FindProperty("difficultyDropdown").objectReferenceValue =
            diffDropdown.GetComponent<TMP_Dropdown>();

        // Spacer
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(content.transform, false);
        spacer.AddComponent<RectTransform>();
        LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.minHeight = 10;

        // Bottom buttons row
        GameObject bottomRow = EditorUiFactory.CreateHorizontalRow(content.transform, "BottomRow", 30, minHeight: 60f);

        GameObject backBtn = EditorUiFactory.CreateButton(bottomRow.transform, "BackButton", "Back", 150, 55, labelFontSize: 22);
        so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        SetButtonColor(backBtn, new Color(0.45f, 0.2f, 0.2f));

        GameObject playBtn = EditorUiFactory.CreateButton(bottomRow.transform, "PlayButton", "Play", 200, 55, labelFontSize: 22);
        so.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();
        SetButtonColor(playBtn, new Color(0.2f, 0.45f, 0.2f));

        return panel;
    }

    // ===================== HELPER METHODS =====================

    private static void SetButtonColor(GameObject btnObj, Color color)
    {
        Image img = btnObj.GetComponent<Image>();
        if (img != null) img.color = color;

        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            colors.selectedColor = color * 1.1f;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            btn.colors = colors;
        }
    }

}
#endif
