#!/bin/bash
# Log Analyzer Command - Quick access to multiplayer log analysis

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOGS_DIR="$PROJECT_ROOT/test-logs"
ANALYZER="$PROJECT_ROOT/analyze_logs.py"

echo "🔍 Tarot Battlegrounds - Log Analyzer"
echo "====================================="
echo ""

# Check if logs directory exists
if [ ! -d "$LOGS_DIR" ]; then
    echo "❌ Error: test-logs directory not found!"
    echo "Creating directory: $LOGS_DIR"
    mkdir -p "$LOGS_DIR"
fi

# Check for log files
HOST_LOG=""
CLIENT_LOG=""

# Try to find most recent logs automatically
if [ -z "$1" ]; then
    echo "📂 Searching for log files in test-logs/..."

    # Find most recent files
    HOST_LOG=$(ls -t "$LOGS_DIR"/*regular*.txt "$LOGS_DIR"/*host*.txt "$LOGS_DIR"/*editor*.txt 2>/dev/null | head -1)
    CLIENT_LOG=$(ls -t "$LOGS_DIR"/*clone*.txt "$LOGS_DIR"/*client*.txt 2>/dev/null | head -1)

    if [ -z "$HOST_LOG" ] || [ -z "$CLIENT_LOG" ]; then
        echo ""
        echo "❌ Could not find log files automatically."
        echo ""
        echo "📋 Usage:"
        echo "  ./analyze-logs.sh                    # Auto-detect most recent logs"
        echo "  ./analyze-logs.sh <host> <client>    # Specify log files"
        echo ""
        echo "💡 Expected log files in test-logs/:"
        echo "  - Host:   *regular.txt, *host.txt, or *editor.txt"
        echo "  - Client: *clone.txt or *client.txt"
        echo ""
        exit 1
    fi

    echo "✅ Found host log:   $(basename "$HOST_LOG")"
    echo "✅ Found client log: $(basename "$CLIENT_LOG")"
else
    HOST_LOG="$1"
    CLIENT_LOG="$2"

    if [ -z "$CLIENT_LOG" ]; then
        echo "❌ Error: Both host and client logs required"
        echo "Usage: ./analyze-logs.sh <host_log> <client_log>"
        exit 1
    fi
fi

echo ""
echo "🔍 Running analysis..."
echo ""

# Run the analyzer
python3 "$ANALYZER" "$HOST_LOG" "$CLIENT_LOG"

echo ""
echo "✅ Analysis complete!"
echo "📄 Full report: test-logs/analysis_report.md"
echo ""
