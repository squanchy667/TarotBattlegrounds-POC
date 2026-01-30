#if UNITY_EDITOR && PHOTON_UNITY_NETWORKING
using UnityEngine;
using UnityEditor;
using Photon.Pun;

/// <summary>
/// Editor script to add NetworkGameBridge and NetworkGameSetup objects to the Game scene.
/// Run via menu: Tools/Game/Setup Network Objects
/// </summary>
public class NetworkSceneSetup : EditorWindow
{
    [MenuItem("Tools/Game/Setup Network Objects")]
    public static void SetupNetworkObjects()
    {
        int changes = 0;

        changes += SetupNetworkGameBridge();
        changes += SetupNetworkGameSetup();

        if (changes > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        string msg = $"Network setup complete. {changes} objects created/configured.\n\n" +
            "NetworkGameBridge: PhotonView (ViewID=1) + NetworkGameBridge component\n" +
            "NetworkGameSetup: NetworkGameSetup component\n\n" +
            "Save the scene to keep changes.";

        Debug.Log($"[NetworkSceneSetup] {msg}");
        EditorUtility.DisplayDialog("Network Objects Setup", msg, "OK");
    }

    private static int SetupNetworkGameBridge()
    {
        // Check if already exists
        NetworkGameBridge existing = Object.FindObjectOfType<NetworkGameBridge>();
        if (existing != null)
        {
            Debug.Log("[NetworkSceneSetup] NetworkGameBridge already exists in scene.");
            // Ensure PhotonView is configured
            EnsurePhotonView(existing.gameObject);
            return 0;
        }

        GameObject bridgeObj = new GameObject("NetworkGameBridge");
        Undo.RegisterCreatedObjectUndo(bridgeObj, "Create NetworkGameBridge");

        // Add PhotonView first
        PhotonView pv = bridgeObj.AddComponent<PhotonView>();
        pv.ViewID = 1; // Scene object ViewID 1

        // Add NetworkGameBridge component
        bridgeObj.AddComponent<NetworkGameBridge>();

        Debug.Log("[NetworkSceneSetup] Created NetworkGameBridge with PhotonView (ViewID=1)");
        return 1;
    }

    private static void EnsurePhotonView(GameObject obj)
    {
        PhotonView pv = obj.GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = obj.AddComponent<PhotonView>();
            pv.ViewID = 1;
            Debug.Log("[NetworkSceneSetup] Added missing PhotonView to NetworkGameBridge");
        }
        else if (pv.ViewID != 1)
        {
            pv.ViewID = 1;
            Debug.Log("[NetworkSceneSetup] Fixed PhotonView ViewID to 1");
        }
    }

    private static int SetupNetworkGameSetup()
    {
        // Check if already exists
        NetworkGameSetup existing = Object.FindObjectOfType<NetworkGameSetup>();
        if (existing != null)
        {
            Debug.Log("[NetworkSceneSetup] NetworkGameSetup already exists in scene.");
            return 0;
        }

        GameObject setupObj = new GameObject("NetworkGameSetup");
        Undo.RegisterCreatedObjectUndo(setupObj, "Create NetworkGameSetup");

        setupObj.AddComponent<NetworkGameSetup>();

        Debug.Log("[NetworkSceneSetup] Created NetworkGameSetup");
        return 1;
    }
}
#endif
