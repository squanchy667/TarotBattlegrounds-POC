using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// AutoPlaytest v1 — a self-playing PlayMode match that records structured evidence
/// (JSON event log + screenshots) and runs the mechanical checks mapped to the manual
/// playtest rows in docs/work-orders/PLAYTEST_LOG.md, then writes an AUTO_PLAYTEST_REPORT.md
/// bundle outside Assets (next to the Unity project folder) for a human reviewer.
///
/// This follows the same boot/pump pattern as MatchPlaythroughTests.cs (load the real Game
/// scene, let AI players self-play, pump frames at Time.timeScale=5 until OnGameOver fires)
/// and deliberately does NOT suppress log errors, for the same reason: a clean pass also
/// proves the match ran without runtime errors.
///
/// Automated rows: A1 (game-over panel active), A4 (single "Turn N" label), A5 (tier-6
/// discovery not empty after pool depletion), B4 (no auto match-info popup after combat),
/// B5 (combat chrome hidden — NEEDS-HUMAN in pure AIvsAI mode, see PlaytestEvidenceRecorder's
/// header comment for why). All other PLAYTEST_LOG.md rows are listed as NEEDS-HUMAN in the
/// generated report with the closest relevant screenshot attached as evidence.
/// </summary>
public class AutoPlaytestTests
{
    private GameOverData _lastGameOver;
    private bool _gameOverFired;
    private float _originalTimeScale;
    private PlaytestEvidenceRecorder _recorder;
    private GameObject _recorderGO;

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

        if (_recorder != null)
            _recorder.StopRecording();
        if (_recorderGO != null)
            UnityEngine.Object.Destroy(_recorderGO);
    }

    /// <summary>
    /// Boot a 4-player offline AIvsAI match, attach the evidence recorder once the scene is
    /// live, let the match self-play to completion (or timeout), then run the mechanical
    /// PLAYTEST_LOG.md checks and write the evidence bundle.
    /// </summary>
    [UnityTest, Timeout(300000)]
    public IEnumerator AutoPlaytest_FourPlayerAIvsAI_RecordsEvidenceAndReport()
    {
        float matchStartRealtime = Time.realtimeSinceStartup;

        // Bundle dir: $P/../AutoPlaytest/run_<timestamp>/ — OUTSIDE Assets, next to the
        // Unity project folder. Application.dataPath is ".../TarotBattlegrounds-POC/Assets";
        // "../.." walks up to the project's parent, matching the spec exactly.
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string bundleDir = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "..", "AutoPlaytest", "run_" + timestamp));

        GameConfig.CurrentGameMode = GameConfig.GameMode.AIvsAI;
        GameConfig.PlayerCount = 4;
        GameConfig.HumanPlayerIndex = 0;
        GameConfig.DefaultAIDifficulty = AIDifficulty.Medium;

        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        // Let the scene load and GameManager.Start() start the game loop.
        yield return null;
        yield return null;

        Assert.IsNotNull(GameManager.Instance, "GameManager did not initialize on scene load.");

        // Create the recorder AFTER the scene load — LoadSceneMode.Single destroys any
        // objects that existed before it, so creating this earlier would just get wiped.
        _recorderGO = new GameObject("PlaytestEvidenceRecorder");
        _recorder = _recorderGO.AddComponent<PlaytestEvidenceRecorder>();
        _recorder.BeginRecording(bundleDir);

        // Key moment 1: shortly after scene boot, recruit phase HUD.
        yield return _recorder.CaptureScreenshotAfterFrames("01_boot_recruit_hud", 2);
        _recorder.SampleTurnLabel("post-boot (turn 1 recruit)");

        // Speed the match up: combat WaitForSeconds are timeScale-scaled, and the recruit
        // phase is already instant in AIvsAI (every AI auto-readies). Mirrors MatchPlaythroughTests.
        Time.timeScale = 5f;

        float elapsed = 0f;
        float timeoutSeconds = 120f;
        while (!_gameOverFired && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;

        // Key moment 3: game over.
        if (_gameOverFired)
        {
            yield return _recorder.CaptureScreenshotAfterFrames("03_game_over", 20);
            _recorder.CheckGameOverUIActive();
        }
        else
        {
            _recorder.RecordCheck("A1", "Game over — win or lose shows full-screen panel (not cut off)",
                PlaytestEvidenceRecorder.AutoFail,
                $"Match did not reach Game Over within the {timeoutSeconds}s timeout — possible stall/deadlock.",
                "");
        }

        _recorder.StopRecording();

        _recorder.FinalizeTurnLabelCheck();
        _recorder.FinalizeMatchInfoCheck();
        _recorder.FinalizeCombatChromeCheck();
        RunDiscoveryPoolCheck();

        var summary = new PlaytestMatchSummary
        {
            playerCount = 4,
            gameOverFired = _gameOverFired,
            winnerPlayerIndex = _gameOverFired ? _lastGameOver.winnerPlayerIndex : -1,
            totalTurns = _gameOverFired ? _lastGameOver.totalTurns : 0,
            durationRealSeconds = Time.realtimeSinceStartup - matchStartRealtime,
            standings = _gameOverFired && _lastGameOver.standings != null
                ? new System.Collections.Generic.List<int>(_lastGameOver.standings)
                : new System.Collections.Generic.List<int>()
        };
        _recorder.WriteBundle(summary);

        // Hard regression bar — mirrors MatchPlaythroughTests.AssertValidStandings, so a
        // stalled/broken match still fails this suite even though the bundle above already
        // captured whatever evidence was available.
        AssertValidStandings(4);

        AssertNoAutoFailChecks();
    }

    /// <summary>
    /// Row A5 — tier-6 discovery must never offer an empty choice, even once the live card
    /// pool is thin. TavernManager.GetDiscoveryCards(tier, count) walks the live pool down
    /// from `tier`, then falls back to cloning unique templates from masterCards, so it should
    /// never return an empty list as long as masterCards is non-empty. This deliberately
    /// drains the pool via repeated tier-6 draws (on top of whatever the AI match above already
    /// consumed) to reproduce the "Tier 6 triple: no cards to choose" scenario from the manual
    /// playtest notes, then asserts the fallback still produces a non-empty offer.
    /// Runs directly against TavernManager.Instance (still alive — the Game scene is not
    /// unloaded after OnGameOver), no extra scene coupling needed.
    /// </summary>
    private void RunDiscoveryPoolCheck()
    {
        if (TavernManager.Instance == null)
        {
            _recorder.RecordCheck("A5", "Tier 6 triple -> discovery offers cards (not empty)",
                PlaytestEvidenceRecorder.NeedsHuman,
                "TavernManager.Instance was not available after the match — could not exercise GetDiscoveryCards.", "");
            return;
        }

        int drains = 0;
        const int maxDrains = 40;
        while (drains < maxDrains)
        {
            var drained = TavernManager.Instance.GetDiscoveryCards(6, 3);
            drains++;
            if (drained.Count == 0) break; // pool + master templates both exhausted
        }

        var finalOffer = TavernManager.Instance.GetDiscoveryCards(6, 3);
        bool pass = finalOffer.Count > 0;
        string detail = $"After {drains} depleting draw(s) of GetDiscoveryCards(6,3), a further draw returned " +
            $"{finalOffer.Count}/3 card(s)" +
            (pass
                ? " (master-template clone fallback kept the discovery offer non-empty)."
                : " — EMPTY: tier-6 discovery would show the player no cards to choose.");

        _recorder.RecordCheck("A5", "Tier 6 triple -> discovery offers cards (not empty)",
            pass ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail, detail, "");
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

    /// <summary>
    /// Fails the test with an aggregate message if any mechanical row check came back
    /// AUTO-FAIL. NEEDS-HUMAN rows never fail the suite — they are advisory.
    /// </summary>
    private void AssertNoAutoFailChecks()
    {
        var failures = new System.Collections.Generic.List<string>();
        foreach (var c in _recorder.Checks)
        {
            if (c.status == PlaytestEvidenceRecorder.AutoFail)
                failures.Add($"{c.row}: {c.detail}");
        }
        if (failures.Count > 0)
            Assert.Fail("AutoPlaytest mechanical checks failed:\n" + string.Join("\n", failures));
    }
}
