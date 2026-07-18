#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using TarotBattlegrounds.UI;

/// <summary>
/// WO-07: Wire card_frame_t1–t5, nameplate, and chips into Resources/UiSprites.
/// Dialog-free core for MCP. Does not edit scenes.
/// </summary>
public static class UiSpritesCardKitSetup
{
    private const string AssetPath = "Assets/Resources/UiSprites.asset";
    private const string SpriteDir = "Assets/Art/UI/Sprites";

    [MenuItem("Tools/Game/Wire UiSprites Card Kit (WO-07)")]
    public static void MenuWire()
    {
        if (!EditorUtility.DisplayDialog("WO-07", "Wire card frames + chips into UiSprites.asset?", "Wire", "Cancel"))
            return;
        EditorUtility.DisplayDialog("WO-07", WireCardKitCore(), "OK");
    }

    public static string WireCardKitCore()
    {
        var sb = new StringBuilder();
        sb.AppendLine("WO-07 UiSpritesCardKitSetup");

        UiSprites asset = AssetDatabase.LoadAssetAtPath<UiSprites>(AssetPath);
        if (asset == null)
            return "ABORT: missing " + AssetPath;

        SerializedObject so = new SerializedObject(asset);
        Set(so, "cardFrameT1Stone", Load("card_frame_t1_stone.png", sb));
        Set(so, "cardFrameT2Bronze", Load("card_frame_t2_bronze.png", sb));
        Set(so, "cardFrameT3Gold", Load("card_frame_t3_gold.png", sb));
        Set(so, "cardFrameT4Ether", Load("card_frame_t4_ether.png", sb));
        Set(so, "cardFrameT5Mythic", Load("card_frame_t5_mythic.png", sb));
        Set(so, "cardNameplate", Load("card_nameplate.png", sb));
        Set(so, "chipAttack", Load("chip_attack.png", sb));
        Set(so, "chipHealth", Load("chip_health.png", sb));
        Set(so, "chipCost", Load("chip_cost.png", sb));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        sb.AppendLine("DONE — UiSprites card kit wired");
        return sb.ToString();
    }

    private static Sprite Load(string file, StringBuilder sb)
    {
        string path = SpriteDir + "/" + file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            // DESIGN §10.4: 9-slice — leave border as authored on asset if already set
            if (dirty) importer.SaveAndReimport();
        }
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) { sp = s; break; }
        }
        sb.AppendLine(sp != null ? "OK " + file : "WARN missing " + file);
        return sp;
    }

    private static void Set(SerializedObject so, string prop, Sprite sp)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = sp;
    }
}
#endif
