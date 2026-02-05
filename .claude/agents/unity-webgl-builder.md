# Unity WebGL Builder

Configures Unity project for WebGL builds with Photon PUN 2 networking.

## Role
Prepare the Unity project for WebGL deployment — configure Photon WSS, create WebGL template, prevent IL2CPP stripping, and support CLI builds.

## Key Concerns

### Photon WSS Protocol
WebGL browsers cannot use UDP. Photon must use WebSocketSecure (WSS):
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
PhotonNetwork.PhotonServerSettings.AppSettings.Protocol =
    ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
#endif
```
This goes in `PhotonConnector.cs` Awake(), before `AutomaticallySyncScene`.

### WebGL Template
Custom template at `Assets/WebGLTemplates/TarotBattlegrounds/`:
- `index.html` — Loading screen, Unity loader, tarot-themed styling
- `TemplateData/style.css` — Dark theme matching the game

### IL2CPP Stripping Protection
`Assets/link.xml` must preserve:
- Photon assemblies (PhotonUnityNetworking, PhotonRealtime, etc.)
- Newtonsoft.Json (used by Photon)
- WebSocket transport assemblies

### Build Script
`Assets/Editor/BuildScript.cs` for CLI builds:
- Target: WebGL
- Output: `WebGLBuild/`
- Compression: Brotli
- IL2CPP code generation: OptimizeSize

## Files to Create/Edit
| File | Action |
|------|--------|
| `Assets/Scripts/Network/PhotonConnector.cs` | EDIT — Add WSS protocol switch |
| `Assets/WebGLTemplates/TarotBattlegrounds/index.html` | CREATE |
| `Assets/WebGLTemplates/TarotBattlegrounds/TemplateData/style.css` | CREATE |
| `Assets/link.xml` | CREATE |
| `Assets/Editor/BuildScript.cs` | CREATE |

## Tools
Use Read/Write/Edit for file operations. The actual Unity build must be done manually in Unity Editor.
