using BrainCloud;
using BrainCloud.JSONHelper;
using System.Collections;
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
    [SerializeField] private TMP_InputField PictureURLField = default;
    [SerializeField] private Button ClearButton = default;

    [Header("Controls")]
    [SerializeField] private Button SaveButton = default;
    [SerializeField] private Button BackButton = default;

    [Header("Navigation")]
    [SerializeField] private MainContentUI MainContent = default;

    private BrainCloudPlayerState playerService = null;

    #region Unity Messages

    protected override void Awake()
    {
        UsernameField.text = string.Empty;
        UserInitialText.text = string.Empty;
        PictureURLField.text = string.Empty;
        UserImage.color = Color.white;

        base.Awake();
    }

    private void OnEnable()
    {
        UsernameField.onEndEdit.AddListener((username) => CheckUsernameVerification(username));
        //PictureButton.onClick.AddListener(OnPictureButton);
        PictureURLField.onEndEdit.AddListener((url) => CheckURLVerification(url));
        ClearButton.onClick.AddListener(OnClearButton);
        SaveButton.onClick.AddListener(OnSaveButton);
        BackButton.onClick.AddListener(OnBackButton);
    }

    protected override void Start()
    {
        playerService = BCManager.PlayerStateService;

        base.Start();

        PictureButton.interactable = false;
        IsInteractable = false;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        UsernameField.onEndEdit.RemoveAllListeners();
        //PictureButton.onClick.RemoveAllListeners();
        PictureURLField.onEndEdit.RemoveAllListeners();
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

        UsernameField.text = string.Empty;
        UserInitialText.text = string.Empty;
        UserImage.color = NO_PROFILE_PICTURE_COLOR;
        UserInitialText.gameObject.SetActive(true);
        PictureURLField.text = string.Empty;
        UserImage.color = new Color32(0, 160, 255, 255);
        PictureURLField.gameObject.SetActive(true);
        ClearButton.gameObject.SetActive(false);

        // Get the name and profile image of the user
        playerService.ReadUserState(HandleReadUserState,
                                    OnFailure("Could not read profile.", OnBackButton));
    }

    private IEnumerator DownloadProfileImage(string pictureURL)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(pictureURL);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PictureURLField.text = pictureURL;
            UserImage.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            UserImage.color = Color.white;
            UserInitialText.gameObject.SetActive(false);
            PictureURLField.gameObject.SetActive(false);
            ClearButton.gameObject.SetActive(true);
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

    private bool CheckURLVerification(string value)
    {
        PictureURLField.text = value.Trim();
        if (!PictureURLField.text.IsEmpty())
        {
            if (PictureURLField.text.Length <= 3 || PictureURLField.text.Split('.').Length < 2)
            {
                PictureURLField.DisplayError();
                Debug.LogError($"Server URL is not a proper URL or the server is offline.");
                return false;
            }

            return true;
        }

        return false;
    }

    //private void OnPictureButton() { }

    private void OnClearButton()
    {
        if (UserImage.texture != null)
        {
            UserImage.texture = null;
            UserImage.color = NO_PROFILE_PICTURE_COLOR;
            UserInitialText.gameObject.SetActive(true);
            PictureURLField.text = string.Empty;
            PictureURLField.gameObject.SetActive(true);
            ClearButton.gameObject.SetActive(false);
        }
    }

    private void OnSaveButton()
    {
        if(!CheckUsernameVerification(UsernameField.text) &&
           !PictureURLField.text.IsEmpty() &&
           !CheckURLVerification(PictureURLField.text))
        {
            return;
        }

        IsInteractable = false;

        playerService.UpdateName(UsernameField.text);
        playerService.UpdateUserPictureUrl(PictureURLField.text,
                                           OnSuccess("Profile changes saved!", OnBackButton),
                                           OnFailure("Could not save profile. Please try again.", () => IsInteractable = true));
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

    void HandleReadUserState(string jsonResponse, object cbObject)
    {
        var data = jsonResponse.Deserialize("data");

        UsernameField.text = data.GetString("playerName");
        UserInitialText.text = $"{UsernameField.text.ToUpper()[0]}";

        if (data.GetString("pictureUrl") is string pictureURL &&
            !pictureURL.IsEmpty() && pictureURL != PictureURLField.text)
        {
            StartCoroutine(DownloadProfileImage(pictureURL));
        }
        else
        {
            IsInteractable = true;
        }
    }

    #endregion
}
