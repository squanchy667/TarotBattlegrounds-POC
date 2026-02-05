#!/usr/bin/env bash
set -euo pipefail

# Tarot Battlegrounds — Deployment Validation
# Usage: bash AWS/scripts/validate-deployment.sh <cloudfront-domain>
# Example: bash AWS/scripts/validate-deployment.sh d1234abcdef.cloudfront.net

if [ $# -eq 0 ]; then
    echo "Usage: bash AWS/scripts/validate-deployment.sh <cloudfront-domain>"
    echo "Example: bash AWS/scripts/validate-deployment.sh d1234abcdef.cloudfront.net"
    exit 1
fi

DOMAIN="$1"
URL="https://$DOMAIN"
PASS=0
FAIL=0

check() {
    local name="$1"
    local result="$2"
    if [ "$result" = "PASS" ]; then
        echo "  [PASS] $name"
        PASS=$((PASS + 1))
    else
        echo "  [FAIL] $name — $result"
        FAIL=$((FAIL + 1))
    fi
}

echo "=== Validating Deployment: $URL ==="
echo ""

# --- Check 1: HTTP 200 ---
echo "--- Check 1: HTTP Response ---"
HTTP_CODE=$(curl -sI -o /dev/null -w "%{http_code}" "$URL/" 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
    check "HTTP 200 on index" "PASS"
else
    check "HTTP 200 on index" "Got HTTP $HTTP_CODE"
fi

# --- Check 2: COOP Header ---
echo "--- Check 2: COOP/COEP Headers ---"
HEADERS=$(curl -sI "$URL/" 2>/dev/null)

if echo "$HEADERS" | grep -qi "cross-origin-opener-policy.*same-origin"; then
    check "Cross-Origin-Opener-Policy: same-origin" "PASS"
else
    check "Cross-Origin-Opener-Policy: same-origin" "Header missing or wrong"
fi

if echo "$HEADERS" | grep -qi "cross-origin-embedder-policy.*require-corp"; then
    check "Cross-Origin-Embedder-Policy: require-corp" "PASS"
else
    check "Cross-Origin-Embedder-Policy: require-corp" "Header missing or wrong"
fi

# --- Check 3: Unity loader in HTML ---
echo "--- Check 3: Unity Loader ---"
PAGE_CONTENT=$(curl -s "$URL/" 2>/dev/null)

if echo "$PAGE_CONTENT" | grep -q "createUnityInstance"; then
    check "createUnityInstance in index.html" "PASS"
else
    check "createUnityInstance in index.html" "Not found in page source"
fi

# --- Check 4: Build files ---
echo "--- Check 4: Build Files ---"

# Find actual build filenames from index.html
WASM_FILE=$(echo "$PAGE_CONTENT" | grep -oP '[^"]+\.wasm\.(br|gz)' | head -1 || echo "")
JS_FILE=$(echo "$PAGE_CONTENT" | grep -oP '[^"]+\.framework\.js\.(br|gz)' | head -1 || echo "")
DATA_FILE=$(echo "$PAGE_CONTENT" | grep -oP '[^"]+\.data\.(br|gz)' | head -1 || echo "")

if [ -n "$WASM_FILE" ]; then
    WASM_HEADERS=$(curl -sI "$URL/$WASM_FILE" 2>/dev/null)
    WASM_CT=$(echo "$WASM_HEADERS" | grep -i "content-type" | head -1)
    if echo "$WASM_CT" | grep -qi "application/wasm"; then
        check "WASM Content-Type: application/wasm" "PASS"
    else
        check "WASM Content-Type: application/wasm" "Got: $WASM_CT"
    fi
    if echo "$WASM_HEADERS" | grep -qi "content-encoding.*br\|content-encoding.*gzip"; then
        check "WASM Content-Encoding present" "PASS"
    else
        check "WASM Content-Encoding present" "Missing Content-Encoding header"
    fi
else
    check "WASM file found" "Could not detect .wasm file in index.html"
fi

if [ -n "$JS_FILE" ]; then
    JS_HEADERS=$(curl -sI "$URL/$JS_FILE" 2>/dev/null)
    JS_CT=$(echo "$JS_HEADERS" | grep -i "content-type" | head -1)
    if echo "$JS_CT" | grep -qi "application/javascript\|text/javascript"; then
        check "JS Content-Type: application/javascript" "PASS"
    else
        check "JS Content-Type: application/javascript" "Got: $JS_CT"
    fi
else
    check "JS framework file found" "Could not detect .framework.js file in index.html"
fi

if [ -n "$DATA_FILE" ]; then
    DATA_HEADERS=$(curl -sI "$URL/$DATA_FILE" 2>/dev/null)
    DATA_CODE=$(echo "$DATA_HEADERS" | head -1 | grep -oP '\d{3}' | head -1)
    if [ "$DATA_CODE" = "200" ]; then
        check "Data file accessible (HTTP 200)" "PASS"
    else
        check "Data file accessible (HTTP 200)" "Got HTTP $DATA_CODE"
    fi
else
    check "Data file found" "Could not detect .data file in index.html"
fi

# --- Summary ---
echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="

if [ "$FAIL" -eq 0 ]; then
    echo "All checks passed! Open $URL in Chrome to verify Unity loads."
    echo ""
    echo "Manual checks:"
    echo "  1. Unity loading bar reaches 100%"
    echo "  2. DevTools → Network → filter 'ws' → WSS to photonengine.io"
    echo "  3. Open second tab → both can join same Photon room"
else
    echo "Some checks failed. Review errors above."
fi
