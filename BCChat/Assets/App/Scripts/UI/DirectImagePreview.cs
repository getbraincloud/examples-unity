using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Downloads and displays a direct image URL inside a chat message.
/// </summary>
[RequireComponent(typeof(RawImage), typeof(LayoutElement))]
public sealed class DirectImagePreview : MonoBehaviour, IPointerClickHandler
{
    // Advertise only formats handled by LoadImage or our GIF decoder. Some
    // CDNs return WebP for .jpg URLs when the Accept header allows it.
    public const string SupportedImageAccept = "image/gif,image/png,image/jpeg";
    private const float MaxWidth = 360.0f;
    private const float MaxHeight = 240.0f;
    private const long MaxDownloadBytes = 80L * 1024L * 1024L;
    private const long MaxDecodedBytes = 512L * 1024L * 1024L;
    private const long MaxGifCanvasPixels = 4096L * 4096L;
    private const float MinimumFrameDelay = 0.02f;
    private const int MaxCachedImages = 32;
    private static readonly Regex CloudinaryTransformationRegex = new(
        @"(/(?:images|image/upload)/)[^/]+(/v\d+/)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private sealed class CacheEntry
    {
        public Texture2D Texture;
        public List<UniGif.GifTexture> Frames;
        public int References;
        public int LastUse;
    }

    private static readonly Dictionary<string, CacheEntry> Cache = new();
    private static int useCounter;

    private RawImage image;
    private LayoutElement layout;
    private string imageUrl;
    private CacheEntry cacheEntry;
    private List<UniGif.GifTexture> animationFrames;
    private int animationFrame;
    private float nextFrameTime;
    private Action<bool> decodeCompleted;

    public void Initialize(string url)
    {
        InitializeView(url);

        if (TryDisplayCached())
        {
            return;
        }

        StartCoroutine(Download());
    }

    public void InitializeFromBytes(string url, byte[] bytes, string contentType, Action<bool> completed = null)
    {
        InitializeView(url);
        decodeCompleted = completed;

        if (TryDisplayCached())
        {
            CompleteDecode(true);
            return;
        }

        StartCoroutine(DecodeDownloadedBytes(bytes, contentType));
    }

    private void InitializeView(string url)
    {
        image = GetComponent<RawImage>();
        layout = GetComponent<LayoutElement>();
        imageUrl = url;

        image.color = Color.clear;
        image.raycastTarget = false;
        layout.preferredWidth = 0;
        layout.preferredHeight = 0;
    }

    private bool TryDisplayCached()
    {
        if (Cache.TryGetValue(imageUrl, out cacheEntry) && cacheEntry.Texture != null)
        {
            Acquire(cacheEntry);
            Display(cacheEntry.Texture);
            StartAnimation(cacheEntry.Frames);
            return true;
        }

        cacheEntry = null;
        return false;
    }

    private IEnumerator Download()
    {
        using UnityWebRequest request = UnityWebRequest.Get(GetPreviewDownloadUrl(imageUrl));
        request.timeout = 30;
        request.SetRequestHeader("Accept", SupportedImageAccept);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Unable to load image preview '{imageUrl}': {request.error}");
            yield break;
        }

        yield return DecodeDownloadedBytes(request.downloadHandler.data,
                                           request.GetResponseHeader("Content-Type") ?? string.Empty);
    }

    private IEnumerator DecodeDownloadedBytes(byte[] bytes, string contentType)
    {
        if (bytes == null || bytes.LongLength == 0 || bytes.LongLength > MaxDownloadBytes ||
            (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && !IsSupportedImageData(bytes)))
        {
            Debug.LogWarning($"The preview URL did not return a supported image: {imageUrl}");
            CompleteDecode(false);
            yield break;
        }

        if (IsGif(bytes))
        {
            yield return DecodeGif(bytes);
            yield break;
        }

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes, true))
        {
            Destroy(texture);
            Debug.LogWarning($"Unity could not decode image preview '{imageUrl}'.");
            CompleteDecode(false);
            yield break;
        }

        // Another message may have completed the same URL while this request
        // was in flight. Reuse that texture instead of replacing/leaking it.
        if (Cache.TryGetValue(imageUrl, out CacheEntry existing) && existing.Texture != null)
        {
            Destroy(texture);
            cacheEntry = existing;
            Acquire(cacheEntry);
            Display(cacheEntry.Texture);
            StartAnimation(cacheEntry.Frames);
            CompleteDecode(true);
            yield break;
        }

        cacheEntry = new CacheEntry { Texture = texture };
        Cache[imageUrl] = cacheEntry;
        Acquire(cacheEntry);
        TrimCache();
        Display(texture);
        CompleteDecode(true);
    }

    private IEnumerator DecodeGif(byte[] bytes)
    {
        int width = bytes[6] | bytes[7] << 8;
        int height = bytes[8] | bytes[9] << 8;
        if (width <= 0 || height <= 0 || (long)width * height > MaxGifCanvasPixels)
        {
            Debug.LogWarning($"GIF preview dimensions are invalid or too large: {imageUrl}");
            CompleteDecode(false);
            yield break;
        }

        List<UniGif.GifTexture> frames = null;
        yield return UniGif.GetTextureListCoroutine(bytes,
            (decodedFrames, loopCount, gifWidth, gifHeight) => frames = decodedFrames,
            FilterMode.Bilinear,
            TextureWrapMode.Clamp,
            false,
            firstFrame =>
            {
                if (image.texture == null)
                {
                    Display(firstFrame.m_texture2d);
                    CompleteDecode(true);
                }
            },
            (int)MaxWidth,
            (int)MaxHeight);

        if (frames == null || frames.Count == 0)
        {
            Debug.LogWarning($"Unity could not decode animated GIF preview '{imageUrl}'.");
            CompleteDecode(false);
            yield break;
        }

        long decodedBytes = 0;
        foreach (UniGif.GifTexture frame in frames)
        {
            decodedBytes += (long)frame.m_texture2d.width * frame.m_texture2d.height * 4L;
        }
        if (decodedBytes > MaxDecodedBytes)
        {
            DestroyFrames(frames);
            Debug.LogWarning($"Decoded GIF preview is too large: {imageUrl}");
            CompleteDecode(false);
            yield break;
        }

        // Reuse a GIF that another message finished decoding concurrently.
        if (Cache.TryGetValue(imageUrl, out CacheEntry existing) && existing.Texture != null)
        {
            DestroyFrames(frames);
            cacheEntry = existing;
            Acquire(cacheEntry);
            Display(cacheEntry.Texture);
            StartAnimation(cacheEntry.Frames);
            CompleteDecode(true);
            yield break;
        }

        cacheEntry = new CacheEntry
        {
            Texture = frames[0].m_texture2d,
            Frames = frames
        };
        Cache[imageUrl] = cacheEntry;
        Acquire(cacheEntry);
        TrimCache();
        Display(cacheEntry.Texture);
        StartAnimation(frames);
        CompleteDecode(true);
    }

    private void CompleteDecode(bool succeeded)
    {
        Action<bool> callback = decodeCompleted;
        decodeCompleted = null;
        callback?.Invoke(succeeded);
    }

    private void StartAnimation(List<UniGif.GifTexture> frames)
    {
        animationFrames = frames;
        animationFrame = 0;
        if (animationFrames != null && animationFrames.Count > 1)
        {
            nextFrameTime = Time.unscaledTime + Mathf.Max(MinimumFrameDelay, animationFrames[0].m_delaySec);
        }
    }

    private void Update()
    {
        if (animationFrames == null || animationFrames.Count <= 1 || Time.unscaledTime < nextFrameTime)
        {
            return;
        }

        animationFrame = (animationFrame + 1) % animationFrames.Count;
        image.texture = animationFrames[animationFrame].m_texture2d;
        nextFrameTime = Time.unscaledTime + Mathf.Max(MinimumFrameDelay, animationFrames[animationFrame].m_delaySec);
    }

    private void Display(Texture2D texture)
    {
        float scale = Mathf.Min(1.0f, MaxWidth / texture.width, MaxHeight / texture.height);
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = true;
        layout.preferredWidth = Mathf.Max(1.0f, texture.width * scale);
        layout.preferredHeight = Mathf.Max(1.0f, texture.height * scale);
        LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
    }

    public static bool IsSupportedImageData(byte[] bytes)
    {
        return bytes.Length >= 8 &&
               ((bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) ||
                (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                 bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A) ||
                IsGif(bytes));
    }

    public static string GetPreviewDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
            !uri.Host.EndsWith(".cloudinary.com", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // Cloudinary transformation URLs can provide a dramatically smaller
        // animated preview. Keep imageUrl unchanged so clicks still open the
        // original asset, and only rewrite the network request.
        return CloudinaryTransformationRegex.Replace(url, "$1w_360,c_limit$2", 1);
    }

    private static bool IsGif(byte[] bytes)
    {
        return bytes.Length >= 10 && bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F' &&
               bytes[3] == '8' && (bytes[4] == '7' || bytes[4] == '9') && bytes[5] == 'a';
    }

    private static void Acquire(CacheEntry entry)
    {
        entry.References++;
        entry.LastUse = ++useCounter;
    }

    private static void TrimCache()
    {
        while (Cache.Count > MaxCachedImages)
        {
            string oldestUrl = null;
            CacheEntry oldest = null;
            foreach (var pair in Cache)
            {
                if (pair.Value.References == 0 && (oldest == null || pair.Value.LastUse < oldest.LastUse))
                {
                    oldestUrl = pair.Key;
                    oldest = pair.Value;
                }
            }

            if (oldest == null)
            {
                return;
            }

            Cache.Remove(oldestUrl);
            if (oldest.Frames != null)
            {
                DestroyFrames(oldest.Frames);
            }
            else
            {
                Destroy(oldest.Texture);
            }
        }
    }

    private static void DestroyFrames(List<UniGif.GifTexture> frames)
    {
        foreach (UniGif.GifTexture frame in frames)
        {
            if (frame?.m_texture2d != null) Destroy(frame.m_texture2d);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (image != null && image.texture != null)
        {
            Application.OpenURL(imageUrl);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        animationFrames = null;
        if (cacheEntry != null)
        {
            cacheEntry.References = Mathf.Max(0, cacheEntry.References - 1);
            cacheEntry.LastUse = ++useCounter;
            cacheEntry = null;
            TrimCache();
        }
    }
}
