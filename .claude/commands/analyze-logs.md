# analyze-logs - Automated Multiplayer Log Analysis

Analyze Unity console logs from ParrelSync multiplayer testing to identify bugs, desyncs, and issues.

## Quick Start

```bash
# From project root - auto-detects most recent logs
./analyze-logs.sh

# Or specify logs manually
./analyze-logs.sh test-logs/host.txt test-logs/client.txt
```

## Full Workflow

See `TESTING_WORKFLOW.md` for complete testing methodology.

## Usage

This command will:
1. Look for log files in `test-logs/` directory
2. Parse host and client logs
3. Compare events for desyncs
4. Generate prioritized bug report
5. Map findings to Phase M tasks (M1-M8)

## Expected Files

- `test-logs/host_console.log` - Host player's Unity console output
- `test-logs/client_console.log` - Client player's Unity console output

## Output

Generates:
- `test-logs/analysis_report.md` - Detailed bug report
- Console summary of critical findings

## How to Capture Logs

### From Unity Console (Manual)
1. **Host Editor:** Right-click Console → Select All → Copy
2. Save to `test-logs/host_console.log`
3. **Client Editor (ParrelSync):** Same process
4. Save to `test-logs/client_console.log`
5. Run `/analyze-logs`

### From Unity Log Files (Automatic)
Unity saves logs to:
- **Mac:** `~/Library/Logs/Unity/Editor.log`
- **Windows:** `%LOCALAPPDATA%\Unity\Editor\Editor.log`

Copy the relevant sections to test-logs directory.

## What Gets Analyzed

### M1: Synergy Per-Player
- Checks for separate synergy snapshots
- Flags global state overwrites
- Verifies tribe counts per player

### M2: Discovery Queue
- Tracks discovery UI events
- Checks for race conditions
- Verifies per-player choices

### M3: Shop Sync
- Compares shop card counts (Host vs Client)
- Detects CardLookup deserialization failures
- Flags "Template not found" errors

### M4: Buy RPC
- Validates RPC flow (request → host → response)
- Checks shopIndex bounds
- Confirms card additions and coin deductions

### M5: Upgrade Cost
- Compares costs at same turn number
- Detects cost desyncs
- Verifies lifecycle event triggers
- Checks cost resets after upgrades

### M6: Memory Cleanup
- Finds AbilityManager.ClearAll() calls
- Detects stale ability registrations
- Flags duplicate registrations

### M7: Combat Log Filter
- Verifies local player filtering
- Checks for "Skipping battle" messages
- Confirms only local battles shown

### M8: Coin Events
- Tracks coin changes
- Verifies OnCoinsChanged events fire
- Checks coin progression (3→4→5→...→10)

## Desync Detection

The analyzer automatically detects:
- **Value Mismatches:** Same event, different values (e.g., Host sees 4, Client sees 5)
- **Missing Events:** Event in Host log but not Client (or vice versa)
- **Timing Issues:** Events out of order between Host/Client
- **State Divergence:** Accumulating differences over time

## Report Format

```markdown
# Phase M - Log Analysis Report

## 🎯 Critical Issues (P0) - MUST FIX
[Blocking bugs that prevent multiplayer from working]

## ⚠️ Important Issues (P1) - SHOULD FIX
[UX issues that affect gameplay but not blocking]

## 💡 Minor Issues (P2) - COULD FIX
[Polish items, not urgent]

## ✅ Verified Working
[Features confirmed to work correctly]

## 📊 Test Statistics
- Duration: X minutes
- Turns completed: Y
- Events analyzed: Z
- Desyncs found: W
```

## Example Output

```
🔍 Analyzing logs...
   ├─ Reading host_console.log (2,456 lines)
   ├─ Reading client_console.log (2,398 lines)
   ├─ Parsing events...
   ├─ Comparing Host vs Client...
   └─ Generating report...

📊 Analysis Complete!

🎯 Critical Issues Found: 2
   ❌ M5: Upgrade cost desync at Turn 2 (Host:4, Client:5)
   ❌ M3: Shop shows 2 cards instead of 3 (Client)

⚠️ Important Issues: 1
   🟡 M2: Discovery UI appeared for both players simultaneously

✅ Working Systems: 5
   ✓ M1: Synergy calculations correct
   ✓ M4: Buy RPC working
   ✓ M6: AbilityManager cleanup working
   ✓ M7: Combat log filtered correctly
   ✓ M8: Coin events firing

📄 Full report: test-logs/analysis_report.md
```

## Tips

### Get Better Logs
- Clear console before starting test
- Enable timestamps (Unity Preferences → Console → Show Timestamp)
- Use debug tags: `[M1]`, `[M2]`, etc. in your own logs

### Focus Analysis
If you only want to check specific tasks:
```bash
/analyze-logs --tasks M3,M4,M5
```

### Include Player Notes
Add a file `test-logs/tester_notes.txt` with your observations:
```
Turn 3: Host upgraded, saw cost stay at 8
Turn 5: Client bought card but it didn't appear
Combat Phase 2: Wrong synergy bonus applied
```

The analyzer will include these in context.

## Troubleshooting

**"No log files found"**
- Check files exist: `ls test-logs/`
- Ensure correct names: `host_console.log`, `client_console.log`

**"Failed to parse logs"**
- Ensure logs are UTF-8 text
- Check for binary/corrupted data
- Try with smaller log section

**"No events found"**
- Verify logs are from actual gameplay (not just startup)
- Check logs contain `[` tags or timestamps

## Advanced Usage

### Compare Multiple Test Runs
```bash
/analyze-logs --compare test-logs/run1/ test-logs/run2/
```

### Export for GitHub Issue
```bash
/analyze-logs --format github
```
Generates markdown ready to paste into GitHub issue.

### JSON Output (for scripts)
```bash
/analyze-logs --format json > results.json
```

## Integration

Works with:
- **PHASE_M_TEST_SHEET.md:** Cross-reference findings with test cases
- **unity-game-developer:** Auto-creates fix tasks for bugs found
- **tarot-orchestrator:** Updates PLAN.md with new issues

## Next Steps After Analysis

Based on report severity:

**If P0 issues found:**
1. Stop testing
2. Review bug details in report
3. Dispatch unity-game-developer to fix
4. Re-test after fix

**If only P1/P2 issues:**
1. Continue testing remaining tasks
2. Document in known-issues.md
3. Fix during polish phase

**If all ✅:**
1. Mark tests as PASS in PHASE_M_TEST_SHEET.md
2. Update PLAN.md to 100% complete
3. Move to Phase I

## Future Enhancements

- [ ] Real-time log monitoring (watch mode)
- [ ] Automated test execution + analysis
- [ ] Machine learning for pattern detection
- [ ] Performance profiling integration
- [ ] Visual timeline of events

---

**Ready to analyze your logs!** Just paste them into the test-logs directory and run `/analyze-logs`. 🔍✨
