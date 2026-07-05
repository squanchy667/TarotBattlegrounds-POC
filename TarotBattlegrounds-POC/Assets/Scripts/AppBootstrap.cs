using UnityEngine;
using UnityEngine.SceneManagement;
using TarotBattlegrounds.Combat.Audio;

/// <summary>
/// T701/T702/T703/T704: Runtime bootstrap for systems that are code-created rather than
/// scene-authored (this project's UI/managers are largely built from code). Runs once
/// before the first scene loads:
///  - creates the persistent AudioSystem (MusicManager + SFXManager) — without this the
///    audio pipeline is inert because nothing in any scene instantiates the managers
///  - applies the player's saved audio/VFX settings at startup (SettingsUI only applies
///    them when its panel is opened)
///  - caps the frame rate for mobile battery/thermal (this is a turn-based auto-battler)
///  - attaches SafeAreaHandler to root canvases on every scene load (only Game.unity
///    carried one; MainMenu/Lobby ignored notches)
/// </summary>
public static class AppBootstrap
{
    // Must match SettingsUI's PlayerPrefs keys (SettingsUI.cs:39-44).
    private const string KEY_MASTER_VOL = "Settings_MasterVolume";
    private const string KEY_MUSIC_VOL = "Settings_MusicVolume";
    private const string KEY_SFX_VOL = "Settings_SFXVolume";
    private const string KEY_VFX = "Settings_VFX";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Application.targetFrameRate = 60;

        if (MusicManager.Instance == null && SFXManager.Instance == null)
        {
            var audioSystem = new GameObject("AudioSystem");
            audioSystem.AddComponent<MusicManager>();   // Awake handles singleton + DontDestroyOnLoad
            audioSystem.AddComponent<SFXManager>();     // Awake loads SFXConfig from Resources
            Object.DontDestroyOnLoad(audioSystem);
        }

        ApplySavedSettings();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);

        if (MusicManager.Instance != null)
            MusicManager.Instance.MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.7f);
        if (SFXManager.Instance != null)
            SFXManager.Instance.MasterVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);

        // VFXManager is scene-scoped and may not exist yet; SettingsUI re-applies on open,
        // and the toggle defaults to enabled, so only propagate an explicit "off".
        if (PlayerPrefs.GetInt(KEY_VFX, 1) == 0)
            TarotBattlegrounds.Combat.VFX.VFXManager.Instance?.SetEnabled(false);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // T703: every root canvas respects the device safe area. Skip canvases whose
        // hierarchy already carries a handler (Game.unity's authored one stays canonical).
        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null && root.GetComponentInChildren<SafeAreaHandler>(true) == null)
                root.AddComponent<SafeAreaHandler>();
        }
    }
}
