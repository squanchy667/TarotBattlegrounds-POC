using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using TarotBattlegrounds.Combat.Animator;

// ====================================================================================
// AutoPlaytest v1 — data-transfer objects for the JSON evidence bundle.
// Plain [Serializable] DTOs (Unity JsonUtility, not Newtonsoft): PlayModeTests.asmdef
// does not reference com.unity.nuget.newtonsoft-json, and per the harness spec the
// safer default when that reference is absent is JsonUtility. JsonUtility requires
// public fields (not properties) and cannot serialize a bare top-level List<T>, so
// every list is wrapped in a small container class below.
// ====================================================================================

[Serializable]
public class PlaytestEventRecord
{
    public float t;       // seconds since recording started (Time.realtimeSinceStartup delta)
    public string type;   // "TurnStart" | "PhaseChange" | "CombatStart" | "CombatEnd" | "Screenshot" | "Check" | ...
    public string detail;
}

[Serializable]
public class PlaytestEventLog
{
    public List<PlaytestEventRecord> events = new List<PlaytestEventRecord>();
}

[Serializable]
public class PlaytestCheckResult
{
    public string row;          // PLAYTEST_LOG.md row id, e.g. "A1"
    public string title;        // row description (mirrors PLAYTEST_LOG.md wording)
    public string status;       // "AUTO-PASS" | "AUTO-FAIL" | "NEEDS-HUMAN"
    public string detail;
    public string evidencePath; // relative path (from the bundle dir) to a screenshot/json, or ""
}

[Serializable]
public class PlaytestCheckLog
{
    public List<PlaytestCheckResult> checks = new List<PlaytestCheckResult>();
}

[Serializable]
public class PlaytestMatchSummary
{
    public int playerCount;
    public bool gameOverFired;
    public int winnerPlayerIndex = -1;
    public int totalTurns;
    public float durationRealSeconds;
    public List<int> standings = new List<int>();
}

/// <summary>
/// AutoPlaytest v1 evidence recorder. Attached to a runtime-only GameObject (created by
/// AutoPlaytestTests AFTER the Game scene has loaded — LoadSceneMode.Single would destroy
/// it otherwise). Buffers a structured event log, captures screenshots, and runs the
/// mechanical checks mapped to docs/work-orders/PLAYTEST_LOG.md rows A1/A4/A5/B4/B5,
/// then writes the whole bundle (events.json, checks.json, match_summary.json,
/// AUTO_PLAYTEST_REPORT.md, screenshots/*.png) to an output directory outside Assets.
///
/// Design notes (read before changing the B4/B5 checks):
///  - GameManager.CurrentPhase / TurnNumber are public, so phase/turn tracking is plain
///    polling — no reflection needed.
///  - CombatManager.OnCombatStart/OnCombatEnd are public static events — used directly.
///  - ShopUI / HandUI / BoardUI / MatchInfoUI / GameOverUI are all public classes, so
///    instances are fetched with FindObjectOfType(true) rather than reflection.
///  - GameUIManager's own shopUI/handUI/boardUI/matchInfoUI fields are `internal` and this
///    assembly (PlayModeTests) has no [InternalsVisibleTo] grant (only "Tests", the Editor
///    asmdef, has one — see Assets/Scripts/AssemblyInfo.cs) — so we never touch those
///    fields directly, using FindObjectOfType on the concrete UI types instead.
///  - MatchInfoUI has no public "is the panel showing" accessor (unlike GameOverUI.IsShowing),
///    so the B4 check reflects into MatchInfoUI's private `infoPanel` field. This is the one
///    spot in this file that uses reflection, and only because there's no public surface for it.
///  - B5 (combat chrome hidden) is gated on CombatAnimator.IsPlaying, NOT GameManager.CurrentPhase.
///    Reading BattleExecutor.RunBattle shows GameUIManager.SetCombatPresentationMode is only
///    invoked from CombatAnimator's PlaybackCoroutine, which BattleExecutor only starts when
///    IsLocalPlayerBattle(p1,p2) is true. GameConfig.IsHumanPlayer() unconditionally returns
///    false while CurrentGameMode == AIvsAI, so a pure AIvsAI match (as this harness runs)
///    NEVER triggers the animated replay or its chrome-hiding — that is correct product
///    behavior (nothing to hide the shop from when there's no local viewer), not a bug. If no
///    animation is ever observed, B5 is reported NEEDS-HUMAN with that explanation rather than
///    a misleading AUTO-PASS/AUTO-FAIL.
/// </summary>
public class PlaytestEvidenceRecorder : MonoBehaviour
{
    public const string AutoPass = "AUTO-PASS";
    public const string AutoFail = "AUTO-FAIL";
    public const string NeedsHuman = "NEEDS-HUMAN";

    private static readonly Regex TurnLabelPattern = new Regex(@"^Turn\s+\d+\b", RegexOptions.IgnoreCase);

    private readonly List<PlaytestEventRecord> _events = new List<PlaytestEventRecord>();
    private readonly List<PlaytestCheckResult> _checks = new List<PlaytestCheckResult>();
    private readonly Dictionary<string, string> _screenshotRelativePaths = new Dictionary<string, string>();

    public IReadOnlyList<PlaytestCheckResult> Checks => _checks;

    private string _bundleDir;
    private string _screenshotsDir;
    private float _startRealtime;
    private bool _recording;

    // Cached UI refs (found once — scene composition is static across the match).
    private ShopUI _shopUI;
    private HandUI _handUI;
    private BoardUI _boardUI;

    // B5 (combat chrome) accumulators.
    private bool _sawAnyCombatAnimationPlaying;
    private bool _sawChromeVisibleWhileAnimatorPlaying;
    private int _chromeHiddenSampleCount;
    private int _chromeVisibleSampleCount;

    // B4 (match-info auto-popup) accumulators.
    private bool _observedCombatToRecruitTransition;
    private bool _matchInfoEverShownAfterCombat;
    private int _transitionsChecked;

    // A4 (single turn label) samples.
    private readonly List<(string context, int count, string detail)> _turnLabelSamples =
        new List<(string, int, string)>();

    // ================================================================
    // Lifecycle
    // ================================================================

    /// <summary>
    /// Begin recording. Must be called AFTER the Game scene has finished loading (so
    /// GameManager.Instance and the UI singletons already exist).
    /// </summary>
    public void BeginRecording(string bundleDirectory)
    {
        _bundleDir = bundleDirectory;
        _screenshotsDir = Path.Combine(_bundleDir, "screenshots");
        Directory.CreateDirectory(_bundleDir);
        Directory.CreateDirectory(_screenshotsDir);

        _startRealtime = Time.realtimeSinceStartup;
        _recording = true;

        _shopUI = FindObjectOfType<ShopUI>(true);
        _handUI = FindObjectOfType<HandUI>(true);
        _boardUI = FindObjectOfType<BoardUI>(true);

        CombatManager.OnCombatStart += HandleCombatStart;
        CombatManager.OnCombatEnd += HandleCombatEnd;

        LogEvent("RecordingStarted", $"Bundle dir: {_bundleDir}");

        StartCoroutine(BootCapture());
        StartCoroutine(PollLoop());
    }

    /// <summary>
    /// The recorder owns the boot capture, and PollLoop's combat capture waits for it —
    /// run_20260718_220720 showed the combat shot landing BEFORE the boot shot (in AIvsAI
    /// the recruit phase is instantaneous, so the game was already in Combat two frames
    /// after scene load). Ordering is now deterministic: 01 always precedes 02.
    /// </summary>
    private IEnumerator BootCapture()
    {
        yield return CaptureScreenshotAfterFrames("01_boot_recruit_hud", 2);
        _bootCaptureDone = true;
    }

    private bool _bootCaptureDone;
    public bool BootCaptureDone => _bootCaptureDone;

    public void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        CombatManager.OnCombatStart -= HandleCombatStart;
        CombatManager.OnCombatEnd -= HandleCombatEnd;
    }

    private void OnDestroy()
    {
        StopRecording();
    }

    private void HandleCombatStart(string p1Name, string p2Name)
    {
        int turn = GameManager.Instance != null ? GameManager.Instance.TurnNumber : -1;
        LogEvent("CombatStart", $"{p1Name} vs {p2Name} (turn {turn})");
    }

    private void HandleCombatEnd(string winnerName, int damage)
    {
        LogEvent("CombatEnd", $"Winner={winnerName}, damage={damage}");
    }

    // ================================================================
    // Per-frame polling: turn/phase events + B4/B5 sampling + combat screenshot trigger
    // ================================================================

    private IEnumerator PollLoop()
    {
        var lastPhase = (GameManager.GamePhase)(-1);
        int lastTurn = -1;
        bool combatScreenshotTaken = false;

        while (_recording)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                yield return null;
                continue;
            }

            int turn = gm.TurnNumber;
            if (turn != lastTurn)
            {
                LogEvent("TurnStart", $"Turn {turn}");
                lastTurn = turn;
            }

            var phase = gm.CurrentPhase;
            if (phase != lastPhase)
            {
                LogEvent("PhaseChange", $"{lastPhase} -> {phase} (turn {turn})");

                if (lastPhase == GameManager.GamePhase.Combat && phase == GameManager.GamePhase.Recruit)
                    SampleMatchInfoNotAutoShown();

                if (phase == GameManager.GamePhase.Combat && !combatScreenshotTaken && _bootCaptureDone)
                {
                    combatScreenshotTaken = true;
                    StartCoroutine(CaptureScreenshotAfterFrames("02_combat_phase", 2));
                }

                lastPhase = phase;
            }

            bool animatorPlaying = CombatAnimator.Instance != null && CombatAnimator.Instance.IsPlaying;
            if (animatorPlaying)
            {
                _sawAnyCombatAnimationPlaying = true;
                bool hidden = IsInactive(_shopUI) && IsInactive(_handUI) && IsInactive(_boardUI);
                if (hidden)
                {
                    _chromeHiddenSampleCount++;
                }
                else
                {
                    _chromeVisibleSampleCount++;
                    _sawChromeVisibleWhileAnimatorPlaying = true;
                }
            }

            yield return null;
        }
    }

    private static bool IsInactive(Component c) => c == null || !c.gameObject.activeSelf;

    // ================================================================
    // Screenshots
    // ================================================================

    /// <summary>
    /// Waits `extraFrames` frames, then WaitForEndOfFrame, then captures the screen via
    /// ScreenCapture.CaptureScreenshotAsTexture + File.WriteAllBytes(EncodeToPNG).
    /// NOTE: this requires an actual rendered frame / graphics context. It works from the
    /// Unity Editor Test Runner (PlayMode) and from a CLI test run WITHOUT -nographics; a
    /// -nographics batch run will not have pixels to capture and the screenshot is skipped
    /// with a logged "ScreenshotError" event (not a Debug.LogError, so it will not fail the
    /// no-unexpected-LogError bar).
    /// </summary>
    public IEnumerator CaptureScreenshotAfterFrames(string name, int extraFrames)
    {
        for (int i = 0; i < Mathf.Max(1, extraFrames); i++)
            yield return null;
        // Required for CaptureScreenshotAsTexture; keep it — hangs were from stuck combat coroutines (fixed T839 SkipReplay force-stop), not EOF itself.
        yield return new WaitForEndOfFrame();
        CaptureNow(name);
    }

    private void CaptureNow(string name)
    {
        Texture2D tex = null;
        bool prevIgnore = LogAssert.ignoreFailingMessages;
        LogAssert.ignoreFailingMessages = true; // ScreenCapture can log Error if not true EOF
        try
        {
            tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null || tex.width == 0 || tex.height == 0)
            {
                LogEvent("ScreenshotError", $"{name}: CaptureScreenshotAsTexture returned no pixel data (no graphics context — likely a -nographics run).");
                return;
            }

            byte[] png = tex.EncodeToPNG();
            string relative = "screenshots/" + name + ".png";
            string fullPath = Path.Combine(_bundleDir, relative);
            File.WriteAllBytes(fullPath, png);
            _screenshotRelativePaths[name] = relative;
            var gm = GameManager.Instance;
            string state = gm != null ? $", phase={gm.CurrentPhase}, turn={gm.TurnNumber}" : "";
            LogEvent("Screenshot", $"{name} -> {relative} ({tex.width}x{tex.height}{state})");
        }
        catch (Exception e)
        {
            LogEvent("ScreenshotError", $"{name}: {e.Message}");
        }
        finally
        {
            LogAssert.ignoreFailingMessages = prevIgnore;
            if (tex != null) Destroy(tex);
        }
    }

    public string GetScreenshotRelativePath(string name) =>
        _screenshotRelativePaths.TryGetValue(name, out var p) ? p : "";

    public bool HasScreenshot(string name) => _screenshotRelativePaths.ContainsKey(name);

    // ================================================================
    // Row checks
    // ================================================================

    public void RecordCheck(string row, string title, string status, string detail, string evidenceRelativePath)
    {
        _checks.Add(new PlaytestCheckResult
        {
            row = row,
            title = title,
            status = status,
            detail = detail ?? "",
            evidencePath = evidenceRelativePath ?? ""
        });
        LogEvent("Check", $"{row} [{status}] {title}: {detail}");
    }

    /// <summary>Row A1 — game over fires and GameOverUI's panel is active.</summary>
    public void CheckGameOverUIActive()
    {
        var gameOverUI = FindObjectOfType<GameOverUI>(true);
        bool pass = gameOverUI != null && gameOverUI.IsShowing;
        string detail = gameOverUI == null
            ? "No GameOverUI component found in the scene."
            : $"GameOverUI.IsShowing = {gameOverUI.IsShowing} after GameManager.OnGameOver fired.";
        RecordCheck("A1", "Game over — win or lose shows full-screen panel (not cut off)",
            pass ? AutoPass : AutoFail, detail, GetScreenshotRelativePath("03_game_over"));
    }

    /// <summary>
    /// Row A1b — game over must be the ONLY full-screen UI. GameOverUI.Show's own comment says
    /// "Tear down combat presentation so game-over is the only full-screen UI", but
    /// run_20260718_220720's 03_game_over.png showed the CombatResultBanner and shop chrome
    /// still fully visible behind the panel. This check is the regression test for that bug:
    /// it AUTO-FAILs (and therefore fails the suite) until the teardown actually covers them.
    /// </summary>
    public void CheckGameOverExclusivity()
    {
        var visible = new List<string>();

        var banner = CombatResultBanner.Instance;
        if (banner != null)
        {
            FieldInfo rootField = typeof(CombatResultBanner).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance);
            var rootObj = rootField?.GetValue(banner) as GameObject;
            if (rootObj != null && rootObj.activeInHierarchy)
                visible.Add("CombatResultBanner.root");
        }
        if (!IsInactive(_shopUI)) visible.Add("ShopUI");
        if (!IsInactive(_handUI)) visible.Add("HandUI");
        if (!IsInactive(_boardUI)) visible.Add("BoardUI");

        RecordCheck("A1b", "Game over is the only full-screen UI (combat banner + shop chrome hidden)",
            visible.Count == 0 ? AutoPass : AutoFail,
            visible.Count == 0
                ? "No combat result banner or shop chrome active behind the game-over panel."
                : "Still active behind the game-over panel: " + string.Join(", ", visible),
            GetScreenshotRelativePath("03_game_over"));
    }

    /// <summary>Row A4 sample point — count active TMP_Text components whose text matches "Turn N".</summary>
    public void SampleTurnLabel(string context)
    {
        var matches = new List<string>();
        var texts = FindObjectsOfType<TMP_Text>(); // active-in-hierarchy only (default, no includeInactive)
        foreach (var t in texts)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            string trimmed = t.text.Trim();
            if (TurnLabelPattern.IsMatch(trimmed))
                matches.Add($"{t.gameObject.name}='{trimmed}'");
        }
        string detail = matches.Count == 0 ? "(none found)" : string.Join("; ", matches);
        _turnLabelSamples.Add((context, matches.Count, detail));
        LogEvent("Check-Sample", $"A4 sample [{context}]: {matches.Count} active 'Turn N' label(s) -> {detail}");
    }

    /// <summary>Row A4 — aggregate all SampleTurnLabel() calls into a single AUTO-PASS/AUTO-FAIL.</summary>
    public void FinalizeTurnLabelCheck()
    {
        if (_turnLabelSamples.Count == 0)
        {
            RecordCheck("A4", "One turn label only (no Turn V badge + Turn 5)", NeedsHuman,
                "No samples were taken during this run.", GetScreenshotRelativePath("01_boot_recruit_hud"));
            return;
        }

        bool anyViolation = false;
        foreach (var s in _turnLabelSamples)
            if (s.count != 1) anyViolation = true;

        var parts = new List<string>();
        foreach (var s in _turnLabelSamples)
            parts.Add($"{s.context}: {s.count} ({s.detail})");
        string summary = string.Join(" | ", parts);

        RecordCheck("A4", "One turn label only (no Turn V badge + Turn 5)",
            anyViolation ? AutoFail : AutoPass, summary, GetScreenshotRelativePath("01_boot_recruit_hud"));
    }

    /// <summary>
    /// Row B4 sample point, called from PollLoop on each Combat -> Recruit transition.
    /// Reflects into MatchInfoUI's private `infoPanel` field — MatchInfoUI exposes no public
    /// "is showing" accessor (unlike GameOverUI.IsShowing), so this is the one place in the
    /// recorder that uses reflection.
    /// </summary>
    private void SampleMatchInfoNotAutoShown()
    {
        _observedCombatToRecruitTransition = true;
        _transitionsChecked++;

        var matchInfo = FindObjectOfType<MatchInfoUI>(true);
        if (matchInfo == null)
        {
            LogEvent("Check-Sample", $"B4 sample #{_transitionsChecked}: no MatchInfoUI found in scene.");
            return;
        }

        FieldInfo field = typeof(MatchInfoUI).GetField("infoPanel", BindingFlags.NonPublic | BindingFlags.Instance);
        var panelObj = field?.GetValue(matchInfo) as GameObject;
        bool panelActive = panelObj != null && panelObj.activeSelf;
        if (panelActive) _matchInfoEverShownAfterCombat = true;

        LogEvent("Check-Sample", $"B4 sample #{_transitionsChecked}: MatchInfoUI infoPanel active = {panelActive}");
    }

    /// <summary>Row B4 — aggregate the SampleMatchInfoNotAutoShown() samples.</summary>
    public void FinalizeMatchInfoCheck()
    {
        if (!_observedCombatToRecruitTransition)
        {
            RecordCheck("B4", "No auto match-status popup after combat", NeedsHuman,
                "No Combat -> Recruit phase transition was observed during this run — could not sample MatchInfoUI state.",
                "");
            return;
        }

        RecordCheck("B4", "No auto match-status popup after combat",
            _matchInfoEverShownAfterCombat ? AutoFail : AutoPass,
            _matchInfoEverShownAfterCombat
                ? "MatchInfoUI's infoPanel was active immediately after at least one Combat -> Recruit transition."
                : $"Sampled {_transitionsChecked} Combat -> Recruit transition(s); MatchInfoUI's infoPanel stayed " +
                  "inactive every time (GameUIManager.ShowMatchInfoAfterCombat is an intentional no-op in the current code).",
            "");
    }

    /// <summary>Row B5 — aggregate the PollLoop's CombatAnimator.IsPlaying samples.</summary>
    public void FinalizeCombatChromeCheck()
    {
        string evidence = GetScreenshotRelativePath("02_combat_phase");

        if (!_sawAnyCombatAnimationPlaying)
        {
            RecordCheck("B5", "Combat: shop/hand/board hidden; arena only", NeedsHuman,
                "CombatAnimator.PlayReplay (and therefore GameUIManager.SetCombatPresentationMode) is only invoked " +
                "by BattleExecutor when IsLocalPlayerBattle(p1,p2) is true, which delegates to GameConfig.IsHumanPlayer() " +
                "— that unconditionally returns false while GameConfig.CurrentGameMode == AIvsAI. This harness runs a " +
                "pure AIvsAI match, so no battle ever triggered the animated replay / chrome-hiding path; nothing was " +
                "observed to check. Re-run as a HumanVsAI (or human) playtest to exercise this row.",
                evidence);
            return;
        }

        RecordCheck("B5", "Combat: shop/hand/board hidden; arena only",
            _sawChromeVisibleWhileAnimatorPlaying ? AutoFail : AutoPass,
            _sawChromeVisibleWhileAnimatorPlaying
                ? $"Observed {_chromeVisibleSampleCount} frame(s) where CombatAnimator.IsPlaying was true but " +
                  $"shop/hand/board chrome was still active (hidden on {_chromeHiddenSampleCount} frame(s))."
                : $"Observed {_chromeHiddenSampleCount} frame(s) with CombatAnimator.IsPlaying true; shop/hand/board " +
                  "were all inactive every time.",
            evidence);
    }

    // ================================================================
    // Bundle output
    // ================================================================

    private void LogEvent(string type, string detail)
    {
        _events.Add(new PlaytestEventRecord { t = Time.realtimeSinceStartup - _startRealtime, type = type, detail = detail });
    }

    public void WriteBundle(PlaytestMatchSummary summary)
    {
        var eventLog = new PlaytestEventLog { events = _events };
        File.WriteAllText(Path.Combine(_bundleDir, "events.json"), JsonUtility.ToJson(eventLog, true));

        var checkLog = new PlaytestCheckLog { checks = _checks };
        File.WriteAllText(Path.Combine(_bundleDir, "checks.json"), JsonUtility.ToJson(checkLog, true));

        File.WriteAllText(Path.Combine(_bundleDir, "match_summary.json"), JsonUtility.ToJson(summary, true));

        File.WriteAllText(Path.Combine(_bundleDir, "AUTO_PLAYTEST_REPORT.md"), BuildMarkdownReport(summary));
    }

    // Rows from docs/work-orders/PLAYTEST_LOG.md that this harness does not attempt to
    // automate (require human visual/interaction judgment, or aren't mechanically checkable
    // from PlayMode test code without deep replay/ability introspection). Listed explicitly
    // so the report always covers every row in the log, not just the automated ones.
    private static readonly (string row, string title, string note)[] KnownHumanRows =
    {
        ("A2", "Game over Quit to Menu works", "Requires clicking the button and observing the scene load; not driven from test code in this pass."),
        ("A3", "Game over Play Again reloads Game", "Requires clicking the button and observing the scene reload; not driven from test code in this pass."),
        ("B1", "Recruit timer large top-center", "Visual layout/size judgment — see the recruit HUD screenshot."),
        ("B2", "Settings opens; labels horizontal", "Interaction feel + layout judgment; gear was not clicked by this harness."),
        ("B3", "Match info opens/closes; not stacked on gear", "Interaction feel + layout judgment; the 'i' button was not clicked by this harness."),
        ("B6", "Synergies left panel shows; counts change with tribes", "Visual/content judgment — see the recruit HUD screenshot."),
        ("B7", "Reborn (e.g. Shooting Star) only once per combat", "Needs per-card ability trigger-count assertions against CombatReplay; out of scope for this pass."),
        ("C1", "Combat cards brighter / readable", "Visual contrast/readability judgment — see the combat phase screenshot."),
        ("C2", "Match Status redesign", "Design judgment; MatchInfoUI is on-demand only (see B4) so it was never shown during this automated run."),
        ("C3", "Synergy tooltips (what 2/4/6 does)", "Content/interaction judgment — see the recruit HUD screenshot."),
        ("C4", "Shop card frames complete", "Visual/asset judgment — see the recruit HUD screenshot."),
    };

    private string BuildMarkdownReport(PlaytestMatchSummary summary)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# AutoPlaytest Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## Match Summary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Players | {summary.playerCount} |");
        sb.AppendLine($"| Game Over Fired | {summary.gameOverFired} |");
        sb.AppendLine($"| Winner | {(summary.winnerPlayerIndex >= 0 ? $"Player {summary.winnerPlayerIndex + 1}" : "(none)")} |");
        sb.AppendLine($"| Turns Played | {summary.totalTurns} |");
        sb.AppendLine($"| Duration (real seconds) | {summary.durationRealSeconds:0.0} |");
        string standingsStr = summary.standings.Count == 0
            ? "(none)"
            : string.Join(", ", summary.standings.ConvertAll(i => $"P{i + 1}"));
        sb.AppendLine($"| Standings (1st..last) | {standingsStr} |");
        sb.AppendLine();

        string dupNote = DuplicateScreenshotNote();
        if (!string.IsNullOrEmpty(dupNote))
        {
            sb.AppendLine(dupNote);
            sb.AppendLine();
        }

        sb.AppendLine("## Automated Row Checks");
        sb.AppendLine();
        sb.AppendLine("| Row | Status | Detail | Evidence |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var c in _checks)
        {
            string ev = string.IsNullOrEmpty(c.evidencePath) ? "—" : c.evidencePath;
            sb.AppendLine($"| {c.row} — {EscapePipes(c.title)} | {c.status} | {EscapePipes(c.detail)} | {ev} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Rows Needing Human Judgment");
        sb.AppendLine();
        sb.AppendLine("| Row | Check | Notes | Evidence |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var r in KnownHumanRows)
        {
            string ev = "—";
            if (r.row == "B1" || r.row == "B6" || r.row == "C3" || r.row == "C4")
                ev = string.IsNullOrEmpty(GetScreenshotRelativePath("01_boot_recruit_hud")) ? "—" : GetScreenshotRelativePath("01_boot_recruit_hud");
            else if (r.row == "C1")
                ev = string.IsNullOrEmpty(GetScreenshotRelativePath("02_combat_phase")) ? "—" : GetScreenshotRelativePath("02_combat_phase");
            sb.AppendLine($"| {r.row} | {EscapePipes(r.title)} | {EscapePipes(r.note)} | {ev} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Event Log");
        sb.AppendLine();
        sb.AppendLine("Full structured timeline (turn starts, phase changes, combat results, screenshots, checks) is in `events.json`.");
        sb.AppendLine($"Total events recorded: {_events.Count}.");

        return sb.ToString();
    }

    /// <summary>
    /// Flags byte-identical boot/combat screenshots in the report. In AIvsAI this can happen
    /// legitimately (instant recruit phase + no combat animation for a non-local viewer), so
    /// the note explains it instead of letting the duplicate silently pose as two moments.
    /// </summary>
    private string DuplicateScreenshotNote()
    {
        string a = Path.Combine(_bundleDir, "screenshots", "01_boot_recruit_hud.png");
        string b = Path.Combine(_bundleDir, "screenshots", "02_combat_phase.png");
        if (!File.Exists(a) || !File.Exists(b)) return "";
        byte[] ba = File.ReadAllBytes(a), bb = File.ReadAllBytes(b);
        if (ba.Length != bb.Length) return "";
        for (int i = 0; i < ba.Length; i++)
            if (ba[i] != bb[i]) return "";
        return "> **Note:** the boot and combat screenshots are byte-identical. In AIvsAI the recruit " +
               "phase is instantaneous (all AIs auto-ready) and combat never animates for a non-local " +
               "viewer, so the screen is static across both captures. Recruit-HUD visual rows " +
               "(B1/B6/C3/C4) need a HumanVsAI run (T839) for valid evidence.";
    }

    private static string EscapePipes(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|").Replace("\n", " ");
}
