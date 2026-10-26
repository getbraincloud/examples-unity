using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Displays a compact Open Graph/Twitter metadata card for a web-page URL.
/// </summary>
[RequireComponent(typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))]
public sealed class UrlPreviewCard : MonoBehaviour, IPointerClickHandler
{
    private const float CardWidth = 400.0f;
    private const float ImageMaxWidth = 360.0f;
    private const float ImageMaxHeight = 240.0f;
    private const long MaxHtmlBytes = 2L * 1024L * 1024L;
    private const long MaxImageBytes = 20L * 1024L * 1024L;
    private const string GoogleDescription = "Search the world's information, including webpages, images, videos and more. " +
                                             "Google has many special features to help you find exactly what you're looking for.";

    private static readonly Regex MetaTagRegex = new("<meta\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TitleRegex = new("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private string pageUrl;
    private TMP_Text providerText;
    private TMP_Text authorText;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private RawImage previewImage;
    private LayoutElement imageLayout;
    private Texture2D downloadedTexture;
    private string youtubeId;
    private bool initialized;
    private bool loading;
    private bool complete;

    [Serializable]
    private sealed class YouTubeMetadata
    {
        public string title = string.Empty;
        public string author_name = string.Empty;
    }

    public void Initialize(string url, TMP_Text messageTextStyle, Image cardStyle)
    {
        pageUrl = url;
        TryGetYouTubeId(pageUrl, out youtubeId);
        initialized = true;

        // The card already displays the destination host and loading state.
        // Remove the duplicated raw URL immediately, including for CDN links
        // whose response can only be recognized as an image after download.
        GetComponentInParent<ChatMessage>()?.HidePreviewUrl(pageUrl);

        ConfigureCard(cardStyle);

        providerText = CreateText("Provider", messageTextStyle, 12, new Color32(170, 180, 195, 255));
        authorText = CreateText("Author", messageTextStyle, 16, Color.white);
        authorText.fontStyle = FontStyles.Bold;
        titleText = CreateText("Title", messageTextStyle, 16, new Color32(95, 175, 255, 255));
        titleText.fontStyle = FontStyles.Bold;
        descriptionText = CreateText("Description", messageTextStyle, 13, new Color32(220, 225, 235, 255));
        CreatePreviewImage();

        providerText.text = string.IsNullOrEmpty(youtubeId) ? GetHost(pageUrl) : "YouTube";
        titleText.text = "Loading preview...";
        authorText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);
        previewImage.gameObject.SetActive(false);

        BeginLoading();
    }

    private void OnEnable()
    {
        // Unity stops coroutines when a parent view is deactivated. Resume a
        // card that was interrupted by navigating away from the chat screen.
        if (initialized && !complete && !loading)
        {
            BeginLoading();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        loading = false;
    }

    private void BeginLoading()
    {
        loading = true;
        StartCoroutine(DownloadMetadata());
    }

    private void ConfigureCard(Image cardStyle)
    {
        Image background = GetComponent<Image>();
        background.color = new Color32(50, 52, 52, 255);
        background.raycastTarget = true;
        background.sprite = cardStyle.sprite;
        background.type = cardStyle.type;

        Outline border = gameObject.AddComponent<Outline>();
        border.effectColor = new Color32(105, 110, 110, 255);
        border.effectDistance = new Vector2(1, -1);
        border.useGraphicAlpha = true;

        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement cardLayout = gameObject.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = CardWidth;
        cardLayout.flexibleWidth = 0;
    }

    private TMP_Text CreateText(string objectName, TMP_Text style, float size, Color color)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(transform, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = style.font;
        text.fontSharedMaterial = style.fontSharedMaterial;
        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        return text;
    }

    private void CreatePreviewImage()
    {
        GameObject imageObject = new("PageImage", typeof(RectTransform), typeof(CanvasRenderer),
                                          typeof(RawImage), typeof(LayoutElement));
        imageObject.layer = gameObject.layer;
        imageObject.transform.SetParent(transform, false);
        previewImage = imageObject.GetComponent<RawImage>();
        previewImage.raycastTarget = false;
        imageLayout = imageObject.GetComponent<LayoutElement>();
    }

    private IEnumerator DownloadMetadata()
    {
        if (!string.IsNullOrEmpty(youtubeId))
        {
            yield return DownloadYouTubeMetadata();
            yield break;
        }

        // Prefer TLS for metadata retrieval. This also avoids inconsistent
        // behavior from sites that handle HTTP-to-HTTPS redirects differently.
        string requestUrl = pageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? "https://" + pageUrl.Substring("http://".Length)
            : pageUrl;
        requestUrl = DirectImagePreview.GetPreviewDownloadUrl(requestUrl);
        using UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        request.timeout = 30;
        request.SetRequestHeader("User-Agent", "BCChat/1.0");
        request.SetRequestHeader("Accept", "text/html,application/xhtml+xml," + DirectImagePreview.SupportedImageAccept);
        yield return request.SendWebRequest();

        byte[] responseBytes = request.downloadHandler?.data;
        string contentType = request.GetResponseHeader("Content-Type") ?? string.Empty;
        if (request.result == UnityWebRequest.Result.Success && responseBytes != null &&
            (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
             DirectImagePreview.IsSupportedImageData(responseBytes)))
        {
            PromoteToDirectImage(responseBytes, contentType);
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success || responseBytes == null ||
            responseBytes.LongLength > MaxHtmlBytes)
        {
            ShowFallback();
            yield break;
        }

        if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            ShowFallback();
            yield break;
        }

        string html = request.downloadHandler.text;
        string resolvedPageUrl = request.url;
        string title = string.Empty;
        string description = string.Empty;
        string siteName = string.Empty;
        string imageUrl = string.Empty;

        foreach (Match match in MetaTagRegex.Matches(html))
        {
            string tag = match.Value;
            string key = GetAttribute(tag, "property");
            if (string.IsNullOrEmpty(key)) key = GetAttribute(tag, "name");
            if (string.IsNullOrEmpty(key)) key = GetAttribute(tag, "itemprop");
            string content = DecodeText(GetAttribute(tag, "content"));
            if (string.IsNullOrEmpty(content)) continue;

            switch (key.ToLowerInvariant())
            {
                case "og:title": title = content; break;
                case "og:description": description = content; break;
                case "og:site_name": siteName = content; break;
                case "og:image": imageUrl = ResolveUrl(resolvedPageUrl, content); break;
                case "twitter:title" when string.IsNullOrEmpty(title): title = content; break;
                case "twitter:description" when string.IsNullOrEmpty(description): description = content; break;
                case "twitter:image" when string.IsNullOrEmpty(imageUrl): imageUrl = ResolveUrl(resolvedPageUrl, content); break;
                case "description" when string.IsNullOrEmpty(description): description = content; break;
            }
        }

        if (string.IsNullOrEmpty(title))
        {
            Match titleMatch = TitleRegex.Match(html);
            if (titleMatch.Success) title = DecodeText(titleMatch.Groups[1].Value);
        }

        string foldedTitle = title.ToLowerInvariant();
        if (foldedTitle == "loading" || foldedTitle == "loading..." ||
            foldedTitle == "untitled" || foldedTitle == "untitled page")
        {
            title = description;
            description = string.Empty;
        }

        string pageHost = GetHost(resolvedPageUrl).ToLowerInvariant();
        if (string.IsNullOrEmpty(description) && (pageHost == "google.com" || pageHost == "www.google.com"))
        {
            description = GoogleDescription;
        }

        providerText.text = string.IsNullOrEmpty(siteName) ? GetHost(pageUrl) : siteName;
        titleText.text = string.IsNullOrEmpty(title) ? "Web page" : Truncate(title, 120);
        if (!string.IsNullOrEmpty(description))
        {
            descriptionText.text = Truncate(description, 300);
            descriptionText.gameObject.SetActive(true);
        }

        if (!string.IsNullOrEmpty(imageUrl))
        {
            yield return DownloadImage(imageUrl);
        }

        RebuildLayout();
        loading = false;
        complete = true;
    }

    private void PromoteToDirectImage(byte[] bytes, string contentType)
    {
        ChatMessage chatMessage = GetComponentInParent<ChatMessage>();
        chatMessage?.HidePreviewUrl(pageUrl);

        GameObject previewObject = new("DirectImagePreview", typeof(RectTransform), typeof(CanvasRenderer),
                                              typeof(RawImage), typeof(LayoutElement), typeof(DirectImagePreview));
        previewObject.layer = gameObject.layer;
        previewObject.transform.SetParent(transform.parent, false);
        previewObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
        previewObject.GetComponent<DirectImagePreview>().InitializeFromBytes(pageUrl, bytes, contentType, succeeded =>
        {
            if (succeeded)
            {
                complete = true;
                loading = false;
                Destroy(gameObject);
                return;
            }

            Destroy(previewObject);
            ShowFallback();
        });
    }

    private IEnumerator DownloadYouTubeMetadata()
    {
        string metadataUrl = "https://www.youtube.com/oembed?format=json&url=" + UnityWebRequest.EscapeURL(pageUrl);
        using (UnityWebRequest request = UnityWebRequest.Get(metadataUrl))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                YouTubeMetadata metadata = JsonUtility.FromJson<YouTubeMetadata>(request.downloadHandler.text);
                titleText.text = string.IsNullOrEmpty(metadata?.title) ? "YouTube video" : Truncate(metadata.title, 120);
                if (!string.IsNullOrEmpty(metadata?.author_name))
                {
                    authorText.text = metadata.author_name;
                    authorText.gameObject.SetActive(true);
                }
            }
            else
            {
                titleText.text = "YouTube video";
            }
        }

        yield return DownloadImage($"https://img.youtube.com/vi/{youtubeId}/hqdefault.jpg");
        RebuildLayout();
        loading = false;
        complete = true;
    }

    private IEnumerator DownloadImage(string imageUrl)
    {
        using UnityWebRequest request = UnityWebRequest.Get(imageUrl);
        request.timeout = 30;
        request.SetRequestHeader("Accept", DirectImagePreview.SupportedImageAccept);
        yield return request.SendWebRequest();

        byte[] bytes = request.downloadHandler?.data;
        if (request.result != UnityWebRequest.Result.Success || bytes == null ||
            bytes.LongLength == 0 || bytes.LongLength > MaxImageBytes)
        {
            yield break;
        }

        downloadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!downloadedTexture.LoadImage(bytes, true))
        {
            Destroy(downloadedTexture);
            downloadedTexture = null;
            yield break;
        }

        float scale = Mathf.Min(1.0f, ImageMaxWidth / downloadedTexture.width,
                                      ImageMaxHeight / downloadedTexture.height);
        previewImage.texture = downloadedTexture;
        imageLayout.preferredWidth = Mathf.Max(1.0f, downloadedTexture.width * scale);
        imageLayout.preferredHeight = Mathf.Max(1.0f, downloadedTexture.height * scale);
        previewImage.gameObject.SetActive(true);
    }

    private void ShowFallback()
    {
        string host = GetHost(pageUrl);
        bool isGoogle = host.Equals("google.com", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("www.google.com", StringComparison.OrdinalIgnoreCase);
        providerText.text = host;
        titleText.text = isGoogle ? "Google" : "Web page";
        descriptionText.text = isGoogle ? GoogleDescription : string.Empty;
        descriptionText.gameObject.SetActive(isGoogle);
        RebuildLayout();
        loading = false;
        complete = true;
    }

    private void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        LayoutRebuilder.MarkLayoutForRebuild(transform.parent as RectTransform);
    }

    private static string GetAttribute(string tag, string attribute)
    {
        Match match = Regex.Match(tag, $@"(?:^|\s){Regex.Escape(attribute)}\s*=\s*([""'])(.*?)\1",
                                  RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[2].Value : string.Empty;
    }

    private static string DecodeText(string value)
    {
        string decoded = System.Net.WebUtility.HtmlDecode(Regex.Replace(value ?? string.Empty, "<[^>]+>", string.Empty));
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static string ResolveUrl(string page, string value)
    {
        return Uri.TryCreate(new Uri(page), value, out Uri resolved) ? resolved.AbsoluteUri : string.Empty;
    }

    private static bool TryGetYouTubeId(string url, out string id)
    {
        id = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
        {
            return false;
        }

        string host = uri.Host.ToLowerInvariant();
        if (host == "youtu.be" || host.EndsWith(".youtu.be", StringComparison.Ordinal))
        {
            id = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else if (host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal))
        {
            string[] segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length >= 2 &&
                (segments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase) ||
                 segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase) ||
                 segments[0].Equals("live", StringComparison.OrdinalIgnoreCase)))
            {
                id = segments[1];
            }
            else
            {
                foreach (string pair in uri.Query.TrimStart('?').Split('&'))
                {
                    string[] parts = pair.Split(new[] { '=' }, 2);
                    if (parts.Length == 2 && parts[0] == "v")
                    {
                        id = Uri.UnescapeDataString(parts[1]);
                        break;
                    }
                }
            }
        }

        int delimiter = id.IndexOfAny(new[] { '&', '?', '#', '/' });
        if (delimiter >= 0) id = id.Substring(0, delimiter);
        return !string.IsNullOrWhiteSpace(id);
    }

    public static bool IsYouTubeUrl(string url) => TryGetYouTubeId(url, out _);

    private static string GetHost(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ? uri.Host : url;
    }

    private static string Truncate(string value, int length)
    {
        return value.Length <= length ? value : value.Substring(0, length - 3).TrimEnd() + "...";
    }

    public void OnPointerClick(PointerEventData eventData) => Application.OpenURL(pageUrl);

    private void OnDestroy()
    {
        StopAllCoroutines();
        if (downloadedTexture != null) Destroy(downloadedTexture);
    }
}
