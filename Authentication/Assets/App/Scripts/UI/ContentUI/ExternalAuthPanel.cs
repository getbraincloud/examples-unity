using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using UnityEngine;

#if FACEBOOK_SDK
using Facebook.Unity;
#endif

using GooglePlayGames;

public class ExternalAuthPanel : ContentUIBehaviour
{
    [Header("Main")]
    [SerializeField] private Transform ButtonContent = default;
    [SerializeField] private ButtonContent ButtonTemplate = default;
    [SerializeField] private ExternalAuthItem[] AuthItems = default;

    [Header("UI Control")]
    [SerializeField] private MainMenuUI MainMenu = default;
    [SerializeField] private PopupUI Popup = default;
    [SerializeField] private LoginContentUI LoginContent = default;
    [SerializeField] private MainLoginPanelUI MainLoginPanel = default;

    private AuthenticationType selectedAuthenticationType = AuthenticationType.Unknown;
    private List<ButtonContent> authButtons = default;

    #region Unity Messages

    private void OnEnable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = true;
            }
        }
    }

    protected override void Start()
    {
        authButtons = new List<ButtonContent>();

        foreach (ExternalAuthItem authItem in AuthItems)
        {
            ButtonContent button = Instantiate(ButtonTemplate, ButtonContent);
            button.gameObject.SetActive(true);
            button.gameObject.SetName(authItem.Name, "{0}MenuItem");
            button.Label = authItem.Name;
            button.LeftIcon = authItem.Icon;
            button.LabelColor = authItem.LabelColor;
            button.LeftIconColor = authItem.IconColor;
            button.BackgroundColor = authItem.BackgroundColor;
            button.Button.onClick.AddListener(() => OnExternalAuthentication(authItem.AuthenticationType));

            authButtons.Add(button);
        }

        InitializeUI();

        base.Start();
    }

    private void OnDisable()
    {
        if (!authButtons.IsNullOrEmpty())
        {
            foreach (ButtonContent button in authButtons)
            {
                button.enabled = false;
            }
        }
    }

    protected override void OnDestroy()
    {
        authButtons.Clear();
        authButtons = null;

        base.OnDestroy();
    }

    #endregion

    #region UI & Authentication

    protected override void InitializeUI()
    {
#if FACEBOOK_SDK
        void OnInitComplete()
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
                FB.LimitAppEventUsage = true;
            }
            else
            {
                Debug.LogError("Failed to Initialize the Facebook SDK!");
            }
        }

        void HandleFacebookInitialized(bool isGameShown)
        {
            LoginContent.IsInteractable = isGameShown;
        }

        if (!FB.IsInitialized)
        {
            FB.Init(OnInitComplete, HandleFacebookInitialized);
        }
        else
        {
            OnInitComplete();
        }
#endif

        PlayGamesPlatform.DebugLogEnabled = true;

    }

    private void HandleFacebookAuthenticationButton()
    {
#if FACEBOOK_SDK && (UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID)
        selectedAuthenticationType = AuthenticationType.Facebook;
        UserHandler.AuthenticateFacebook(true,
                                         OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                         OnFailure("Authentication Failed", OnAuthenticationFailure));
#elif FACEBOOK_SDK && UNITY_IOS
        PopupInfoButton[] buttons = new PopupInfoButton[]
        { new PopupInfoButton("Facebook Standard", PopupInfoButton.Color.Blue, () =>
            {
                selectedAuthenticationType = AuthenticationType.Facebook;
                UserHandler.AuthenticateFacebook(true,
                                                 OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                                 OnFailure("Authentication Failed", OnAuthenticationFailure));
            }),
            new PopupInfoButton("Facebook Limited", PopupInfoButton.Color.Blue, () =>
            {
                selectedAuthenticationType = AuthenticationType.FacebookLimited;
                UserHandler.AuthenticateFacebookLimited(true,
                                                        OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                                        OnFailure("Authentication Failed", OnAuthenticationFailure));
            }),
            new PopupInfoButton("Cancel", PopupInfoButton.Color.Red, () =>
            {
                selectedAuthenticationType = AuthenticationType.Unknown;
                LoginContent.IsInteractable = true;
            })
        };

        Popup.DisplayPopup(new PopupInfo("Facebook Login Preference",
                                     new PopupInfoBody[] { new PopupInfoBody("Would you like to log into Facebook in Standard or Limited mode?", PopupInfoBody.Type.Centered),
                                                           new PopupInfoBody("Note: Limited mode does not allow you to use Facebook's Graph API features.", PopupInfoBody.Type.Centered)},
                                     buttons,
                                     false));
#else
        Debug.LogError("Either FACEBOOK_SDK has not been added to your Scripting Define Symbols or AuthenticateFacebook is not available on this platform.");
        selectedAuthenticationType = AuthenticationType.Unknown;
        LoginContent.IsInteractable = true;
#endif
    }

    private void OnExternalAuthentication(AuthenticationType type)
    {
        LoginContent.IsInteractable = false;
        selectedAuthenticationType = type;

        if (type == AuthenticationType.Facebook ||
            type == AuthenticationType.FacebookLimited)
        {
            HandleFacebookAuthenticationButton();
        }
        else if (type == AuthenticationType.Google)
        {
            UserHandler.AuthenticateGoogle(true,
                                           OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                           OnFailure("Authentication Failed", OnAuthenticationFailure));
        }
        else
        {
            Debug.LogError($"Authentication method is either unavailable on this platform or is unknown: {type}");
            selectedAuthenticationType = AuthenticationType.Unknown;
            LoginContent.IsInteractable = true;
        }
    }

    private void OnAuthenticationSuccess()
    {
        BCManager.IdentityService.GetIdentities(OnSuccess("Get Identities Success", OnGetIdentitiesSuccess),
                                                OnFailure("Get Identities Failed", OnAuthenticationFailure));
    }

    private void OnGetIdentitiesSuccess(string response)
    {
        var data = (JsonReader.Deserialize(response) as Dictionary<string, object>)["data"] as Dictionary<string, object>;

        UserHandler.AnonymousUser = (data["identities"] as Dictionary<string, object>).Count <= 0;

        BCManager.Wrapper.SetStoredAuthenticationType(selectedAuthenticationType.ToString());
        MainLoginPanel.SetRememberMePref(true);

        MainMenu.ChangeToAppContent();
    }

    private void OnAuthenticationFailure(ErrorResponse response)
    {
#if FACEBOOK_SDK
        if (FB.IsLoggedIn)
        {
            FB.LogOut();
        }
#endif

        Popup.DisplayPopup(new PopupInfo("Could not Authenticate",
                                         new PopupInfoBody[] { new PopupInfoBody(response.Message, PopupInfoBody.Type.Centered) },
                                         null, true, "Close"));

        selectedAuthenticationType = AuthenticationType.Unknown;
        LoginContent.IsInteractable = true;
    }

    #endregion
}
