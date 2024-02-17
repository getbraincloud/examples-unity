using BrainCloud;
using BrainCloud.JSONHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserContentUI : ContentUIBehaviour
{
    private const int MINIMUM_USERNAME_LENGTH = 4;
    private static readonly Color NO_PROFILE_PICTURE_COLOR = new Color32(0, 160, 255, 255);

    [Header("User Settings")]
    [SerializeField] private TMP_InputField UsernameField = default;
    [SerializeField] private Button PictureButton = default;
    [SerializeField] private RawImage UserImage = default;
    [SerializeField] private TMP_Text UserInitialText = default;
    [SerializeField] private Button ClearButton = default;

    [Header("Controls")]
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button BackButton = default;

    [Header("Navigation")]
    [SerializeField] private MainContentUI MainContent = default;

    private string currentPictureURL = string.Empty;
    private BrainCloudPlayerState playerService = null;

    #region Unity Messages

    protected override void Awake()
    {
        UsernameField.text = string.Empty;
        UserInitialText.text = string.Empty;
        UserImage.color = Color.white;

        base.Awake();
    }

    private void OnEnable()
    {
        UsernameField.onEndEdit.AddListener((username) => CheckUsernameVerification(username));
        PictureButton.onClick.AddListener(OnPictureButton);
        ClearButton.onClick.AddListener(OnClearButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        BackButton.onClick.AddListener(OnBackButton);
    }

    protected override void Start()
    {
        playerService = BCManager.PlayerStateService;

        base.Start();

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        UsernameField.onEndEdit.RemoveAllListeners();
        PictureButton.onClick.RemoveAllListeners();
        ClearButton.onClick.RemoveAllListeners();
        SaveButton.onClick.RemoveAllListeners();
        BackButton.onClick.RemoveAllListeners();
    }

    protected override void OnDestroy()
    {
        playerService = null;

        base.OnDestroy();
    }

    #endregion

    #region UI

    protected override void InitializeUI()
    {
        IsInteractable = false;

        UserImage.color = NO_PROFILE_PICTURE_COLOR;
        UserInitialText.gameObject.SetActive(true);

        void HandleReadUserState(string jsonResponse, object cbObject)
        {
            var data = jsonResponse.Deserialize("data");

            UsernameField.text = data.GetString("playerName");
            UserInitialText.text = $"{UsernameField.text.ToUpper()[0]}";

            if (data.GetString("pictureUrl") is string pictureURL &&
                !pictureURL.IsEmpty() && pictureURL != currentPictureURL)
            {
                StartCoroutine(DownloadProfileImage(pictureURL));
            }
            else
            {
                IsInteractable = true;
            }
        }

        playerService.ReadUserState(HandleReadUserState, HandleFailures);
    }

    private IEnumerator DownloadProfileImage(string pictureURL)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(pictureURL);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            currentPictureURL = pictureURL;
            UserImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            UserImage.color = Color.white;
            UserInitialText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError(request.error);
        }

        IsInteractable = true;
    }

    private bool CheckUsernameVerification(string value)
    {
        UsernameField.text = value.Trim();
        if (!UsernameField.text.IsEmpty())
        {
            if (UsernameField.text.Length < MINIMUM_USERNAME_LENGTH)
            {
                UsernameField.DisplayError();
                Debug.LogError($"Please use a username with at least {MINIMUM_USERNAME_LENGTH} characters.");
                return false;
            }

            return true;
        }

        return false;
    }

    private void OnPictureButton()
    {
        // Need to be able to upload new profile picture
    }
    private void OnClearButton()
    {
        if (UserImage.texture != null)
        {
            currentPictureURL = string.Empty;
            UserImage.texture = null;
            UserImage.color = NO_PROFILE_PICTURE_COLOR;
            UserInitialText.gameObject.SetActive(true);
        }
    }

    private void OnSaveButton()
    {
        if(!CheckUsernameVerification(UsernameField.text))
        {
            return;
        }

        IsInteractable = false;

        playerService.UpdateName(UsernameField.text);
        playerService.UpdateUserPictureUrl(currentPictureURL,
                                           (_, _) =>
                                           {
                                               OnBackButton();
                                           },
                                           HandleFailures);
    }

    private void OnBackButton()
    {
        MainContent.IsInteractable = true;
        MainContent.gameObject.SetActive(true);

        IsInteractable = false;
        gameObject.SetActive(false);
    }

    #endregion

    #region brainCloud

    private void HandleFailures(int status, int reasonCode, string jsonError, object cbObject)
    {
        IsInteractable = true;

        Debug.LogError("An error occurred. Please try again.");
    }

    #endregion
}
