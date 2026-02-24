#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to create and wire the ResourceBar UI hierarchy.
/// Creates a horizontal layout with 3 styled containers (coin, health, tier).
/// </summary>
public class ResourceBarSetup : Editor
{
    [MenuItem("Tools/Game/Setup Resource Bar")]
    public static void SetupResourceBar()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ResourceBarSetup] No Canvas found in scene. Create a Canvas first.");
            return;
        }

        // Create root resource bar object
        GameObject resourceBarRoot = new GameObject("ResourceBar");
        resourceBarRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = resourceBarRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -10f);
        rootRect.sizeDelta = new Vector2(-20f, 50f);

        // Horizontal layout for the 3 containers
        HorizontalLayoutGroup rootLayout = resourceBarRoot.AddComponent<HorizontalLayoutGroup>();
        rootLayout.spacing = 12f;
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = true;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = true;
        rootLayout.padding = new RectOffset(12, 12, 4, 4);

        // Add ResourceBar component
        ResourceBar resourceBar = resourceBarRoot.AddComponent<ResourceBar>();

        // Create the 3 containers
        var coinRefs = CreateResourceContainer(resourceBarRoot.transform, "CoinContainer",
            new Color(1f, 0.82f, 0.12f), true);
        var healthRefs = CreateResourceContainer(resourceBarRoot.transform, "HealthContainer",
            new Color(0.9f, 0.2f, 0.25f), false);
        var tierRefs = CreateTierContainer(resourceBarRoot.transform, "TierContainer",
            new Color(0.55f, 0.3f, 0.75f));

        // Wire references to ResourceBar via SerializedObject
        SerializedObject so = new SerializedObject(resourceBar);

        // Coin refs
        so.FindProperty("coinIcon").objectReferenceValue = coinRefs.icon;
        so.FindProperty("coinText").objectReferenceValue = coinRefs.valueText;
        so.FindProperty("coinContainer").objectReferenceValue = coinRefs.container;

        // Health refs
        so.FindProperty("healthIcon").objectReferenceValue = healthRefs.icon;
        so.FindProperty("healthText").objectReferenceValue = healthRefs.valueText;
        so.FindProperty("healthContainer").objectReferenceValue = healthRefs.container;

        // Tier refs
        so.FindProperty("tierIcon").objectReferenceValue = tierRefs.icon;
        so.FindProperty("tierText").objectReferenceValue = tierRefs.valueText;
        so.FindProperty("upgradeCostText").objectReferenceValue = tierRefs.upgradeCostText;
        so.FindProperty("tierContainer").objectReferenceValue = tierRefs.container;

        so.ApplyModifiedProperties();

        // Wire ResourceBar to GameUIManager if present
        GameUIManager uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager != null)
        {
            SerializedObject uiSo = new SerializedObject(uiManager);
            SerializedProperty resourceBarProp = uiSo.FindProperty("resourceBar");
            if (resourceBarProp != null)
            {
                resourceBarProp.objectReferenceValue = resourceBar;
                uiSo.ApplyModifiedProperties();
                Debug.Log("[ResourceBarSetup] Wired ResourceBar to GameUIManager.");
            }
        }

        Undo.RegisterCreatedObjectUndo(resourceBarRoot, "Setup Resource Bar");
        Selection.activeGameObject = resourceBarRoot;

        Debug.Log("[ResourceBarSetup] Resource Bar created successfully with coin, health, and tier containers.");
    }

    private struct ContainerRefs
    {
        public Image container;
        public Image icon;
        public TMP_Text valueText;
        public TMP_Text upgradeCostText;
    }

    /// <summary>
    /// Create a single resource container with icon + value text.
    /// </summary>
    private static ContainerRefs CreateResourceContainer(Transform parent, string name, Color iconColor, bool showMaxValue)
    {
        ContainerRefs refs = new ContainerRefs();

        // Container background
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(showMaxValue ? 100f : 80f, 40f);

        Image containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.08f, 0.05f, 0.14f, 0.8f);
        containerImage.raycastTarget = false;
        refs.container = containerImage;

        // Horizontal layout for icon + text
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.padding = new RectOffset(8, 8, 4, 4);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(container.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(32f, 32f);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = iconColor;
        iconImage.raycastTarget = false;
        refs.icon = iconImage;

        // Value text
        GameObject textObj = new GameObject("ValueText");
        textObj.transform.SetParent(container.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(50f, 32f);

        TMP_Text valueText = textObj.AddComponent<TextMeshProUGUI>();
        valueText.text = showMaxValue ? "0/10" : "0";
        valueText.fontSize = 18f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.MidlineLeft;
        valueText.raycastTarget = false;
        refs.valueText = valueText;

        return refs;
    }

    /// <summary>
    /// Create the tier container with icon + tier text + upgrade cost text.
    /// </summary>
    private static ContainerRefs CreateTierContainer(Transform parent, string name, Color iconColor)
    {
        ContainerRefs refs = new ContainerRefs();

        // Container background
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(180f, 40f);

        Image containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.08f, 0.05f, 0.14f, 0.8f);
        containerImage.raycastTarget = false;
        refs.container = containerImage;

        // Horizontal layout for icon + tier text + cost text
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.padding = new RectOffset(8, 8, 4, 4);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(container.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(32f, 32f);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = iconColor;
        iconImage.raycastTarget = false;
        refs.icon = iconImage;

        // Tier text
        GameObject tierTextObj = new GameObject("TierText");
        tierTextObj.transform.SetParent(container.transform, false);

        RectTransform tierTextRect = tierTextObj.AddComponent<RectTransform>();
        tierTextRect.sizeDelta = new Vector2(60f, 32f);

        TMP_Text tierText = tierTextObj.AddComponent<TextMeshProUGUI>();
        tierText.text = "Tier I";
        tierText.fontSize = 18f;
        tierText.fontStyle = FontStyles.Bold;
        tierText.color = Color.white;
        tierText.alignment = TextAlignmentOptions.MidlineLeft;
        tierText.raycastTarget = false;
        refs.valueText = tierText;

        // Upgrade cost text
        GameObject costTextObj = new GameObject("UpgradeCostText");
        costTextObj.transform.SetParent(container.transform, false);

        RectTransform costTextRect = costTextObj.AddComponent<RectTransform>();
        costTextRect.sizeDelta = new Vector2(80f, 32f);

        TMP_Text costText = costTextObj.AddComponent<TextMeshProUGUI>();
        costText.text = "5g to upgrade";
        costText.fontSize = 14f;
        costText.fontStyle = FontStyles.Normal;
        costText.color = new Color(0.8f, 0.8f, 0.8f);
        costText.alignment = TextAlignmentOptions.MidlineLeft;
        costText.raycastTarget = false;
        refs.upgradeCostText = costText;

        return refs;
    }
}
#endif
