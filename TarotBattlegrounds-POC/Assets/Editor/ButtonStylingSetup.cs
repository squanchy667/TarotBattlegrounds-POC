#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TarotBattlegrounds.UI;

/// <summary>
/// T750: migrates every Button in the active scene to the design-system chrome —
/// removes legacy TarotButton/ButtonMicroFeedback styling, attaches IgniteButton,
/// and binds the notched kit sprites (idle bronze / ignited ember / press flash;
/// danger buttons idle blood). DESIGN.md §10.5: one interaction component, no scaling.
/// Run via menu: Tools/Game/Setup Styled Buttons
/// </summary>
public class ButtonStylingSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Styled Buttons")]
    public static void SetupStyledButtons()
    {
        string report = MigrateActiveScene();
        EditorUtility.DisplayDialog("Setup Styled Buttons", report, "OK");
    }

    /// <summary>
    /// Dialog-free core (also invoked programmatically, e.g. over MCP where a
    /// modal would hang the editor). Returns the migration report.
    /// </summary>
    public static string MigrateActiveScene()
    {
        UiSprites sprites = UiSprites.Instance;
        if (sprites == null || sprites.ButtonBronze == null)
            return "ABORT: Resources/UiSprites.asset is missing or unwired.";

        Button[] allButtons = FindObjectsOfType<Button>(true);
        if (allButtons.Length == 0)
            return "No Button components found in the current scene.";

        int migrated = 0;
        int undersized = 0;
        var undersizedNames = new System.Text.StringBuilder();

        foreach (Button button in allButtons)
        {
            Undo.RegisterCompleteObjectUndo(button.gameObject, "Setup Styled Buttons");
            MigrateButton(button, sprites);
            migrated++;

            // §11: every tappable ≥ MinTouchTarget. Report — sizing is fixed in the
            // per-screen restyle phases, not silently here.
            Rect r = ((RectTransform)button.transform).rect;
            if (r.width < Tokens.MinTouchTarget || r.height < Tokens.MinTouchTarget)
            {
                undersized++;
                undersizedNames.AppendLine(
                    $"  {button.gameObject.name}: {r.width:F0}x{r.height:F0}");
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string report = $"Migrated {migrated} buttons to IgniteButton.";
        if (undersized > 0)
            report += $"\n{undersized} below {Tokens.MinTouchTarget}px touch target:\n{undersizedNames}";
        Debug.Log($"[ButtonStylingSetup] {report}");
        return report;
    }

    private static void MigrateButton(Button button, UiSprites sprites)
    {
        GameObject go = button.gameObject;
        Transform t = button.transform;

        // Drop legacy styling components — by type name so this tool keeps compiling
        // after the legacy classes are deleted; then clear any missing-script slots.
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            if (typeName == "TarotButton" || typeName == "ButtonMicroFeedback")
                Undo.DestroyObjectImmediate(mb);
        }
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        // Drop legacy overlay children (gradient BG copy / translucent border fill).
        Transform border = t.Find("ButtonBorder");
        if (border != null) Undo.DestroyObjectImmediate(border.gameObject);
        Transform ripple = t.Find("RippleEffect");
        if (ripple != null) Undo.DestroyObjectImmediate(ripple.gameObject);

        // Background image: prefer a "ButtonBG" child if the legacy hierarchy made one,
        // else the button's own Image.
        Image bg = null;
        Transform bgChild = t.Find("ButtonBG");
        if (bgChild != null) bg = bgChild.GetComponent<Image>();
        if (bg == null) bg = go.GetComponent<Image>();

        bool danger = IsDanger(go.name);
        Sprite idle = danger ? sprites.ButtonBlood : sprites.ButtonBronze;
        if (bg != null)
        {
            UiSprites.ApplySliced(bg, idle);
            bg.raycastTarget = true;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        var ignite = go.GetComponent<IgniteButton>();
        if (ignite == null) ignite = Undo.AddComponent<IgniteButton>(go);

        var so = new SerializedObject(ignite);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("idleSprite").objectReferenceValue = idle;
        so.FindProperty("ignitedSprite").objectReferenceValue = sprites.ButtonEmber;
        so.FindProperty("pressedSprite").objectReferenceValue = sprites.ButtonBronzePressed;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.Bone;
        so.ApplyModifiedProperties();

        button.transition = Selectable.Transition.None;
        if (label != null)
        {
            label.color = Tokens.Bone;
            if (FontRefs.Instance != null && FontRefs.Instance.Label != null)
                label.font = FontRefs.Instance.Label;
        }
    }

    private static bool IsDanger(string buttonName)
    {
        string name = buttonName.ToLower();
        return name.Contains("danger") || name.Contains("delete") || name.Contains("quit")
            || name.Contains("exit") || name.Contains("cancel");
    }
}
#endif
