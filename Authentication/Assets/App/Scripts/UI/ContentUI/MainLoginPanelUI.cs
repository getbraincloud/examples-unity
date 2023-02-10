using BrainCloud;
using BrainCloud.Common;
using System.Collections.Generic;
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
public class MainLoginPanelUI : MonoBehaviour, IContentUI
{
    private static string PREFS_REMEMBER_ME => BCManager.AppName + ".rememberMe";

    [Header("Main")]
    [SerializeField] private CanvasGroup UICanvasGroup = default;
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

    #region IContentUI

    public bool IsInteractable
    {
        get { return UICanvasGroup.interactable; }
        set { UICanvasGroup.interactable = value; }
    }

    public float Opacity
    {
        get { return UICanvasGroup.alpha; }
        set { UICanvasGroup.alpha = value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value; }
    }

    public GameObject GameObject => gameObject;

    public Transform Transform => transform;

    #endregion

    #region Unity Messages

    private void Awake()
    {
        EmailField.text = string.Empty;
        UsernameField.text = string.Empty;
        PasswordField.text = string.Empty;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;
        ErrorLabel.text = string.Empty;

        InitRememberMePref();
    }

    private void OnEnable()
    {
        EmailRadio.onValueChanged.AddListener(OnEmailRadio);
        UniversalRadio.onValueChanged.AddListener(OnUniversalRadio);
        AnonymousRadio.onValueChanged.AddListener(OnAnonymousRadio);
        //EmailField.onValueChanged.AddListener();
        //UsernameField.onValueChanged.AddListener();
        //PasswordField.onValueChanged.AddListener();
        LoginButton.onClick.AddListener(OnLoginButton);
        //NameField.onValueChanged.AddListener();
        //AgeField.onValueChanged.AddListener();
    }

    private void Start()
    {
        bool rememberUserToggle = GetRememberMePref();
        RememberMeToggle.isOn = rememberUserToggle && !UserHandler.AnonymousID.IsNullOrEmpty();

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
            OnAutomaticLogin();
        }
        else if (rememberUserToggle && !UserHandler.AnonymousID.IsNullOrEmpty())
        {
            SetRememberMePref(false);
        }
    }

    private void OnDisable()
    {
        EmailRadio.onValueChanged.RemoveAllListeners();
        UniversalRadio.onValueChanged.RemoveAllListeners();
        AnonymousRadio.onValueChanged.RemoveAllListeners();
        EmailField.onValueChanged.RemoveAllListeners();
        UsernameField.onValueChanged.RemoveAllListeners();
        PasswordField.onValueChanged.RemoveAllListeners();
        RememberMeToggle.onValueChanged.RemoveAllListeners();
        LoginButton.onClick.RemoveAllListeners();
        NameField.onValueChanged.RemoveAllListeners();
        AgeField.onValueChanged.RemoveAllListeners();
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

    public bool GetRememberMePref()
    {
        return PlayerPrefs.GetInt(PREFS_REMEMBER_ME) > 0;
    }

    public void SetRememberMePref(bool value)
    {
        PlayerPrefs.SetInt(PREFS_REMEMBER_ME, value ? int.MaxValue : 0);
    }

    public void ClearFields()
    {
        EmailField.text = string.Empty;
        UsernameField.text = string.Empty;
        PasswordField.text = string.Empty;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;
        ErrorLabel.text = string.Empty;
    }

    private void OnEmailRadio(bool value)
    {
        if (value)
        {
            EmailField.gameObject.SetActive(true);
            UsernameField.gameObject.SetActive(false);
        }
    }

    private void OnUniversalRadio(bool value)
    {
        if (value)
        {
            UsernameField.gameObject.SetActive(true);
            EmailField.gameObject.SetActive(false);
        }
    }

    private void OnAnonymousRadio(bool value)
    {
        EmailField.interactable = !value;
        UsernameField.interactable = !value;
        PasswordField.interactable = !value;
    }

    private void OnLoginButton()
    {
        AuthenticationIds ids;
        string inputUser = EmailRadio.isOn ? EmailField.text : UsernameField.text;
        string inputPassword = PasswordField.text;
        string inputName = NameField.text;
        string inputAge = AgeField.text;

        if (!AnonymousRadio.isOn && (inputUser.IsNullOrEmpty() || inputPassword.IsNullOrEmpty()))
        {
            ErrorLabel.text = "Please fill out login details in order to log in.";
            return;
        }

        // Advanced Authentication if Name & Age were inputted
        if (!inputName.IsNullOrEmpty() && !inputAge.IsNullOrEmpty())
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
                ids.externalId = inputUser;
                ids.authenticationToken = inputPassword;
                ids.authenticationSubType = "";
            }

            LoginContent.IsInteractable = false;
            UserHandler.AuthenticateAdvanced(authenticationType, ids, extraJson, OnAuthenticationSuccess, OnAuthenticationFailure);

            return;
        }

        LoginContent.IsInteractable = false;

        if (EmailRadio.isOn)
        {
            UserHandler.AuthenticateEmail(inputUser, inputPassword, OnAuthenticationSuccess, OnAuthenticationFailure);
        }
        else if (UniversalRadio.isOn)
        {
            UserHandler.AuthenticateUniversal(inputUser, inputPassword, OnAuthenticationSuccess, OnAuthenticationFailure);
        }
        else if (AnonymousRadio.isOn)
        {
            UserHandler.AuthenticateAnonymous(OnAuthenticationSuccess, OnAuthenticationFailure);
        }
    }

    #endregion

    #region brainCloud

    private void OnAutomaticLogin()
    {
        Debug.Log("Logging in automatically...");

        LoginContent.IsInteractable = false;

        UserHandler.AuthenticateAnonymous(() =>
        {
            Debug.Log("Automatic Login Successful");

            RememberMeToggle.isOn = false;

            MainMenu.ChangeToAppContent();
        },
        () =>
        {
            LoginContent.IsInteractable = true;

            ErrorLabel.text = "Automatic Login Failed\nPlease try logging in manually.";

            RememberMeToggle.isOn = false;

            UserHandler.ResetAuthenticationData();
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
        ClearFields();

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
        ErrorLabel.text = errorMessage;
    }

    #endregion
}
