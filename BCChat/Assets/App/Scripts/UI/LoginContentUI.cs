using BrainCloud;
using BrainCloud.Common;
using BrainCloud.JSONHelper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginContentUI : ContentUIBehaviour
{
    // User Input Restrictions
    private const int MINIMUM_USERNAME_LENGTH = 4;
    private const int MINIMUM_PASSWORD_LENGTH = 6;

    private static string PREFS_REMEMBER_ME => BCManager.AppName + ".rememberMe";

    [Header("Main")]
    [SerializeField] private TMP_InputField UsernameField = default;
    [SerializeField] private TMP_InputField PasswordField = default;
    [SerializeField] private Toggle RememberMeToggle = default;
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private TMP_Text ErrorLabel = default;

    [Header("Navigation")]
    [SerializeField] private MainContentUI MainContent = default;

    #region Unity Messages

    protected override void Awake()
    {
        UsernameField.text = string.Empty;
        PasswordField.text = string.Empty;
        ErrorLabel.text = string.Empty;

        InitRememberMePref();

        base.Awake();
    }

    private void OnEnable()
    {
        UsernameField.onEndEdit.AddListener((username) => CheckUsernameVerification(username));
        PasswordField.onEndEdit.AddListener((password) => CheckPasswordVerification(password));
        LoginButton.onClick.AddListener(OnLoginButton);
    }

    protected override void Start()
    {
        if (GetRememberMePref() && !UserHandler.AnonymousID.IsEmpty())
        {
            RememberMeToggle.isOn = true;
            HandleAutomaticLogin();
        }
        else
        {
            RememberMeToggle.isOn = false;
            SetRememberMePref(false);
        }

        base.Start();
    }

    private void OnDisable()
    {
        UsernameField.onEndEdit.RemoveAllListeners();
        PasswordField.onEndEdit.RemoveAllListeners();
        RememberMeToggle.onValueChanged.RemoveAllListeners();
        LoginButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region UI

    public void InitRememberMePref()
    {
        if (!PlayerPrefs.HasKey(PREFS_REMEMBER_ME))
        {
            PlayerPrefs.SetInt(PREFS_REMEMBER_ME, 0);
        }
    }

    public bool GetRememberMePref() =>
        PlayerPrefs.GetInt(PREFS_REMEMBER_ME) > 0;

    public void SetRememberMePref(bool value) =>
        PlayerPrefs.SetInt(PREFS_REMEMBER_ME, value ? int.MaxValue : 0);

    public void DisplayError(string error, Selectable problemSelectable = null)
    {
        if (problemSelectable != null)
        {
            problemSelectable.DisplayError();
        }

        ErrorLabel.text = error;
    }

    protected override void InitializeUI()
    {
        UsernameField.text = string.Empty;
        UsernameField.DisplayNormal();
        PasswordField.text = string.Empty;
        PasswordField.DisplayNormal();
        ErrorLabel.text = string.Empty;
    }

    private bool CheckUsernameVerification(string value)
    {
        UsernameField.text = value.Trim();
        if (!UsernameField.text.IsEmpty())
        {
            ErrorLabel.text = string.Empty;
            if (UsernameField.text.Length < MINIMUM_USERNAME_LENGTH)
            {
                DisplayError($"Please use a username with at least {MINIMUM_USERNAME_LENGTH} characters.", UsernameField);
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CheckPasswordVerification(string value)
    {
        PasswordField.text = value.Trim();
        if (!PasswordField.text.IsEmpty())
        {
            ErrorLabel.text = string.Empty;
            if (PasswordField.text.Length < MINIMUM_PASSWORD_LENGTH)
            {
                DisplayError($"Please use a password with at least {MINIMUM_PASSWORD_LENGTH} characters.", PasswordField);
                return false;
            }

            return true;
        }

        return false;
    }

    private void OnLoginButton()
    {
        string inputUser = UsernameField.text;
        string inputPassword = PasswordField.text;

        ErrorLabel.text = string.Empty;
        if (!CheckPasswordVerification(inputPassword) || !CheckUsernameVerification(inputUser))
        {
            if (inputUser.IsEmpty())
            {
                DisplayError("Please use a valid username.", UsernameField);
            }

            if (inputPassword.IsEmpty())
            {
                DisplayError("Please use a valid password.", PasswordField);
            }

            return;
        }

        IsInteractable = false;

        UserHandler.AuthenticateUniversal(inputUser, inputPassword, true,
                                          OnSuccess("Authentication Success", OnAuthenticationSuccess),
                                          OnFailure("Authentication Failed", OnAuthenticationFailure));
    }

    #endregion

    #region brainCloud

    private void HandleAutomaticLogin()
    {
        IsInteractable = false;

        FailureCallback onFailure = OnFailure("Automatic Login Failed", () =>
        {
            IsInteractable = true;
        
            DisplayError("Automatic Login Failed\nPlease try logging in manually.");
        
            RememberMeToggle.isOn = false;
            SetRememberMePref(false);
        });
        
        UserHandler.HandleUserReconnect(OnSuccess("Automatically Logging In...", OnAuthenticationSuccess), onFailure);
    }

    private void OnAuthenticationSuccess()
    {
        SetRememberMePref(RememberMeToggle.isOn);

        BCManager.Wrapper.SetStoredAuthenticationType(AuthenticationType.Universal.ToString());

        BCManager.IdentityService.GetIdentities(OnSuccess("Get Identities Success", OnGetIdentitiesSuccess),
                                                OnFailure("Get Identities Failed", OnAuthenticationFailure));
    }

    private void OnGetIdentitiesSuccess(string response)
    {
        var data = response.Deserialize("data", "identities");

        UserHandler.AnonymousUser = data.Count <= 0;

        MainContent.gameObject.SetActive(true);
        MainContent.ResetUI();

        RememberMeToggle.isOn = false;
        gameObject.SetActive(false);
    }

    private void OnAuthenticationFailure()
    {
        IsInteractable = true;

        PasswordField.text = string.Empty;

        DisplayError("Authentication Failed\nPlease check your username and password and try again.");
    }

    #endregion
}
