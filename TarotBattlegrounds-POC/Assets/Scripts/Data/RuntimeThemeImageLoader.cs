using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Loads theme sprite assets from URLs at runtime and applies them to ThemeConfig progressively.
/// Modeled after RuntimeCardImageLoader, with concurrent download management.
/// </summary>
public class RuntimeThemeImageLoader : MonoBehaviour
{
    public static RuntimeThemeImageLoader Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Placeholder sprite used while theme assets are loading")]
    public Sprite placeholderSprite;

    [Tooltip("Maximum concurrent image downloads")]
    public int maxConcurrentDownloads = 4;

    private Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
    private int _activeDownloads = 0;
    private Queue<DownloadRequest> _downloadQueue = new Queue<DownloadRequest>();
    private ThemeConfig _targetConfig;

    private struct DownloadRequest
    {
        public string url;
        public string slotName;
        public int tribeIndex; // -1 for non-tribe assets
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Load all theme assets from the runtime theme data and apply them to the given ThemeConfig.
    /// </summary>
    public void LoadThemeAssets(RuntimeThemeData themeData, ThemeConfig targetConfig)
    {
        if (themeData == null || targetConfig == null) return;

        _targetConfig = targetConfig;

        // Queue asset downloads
        if (themeData.assets != null)
        {
            QueueAsset(themeData.assets.gameBackground, "gameBackground");
            QueueAsset(themeData.assets.cardFrameCommon, "cardFrameCommon");
            QueueAsset(themeData.assets.cardFrameRare, "cardFrameRare");
            QueueAsset(themeData.assets.cardFrameEpic, "cardFrameEpic");
            QueueAsset(themeData.assets.cardBack, "cardBack");
            QueueAsset(themeData.assets.panelBackground, "panelBackground");
            QueueAsset(themeData.assets.buttonNormal, "buttonNormal");
            QueueAsset(themeData.assets.buttonHighlighted, "buttonHighlighted");
            QueueAsset(themeData.assets.buttonPressed, "buttonPressed");
            QueueAsset(themeData.assets.buttonDisabled, "buttonDisabled");
            QueueAsset(themeData.assets.coinIcon, "coinIcon");
            QueueAsset(themeData.assets.healthIcon, "healthIcon");
            QueueAsset(themeData.assets.attackIcon, "attackIcon");
            QueueAsset(themeData.assets.shieldIcon, "shieldIcon");
        }

        // Queue tribe icon downloads
        if (themeData.tribes != null)
        {
            string[] tribeOrder = { "Pentacles", "Cups", "Swords", "Wands" };
            for (int i = 0; i < tribeOrder.Length; i++)
            {
                if (themeData.tribes.TryGetValue(tribeOrder[i], out RuntimeTribeThemeData tribeData))
                {
                    if (!string.IsNullOrEmpty(tribeData.iconUrl))
                    {
                        _downloadQueue.Enqueue(new DownloadRequest
                        {
                            url = tribeData.iconUrl,
                            slotName = "tribeIcon",
                            tribeIndex = i
                        });
                    }
                }
            }
        }

        int queued = _downloadQueue.Count;
        if (queued > 0)
        {
            Debug.Log($"[RuntimeThemeImageLoader] Queued {queued} theme asset downloads");
            TryStartNextDownload();
        }
    }

    private void QueueAsset(string url, string slotName)
    {
        if (string.IsNullOrEmpty(url)) return;

        _downloadQueue.Enqueue(new DownloadRequest
        {
            url = url,
            slotName = slotName,
            tribeIndex = -1
        });
    }

    private void TryStartNextDownload()
    {
        while (_activeDownloads < maxConcurrentDownloads && _downloadQueue.Count > 0)
        {
            var request = _downloadQueue.Dequeue();
            if (_cache.ContainsKey(request.url))
            {
                ApplySprite(request, _cache[request.url]);
                continue;
            }
            _activeDownloads++;
            StartCoroutine(DownloadImage(request));
        }
    }

    private IEnumerator DownloadImage(DownloadRequest request)
    {
        using (var webRequest = UnityWebRequestTexture.GetTexture(request.url))
        {
            webRequest.timeout = 15;
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(webRequest);
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                _cache[request.url] = sprite;
                ApplySprite(request, sprite);
            }
            else
            {
                Debug.LogWarning($"[RuntimeThemeImageLoader] Failed to load {request.slotName} from {request.url}: {webRequest.error}");
            }
        }

        _activeDownloads--;
        TryStartNextDownload();
    }

    private void ApplySprite(DownloadRequest request, Sprite sprite)
    {
        if (_targetConfig == null) return;

        if (request.tribeIndex >= 0)
        {
            _targetConfig.SetTribeIcon(request.tribeIndex, sprite);
            Debug.Log($"[RuntimeThemeImageLoader] Applied tribe icon for index {request.tribeIndex}");
        }
        else
        {
            if (_targetConfig.SetSpriteBySlotName(request.slotName, sprite))
                Debug.Log($"[RuntimeThemeImageLoader] Applied sprite: {request.slotName}");
            else
                Debug.LogWarning($"[RuntimeThemeImageLoader] Unknown sprite slot: {request.slotName}");
        }

        // Fire theme changed event so UI can refresh
        ThemeManager.NotifyThemeChanged();
    }

    /// <summary>
    /// Clear the sprite cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
