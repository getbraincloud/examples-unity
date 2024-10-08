using System;
using System.Collections;
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
        MessageText.text = message.content.text;
        DateText.text = $"{message.date.ToShortDateString()} at {message.date.ToShortTimeString()}";

        if(message.ver > 1)
        {
            MessageText.text += " <size=50%>(edited)</size>";
        }

        ContentLayout.reverseArrangement = !isActiveUser;
        MessageBox.color = isActiveUser ? ACTIVE_USER_COLOR : OTHER_USER_COLOR;
        UserImage.color = isActiveUser ? ACTIVE_USER_COLOR : OTHER_USER_COLOR;
        if (message.from.name.Length > 0)
            UserInitialText.text = $"{message.from.name.ToUpper()[0]}";
        else
            UserInitialText.text = string.Empty;

        DisplayFooter(isActiveUser);

        Message = message;

        if (!message.from.pic.IsEmpty())
        {
            StartCoroutine(DownloadProfileImage());
        }
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
