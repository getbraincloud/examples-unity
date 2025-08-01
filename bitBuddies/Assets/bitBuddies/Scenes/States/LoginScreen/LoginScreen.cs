using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreen : ContentUIBehaviour
{
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _forgotPasswordButton;
    [SerializeField] private TMP_InputField Username;
    [SerializeField] private TMP_InputField Password;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        _loginButton.onClick.AddListener(OnLoginButtonClick);
        _forgotPasswordButton.onClick.AddListener(OnForgotPasswordButtonClick);
        base.Awake();
    }

    protected override void InitializeUI()
    {
        
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
        
        BrainCloudManager.Instance.AuthenticateUniversal(Username.text, Password.text);
    }
    
    private void OnForgotPasswordButtonClick()
    {
        
    }
}
