#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class RunAllSetups
{
    [MenuItem("Tools/Game/--- Run ALL Setups ---", priority = -100)]
    public static void RunAll()
    {
        int success = 0;
        int failed = 0;

        void Run(string name, System.Action action)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Running All Setups", name, (float)(success + failed) / 22f);
                action();
                success++;
                Debug.Log($"[SetupAll] {name} - OK");
            }
            catch (System.Exception e)
            {
                failed++;
                Debug.LogWarning($"[SetupAll] {name} - FAILED: {e.Message}");
            }
        }

        // Phase 1: Foundation
        Run("Setup Background", BackgroundSetup.SetupBackground);
        Run("Setup Panel Backgrounds", PanelBackgroundSetup.SetupPanelBackgrounds);
        Run("Setup Styled Buttons", ButtonStylingSetup.SetupStyledButtons);

        // Phase 2: Cards
        Run("Setup Card Frames", CardFrameSetup.SetupCardFrames);
        Run("Setup Card Layout", CardLayoutSetup.SetupCardLayout);

        // Phase 3: HUD
        Run("Setup Resource Bar", ResourceBarSetup.SetupResourceBar);
        Run("Setup Circular Timer", CircularTimerSetup.SetupCircularTimer);
        Run("Setup Phase Banner", PhaseBannerSetup.SetupPhaseBanner);
        Run("Setup Synergy Display", SynergyDisplaySetup.SetupSynergyDisplay);

        // Phase 4: Feedback
        Run("Setup Combat Arena", CombatArenaSetup.SetupCombatArena);

        // Phase 5: Screens
        Run("Setup Main Menu", MainMenuSetup.SetupMainMenu);
        Run("Setup Game Over Screen", GameOverSetup.SetupGameOverScreen);
        Run("Setup Discovery Popup", DiscoverySetup.SetupDiscoveryPopup);
        Run("Setup Collection UI", CollectionSetup.SetupCollectionUI);

        // Pre-existing setups
        Run("Setup Main Menu UI", MainMenuSceneSetup.SetupMainMenuUI);
        Run("Setup Lobby Scene", LobbySceneSetup.SetupLobbyScene);
        Run("Setup Network Objects", NetworkSceneSetup.SetupNetworkObjects);
        Run("Setup Player Prefab", PlayerPrefabSetup.SetupPlayerPrefab);
        Run("Setup Match Info Panel", MatchInfoPanelSetup.SetupMatchInfoPanel);
        Run("Setup Phase P UI", PhasePUISetup.SetupPhasePUI);
        Run("Setup Runtime Data Loader", RuntimeDataSetup.SetupRuntimeDataLoader);

        EditorUtility.ClearProgressBar();

        string msg = $"All setups complete!\n{success} succeeded, {failed} failed.\nCheck Console for details.";
        EditorUtility.DisplayDialog("Run All Setups", msg, "OK");
        Debug.Log($"[SetupAll] Finished: {success} succeeded, {failed} failed");
    }
}
#endif
