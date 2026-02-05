# Deploy to AWS

Deploy Tarot Battlegrounds WebGL build to S3 + CloudFront.

## Pipeline

Run the full deployment pipeline for the Tarot Battlegrounds WebGL client:

1. **Pre-flight:** Verify AWS CLI is configured and WebGL build exists in `WebGLBuild/`
2. **Deploy:** Run `AWS/scripts/deploy-webgl.sh` to create/update S3 bucket, upload with correct MIME types, and configure CloudFront with COOP/COEP headers
3. **Validate:** Run `AWS/scripts/validate-deployment.sh` to verify HTTP 200, headers, and MIME types
4. **Report:** Output the CloudFront URL and any issues found

## Quick Commands

**First deployment:**
```bash
bash AWS/scripts/deploy-webgl.sh
bash AWS/scripts/validate-deployment.sh
```

**Redeployment (after new build):**
```bash
bash AWS/scripts/redeploy.sh
bash AWS/scripts/validate-deployment.sh
```

## Prerequisites
- AWS CLI configured (`aws configure`)
- Unity WebGL build in `WebGLBuild/` folder (build manually in Unity Editor)
- Build should use Brotli compression and the TarotBattlegrounds WebGL template

## Architecture
- S3 + CloudFront for static WebGL hosting
- Photon Cloud (PUN 2) handles all multiplayer networking — no EC2 needed
- Cost: ~$0.15/month
