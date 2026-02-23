#!/usr/bin/env bash
set -euo pipefail

# Tarot Battlegrounds — First-time WebGL deployment to AWS
# Usage: bash AWS/scripts/deploy-webgl.sh [build-dir]

BUCKET_NAME="tarot-battlegrounds-webgl"
REGION="us-east-1"
BUILD_DIR="${1:-WebGLBuild}"
HEADERS_POLICY_NAME="TarotBG-Unity-COOP-COEP"
OAC_NAME="TarotBG-S3-OAC"

echo "=== Tarot Battlegrounds WebGL Deployment ==="

# --- Pre-flight checks ---
if ! aws sts get-caller-identity &>/dev/null; then
    echo "ERROR: AWS CLI not configured. Run 'aws configure' first."
    exit 1
fi

if [ ! -d "$BUILD_DIR" ]; then
    echo "ERROR: Build directory '$BUILD_DIR' not found."
    echo "Build in Unity first: File → Build Settings → WebGL → Build to 'WebGLBuild/'"
    exit 1
fi

if [ ! -f "$BUILD_DIR/index.html" ]; then
    echo "ERROR: '$BUILD_DIR/index.html' not found. Is this a valid Unity WebGL build?"
    exit 1
fi

ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
echo "AWS Account: $ACCOUNT_ID"
echo "Region: $REGION"
echo "Build dir: $BUILD_DIR"

# --- Step 1: Create S3 bucket ---
echo ""
echo "--- Step 1: S3 Bucket ---"
if aws s3api head-bucket --bucket "$BUCKET_NAME" 2>/dev/null; then
    echo "Bucket '$BUCKET_NAME' already exists."
else
    echo "Creating bucket '$BUCKET_NAME'..."
    aws s3api create-bucket --bucket "$BUCKET_NAME" --region "$REGION"
    echo "Blocking public access..."
    aws s3api put-public-access-block --bucket "$BUCKET_NAME" \
        --public-access-block-configuration \
        "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
    echo "Bucket created."
fi

# --- Step 2: Upload build with correct MIME types ---
echo ""
echo "--- Step 2: Upload Build ---"

# Upload HTML/CSS/JSON/PNG files normally
echo "Uploading static files..."
aws s3 sync "$BUILD_DIR" "s3://$BUCKET_NAME/" \
    --exclude "*.wasm.br" --exclude "*.js.br" --exclude "*.data.br" \
    --exclude "*.wasm.gz" --exclude "*.js.gz" --exclude "*.data.gz" \
    --cache-control "max-age=86400"

# Upload Brotli-compressed files with correct MIME types
echo "Uploading .wasm.br files..."
aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.wasm.br" \
    --content-type "application/wasm" \
    --content-encoding "br" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

echo "Uploading .js.br files..."
aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.js.br" \
    --content-type "application/javascript" \
    --content-encoding "br" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

echo "Uploading .data.br files..."
aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.data.br" \
    --content-type "application/octet-stream" \
    --content-encoding "br" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

# Handle gzip-compressed builds (fallback)
echo "Uploading .wasm.gz files (if any)..."
aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.wasm.gz" \
    --content-type "application/wasm" \
    --content-encoding "gzip" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.js.gz" \
    --content-type "application/javascript" \
    --content-encoding "gzip" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

aws s3 cp "$BUILD_DIR/Build/" "s3://$BUCKET_NAME/Build/" \
    --recursive \
    --exclude "*" --include "*.data.gz" \
    --content-type "application/octet-stream" \
    --content-encoding "gzip" \
    --cache-control "max-age=31536000" \
    --metadata-directive REPLACE 2>/dev/null || true

echo "Upload complete."

# --- Step 3: Create CloudFront Response Headers Policy (COOP/COEP) ---
echo ""
echo "--- Step 3: CloudFront Response Headers Policy ---"

EXISTING_POLICY_ID=$(aws cloudfront list-response-headers-policies \
    --query "ResponseHeadersPolicyList.Items[?ResponseHeadersPolicy.ResponseHeadersPolicyConfig.Name=='$HEADERS_POLICY_NAME'].ResponseHeadersPolicy.Id" \
    --output text 2>/dev/null || echo "")

if [ -n "$EXISTING_POLICY_ID" ] && [ "$EXISTING_POLICY_ID" != "None" ]; then
    echo "Response headers policy '$HEADERS_POLICY_NAME' already exists: $EXISTING_POLICY_ID"
    POLICY_ID="$EXISTING_POLICY_ID"
else
    echo "Creating response headers policy..."
    POLICY_ID=$(aws cloudfront create-response-headers-policy \
        --response-headers-policy-config '{
            "Name": "'"$HEADERS_POLICY_NAME"'",
            "Comment": "COOP/COEP headers for Unity WebGL SharedArrayBuffer",
            "CustomHeadersConfig": {
                "Quantity": 2,
                "Items": [
                    {
                        "Header": "Cross-Origin-Opener-Policy",
                        "Value": "same-origin",
                        "Override": true
                    },
                    {
                        "Header": "Cross-Origin-Embedder-Policy",
                        "Value": "credentialless",
                        "Override": true
                    }
                ]
            }
        }' \
        --query 'ResponseHeadersPolicy.Id' --output text)
    echo "Created policy: $POLICY_ID"
fi

# --- Step 4: Create CloudFront OAC ---
echo ""
echo "--- Step 4: CloudFront OAC ---"

EXISTING_OAC_ID=$(aws cloudfront list-origin-access-controls \
    --query "OriginAccessControlList.Items[?Name=='$OAC_NAME'].Id" \
    --output text 2>/dev/null || echo "")

if [ -n "$EXISTING_OAC_ID" ] && [ "$EXISTING_OAC_ID" != "None" ]; then
    echo "OAC '$OAC_NAME' already exists: $EXISTING_OAC_ID"
    OAC_ID="$EXISTING_OAC_ID"
else
    echo "Creating OAC..."
    OAC_ID=$(aws cloudfront create-origin-access-control \
        --origin-access-control-config '{
            "Name": "'"$OAC_NAME"'",
            "Description": "OAC for Tarot Battlegrounds S3 bucket",
            "SigningProtocol": "sigv4",
            "SigningBehavior": "always",
            "OriginAccessControlOriginType": "s3"
        }' \
        --query 'OriginAccessControl.Id' --output text)
    echo "Created OAC: $OAC_ID"
fi

# --- Step 5: Create CloudFront Distribution ---
echo ""
echo "--- Step 5: CloudFront Distribution ---"

# Check if distribution already exists for this bucket
EXISTING_DIST_ID=$(aws cloudfront list-distributions \
    --query "DistributionList.Items[?Origins.Items[0].DomainName=='${BUCKET_NAME}.s3.${REGION}.amazonaws.com'].Id" \
    --output text 2>/dev/null || echo "")

if [ -n "$EXISTING_DIST_ID" ] && [ "$EXISTING_DIST_ID" != "None" ]; then
    echo "Distribution already exists: $EXISTING_DIST_ID"
    DIST_DOMAIN=$(aws cloudfront get-distribution --id "$EXISTING_DIST_ID" \
        --query 'Distribution.DomainName' --output text)
else
    echo "Creating CloudFront distribution..."
    DIST_RESULT=$(aws cloudfront create-distribution \
        --distribution-config '{
            "CallerReference": "tarot-bg-'"$(date +%s)"'",
            "Comment": "Tarot Battlegrounds WebGL",
            "Enabled": true,
            "DefaultRootObject": "index.html",
            "Origins": {
                "Quantity": 1,
                "Items": [{
                    "Id": "S3-'"$BUCKET_NAME"'",
                    "DomainName": "'"$BUCKET_NAME"'.s3.'"$REGION"'.amazonaws.com",
                    "OriginAccessControlId": "'"$OAC_ID"'",
                    "S3OriginConfig": {
                        "OriginAccessIdentity": ""
                    }
                }]
            },
            "DefaultCacheBehavior": {
                "TargetOriginId": "S3-'"$BUCKET_NAME"'",
                "ViewerProtocolPolicy": "redirect-to-https",
                "AllowedMethods": {
                    "Quantity": 2,
                    "Items": ["GET", "HEAD"],
                    "CachedMethods": {
                        "Quantity": 2,
                        "Items": ["GET", "HEAD"]
                    }
                },
                "CachePolicyId": "658327ea-f89d-4fab-a63d-7e88639e58f6",
                "ResponseHeadersPolicyId": "'"$POLICY_ID"'",
                "Compress": true
            },
            "PriceClass": "PriceClass_100",
            "ViewerCertificate": {
                "CloudFrontDefaultCertificate": true
            }
        }')

    EXISTING_DIST_ID=$(echo "$DIST_RESULT" | python3 -c "import sys,json; print(json.load(sys.stdin)['Distribution']['Id'])")
    DIST_DOMAIN=$(echo "$DIST_RESULT" | python3 -c "import sys,json; print(json.load(sys.stdin)['Distribution']['DomainName'])")
    echo "Created distribution: $EXISTING_DIST_ID"
fi

# --- Step 6: Update S3 Bucket Policy for OAC ---
echo ""
echo "--- Step 6: S3 Bucket Policy ---"

aws s3api put-bucket-policy --bucket "$BUCKET_NAME" --policy '{
    "Version": "2012-10-17",
    "Statement": [{
        "Sid": "AllowCloudFrontOAC",
        "Effect": "Allow",
        "Principal": {
            "Service": "cloudfront.amazonaws.com"
        },
        "Action": "s3:GetObject",
        "Resource": "arn:aws:s3:::'"$BUCKET_NAME"'/*",
        "Condition": {
            "StringEquals": {
                "AWS:SourceArn": "arn:aws:cloudfront::'"$ACCOUNT_ID"':distribution/'"$EXISTING_DIST_ID"'"
            }
        }
    }]
}'
echo "Bucket policy updated."

# --- Done ---
echo ""
echo "=== Deployment Complete ==="
echo "Distribution ID: $EXISTING_DIST_ID"
echo "CloudFront URL:  https://$DIST_DOMAIN"
echo ""
echo "Note: CloudFront may take 5-15 minutes to fully deploy."
echo "Run 'bash AWS/scripts/validate-deployment.sh $DIST_DOMAIN' to validate."
