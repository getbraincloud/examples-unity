using System;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using System.Net.Mail;
using UnityEngine.UI;

public class LoginScreen : ContentUIBehaviour
{
    [SerializeField] private Button QuickCreateButton;
    [SerializeField] private Button OpenCreateAccountButton;
    [SerializeField] private Button OpenLoginButton;
    [SerializeField] private Button LoginButton;
    [SerializeField] private Button OpenForgotPasswordButton;
    [SerializeField] private Button SubmitEmailButton;
    [SerializeField] private TMP_Text _gameVersionText;
    [SerializeField] private TMP_Text _bcClientVersionText;
    [SerializeField] private TMP_InputField Email;
    [SerializeField] private TMP_InputField Password;
    [SerializeField] private TMP_InputField ForgotPasswordEmailInputField;
    [SerializeField] private GameObject ForgotPasswordPanel;
    private bool _createAccount;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
    }

    protected override void InitializeUI()
    {
        ForgotPasswordPanel.SetActive(false);
        QuickCreateButton.onClick.AddListener(OnQuickCreateButtonClick);
        OpenLoginButton.onClick.AddListener(OnOpenLoginButtonClick);
        OpenCreateAccountButton.onClick.AddListener(OnOpenCreateAccountButtonClick);
        LoginButton.onClick.AddListener(OnLoginButtonClick);
        SubmitEmailButton.onClick.AddListener(OnForgotPasswordButtonClick);
        OpenForgotPasswordButton.onClick.AddListener(OnOpenForgotPasswordPanel);
        
        _gameVersionText.text = $"Game Version: {Application.version}";
        _bcClientVersionText.text = $"BC Client Version: {BrainCloud.Version.GetVersion()}";
    }
    
    private void OnQuickCreateButtonClick()
    {
        //Authenticate Anonymously but still go through the normal logging in path
        _createAccount = true;
        IsInteractable = false;
        BrainCloudManager.Instance.UserInfo = new UserInfo();
        BrainCloudManager.Wrapper.AuthenticateAnonymous
        (
            OnSuccess("Anonymous Account creation successfully", OnLoggedInUser),
            OnFailure("Anonymous Account creation failed", OnFailureCallback)
        );
    }
    
    private void OnOpenCreateAccountButtonClick()
    {
        _createAccount = true;
        OpenForgotPasswordButton.gameObject.SetActive(false);
    }
    
    private void OnOpenLoginButtonClick()
    {
        _createAccount = false;
        OpenForgotPasswordButton.gameObject.SetActive(true);
    }

    private void OnLoginButtonClick()
    {
        if(Email.text.IsNullOrEmpty())
        {
            StateManager.Instance.OpenInfoPopUp("Username is empty", "Please enter a username");
            Debug.LogError("Username is empty");
            return;
        }
        if(Password.text.IsNullOrEmpty())
        {
            StateManager.Instance.OpenInfoPopUp("Password is empty", "Please enter a password");
            Debug.LogError("Password is empty");
            return;
        }
        
        if(!IsEmailValid(Email.text))
        {
            StateManager.Instance.OpenInfoPopUp("Email entered is invalid", "Please enter a valid email");
            Debug.LogError("Please enter a valid email");
            return;
        }
        IsInteractable = false;
        int atSymbol = Email.text.IndexOf('@');
        string username =  Email.text.Substring(0, atSymbol);
        Debug.LogWarning("Username: " + username);
        BrainCloudManager.Instance.UserInfo = new UserInfo();
        BrainCloudManager.Instance.UserInfo.UpdateUsername(username);
        BrainCloudManager.Instance.UserInfo.UpdateEmail(Email.text);
        
        AuthenticateEmail(Email.text, Password.text);
    }
    
    private static bool IsEmailValid(string in_email)
    {
        try
        {
            var address = new MailAddress(in_email);
            return address.Address == in_email;
        }
        catch
        {
            return false;
        }
    }
        
    private void AuthenticateEmail(string username, string password)
    {
        BrainCloudManager.Wrapper.AuthenticateEmailPassword
        (
            username, 
            password, 
            _createAccount, 
            OnSuccess("Authentication Success", OnLoggedInUser), 
            OnFailure("", OnFailureCallback)
        );
    }
    
    private void OnLoggedInUser(string jsonResponse)
    {
        IsInteractable = true;
        BrainCloudManager.Instance.OnAuthenticateSuccess(jsonResponse);
        StateManager.Instance.GoToParent();
    }
    
    private void OnFailureCallback()
    {
        StateManager.Instance.OpenInfoPopUp("Something went wrong", "Please try again later");
    }
    
    private void OnOpenForgotPasswordPanel()
    {
        ForgotPasswordPanel.SetActive(true);
    }
    
    private void OnForgotPasswordButtonClick()
    {
        //No idea why but I keep getting status 202 and reason code 40209
        BrainCloudManager.Wrapper.ResetEmailPassword(ForgotPasswordEmailInputField.text, null, OnFailure("Something went wrong", OnFailureCallback));
    }
}
