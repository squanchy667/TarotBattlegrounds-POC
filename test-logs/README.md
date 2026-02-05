# Test Logs Directory

This directory is for storing Unity console logs from ParrelSync multiplayer testing.

## 📥 How to Save Your Logs

### Method 1: From Unity Console (Recommended)

**Host Editor:**
1. After your test run, go to Unity Console
2. Right-click anywhere in console → **Select All** (or Cmd+A / Ctrl+A)
3. Right-click → **Copy** (or Cmd+C / Ctrl+C)
4. Paste into `host_console.log` in this directory

**Client Editor (ParrelSync Clone):**
1. Same process as above
2. Paste into `client_console.log` in this directory

### Method 2: From Unity Log Files

Unity automatically saves logs here:
- **Mac:** `~/Library/Logs/Unity/Editor.log`
- **Windows:** `%LOCALAPPDATA%\Unity\Editor\Editor.log`

Copy the relevant sections to this directory.

### Method 3: Using Unity's Export

1. Console → Right-click → **Export Selection** (if available in your Unity version)
2. Save as `host_console.log` or `client_console.log`

## 🔍 Analyze Your Logs

Once you have both files:

```bash
# From project root
python analyze_logs.py test-logs/host_console.log test-logs/client_console.log
```

Or use the Claude command:
```bash
/analyze-logs
```

## 📄 Expected Files

- `host_console.log` - Host player's console output
- `client_console.log` - Client player's console output
- `tester_notes.txt` (optional) - Your observations during testing
- `analysis_report.md` (generated) - Automated bug report

## 📊 What Gets Analyzed

The analyzer checks:
- ✅ M1: Synergy per-player state
- ✅ M2: Discovery queue
- ✅ M3: Shop sync (card counts, CardLookup)
- ✅ M4: Buy RPC (validation, errors)
- ✅ M5: Upgrade cost (desyncs at same turn)
- ✅ M6: Memory cleanup (AbilityManager.ClearAll)
- ✅ M7: Combat log filter
- ✅ M8: Coin events

## 🎯 Quick Start

**Right now, paste your logs:**

1. Create `host_console.log` and paste host editor's console
2. Create `client_console.log` and paste client editor's console
3. Run: `python analyze_logs.py test-logs/host_console.log test-logs/client_console.log`
4. Check `analysis_report.md` for results

## 💡 Tips

**Get Better Logs:**
- Clear console before starting test (right-click → Clear)
- Enable timestamps: Unity Preferences → Console → Show Timestamp
- Run complete test (at least 5-10 turns)
- Include combat phases

**Add Your Notes:**
Create `tester_notes.txt`:
```
Turn 3: Host upgraded, cost showed 8 (expected 7)
Turn 5: Client bought card but didn't appear in hand
Combat Phase 2: Synergies looked wrong for Player 2
```

The analyzer will include these as context.

## 🐛 If You Find Bugs

The analyzer generates a prioritized report:
- **P0 (Critical):** Must fix before proceeding
- **P1 (Important):** Should fix soon
- **P2 (Minor):** Polish items

Follow up with:
```
claude --agent unity-game-developer
```
And share the bug report for immediate fixes.

---

**Ready to paste your logs and find bugs! 🔍✨**
