#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script to create a Player prefab from an existing Player in the Game scene
/// and wire it to GameManager's playerPrefab field.
/// Run via menu: Tools/Game/Setup Player Prefab
/// </summary>
public class PlayerPrefabSetup : EditorWindow
{
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";

    [MenuItem("Tools/Game/Setup Player Prefab")]
    public static void SetupPlayerPrefab()
    {
        // Ensure we're in the Game scene
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Game"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not the Game scene. Open it first?\n\n" +
                "(This will save the current scene.)",
                "Open Game Scene", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }

        // Find GameManager
        GameManager gameManager = Object.FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No GameManager found in the Game scene.", "OK");
            return;
        }

        // Check if prefab already exists
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existingPrefab != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                "Player.prefab already exists at " + PrefabPath + ".\n\nRecreate it?",
                "Recreate", "Just Wire It"))
            {
                // Just wire the existing prefab
                WirePrefab(gameManager, existingPrefab);
                return;
            }
        }

        // Find a Player in the scene to use as template
        if (gameManager.players == null || gameManager.players.Count == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "GameManager has no Player objects in its players list.\n" +
                "Add at least one Player to the scene first.", "OK");
            return;
        }

        Player templatePlayer = gameManager.players[0];
        if (templatePlayer == null)
        {
            EditorUtility.DisplayDialog("Error",
                "The first Player in GameManager.players is null.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Create Player Prefab",
            $"This will:\n" +
            $"1. Create a prefab from '{templatePlayer.gameObject.name}'\n" +
            $"2. Save it to {PrefabPath}\n" +
            $"3. Wire it to GameManager.playerPrefab\n\nContinue?",
            "Create", "Cancel"))
            return;

        // Ensure Prefabs folder exists
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Create the prefab from the template player
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(templatePlayer.gameObject, PrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Failed to create prefab. Check the console for details.", "OK");
            return;
        }

        Debug.Log($"[PlayerPrefabSetup] Created Player prefab at {PrefabPath}");

        // Wire it to GameManager
        WirePrefab(gameManager, prefab);

        EditorUtility.DisplayDialog("Player Prefab Created",
            $"Player prefab saved to {PrefabPath}\n" +
            $"GameManager.playerPrefab wired.\n\n" +
            $"The game can now dynamically spawn up to 8 players.\n" +
            $"Save the scene to keep changes.", "OK");
    }

    private static void WirePrefab(GameManager gameManager, GameObject prefab)
    {
        Undo.RecordObject(gameManager, "Wire Player Prefab");

        SerializedObject so = new SerializedObject(gameManager);
        SerializedProperty prop = so.FindProperty("playerPrefab");
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PlayerPrefabSetup] GameManager.playerPrefab wired successfully.");
        }
        else
        {
            Debug.LogError("[PlayerPrefabSetup] Could not find 'playerPrefab' field on GameManager. " +
                "Make sure the field exists as [SerializeField] private GameObject playerPrefab;");
        }
    }
}
#endif
