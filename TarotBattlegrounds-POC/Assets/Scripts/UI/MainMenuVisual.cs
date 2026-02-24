using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Selector Colors (overridden by theme)")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.15f);
    [SerializeField] private Color unselectedColor = new Color(0.25f, 0.22f, 0.35f);
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color unselectedTextColor = new Color(0.6f, 0.6f, 0.65f);

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
                difficultyLabels[i].color = isSelected ? selectedTextColor : unselectedTextColor;
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
                playerCountLabels[i].color = isSelected ? selectedTextColor : unselectedTextColor;
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

        // Check for TarotButton first
        TarotButton tarotBtn = button.GetComponent<TarotButton>();
        if (tarotBtn != null)
        {
            tarotBtn.SetInteractable(true);
            tarotBtn.SetVariant(isSelected ? ButtonVariant.Primary : ButtonVariant.Secondary);
            return;
        }

        // Fallback: style the button Image directly
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = isSelected ? selectedColor : unselectedColor;
        }

        ColorBlock colors = button.colors;
        Color baseColor = isSelected ? selectedColor : unselectedColor;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.15f;
        colors.pressedColor = baseColor * 0.8f;
        colors.selectedColor = baseColor * 1.1f;
        button.colors = colors;
    }

    // ========== THEME ==========

    /// <summary>
    /// Apply theme colors to selector buttons and title.
    /// </summary>
    public override void ApplyTheme(ThemeConfig theme)
    {
        if (theme == null) return;

        // Update selector colors from theme
        selectedColor = theme.accentColor;
        unselectedColor = new Color(
            theme.secondaryColor.r + 0.1f,
            theme.secondaryColor.g + 0.1f,
            theme.secondaryColor.b + 0.12f
        );
        selectedTextColor = theme.textColorLight;
        unselectedTextColor = new Color(
            theme.textColorLight.r * 0.6f,
            theme.textColorLight.g * 0.6f,
            theme.textColorLight.b * 0.65f
        );

        // Apply title color (gold/accent)
        if (titleText != null)
        {
            titleText.color = theme.accentColor;
        }

        // Apply fallback gradient color
        if (fallbackGradient != null && backgroundController == null)
        {
            fallbackGradient.color = theme.gameBackgroundColor;
        }

        // Refresh selector visuals with new colors
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
