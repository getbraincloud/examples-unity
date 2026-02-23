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
    // Core Logic
    private async Task<Sprite> LoadOrDownloadAsync(string url)
    {
        string filePath = GetFilePathFromUrl(url);

        // Check disk image
        if (File.Exists(filePath))
        {
            byte[] fileData = await Task.Run(() => File.ReadAllBytes(filePath));
            Sprite spriteFromDisk = CreateSpriteFromBytes(fileData);

            memoryCache[url] = spriteFromDisk;
            return spriteFromDisk;
        }

        // Download image
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
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

            // Save to disk
            byte[] pngData = texture.EncodeToPNG();
            await Task.Run(() => File.WriteAllBytes(filePath, pngData));

            memoryCache[url] = sprite;
            return sprite;
        }
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
