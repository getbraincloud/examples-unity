using System;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Authentication : MonoBehaviour
{
    private const int MINIMUM_USERNAME_LENGTH = 4;
    private const int MINIMUM_PASSWORD_LENGTH = 6;
    private const string NO_COPY = "[nc]";
    private const string PROFILE_ID_FORMAT = "Profile ID: {0}";
    private const string ANONYMOUS_ID_FORMAT = "Anonymous ID: {0}";
    private const string APP_INFO_FORMAT = "{0} ({1}) v{2}";
    private const string BC_VERSION_FORMAT = "brainCloud v{0}";

    [SerializeField] private CanvasGroup MainCG = default;

    [Header("Toggle Group")]
    [SerializeField] private CanvasGroup ToggleCG = default;
    [SerializeField] private Toggle EmailRadio = default;
    [SerializeField] private Toggle UniversalRadio = default;
    [SerializeField] private Toggle AnonymousRadio = default;

    [Header("Email Group")]
    [SerializeField] private CanvasGroup EmailCG = default;
    [SerializeField] private TMP_InputField EmailInputField = default;
    [SerializeField] private TMP_InputField EmailPasswordField = default;

    [Header("Universal Group")]
    [SerializeField] private CanvasGroup UniversalCG = default;
    [SerializeField] private TMP_InputField UserInputField = default;
    [SerializeField] private TMP_InputField UserPasswordField = default;

    [Header("Login/Logout")]
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private Button LogoutButton = default;

    [Header("User Info")]
    [SerializeField] private TMP_Text ProfileIDLabel = default;
    [SerializeField] private TMP_Text AnonIDLabel = default;

    [Header("App Info")]
    [SerializeField] private TMP_Text AppInfoLabel = default;
    [SerializeField] private TMP_Text VersionInfoLabel = default;

    private BrainCloudWrapper BC = null;

    #region Unity Messages

    private void Awake()
    {
        // Init BrainCloudWrapper
        BC = gameObject.GetComponent<BrainCloudWrapper>();
        BC.WrapperName = Application.productName;
        BC.Init();

        EmailInputField.text = string.Empty;
        EmailPasswordField.text = string.Empty;
        UserInputField.text = string.Empty;
        UserPasswordField.text = string.Empty;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EmailRadio.onValueChanged.AddListener(OnEmailRadio);
        UniversalRadio.onValueChanged.AddListener(OnUniversalRadio);
        LoginButton.onClick.AddListener(OnLoginButton);
        LogoutButton.onClick.AddListener(OnLogoutButton);
    }

    private void Start()
    {
        // Set default radio
        AuthenticationType authenticationType = AuthenticationType.FromString(BC.GetStoredAuthenticationType());
        if (authenticationType == AuthenticationType.Universal)
        {
            UniversalRadio.isOn = true;
            OnEmailRadio(false);
            OnUniversalRadio(true);
        }
        else if (authenticationType == AuthenticationType.Anonymous)
        {
            AnonymousRadio.isOn = true;
            OnEmailRadio(false);
            OnUniversalRadio(false);
        }
        else // Email is default
        {
            EmailRadio.isOn = true;
            OnEmailRadio(true);
            OnUniversalRadio(false);
        }

        // Disable login button
        LogoutButton.gameObject.SetActive(false);

        AppInfoLabel.text = string.Format(APP_INFO_FORMAT, BC.WrapperName, BC.Client.AppId, BC.Client.AppVersion);
        VersionInfoLabel.text = string.Format(BC_VERSION_FORMAT, BC.Client.BrainCloudClientVersion);

        // Do BC reconnect if there is a stored profile ID and anonymous ID
        if (GetStoredUserIDs() &&
            (authenticationType == AuthenticationType.Email ||
             authenticationType == AuthenticationType.Universal ||
             authenticationType == AuthenticationType.Anonymous))
        {
            HandleAutomaticLogin();
        }
    }

    private void OnDisable()
    {
        EmailRadio.onValueChanged.RemoveAllListeners();
        UniversalRadio.onValueChanged.RemoveAllListeners();
        LoginButton.onClick.RemoveAllListeners();
        LogoutButton.onClick.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        BC = null;
    }

    #endregion

    #region UI

    private void OnEmailRadio(bool value)
    {
        EmailCG.interactable = value;
        EmailCG.gameObject.SetActive(value);
    }

    private void OnUniversalRadio(bool value)
    {
        UniversalCG.interactable = value;
        UniversalCG.gameObject.SetActive(value);
    }

    private bool CheckEmailVerification(string value)
    {
        EmailInputField.text = value.Trim();
        if (!string.IsNullOrWhiteSpace(EmailInputField.text))
        {
            try
            {
                // Use MailAddress to validate the email address
                // NOTE: This is NOT a guaranteed way to validate an email address and this will NOT ping the email to
                // validate that it is a real email address. brainCloud DOES NOT validate this upon registration as well.
                // You should implement your own method of verificaiton.
                System.Net.Mail.MailAddress validate = new(EmailInputField.text);

                string user = validate.User;
                string host = validate.Host;
                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(host) ||
                    !host.Contains('.') || host.StartsWith('.') || host.EndsWith('.'))
                {
                    Debug.LogError($"{NO_COPY}Please use a valid email address.");
                    return false;
                }
            }
            catch
            {
                Debug.LogError($"{NO_COPY}Please use a valid email address.");
                return false;
            }

            return true;
        }

        Debug.LogError($"{NO_COPY}Please use a valid email address.");

        return false;
    }

    private bool CheckUsernameVerification(string value)
    {
        UserInputField.text = value.Trim();
        if (!string.IsNullOrWhiteSpace(UserInputField.text))
        {
            if (UserInputField.text.Length < MINIMUM_USERNAME_LENGTH)
            {
                Debug.LogError($"{NO_COPY}Please use a username with at least {MINIMUM_USERNAME_LENGTH} characters.");
                return false;
            }

            return true;
        }

        Debug.LogError($"{NO_COPY}Please use a valid username.");

        return false;
    }

    private bool CheckPasswordVerification(TMP_InputField passwordField, string value)
    {
        if (passwordField == null)
        {
            Debug.LogError($"{NO_COPY}Cannot verify which password field. Is Anonymous toggle on?");
            return false;
        }

        passwordField.text = value.Trim();
        if (!string.IsNullOrWhiteSpace(passwordField.text))
        {
            if (passwordField.text.Length < MINIMUM_PASSWORD_LENGTH)
            {
                Debug.LogError($"{NO_COPY}Please use a password with at least {MINIMUM_PASSWORD_LENGTH} characters.");
                return false;
            }

            return true;
        }

        Debug.LogError($"{NO_COPY}Please use a valid password.");

        return false;
    }

    public bool GetStoredUserIDs()
    {
        bool canDoReconnect = false;

        // Set Profile ID info
        if (!string.IsNullOrWhiteSpace(BC.GetStoredProfileId()))
        {
            ProfileIDLabel.text = string.Format(PROFILE_ID_FORMAT, BC.GetStoredProfileId());
            canDoReconnect = true;
        }
        else
        {
            ProfileIDLabel.text = string.Format(PROFILE_ID_FORMAT, "---");
        }

        // Set Anonymous ID info
        if (!string.IsNullOrWhiteSpace(BC.GetStoredAnonymousId()))
        {
            AnonIDLabel.text = string.Format(ANONYMOUS_ID_FORMAT, BC.GetStoredAnonymousId());
        }
        else
        {
            AnonIDLabel.text = string.Format(ANONYMOUS_ID_FORMAT, "---");
            canDoReconnect = false;
        }

        return canDoReconnect;
    }

    private void OnLoginButton()
    {
        string inputUser = EmailRadio.isOn ? EmailInputField.text : UniversalRadio.isOn ? UserInputField.text : string.Empty;
        string inputPassword = EmailRadio.isOn ? EmailPasswordField.text : UniversalRadio.isOn ? UserPasswordField.text : string.Empty;

        if (!AnonymousRadio.isOn)
        {
            bool cancel = false;
            if (EmailRadio.isOn && !CheckEmailVerification(inputUser))
            {
                cancel = true;
            }
            else if (UniversalRadio.isOn && !CheckUsernameVerification(inputUser))
            {
                cancel = true;
            }

            if (!CheckPasswordVerification(EmailRadio.isOn ? EmailPasswordField :
                                           UniversalRadio.isOn ? UserPasswordField : null,
                                           inputPassword))
            {
                cancel = true;
            }

            if (cancel)
            {
                return;
            }
        }

        EmailPasswordField.text = string.Empty;
        UserPasswordField.text = string.Empty;

        MainCG.interactable = false;

        Debug.Log($"{NO_COPY}Attempting to authenticate...");

        if (EmailRadio.isOn)
        {
            BC.AuthenticateEmailPassword(inputUser, inputPassword, true, OnAuthenticationSuccess, OnAuthenticationFailure, this);
        }
        else if (UniversalRadio.isOn)
        {
            BC.AuthenticateUniversal(inputUser, inputPassword, true, OnAuthenticationSuccess, OnAuthenticationFailure, this);
        }
        else // Anonymous login
        {
            if (GetStoredUserIDs())
            {
                HandleAutomaticLogin();
            }
            else
            {
                BC.AuthenticateAnonymous(OnAuthenticationSuccess, OnAuthenticationFailure, this);
            }
        }
    }

    private void OnLogoutButton()
    {
        MainCG.interactable = false;

        SuccessCallback onSuccess = (_, _) =>
        {
            Debug.Log($"{NO_COPY}Logout success!");

            EmailInputField.text = string.Empty;
            EmailPasswordField.text = string.Empty;
            UserInputField.text = string.Empty;
            UserPasswordField.text = string.Empty;

            ToggleCG.interactable = true;
            LoginButton.gameObject.SetActive(true);
            LogoutButton.gameObject.SetActive(false);

            OnEmailRadio(AuthenticationType.FromString(BC.GetStoredAuthenticationType()) == AuthenticationType.Email);
            OnUniversalRadio(AuthenticationType.FromString(BC.GetStoredAuthenticationType()) == AuthenticationType.Universal);

            BC.ResetStoredAuthenticationType();
            GetStoredUserIDs();

            MainCG.interactable = true;
        };

        FailureCallback onFailure = (_, _, _, _) =>
        {
            Debug.LogError($"{NO_COPY}Logout failed!");
            Debug.LogError($"{NO_COPY}Try restarting the app...");

            BC.ResetStoredAuthenticationType();
            GetStoredUserIDs();
        };

        BC.PlayerStateService.Logout(onSuccess, onFailure, this);
    }

    #endregion

    #region brainCloud

    public void HandleAutomaticLogin()
    {
        MainCG.interactable = false;

        FailureCallback onFailure = (_, _, _, _) =>
        {
            MainCG.interactable = true;

            Debug.LogError($"{NO_COPY}Automatic login failed. Please try logging in manually.");
        };

        Debug.Log($"{NO_COPY}Performing automatic authentication...");

        BC.Reconnect(OnAuthenticationSuccess, onFailure, this);
    }

    private void OnAuthenticationSuccess(string jsonResponse, object cbObject)
    {
        BC.SetStoredAuthenticationType(EmailRadio.isOn ? AuthenticationType.Email.ToString() :
                                              UniversalRadio.isOn ? AuthenticationType.Universal.ToString() :
                                              AuthenticationType.Anonymous.ToString());

        ToggleCG.interactable = false;
        EmailCG.interactable = false;
        UniversalCG.interactable = false;
        LoginButton.gameObject.SetActive(false);
        LogoutButton.gameObject.SetActive(true);
        MainCG.interactable = true;

        GetStoredUserIDs();
        Debug.Log($"User Profile ID: {BC.GetStoredProfileId()}");
        Debug.Log($"User Anonymous ID: {BC.GetStoredAnonymousId()}");
        Debug.Log($"Authentication Method: {BC.GetStoredAuthenticationType()}");

        // Deserialize jsonResponse
        var data = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse)["data"] as Dictionary<string, object>;

        // Properties to potentially store
        var newUser = data["newUser"];
        var sessionId = (string)data["sessionId"];
        var loginCount = data["loginCount"].GetType() == typeof(long) ? (long)data["loginCount"]
                                                                      : (int)data["loginCount"];
        var expiryTime = data["playerSessionExpiry"].GetType() == typeof(long) ? TimeSpan.FromSeconds((long)data["playerSessionExpiry"])
                                                                               : TimeSpan.FromSeconds((int)data["playerSessionExpiry"]);

        Debug.Log("Deserializing some properties:\n" +
                  $"  Are they a new user? {newUser}\n" +
                  $"  User Session ID: {sessionId}\n" +
                  $"  Login Counts: {loginCount}\n" +
                  $"  Session Expiry Time: {expiryTime.TotalSeconds} seconds");

        Debug.Log($"{NO_COPY}Authentication success! You are now logged into your app on brainCloud.");
    }

    private void OnAuthenticationFailure(int status, int reason, string jsonError, object cbObject)
    {
        BC.ResetStoredAuthenticationType();
        GetStoredUserIDs();

        // Deserialize jsonError
        var error = JsonReader.Deserialize<Dictionary<string, object>>(jsonError);
        var message = (string)error["status_message"];

        Debug.LogError($"Status: {status} | Reason: {reason} | Message:\n  {message}");

        Debug.LogError($"{NO_COPY}Authentication failed! Please try again.");

        MainCG.interactable = true;
    }

    #endregion
}
