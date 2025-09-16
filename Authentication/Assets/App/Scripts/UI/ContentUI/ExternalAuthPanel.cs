using BrainCloud.Common;
using BrainCloud.JSONHelper;
using System.Collections.Generic;
using UnityEngine;

#if FACEBOOK_SDK
using Facebook.Unity;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif
#endif

#if GOOGLE_SDK
using GooglePlayGames;
#endif

#if GOOGLE_OPENID_SDK
using Google;
#endif

#if APPLE_SDK
using AppleAuth;
#endif

/// <summary>
/// A login panel to hold buttons for multiple external authentication methods.
/// </summary>
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

    protected override void Awake()
    {
        base.Awake();

#if GOOGLE_SDK && GOOGLE_OPENID_SDK && UNITY_ANDROID
        Debug.LogError("Google Play Games & Google OpenID are not supported at the same time! This may lead to undesirable results...");
#endif
    }

    protected override void Start()
    {
        HashSet<string> authentications = new()
        {
#if APPLE_SDK && (UNITY_STANDALONE_OSX || UNITY_IOS)
            AuthenticationType.Apple.ToString(),
#endif
#if GAMECENTER_SDK && (UNITY_STANDALONE_OSX || UNITY_IOS)
            AuthenticationType.GameCenter.ToString(),
#endif
#if FACEBOOK_SDK && (UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID)
            AuthenticationType.Facebook.ToString(),
#elif FACEBOOK_SDK && UNITY_IOS
            AuthenticationType.Facebook.ToString(),
            AuthenticationType.FacebookLimited.ToString(),
#endif
#if GOOGLE_SDK && UNITY_ANDROID
            AuthenticationType.Google.ToString(),
#endif
#if GOOGLE_OPENID_SDK && (UNITY_ANDROID || UNITY_IOS)
            AuthenticationType.GoogleOpenId.ToString(),
#endif
        };

        authButtons = new List<ButtonContent>();

        foreach (ExternalAuthItem authItem in AuthItems)
        {
            if (authItem.AuthenticationType == AuthenticationType.Unknown ||
                !authentications.Contains(authItem.AuthenticationType.ToString()))
            {
                continue;
            }

            ButtonContent button = Instantiate(ButtonTemplate, ButtonContent);
            button.gameObject.SetActive(true);
            button.gameObject.SetName("{0}MenuItem", authItem.Name);
            button.Label = authItem.Name;
            button.LeftIcon = authItem.Icon;
            button.LabelColor = authItem.LabelColor;
            button.LeftIconColor = authItem.IconColor;
            button.BackgroundColor = authItem.BackgroundColor;
            button.Button.onClick.AddListener(() => OnExternalAuthentication(authItem.AuthenticationType));

            authButtons.Add(button);
        }

        if (authButtons.Count == 0)
        {
            gameObject.SetActive(false);
            return;
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
#if UNITY_IOS
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
            ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking();
        }
#endif
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

#if GOOGLE_SDK
        PlayGamesPlatform.DebugLogEnabled = true;
#endif
    }

#if FACEBOOK_SDK
    private void HandleFacebookAuthenticationButton()
    {
#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_ANDROID
        selectedAuthenticationType = AuthenticationType.Facebook;
        UserHandler.AuthenticateFacebook(true,
                                         OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                         OnFailure("Authentication Failed", OnAuthenticationFailure));
#elif UNITY_IOS
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED)
        {
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
        }
        else // If user Denies or Blocks Tracking then we default to FacebookLimited
        {
            selectedAuthenticationType = AuthenticationType.FacebookLimited;
            UserHandler.AuthenticateFacebookLimited(true,
                                                    OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                                    OnFailure("Authentication Failed", OnAuthenticationFailure));
        }
#endif
    }
#endif

    private void OnExternalAuthentication(AuthenticationType type)
    {
        LoginContent.IsInteractable = false;
        selectedAuthenticationType = type;

        if (type == AuthenticationType.Apple)
        {
#if APPLE_SDK
            UserHandler.AuthenticateApple(true,
                                          OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                          OnFailure("Authentication Failed", OnAuthenticationFailure));
            return;
#endif
        }
        else if (type == AuthenticationType.GameCenter)
        {
#if GAMECENTER_SDK
            UserHandler.AuthenticateGameCenter(true,
                                               OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                               OnFailure("Authentication Failed", OnAuthenticationFailure));
            return;
#endif
        }
        else if (type == AuthenticationType.Facebook ||
                 type == AuthenticationType.FacebookLimited)
        {
#if FACEBOOK_SDK
            HandleFacebookAuthenticationButton();
            return;
#endif
        }
        else if (type == AuthenticationType.Google)
        {
#if GOOGLE_SDK
            UserHandler.AuthenticateGoogle(true,
                                           OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                           OnFailure("Authentication Failed", OnAuthenticationFailure));
            return;
#endif
        }
        else if (type == AuthenticationType.GoogleOpenId)
        {
#if GOOGLE_OPENID_SDK
            UserHandler.AuthenticateGoogleOpenId(true,
                                                 OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                                 OnFailure("Authentication Failed", OnAuthenticationFailure));
            return;
#endif
        }

        Debug.LogError($"Authentication method is either unavailable on this platform or is unknown: {type}");
        selectedAuthenticationType = AuthenticationType.Unknown;
        LoginContent.IsInteractable = true;
    }

    private void OnAuthenticationSuccess()
    {
        BCManager.IdentityService.GetIdentities(OnSuccess("Get Identities Success", OnGetIdentitiesSuccess),
                                                OnFailure("Get Identities Failed", OnAuthenticationFailure));
    }

    private void OnGetIdentitiesSuccess(string response)
    {
        var data = response.Deserialize("data", "identities");

        UserHandler.AnonymousUser = data.Count <= 0;

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
#if GOOGLE_OPENID_SDK
        GoogleSignIn.DefaultInstance.Disconnect();
#endif
        Popup.DisplayPopup(new PopupInfo("Could not Authenticate",
                                         new PopupInfoBody[] { new PopupInfoBody(response.Message, PopupInfoBody.Type.Centered) },
                                         null, true, "Close"));

        selectedAuthenticationType = AuthenticationType.Unknown;
        LoginContent.IsInteractable = true;
    }

    #endregion
}
