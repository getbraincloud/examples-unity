using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ImageCacheService : MonoBehaviour
{
    [SerializeField]
    private List<CurrencySprite> currencySprites;

    [SerializeField]
    private List<ItemSectionSprite> sectionSprites;

    [SerializeField]
    private List<LocalImageOverride> localImageOverrides;

    [SerializeField]
    public Sprite noAdsSprite, timerSprite;
    public static ImageCacheService Instance { get; private set; }

    private Dictionary<string, Sprite> memoryCache = new Dictionary<string, Sprite>();
    private Dictionary<string, Task<Sprite>> ongoingDownloads = new Dictionary<string, Task<Sprite>>();

    private string CacheFolderPath => Path.Combine(Application.persistentDataPath, "ImageCache");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Directory.Exists(CacheFolderPath))
            Directory.CreateDirectory(CacheFolderPath);
    }
    
    public Sprite GetSpriteForCurrency(CurrencyType currencyType)
    {
        foreach(CurrencySprite cSprite in currencySprites)
        {
            if(cSprite.type == currencyType)
            {
                return cSprite.sprite;
            }
        }

        return null;
    }
    private Sprite GetLocalOverrideSprite(string key)
    {
        if (localImageOverrides == null || string.IsNullOrEmpty(key))
            return null;

        string normalizedKey = Path.GetFileNameWithoutExtension(key).ToLowerInvariant();

        foreach (LocalImageOverride entry in localImageOverrides)
        {
            if (string.IsNullOrEmpty(entry.key))
                continue;

            string normalizedEntryKey = Path.GetFileNameWithoutExtension(entry.key).ToLowerInvariant();
            if (normalizedEntryKey == normalizedKey)
                return entry.sprite;
        }

        return null;
    }

    public Sprite GetSpriteForSection(string sectionName)
    {
        foreach (ItemSectionSprite sSprite in sectionSprites)
        {
            if (sSprite.sectionName == sectionName)
            {
                return sSprite.sprite;
            }
        }

        return null;
    }

    public async Task<Sprite> GetImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // Some catalog items (e.g. freebies, default starter items) carry a bare
        // filename in their "image" field rather than a hosted URL, since their
        // art already ships with the app. Attempting to fetch a bare filename as
        // a network request fails everywhere, and on WebGL the browser resolves
        // it as if it were a hostname (e.g. "https://foo.png/"), producing a
        // CORS/NetworkError. Resolve those against the local override list
        // instead of hitting the network.
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            return GetLocalOverrideSprite(url);
        }

        // First check memory cache
        if (memoryCache.TryGetValue(url, out Sprite cachedSprite))
            return cachedSprite;

        // Prevent duplicate downloads
        if (ongoingDownloads.TryGetValue(url, out Task<Sprite> existingTask))
            return await existingTask;

        // Either get image from disk if exists or download it if not
        var downloadTask = LoadOrDownloadAsync(url);
        ongoingDownloads[url] = downloadTask;

        try
        {
            return await downloadTask;
        }
        catch (Exception e)
        {
            // A failure here (e.g. disk persistence) must never leave the
            // caller's loading state stuck — return null instead of faulting.
            Debug.LogError($"[ImageCache] Failed to load image {url}: {e}");
            return null;
        }
        finally
        {
            // Always clear the entry so a failed load isn't cached forever.
            ongoingDownloads.Remove(url);
        }
    }
    [Serializable]
    private class ImageMeta
    {
        public string etag;
        public string lastModified;
    }

    // Core Logic
    private async Task<Sprite> LoadOrDownloadAsync(string url)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // On WebGL there is no reliable persistent file cache and the browser
        // already caches HTTP responses, so skip the disk layer entirely and
        // download straight into the memory cache. This also avoids the
        // EncodeToPNG()/File IO calls that fail on WebGL and would otherwise
        // fault the task and leave item cards stuck on "loading".
        return await DownloadToMemoryAsync(url);
#else
        string filePath = GetFilePathFromUrl(url);
        string metaPath = filePath + ".meta";

        if (File.Exists(filePath))
        {
            // Load any cached validation headers so we can make a conditional request
            ImageMeta meta = null;
            if (File.Exists(metaPath))
            {
                string json = File.ReadAllText(metaPath);
                meta = JsonUtility.FromJson<ImageMeta>(json);
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                if (meta != null)
                {
                    if (!string.IsNullOrEmpty(meta.etag))
                        request.SetRequestHeader("If-None-Match", meta.etag);
                    else if (!string.IsNullOrEmpty(meta.lastModified))
                        request.SetRequestHeader("If-Modified-Since", meta.lastModified);
                }

                var op = request.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                // 304 = image unchanged on server, keep cached copy
                if (request.responseCode == 304)
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    Sprite sprite = CreateSpriteFromBytes(data);
                    memoryCache[url] = sprite;
                    return sprite;
                }

#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    // 200 = image was updated, save new copy and carry on
                    return SaveDownloadedImage(url, filePath, metaPath, request);
                }

                // Network error — serve the stale cached copy rather than returning null
                Debug.LogWarning($"[ImageCache] Could not validate {url} ({request.error}), using cached copy.");
                byte[] cachedData = File.ReadAllBytes(filePath);
                Sprite cachedSprite = CreateSpriteFromBytes(cachedData);
                memoryCache[url] = cachedSprite;
                return cachedSprite;
            }
        }

        // No cached copy — fresh download
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Image download failed: {url}\n{request.error}");
                return null;
            }

            return SaveDownloadedImage(url, filePath, metaPath, request);
        }
#endif
    }

    // Plain download into the memory cache with no disk persistence.
    private async Task<Sprite> DownloadToMemoryAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"Image download failed: {url}\n{request.error}");
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = CreateSpriteFromTexture(texture);
            memoryCache[url] = sprite;
            return sprite;
        }
    }

    private Sprite SaveDownloadedImage(string url, string filePath, string metaPath, UnityWebRequest request)
    {
        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        Sprite sprite = CreateSpriteFromTexture(texture);

        // Cache the sprite before touching the disk so the caller always gets
        // its image even if persistence below fails.
        memoryCache[url] = sprite;

        // Disk persistence is best-effort — a failure here must not fault the
        // load or leave the caller stuck loading.
        try
        {
            byte[] pngData = texture.EncodeToPNG();
            if (pngData != null && pngData.Length > 0)
            {
                string metaJson = JsonUtility.ToJson(new ImageMeta
                {
                    etag = request.GetResponseHeader("ETag"),
                    lastModified = request.GetResponseHeader("Last-Modified")
                });

                File.WriteAllBytes(filePath, pngData);
                File.WriteAllText(metaPath, metaJson);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ImageCache] Failed to persist {url} to disk: {e.Message}");
        }

        return sprite;
    }

    // Helpers
    private Sprite CreateSpriteFromBytes(byte[] data)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(data);
        return CreateSpriteFromTexture(texture);
    }

    private Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    private string GetFilePathFromUrl(string url)
    {
        string hash = GetHash(url);
        return Path.Combine(CacheFolderPath, hash + ".png");
    }

    private string GetHash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("x2"));

            return sb.ToString();
        }
    }

    // Clear cache (optional use)
    public void ClearMemoryCache()
    {
        memoryCache.Clear();
    }

    public void ClearDiskCache()
    {
        if (Directory.Exists(CacheFolderPath))
            Directory.Delete(CacheFolderPath, true);

        Directory.CreateDirectory(CacheFolderPath);
    }
}
