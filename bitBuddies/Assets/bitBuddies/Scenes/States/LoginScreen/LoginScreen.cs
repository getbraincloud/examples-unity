using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LoginScreen : ContentUIBehaviour
{
    [SerializeField] private Button QuickCreateButton;
    [SerializeField] private Button OpenCreateAccountButton;
    [SerializeField] private Button OpenLoginButton;
    [SerializeField] private Button LoginButton;
    [SerializeField] private Button ForgotPasswordButton;
    [SerializeField] private TMP_InputField Username;
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
    }
    
    private void OnQuickCreateButtonClick()
    {
        //Authenticate Anonymously but still go through the normal logging in path
        _createAccount = true;
        IsInteractable = false;
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
        if(Username.text.IsNullOrEmpty())
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
        IsInteractable = false;
        AuthenticateUniversal(Username.text, Password.text);
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
    
    private void OnLoggedInUser(string jsonResponse, object cbObject)
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
