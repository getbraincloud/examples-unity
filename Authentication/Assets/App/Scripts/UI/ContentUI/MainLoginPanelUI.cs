using BrainCloud;
using BrainCloud.Common;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <para>
/// An example on how User Authentication can he handled in your app.
/// </para>
///
/// <seealso cref="BCManager"/><br></br>
/// <seealso cref="UserHandler"/>
/// </summary>
public class MainLoginPanelUI : ContentUIBehaviour
{
    // User Input Restrictions
    private const int MINIMUM_USERNAME_LENGTH = 4;
    private const int MINIMUM_PASSWORD_LENGTH = 6;
    private const int MINIMUM_REGISTRATION_NAME_LENGTH = 3;
    private const int MINIMUM_REGISTRATION_AGE = 13;
    private const int MAXIMUM_REGISTRATION_AGE = 120;

    private static string PREFS_REMEMBER_ME => BCManager.AppName + ".rememberMe";

    [Header("Main")]
    [SerializeField] private Toggle EmailRadio = default;
    [SerializeField] private Toggle UniversalRadio = default;
    [SerializeField] private Toggle AnonymousRadio = default;
    [SerializeField] private TMP_InputField EmailField = default;
    [SerializeField] private TMP_InputField UsernameField = default;
    [SerializeField] private TMP_InputField PasswordField = default;
    [SerializeField] private Toggle RememberMeToggle = default;
    [SerializeField] private TMP_Text ErrorLabel = default;
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;

    [Header("UI Control")]
    [SerializeField] private MainMenuUI MainMenu = default;
    [SerializeField] private LoginContentUI LoginContent = default;

    #region Unity Messages

    protected override void Awake()
    {
        EmailField.text = string.Empty;
        UsernameField.text = string.Empty;
        PasswordField.text = string.Empty;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;
        ErrorLabel.text = string.Empty;

        InitRememberMePref();

        base.Awake();
    }

    private void OnEnable()
    {
        EmailRadio.onValueChanged.AddListener(OnEmailRadio);
        UniversalRadio.onValueChanged.AddListener(OnUniversalRadio);
        AnonymousRadio.onValueChanged.AddListener(OnAnonymousRadio);
        EmailField.onEndEdit.AddListener((email) => CheckEmailVerification(email));
        UsernameField.onEndEdit.AddListener((username) => CheckUsernameVerification(username));
        PasswordField.onEndEdit.AddListener((password) => CheckPasswordVerification(password));
        LoginButton.onClick.AddListener(OnLoginButton);
        NameField.onEndEdit.AddListener((name) => CheckNameVerification(name));
        AgeField.onEndEdit.AddListener((age) => CheckAgeVerification(age));
    }

    protected override void Start()
    {
        bool rememberUserToggle = GetRememberMePref();
        RememberMeToggle.isOn = rememberUserToggle && !UserHandler.AnonymousID.IsEmpty();

        AuthenticationType authenticationType = UserHandler.AuthenticationType;
        if (authenticationType == AuthenticationType.Universal)
        {
            UniversalRadio.isOn = true;
            OnUniversalRadio(true);
        }
        else if (authenticationType == AuthenticationType.Anonymous)
        {
            AnonymousRadio.isOn = true;
            OnEmailRadio(true);
            OnAnonymousRadio(true);
        }
        else // Email is default
        {
            EmailRadio.isOn = true;
            OnEmailRadio(true);
        }

        if (RememberMeToggle.isOn)
        {
            HandleAutomaticLogin();
        }
        else if (rememberUserToggle && !UserHandler.AnonymousID.IsEmpty())
        {
            SetRememberMePref(false);
        }

        base.Start();
    }

    private void OnDisable()
    {
        EmailRadio.onValueChanged.RemoveAllListeners();
        UniversalRadio.onValueChanged.RemoveAllListeners();
        AnonymousRadio.onValueChanged.RemoveAllListeners();
        EmailField.onEndEdit.RemoveAllListeners();
        UsernameField.onEndEdit.RemoveAllListeners();
        PasswordField.onEndEdit.RemoveAllListeners();
        RememberMeToggle.onValueChanged.RemoveAllListeners();
        LoginButton.onClick.RemoveAllListeners();
        NameField.onEndEdit.RemoveAllListeners();
        AgeField.onEndEdit.RemoveAllListeners();
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
        EmailField.text = string.Empty;
        EmailField.DisplayNormal();
        UsernameField.text = string.Empty;
        UsernameField.DisplayNormal();
        PasswordField.text = string.Empty;
        PasswordField.DisplayNormal();
        NameField.text = string.Empty;
        NameField.DisplayNormal();
        AgeField.text = string.Empty;
        AgeField.DisplayNormal();
        ErrorLabel.text = string.Empty;
    }

    private void OnEmailRadio(bool value)
    {
        if (value)
        {
            InitializeUI();
            EmailField.gameObject.SetActive(true);
            UsernameField.gameObject.SetActive(false);
        }
    }

    private void OnUniversalRadio(bool value)
    {
        if (value)
        {
            InitializeUI();
            UsernameField.gameObject.SetActive(true);
            EmailField.gameObject.SetActive(false);
        }
    }

    private void OnAnonymousRadio(bool value)
    {
        if (value)
        {
            InitializeUI();
        }

        EmailField.interactable = !value;
        UsernameField.interactable = !value;
        PasswordField.interactable = !value;
    }

    private bool CheckEmailVerification(string value)
    {
        EmailField.text = value.Trim();
        if (!EmailField.text.IsEmpty())
        {
            ErrorLabel.text = string.Empty;

            try
            {
                // Use MailAddress to validate the email address
                // NOTE: This is NOT a guaranteed way to validate an email address and this will NOT ping the email to
                // validate that it is a real email address. brainCloud DOES NOT validate this upon registration as well.
                // You should implement your own method of verificaiton.
                MailAddress validate = new MailAddress(EmailField.text);

                string user = validate.User;
                string host = validate.Host;
                if (user.IsEmpty() || host.IsEmpty() ||
                    !host.Contains('.') || host.StartsWith('.') || host.EndsWith('.'))
                {
                    DisplayError("Please use a valid email address.", EmailField);
                    return false;
                }
            }
            catch
            {
                DisplayError("Please use a valid email address.", EmailField);
                return false;
            }

            return true;
        }

        return false;
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

    private bool CheckNameVerification(string value)
    {
        NameField.text = value.Trim();
        if (!NameField.text.IsEmpty())
        {
            ErrorLabel.text = string.Empty;
            if (NameField.text.Length < MINIMUM_REGISTRATION_NAME_LENGTH)
            {
                DisplayError($"Please register with a name with at least {MINIMUM_REGISTRATION_NAME_LENGTH} characters.", NameField);
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CheckAgeVerification(string value)
    {
        AgeField.text = value.Trim();
        if (!AgeField.text.IsEmpty())
        {
            ErrorLabel.text = string.Empty;
            if (int.TryParse(AgeField.text, out int result))
            {
                if (result < MINIMUM_REGISTRATION_AGE)
                {
                    AgeField.text = result < 0 ? 0.ToString() : AgeField.text;
                    DisplayError($"You must be at least {MINIMUM_REGISTRATION_AGE} years old to register.", AgeField);
                    return false;
                }
                else if (result > MAXIMUM_REGISTRATION_AGE)
                {
                    DisplayError("Please register with a valid age.", AgeField);
                    return false;
                }

                return true;
            }

            DisplayError("Please register with a valid age.", AgeField);
        }

        return false;
    }

    private void OnLoginButton()
    {
        AuthenticationIds ids;
        string inputEmail = EmailField.text;
        string inputUser = UsernameField.text;
        string inputPassword = PasswordField.text;
        string inputName = NameField.text;
        string inputAge = AgeField.text;

        ErrorLabel.text = string.Empty;
        if (!AnonymousRadio.isOn && (!CheckPasswordVerification(inputPassword) ||
            !(CheckEmailVerification(inputEmail) || CheckUsernameVerification(inputUser))))
        {
            if (EmailRadio.isOn && inputEmail.IsEmpty())
            {
                DisplayError("Please use a valid email address.", EmailField);
            }
            else if (UniversalRadio.isOn && inputUser.IsEmpty())
            {
                DisplayError("Please use a valid username.", UsernameField);
            }

            if (inputPassword.IsEmpty())
            {
                DisplayError("Please use a valid password.", PasswordField);
            }

            return;
        }

        // Advanced Authentication if Name & Age were inputted
        if (CheckNameVerification(inputName) && CheckAgeVerification(inputAge))
        {
            AuthenticationType authenticationType;
            Dictionary<string, object> extraJson = new Dictionary<string, object>();
            extraJson["name"] = inputName;
            extraJson["age"] = inputAge;

            if (AnonymousRadio.isOn)
            {
                authenticationType = AuthenticationType.Anonymous;
                ids.externalId = UserHandler.AnonymousID;
                ids.authenticationToken = "";
                ids.authenticationSubType = "";
            }
            else
            {
                authenticationType = EmailRadio.isOn ? AuthenticationType.Email : AuthenticationType.Universal;
                ids.externalId = EmailRadio.isOn ? inputEmail : inputUser;
                ids.authenticationToken = inputPassword;
                ids.authenticationSubType = "";
            }

            LoginContent.IsInteractable = false;
            UserHandler.AuthenticateAdvanced(authenticationType, ids, extraJson, OnAuthenticationSuccess, OnAuthenticationFailure);

            return;
        }
        else if (!inputName.IsEmpty() || !inputAge.IsEmpty())
        {
            NameField.DisplayError();
            AgeField.DisplayError();
            DisplayError("Please register with both a valid name and age.");
            return;
        }

        LoginContent.IsInteractable = false;

        if (EmailRadio.isOn)
        {
            UserHandler.AuthenticateEmail(inputEmail, inputPassword, OnAuthenticationSuccess, OnAuthenticationFailure);
        }
        else if (UniversalRadio.isOn)
        {
            UserHandler.AuthenticateUniversal(inputUser, inputPassword, OnAuthenticationSuccess, OnAuthenticationFailure);
        }
        else // Anonymous login
        {
            UserHandler.AuthenticateAnonymous(OnAuthenticationSuccess, OnAuthenticationFailure);
        }
    }

    #endregion

    #region brainCloud

    private void HandleAutomaticLogin()
    {
        Debug.Log("Logging in automatically...");

        LoginContent.IsInteractable = false;

        UserHandler.HandleUserReconnect(() =>
        {
            RememberMeToggle.isOn = false;

            MainMenu.ChangeToAppContent();
        },
        () =>
        {
            LoginContent.IsInteractable = true;

            DisplayError("Automatic Login Failed\nPlease try logging in manually.");

            RememberMeToggle.isOn = false;
            SetRememberMePref(false);
        });
    }

    private void OnAuthenticationSuccess()
    {
        if (EmailRadio.isOn || UniversalRadio.isOn)
        {
            BCManager.Wrapper.SetStoredAuthenticationType(EmailRadio.isOn ? AuthenticationType.Email.ToString()
                                                                          : AuthenticationType.Universal.ToString());
        }
        else if (AnonymousRadio.isOn)
        {
            BCManager.Wrapper.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());
        }

        SetRememberMePref(RememberMeToggle.isOn);

        RememberMeToggle.isOn = false;
        InitializeUI();

        MainMenu.ChangeToAppContent();
    }

    private void OnAuthenticationFailure()
    {
        LoginContent.IsInteractable = true;

        string errorMessage;
        if (EmailRadio.isOn)
        {
            errorMessage = "Authentication Failed\nPlease check your email and password and try again.";
        }
        else if (UniversalRadio.isOn)
        {
            errorMessage = "Authentication Failed\nPlease check your username and password and try again.";
        }
        else
        {
            errorMessage = "Authentication Failed\nPlease try again in a few moments.";
        }

        PasswordField.text = string.Empty;
        DisplayError(errorMessage);
    }

    #endregion
}
