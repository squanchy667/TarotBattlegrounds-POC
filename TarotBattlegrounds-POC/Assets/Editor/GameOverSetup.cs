#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// UX18: Editor tool that sets up the Game Over screen polish elements.
/// Creates the dark overlay, placement badge (circular image + number text),
/// and wires all new serialized fields on the GameOverUI component.
/// Run from Tools > Game > Setup Game Over Screen.
/// </summary>
public class GameOverSetup : Editor
{
    [MenuItem("Tools/Game/Setup Game Over Screen")]
    public static void SetupGameOverScreen()
    {
        GameOverUI gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI == null)
        {
            Debug.LogError("[GameOverSetup] GameOverUI not found in scene. Make sure the Game scene is open.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(gameOverUI.gameObject, "Setup Game Over Screen UX18");

        SerializedObject so = new SerializedObject(gameOverUI);

        // === Dark Overlay ===
        // Create a fullscreen dark semi-transparent overlay behind the panel
        Canvas rootCanvas = gameOverUI.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();

        if (rootCanvas != null)
        {
            // Check if overlay already exists
            Transform existingOverlay = rootCanvas.transform.Find("GameOverDarkOverlay");
            if (existingOverlay == null)
            {
                GameObject overlayObj = new GameObject("GameOverDarkOverlay", typeof(RectTransform));
                overlayObj.transform.SetParent(rootCanvas.transform, false);
                Undo.RegisterCreatedObjectUndo(overlayObj, "Create GameOver Dark Overlay");

                RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                Image overlayImage = overlayObj.AddComponent<Image>();
                overlayImage.color = new Color(0f, 0f, 0f, 0.7f);
                overlayImage.raycastTarget = false;

                // Place overlay just before the GameOverUI panel in hierarchy
                overlayObj.transform.SetSiblingIndex(gameOverUI.transform.GetSiblingIndex());

                // Start hidden (will be shown by GameOverUI.ShowGameOver)
                overlayObj.SetActive(false);

                so.FindProperty("darkOverlay").objectReferenceValue = overlayImage;
                Debug.Log("[GameOverSetup] Created dark overlay");
            }
            else
            {
                Image existingImage = existingOverlay.GetComponent<Image>();
                if (existingImage != null)
                    so.FindProperty("darkOverlay").objectReferenceValue = existingImage;
                Debug.Log("[GameOverSetup] Dark overlay already exists, wiring reference");
            }
        }

        // === Panel Rect (for scale animation) ===
        // Find the gameOverPanel and get/wire its RectTransform
        SerializedProperty panelProp = so.FindProperty("gameOverPanel");
        GameObject panelGO = panelProp != null ? panelProp.objectReferenceValue as GameObject : null;
        if (panelGO != null)
        {
            RectTransform panelRectTransform = panelGO.GetComponent<RectTransform>();
            if (panelRectTransform != null)
            {
                so.FindProperty("panelRect").objectReferenceValue = panelRectTransform;
                Debug.Log("[GameOverSetup] Wired panelRect for animation");
            }

            // Also ensure CanvasGroup exists on the panel for alpha animation
            CanvasGroup canvasGroup = panelGO.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = Undo.AddComponent<CanvasGroup>(panelGO);
                Debug.Log("[GameOverSetup] Added CanvasGroup to game over panel");
            }
            so.FindProperty("panelCanvasGroup").objectReferenceValue = canvasGroup;
        }
        else
        {
            Debug.LogWarning("[GameOverSetup] gameOverPanel not assigned. Cannot wire panelRect or panelCanvasGroup.");
        }

        // === Placement Badge ===
        // Create a circular badge image with a number text on top
        if (panelGO != null)
        {
            Transform existingBadge = panelGO.transform.Find("PlacementBadge");
            if (existingBadge == null)
            {
                // Create badge container
                GameObject badgeObj = new GameObject("PlacementBadge", typeof(RectTransform));
                badgeObj.transform.SetParent(panelGO.transform, false);
                Undo.RegisterCreatedObjectUndo(badgeObj, "Create Placement Badge");

                RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
                // Position at top of panel, centered
                badgeRect.anchorMin = new Vector2(0.5f, 1f);
                badgeRect.anchorMax = new Vector2(0.5f, 1f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(0f, -50f);
                badgeRect.sizeDelta = new Vector2(80f, 80f);

                // Badge background (circular) — use default white sprite tinted by medal color
                Image badgeImage = badgeObj.AddComponent<Image>();
                badgeImage.color = new Color(1f, 0.82f, 0.12f); // Default gold
                badgeImage.raycastTarget = false;

                so.FindProperty("placementBadge").objectReferenceValue = badgeImage;

                // Create number text on top of badge
                GameObject numberObj = new GameObject("PlacementNumber", typeof(RectTransform));
                numberObj.transform.SetParent(badgeObj.transform, false);
                Undo.RegisterCreatedObjectUndo(numberObj, "Create Placement Number");

                RectTransform numberRect = numberObj.GetComponent<RectTransform>();
                numberRect.anchorMin = Vector2.zero;
                numberRect.anchorMax = Vector2.one;
                numberRect.offsetMin = Vector2.zero;
                numberRect.offsetMax = Vector2.zero;

                TMP_Text numberText = numberObj.AddComponent<TextMeshProUGUI>();
                numberText.text = "1st";
                numberText.fontSize = 28f;
                numberText.fontStyle = FontStyles.Bold;
                numberText.color = Color.white;
                numberText.alignment = TextAlignmentOptions.Center;
                numberText.raycastTarget = false;

                so.FindProperty("placementNumber").objectReferenceValue = numberText;

                // Move badge to be first child so it appears at the top
                badgeObj.transform.SetAsFirstSibling();

                Debug.Log("[GameOverSetup] Created placement badge with number text");
            }
            else
            {
                // Wire existing references
                Image existingBadgeImage = existingBadge.GetComponent<Image>();
                if (existingBadgeImage != null)
                    so.FindProperty("placementBadge").objectReferenceValue = existingBadgeImage;

                Transform existingNumber = existingBadge.Find("PlacementNumber");
                if (existingNumber != null)
                {
                    TMP_Text existingNumberText = existingNumber.GetComponent<TMP_Text>();
                    if (existingNumberText != null)
                        so.FindProperty("placementNumber").objectReferenceValue = existingNumberText;
                }

                Debug.Log("[GameOverSetup] Placement badge already exists, wiring references");
            }
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[GameOverSetup] Game Over screen setup complete. Save the scene to persist changes.");
    }
}
#endif
