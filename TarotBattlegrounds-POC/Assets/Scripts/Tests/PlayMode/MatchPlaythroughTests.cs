using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayMode integration tests that boot the real Game scene and let the
/// self-playing AIvsAI match run to completion, asserting the full game loop
/// (recruit -> shop/economy -> combat -> elimination -> game over) is valid.
///
/// These are the durable form of the live MCP playthrough done on 2026-07-07,
/// where a 4-player AIvsAI match completed cleanly in 16 turns with a single winner.
/// EditMode tests already cover logic units deterministically; these cover the
/// integration path EditMode cannot: the scene boots and a real match plays out.
///
/// These tests deliberately do NOT suppress log errors: Unity's test framework fails
/// a test on any unexpected Debug.LogError/LogException, so a clean pass also proves a
/// full match runs without runtime errors. (The prod content CDN 404s are logged as
/// warnings by RuntimeDataLoader and fall back to built-in data, so they do not fail.)
/// </summary>
public class MatchPlaythroughTests
{
    private GameOverData _lastGameOver;
    private bool _gameOverFired;
    private float _originalTimeScale;

    private void HandleGameOver(GameOverData data)
    {
        _gameOverFired = true;
        _lastGameOver = data;
    }

    [SetUp]
    public void SetUp()
    {
        _gameOverFired = false;
        _lastGameOver = default;
        _originalTimeScale = Time.timeScale;
        GameManager.OnGameOver += HandleGameOver;
    }

    [TearDown]
    public void TearDown()
    {
        GameManager.OnGameOver -= HandleGameOver;
        Time.timeScale = _originalTimeScale;
    }

    /// <summary>
    /// Configure an offline AIvsAI match, load the Game scene, and pump frames
    /// until the game loop fires OnGameOver or the timeout elapses.
    /// </summary>
    private IEnumerator RunAIMatch(int playerCount, float timeoutSeconds)
    {
        GameConfig.CurrentGameMode = GameConfig.GameMode.AIvsAI;
        GameConfig.PlayerCount = playerCount;
        GameConfig.HumanPlayerIndex = 0;
        GameConfig.DefaultAIDifficulty = AIDifficulty.Medium;

        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        // Let the scene load and GameManager.Start() start the game loop.
        yield return null;
        yield return null;

        // Speed the match up: combat WaitForSeconds are timeScale-scaled, and the
        // recruit phase is already instant in AIvsAI (every AI auto-readies).
        Time.timeScale = 5f;

        float elapsed = 0f;
        while (!_gameOverFired && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void AssertValidStandings(int expectedPlayers)
    {
        Assert.IsTrue(_gameOverFired,
            "Match did not reach Game Over within the timeout - possible stall/deadlock in the game loop.");
        Assert.GreaterOrEqual(_lastGameOver.winnerPlayerIndex, 0, "Game ended with no winner.");
        Assert.AreEqual(expectedPlayers, _lastGameOver.standings.Count,
            "Standings should list every player exactly once.");
        Assert.Greater(_lastGameOver.totalTurns, 0, "Match should have played at least one turn.");
        Assert.AreEqual(_lastGameOver.winnerPlayerIndex, _lastGameOver.standings[0],
            "Winner must be ranked #1 in the standings.");
        CollectionAssert.AllItemsAreUnique(_lastGameOver.standings,
            "Standings contain duplicate player indices.");
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator AIvsAI_FourPlayers_CompletesWithSingleWinner()
    {
        yield return RunAIMatch(4, 120f);
        AssertValidStandings(4);
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator AIvsAI_EightPlayers_CompletesWithSingleWinner()
    {
        yield return RunAIMatch(8, 200f);
        AssertValidStandings(8);
    }

    /// <summary>
    /// Fast, deterministic boot check: the scene wires up GameManager, spawns the
    /// right number of players, and everyone starts at tavern tier 1 on turn 1.
    /// </summary>
    [UnityTest, Timeout(120000)]
    public IEnumerator GameScene_BootsIntoRunningMatch()
    {
        GameConfig.CurrentGameMode = GameConfig.GameMode.AIvsAI;
        GameConfig.PlayerCount = 4;
        GameConfig.HumanPlayerIndex = 0;
        GameConfig.DefaultAIDifficulty = AIDifficulty.Medium;

        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var gm = GameManager.Instance;
        Assert.IsNotNull(gm, "GameManager did not initialize on scene load.");
        Assert.AreEqual(4, gm.playerCount, "GameManager did not pick up PlayerCount from GameConfig.");
        Assert.IsNotNull(gm.players, "Players list was not initialized.");
        Assert.AreEqual(4, gm.players.Count, "Expected 4 Player objects (2 in scene + 2 spawned).");
        for (int i = 0; i < gm.playerCount; i++)
            Assert.AreEqual(1, gm.players[i].currentTavernTier,
                "Player " + (i + 1) + " should start at tavern tier 1.");
    }
}
