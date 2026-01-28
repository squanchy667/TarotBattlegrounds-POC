using UnityEngine;

/// <summary>
/// Static configuration for game settings.
/// Persists across scenes using PlayerPrefs.
/// </summary>
public static class GameConfig
{
    // Keys for PlayerPrefs
    private const string KEY_PLAYER_COUNT = "GameConfig_PlayerCount";
    private const string KEY_HUMAN_PLAYER_INDEX = "GameConfig_HumanPlayerIndex";
    private const string KEY_GAME_MODE = "GameConfig_GameMode";
    private const string KEY_AI_DIFFICULTY = "GameConfig_AIDifficulty";

    /// <summary>
    /// Game modes available.
    /// </summary>
    public enum GameMode
    {
        HumanVsAI,      // 1 human player, rest are AI
        AIvsAI,         // All AI (for testing/spectating)
        Multiplayer     // Future: multiple human players
    }

    /// <summary>
    /// Number of players (2-4).
    /// </summary>
    public static int PlayerCount
    {
        get => PlayerPrefs.GetInt(KEY_PLAYER_COUNT, 2);
        set => PlayerPrefs.SetInt(KEY_PLAYER_COUNT, Mathf.Clamp(value, 2, 4));
    }

    /// <summary>
    /// Which player slot is human (0-based index).
    /// Only used in HumanVsAI mode.
    /// </summary>
    public static int HumanPlayerIndex
    {
        get => PlayerPrefs.GetInt(KEY_HUMAN_PLAYER_INDEX, 0);
        set => PlayerPrefs.SetInt(KEY_HUMAN_PLAYER_INDEX, Mathf.Clamp(value, 0, PlayerCount - 1));
    }

    /// <summary>
    /// Current game mode.
    /// </summary>
    public static GameMode CurrentGameMode
    {
        get => (GameMode)PlayerPrefs.GetInt(KEY_GAME_MODE, (int)GameMode.HumanVsAI);
        set => PlayerPrefs.SetInt(KEY_GAME_MODE, (int)value);
    }

    /// <summary>
    /// Default AI difficulty for all AI players.
    /// </summary>
    public static AIDifficulty DefaultAIDifficulty
    {
        get => (AIDifficulty)PlayerPrefs.GetInt(KEY_AI_DIFFICULTY, (int)AIDifficulty.Medium);
        set => PlayerPrefs.SetInt(KEY_AI_DIFFICULTY, (int)value);
    }

    /// <summary>
    /// Check if a player index is controlled by a human.
    /// </summary>
    public static bool IsHumanPlayer(int playerIndex)
    {
        if (CurrentGameMode == GameMode.AIvsAI)
            return false;
        if (CurrentGameMode == GameMode.HumanVsAI)
            return playerIndex == HumanPlayerIndex;
        // Multiplayer mode - could extend this
        return playerIndex == HumanPlayerIndex;
    }

    /// <summary>
    /// Get AI difficulty for a specific player.
    /// Can be extended to support per-player difficulty.
    /// </summary>
    public static AIDifficulty GetAIDifficulty(int playerIndex)
    {
        // For now, all AI use the same difficulty
        // Could extend to store per-player difficulties
        return DefaultAIDifficulty;
    }

    /// <summary>
    /// Save all settings to PlayerPrefs.
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
        Debug.Log($"[GameConfig] Saved: {PlayerCount} players, Mode: {CurrentGameMode}, AI: {DefaultAIDifficulty}");
    }

    /// <summary>
    /// Reset to default settings.
    /// </summary>
    public static void ResetToDefaults()
    {
        PlayerCount = 2;
        HumanPlayerIndex = 0;
        CurrentGameMode = GameMode.HumanVsAI;
        DefaultAIDifficulty = AIDifficulty.Medium;
        Save();
    }

    /// <summary>
    /// Log current configuration.
    /// </summary>
    public static void LogConfig()
    {
        Debug.Log($"[GameConfig] Players: {PlayerCount}, Mode: {CurrentGameMode}, Human: P{HumanPlayerIndex + 1}, AI Difficulty: {DefaultAIDifficulty}");
    }
}
