using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Displays the content of brainCloud's <see cref="Message"/> in a Prefab.
/// </summary>
public class ChatMessage : MonoBehaviour
{
    private const int PROFILE_DISPLAY_SIZE = 100;
    private static readonly Color ACTIVE_USER_COLOR = new Color32(20, 35, 75, 255);
    private static readonly Color OTHER_USER_COLOR = new Color32(66, 66, 66, 255);
    private static readonly Regex UrlRegex = new(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Header("Header")]
    [SerializeField] private GameObject Header = default;
    [SerializeField] private TMP_Text UsernameText = default;
    [SerializeField] private TMP_Text DateText = default;

    [Header("Content")]
    [SerializeField] private HorizontalLayoutGroup ContentLayout = default;
    [SerializeField] private Image MessageBox = default;
    [SerializeField] private TMP_Text MessageText = default;

    [Header("Profile Image")]
    [SerializeField] private GameObject ProfileImageContainer = default;
    [SerializeField] private LayoutElement UserImageLayoutElement = default;
    [SerializeField] private GameObject ImageContainer = default;
    [SerializeField] private RawImage UserImage = default;
    [SerializeField] private TMP_Text UserInitialText = default;

    [Header("Footer")]
    [SerializeField] private GameObject Footer = default;
    [SerializeField] private Button DeleteButton = default;
    [SerializeField] private Button EditButton = default;

    public Message Message { get; private set; }

    public Action<Message> DeleteAction = null;
    public Action<Message> EditAction = null;

    #region Unity Messages

    private void Awake()
    {
        UsernameText.text = string.Empty;
        MessageText.text = string.Empty;
        MessageText.emojiFallbackSupport = true;
        DateText.text = string.Empty;
    }

    private void OnEnable()
    {
        DeleteButton.onClick.AddListener(OnDeleteButton);
        EditButton.onClick.AddListener(OnEditButton);
    }

    private void OnDisable()
    {
        DeleteButton.onClick.RemoveAllListeners();
        EditButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        DeleteAction = null;
        EditAction = null;
    }

    #endregion

    #region UI

    public void DisplayHeader(bool isDisplayed)
    {
        Header.SetActive(isDisplayed);
        UserImageLayoutElement.minHeight = isDisplayed ? PROFILE_DISPLAY_SIZE : 0;
        ImageContainer.SetActive(isDisplayed);
    }

    public void DisplayProfileImage(bool isDisplayed)
    {
        ProfileImageContainer.SetActive(isDisplayed);
    }

    public void DisplayFooter(bool isDisplayed)
    {
        Footer.SetActive(isDisplayed);
    }

    public void SetChatContents(Message message)
    {
        bool isActiveUser = message.from.id == UserHandler.ProfileID;

        UsernameText.text = message.from.name;
        MessageText.text = RemovePreviewOnlyUrls(EmojiShortcodes.Expand(message.content.text));
        DateText.text = $"{message.date.ToShortDateString()} at {message.date.ToShortTimeString()}";

        if(message.ver > 1 && !MessageText.text.IsEmpty())
        {
            MessageText.text += " <size=50%>(edited)</size>";
        }
        MessageText.gameObject.SetActive(!MessageText.text.IsEmpty());

        ContentLayout.reverseArrangement = !isActiveUser;
        MessageBox.color = isActiveUser ? ACTIVE_USER_COLOR : OTHER_USER_COLOR;
        UserImage.color = isActiveUser ? ACTIVE_USER_COLOR : OTHER_USER_COLOR;
        if (message.from.name.Length > 0)
            UserInitialText.text = $"{message.from.name.ToUpper()[0]}";
        else
            UserInitialText.text = string.Empty;

        DisplayFooter(isActiveUser);

        Message = message;

        RebuildLinkPreviews(message.content.text);

        if (!message.from.pic.IsEmpty())
        {
            StartCoroutine(DownloadProfileImage());
        }
    }

    private void RebuildLinkPreviews(string text)
    {
        for (int i = MessageBox.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = MessageBox.transform.GetChild(i);
            if (child.GetComponent<DirectImagePreview>() != null || child.GetComponent<UrlPreviewCard>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Match match in UrlRegex.Matches(text ?? string.Empty))
        {
            string url = match.Value.TrimEnd('.', ',', '!', ')', ']');
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                continue;
            }

            bool isDirectImage = IsDirectImageUrl(url);
            GameObject previewObject = isDirectImage
                ? new GameObject("DirectImagePreview", typeof(RectTransform), typeof(CanvasRenderer),
                                 typeof(RawImage), typeof(LayoutElement), typeof(DirectImagePreview))
                : new GameObject("UrlPreviewCard", typeof(RectTransform), typeof(CanvasRenderer),
                                 typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(UrlPreviewCard));
            previewObject.layer = gameObject.layer;
            previewObject.transform.SetParent(MessageBox.transform, false);
            previewObject.transform.SetSiblingIndex(Footer.transform.GetSiblingIndex());
            if (isDirectImage)
            {
                previewObject.GetComponent<DirectImagePreview>().Initialize(url);
            }
            else
            {
                previewObject.GetComponent<UrlPreviewCard>().Initialize(url, MessageText, MessageBox);
            }
        }
    }

    private static bool IsDirectImageUrl(string url)
    {
        if (!System.Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        string path = Uri.UnescapeDataString(uri.AbsolutePath);
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemovePreviewOnlyUrls(string text)
    {
        string withoutImages = UrlRegex.Replace(text ?? string.Empty, match =>
        {
            string url = match.Value.TrimEnd('.', ',', '!', ')', ']');
            return IsDirectImageUrl(url) || UrlPreviewCard.IsYouTubeUrl(url) ? string.Empty : match.Value;
        });

        return withoutImages.Trim();
    }

    public void HidePreviewUrl(string url)
    {
        MessageText.text = UrlRegex.Replace(MessageText.text, match =>
        {
            string candidate = match.Value.TrimEnd('.', ',', '!', ')', ']');
            return string.Equals(candidate, url, StringComparison.OrdinalIgnoreCase)
                ? match.Value.Substring(candidate.Length)
                : match.Value;
        }).Trim();
        MessageText.gameObject.SetActive(!MessageText.text.IsEmpty());
        LayoutRebuilder.MarkLayoutForRebuild(MessageBox.rectTransform);
    }

    private IEnumerator DownloadProfileImage()
    {
        yield return new WaitForEndOfFrame();

        if (!ImageContainer.activeSelf)
        {
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(Message.from.pic);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            UserImage.color = Color.white;
            UserInitialText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    private void OnDeleteButton()
    {
        DeleteAction?.Invoke(Message);
    }

    private void OnEditButton()
    {
        EditAction?.Invoke(Message);
    }

    #endregion
}
