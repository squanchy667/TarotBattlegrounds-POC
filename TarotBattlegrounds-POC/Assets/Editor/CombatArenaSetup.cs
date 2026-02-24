#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.Combat.Animator;

/// <summary>
/// UX16: Editor script to set up Combat Arena Visual elements in the Game scene.
/// Creates divider line, team labels, slot indicators, attack trail, and arena background.
/// Run via menu: Tools/Game/Setup Combat Arena
/// </summary>
public class CombatArenaSetup : EditorWindow
{
    private const int SLOT_COUNT = 7;

    [MenuItem("Tools/Game/Setup Combat Arena")]
    public static void SetupCombatArena()
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

        if (!EditorUtility.DisplayDialog("Setup Combat Arena",
            "This will create:\n" +
            "  - Arena dark background overlay\n" +
            "  - Center divider line with glow\n" +
            "  - Team labels (attacker/defender)\n" +
            "  - Slot position indicators (1-7)\n" +
            "  - Attack trail line\n" +
            "  - CombatArenaVisual component wired to CombatAnimator\n\n" +
            "Continue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas().GetComponent<Canvas>();

        RemoveOld(canvas.transform);

        // Create the arena visual elements
        GameObject arenaRoot = CreateArenaUI(canvas.transform);

        // Wire CombatArenaVisual to CombatAnimator
        CombatAnimator combatAnimator = Object.FindObjectOfType<CombatAnimator>();
        if (combatAnimator != null)
        {
            Undo.RegisterCompleteObjectUndo(combatAnimator, "Wire CombatArenaVisual");
            SerializedObject so = new SerializedObject(combatAnimator);
            SerializedProperty prop = so.FindProperty("arenaVisual");
            if (prop != null)
            {
                prop.objectReferenceValue = arenaRoot.GetComponent<CombatArenaVisual>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(combatAnimator);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Combat Arena Setup Complete",
            "Created Combat Arena visual elements.\n" +
            "Save the scene to keep changes.", "OK");
    }

    private static void RemoveOld(Transform canvasTransform)
    {
        string[] oldNames = { "CombatArenaRoot" };
        foreach (string name in oldNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
        foreach (var existing in Object.FindObjectsOfType<CombatArenaVisual>())
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    private static GameObject CreateArenaUI(Transform canvasTransform)
    {
        // === Root object for CombatArenaVisual component ===
        GameObject root = new GameObject("CombatArenaRoot");
        root.transform.SetParent(canvasTransform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CombatArenaVisual arenaVisual = root.AddComponent<CombatArenaVisual>();

        TMP_FontAsset font = FindFont();

        // === 1. Arena Background (dark overlay) ===
        GameObject bgObj = new GameObject("ArenaBackground");
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.03f, 0.02f, 0.06f, 0.9f);
        bgImage.raycastTarget = false;
        bgObj.SetActive(false); // Start hidden

        // === 2. Attacker Team Label (top area) ===
        GameObject attackerLabelBgObj = new GameObject("AttackerLabelBg");
        attackerLabelBgObj.transform.SetParent(root.transform, false);
        RectTransform attackerLabelBgRect = attackerLabelBgObj.AddComponent<RectTransform>();
        attackerLabelBgRect.anchorMin = new Vector2(0f, 0.82f);
        attackerLabelBgRect.anchorMax = new Vector2(1f, 0.88f);
        attackerLabelBgRect.offsetMin = Vector2.zero;
        attackerLabelBgRect.offsetMax = Vector2.zero;

        Image attackerBgImage = attackerLabelBgObj.AddComponent<Image>();
        attackerBgImage.color = new Color(0.15f, 0.35f, 0.65f, 0.5f); // Blue tint for attacker
        attackerBgImage.raycastTarget = false;
        attackerLabelBgObj.SetActive(false);

        GameObject attackerLabelObj = new GameObject("AttackerLabel");
        attackerLabelObj.transform.SetParent(attackerLabelBgObj.transform, false);
        RectTransform attackerLabelRect = attackerLabelObj.AddComponent<RectTransform>();
        attackerLabelRect.anchorMin = Vector2.zero;
        attackerLabelRect.anchorMax = Vector2.one;
        attackerLabelRect.offsetMin = new Vector2(10f, 0f);
        attackerLabelRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI attackerTmp = attackerLabelObj.AddComponent<TextMeshProUGUI>();
        attackerTmp.text = "YOUR BOARD";
        attackerTmp.fontSize = 16;
        attackerTmp.fontStyle = FontStyles.Bold;
        attackerTmp.color = new Color(0.7f, 0.85f, 1f);
        attackerTmp.alignment = TextAlignmentOptions.Center;
        attackerTmp.enableWordWrapping = false;
        attackerTmp.raycastTarget = false;
        if (font != null) attackerTmp.font = font;
        attackerLabelObj.SetActive(false);

        // === 3. Defender Team Label (bottom area) ===
        GameObject defenderLabelBgObj = new GameObject("DefenderLabelBg");
        defenderLabelBgObj.transform.SetParent(root.transform, false);
        RectTransform defenderLabelBgRect = defenderLabelBgObj.AddComponent<RectTransform>();
        defenderLabelBgRect.anchorMin = new Vector2(0f, 0.12f);
        defenderLabelBgRect.anchorMax = new Vector2(1f, 0.18f);
        defenderLabelBgRect.offsetMin = Vector2.zero;
        defenderLabelBgRect.offsetMax = Vector2.zero;

        Image defenderBgImage = defenderLabelBgObj.AddComponent<Image>();
        defenderBgImage.color = new Color(0.65f, 0.2f, 0.2f, 0.5f); // Red tint for defender
        defenderBgImage.raycastTarget = false;
        defenderLabelBgObj.SetActive(false);

        GameObject defenderLabelObj = new GameObject("DefenderLabel");
        defenderLabelObj.transform.SetParent(defenderLabelBgObj.transform, false);
        RectTransform defenderLabelRect = defenderLabelObj.AddComponent<RectTransform>();
        defenderLabelRect.anchorMin = Vector2.zero;
        defenderLabelRect.anchorMax = Vector2.one;
        defenderLabelRect.offsetMin = new Vector2(10f, 0f);
        defenderLabelRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI defenderTmp = defenderLabelObj.AddComponent<TextMeshProUGUI>();
        defenderTmp.text = "OPPONENT";
        defenderTmp.fontSize = 16;
        defenderTmp.fontStyle = FontStyles.Bold;
        defenderTmp.color = new Color(1f, 0.7f, 0.7f);
        defenderTmp.alignment = TextAlignmentOptions.Center;
        defenderTmp.enableWordWrapping = false;
        defenderTmp.raycastTarget = false;
        if (font != null) defenderTmp.font = font;
        defenderLabelObj.SetActive(false);

        // === 4. Center Divider Line ===
        GameObject dividerObj = new GameObject("DividerLine");
        dividerObj.transform.SetParent(root.transform, false);
        RectTransform dividerRect = dividerObj.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.05f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.95f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(0, 2f); // 2px height, full width via anchors
        dividerRect.anchoredPosition = Vector2.zero;

        Image dividerImage = dividerObj.AddComponent<Image>();
        dividerImage.color = new Color(0.55f, 0.3f, 0.75f, 0.6f);
        dividerImage.raycastTarget = false;
        dividerObj.SetActive(false);

        // === 5. Attacker Slot Indicators (above divider) ===
        GameObject attackerSlotsContainer = new GameObject("AttackerSlots");
        attackerSlotsContainer.transform.SetParent(root.transform, false);
        RectTransform attackerSlotsRect = attackerSlotsContainer.AddComponent<RectTransform>();
        attackerSlotsRect.anchorMin = new Vector2(0.1f, 0.52f);
        attackerSlotsRect.anchorMax = new Vector2(0.9f, 0.56f);
        attackerSlotsRect.offsetMin = Vector2.zero;
        attackerSlotsRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup attackerLayout = attackerSlotsContainer.AddComponent<HorizontalLayoutGroup>();
        attackerLayout.childAlignment = TextAnchor.MiddleCenter;
        attackerLayout.spacing = 0;
        attackerLayout.childForceExpandWidth = true;
        attackerLayout.childForceExpandHeight = true;
        attackerLayout.childControlWidth = true;
        attackerLayout.childControlHeight = true;

        TextMeshProUGUI[] attackerSlotTexts = new TextMeshProUGUI[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i + 1}");
            slotObj.transform.SetParent(attackerSlotsContainer.transform, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();

            TextMeshProUGUI slotTmp = slotObj.AddComponent<TextMeshProUGUI>();
            slotTmp.text = (i + 1).ToString();
            slotTmp.fontSize = 10;
            slotTmp.color = new Color(1f, 1f, 1f, 0.25f); // Subtle
            slotTmp.alignment = TextAlignmentOptions.Center;
            slotTmp.enableWordWrapping = false;
            slotTmp.raycastTarget = false;
            if (font != null) slotTmp.font = font;
            attackerSlotTexts[i] = slotTmp;
            slotObj.SetActive(false);
        }

        // === 6. Defender Slot Indicators (below divider) ===
        GameObject defenderSlotsContainer = new GameObject("DefenderSlots");
        defenderSlotsContainer.transform.SetParent(root.transform, false);
        RectTransform defenderSlotsRect = defenderSlotsContainer.AddComponent<RectTransform>();
        defenderSlotsRect.anchorMin = new Vector2(0.1f, 0.44f);
        defenderSlotsRect.anchorMax = new Vector2(0.9f, 0.48f);
        defenderSlotsRect.offsetMin = Vector2.zero;
        defenderSlotsRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup defenderLayout = defenderSlotsContainer.AddComponent<HorizontalLayoutGroup>();
        defenderLayout.childAlignment = TextAnchor.MiddleCenter;
        defenderLayout.spacing = 0;
        defenderLayout.childForceExpandWidth = true;
        defenderLayout.childForceExpandHeight = true;
        defenderLayout.childControlWidth = true;
        defenderLayout.childControlHeight = true;

        TextMeshProUGUI[] defenderSlotTexts = new TextMeshProUGUI[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            GameObject slotObj = new GameObject($"Slot_{i + 1}");
            slotObj.transform.SetParent(defenderSlotsContainer.transform, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();

            TextMeshProUGUI slotTmp = slotObj.AddComponent<TextMeshProUGUI>();
            slotTmp.text = (i + 1).ToString();
            slotTmp.fontSize = 10;
            slotTmp.color = new Color(1f, 1f, 1f, 0.25f); // Subtle
            slotTmp.alignment = TextAlignmentOptions.Center;
            slotTmp.enableWordWrapping = false;
            slotTmp.raycastTarget = false;
            if (font != null) slotTmp.font = font;
            defenderSlotTexts[i] = slotTmp;
            slotObj.SetActive(false);
        }

        // === 7. Attack Trail Line ===
        GameObject trailObj = new GameObject("AttackTrailLine");
        trailObj.transform.SetParent(root.transform, false);
        RectTransform trailRect = trailObj.AddComponent<RectTransform>();
        trailRect.pivot = new Vector2(0.5f, 0.5f);
        trailRect.sizeDelta = new Vector2(100f, 3f); // Will be resized at runtime
        trailRect.anchoredPosition = Vector2.zero;

        Image trailImage = trailObj.AddComponent<Image>();
        trailImage.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        trailImage.raycastTarget = false;
        trailObj.SetActive(false);

        // === Wire serialized fields on CombatArenaVisual ===
        SerializedObject so = new SerializedObject(arenaVisual);

        // Divider
        so.FindProperty("dividerLine").objectReferenceValue = dividerImage;

        // Team labels
        so.FindProperty("attackerLabel").objectReferenceValue = attackerTmp;
        so.FindProperty("defenderLabel").objectReferenceValue = defenderTmp;
        so.FindProperty("attackerLabelBg").objectReferenceValue = attackerBgImage;
        so.FindProperty("defenderLabelBg").objectReferenceValue = defenderBgImage;

        // Slot indicators
        SerializedProperty attackerSlotsProp = so.FindProperty("attackerSlotNumbers");
        if (attackerSlotsProp != null)
        {
            attackerSlotsProp.arraySize = SLOT_COUNT;
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                attackerSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = attackerSlotTexts[i];
            }
        }

        SerializedProperty defenderSlotsProp = so.FindProperty("defenderSlotNumbers");
        if (defenderSlotsProp != null)
        {
            defenderSlotsProp.arraySize = SLOT_COUNT;
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                defenderSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = defenderSlotTexts[i];
            }
        }

        // Attack trail
        so.FindProperty("attackTrailLine").objectReferenceValue = trailImage;

        // Arena background
        so.FindProperty("arenaBackground").objectReferenceValue = bgImage;

        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(root, "Create CombatArenaRoot");

        return root;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return canvasObj;
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
