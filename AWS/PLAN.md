# Tarot Battlegrounds — AWS WebGL Deployment Plan

## Overview

**Goal:** Deploy Tarot Battlegrounds as a browser-playable WebGL game on AWS for ~$0.15/month.

**Key insight:** The project uses **Photon PUN 2** for all networking. Photon Cloud handles multiplayer (free 20 CCU tier) — no EC2 game server needed.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                      PLAYERS                         │
│              (Browser — any device)                  │
└────────────┬──────────────────────┬─────────────────┘
             │ HTTPS (static)       │ WSS (game traffic)
             ▼                      ▼
┌────────────────────┐   ┌─────────────────────────┐
│   S3 + CloudFront  │   │   Photon Cloud (PUN 2)  │
│   WebGL Client     │   │   Free 20 CCU tier      │
│   HTML/JS/WASM     │   │   WSS auto-handled      │
│   ~$0.15/month     │   │   $0/month (POC)        │
└────────────────────┘   └─────────────────────────┘
```

**Why this architecture:**
- **S3 + CloudFront** for static WebGL files — dirt cheap, globally fast, zero maintenance
- **Photon Cloud** handles all multiplayer — matchmaking, rooms, RPCs, state sync
- **No EC2** — Photon is a managed service, eliminates server management entirely
- **WSS (WebSocketSecure)** — Photon auto-uses WSS in WebGL builds with our protocol config

---

## Cost Breakdown

| Service | Spec | Monthly Cost |
|---------|------|-------------|
| S3 | ~50MB WebGL build, minimal requests | ~$0.05 |
| CloudFront | Low traffic POC | ~$0.10 |
| Photon Cloud | Free tier (20 CCU) | $0 |
| **TOTAL** | | **~$0.15/month** |

---

## Implementation Steps

### Step 1: Configure Unity for WebGL

**PhotonConnector.cs** — Add WSS protocol for WebGL:
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
PhotonNetwork.PhotonServerSettings.AppSettings.Protocol =
    ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
#endif
```

**WebGL Template** — Custom template at `Assets/WebGLTemplates/TarotBattlegrounds/`

**link.xml** — Prevent IL2CPP from stripping Photon/JSON assemblies

**BuildScript.cs** — CLI build support for automation

### Step 2: Build WebGL

**Option A — From Unity Editor:**
1. Open Unity Editor → `Build > WebGL` menu item
2. Build auto-increments to `WebGLBuild/WebGLBuildNN`, keeps last 10 builds

**Option B — From CLI (Unity must be closed):**
```bash
/Applications/Unity/Hub/Editor/2022.3.48f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath TarotBattlegrounds-POC \
  -executeMethod BuildScript.BuildWebGLCLI
```

### Step 3: Deploy to AWS

Run `AWS/scripts/deploy-webgl.sh`:

1. **Create S3 bucket** `tarot-battlegrounds-webgl` (us-east-1)
   - Block all public access (OAC handles it)

2. **Upload build** with correct MIME types:
   | File Pattern | Content-Type | Content-Encoding |
   |-------------|-------------|-----------------|
   | `*.wasm.br` | application/wasm | br |
   | `*.js.br` | application/javascript | br |
   | `*.data.br` | application/octet-stream | br |

3. **Create CloudFront response headers policy**:
   ```
   Cross-Origin-Opener-Policy: same-origin
   Cross-Origin-Embedder-Policy: require-corp
   ```
   Required for Unity SharedArrayBuffer (multi-threaded WASM).

4. **Create CloudFront OAC** (Origin Access Control)

5. **Create CloudFront distribution**:
   - Origin: S3 via OAC
   - Default root object: index.html
   - Response headers: COOP/COEP policy
   - Cache: CachingOptimized

6. **Update S3 bucket policy** to allow CloudFront OAC access

### Step 4: Validate

Run `AWS/scripts/validate-deployment.sh`:
- HTTP 200 on CloudFront URL
- COOP/COEP headers present
- Correct MIME types for .wasm.br, .js.br, .data.br
- Unity loader script present in index.html

### Step 5: Manual Verification

1. Open CloudFront URL in Chrome
2. Unity loading bar should reach 100%
3. DevTools → Network → filter "ws" → verify WSS to photonengine.io
4. Open second tab → both join same Photon room
5. Play a game to completion

### Step 6 (Optional): Custom Domain + SSL

Run by `ssl-dns-agent`:
- ACM certificate in us-east-1 (free)
- DNS validation via CNAME
- Attach custom domain to CloudFront distribution

---

## Redeployment

After building a new WebGL version:
```bash
bash AWS/scripts/redeploy.sh
```
The script auto-detects the highest-numbered `WebGLBuild/WebGLBuildNN` directory, uploads to S3 with correct MIME types, and invalidates the CloudFront cache. You can also pass a specific path: `bash AWS/scripts/redeploy.sh path/to/build`

**Live URL:** `https://dui22oafwco41.cloudfront.net`

---

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/Network/PhotonConnector.cs` | WSS protocol switch for WebGL |
| `Assets/WebGLTemplates/TarotBattlegrounds/index.html` | Custom WebGL template |
| `Assets/link.xml` | IL2CPP stripping protection |
| `Assets/Editor/BuildScript.cs` | WebGL build with auto-increment + cleanup |
| `Assets/Editor/MainMenuSceneSetup.cs` | Programmatic MainMenu scene setup |
| `Assets/Editor/PlayerPrefabSetup.cs` | Programmatic Player prefab creation |
| `AWS/scripts/deploy-webgl.sh` | First-time deployment |
| `AWS/scripts/validate-deployment.sh` | Post-deploy validation |
| `AWS/scripts/redeploy.sh` | Subsequent deployments (auto-detects latest build) |

---

## Gotchas

1. **WebGL = WebSockets only** — Browsers can't do UDP. Photon must use WSS protocol.
2. **COOP/COEP headers required** — Without them, Unity falls back to single-threaded (slow).
3. **Brotli MIME types** — S3 doesn't auto-detect `.wasm.br`. Must set Content-Type and Content-Encoding explicitly.
4. **CloudFront OAC** — Use OAC (not OAI) for S3 access. OAI is legacy.
5. **link.xml** — IL2CPP will strip Photon assemblies without it, causing runtime errors.
6. **Cache invalidation** — Always invalidate CloudFront after redeployment (`/*`).

---

## Future Scaling

| Need | Solution |
|------|----------|
| >20 CCU | Photon Plus plan ($95/mo for 500 CCU) |
| Player accounts | AWS Cognito (free tier: 50k MAU) |
| Leaderboards | DynamoDB (free tier: 25GB) |
| Analytics | CloudWatch or Mixpanel free tier |
| Custom domain | ACM + Route 53 (see Step 6) |

---

*Plan updated: February 2026 | Architecture: Photon PUN 2 + S3/CloudFront (no EC2) | Build: auto-increment WebGLBuildNN*
