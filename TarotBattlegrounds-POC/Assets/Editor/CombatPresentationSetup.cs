#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using System.Text;
using TarotBattlegrounds.Combat.Animator;
using TarotBattlegrounds.Combat.VFX;
using TarotBattlegrounds.UI;

/// <summary>
/// WO-04: Canonical Game-scene combat presentation setup.
/// Supersedes ad-hoc CombatArenaSetup usage for Game combat wiring —
/// builds CombatRoot hierarchy, wires CombatAnimator/VFX/FloatingNumbers/Log,
/// and embeds CombatArenaVisual under CombatRoot.
/// Dialog-free core for MCP: SetupCombatPresentationCore().
///
/// FIX-ADDENDUM 1 (2026-07-16): do not author a card-visual prefab (leave field null → CreateFromCode);
/// CombatLogEntry prefab; fixed ≥88px Skip/Speed; orphan CombatArenaVisual cleanup.
/// </summary>
public static class CombatPresentationSetup
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string PrefabDir = "Assets/Prefabs/Combat";
    private const string LogEntryPrefabPath = PrefabDir + "/CombatLogEntry.prefab";

    [MenuItem("Tools/Game/Setup Combat Presentation (WO-04)")]
    public static void SetupCombatPresentationMenu()
    {
        if (!EditorUtility.DisplayDialog("WO-04: Combat Presentation",
            "Wire combat presentation stack into Game.unity:\n" +
            "  - CombatRoot / CombatPanel / containers / Skip / Speed\n" +
            "  - CombatAnimator + VFXManager + FloatingNumberManager\n" +
            "  - CombatLogUI + CombatLogEntry prefab → GameUIManager\n" +
            "  - CombatArenaVisual (under CombatRoot only)\n" +
            "  - cardVisualPrefab left null (CreateFromCode)\n\nContinue?",
            "Setup", "Cancel"))
            return;

        string report = SetupCombatPresentationCore();
        EditorUtility.DisplayDialog("WO-04 Complete", report, "OK");
    }

    /// <summary>Dialog-free core for MCP / batch. Opens Game scene if needed and marks dirty.</summary>
    public static string SetupCombatPresentationCore()
    {
        var sb = new StringBuilder();
        sb.AppendLine("WO-04 CombatPresentationSetup (addendum-1)");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == null || !scene.name.Contains("Game"))
        {
            if (!File.Exists(ScenePath))
                return "ABORT: Game scene missing at " + ScenePath;
            scene = EditorSceneManager.OpenScene(ScenePath);
            sb.AppendLine("Opened " + ScenePath);
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            return "ABORT: No Canvas in Game scene.";

        Transform safe = EnsureSafeArea(canvas.transform);
        sb.AppendLine("SafeArea: " + safe.name);

        // Remove prior CombatRoot for idempotent re-run
        Transform oldRoot = safe.Find("CombatRoot");
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot.gameObject);
            sb.AppendLine("Removed previous CombatRoot");
        }

        // Also remove orphan CombatArenaRoot from legacy CombatArenaSetup (canvas-level)
        Transform legacyArena = canvas.transform.Find("CombatArenaRoot");
        if (legacyArena != null)
        {
            Object.DestroyImmediate(legacyArena.gameObject);
            sb.AppendLine("Removed legacy CombatArenaRoot (superseded)");
        }

        GameObject combatRoot = new GameObject("CombatRoot");
        combatRoot.transform.SetParent(safe, false);
        RectTransform rootRt = combatRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);
        sb.AppendLine("Created CombatRoot under SafeArea");

        // --- CombatPanel (starts inactive; not opaque full-screen blocker) ---
        GameObject combatPanel = new GameObject("CombatPanel");
        combatPanel.transform.SetParent(combatRoot.transform, false);
        RectTransform panelRt = combatPanel.AddComponent<RectTransform>();
        StretchFull(panelRt);
        Image panelBg = combatPanel.AddComponent<Image>();
        // Clean combat screen: heavy dim so shop chrome behind is blocked out;
        // env still still peeks through slightly under the cards.
        panelBg.color = Tokens.WithAlpha(Tokens.Ash, 0.82f);
        panelBg.raycastTarget = true;
        combatPanel.SetActive(false);

        // Enemy board (top) / Your board (bottom) — clearer layout
        RectTransform attackerContainer = CreateBoardContainer(combatPanel.transform, "AttackerContainer",
            new Vector2(0.04f, 0.54f), new Vector2(0.72f, 0.86f));

        RectTransform defenderContainer = CreateBoardContainer(combatPanel.transform, "DefenderContainer",
            new Vector2(0.04f, 0.16f), new Vector2(0.72f, 0.48f));

        // Status strip
        TMP_Text statusText = CreateLabel(combatPanel.transform, "StatusText", "COMBAT",
            new Vector2(0.1f, 0.90f), new Vector2(0.9f, 0.98f), Tokens.TextH3, Tokens.BoneBright);

        TMP_Text attackerName = CreateLabel(combatPanel.transform, "AttackerNameText", "ENEMY",
            new Vector2(0.04f, 0.86f), new Vector2(0.5f, 0.90f), Tokens.TextCaption, Tokens.Blood);

        TMP_Text defenderName = CreateLabel(combatPanel.transform, "DefenderNameText", "YOU",
            new Vector2(0.04f, 0.12f), new Vector2(0.5f, 0.16f), Tokens.TextCaption, Tokens.BronzeBright);

        TMP_Text resultText = CreateLabel(combatPanel.transform, "ResultText", "",
            new Vector2(0.08f, 0.42f), new Vector2(0.68f, 0.58f), Tokens.TextH1, Tokens.BronzeBright);

        // F3: Skip/Speed — fixed size ≥ Tokens.MinTouchTarget (point anchors; no inert LayoutElement)
        float btnW = 220f;
        float btnH = Tokens.MinTouchTarget + 8f; // 96
        Button skipBtn;
        TMP_Text skipLabel;
        CreateControlButton(combatPanel.transform, "SkipButton", "Skip",
            new Vector2(0.08f, 0.02f), Vector2.zero, btnW, btnH, out skipBtn, out skipLabel);

        Button speedBtn;
        TMP_Text speedLabel;
        CreateControlButton(combatPanel.transform, "SpeedButton", "1x",
            new Vector2(0.08f, 0.02f), new Vector2(btnW + Tokens.Space2, 0f), btnW, btnH,
            out speedBtn, out speedLabel);
        sb.AppendLine($"Skip/Speed fixed size {btnW}x{btnH} (≥ MinTouchTarget={Tokens.MinTouchTarget})");

        // Combat log panel (right rail — narrower so boards stay readable)
        GameObject logPanel = new GameObject("CombatLogPanel");
        logPanel.transform.SetParent(combatPanel.transform, false);
        RectTransform logRt = logPanel.AddComponent<RectTransform>();
        logRt.anchorMin = new Vector2(0.74f, 0.16f);
        logRt.anchorMax = new Vector2(0.98f, 0.86f);
        logRt.offsetMin = Vector2.zero;
        logRt.offsetMax = Vector2.zero;
        Image logBg = logPanel.AddComponent<Image>();
        logBg.color = Tokens.WithAlpha(Tokens.CharredWood, 0.78f);
        logBg.raycastTarget = false;

        GameObject logViewport = new GameObject("Viewport");
        logViewport.transform.SetParent(logPanel.transform, false);
        RectTransform vpRt = logViewport.AddComponent<RectTransform>();
        StretchFull(vpRt);
        vpRt.offsetMin = new Vector2(Tokens.Space1, Tokens.Space1);
        vpRt.offsetMax = new Vector2(-Tokens.Space1, -Tokens.Space1);
        logViewport.AddComponent<RectMask2D>();
        Image vpImg = logViewport.AddComponent<Image>();
        vpImg.color = Tokens.WithAlpha(Tokens.Ash, 0.01f);
        vpImg.raycastTarget = false;

        GameObject logContent = new GameObject("Content");
        logContent.transform.SetParent(logViewport.transform, false);
        RectTransform contentRt = logContent.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        var vlg = logContent.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = logContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = logPanel.AddComponent<ScrollRect>();
        scroll.viewport = vpRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;

        // F2: log entry prefab required by CombatLogUI
        GameObject logEntryPrefab = EnsureLogEntryPrefab(sb);

        CombatLogUI combatLog = logPanel.AddComponent<CombatLogUI>();
        SerializedObject logSo = new SerializedObject(combatLog);
        SetObj(logSo, "combatLogPanel", logPanel);
        SetObj(logSo, "logEntriesContainer", logContent.transform);
        SetObj(logSo, "scrollRect", scroll);
        SetObj(logSo, "panelBackground", logBg);
        if (logEntryPrefab != null)
            SetObj(logSo, "logEntryPrefab", logEntryPrefab);
        logSo.ApplyModifiedPropertiesWithoutUndo();
        sb.AppendLine("CombatLogUI on CombatLogPanel (logEntryPrefab assigned)");

        // --- CombatAnimator on CombatRoot ---
        // F1: leave cardVisualPrefab null so CreateFromCode builds real card visuals
        CombatAnimator animator = combatRoot.AddComponent<CombatAnimator>();
        SerializedObject animSo = new SerializedObject(animator);
        SetObj(animSo, "combatPanel", panelRt);
        SetObj(animSo, "attackerBoardContainer", attackerContainer);
        SetObj(animSo, "defenderBoardContainer", defenderContainer);
        SetObj(animSo, "attackerNameText", attackerName);
        SetObj(animSo, "defenderNameText", defenderName);
        SetObj(animSo, "statusText", statusText);
        SetObj(animSo, "resultText", resultText);
        SetObj(animSo, "skipButton", skipBtn);
        SetObj(animSo, "speedButton", speedBtn);
        SetObj(animSo, "speedButtonText", speedLabel);
        // deliberately NOT assigning cardVisualPrefab
        animSo.ApplyModifiedPropertiesWithoutUndo();
        sb.AppendLine("CombatAnimator wired (cardVisualPrefab=null → CreateFromCode)");

        // --- Arena visual under CombatRoot (canonical) ---
        CombatArenaVisual arena = EnsureArenaVisual(combatRoot.transform, sb);
        if (arena != null)
        {
            animSo = new SerializedObject(animator);
            SetObj(animSo, "arenaVisual", arena);
            animSo.ApplyModifiedPropertiesWithoutUndo();
            sb.AppendLine("CombatArenaVisual → CombatAnimator.arenaVisual");
        }

        // F4: destroy every CombatArenaVisual not under CombatRoot; end state exactly one
        int destroyed = 0;
        foreach (var v in Object.FindObjectsOfType<CombatArenaVisual>(true))
        {
            if (v == null) continue;
            if (v.transform.IsChildOf(combatRoot.transform))
                continue;
            string path = GetHierarchyPath(v.transform);
            sb.AppendLine("Destroyed orphan CombatArenaVisual: " + path);
            Object.DestroyImmediate(v.gameObject);
            destroyed++;
        }
        int remaining = Object.FindObjectsOfType<CombatArenaVisual>(true).Length;
        sb.AppendLine($"Arena cleanup: destroyed={destroyed}, remaining={remaining} (expect 1 under CombatRoot)");

        // --- VFXManager + FloatingNumberManager on CombatRoot ---
        if (combatRoot.GetComponent<VFXManager>() == null)
            combatRoot.AddComponent<VFXManager>();
        FloatingNumberManager fnm = combatRoot.GetComponent<FloatingNumberManager>();
        if (fnm == null)
            fnm = combatRoot.AddComponent<FloatingNumberManager>();
        SerializedObject fnSo = new SerializedObject(fnm);
        SetObj(fnSo, "parentCanvas", canvas);
        fnSo.ApplyModifiedPropertiesWithoutUndo();
        sb.AppendLine("VFXManager + FloatingNumberManager on CombatRoot");

        // --- Wire GameUIManager.combatLogUI ---
        GameUIManager gui = Object.FindObjectOfType<GameUIManager>(true);
        if (gui != null)
        {
            SerializedObject guiSo = new SerializedObject(gui);
            var prop = guiSo.FindProperty("combatLogUI");
            if (prop != null)
            {
                prop.objectReferenceValue = combatLog;
                guiSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gui);
                sb.AppendLine("GameUIManager.combatLogUI assigned");
            }
            else
            {
                sb.AppendLine("WARN: GameUIManager.combatLogUI property not found");
            }
        }
        else
        {
            sb.AppendLine("WARN: No GameUIManager in scene — combat log events may not subscribe");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.SetDirty(combatRoot);
        sb.AppendLine("Scene marked dirty — Fable must save Game.unity");
        sb.AppendLine("DONE");
        return sb.ToString();
    }

    private static RectTransform CreateBoardContainer(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = Tokens.Space2;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        return rt;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string text,
        Vector2 aMin, Vector2 aMax, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        if (FontRefs.Instance != null && FontRefs.Instance.Body != null)
            tmp.font = FontRefs.Instance.Body;
        return tmp;
    }

    /// <summary>
    /// F3: point-anchored fixed-size control button (LayoutElement is useless without a LayoutGroup).
    /// </summary>
    private static void CreateControlButton(Transform parent, string name, string label,
        Vector2 anchorPoint, Vector2 anchoredPos, float width, float height,
        out Button button, out TMP_Text labelTmp)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPoint;
        rt.anchorMax = anchorPoint;
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        Sprite bronze = UiSprites.Instance != null ? UiSprites.Instance.ButtonBronze : null;
        if (bronze != null)
            UiSprites.ApplySliced(img, bronze);
        else
            img.color = Tokens.Bronze;
        img.raycastTarget = true;

        button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = img;

        GameObject labelGo = new GameObject("Text (TMP)");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.AddComponent<RectTransform>();
        StretchFull(lrt);
        labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = Tokens.TextLabel;
        labelTmp.color = Tokens.BoneBright;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;
        if (FontRefs.Instance != null && FontRefs.Instance.Display != null)
            labelTmp.font = FontRefs.Instance.Display;
        else if (FontRefs.Instance != null && FontRefs.Instance.Body != null)
            labelTmp.font = FontRefs.Instance.Body;

        var ignite = go.AddComponent<IgniteButton>();
        SerializedObject iso = new SerializedObject(ignite);
        iso.FindProperty("background").objectReferenceValue = img;
        iso.FindProperty("idleSprite").objectReferenceValue = bronze;
        iso.FindProperty("ignitedSprite").objectReferenceValue =
            UiSprites.Instance != null ? UiSprites.Instance.ButtonEmber : null;
        iso.FindProperty("pressedSprite").objectReferenceValue =
            UiSprites.Instance != null ? UiSprites.Instance.ButtonBronzePressed : null;
        iso.FindProperty("label").objectReferenceValue = labelTmp;
        iso.FindProperty("idleTone").enumValueIndex = (int)IgniteButton.LabelTone.BoneBright;
        iso.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// F2: active-root TMP_Text prefab for CombatLogUI log lines.
    /// </summary>
    private static GameObject EnsureLogEntryPrefab(StringBuilder sb)
    {
        if (!Directory.Exists(PrefabDir))
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
        }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(LogEntryPrefabPath);
        if (existing != null)
        {
            sb.AppendLine("Log entry prefab exists: " + LogEntryPrefabPath);
            return existing;
        }

        GameObject temp = new GameObject("CombatLogEntry");
        RectTransform rt = temp.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 24f);

        var tmp = temp.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.fontSize = Tokens.TextCaption;
        tmp.color = Tokens.BoneBright;
        if (FontRefs.Instance != null && FontRefs.Instance.Body != null)
            tmp.font = FontRefs.Instance.Body;

        LayoutElement le = temp.AddComponent<LayoutElement>();
        le.minHeight = 24f;
        le.preferredHeight = 24f;
        le.flexibleWidth = 1f;

        // ACTIVE root — CombatLogUI never re-activates clones
        temp.SetActive(true);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, LogEntryPrefabPath);
        Object.DestroyImmediate(temp);
        sb.AppendLine("Created log entry prefab: " + LogEntryPrefabPath);
        return prefab;
    }

    /// <summary>
    /// Lightweight arena chrome under CombatRoot (replaces canvas-level CombatArenaSetup for Game).
    /// </summary>
    private static CombatArenaVisual EnsureArenaVisual(Transform combatRoot, StringBuilder sb)
    {
        GameObject root = new GameObject("CombatArenaRoot");
        root.transform.SetParent(combatRoot, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        StretchFull(rootRect);
        CombatArenaVisual arenaVisual = root.AddComponent<CombatArenaVisual>();

        GameObject bgObj = new GameObject("ArenaBackground");
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        StretchFull(bgRect);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Tokens.WithAlpha(Tokens.Ash, 0.9f);
        bgImage.raycastTarget = false;
        bgObj.SetActive(false);

        GameObject dividerObj = new GameObject("DividerLine");
        dividerObj.transform.SetParent(root.transform, false);
        RectTransform dividerRect = dividerObj.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.05f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.95f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(0, 2f);
        Image dividerImage = dividerObj.AddComponent<Image>();
        dividerImage.color = Tokens.WithAlpha(Tokens.BronzeBright, 0.6f);
        dividerImage.raycastTarget = false;
        dividerObj.SetActive(false);

        GameObject trailObj = new GameObject("AttackTrailLine");
        trailObj.transform.SetParent(root.transform, false);
        RectTransform trailRect = trailObj.AddComponent<RectTransform>();
        trailRect.sizeDelta = new Vector2(100f, 3f);
        Image trailImage = trailObj.AddComponent<Image>();
        trailImage.color = Tokens.WithAlpha(Tokens.Ember, 0.8f);
        trailImage.raycastTarget = false;
        trailObj.SetActive(false);

        SerializedObject so = new SerializedObject(arenaVisual);
        SetObj(so, "dividerLine", dividerImage);
        SetObj(so, "attackTrailLine", trailImage);
        SetObj(so, "arenaBackground", bgImage);
        so.ApplyModifiedPropertiesWithoutUndo();

        sb.AppendLine("CombatArenaRoot under CombatRoot (canonical)");
        return arenaVisual;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "(null)";
        var parts = new System.Collections.Generic.List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static Transform EnsureSafeArea(Transform canvasTransform)
    {
        Transform existing = FindDeep(canvasTransform, "SafeArea");
        if (existing != null)
            return existing;

        GameObject safe = new GameObject("SafeArea");
        safe.transform.SetParent(canvasTransform, false);
        RectTransform rt = safe.AddComponent<RectTransform>();
        StretchFull(rt);
        if (safe.GetComponent<SafeAreaHandler>() == null)
            safe.AddComponent<SafeAreaHandler>();
        return safe.transform;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetObj(SerializedObject so, string prop, Object value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p != null)
            p.objectReferenceValue = value;
    }
}
#endif
