using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T418: Settings menu for audio, graphics, and controls.
    /// Persists settings via PlayerPrefs.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vfxToggle;

        [Header("Gameplay")]
        [SerializeField] private Slider combatSpeedSlider;
        [SerializeField] private TMP_Text combatSpeedText;
        [SerializeField] private Toggle autoEndTurnToggle;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        // PlayerPrefs keys
        private const string KEY_MASTER_VOL = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOL = "Settings_MusicVolume";
        private const string KEY_SFX_VOL = "Settings_SFXVolume";
        private const string KEY_QUALITY = "Settings_Quality";
        private const string KEY_FULLSCREEN = "Settings_Fullscreen";
        private const string KEY_VFX = "Settings_VFX";
        private const string KEY_COMBAT_SPEED = "Settings_CombatSpeed";
        private const string KEY_AUTO_END = "Settings_AutoEndTurn";

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);

            // T702: fullscreen is meaningless on mobile — hide its whole settings row.
            if (Application.isMobilePlatform && fullscreenToggle != null)
                fullscreenToggle.transform.parent.gameObject.SetActive(false);

            // Audio listeners
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // Graphics listeners
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (vfxToggle != null) vfxToggle.onValueChanged.AddListener(OnVFXToggleChanged);

            // Gameplay listeners
            if (combatSpeedSlider != null) combatSpeedSlider.onValueChanged.AddListener(OnCombatSpeedChanged);
            if (autoEndTurnToggle != null) autoEndTurnToggle.onValueChanged.AddListener(OnAutoEndTurnChanged);

            LoadSettings();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void Open()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            LoadSettings();
        }

        public void Close()
        {
            SaveSettings();
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            float music = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.7f);
            float sfx = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);
            int quality = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
            bool fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            bool vfx = PlayerPrefs.GetInt(KEY_VFX, 1) == 1;
            float combatSpeed = PlayerPrefs.GetFloat(KEY_COMBAT_SPEED, 1f);
            bool autoEnd = PlayerPrefs.GetInt(KEY_AUTO_END, 0) == 1;

            if (masterVolumeSlider != null) masterVolumeSlider.value = master;
            if (musicVolumeSlider != null) musicVolumeSlider.value = music;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;
            if (qualityDropdown != null) qualityDropdown.value = quality;
            if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
            if (vfxToggle != null) vfxToggle.isOn = vfx;
            if (combatSpeedSlider != null) combatSpeedSlider.value = combatSpeed;
            if (autoEndTurnToggle != null) autoEndTurnToggle.isOn = autoEnd;

            UpdateVolumeTexts();
            UpdateCombatSpeedText();
        }

        private void SaveSettings()
        {
            if (masterVolumeSlider != null) PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVolumeSlider.value);
            if (musicVolumeSlider != null) PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolumeSlider.value);
            if (sfxVolumeSlider != null) PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolumeSlider.value);
            if (qualityDropdown != null) PlayerPrefs.SetInt(KEY_QUALITY, qualityDropdown.value);
            if (fullscreenToggle != null) PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreenToggle.isOn ? 1 : 0);
            if (vfxToggle != null) PlayerPrefs.SetInt(KEY_VFX, vfxToggle.isOn ? 1 : 0);
            if (combatSpeedSlider != null) PlayerPrefs.SetFloat(KEY_COMBAT_SPEED, combatSpeedSlider.value);
            if (autoEndTurnToggle != null) PlayerPrefs.SetInt(KEY_AUTO_END, autoEndTurnToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ResetToDefaults()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
            if (musicVolumeSlider != null) musicVolumeSlider.value = 0.7f;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.8f;
            if (qualityDropdown != null) qualityDropdown.value = QualitySettings.names.Length - 1;
            if (fullscreenToggle != null) fullscreenToggle.isOn = true;
            if (vfxToggle != null) vfxToggle.isOn = true;
            if (combatSpeedSlider != null) combatSpeedSlider.value = 1f;
            if (autoEndTurnToggle != null) autoEndTurnToggle.isOn = false;
            SaveSettings();
        }

        // Audio callbacks
        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            UpdateVolumeTexts();
        }

        private void OnMusicVolumeChanged(float value)
        {
            // T702: MusicManager.MusicVolume setter already existed — just wasn't wired.
            if (TarotBattlegrounds.Combat.Audio.MusicManager.Instance != null)
                TarotBattlegrounds.Combat.Audio.MusicManager.Instance.MusicVolume = value;
            UpdateVolumeTexts();
        }

        private void OnSFXVolumeChanged(float value)
        {
            // T702: SFXManager.MasterVolume setter already existed — just wasn't wired.
            if (TarotBattlegrounds.Combat.Audio.SFXManager.Instance != null)
                TarotBattlegrounds.Combat.Audio.SFXManager.Instance.MasterVolume = value;
            UpdateVolumeTexts();
        }

        private void UpdateVolumeTexts()
        {
            if (masterVolumeText != null && masterVolumeSlider != null)
                masterVolumeText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
            if (musicVolumeText != null && musicVolumeSlider != null)
                musicVolumeText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            if (sfxVolumeText != null && sfxVolumeSlider != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
        }

        // Graphics callbacks
        private void OnQualityChanged(int index) { QualitySettings.SetQualityLevel(index); }
        private void OnFullscreenChanged(bool value) { Screen.fullScreen = value; }
        private void OnVFXToggleChanged(bool value) { TarotBattlegrounds.Combat.VFX.VFXManager.Instance?.SetEnabled(value); }

        // Gameplay callbacks
        private void OnCombatSpeedChanged(float value) { UpdateCombatSpeedText(); }
        private void OnAutoEndTurnChanged(bool value) { /* GameManager integration */ }

        private void UpdateCombatSpeedText()
        {
            if (combatSpeedText != null && combatSpeedSlider != null)
                combatSpeedText.text = $"{combatSpeedSlider.value:F1}x";
        }

        // Static accessors for other systems
        public static float MasterVolume => PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        public static float MusicVolume => PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.7f);
        public static float SFXVolume => PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.8f);
        public static float CombatSpeed => PlayerPrefs.GetFloat(KEY_COMBAT_SPEED, 1f);
        public static bool VFXEnabled => PlayerPrefs.GetInt(KEY_VFX, 1) == 1;
        public static bool AutoEndTurn => PlayerPrefs.GetInt(KEY_AUTO_END, 0) == 1;

        public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (resetButton != null) resetButton.onClick.RemoveAllListeners();
        }
    }
}
