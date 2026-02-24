#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TarotBattlegrounds.UI;

/// <summary>
/// UX05: Editor tool that sets up layered card frame Image children on all
/// CardDisplayUI / CardFrameGenerator objects in the scene.
/// Creates OuterFrame, InnerFrame, InnerShadow, and TierGems Image children
/// and wires their references to the CardFrameGenerator component.
/// </summary>
public class CardFrameSetup : Editor
{
    [MenuItem("Tools/Game/Setup Card Frames")]
    public static void SetupCardFrames()
    {
        int setupCount = 0;

        // Find all CardFrameGenerator components in scene
        CardFrameGenerator[] generators = FindObjectsOfType<CardFrameGenerator>();
        foreach (var generator in generators)
        {
            if (SetupFrameLayersOnGenerator(generator))
                setupCount++;
        }

        // Also find CardDisplayUI objects that lack a CardFrameGenerator
        // and add one if they have a card frame Image
        CardDisplayUI[] displays = FindObjectsOfType<CardDisplayUI>();
        foreach (var display in displays)
        {
            CardFrameGenerator existingGen = display.GetComponent<CardFrameGenerator>();
            if (existingGen == null)
            {
                // Add CardFrameGenerator component
                existingGen = Undo.AddComponent<CardFrameGenerator>(display.gameObject);
                Debug.Log($"[CardFrameSetup] Added CardFrameGenerator to {display.gameObject.name}");
            }

            if (SetupFrameLayersOnGenerator(existingGen))
                setupCount++;
        }

        if (setupCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[CardFrameSetup] Successfully set up card frames on {setupCount} objects. Save the scene to persist changes.");
        }
        else
        {
            Debug.LogWarning("[CardFrameSetup] No CardFrameGenerator or CardDisplayUI objects found in scene, or all already set up.");
        }
    }

    /// <summary>
    /// Set up frame layer Image children on a CardFrameGenerator and wire references.
    /// Returns true if setup was performed, false if already set up.
    /// </summary>
    private static bool SetupFrameLayersOnGenerator(CardFrameGenerator generator)
    {
        if (generator == null) return false;

        // Check if already set up by looking for OuterFrame child
        Transform existingOuter = generator.transform.Find("OuterFrame");
        if (existingOuter != null)
        {
            Debug.Log($"[CardFrameSetup] {generator.gameObject.name} already has frame layers, skipping");
            return false;
        }

        Undo.RegisterCompleteObjectUndo(generator.gameObject, $"Setup Card Frame on {generator.gameObject.name}");

        RectTransform parentRect = generator.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning($"[CardFrameSetup] {generator.gameObject.name} has no RectTransform, skipping");
            return false;
        }

        // Create OuterFrame — stretched to fill, behind content
        GameObject outerFrameObj = CreateUIChild(generator.gameObject, "OuterFrame");
        Image outerFrameImage = outerFrameObj.AddComponent<Image>();
        outerFrameImage.raycastTarget = false;
        StretchToParent(outerFrameObj.GetComponent<RectTransform>());

        // Create InnerFrame — stretched with padding (3px inset), behind content
        GameObject innerFrameObj = CreateUIChild(generator.gameObject, "InnerFrame");
        Image innerFrameImage = innerFrameObj.AddComponent<Image>();
        innerFrameImage.raycastTarget = false;
        RectTransform innerFrameRect = innerFrameObj.GetComponent<RectTransform>();
        innerFrameRect.anchorMin = Vector2.zero;
        innerFrameRect.anchorMax = Vector2.one;
        innerFrameRect.offsetMin = new Vector2(3f, 3f);   // 3px inset from edges
        innerFrameRect.offsetMax = new Vector2(-3f, -3f);

        // Create InnerShadow — stretched to fill, on top of background
        GameObject innerShadowObj = CreateUIChild(generator.gameObject, "InnerShadow");
        Image innerShadowImage = innerShadowObj.AddComponent<Image>();
        innerShadowImage.raycastTarget = false;
        StretchToParent(innerShadowObj.GetComponent<RectTransform>());

        // Create TierGems — small area at top-right corner
        GameObject tierGemsObj = CreateUIChild(generator.gameObject, "TierGems");
        Image tierGemsImage = tierGemsObj.AddComponent<Image>();
        tierGemsImage.raycastTarget = false;
        RectTransform tierGemsRect = tierGemsObj.GetComponent<RectTransform>();
        // Anchor to top-right
        tierGemsRect.anchorMin = new Vector2(1f, 1f);
        tierGemsRect.anchorMax = new Vector2(1f, 1f);
        tierGemsRect.pivot = new Vector2(1f, 1f);
        tierGemsRect.sizeDelta = new Vector2(64f, 16f);
        tierGemsRect.anchoredPosition = new Vector2(-4f, -4f); // 4px offset from top-right

        // Move frame layers to beginning of sibling order (behind existing content)
        outerFrameObj.transform.SetAsFirstSibling();
        innerFrameObj.transform.SetSiblingIndex(1);
        innerShadowObj.transform.SetSiblingIndex(2);
        // TierGems stays at end (on top of everything)

        // Wire references to CardFrameGenerator via SerializedObject
        SerializedObject so = new SerializedObject(generator);
        so.FindProperty("outerFrame").objectReferenceValue = outerFrameImage;
        so.FindProperty("innerFrame").objectReferenceValue = innerFrameImage;
        so.FindProperty("innerShadow").objectReferenceValue = innerShadowImage;
        so.FindProperty("tierIndicator").objectReferenceValue = tierGemsImage;
        so.ApplyModifiedProperties();

        // Also wire tierIndicatorImage on CardDisplayUI if present
        CardDisplayUI displayUI = generator.GetComponent<CardDisplayUI>();
        if (displayUI != null)
        {
            SerializedObject displaySO = new SerializedObject(displayUI);
            SerializedProperty tierIndicatorProp = displaySO.FindProperty("tierIndicatorImage");
            if (tierIndicatorProp != null)
            {
                tierIndicatorProp.objectReferenceValue = tierGemsImage;
                displaySO.ApplyModifiedProperties();
            }
        }

        Debug.Log($"[CardFrameSetup] Set up frame layers on {generator.gameObject.name}");
        return true;
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
