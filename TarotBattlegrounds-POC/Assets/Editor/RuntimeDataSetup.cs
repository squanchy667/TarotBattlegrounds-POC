#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script to add RuntimeDataLoader to the Game scene and wire up DataConfig.
/// Run via menu: Tools/Game/Setup Runtime Data Loader
/// </summary>
public class RuntimeDataSetup
{
    private const string DataConfigPath = "Assets/Data/DataConfig.asset";

    [MenuItem("Tools/Game/Setup Runtime Data Loader")]
    public static void SetupRuntimeDataLoader()
    {
        // Ensure we're in the Game scene
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Game"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene",
                "The active scene is not Game. Open Game scene first?\n\n" +
                "(This will save the current scene.)",
                "Open Game", "Cancel"))
                return;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        }

        // Load DataConfig asset
        DataConfig dataConfig = AssetDatabase.LoadAssetAtPath<DataConfig>(DataConfigPath);
        if (dataConfig == null)
        {
            EditorUtility.DisplayDialog("Missing DataConfig",
                $"Could not find DataConfig asset at:\n{DataConfigPath}\n\n" +
                "Please create it via Assets > Create > Game > Data Config.",
                "OK");
            return;
        }

        // Find or create RuntimeDataLoader
        RuntimeDataLoader loader = Object.FindObjectOfType<RuntimeDataLoader>();
        if (loader == null)
        {
            GameObject loaderObj = new GameObject("RuntimeDataLoader");
            Undo.RegisterCreatedObjectUndo(loaderObj, "Create RuntimeDataLoader");
            loader = loaderObj.AddComponent<RuntimeDataLoader>();
        }

        // Assign DataConfig
        Undo.RecordObject(loader, "Assign DataConfig");
        loader.dataConfig = dataConfig;
        EditorUtility.SetDirty(loader);

        // Set Script Execution Order: RuntimeDataLoader before CardPoolInitializer
        SetExecutionOrder<RuntimeDataLoader>(-100);
        SetExecutionOrder<CardPoolInitializer>(0);

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[RuntimeDataSetup] RuntimeDataLoader added to scene with DataConfig assigned.");
        Debug.Log($"[RuntimeDataSetup] Data URL: {dataConfig.dataBaseUrl}");
        Debug.Log($"[RuntimeDataSetup] Runtime loading: {dataConfig.enableRuntimeLoading}");
        Debug.Log($"[RuntimeDataSetup] Fallback: {dataConfig.fallbackToBuiltIn}");

        EditorUtility.DisplayDialog("Setup Complete",
            "RuntimeDataLoader has been added to the Game scene.\n\n" +
            $"Data URL: {dataConfig.dataBaseUrl}\n" +
            $"Runtime Loading: {dataConfig.enableRuntimeLoading}\n" +
            $"Fallback to Built-in: {dataConfig.fallbackToBuiltIn}\n\n" +
            "Script Execution Order:\n" +
            "  RuntimeDataLoader: -100\n" +
            "  CardPoolInitializer: 0",
            "OK");
    }

    private static void SetExecutionOrder<T>( int order) where T : MonoBehaviour
    {
        MonoScript script = null;
        foreach (var ms in MonoImporter.GetAllRuntimeMonoScripts())
        {
            if (ms.GetClass() == typeof(T))
            {
                script = ms;
                break;
            }
        }

        if (script != null && MonoImporter.GetExecutionOrder(script) != order)
        {
            MonoImporter.SetExecutionOrder(script, order);
            Debug.Log($"[RuntimeDataSetup] Set execution order for {typeof(T).Name} to {order}");
        }
    }
}
#endif
