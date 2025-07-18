using Gameframework;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreen : ContentUIBehaviour
{
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _forgotPasswordButton;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _loginButton.onClick.AddListener(OnLoginButtonClick);
        _forgotPasswordButton.onClick.AddListener(OnForgotPasswordButtonClick);
    }

    protected override void InitializeUI()
    {
        throw new System.NotImplementedException();
    }

    private void OnLoginButtonClick()
    {
        
    }
    
    private void OnForgotPasswordButtonClick()
    {
        
    }
}
