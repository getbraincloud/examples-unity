using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using System.Net.Mail;
using TMPro;
using UnityEngine;
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
        BrainCloudManager.Instance.CurrentUserInfo = new UserInfo();
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
        if (Email.text.IsNullOrEmpty())
        {
            PopUpUI.Show("Username is empty")
                   .AddBodyText("Please enter a username");
            return;
        }

        if (Password.text.IsNullOrEmpty())
        {
            PopUpUI.Show("Password is empty")
                   .AddBodyText("Please enter a password");
            return;
        }

        if (!IsEmailValid(Email.text))
        {
            PopUpUI.Show("Email entered is invalid")
                   .AddBodyText("Please enter a valid email");
            return;
        }

        IsInteractable = false;
        int atSymbol = Email.text.IndexOf('@');
        string username = Email.text.Substring(0, atSymbol);
        BrainCloudManager.Instance.CurrentUserInfo = new UserInfo();
        BrainCloudManager.Instance.CurrentUserInfo.UpdateUsername(username);
        BrainCloudManager.Instance.CurrentUserInfo.UpdateEmail(Email.text);

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
        PopUpUI.Show("Something went wrong")
               .AddBodyText("Please try again later");
        IsInteractable = true;
    }

    private void OnOpenForgotPasswordPanel()
    {
        ForgotPasswordPanel.SetActive(true);
    }

    private void OnForgotPasswordButtonClick()
    {
        if (ForgotPasswordEmailInputField.text.IsNullOrEmpty())
        {
            PopUpUI.Show("Email is empty")
                   .AddBodyText("Please enter an email");
            return;
        }
        if (!IsEmailValid(ForgotPasswordEmailInputField.text))
        {
            PopUpUI.Show("Email entered is invalid")
                   .AddBodyText("Please enter a valid email");
            return;
        }

        PopUpUI.Show("Are you sure?", false)
               .AddBodyText("We will send you an email with a link to reset your password")
               .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
               .AddButton("Confirm", PopUpUI.ButtonColor.Green, OnConfirmForgotPasswordButtonClick);
    }

    private void OnConfirmForgotPasswordButtonClick()
    {
        //No idea why but I keep getting status 202 and reason code 40209
        BrainCloudManager.Wrapper.ResetEmailPassword(ForgotPasswordEmailInputField.text, OnSuccess("ForgotPasswordEmailSuccess", OnForgotPasswordSuccess), OnFailure("Something went wrong", OnFailureCallback));
    }

    private void OnForgotPasswordSuccess(string jsonResponse)
    {
        PopUpUI.Show("Email sent")
               .AddBodyText("Please check your email for a link to reset your password");
        Email.text = ForgotPasswordEmailInputField.text;
        ForgotPasswordPanel.SetActive(false);
    }
}
