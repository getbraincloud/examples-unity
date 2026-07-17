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

        // First check memory cache
        if (memoryCache.TryGetValue(url, out Sprite cachedSprite))
            return cachedSprite;

        // Prevent duplicate downloads
        if (ongoingDownloads.TryGetValue(url, out Task<Sprite> existingTask))
            return await existingTask;

        // Either get image from disk if exists or download it if not
        var downloadTask = LoadOrDownloadAsync(url);
        ongoingDownloads[url] = downloadTask;

        Sprite result = await downloadTask;

        ongoingDownloads.Remove(url);

        return result;
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
                    return await SaveDownloadedImage(url, filePath, metaPath, request);
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

            return await SaveDownloadedImage(url, filePath, metaPath, request);
        }
    }

    private async Task<Sprite> SaveDownloadedImage(string url, string filePath, string metaPath, UnityWebRequest request)
    {
        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        Sprite sprite = CreateSpriteFromTexture(texture);
        byte[] pngData = texture.EncodeToPNG();

        string metaJson = JsonUtility.ToJson(new ImageMeta
        {
            etag = request.GetResponseHeader("ETag"),
            lastModified = request.GetResponseHeader("Last-Modified")
        });

        File.WriteAllBytes(filePath, pngData);
        File.WriteAllText(metaPath, metaJson);

        memoryCache[url] = sprite;
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
