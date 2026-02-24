#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool that sets up panel backgrounds on ShopUI, HandUI, BoardUI, and GameUIManager panels.
/// Inserts background layers (PanelBG, PanelBorder, PanelHeader) as first children,
/// wires references to the StyledPanel component, and ensures Raycast Target = false.
/// </summary>
public class PanelBackgroundSetup : Editor
{
    [MenuItem("Tools/Game/Setup Panel Backgrounds")]
    public static void SetupPanelBackgrounds()
    {
        int panelCount = 0;

        // Find and setup ShopUI panel
        ShopUI shopUI = FindObjectOfType<ShopUI>();
        if (shopUI != null)
        {
            SetupPanelOnObject(shopUI.gameObject, "ShopUI");
            panelCount++;
        }
        else
        {
            Debug.LogWarning("[PanelBackgroundSetup] ShopUI not found in scene");
        }

        // Find and setup HandUI panel
        HandUI handUI = FindObjectOfType<HandUI>();
        if (handUI != null)
        {
            SetupPanelOnObject(handUI.gameObject, "HandUI");
            panelCount++;
        }
        else
        {
            Debug.LogWarning("[PanelBackgroundSetup] HandUI not found in scene");
        }

        // Find and setup BoardUI panel
        BoardUI boardUI = FindObjectOfType<BoardUI>();
        if (boardUI != null)
        {
            SetupPanelOnObject(boardUI.gameObject, "BoardUI");
            panelCount++;
        }
        else
        {
            Debug.LogWarning("[PanelBackgroundSetup] BoardUI not found in scene");
        }

        // Find and setup GameUIManager info panel
        GameUIManager gameUIManager = FindObjectOfType<GameUIManager>();
        if (gameUIManager != null)
        {
            SetupPanelOnObject(gameUIManager.gameObject, "GameUIManager");
            panelCount++;
        }
        else
        {
            Debug.LogWarning("[PanelBackgroundSetup] GameUIManager not found in scene");
        }

        if (panelCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[PanelBackgroundSetup] Successfully set up panel backgrounds on {panelCount} panels. Save the scene to persist changes.");
        }
        else
        {
            Debug.LogError("[PanelBackgroundSetup] No panels found. Make sure you have the Game scene open.");
        }
    }

    /// <summary>
    /// Set up panel background layers on a given GameObject.
    /// Creates PanelBG, PanelBorder, and PanelHeader as first children,
    /// adds StyledPanel component and wires all references.
    /// </summary>
    private static void SetupPanelOnObject(GameObject panelObj, string panelName)
    {
        // Check if StyledPanel already exists
        StyledPanel existingPanel = panelObj.GetComponent<StyledPanel>();
        if (existingPanel != null)
        {
            Debug.Log($"[PanelBackgroundSetup] {panelName} already has StyledPanel, skipping");
            return;
        }

        Undo.RegisterCompleteObjectUndo(panelObj, $"Setup Panel Background on {panelName}");

        // Ensure the panel has a RectTransform (it should, being UI)
        RectTransform parentRect = panelObj.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning($"[PanelBackgroundSetup] {panelName} has no RectTransform, skipping");
            return;
        }

        // Create PanelBG (background fill)
        GameObject bgObj = CreateUIChild(panelObj, "PanelBG");
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.raycastTarget = false;
        StretchToParent(bgObj.GetComponent<RectTransform>());

        // Create PanelBorder (border outline)
        GameObject borderObj = CreateUIChild(panelObj, "PanelBorder");
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.raycastTarget = false;
        StretchToParent(borderObj.GetComponent<RectTransform>());

        // Create PanelHeader (header bar at top)
        GameObject headerObj = CreateUIChild(panelObj, "PanelHeader");
        Image headerImage = headerObj.AddComponent<Image>();
        headerImage.raycastTarget = false;
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        // Anchor to top, stretch horizontally
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(4f, -32f); // left inset, bottom (32px tall)
        headerRect.offsetMax = new Vector2(-4f, 0f);  // right inset, top

        // Move all three to the beginning of sibling order (behind existing content)
        bgObj.transform.SetAsFirstSibling();
        borderObj.transform.SetSiblingIndex(1);
        headerObj.transform.SetSiblingIndex(2);

        // Add StyledPanel component and wire references via SerializedObject
        StyledPanel styledPanel = Undo.AddComponent<StyledPanel>(panelObj);

        SerializedObject so = new SerializedObject(styledPanel);
        so.FindProperty("panelBackground").objectReferenceValue = bgImage;
        so.FindProperty("panelBorder").objectReferenceValue = borderImage;
        so.FindProperty("headerBar").objectReferenceValue = headerImage;
        so.ApplyModifiedProperties();

        Debug.Log($"[PanelBackgroundSetup] Set up panel background on {panelName}");
    }

    /// <summary>
    /// Create a UI child GameObject with a RectTransform.
    /// </summary>
    private static GameObject CreateUIChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        return child;
    }

    /// <summary>
    /// Stretch a RectTransform to fill its parent completely.
    /// </summary>
    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
