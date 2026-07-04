#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor script to set up the multi-layered background system in Game and MainMenu scenes.
/// Creates background layers as first children of the Canvas (behind all other UI).
/// Run via menu: Tools/Game/Setup Background
/// </summary>
public class BackgroundSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Background")]
    public static void SetupBackground()
    {
        if (!EditorUtility.DisplayDialog("Setup Background",
            "This will create background layers (Base, Gradient, Vignette, Particles) " +
            "as the first children of the Canvas and add BackgroundController.\n\n" +
            "Works in both Game and MainMenu scenes.\n\nContinue?",
            "Create", "Cancel"))
            return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = EditorUiFactory.CreateCanvas().GetComponent<Canvas>();
        }

        // Remove old background layers if they exist
        RemoveOldBackgroundLayers(canvas.transform);

        // Create background layers as first children (behind everything)
        GameObject bgBase = CreateFullscreenImage(canvas.transform, "BG_Base", new Color(0.05f, 0.03f, 0.10f));
        bgBase.transform.SetAsFirstSibling();

        GameObject bgGradient = CreateFullscreenImage(canvas.transform, "BG_Gradient", Color.white);
        bgGradient.transform.SetSiblingIndex(1);

        GameObject bgVignette = CreateFullscreenImage(canvas.transform, "BG_Vignette", Color.white);
        bgVignette.transform.SetSiblingIndex(2);

        GameObject bgParticles = CreateParticleObject(canvas.transform, "BG_Particles");
        bgParticles.transform.SetSiblingIndex(3);

        // Add or find BackgroundController on the Canvas
        BackgroundController controller = canvas.GetComponent<BackgroundController>();
        if (controller == null)
        {
            controller = canvas.gameObject.AddComponent<BackgroundController>();
        }

        // Wire references via SerializedObject
        Undo.RegisterCompleteObjectUndo(controller, "Setup Background");
        SerializedObject so = new SerializedObject(controller);

        so.FindProperty("backgroundBase").objectReferenceValue = bgBase.GetComponent<Image>();
        so.FindProperty("gradientOverlay").objectReferenceValue = bgGradient.GetComponent<Image>();
        so.FindProperty("vignetteOverlay").objectReferenceValue = bgVignette.GetComponent<Image>();
        so.FindProperty("ambientDust").objectReferenceValue = bgParticles.GetComponent<ParticleSystem>();

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string sceneName = EditorSceneManager.GetActiveScene().name;
        Debug.Log($"[BackgroundSetup] Background layers created in {sceneName} scene!");
        EditorUtility.DisplayDialog("Background Setup Complete",
            "Created background layers:\n" +
            "  - BG_Base (solid color)\n" +
            "  - BG_Gradient (radial gradient overlay)\n" +
            "  - BG_Vignette (edge darkening)\n" +
            "  - BG_Particles (ambient dust particles)\n\n" +
            "BackgroundController added and wired.\n" +
            "Save the scene to keep changes.", "OK");
    }

    /// <summary>
    /// Remove previously created background layers to allow re-running setup.
    /// </summary>
    private static void RemoveOldBackgroundLayers(Transform canvasTransform)
    {
        string[] layerNames = { "BG_Base", "BG_Gradient", "BG_Vignette", "BG_Particles" };
        foreach (string name in layerNames)
        {
            Transform existing = canvasTransform.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[BackgroundSetup] Removed old {name}");
            }
        }
    }

    /// <summary>
    /// Create a fullscreen Image as a child of the given parent.
    /// Stretches to fill the entire Canvas. Raycast Target = false.
    /// </summary>
    private static GameObject CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return obj;
    }

    /// <summary>
    /// Create a ParticleSystem GameObject for ambient dust particles.
    /// Uses World Space simulation, positioned in front of BG layers but behind UI.
    /// </summary>
    private static GameObject CreateParticleObject(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        // Add RectTransform so it lives in Canvas hierarchy
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        // Basic configuration -- BackgroundController.SetupParticles() handles runtime settings
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
        main.startSpeed = 8f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startColor = new Color(1f, 0.82f, 0.12f, 0.15f);
        main.gravityModifier = -0.02f;
        main.maxParticles = 80;

        // Emission
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 4f;

        // Shape
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 12f, 1f);

        // Renderer: set to billboard, try to assign particle material
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = -1;

            // Try to find the default particle material
            Material particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }
        }

        return obj;
    }

    /// <summary>
    /// Create a basic Canvas if none exists in the scene.
    /// </summary>
}
#endif
