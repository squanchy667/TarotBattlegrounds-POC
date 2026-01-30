#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor script to scale up all game UI elements for better readability.
/// Run via menu: Tools/Game/Scale Up UI
///
/// Affects:
/// - CardDisplay prefab: bigger cards with larger fonts
/// - EmptySlot prefab: matches card size
/// - Scene buttons: bigger with larger text
/// - Scene text: larger fonts for phase, timer, stats, panel titles
/// - Layout groups: increased spacing for bigger cards
/// </summary>
public class UIScaleSetup : EditorWindow
{
    // ===================== SIZE CONSTANTS =====================

    // Card dimensions
    const float CARD_WIDTH = 160f;
    const float CARD_HEIGHT = 210f;

    // Card font sizes
    const float CARD_NAME_SIZE = 16f;
    const float CARD_STATS_SIZE = 22f;   // ATK / HP — big and bold
    const float CARD_TIER_SIZE = 14f;
    const float CARD_TRIBE_SIZE = 13f;
    const float CARD_COST_SIZE = 18f;

    // Action button dimensions
    const float BUTTON_MIN_WIDTH = 140f;
    const float BUTTON_PREF_WIDTH = 160f;
    const float BUTTON_HEIGHT = 55f;
    const float BUTTON_TEXT_SIZE = 20f;

    // Scene HUD font sizes
    const float PHASE_TEXT_SIZE = 28f;
    const float TIMER_TEXT_SIZE = 34f;
    const float TURN_TEXT_SIZE = 24f;
    const float PLAYER_NAME_SIZE = 24f;
    const float COINS_TEXT_SIZE = 22f;
    const float TIER_TEXT_SIZE = 20f;
    const float UPGRADE_TEXT_SIZE = 20f;
    const float HEALTH_TEXT_SIZE = 22f;

    // Panel title / count font sizes
    const float PANEL_TITLE_SIZE = 22f;
    const float PANEL_COUNT_SIZE = 16f;

    // Layout spacing
    const float CARD_CONTAINER_SPACING = 12f;

    // ===================== ENTRY POINT =====================

    [MenuItem("Tools/Game/Scale Up UI (Bigger Cards && Buttons)")]
    public static void ScaleUpUI()
    {
        int changes = 0;

        changes += ScaleCardDisplayPrefab();
        changes += ScaleEmptySlotPrefab();
        changes += ScaleSceneButtons();
        changes += ScaleGameUIManagerText();
        changes += ScalePanelText();
        changes += AdjustCardContainerSpacing();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        string msg = $"Applied {changes} UI scale changes.\n\n" +
            $"Cards: {CARD_WIDTH}x{CARD_HEIGHT}, fonts up to {CARD_STATS_SIZE}pt\n" +
            $"Buttons: {BUTTON_PREF_WIDTH}x{BUTTON_HEIGHT}, text {BUTTON_TEXT_SIZE}pt\n" +
            $"HUD text: {PHASE_TEXT_SIZE}-{TIMER_TEXT_SIZE}pt\n\n" +
            "Save the scene to keep changes.";

        Debug.Log($"[UIScaleSetup] {msg}");
        EditorUtility.DisplayDialog("UI Scaled Up", msg, "OK");
    }

    // ===================== CARD DISPLAY PREFAB =====================

    private static int ScaleCardDisplayPrefab()
    {
        string path = "Assets/Preferbs/UI/CardDisplay.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[UIScaleSetup] CardDisplay prefab not found at {path}");
            return 0;
        }

        // Load prefab contents for editing
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        int changes = 0;

        // Set card size
        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
            changes++;
        }

        // Add or update LayoutElement for minimum sizing in layout groups
        LayoutElement le = root.GetComponent<LayoutElement>();
        if (le == null) le = root.AddComponent<LayoutElement>();
        le.minWidth = CARD_WIDTH;
        le.minHeight = CARD_HEIGHT;
        le.preferredWidth = CARD_WIDTH;
        le.preferredHeight = CARD_HEIGHT;
        changes++;

        // Scale all text in the prefab
        changes += ScaleCardText(root);

        // Save and unload
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[UIScaleSetup] CardDisplay prefab scaled: {CARD_WIDTH}x{CARD_HEIGHT}, {changes} changes");
        return changes;
    }

    private static int ScaleCardText(GameObject cardRoot)
    {
        int changes = 0;

        // Get the CardDisplayUI to find named text fields
        CardDisplayUI cardUI = cardRoot.GetComponent<CardDisplayUI>();
        if (cardUI != null)
        {
            SerializedObject so = new SerializedObject(cardUI);

            changes += SetTextFontSize(so, "cardNameText", CARD_NAME_SIZE);
            changes += SetTextFontSize(so, "attackText", CARD_STATS_SIZE);
            changes += SetTextFontSize(so, "healthText", CARD_STATS_SIZE);
            changes += SetTextFontSize(so, "tierText", CARD_TIER_SIZE);
            changes += SetTextFontSize(so, "tribeText", CARD_TRIBE_SIZE);
            changes += SetTextFontSize(so, "costText", CARD_COST_SIZE);
        }
        else
        {
            // Fallback: scale all TMP_Text in the prefab
            foreach (TMP_Text text in cardRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.fontSize < 14f)
                {
                    text.fontSize = 16f;
                    changes++;
                }
            }
        }

        return changes;
    }

    private static int SetTextFontSize(SerializedObject so, string fieldName, float fontSize)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue == null) return 0;

        TMP_Text text = prop.objectReferenceValue as TMP_Text;
        if (text == null) return 0;

        text.fontSize = fontSize;
        EditorUtility.SetDirty(text);
        return 1;
    }

    // ===================== EMPTY SLOT PREFAB =====================

    private static int ScaleEmptySlotPrefab()
    {
        string path = "Assets/Preferbs/UI/EmptySlot.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[UIScaleSetup] EmptySlot prefab not found at {path}");
            return 0;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        int changes = 0;

        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
            changes++;
        }

        LayoutElement le = root.GetComponent<LayoutElement>();
        if (le == null) le = root.AddComponent<LayoutElement>();
        le.minWidth = CARD_WIDTH;
        le.minHeight = CARD_HEIGHT;
        le.preferredWidth = CARD_WIDTH;
        le.preferredHeight = CARD_HEIGHT;
        changes++;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[UIScaleSetup] EmptySlot prefab scaled: {CARD_WIDTH}x{CARD_HEIGHT}");
        return changes;
    }

    // ===================== SCENE BUTTONS =====================

    private static int ScaleSceneButtons()
    {
        GameUIManager manager = FindObjectOfType<GameUIManager>();
        if (manager == null)
        {
            Debug.LogWarning("[UIScaleSetup] GameUIManager not found in scene.");
            return 0;
        }

        SerializedObject so = new SerializedObject(manager);
        int changes = 0;

        // Scale each action button
        string[] buttonFields = {
            "buyButton", "sellButton", "playCardButton",
            "refreshButton", "upgradeButton", "switchPlayerButton",
            "endTurnButton", "freezeShopButton"
        };

        foreach (string field in buttonFields)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null || prop.objectReferenceValue == null) continue;

            Button btn = prop.objectReferenceValue as Button;
            if (btn == null) continue;

            changes += ScaleButton(btn.gameObject);
        }

        return changes;
    }

    private static int ScaleButton(GameObject btnObj)
    {
        int changes = 0;

        // Scale the button's RectTransform
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector2 size = rect.sizeDelta;
            // Only increase, never shrink
            if (size.y < BUTTON_HEIGHT)
            {
                rect.sizeDelta = new Vector2(Mathf.Max(size.x, BUTTON_MIN_WIDTH), BUTTON_HEIGHT);
                Undo.RecordObject(rect, "Scale button");
                changes++;
            }
        }

        // Scale the LayoutElement
        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        if (le != null)
        {
            Undo.RecordObject(le, "Scale button layout");
            le.minWidth = Mathf.Max(le.minWidth, BUTTON_MIN_WIDTH);
            le.preferredWidth = Mathf.Max(le.preferredWidth, BUTTON_PREF_WIDTH);
            if (le.minHeight > 0) le.minHeight = Mathf.Max(le.minHeight, BUTTON_HEIGHT);
            if (le.preferredHeight > 0) le.preferredHeight = Mathf.Max(le.preferredHeight, BUTTON_HEIGHT);
            changes++;
        }

        // Scale the button text
        TMP_Text text = btnObj.GetComponentInChildren<TMP_Text>(true);
        if (text != null && text.fontSize < BUTTON_TEXT_SIZE)
        {
            Undo.RecordObject(text, "Scale button text");
            text.fontSize = BUTTON_TEXT_SIZE;
            changes++;
        }

        return changes;
    }

    // ===================== GAME UI MANAGER TEXT =====================

    private static int ScaleGameUIManagerText()
    {
        GameUIManager manager = FindObjectOfType<GameUIManager>();
        if (manager == null) return 0;

        SerializedObject so = new SerializedObject(manager);
        int changes = 0;

        // Phase display
        changes += ScaleSerializedText(so, "phaseText", PHASE_TEXT_SIZE);
        changes += ScaleSerializedText(so, "timerText", TIMER_TEXT_SIZE);
        changes += ScaleSerializedText(so, "turnText", TURN_TEXT_SIZE);

        // Player stats
        changes += ScaleSerializedText(so, "playerNameText", PLAYER_NAME_SIZE);
        changes += ScaleSerializedText(so, "coinsText", COINS_TEXT_SIZE);
        changes += ScaleSerializedText(so, "tierText", TIER_TEXT_SIZE);
        changes += ScaleSerializedText(so, "upgradeCostText", UPGRADE_TEXT_SIZE);
        changes += ScaleSerializedText(so, "healthText", HEALTH_TEXT_SIZE);

        if (changes > 0)
            Debug.Log($"[UIScaleSetup] Scaled {changes} GameUIManager text fields");

        return changes;
    }

    private static int ScaleSerializedText(SerializedObject so, string fieldName, float fontSize)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null || prop.objectReferenceValue == null) return 0;

        TMP_Text text = prop.objectReferenceValue as TMP_Text;
        if (text == null) return 0;

        if (text.fontSize < fontSize)
        {
            Undo.RecordObject(text, $"Scale {fieldName}");
            text.fontSize = fontSize;
            return 1;
        }
        return 0;
    }

    // ===================== PANEL TITLES & COUNTS =====================

    private static int ScalePanelText()
    {
        int changes = 0;

        // ShopUI
        ShopUI shopUI = FindObjectOfType<ShopUI>();
        if (shopUI != null)
        {
            SerializedObject so = new SerializedObject(shopUI);
            changes += ScaleSerializedText(so, "shopTitleText", PANEL_TITLE_SIZE);
            changes += ScaleSerializedText(so, "shopTierText", PANEL_COUNT_SIZE);
            changes += ScaleSerializedText(so, "shopCountText", PANEL_COUNT_SIZE);
        }

        // HandUI
        HandUI handUI = FindObjectOfType<HandUI>();
        if (handUI != null)
        {
            SerializedObject so = new SerializedObject(handUI);
            changes += ScaleSerializedText(so, "handTitleText", PANEL_TITLE_SIZE);
            changes += ScaleSerializedText(so, "handCountText", PANEL_COUNT_SIZE);
        }

        // BoardUI
        BoardUI boardUI = FindObjectOfType<BoardUI>();
        if (boardUI != null)
        {
            SerializedObject so = new SerializedObject(boardUI);
            changes += ScaleSerializedText(so, "boardTitleText", PANEL_TITLE_SIZE);
            changes += ScaleSerializedText(so, "boardCountText", PANEL_COUNT_SIZE);
        }

        if (changes > 0)
            Debug.Log($"[UIScaleSetup] Scaled {changes} panel title/count text fields");

        return changes;
    }

    // ===================== LAYOUT GROUP SPACING =====================

    private static int AdjustCardContainerSpacing()
    {
        int changes = 0;

        // Find card containers in ShopUI, HandUI, BoardUI and increase spacing
        changes += AdjustContainerSpacing<ShopUI>("shopSlotsContainer");
        changes += AdjustContainerSpacing<HandUI>("handSlotsContainer");
        changes += AdjustContainerSpacing<BoardUI>("boardSlotsContainer");

        if (changes > 0)
            Debug.Log($"[UIScaleSetup] Adjusted {changes} layout group spacings");

        return changes;
    }

    private static int AdjustContainerSpacing<T>(string containerFieldName) where T : MonoBehaviour
    {
        T component = FindObjectOfType<T>();
        if (component == null) return 0;

        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(containerFieldName);
        if (prop == null || prop.objectReferenceValue == null) return 0;

        Transform container = prop.objectReferenceValue as Transform;
        if (container == null) return 0;

        int changes = 0;

        // Check for HorizontalLayoutGroup
        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null && hlg.spacing < CARD_CONTAINER_SPACING)
        {
            Undo.RecordObject(hlg, "Adjust card spacing");
            hlg.spacing = CARD_CONTAINER_SPACING;
            changes++;
        }

        // Check for VerticalLayoutGroup
        VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg != null && vlg.spacing < CARD_CONTAINER_SPACING)
        {
            Undo.RecordObject(vlg, "Adjust card spacing");
            vlg.spacing = CARD_CONTAINER_SPACING;
            changes++;
        }

        // Check for GridLayoutGroup
        GridLayoutGroup glg = container.GetComponent<GridLayoutGroup>();
        if (glg != null)
        {
            Undo.RecordObject(glg, "Adjust grid cell size");
            if (glg.cellSize.x < CARD_WIDTH || glg.cellSize.y < CARD_HEIGHT)
            {
                glg.cellSize = new Vector2(
                    Mathf.Max(glg.cellSize.x, CARD_WIDTH),
                    Mathf.Max(glg.cellSize.y, CARD_HEIGHT));
                changes++;
            }
            if (glg.spacing.x < CARD_CONTAINER_SPACING)
            {
                glg.spacing = new Vector2(
                    Mathf.Max(glg.spacing.x, CARD_CONTAINER_SPACING),
                    Mathf.Max(glg.spacing.y, CARD_CONTAINER_SPACING));
                changes++;
            }
        }

        return changes;
    }
}
#endif
