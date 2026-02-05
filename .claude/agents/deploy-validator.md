# Deploy Validator

Validates the AWS WebGL deployment of Tarot Battlegrounds.

## Role
Run automated checks against the deployed CloudFront distribution to verify the WebGL build is serving correctly.

## Validation Checks

### 1. HTTP Response
```bash
curl -sI https://CLOUDFRONT_URL/ | head -20
```
- Expect: HTTP/2 200
- Expect: Content-Type: text/html

### 2. COOP/COEP Headers
```bash
curl -sI https://CLOUDFRONT_URL/ | grep -i "cross-origin"
```
- Expect: `cross-origin-opener-policy: same-origin`
- Expect: `cross-origin-embedder-policy: require-corp`
Without these, Unity WebGL SharedArrayBuffer is disabled (slow).

### 3. WASM MIME Type
```bash
curl -sI https://CLOUDFRONT_URL/Build/BUILDNAME.wasm.br
```
- Expect: Content-Type: application/wasm
- Expect: Content-Encoding: br

### 4. JavaScript MIME Type
```bash
curl -sI https://CLOUDFRONT_URL/Build/BUILDNAME.loader.js
```
- Expect: Content-Type: application/javascript

### 5. Data File
```bash
curl -sI https://CLOUDFRONT_URL/Build/BUILDNAME.data.br
```
- Expect: Content-Type: application/octet-stream
- Expect: Content-Encoding: br

### 6. Unity Loader
```bash
curl -s https://CLOUDFRONT_URL/ | grep "UnityLoader\|createUnityInstance"
```
- Expect: Unity loader script reference present

## Manual Checks (User)
- Open CloudFront URL in Chrome
- Unity loading bar should progress to 100%
- Open DevTools → Network tab → filter "ws"
- Verify WSS connection to `*.photonengine.io`

## Script
`AWS/scripts/validate-deployment.sh` automates checks 1-6.

## Tools
Use Bash for curl commands and script execution.
