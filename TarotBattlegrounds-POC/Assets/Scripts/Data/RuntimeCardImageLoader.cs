using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Loads card images from URLs at runtime and caches them as Sprites.
/// Use GetCardSprite() which returns a placeholder while loading, then invokes a callback when ready.
/// </summary>
public class RuntimeCardImageLoader : MonoBehaviour
{
    public static RuntimeCardImageLoader Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Placeholder sprite shown while card images are loading")]
    public Sprite placeholderSprite;

    [Tooltip("Maximum concurrent image downloads")]
    public int maxConcurrentDownloads = 4;

    private Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
    private Dictionary<string, List<Action<Sprite>>> _pendingCallbacks = new Dictionary<string, List<Action<Sprite>>>();
    private int _activeDownloads = 0;
    private Queue<string> _downloadQueue = new Queue<string>();

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
    /// Get a card sprite by URL. Returns cached sprite immediately if available,
    /// otherwise returns placeholder and invokes callback when loaded.
    /// </summary>
    public Sprite GetCardSprite(string imageUrl, Action<Sprite> onLoaded = null)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return placeholderSprite;

        // Already cached
        if (_cache.TryGetValue(imageUrl, out Sprite cached))
        {
            onLoaded?.Invoke(cached);
            return cached;
        }

        // Already downloading - add callback
        if (_pendingCallbacks.ContainsKey(imageUrl))
        {
            if (onLoaded != null)
                _pendingCallbacks[imageUrl].Add(onLoaded);
            return placeholderSprite;
        }

        // Start new download
        _pendingCallbacks[imageUrl] = new List<Action<Sprite>>();
        if (onLoaded != null)
            _pendingCallbacks[imageUrl].Add(onLoaded);

        _downloadQueue.Enqueue(imageUrl);
        TryStartNextDownload();

        return placeholderSprite;
    }

    /// <summary>
    /// Preload a batch of image URLs.
    /// </summary>
    public void Preload(IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            if (!string.IsNullOrEmpty(url) && !_cache.ContainsKey(url) && !_pendingCallbacks.ContainsKey(url))
            {
                _pendingCallbacks[url] = new List<Action<Sprite>>();
                _downloadQueue.Enqueue(url);
            }
        }
        TryStartNextDownload();
    }

    /// <summary>
    /// Check if a sprite is cached for a URL.
    /// </summary>
    public bool IsCached(string imageUrl)
    {
        return !string.IsNullOrEmpty(imageUrl) && _cache.ContainsKey(imageUrl);
    }

    /// <summary>
    /// Clear the sprite cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    private void TryStartNextDownload()
    {
        while (_activeDownloads < maxConcurrentDownloads && _downloadQueue.Count > 0)
        {
            string url = _downloadQueue.Dequeue();
            // Skip if already cached (may have been downloaded while queued)
            if (_cache.ContainsKey(url))
            {
                InvokeCallbacks(url, _cache[url]);
                continue;
            }
            _activeDownloads++;
            StartCoroutine(DownloadImage(url));
        }
    }

    private IEnumerator DownloadImage(string url)
    {
        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                _cache[url] = sprite;
                InvokeCallbacks(url, sprite);
            }
            else
            {
                Debug.LogWarning($"[RuntimeCardImageLoader] Failed to load image {url}: {request.error}");
                InvokeCallbacks(url, placeholderSprite);
            }
        }

        _activeDownloads--;
        TryStartNextDownload();
    }

    private void InvokeCallbacks(string url, Sprite sprite)
    {
        if (_pendingCallbacks.TryGetValue(url, out var callbacks))
        {
            foreach (var cb in callbacks)
            {
                try { cb?.Invoke(sprite); }
                catch (Exception e) { Debug.LogError($"[RuntimeCardImageLoader] Callback error: {e.Message}"); }
            }
            _pendingCallbacks.Remove(url);
        }
    }
}
