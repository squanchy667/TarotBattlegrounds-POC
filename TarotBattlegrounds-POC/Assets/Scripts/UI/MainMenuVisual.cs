using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TarotBattlegrounds.UI;

/// <summary>
/// UX17: Handles menu-specific visual enhancements for the MainMenu scene.
/// - Title float animation (subtle sine-wave Y offset)
/// - Difficulty selector: styled toggle group with highlight on selected
/// - Player count selector: styled toggle group with highlight on selected
/// - Background integration: reuses BackgroundController if present
/// Implements IThemeable via ThemeableUI for theme-aware color management.
/// </summary>
public class MainMenuVisual : ThemeableUI
{
    [Header("Title Animation")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private float floatAmplitude = 3f;
    [SerializeField] private float floatSpeed = 1.5f;

    [Header("Background")]
    [SerializeField] private BackgroundController backgroundController;
    [SerializeField] private Image fallbackGradient;

    [Header("Difficulty Selector")]
    [SerializeField] private List<Button> difficultyButtons = new List<Button>();
    [SerializeField] private List<TMP_Text> difficultyLabels = new List<TMP_Text>();

    [Header("Player Count Selector")]
    [SerializeField] private List<Button> playerCountButtons = new List<Button>();
    [SerializeField] private List<TMP_Text> playerCountLabels = new List<TMP_Text>();

    // Selector colors come from Tokens (T750) — selection is an ignited state:
    // selected Ember/BoneBright, unselected surface/BoneDim. No serialized colors.

    private RectTransform titleRect;
    private Vector2 titleBasePosition;
    private bool titleBasePositionCaptured;

    private int selectedDifficultyIndex = 1; // Default: Medium
    private int selectedPlayerCountIndex = 0; // Default: 4 players

    private void Awake()
    {
        if (titleText != null)
        {
            titleRect = titleText.GetComponent<RectTransform>();
        }

        // Set up difficulty button listeners
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            if (difficultyButtons[i] != null)
            {
                int index = i;
                difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
            }
        }

        // Set up player count button listeners
        for (int i = 0; i < playerCountButtons.Count; i++)
        {
            if (playerCountButtons[i] != null)
            {
                int index = i;
                playerCountButtons[i].onClick.AddListener(() => SelectPlayerCount(index));
            }
        }
    }

    private void Start()
    {
        // Capture title base position after layout has settled
        if (titleRect != null)
        {
            titleBasePosition = titleRect.anchoredPosition;
            titleBasePositionCaptured = true;
        }

        // Set fallback gradient if no BackgroundController present
        if (backgroundController == null && fallbackGradient != null)
        {
            fallbackGradient.gameObject.SetActive(true);
        }
        else if (fallbackGradient != null)
        {
            // BackgroundController handles the background, hide fallback
            fallbackGradient.gameObject.SetActive(false);
        }

        // Apply initial selector visuals
        UpdateDifficultyVisuals();
        UpdatePlayerCountVisuals();
    }

    private void Update()
    {
        AnimateTitleFloat();
    }

    // ========== TITLE ANIMATION ==========

    /// <summary>
    /// Subtle sine-wave Y offset on the title text (+-3px at slow speed).
    /// </summary>
    private void AnimateTitleFloat()
    {
        if (titleRect == null || !titleBasePositionCaptured) return;
        if (UiMotion.ReduceMotion)
        {
            titleRect.anchoredPosition = titleBasePosition;
            return;
        }

        float yOffset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude;
        titleRect.anchoredPosition = titleBasePosition + new Vector2(0f, yOffset);
    }

    // ========== DIFFICULTY SELECTOR ==========

    /// <summary>
    /// Select a difficulty level by index (0=Easy, 1=Medium, 2=Hard).
    /// Updates visuals and sets GameConfig.
    /// </summary>
    public void SelectDifficulty(int index)
    {
        if (index < 0 || index >= difficultyButtons.Count) return;
        selectedDifficultyIndex = index;
        UpdateDifficultyVisuals();

        // Update game config
        GameConfig.DefaultAIDifficulty = (AIDifficulty)index;
        Debug.Log($"[MainMenuVisual] Difficulty set to: {GameConfig.DefaultAIDifficulty}");
    }

    private void UpdateDifficultyVisuals()
    {
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            bool isSelected = (i == selectedDifficultyIndex);
            ApplySelectorButtonStyle(difficultyButtons[i], isSelected);

            if (i < difficultyLabels.Count && difficultyLabels[i] != null)
            {
                difficultyLabels[i].color = isSelected ? Tokens.BoneBright : Tokens.BoneDim;
            }
        }
    }

    // ========== PLAYER COUNT SELECTOR ==========

    /// <summary>
    /// Select a player count by index (0=4, 1=6, 2=8).
    /// Updates visuals. Note: MainMenuManager handles the actual GameConfig write
    /// via its own SelectPlayerCount — this is for visual styling only.
    /// </summary>
    public void SelectPlayerCount(int index)
    {
        if (index < 0 || index >= playerCountButtons.Count) return;
        selectedPlayerCountIndex = index;
        UpdatePlayerCountVisuals();
    }

    private void UpdatePlayerCountVisuals()
    {
        for (int i = 0; i < playerCountButtons.Count; i++)
        {
            bool isSelected = (i == selectedPlayerCountIndex);
            ApplySelectorButtonStyle(playerCountButtons[i], isSelected);

            if (i < playerCountLabels.Count && playerCountLabels[i] != null)
            {
                playerCountLabels[i].color = isSelected ? Tokens.BoneBright : Tokens.BoneDim;
            }
        }
    }

    // ========== SHARED SELECTOR STYLING ==========

    /// <summary>
    /// Apply bright/dim styling to a toggle-style button based on selection state.
    /// Selected buttons use the accent color; unselected buttons are dim.
    /// </summary>
    private void ApplySelectorButtonStyle(Button button, bool isSelected)
    {
        if (button == null) return;

        // Selection is an ignited state — IgniteButton owns the visuals.
        IgniteButton ignite = button.GetComponent<IgniteButton>();
        if (ignite != null)
        {
            ignite.Selected = isSelected;
            return;
        }

        // Fallback for buttons not yet carrying IgniteButton: tokenized tint only.
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = isSelected ? Tokens.Ember : Tokens.CharredWood;
        }
    }

    // ========== THEME ==========

    /// <summary>
    /// T750: menu chrome/text colors come from Tokens, not the theme (DESIGN.md §8:
    /// title is BoneBright; selection is Ember). Themes contribute art/copy only.
    /// </summary>
    public override void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        if (titleText != null)
        {
            titleText.color = Tokens.BoneBright;
        }

        if (fallbackGradient != null && backgroundController == null)
        {
            fallbackGradient.color = Tokens.Ash;
        }

        UpdateDifficultyVisuals();
        UpdatePlayerCountVisuals();
    }

    // ========== PUBLIC API ==========

    /// <summary>
    /// Get the currently selected difficulty index.
    /// </summary>
    public int SelectedDifficultyIndex => selectedDifficultyIndex;

    /// <summary>
    /// Get the currently selected player count index.
    /// </summary>
    public int SelectedPlayerCountIndex => selectedPlayerCountIndex;

    /// <summary>
    /// Update the title base position (call after layout changes, e.g., after fade-in completes).
    /// </summary>
    public void RefreshTitleBasePosition()
    {
        if (titleRect != null)
        {
            titleBasePosition = titleRect.anchoredPosition;
            titleBasePositionCaptured = true;
        }
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        for (int i = 0; i < difficultyButtons.Count; i++)
        {
            if (difficultyButtons[i] != null)
                difficultyButtons[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < playerCountButtons.Count; i++)
        {
            if (playerCountButtons[i] != null)
                playerCountButtons[i].onClick.RemoveAllListeners();
        }
    }
}
