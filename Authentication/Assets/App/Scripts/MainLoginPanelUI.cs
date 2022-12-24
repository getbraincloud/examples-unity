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
    [SerializeField] private Toggle RemUserToggle = default;
    [SerializeField] private Toggle RemPasswordToggle = default;
    [SerializeField] private Button LoginButton = default;
    [SerializeField] private TMP_InputField NameField = default;
    [SerializeField] private TMP_InputField AgeField = default;

    [Header("Misc Elements")]
    [SerializeField] private TMP_Text ErrorLabel = default;
    [SerializeField] private GameObject RemEmailToggleLabel = default;
    [SerializeField] private GameObject RemUsernameToggleLabel = default;
    [SerializeField] private GameObject[] OptionalUIElements = default;

    [Header("TEMP")]
    [SerializeField] private BrainCloudManager BC = default;
    [SerializeField] private GameObject AuthenticationContent = default;
    [SerializeField] private GameObject MainContent = default;
    [SerializeField] private TMP_Text LoggedInText = default;

    private bool showOptionalUIElements = false;

    #region Unity Messages

    private void Awake()
    {
        showOptionalUIElements = true;//string.IsNullOrEmpty(prefsEmail + prefsUsername); // Based on if Username/Email is remembered or not

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
        RemUserToggle.onValueChanged.AddListener(OnRememberUserToggle);
        LoginButton.onClick.AddListener(OnLoginButton);
        //NameField.onValueChanged.AddListener();
        //AgeField.onValueChanged.AddListener();
    }

    private void Start()
    {
        EmailField.text = PlayerPrefsHandler.LoadPlayerPref(PlayerPrefKey.Email);
        UsernameField.text = PlayerPrefsHandler.LoadPlayerPref(PlayerPrefKey.Username);
        PasswordField.text = PlayerPrefsHandler.LoadPlayerPref(PlayerPrefKey.Password);
        NameField.text = string.Empty;
        AgeField.text = string.Empty;

        foreach (GameObject element in OptionalUIElements)
        {
            element.SetActive(showOptionalUIElements);
        }

        bool emailRadio = !string.IsNullOrEmpty(EmailField.text) && !string.IsNullOrEmpty(BC.ProfileID);
        bool universalRadio = !string.IsNullOrEmpty(UsernameField.text) && !string.IsNullOrEmpty(BC.ProfileID);
        bool anonymousRadio = !emailRadio && !universalRadio && !string.IsNullOrEmpty(BC.AnonymousID);

        RemPasswordToggle.isOn = !string.IsNullOrEmpty(PasswordField.text);
        RemUserToggle.isOn = !anonymousRadio && (emailRadio || universalRadio);
        OnRememberUserToggle(RemUserToggle.isOn);

        if (anonymousRadio)
        {
            AnonymousRadio.isOn = true;
            UsernameField.gameObject.SetActive(false);
            OnAnonymousRadio(true);
        }
        else if (universalRadio)
        {
            UniversalRadio.isOn = true;
            OnUniversalRadio(true);
        }
        else // Default to Email
        {
            EmailRadio.isOn = true;
            OnEmailRadio(true);
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
        RemUserToggle.onValueChanged.RemoveAllListeners();
        RemPasswordToggle.onValueChanged.RemoveAllListeners();
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

    private void OnEmailRadio(bool value)
    {
        if (value)
        {
            EmailField.gameObject.SetActive(true);
            RemEmailToggleLabel.SetActive(true);
            UsernameField.gameObject.SetActive(false);
            RemUsernameToggleLabel.SetActive(false);
        }
    }

    private void OnUniversalRadio(bool value)
    {
        if (value)
        {
            UsernameField.gameObject.SetActive(true);
            RemUsernameToggleLabel.SetActive(true);
            EmailField.gameObject.SetActive(false);
            RemEmailToggleLabel.SetActive(false);
        }
    }

    private void OnAnonymousRadio(bool value)
    {
        EmailField.interactable = !value;
        UsernameField.interactable = !value;
        PasswordField.interactable = !value;
        RemUserToggle.interactable = !value;
        RemPasswordToggle.interactable = RemUserToggle.isOn && !value;
    }

    private void OnRememberUserToggle(bool value)
    {
        RemPasswordToggle.interactable = value;
        RemPasswordToggle.isOn = RemPasswordToggle.isOn && RemPasswordToggle.interactable;
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

            BC.AuthenticateAdvanced(authenticationType, ids, extraJson, TempHandleAuthenticationSuccess, TempHandleAuthenticationFailure);

            return;
        }

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
            BC.Wrapper.ResetStoredAnonymousId();
            BC.Wrapper.SetStoredAuthenticationType(EmailRadio.isOn ? AuthenticationType.Email.ToString()
                                                                   : AuthenticationType.Universal.ToString());

            if (RemUserToggle.isOn)
            {
                PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Email, EmailRadio.isOn ? EmailField.text : string.Empty);
                PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Username, UniversalRadio.isOn ? UsernameField.text : string.Empty);

                if (RemPasswordToggle.isOn)
                {
                    PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Password, PasswordField.text);
                }
            }
        }
        else if (AnonymousRadio.isOn)
        {
            BC.Wrapper.ResetStoredProfileId();
            BC.Wrapper.SetStoredAuthenticationType(AuthenticationType.Anonymous.ToString());
            PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Email, string.Empty);
            PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Username, string.Empty);
            PlayerPrefsHandler.SavePlayerPref(PlayerPrefKey.Password, string.Empty);
        }

        AuthenticationContent.SetActive(false);
        MainContent.SetActive(true);
        LoggedInText.text += AnonymousRadio.isOn ? $"\nAnonymous ID: {BC.AnonymousID}" : $"\nProfileID: {BC.ProfileID}";
    }

    private void TempHandleAuthenticationFailure()
    {
        ErrorLabel.text = "Authentication Failed";
    }

    #endregion
}
