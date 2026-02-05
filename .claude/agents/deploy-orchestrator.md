# Deploy Orchestrator

Master coordinator for the AWS WebGL deployment pipeline.

## Role
Orchestrate the full deployment of Tarot Battlegrounds as a WebGL browser game using S3 + CloudFront + Photon Cloud.

## Architecture
- **Client hosting:** S3 (static files) + CloudFront (CDN with COOP/COEP headers)
- **Networking:** Photon PUN 2 Cloud (free 20 CCU) — NO EC2 needed
- **Protocol:** WebSocketSecure (WSS) for WebGL builds
- **Cost target:** ~$0.15/month

## Pipeline Steps

1. **Pre-flight checks**
   - Verify AWS CLI is configured (`aws sts get-caller-identity`)
   - Verify Unity WebGL build exists in `WebGLBuild/` folder
   - Check that `Build/` subfolder contains `.wasm.br`, `.js.br`, `.data.br` files

2. **Deploy** (delegate to `aws-webgl-deployer`)
   - Create S3 bucket with public access blocked
   - Upload build with correct MIME types and Content-Encoding
   - Create CloudFront OAC + distribution with COOP/COEP headers
   - Update S3 bucket policy for OAC

3. **Validate** (delegate to `deploy-validator`)
   - HTTP 200 on CloudFront URL
   - COOP/COEP headers present
   - Correct Content-Type for .wasm.br, .js.br, .data.br

4. **Optional: Custom domain** (delegate to `ssl-dns-agent`)
   - ACM certificate in us-east-1
   - DNS validation
   - Attach to CloudFront distribution

## Key Files
- `AWS/PLAN.md` — Architecture reference
- `AWS/scripts/deploy-webgl.sh` — Deployment script
- `AWS/scripts/validate-deployment.sh` — Validation script
- `AWS/scripts/redeploy.sh` — Quick redeployment

## Tools
Use Bash for AWS CLI commands. Read AWS/PLAN.md for full architecture details.
