#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// UX10: Editor utility to create a fully-wired CircularTimer prefab hierarchy.
/// Tools > Game > Setup Circular Timer
/// </summary>
public class CircularTimerSetup : Editor
{
    [MenuItem("Tools/Game/Setup Circular Timer")]
    public static void SetupCircularTimer()
    {
        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CircularTimerSetup] No Canvas found in scene. Please create a Canvas first.");
            return;
        }

        // Generate a shared circle sprite
        Sprite circleSprite = CircularTimer.GenerateCircleSprite(128);

        // Create root timer GameObject
        GameObject timerRoot = new GameObject("CircularTimer");
        timerRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = timerRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);    // Top-center
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -10f);
        rootRect.sizeDelta = new Vector2(80f, 80f);

        // Add CircularTimer component
        CircularTimer timerComponent = timerRoot.AddComponent<CircularTimer>();

        // --- ArcBackground ---
        GameObject arcBgObj = new GameObject("ArcBackground");
        arcBgObj.transform.SetParent(timerRoot.transform, false);

        RectTransform arcBgRect = arcBgObj.AddComponent<RectTransform>();
        arcBgRect.anchorMin = Vector2.zero;
        arcBgRect.anchorMax = Vector2.one;
        arcBgRect.offsetMin = Vector2.zero;
        arcBgRect.offsetMax = Vector2.zero;

        Image arcBgImage = arcBgObj.AddComponent<Image>();
        arcBgImage.sprite = circleSprite;
        arcBgImage.type = Image.Type.Filled;
        arcBgImage.fillMethod = Image.FillMethod.Radial360;
        arcBgImage.fillOrigin = (int)Image.Origin360.Top;
        arcBgImage.fillClockwise = true;
        arcBgImage.fillAmount = 1f;
        arcBgImage.color = Tokens.WithAlpha(Tokens.CharredWood, 0.5f);
        arcBgImage.raycastTarget = false;

        // --- ArcFill ---
        GameObject arcFillObj = new GameObject("ArcFill");
        arcFillObj.transform.SetParent(timerRoot.transform, false);

        RectTransform arcFillRect = arcFillObj.AddComponent<RectTransform>();
        arcFillRect.anchorMin = Vector2.zero;
        arcFillRect.anchorMax = Vector2.one;
        arcFillRect.offsetMin = Vector2.zero;
        arcFillRect.offsetMax = Vector2.zero;

        Image arcFillImage = arcFillObj.AddComponent<Image>();
        arcFillImage.sprite = circleSprite;
        arcFillImage.type = Image.Type.Filled;
        arcFillImage.fillMethod = Image.FillMethod.Radial360;
        arcFillImage.fillOrigin = (int)Image.Origin360.Top;
        arcFillImage.fillClockwise = true;
        arcFillImage.fillAmount = 1f;
        arcFillImage.color = Tokens.Ember;
        arcFillImage.raycastTarget = false;

        // --- CenterCircle ---
        GameObject centerObj = new GameObject("CenterCircle");
        centerObj.transform.SetParent(timerRoot.transform, false);

        RectTransform centerRect = centerObj.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.15f, 0.15f);
        centerRect.anchorMax = new Vector2(0.85f, 0.85f);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;

        Image centerImage = centerObj.AddComponent<Image>();
        centerImage.sprite = circleSprite;
        centerImage.color = Tokens.WithAlpha(Tokens.Ash, 0.95f);
        centerImage.raycastTarget = false;

        // --- TimeText ---
        GameObject textObj = new GameObject("TimeText");
        textObj.transform.SetParent(timerRoot.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "35";
        tmpText.fontSize = Tokens.TextH3;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Tokens.Ember;
        tmpText.raycastTarget = false;

        // --- Wire SerializedFields ---
        SerializedObject so = new SerializedObject(timerComponent);
        so.FindProperty("arcFill").objectReferenceValue = arcFillImage;
        so.FindProperty("arcBackground").objectReferenceValue = arcBgImage;
        so.FindProperty("timeText").objectReferenceValue = tmpText;
        so.FindProperty("centerCircle").objectReferenceValue = centerImage;
        so.ApplyModifiedProperties();

        // Select the created object
        Selection.activeGameObject = timerRoot;
        Undo.RegisterCreatedObjectUndo(timerRoot, "Create Circular Timer");

        Debug.Log("[CircularTimerSetup] Circular Timer created successfully. " +
                  "Wire it to GameUIManager.circularTimer or RecruitTimerUI.circularTimer in the Inspector.");
    }
}
#endif
