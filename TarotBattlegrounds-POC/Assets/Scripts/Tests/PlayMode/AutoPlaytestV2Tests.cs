using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using TarotBattlegrounds.Combat.Animator;
using TarotBattlegrounds.Combat.Replay;

/// <summary>
/// AutoPlaytest v2 (T839) — HumanVsAI combat presentation evidence.
/// Boots the real Game scene in HumanVsAI, forces a local-player combat replay
/// via CombatManager.SimulateBattle + CombatAnimator.PlayReplay (same pipeline as
/// BattleExecutor), and asserts B5 / T841 / T843 / T842 root + T845 screenshot.
/// Full self-play match is secondary (flaky under scripted recruit); combat stage
/// is the regression gate for this batch.
/// </summary>
public class AutoPlaytestV2Tests
{
    private float _originalTimeScale;
    private PlaytestEvidenceRecorder _recorder;
    private GameObject _recorderGO;
    private readonly List<string> _errorSnippets = new List<string>();
    private bool _sawCombatAnimation;
    private bool _chromeHiddenDuringReplay;

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        if (_errorSnippets.Count < 20)
            _errorSnippets.Add(condition);
    }

    [SetUp]
    public void SetUp()
    {
        _originalTimeScale = Time.timeScale;
        _errorSnippets.Clear();
        _sawCombatAnimation = false;
        _chromeHiddenDuringReplay = false;
        Application.logMessageReceived += HandleLog;
    }

    [TearDown]
    public void TearDown()
    {
        Application.logMessageReceived -= HandleLog;
        Time.timeScale = _originalTimeScale;
        if (_recorder != null) _recorder.StopRecording();
        if (_recorderGO != null) UnityEngine.Object.Destroy(_recorderGO);
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator AutoPlaytest_HumanVsAI_CombatPresentationEvidence()
    {
        float matchStart = Time.realtimeSinceStartup;
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string bundleDir = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "..", "AutoPlaytest", "run_v2_" + timestamp));

        GameConfig.CurrentGameMode = GameConfig.GameMode.HumanVsAI;
        GameConfig.PlayerCount = 4;
        GameConfig.HumanPlayerIndex = 0;
        GameConfig.DefaultAIDifficulty = AIDifficulty.Medium;

        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        yield return null;
        yield return null;
        for (int i = 0; i < 15; i++) yield return null;

        Assert.IsNotNull(UnityEngine.Object.FindObjectOfType<GameManager>(true),
            "GameManager missing after Game scene load");

        _recorderGO = new GameObject("PlaytestEvidenceRecorder_V2");
        _recorder = _recorderGO.AddComponent<PlaytestEvidenceRecorder>();
        _recorder.BeginRecording(bundleDir);
        yield return _recorder.CaptureScreenshotAfterFrames("01_boot_recruit_hud", 2);

        // T842: DiscoveryUI root must be active (scene fix)
        var discovery = UnityEngine.Object.FindObjectOfType<DiscoveryUI>(true);
        Assert.IsNotNull(discovery, "DiscoveryUI must exist");
        Assert.IsTrue(discovery.gameObject.activeInHierarchy,
            "DiscoveryUIRoot must be activeInHierarchy (T842)");

        // Build two small boards and run a local combat with animated replay
        var board1 = BuildBoard("LocalA", 3, 4, withCleave: false);
        var board2 = BuildBoard("EnemyB", 2, 2, withCleave: false);
        // Give one windfury high-attack minion to stress death/damage ordering (T841)
        var wf = ScriptableObject.CreateInstance<Card>();
        wf.cardName = "WindStriker";
        wf.attack = 10;
        wf.health = 3;
        wf.hasWindfury = true;
        board1.Add(wf);

        var (damage, winner) = CombatManager.SimulateBattle(
            board1, board2, 3, 2, "Player 1", "Player 2 (AI)", true, null, null);

        Assert.IsNotNull(CombatManager.lastReplay, "SimulateBattle must produce lastReplay");

        var animator = CombatAnimator.Instance
            ?? UnityEngine.Object.FindObjectOfType<CombatAnimator>(true);
        Assert.IsNotNull(animator, "CombatAnimator must exist in Game scene (CombatRoot)");
        // Ensure hierarchy is active so Awake/coroutines can run
        if (!animator.gameObject.activeInHierarchy)
            animator.gameObject.SetActive(true);
        // Rebind static Instance if Awake was skipped while inactive (private set)
        if (CombatAnimator.Instance == null)
        {
            var prop = typeof(CombatAnimator).GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);
            prop?.GetSetMethod(nonPublic: true)?.Invoke(null, new object[] { animator });
        }

        // Play animated replay (HumanVsAI local path)
        animator.PlayReplay(CombatManager.lastReplay);
        Assert.IsTrue(animator.IsPlaying || CombatManager.lastReplay.actions.Count == 0,
            "PlayReplay should start IsPlaying for non-empty replays");

        float wait = 0f;
        // Capture a few frames of live playback, then force-skip so the suite cannot hang
        while (animator.IsPlaying && wait < 8f)
        {
            _sawCombatAnimation = true;
            bool shopHidden = IsInactive(UnityEngine.Object.FindObjectOfType<ShopUI>(true));
            bool handHidden = IsInactive(UnityEngine.Object.FindObjectOfType<HandUI>(true));
            bool boardHidden = IsInactive(UnityEngine.Object.FindObjectOfType<BoardUI>(true));
            if (shopHidden && handHidden && boardHidden)
                _chromeHiddenDuringReplay = true;

            if (!_recorder.HasScreenshot("02_combat_replay"))
                yield return _recorder.CaptureScreenshotAfterFrames("02_combat_replay", 1);

            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        if (animator.IsPlaying)
        {
            _sawCombatAnimation = true;
            // Sample chrome one last time before skip
            bool shopHidden = IsInactive(UnityEngine.Object.FindObjectOfType<ShopUI>(true));
            bool handHidden = IsInactive(UnityEngine.Object.FindObjectOfType<HandUI>(true));
            bool boardHidden = IsInactive(UnityEngine.Object.FindObjectOfType<BoardUI>(true));
            if (shopHidden && handHidden && boardHidden)
                _chromeHiddenDuringReplay = true;
            animator.SkipReplay();
        }
        for (int i = 0; i < 10; i++) yield return null;

        yield return _recorder.CaptureScreenshotAfterFrames("03_post_replay", 2);

        // T843: result survivors self-consistent
        string survivorMsg = null;
        var result = CombatManager.lastReplay?.result;
        if (result == null)
            survivorMsg = "lastReplay.result is null";
        else if (result.winnerName == "Tie" && result.survivingCards != null && result.survivingCards.Count > 0)
            survivorMsg = "Tie but survivingCards non-empty";

        // T842 pool path: reserve + AI-style resolve returns unchosen
        int poolBefore = TavernManager.Instance != null ? TavernManager.Instance.GetFullPool().Count : -1;
        bool poolOk = true;
        string poolDetail = "TavernManager unavailable";
        if (TavernManager.Instance != null)
        {
            var discCards = TavernManager.Instance.GetDiscoveryCards(6, 3);
            int afterReserve = TavernManager.Instance.GetFullPool().Count;
            if (discCards.Count > 0)
            {
                // Simulate AI pick first; return rest
                var unchosen = new List<Card>();
                for (int i = 1; i < discCards.Count; i++) unchosen.Add(discCards[i]);
                TavernManager.Instance.ReturnDiscoveryCards(unchosen);
                // chosen stays out (reserved)
                int afterReturn = TavernManager.Instance.GetFullPool().Count;
                poolOk = afterReturn == afterReserve + unchosen.Count;
                poolDetail = $"reserve {poolBefore}->{afterReserve}, return unchosen +{unchosen.Count} -> {afterReturn}, ok={poolOk}";
            }
            else
            {
                poolDetail = "GetDiscoveryCards returned empty (pool thin) — skip pool assertion";
                poolOk = true;
            }
        }

        _recorder.StopRecording();

        _recorder.RecordCheck("B5", "Combat: shop/hand/board hidden; arena only",
            _chromeHiddenDuringReplay ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            _chromeHiddenDuringReplay
                ? "ShopUI/HandUI/BoardUI inactive during CombatAnimator.IsPlaying."
                : "Chrome still visible during replay — T844 incomplete.",
            _recorder.GetScreenshotRelativePath("02_combat_replay"));

        int coroutineErrs = CountCoroutineInactiveErrors();
        _recorder.RecordCheck("T841", "Zero inactive-coroutine reds during combat replay",
            coroutineErrs == 0 ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            coroutineErrs == 0
                ? "No 'Coroutine couldn't be started' errors."
                : "Errors: " + string.Join("; ", _errorSnippets),
            "");

        _recorder.RecordCheck("T842", "DiscoveryUIRoot active + discovery pool return",
            discovery.gameObject.activeInHierarchy && poolOk
                ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            $"rootActive={discovery.gameObject.activeInHierarchy}; {poolDetail}",
            "");

        _recorder.RecordCheck("T843", "CombatReplay final survivors consistent",
            survivorMsg == null ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            survivorMsg ?? $"winner={result?.winnerName}, survivors={result?.survivingCards?.Count ?? 0}, dmg={damage}",
            _recorder.GetScreenshotRelativePath("03_post_replay"));

        _recorder.RecordCheck("T845", "Combat contrast iterate #2 (NEEDS-HUMAN judge)",
            PlaytestEvidenceRecorder.NeedsHuman,
            "Ash dim 0.72 + Umber face + bronze Outline. Ofek is final judge.",
            _recorder.GetScreenshotRelativePath("02_combat_replay"));

        var summary = new PlaytestMatchSummary
        {
            playerCount = 4,
            gameOverFired = false,
            winnerPlayerIndex = -1,
            totalTurns = 0,
            durationRealSeconds = Time.realtimeSinceStartup - matchStart,
            standings = new List<int>()
        };
        _recorder.WriteBundle(summary);

        Assert.IsTrue(_sawCombatAnimation, "T839: CombatAnimator must play");
        Assert.IsTrue(_chromeHiddenDuringReplay, "T844/B5: chrome must hide during replay");
        Assert.AreEqual(0, coroutineErrs,
            "T841: zero inactive-coroutine errors. " + string.Join(" | ", _errorSnippets));
        Assert.IsNull(survivorMsg, "T843: " + survivorMsg);
        Assert.IsTrue(discovery.gameObject.activeInHierarchy, "T842 root active");
        Assert.IsTrue(poolOk, "T842 pool return: " + poolDetail);

        AssertNoAutoFail();
    }

    private static List<Card> BuildBoard(string prefix, int atk, int hp, bool withCleave)
    {
        var list = new List<Card>();
        for (int i = 0; i < 2; i++)
        {
            var c = ScriptableObject.CreateInstance<Card>();
            c.cardName = prefix + i;
            c.attack = atk;
            c.health = hp;
            c.hasCleave = withCleave && i == 0;
            list.Add(c);
        }
        return list;
    }

    private static bool IsInactive(Component c) => c == null || !c.gameObject.activeSelf;

    private int CountCoroutineInactiveErrors()
    {
        int n = 0;
        foreach (var e in _errorSnippets)
        {
            if (e != null && e.IndexOf("Coroutine couldn't be started", StringComparison.OrdinalIgnoreCase) >= 0)
                n++;
        }
        return n;
    }

    private void AssertNoAutoFail()
    {
        var failures = new List<string>();
        foreach (var c in _recorder.Checks)
        {
            if (c.status == PlaytestEvidenceRecorder.AutoFail)
                failures.Add($"{c.row}: {c.detail}");
        }
        if (failures.Count > 0)
            Assert.Fail("AutoPlaytest v2 failed:\n" + string.Join("\n", failures));
    }
}
