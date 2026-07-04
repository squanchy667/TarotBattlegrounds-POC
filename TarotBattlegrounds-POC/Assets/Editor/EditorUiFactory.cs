#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Shared UI-construction helpers for the editor *Setup.cs scripts.
/// Consolidates the private static helpers that were previously duplicated
/// across the Setup scripts. Behavior-preserving: optional parameters cover
/// the per-file variants (defaults match the most common variant).
/// </summary>
public static class EditorUiFactory
{
    public static GameObject CreateCanvas(bool withBackground = false, bool withEventSystem = true)
    {
        GameObject canvasObj = new GameObject("Canvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        if (withBackground)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.08f, 0.14f, 1f);
            bgImg.raycastTarget = false;
        }

        // EventSystem
        if (withEventSystem && Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        return canvasObj;
    }

    public static GameObject CreateFullscreenPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return panel;
    }

    public static GameObject CreateCenteredContainer(Transform parent, string name,
        float width, float height)
    {
        GameObject container = new GameObject(name);
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.18f, 0.95f);

        return container;
    }

    public static GameObject CreateText(Transform parent, string name, string text,
        int fontSize, FontStyles style, Color color, TextAlignmentOptions alignment,
        int heightPadding = 12, bool richText = false, bool raycastTarget = true,
        float flexibleHeight = -1f)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + heightPadding);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (richText) tmp.richText = true;
        if (!raycastTarget) tmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.minHeight = fontSize + heightPadding;
        le.flexibleHeight = flexibleHeight;

        return obj;
    }

    public static GameObject CreateButton(Transform parent, string name, string label,
        float width, float height, int labelFontSize = 18, bool addLayoutElement = true,
        bool labelRaycastTarget = true)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.22f, 0.35f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.3f, 0.45f, 1f);
        colors.pressedColor = new Color(0.18f, 0.15f, 0.28f, 1f);
        colors.selectedColor = new Color(0.3f, 0.27f, 0.4f, 1f);
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        btn.colors = colors;

        if (addLayoutElement)
        {
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.minHeight = height;
        }

        // Text child
        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = labelFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        if (!labelRaycastTarget) tmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) tmp.font = font;

        return btnObj;
    }

    public static GameObject CreateHorizontalRow(Transform parent, string name, float spacing,
        float minHeight = 55f, TextAnchor childAlignment = TextAnchor.MiddleCenter,
        RectOffset padding = null, bool addLayoutElement = true)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = childAlignment;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        if (padding != null) hlg.padding = padding;

        if (addLayoutElement)
        {
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
        }

        return row;
    }

    public static GameObject CreateDropdown(Transform parent, string name,
        float width, float height, string labelText = "Medium", int fontSize = 18,
        Color? backgroundColor = null)
    {
        Color bgColor = backgroundColor ?? new Color(0.18f, 0.15f, 0.22f, 1f);

        GameObject dropObj = new GameObject(name);
        dropObj.transform.SetParent(parent, false);

        RectTransform rect = dropObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = dropObj.AddComponent<Image>();
        img.color = bgColor;

        LayoutElement le = dropObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);

        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = labelText;
        labelTmp.fontSize = fontSize;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_FontAsset font = FindFont();
        if (font != null) labelTmp.font = font;

        // Template (dropdown list)
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropObj.transform, false);
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0, 150);

        Image templateImg = template.AddComponent<Image>();
        templateImg.color = bgColor;

        ScrollRect scroll = template.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>();
        viewport.AddComponent<Image>().color = Color.white;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);

        scroll.viewport = vpRect;
        scroll.content = contentRect;

        // Item
        GameObject item = new GameObject("Item");
        item.transform.SetParent(contentObj.transform, false);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 40);
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);

        item.AddComponent<Toggle>();

        // Item label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(item.transform, false);
        RectTransform ilRect = itemLabelObj.AddComponent<RectTransform>();
        ilRect.anchorMin = Vector2.zero;
        ilRect.anchorMax = Vector2.one;
        ilRect.offsetMin = new Vector2(10, 2);
        ilRect.offsetMax = new Vector2(-10, -2);

        TextMeshProUGUI itemTmp = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemTmp.text = "Option";
        itemTmp.fontSize = fontSize;
        itemTmp.color = Color.white;
        if (font != null) itemTmp.font = font;

        template.SetActive(false);

        // TMP_Dropdown
        TMP_Dropdown dropdown = dropObj.AddComponent<TMP_Dropdown>();
        dropdown.template = templateRect;
        dropdown.captionText = labelTmp;
        dropdown.itemText = itemTmp;

        return dropObj;
    }

    public static GameObject CreateInputField(Transform parent, string name,
        string placeholder, float width, float height)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image img = inputObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.15f, 0.22f, 1f);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder text
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        TextMeshProUGUI phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 16;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_FontAsset font = FindFont();
        if (font != null) phTmp.font = font;

        // Input text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textObjRect = textObj.AddComponent<RectTransform>();
        textObjRect.anchorMin = Vector2.zero;
        textObjRect.anchorMax = Vector2.one;
        textObjRect.offsetMin = Vector2.zero;
        textObjRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 16;
        textTmp.color = Color.white;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) textTmp.font = font;

        // TMP_InputField
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = font;
        inputField.pointSize = 16;

        return inputObj;
    }

    public static GameObject CreateInputFieldMasked(Transform parent, string name,
        string placeholder, float width, float height)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.2f, 1f);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 2);
        taRect.offsetMax = new Vector2(-10, -2);
        textArea.AddComponent<RectMask2D>();

        // Placeholder text
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;

        TextMeshProUGUI phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 14;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        TMP_FontAsset font = FindFont();
        if (font != null) phTmp.font = font;

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "";
        textTmp.fontSize = 14;
        textTmp.color = Color.white;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;
        textTmp.raycastTarget = false;

        if (font != null) textTmp.font = font;

        // TMP_InputField
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = taRect;
        inputField.textComponent = textTmp;
        inputField.placeholder = phTmp;
        inputField.fontAsset = font;
        inputField.pointSize = 14;

        // Style the caret
        inputField.caretColor = new Color(1f, 0.78f, 0.15f);
        inputField.selectionColor = new Color(0.55f, 0.3f, 0.75f, 0.4f);

        return inputObj;
    }

    public static GameObject CreateUIChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        return child;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
        return font;
    }
}
#endif
