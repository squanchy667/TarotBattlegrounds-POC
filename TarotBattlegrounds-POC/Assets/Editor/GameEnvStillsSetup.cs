#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;
using TarotBattlegrounds.UI;

/// <summary>
/// WO-09: Wire phase env stills (shop_bg / board_env) and game-over victory/lose stills
/// into the Game scene. Dialog-free core for MCP.
/// Stills only — no videos this cycle (R9).
/// </summary>
public static class GameEnvStillsSetup
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string ShopStillPath = "Assets/Art/Env/Shop/shop_bg.jpg";
    private const string BoardStillPath = "Assets/Art/Env/Board/board_env.jpg";
    private const string VictoryStillPath = "Assets/Art/Env/GameOver/victory.jpg";
    private const string LoseStillPath = "Assets/Art/Env/GameOver/lose.jpg";

    [MenuItem("Tools/Game/Setup Game Env Stills (WO-09)")]
    public static void SetupMenu()
    {
        if (!EditorUtility.DisplayDialog("WO-09: Game Env Stills",
            "Wire:\n  - shop_bg → recruit\n  - board_env → combat\n  - victory/lose stills on GameOverUI\n\nContinue?",
            "Setup", "Cancel"))
            return;
        string report = SetupGameEnvStillsCore();
        EditorUtility.DisplayDialog("WO-09 Complete", report, "OK");
    }

    /// <summary>Dialog-free core for MCP / Fable.</summary>
    public static string SetupGameEnvStillsCore()
    {
        var sb = new StringBuilder();
        sb.AppendLine("WO-09 GameEnvStillsSetup");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == null || !scene.name.Contains("Game"))
        {
            if (!File.Exists(ScenePath))
                return "ABORT: Game scene missing";
            scene = EditorSceneManager.OpenScene(ScenePath);
            sb.AppendLine("Opened " + ScenePath);
        }

        Sprite shop = LoadSprite(ShopStillPath, sb);
        Sprite board = LoadSprite(BoardStillPath, sb);
        Sprite victory = LoadSprite(VictoryStillPath, sb);
        Sprite lose = LoadSprite(LoseStillPath, sb);

        // BackgroundController — ensure present on Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
            return "ABORT: No Canvas";

        BackgroundController bg = Object.FindObjectOfType<BackgroundController>(true);
        if (bg == null)
        {
            bg = canvas.gameObject.AddComponent<BackgroundController>();
            sb.AppendLine("Added BackgroundController on Canvas");
        }

        // Full-screen BG under BG_Root (repair zero-size BG_Base from prior setup)
        bg.EnsureBackgroundBaseLayer();
        if (shop != null)
            bg.SetStillSprite(shop);
        sb.AppendLine("BG pipeline ensured + shop still applied");

        // Also import Resources/Env copies for runtime fallback
        EnsureResourceCopy(ShopStillPath, "Assets/Resources/Env/shop_bg.jpg", sb);
        EnsureResourceCopy(BoardStillPath, "Assets/Resources/Env/board_env.jpg", sb);

        GameUIManager gui = Object.FindObjectOfType<GameUIManager>(true);
        if (gui != null)
        {
            SerializedObject gso = new SerializedObject(gui);
            SetSprite(gso, "recruitEnvStill", shop);
            SetSprite(gso, "combatEnvStill", board);
            var bcp = gso.FindProperty("backgroundController");
            if (bcp != null && bcp.objectReferenceValue == null)
                bcp.objectReferenceValue = bg;
            gso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gui);
            sb.AppendLine("GameUIManager recruit/combat stills assigned");
        }
        else
            sb.AppendLine("WARN: GameUIManager missing");

        // GameOver stills
        GameOverUI go = Object.FindObjectOfType<GameOverUI>(true);
        if (go != null)
        {
            Image stillImg = EnsureResultStillImage(go, canvas, sb);
            SerializedObject goso = new SerializedObject(go);
            SetObj(goso, "resultStillImage", stillImg);
            SetSprite(goso, "victoryStill", victory);
            SetSprite(goso, "loseStill", lose);
            goso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
            sb.AppendLine("GameOverUI victory/lose stills assigned");
        }
        else
            sb.AppendLine("WARN: GameOverUI missing");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        sb.AppendLine("Scene dirty — Fable/Ofek save Game.unity");
        sb.AppendLine("DONE");
        return sb.ToString();
    }

    private static Image EnsureResultStillImage(GameOverUI go, Canvas canvas, StringBuilder sb)
    {
        Transform existing = null;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "ResultStill") { existing = t; break; }
        }
        if (existing != null)
            return existing.GetComponent<Image>();

        // Place under game-over panel root if possible
        Transform parent = go.transform;
        GameObject stillGo = new GameObject("ResultStill");
        stillGo.transform.SetParent(parent, false);
        stillGo.transform.SetAsFirstSibling();
        RectTransform rt = stillGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = stillGo.AddComponent<Image>();
        img.color = Tokens.Ash;
        img.raycastTarget = false;
        stillGo.SetActive(false);
        sb.AppendLine("Created ResultStill under GameOverUI");
        return img;
    }

    private static void EnsureResourceCopy(string src, string dst, StringBuilder sb)
    {
        try
        {
            string dir = Path.GetDirectoryName(dst);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(src))
            {
                File.Copy(src, dst, true);
                AssetDatabase.ImportAsset(dst);
                var imp = AssetImporter.GetAtPath(dst) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.mipmapEnabled = false;
                    imp.SaveAndReimport();
                }
                sb.AppendLine("Resource copy: " + dst);
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine("WARN resource copy: " + e.Message);
        }
    }

    private static Sprite LoadSprite(string path, StringBuilder sb)
    {
        // Ensure sprite import
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            sb.AppendLine("Reimported as Sprite: " + path);
        }
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            // Some jpegs import as Texture2D only — try sub-asset
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var o in all)
            {
                if (o is Sprite s) { sp = s; break; }
            }
        }
        if (sp == null)
            sb.AppendLine("WARN: missing sprite " + path);
        else
            sb.AppendLine("Loaded " + path);
        return sp;
    }

    private static void SetSprite(SerializedObject so, string prop, Sprite sp)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = sp;
    }

    private static void SetObj(SerializedObject so, string prop, Object o)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = o;
    }
}
#endif
