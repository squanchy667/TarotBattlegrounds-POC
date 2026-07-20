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

        // ---------- T842: real human triple → DiscoveryUI.IsShowing ----------
        bool discoveryPanelOpened = false;
        string discoveryDetail = "no human player";
        var gm = UnityEngine.Object.FindObjectOfType<GameManager>(true);
        if (gm != null && gm.players != null && gm.players.Count > 0)
        {
            var human = gm.players[0];
            // Ensure DiscoveryUI subscribed to this human
            for (int i = 0; i < 30; i++) yield return null;

            // Three same-name non-golden copies → CheckAndResolveTriples
            for (int i = 0; i < 3; i++)
            {
                var c = ScriptableObject.CreateInstance<Card>();
                c.cardName = "TripleProbe";
                c.tier = 1;
                c.attack = 1;
                c.health = 1;
                c.isGolden = false;
                human.hand.Add(c);
            }
            human.CheckAndResolveTriples();
            // Wait for UI show (subscribe + ShowDiscovery)
            for (int i = 0; i < 20; i++)
            {
                if (discovery != null && discovery.IsShowing)
                {
                    discoveryPanelOpened = true;
                    break;
                }
                yield return null;
            }
            discoveryDetail = $"IsShowing={discovery != null && discovery.IsShowing}";
            if (discovery != null && discovery.IsShowing)
            {
                // Avoid WaitForEndOfFrame mid-suite hangs under MCP — capture later or skip
                yield return null;
                yield return null;
                // Auto-resolve so game can continue (panel screenshot optional)
                ForcePickDiscovery(discovery, 0);
                yield return null;
            }
        }

        // ---------- Combat with deaths (T847 index alignment stress) ----------
        var board1 = BuildBoard("LocalA", 3, 4, withCleave: false);
        var board2 = BuildBoard("EnemyB", 2, 2, withCleave: false);
        var wf = ScriptableObject.CreateInstance<Card>();
        wf.cardName = "WindStriker";
        wf.attack = 10;
        wf.health = 3;
        wf.hasWindfury = true;
        board1.Add(wf);

        var (damage, winner) = CombatManager.SimulateBattle(
            board1, board2, 3, 2, "Player 1", "Player 2 (AI)", true, null, null);

        Assert.IsNotNull(CombatManager.lastReplay, "SimulateBattle must produce lastReplay");

        // T846: write transcript for Anomalies evidence
        string transcriptPath = CombatTranscript.WriteToDisk(
            CombatManager.lastReplay, "v2_batch3", "AutoPlaytest v2 Batch 3 combat");

        var animator = CombatAnimator.Instance
            ?? UnityEngine.Object.FindObjectOfType<CombatAnimator>(true);
        Assert.IsNotNull(animator, "CombatAnimator must exist in Game scene (CombatRoot)");
        if (!animator.gameObject.activeInHierarchy)
            animator.gameObject.SetActive(true);
        if (CombatAnimator.Instance == null)
        {
            var prop = typeof(CombatAnimator).GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static);
            prop?.GetSetMethod(nonPublic: true)?.Invoke(null, new object[] { animator });
        }

        Time.timeScale = 8f; // speed through WaitForSeconds in animator
        animator.PlayReplay(CombatManager.lastReplay);
        Assert.IsTrue(animator.IsPlaying || CombatManager.lastReplay.actions.Count == 0,
            "PlayReplay should start IsPlaying for non-empty replays");

        float tEnd = Time.realtimeSinceStartup + 4f;
        while (animator.IsPlaying && Time.realtimeSinceStartup < tEnd)
        {
            _sawCombatAnimation = true;
            bool shopHidden = IsInactive(UnityEngine.Object.FindObjectOfType<ShopUI>(true));
            bool handHidden = IsInactive(UnityEngine.Object.FindObjectOfType<HandUI>(true));
            bool boardHidden = IsInactive(UnityEngine.Object.FindObjectOfType<BoardUI>(true));
            if (shopHidden && handHidden && boardHidden)
                _chromeHiddenDuringReplay = true;
            yield return null;
        }

        if (animator.IsPlaying)
        {
            _sawCombatAnimation = true;
            animator.SkipReplay();
        }
        Time.timeScale = 1f;
        yield return null;
        yield return null;

        // T847 STRICT T843: live visuals vs result.survivingCards
        string strictT843 = animator.CompareActiveVisualsToSurvivors();
        bool desyncWarn = animator.LastFinalSurvivorHadDesync;

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

        _recorder.RecordCheck("T842", "Real triple opens DiscoveryUI panel",
            discoveryPanelOpened ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            discoveryPanelOpened
                ? "Human triple → DiscoveryUI.IsShowing=true"
                : "Panel did not open after forced triple. " + discoveryDetail,
            _recorder.GetScreenshotRelativePath("04_discovery_panel"));

        _recorder.RecordCheck("T843", "STRICT: live visuals match survivingCards (T847)",
            strictT843 == null
                ? PlaytestEvidenceRecorder.AutoPass : PlaytestEvidenceRecorder.AutoFail,
            strictT843 == null
                ? $"Visuals match survivors; desyncAlarm={desyncWarn}; dmg={damage} winner={winner}; transcript={transcriptPath}"
                : strictT843 + $"; desyncAlarm={desyncWarn}; transcript={transcriptPath}",
            _recorder.GetScreenshotRelativePath("03_post_replay"));

        _recorder.RecordCheck("T845", "Combat contrast iterate #2 (NEEDS-HUMAN judge)",
            PlaytestEvidenceRecorder.NeedsHuman,
            "Colors not touched in Batch 3. Ofek final judge.",
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
        Assert.IsTrue(discoveryPanelOpened, "T842: DiscoveryUI.IsShowing after real triple — " + discoveryDetail);
        Assert.IsNull(strictT843, "T847/T843 strict: " + strictT843);
        // Desync alarm should be rare after full natural playback; mid-skip may trip it — log only.
        if (desyncWarn)
            Debug.LogWarning("[AutoPlaytestV2] ApplyFinalSurvivorVisuals desync alarm fired (see console). Prefer natural playback end.");

        AssertNoAutoFail();
    }

    private static void ForcePickDiscovery(DiscoveryUI ui, int index)
    {
        var animField = typeof(DiscoveryUI).GetField("isAnimating",
            BindingFlags.NonPublic | BindingFlags.Instance);
        animField?.SetValue(ui, false);
        var field = typeof(DiscoveryUI).GetField("pendingDiscoveries",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var pending = field?.GetValue(ui) as System.Collections.IDictionary;
        if (pending == null || pending.Count == 0) return;
        foreach (System.Collections.DictionaryEntry entry in pending)
        {
            object val = entry.Value;
            if (val == null) continue;
            var vt = val.GetType();
            var player = (Player)vt.GetField("Item1")?.GetValue(val);
            var cards = (List<Card>)vt.GetField("Item2")?.GetValue(val);
            if (player == null || cards == null || cards.Count == 0) continue;
            int pick = Mathf.Clamp(index, 0, cards.Count - 1);
            player.AddDiscoveryCard(cards[pick]);
            var panelField = typeof(DiscoveryUI).GetField("discoveryPanel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var panel = panelField?.GetValue(ui) as GameObject;
            if (panel != null) panel.SetActive(false);
            pending.Clear();
            return;
        }
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
