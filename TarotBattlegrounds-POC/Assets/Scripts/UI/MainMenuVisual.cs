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

    [Header("Hero sigil (underline_ember breathe)")]
    [SerializeField] private CanvasGroup sigilUnderline;
    [SerializeField] private float sigilBreatheSpeed = 0.55f; // ~3.2s period-ish with sin
    [SerializeField] private float sigilAlphaMin = 0.55f;
    [SerializeField] private float sigilAlphaMax = 1f;

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
    /// <summary>
    /// When true, title sits under a LayoutGroup — never write anchoredPosition
    /// (that fought VLG after Lobby→Menu and shoved the left of the title off-screen).
    /// </summary>
    private bool titleDrivenByLayout;

    private int selectedDifficultyIndex = 1; // Default: Medium
    private int selectedPlayerCountIndex = 0; // Default: 4 players

    private void Awake()
    {
        if (titleText != null)
        {
            titleRect = titleText.GetComponent<RectTransform>();
            // Parent VerticalLayoutGroup (HeroLeft) owns placement
            titleDrivenByLayout = titleRect != null
                && titleRect.GetComponentInParent<UnityEngine.UI.LayoutGroup>() != null;
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
        // Title float disabled when layout-driven — see AnimateTitleFloat docs
        AnimateSigilBreathe();
    }

    // ========== TITLE ANIMATION ==========

    /// <summary>
    /// Legacy float kept for non-layout titles only. HeroLeft uses VerticalLayoutGroup;
    /// writing anchoredPosition every frame captured a pre-layout (0,0) base on scene
    /// reload (Lobby→Menu) and clipped "Tarot Battlegrounds" off the left edge.
    /// </summary>
    private void AnimateTitleFloat()
    {
        // Intentionally no-op for layout-driven titles (current §8 MainMenu).
        if (titleDrivenByLayout || titleRect == null) return;
    }

    /// <summary>
    /// Slow-burning opacity on the hero underline_ember strip (design-site mock).
    /// Off under reduce-motion — static mid opacity.
    /// </summary>
    private void AnimateSigilBreathe()
    {
        if (sigilUnderline == null) return;
        if (UiMotion.ReduceMotion)
        {
            sigilUnderline.alpha = 0.85f;
            return;
        }

        // Map sin [-1,1] → [min,max]
        float t = (Mathf.Sin(Time.unscaledTime * sigilBreatheSpeed) + 1f) * 0.5f;
        sigilUnderline.alpha = Mathf.Lerp(sigilAlphaMin, sigilAlphaMax, t);
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
    /// No-op: title is layout-driven; position is not animated.
    /// Kept so any fade-in callers still compile.
    /// </summary>
    public void RefreshTitleBasePosition()
    {
        // Layout owns TitleText placement under HeroLeft VLG.
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
