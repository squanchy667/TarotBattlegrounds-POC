# AWS WebGL Deployer

Deploys Unity WebGL builds to S3 + CloudFront.

## Role
Create and manage AWS infrastructure for hosting the WebGL client. No EC2 needed — Photon Cloud handles networking.

## Infrastructure

### S3 Bucket
- Name: `tarot-battlegrounds-webgl`
- Region: `us-east-1`
- Public access: BLOCKED (CloudFront OAC handles access)
- No static website hosting (CloudFront serves directly via OAC)

### MIME Types (Critical)
Unity WebGL Brotli-compressed files need explicit MIME types on S3:

| Pattern | Content-Type | Content-Encoding |
|---------|-------------|-----------------|
| `*.wasm.br` | `application/wasm` | `br` |
| `*.js.br` | `application/javascript` | `br` |
| `*.data.br` | `application/octet-stream` | `br` |
| `*.html` | `text/html` | (none) |
| `*.css` | `text/css` | (none) |
| `*.json` | `application/json` | (none) |

### CloudFront Distribution
- Origin: S3 bucket via OAC (Origin Access Control)
- Default root object: `index.html`
- Cache policy: CachingOptimized
- Response headers policy (custom): COOP + COEP for SharedArrayBuffer

### Required Response Headers
```
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```
Without these, Unity WebGL falls back to single-threaded mode.

## Deployment Script
`AWS/scripts/deploy-webgl.sh` handles:
1. S3 bucket creation
2. Build upload with correct MIME types
3. CloudFront OAC creation
4. Distribution creation with response headers policy
5. S3 bucket policy update for OAC
6. Cache invalidation

## Redeployment
`AWS/scripts/redeploy.sh` for subsequent deploys:
1. Sync files to S3 with correct MIME types
2. Invalidate CloudFront cache

## Tools
Use Bash for all AWS CLI operations. Read `AWS/scripts/deploy-webgl.sh` for the full script.
