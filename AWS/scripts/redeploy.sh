#!/usr/bin/env bash
set -euo pipefail

# Tarot Battlegrounds — Quick Redeployment
# Usage: bash AWS/scripts/redeploy.sh [build-dir]
# If no build-dir given, auto-detects the latest WebGLBuildNN folder.
# Syncs updated WebGL build to S3 and invalidates CloudFront cache.

BUCKET_NAME="tarot-battlegrounds-webgl"
REGION="us-east-1"
WEBGL_ROOT="TarotBattlegrounds-POC/WebGLBuild"

if [ -n "${1:-}" ]; then
    BUILD_DIR="$1"
else
    # Auto-detect: pick the highest-numbered WebGLBuildNN directory
    LATEST=$(ls -1d "$WEBGL_ROOT"/WebGLBuild[0-9]* 2>/dev/null | sort -t 'd' -k2 -n | tail -1)
    if [ -z "$LATEST" ]; then
        echo "ERROR: No WebGLBuild* directories found in $WEBGL_ROOT/"
        exit 1
    fi
    BUILD_DIR="$LATEST"
fi

echo "=== Tarot Battlegrounds — Redeployment ==="

# Pre-flight
if [ ! -d "$BUILD_DIR" ]; then
    echo "ERROR: Build directory '$BUILD_DIR' not found."
    exit 1
fi

# Find CloudFront distribution ID
DIST_ID=$(aws cloudfront list-distributions \
    --query "DistributionList.Items[?Origins.Items[0].DomainName=='${BUCKET_NAME}.s3.${REGION}.amazonaws.com'].Id" \
    --output text 2>/dev/null || echo "")

if [ -z "$DIST_ID" ] || [ "$DIST_ID" = "None" ]; then
    echo "ERROR: No CloudFront distribution found for bucket '$BUCKET_NAME'."
    echo "Run 'bash AWS/scripts/deploy-webgl.sh' for first-time deployment."
    exit 1
fi

DIST_DOMAIN=$(aws cloudfront get-distribution --id "$DIST_ID" \
    --query 'Distribution.DomainName' --output text)

echo "Distribution: $DIST_ID ($DIST_DOMAIN)"
echo "Build dir: $BUILD_DIR"
echo ""

# Upload static files
echo "--- Syncing static files ---"
aws s3 sync "$BUILD_DIR" "s3://$BUCKET_NAME/" \
    --exclude "*.wasm.br" --exclude "*.js.br" --exclude "*.data.br" \
    --exclude "*.wasm.gz" --exclude "*.js.gz" --exclude "*.data.gz" \
    --delete \
    --cache-control "max-age=86400"

# Upload compressed build files with correct MIME types
echo "--- Uploading compressed build files ---"

for ext in wasm js data; do
    case $ext in
        wasm) CT="application/wasm" ;;
        js)   CT="application/javascript" ;;
        data) CT="application/octet-stream" ;;
    esac

    # Brotli
    aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
        --recursive \
        --exclude "*" --include "*.${ext}.br" \
        --content-type "$CT" \
        --content-encoding "br" \
        --cache-control "max-age=31536000" \
        --metadata-directive REPLACE 2>/dev/null || true

    # Gzip fallback
    aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
        --recursive \
        --exclude "*" --include "*.${ext}.gz" \
        --content-type "$CT" \
        --content-encoding "gzip" \
        --cache-control "max-age=31536000" \
        --metadata-directive REPLACE 2>/dev/null || true
done

echo "Upload complete."

# Invalidate CloudFront cache
echo ""
echo "--- Invalidating CloudFront cache ---"
INVALIDATION_ID=$(aws cloudfront create-invalidation \
    --distribution-id "$DIST_ID" \
    --paths "/*" \
    --query 'Invalidation.Id' --output text)
echo "Invalidation created: $INVALIDATION_ID"

echo ""
echo "=== Redeployment Complete ==="
echo "URL: https://$DIST_DOMAIN"
echo "Cache invalidation in progress (usually 1-2 minutes)."
echo ""
echo "Validate: bash AWS/scripts/validate-deployment.sh $DIST_DOMAIN"
