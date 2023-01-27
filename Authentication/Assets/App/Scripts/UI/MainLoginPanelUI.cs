using BrainCloud;
using BrainCloud.Common;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainLoginPanelUI : MonoBehaviour
{
    [Header("Main UI")]
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
    [SerializeField] private CanvasGroup LoginContent = default;

    #region Unity Messages

    private void Awake()
    {
        EmailField.text = string.Empty;
        UsernameField.text = string.Empty;
        PasswordField.text = string.Empty;
        NameField.text = string.Empty;
        AgeField.text = string.Empty;
        ErrorLabel.text = string.Empty;
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
        PlayerPrefsHandler.LoadPlayerPref(PlayerPrefKey.RememberUser, out bool rememberUserToggle);
        RememberMeToggle.isOn = rememberUserToggle && !string.IsNullOrEmpty(UserHandler.AnonymousID);

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
        else if (rememberUserToggle && !string.IsNullOrEmpty(UserHandler.AnonymousID))
        {
            PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.RememberUser, false);
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

    #region UI Functionality

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

        if (!AnonymousRadio.isOn && (string.IsNullOrEmpty(inputUser) || string.IsNullOrEmpty(inputPassword)))
        {
            return;
        }

        // Advanced Authentication if Name & Age were inputted
        if (!string.IsNullOrEmpty(inputName) && !string.IsNullOrEmpty(inputAge))
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

            LoginContent.interactable = false;
            UserHandler.AuthenticateAdvanced(authenticationType, ids, extraJson, HandleAuthenticationSuccess, HandleAuthenticationFailure);

            return;
        }

        LoginContent.interactable = false;

        if (EmailRadio.isOn)
        {
            UserHandler.AuthenticateEmail(inputUser, inputPassword, HandleAuthenticationSuccess, HandleAuthenticationFailure);
        }
        else if (UniversalRadio.isOn)
        {
            UserHandler.AuthenticateUniversal(inputUser, inputPassword, HandleAuthenticationSuccess, HandleAuthenticationFailure);
        }
        else if (AnonymousRadio.isOn)
        {
            UserHandler.AuthenticateAnonymous(HandleAuthenticationSuccess, HandleAuthenticationFailure);
        }
    }

    #endregion

    #region brainCloud Authentication

    private void HandleAutomaticLogin()
    {
        Debug.Log("Logging in automatically...");

        LoginContent.interactable = false;

        UserHandler.AuthenticateAnonymous(() =>
        {
            Debug.Log("Automatic Login Successful");

            MainMenu.ChangeToAppContent();
        },
        () =>
        {
            LoginContent.interactable = true;

            ErrorLabel.text = "Automatic Login Failed\nPlease try logging in manually.";

            RememberMeToggle.isOn = false;

            UserHandler.ResetAuthenticationData();
        });
    }

    private void HandleAuthenticationSuccess()
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

        PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.RememberUser, RememberMeToggle.isOn);

        MainMenu.ChangeToAppContent();
    }

    private void HandleAuthenticationFailure()
    {
        LoginContent.interactable = true;

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

        ErrorLabel.text = errorMessage;
    }

    #endregion
}
