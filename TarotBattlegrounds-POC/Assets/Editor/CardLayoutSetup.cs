#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// UX06: Editor tool that restructures CardDisplayUI for a proper card game layout.
/// Creates layout zones (name banner, artwork area, ability area, stat bar),
/// stat badges (ATK, HP, Cost) with circular backgrounds,
/// and wires all references to CardDisplayUI.
///
/// Menu: Tools > Game > Setup Card Layout
/// </summary>
public class CardLayoutSetup : Editor
{
    // Badge colors matching CardDisplayUI constants
    private static readonly Color attackBadgeColor = new Color(0.7f, 0.15f, 0.15f, 1f);
    private static readonly Color healthBadgeColor = new Color(0.15f, 0.55f, 0.15f, 1f);
    private static readonly Color costBadgeColor = new Color(0.2f, 0.4f, 0.8f, 1f);

    [MenuItem("Tools/Game/Setup Card Layout")]
    public static void SetupCardLayout()
    {
        int setupCount = 0;

        CardDisplayUI[] displays = FindObjectsOfType<CardDisplayUI>();
        foreach (var display in displays)
        {
            if (SetupLayoutOnDisplay(display))
                setupCount++;
        }

        if (setupCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[CardLayoutSetup] Successfully set up card layout on {setupCount} CardDisplayUI objects. Save the scene to persist changes.");
        }
        else
        {
            Debug.LogWarning("[CardLayoutSetup] No CardDisplayUI objects found in scene, or all already set up.");
        }
    }

    /// <summary>
    /// Set up UX06 layout zones and stat badges on a CardDisplayUI.
    /// Returns true if setup was performed, false if already set up.
    /// </summary>
    private static bool SetupLayoutOnDisplay(CardDisplayUI display)
    {
        if (display == null) return false;

        // Check if already set up by looking for NameBanner child
        Transform existingBanner = display.transform.Find("NameBanner");
        if (existingBanner != null)
        {
            Debug.Log($"[CardLayoutSetup] {display.gameObject.name} already has UX06 layout, skipping");
            return false;
        }

        RectTransform parentRect = display.GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning($"[CardLayoutSetup] {display.gameObject.name} has no RectTransform, skipping");
            return false;
        }

        Undo.RegisterCompleteObjectUndo(display.gameObject, $"Setup Card Layout on {display.gameObject.name}");

        // ===== 1. Create Layout Zones =====

        // Name Banner: top strip, stretched horizontal, 24px height
        GameObject nameBannerObj = CreateUIChild(display.gameObject, "NameBanner");
        Image nameBannerImage = nameBannerObj.AddComponent<Image>();
        nameBannerImage.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent dark
        nameBannerImage.raycastTarget = false;
        RectTransform nameBannerRect = nameBannerObj.GetComponent<RectTransform>();
        nameBannerRect.anchorMin = new Vector2(0f, 1f);
        nameBannerRect.anchorMax = new Vector2(1f, 1f);
        nameBannerRect.pivot = new Vector2(0.5f, 1f);
        nameBannerRect.sizeDelta = new Vector2(0f, 24f);
        nameBannerRect.anchoredPosition = Vector2.zero;

        // Artwork Area: center, with margins for name banner and stat bar
        GameObject artworkAreaObj = CreateUIChild(display.gameObject, "ArtworkArea");
        RectTransform artworkAreaRect = artworkAreaObj.GetComponent<RectTransform>();
        artworkAreaRect.anchorMin = Vector2.zero;
        artworkAreaRect.anchorMax = Vector2.one;
        artworkAreaRect.offsetMin = new Vector2(4f, 28f + 20f); // Bottom: stat bar + ability area
        artworkAreaRect.offsetMax = new Vector2(-4f, -24f);      // Top: name banner

        // Ability Area: below artwork, above stat bar
        GameObject abilityAreaObj = CreateUIChild(display.gameObject, "AbilityArea");
        RectTransform abilityAreaRect = abilityAreaObj.GetComponent<RectTransform>();
        abilityAreaRect.anchorMin = new Vector2(0f, 0f);
        abilityAreaRect.anchorMax = new Vector2(1f, 0f);
        abilityAreaRect.pivot = new Vector2(0.5f, 0f);
        abilityAreaRect.sizeDelta = new Vector2(0f, 20f);
        abilityAreaRect.anchoredPosition = new Vector2(0f, 28f); // Above stat bar

        // Ability Text inside AbilityArea
        GameObject abilityTextObj = CreateUIChild(abilityAreaObj, "AbilityText");
        TextMeshProUGUI abilityTmp = abilityTextObj.AddComponent<TextMeshProUGUI>();
        abilityTmp.fontSize = 11f;
        abilityTmp.fontStyle = FontStyles.Italic;
        abilityTmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        abilityTmp.alignment = TextAlignmentOptions.Center;
        abilityTmp.enableWordWrapping = true;
        abilityTmp.overflowMode = TextOverflowModes.Ellipsis;
        abilityTmp.raycastTarget = false;
        StretchToParent(abilityTextObj.GetComponent<RectTransform>());

        // Stat Bar: bottom strip, stretched horizontal, 28px height
        GameObject statBarObj = CreateUIChild(display.gameObject, "StatBar");
        RectTransform statBarRect = statBarObj.GetComponent<RectTransform>();
        statBarRect.anchorMin = new Vector2(0f, 0f);
        statBarRect.anchorMax = new Vector2(1f, 0f);
        statBarRect.pivot = new Vector2(0.5f, 0f);
        statBarRect.sizeDelta = new Vector2(0f, 28f);
        statBarRect.anchoredPosition = Vector2.zero;

        // ===== 2. Create Stat Badges =====

        // ATK Badge: bottom-left corner, 28x28
        GameObject atkBadgeObj = CreateStatBadge(display.gameObject, "ATK_Badge",
            new Vector2(0f, 0f), new Vector2(0f, 0f), // Anchor bottom-left
            new Vector2(0f, 0f),                       // Pivot bottom-left
            new Vector2(28f, 28f),
            new Vector2(2f, 2f),                       // Slight offset
            attackBadgeColor);

        // HP Badge: bottom-right corner, 28x28
        GameObject hpBadgeObj = CreateStatBadge(display.gameObject, "HP_Badge",
            new Vector2(1f, 0f), new Vector2(1f, 0f), // Anchor bottom-right
            new Vector2(1f, 0f),                       // Pivot bottom-right
            new Vector2(28f, 28f),
            new Vector2(-2f, 2f),                      // Slight offset
            healthBadgeColor);

        // Cost Badge: top-left corner, 24x24, slightly overlapping frame
        GameObject costBadgeObj = CreateStatBadge(display.gameObject, "Cost_Badge",
            new Vector2(0f, 1f), new Vector2(0f, 1f), // Anchor top-left
            new Vector2(0f, 1f),                       // Pivot top-left
            new Vector2(24f, 24f),
            new Vector2(-2f, 2f),                      // Slightly overlapping frame
            costBadgeColor);

        // ===== 3. Order siblings (back to front) =====
        // Layout zones behind content, badges on top
        nameBannerObj.transform.SetAsLastSibling();
        artworkAreaObj.transform.SetAsLastSibling();
        abilityAreaObj.transform.SetAsLastSibling();
        statBarObj.transform.SetAsLastSibling();
        atkBadgeObj.transform.SetAsLastSibling();
        hpBadgeObj.transform.SetAsLastSibling();
        costBadgeObj.transform.SetAsLastSibling();

        // ===== 4. Wire references to CardDisplayUI =====
        SerializedObject so = new SerializedObject(display);

        // Layout sections
        so.FindProperty("nameBanner").objectReferenceValue = nameBannerRect;
        so.FindProperty("artworkArea").objectReferenceValue = artworkAreaRect;
        so.FindProperty("abilityArea").objectReferenceValue = abilityAreaRect;
        so.FindProperty("statBar").objectReferenceValue = statBarRect;

        // Ability text
        so.FindProperty("abilityText").objectReferenceValue = abilityTmp;

        // Badge images
        Image atkBadgeBgImage = atkBadgeObj.GetComponent<Image>();
        Image hpBadgeBgImage = hpBadgeObj.GetComponent<Image>();
        Image costBadgeBgImage = costBadgeObj.GetComponent<Image>();

        so.FindProperty("attackBadge").objectReferenceValue = atkBadgeBgImage;
        so.FindProperty("healthBadge").objectReferenceValue = hpBadgeBgImage;
        so.FindProperty("costBadge").objectReferenceValue = costBadgeBgImage;

        // StatBadge components
        StatBadge atkStatBadge = atkBadgeObj.GetComponent<StatBadge>();
        StatBadge hpStatBadge = hpBadgeObj.GetComponent<StatBadge>();
        StatBadge costStatBadge = costBadgeObj.GetComponent<StatBadge>();

        so.FindProperty("attackStatBadge").objectReferenceValue = atkStatBadge;
        so.FindProperty("healthStatBadge").objectReferenceValue = hpStatBadge;
        so.FindProperty("costStatBadge").objectReferenceValue = costStatBadge;

        so.ApplyModifiedProperties();

        Debug.Log($"[CardLayoutSetup] Set up card layout on {display.gameObject.name}");
        return true;
    }

    /// <summary>
    /// Create a stat badge GameObject with Image background, StatBadge component,
    /// and centered TMP_Text child for the value.
    /// </summary>
    private static GameObject CreateStatBadge(
        GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 size,
        Vector2 position, Color badgeColor)
    {
        // Badge root with background image
        GameObject badgeObj = CreateUIChild(parent, name);
        Image badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.color = badgeColor;
        badgeBg.raycastTarget = false;

        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = anchorMin;
        badgeRect.anchorMax = anchorMax;
        badgeRect.pivot = pivot;
        badgeRect.sizeDelta = size;
        badgeRect.anchoredPosition = position;

        // Value text child (centered in badge)
        GameObject valueTextObj = CreateUIChild(badgeObj, "Value");
        TextMeshProUGUI valueTmp = valueTextObj.AddComponent<TextMeshProUGUI>();
        valueTmp.fontSize = 22f;
        valueTmp.fontStyle = FontStyles.Bold;
        valueTmp.color = Color.white;
        valueTmp.alignment = TextAlignmentOptions.Center;
        valueTmp.raycastTarget = false;
        valueTmp.enableWordWrapping = false;
        StretchToParent(valueTextObj.GetComponent<RectTransform>());

        // Add StatBadge component and wire internal references
        StatBadge statBadge = badgeObj.AddComponent<StatBadge>();
        SerializedObject badgeSO = new SerializedObject(statBadge);
        badgeSO.FindProperty("badgeBackground").objectReferenceValue = badgeBg;
        badgeSO.FindProperty("valueText").objectReferenceValue = valueTmp;
        badgeSO.FindProperty("badgeColor").colorValue = badgeColor;
        badgeSO.ApplyModifiedProperties();

        return badgeObj;
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
