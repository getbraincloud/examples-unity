using BrainCloud;
using BrainCloud.Common;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System.Collections;
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

    [Header("TEMP")]
    [SerializeField] private BrainCloudManager BC = default;
    [SerializeField] private GameObject LoginContent = default;
    [SerializeField] private GameObject MainContent = default;

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
        RememberMeToggle.isOn = rememberUserToggle;

        AuthenticationType authenticationType = BC.AuthenticationType;
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

        if (rememberUserToggle)
        {
            // Handle disabling of everything & try to log in
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

    private void OnDestroy()
    {
        //
    }

    #endregion

    #region UI Functionality

    public void EnableFields(bool isEnabled)
    {
        EmailRadio.interactable = isEnabled;
        UniversalRadio.interactable = isEnabled;
        AnonymousRadio.interactable = isEnabled;
        EmailField.interactable = isEnabled;
        UsernameField.interactable = isEnabled;
        PasswordField.interactable = isEnabled;
        RememberMeToggle.interactable = isEnabled;
        LoginButton.interactable = isEnabled;
        NameField.interactable = isEnabled;
        AgeField.interactable = isEnabled;

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
                ids.externalId = BC.AnonymousID;
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

            EnableFields(false);
            BC.AuthenticateAdvanced(authenticationType, ids, extraJson, TempHandleAuthenticationSuccess, TempHandleAuthenticationFailure);

            return;
        }

        EnableFields(false);

        if (EmailRadio.isOn)
        {
            BC.AuthenticateEmail(inputUser, inputPassword, TempHandleAuthenticationSuccess, TempHandleAuthenticationFailure);
        }
        else if (UniversalRadio.isOn)
        {
            BC.AuthenticateUniversal(inputUser, inputPassword, TempHandleAuthenticationSuccess, TempHandleAuthenticationFailure);
        }
        else if (AnonymousRadio.isOn)
        {
            BC.AuthenticateAnonymous(TempHandleAuthenticationSuccess, TempHandleAuthenticationFailure);
        }
    }

    #endregion

    #region Misc Functionality

    private void TempHandleAuthenticationSuccess()
    {
        if (EmailRadio.isOn || UniversalRadio.isOn)
        {
            BC.Wrapper.SetStoredAuthenticationType(EmailRadio.isOn ? AuthenticationType.Email.ToString()
                                                                   : AuthenticationType.Universal.ToString());
        }
        else if (AnonymousRadio.isOn)
        {
            BC.Wrapper.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());
        }

        PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.RememberUser, RememberMeToggle.isOn);

        LoginContent.SetActive(false);
        MainContent.SetActive(true);
    }

    private void TempHandleAuthenticationFailure()
    {
        EnableFields(false);

        string errorMessage;
        if (EmailRadio.isOn)
        {
            errorMessage = "Authentication Failed - Please check your email and password and try again.";
        }
        else if (UniversalRadio.isOn)
        {
            errorMessage = "Authentication Failed - Please check your username and password and try again.";
        }
        else
        {
            errorMessage = "Authentication Failed - Please try again in a few moments.";
        }

        ErrorLabel.text = errorMessage;
    }

    #endregion
}
