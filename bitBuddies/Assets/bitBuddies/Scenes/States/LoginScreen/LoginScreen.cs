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
    [SerializeField] private Button ForgotPasswordButton;
    [SerializeField] private TMP_Text _gameVersionText;
    [SerializeField] private TMP_Text _bcClientVersionText;
    [SerializeField] private TMP_InputField Email;
    [SerializeField] private TMP_InputField Password;
    private bool _createAccount;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        InitializeUI();
        base.Awake();
    }

    protected override void InitializeUI()
    {
        QuickCreateButton.onClick.AddListener(OnQuickCreateButtonClick);
        OpenLoginButton.onClick.AddListener(OnOpenLoginButtonClick);
        OpenCreateAccountButton.onClick.AddListener(OnOpenCreateAccountButtonClick);
        LoginButton.onClick.AddListener(OnLoginButtonClick);
        ForgotPasswordButton.onClick.AddListener(OnForgotPasswordButtonClick);
        
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
    }
    
    private void OnOpenLoginButtonClick()
    {
        _createAccount = false;
    }

    private void OnLoginButtonClick()
    {
        if(Email.text.IsNullOrEmpty())
        {
            //ToDo: make a pop up
            Debug.LogError("Username is empty");
            return;
        }
        if(Password.text.IsNullOrEmpty())
        {
            //ToDo: make a pop up
            Debug.LogError("Password is empty");
            return;
        }
        
        if(!IsEmailValid(Email.text))
        {
            //ToDo: make a pop up
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
        
        AuthenticateUniversal(Email.text, Password.text);
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
        
    private void AuthenticateUniversal(string username, string password)
    {
        BrainCloudManager.Wrapper.AuthenticateUniversal
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
    
    }
    
    private void OnForgotPasswordButtonClick()
    {
        
    }
}
