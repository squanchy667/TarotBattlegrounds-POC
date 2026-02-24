#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script that finds all Button components in the scene and adds/wires
/// TarotButton components with appropriate child hierarchy and variant assignment.
/// Run via menu: Tools/Game/Setup Styled Buttons
/// </summary>
public class ButtonStylingSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Styled Buttons")]
    public static void SetupStyledButtons()
    {
        // Find all Button components in the active scene
        Button[] allButtons = FindObjectsOfType<Button>(true);

        if (allButtons.Length == 0)
        {
            EditorUtility.DisplayDialog("Setup Styled Buttons",
                "No Button components found in the current scene.\nPlease open the Game or MainMenu scene first.",
                "OK");
            return;
        }

        int buttonsProcessed = 0;
        int buttonsSkipped = 0;

        foreach (Button button in allButtons)
        {
            // Register undo for the button's GameObject
            Undo.RegisterCompleteObjectUndo(button.gameObject, "Setup Styled Buttons");

            // Add TarotButton if missing
            TarotButton tarotButton = button.GetComponent<TarotButton>();
            if (tarotButton == null)
            {
                tarotButton = Undo.AddComponent<TarotButton>(button.gameObject);
            }

            // Create child hierarchy if needed
            bool created = SetupButtonHierarchy(button, tarotButton);

            // Assign variant based on button name/purpose
            AssignVariant(button, tarotButton);

            if (created)
                buttonsProcessed++;
            else
                buttonsSkipped++;
        }

        // Mark scene as dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Styled Buttons",
            $"Processed {allButtons.Length} buttons.\n" +
            $"  Created hierarchy: {buttonsProcessed}\n" +
            $"  Already set up: {buttonsSkipped}",
            "OK");

        Debug.Log($"[ButtonStylingSetup] Processed {allButtons.Length} buttons " +
                  $"({buttonsProcessed} new, {buttonsSkipped} existing)");
    }

    /// <summary>
    /// Creates the child hierarchy for a button:
    ///   - "ButtonBG" (Image, gradient background, stretched)
    ///   - "ButtonBorder" (Image, rounded-rect outline)
    ///   - "ButtonLabel" (TMP_Text, uses existing text or creates new)
    /// Wires references into the TarotButton via SerializedObject.
    /// Returns true if any new objects were created.
    /// </summary>
    private static bool SetupButtonHierarchy(Button button, TarotButton tarotButton)
    {
        Transform buttonTransform = button.transform;
        bool anyCreated = false;

        // === ButtonBG ===
        Transform bgTransform = buttonTransform.Find("ButtonBG");
        Image bgImage;
        if (bgTransform == null)
        {
            GameObject bgObj = new GameObject("ButtonBG");
            Undo.RegisterCreatedObjectUndo(bgObj, "Create ButtonBG");
            bgObj.transform.SetParent(buttonTransform, false);
            bgObj.transform.SetAsFirstSibling(); // Behind everything else

            // RectTransform — stretch to fill
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            bgImage = bgObj.AddComponent<Image>();
            bgImage.raycastTarget = true;
            anyCreated = true;
        }
        else
        {
            bgImage = bgTransform.GetComponent<Image>();
            if (bgImage == null)
                bgImage = bgTransform.gameObject.AddComponent<Image>();
            bgImage.raycastTarget = true;
        }

        // === ButtonBorder ===
        Transform borderTransform = buttonTransform.Find("ButtonBorder");
        Image borderImage;
        if (borderTransform == null)
        {
            GameObject borderObj = new GameObject("ButtonBorder");
            Undo.RegisterCreatedObjectUndo(borderObj, "Create ButtonBorder");
            borderObj.transform.SetParent(buttonTransform, false);

            // RectTransform — stretch to fill
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            borderImage = borderObj.AddComponent<Image>();
            borderImage.raycastTarget = true;

            // Set as outline-like: thin border effect via color with slight transparency
            borderImage.color = new Color(1f, 0.9f, 0.4f, 0.4f);
            anyCreated = true;
        }
        else
        {
            borderImage = borderTransform.GetComponent<Image>();
            if (borderImage == null)
                borderImage = borderTransform.gameObject.AddComponent<Image>();
            borderImage.raycastTarget = true;
        }

        // === ButtonLabel ===
        Transform labelTransform = buttonTransform.Find("ButtonLabel");
        TMP_Text labelText;

        if (labelTransform == null)
        {
            // Check if there's an existing TMP_Text child we can reuse
            TMP_Text existingText = button.GetComponentInChildren<TMP_Text>();
            if (existingText != null && existingText.gameObject.name != "ButtonBG" &&
                existingText.gameObject.name != "ButtonBorder")
            {
                // Rename existing text to ButtonLabel for consistency
                existingText.gameObject.name = "ButtonLabel";
                labelText = existingText;
            }
            else
            {
                // Create new label
                GameObject labelObj = new GameObject("ButtonLabel");
                Undo.RegisterCreatedObjectUndo(labelObj, "Create ButtonLabel");
                labelObj.transform.SetParent(buttonTransform, false);

                // RectTransform — stretch to fill with some padding
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 4f);
                labelRect.offsetMax = new Vector2(-8f, -4f);

                labelText = labelObj.AddComponent<TextMeshProUGUI>();
                labelText.text = button.gameObject.name;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 16;
                labelText.color = Color.white;
                labelText.raycastTarget = false; // Label doesn't need raycast
                anyCreated = true;
            }
        }
        else
        {
            labelText = labelTransform.GetComponent<TMP_Text>();
            if (labelText == null)
            {
                labelText = labelTransform.gameObject.AddComponent<TextMeshProUGUI>();
                labelText.text = button.gameObject.name;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 16;
                labelText.color = Color.white;
            }
        }

        // Ensure label is on top (last sibling)
        if (labelText != null)
        {
            labelText.transform.SetAsLastSibling();
            labelText.raycastTarget = false;
        }

        // === Wire references via SerializedObject ===
        SerializedObject so = new SerializedObject(tarotButton);

        SerializedProperty bgProp = so.FindProperty("buttonBackground");
        if (bgProp != null) bgProp.objectReferenceValue = bgImage;

        SerializedProperty borderProp = so.FindProperty("buttonBorder");
        if (borderProp != null) borderProp.objectReferenceValue = borderImage;

        SerializedProperty labelProp = so.FindProperty("buttonLabel");
        if (labelProp != null) labelProp.objectReferenceValue = labelText;

        so.ApplyModifiedProperties();

        return anyCreated;
    }

    /// <summary>
    /// Assign a ButtonVariant based on the button's name or its parent's purpose.
    /// </summary>
    private static void AssignVariant(Button button, TarotButton tarotButton)
    {
        string name = button.gameObject.name.ToLower();

        ButtonVariant assignedVariant;

        if (name.Contains("buy") || name.Contains("play") || name.Contains("upgrade"))
        {
            assignedVariant = ButtonVariant.Primary;
        }
        else if (name.Contains("sell") || name.Contains("refresh") || name.Contains("reroll") ||
                 name.Contains("freeze"))
        {
            assignedVariant = ButtonVariant.Secondary;
        }
        else if (name.Contains("endturn") || name.Contains("end_turn") || name.Contains("end turn"))
        {
            assignedVariant = ButtonVariant.Success;
        }
        else if (name.Contains("danger") || name.Contains("delete") || name.Contains("quit") ||
                 name.Contains("exit") || name.Contains("cancel"))
        {
            assignedVariant = ButtonVariant.Danger;
        }
        else
        {
            // Default to Primary for unrecognized buttons
            assignedVariant = ButtonVariant.Primary;
        }

        // Set via SerializedObject for proper Undo support
        SerializedObject so = new SerializedObject(tarotButton);
        SerializedProperty variantProp = so.FindProperty("variant");
        if (variantProp != null)
        {
            variantProp.enumValueIndex = (int)assignedVariant;
        }
        so.ApplyModifiedProperties();
    }
}
#endif
