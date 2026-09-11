using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private TMP_InputField _attachEmailInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private Button _attachEmailButton;
    [SerializeField] private Button _logoutButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _warningText;

    private string _tempUsername;

    private void Awake()
    {
        _attachEmailButton.onClick.AddListener(OnAttachEmail);
        _logoutButton.onClick.AddListener(OnLogout);
        _closeButton.onClick.AddListener(OnClose);

        if (BrainCloudManager.Instance.IsEmailAuthenticated)
        {
            _attachEmailInputField.text = BrainCloudManager.Instance.CurrentUserInfo.Email;
            _passwordInputField.text = "12345";
            _warningText.gameObject.SetActive(false);
        }

        float value = PlayerPrefs.GetFloat(BitBuddiesConsts.VOLUME_SLIDER_KEY, -1.0f);
        if (value == -1)
        {
            value = 0.5f;
        }

        //ToDo: Hook up this value to the main audio source component if we get one..
        _volumeSlider.value = value;
    }

    private void OnAttachEmail()
    {
        if (_attachEmailInputField.text.IsNullOrEmpty() ||
            !_attachEmailInputField.text.Contains('@'))
        {
            PopUpUI.Show("Not enough information")
                   .AddBodyText("Please fill out the email field");
            return;
        }

        if (_passwordInputField.text.IsNullOrEmpty())
        {
            PopUpUI.Show("Not enough information")
                   .AddBodyText("Please fill out the password field");
            return;
        }

        if (_attachEmailInputField.text.Equals(BrainCloudManager.Instance.CurrentUserInfo.Email))
        {
            PopUpUI.Show("Email Already Attached")
                   .AddBodyText("Email is already attached to account, please enter a different email");
            return;
        }

        int atSymbol = _attachEmailInputField.text.IndexOf('@');
        _tempUsername = _attachEmailInputField.text.Substring(0, atSymbol);
        if (BrainCloudManager.Instance.IsEmailAuthenticated)
        {
            //Detach then attach new email.
            BrainCloudManager.Wrapper.IdentityService.DetachEmailIdentity
            (
                BrainCloudManager.Instance.CurrentUserInfo.Email,
                true,
                BrainCloudManager.HandleSuccess("Detach Email Successful", OnDetachEmailSuccess),
                BrainCloudManager.HandleFailure("Detach Email Failed", OnAttachEmailFailed)
            );
        }
        else
        {
            BrainCloudManager.Wrapper.IdentityService.AttachEmailIdentity
            (
                _attachEmailInputField.text,
                _passwordInputField.text,
                BrainCloudManager.HandleSuccess("Attach Email Successful", OnAttachEmailSuccess),
                BrainCloudManager.HandleFailure("Attach Email Failed", OnAttachEmailFailed)
            );
        }

    }

    private void OnDetachEmailSuccess()
    {
        BrainCloudManager.Wrapper.IdentityService.AttachEmailIdentity
        (
            _attachEmailInputField.text,
            _passwordInputField.text,
            BrainCloudManager.HandleSuccess("Attach Email Successful", OnAttachEmailSuccess),
            BrainCloudManager.HandleFailure("Attach Email Failed", OnAttachEmailFailed)
        );
    }

    private void OnAttachEmailSuccess()
    {
        BrainCloudManager.Instance.CurrentUserInfo.UpdateEmail(_attachEmailInputField.text);
        BrainCloudManager.Instance.CurrentUserInfo.UpdateUsername(_tempUsername);
        BrainCloudManager.Wrapper.PlayerStateService.UpdateUserName(_tempUsername);
        StateManager.Instance.RefreshScreen();
        PopUpUI.Show(BitBuddiesConsts.ATTACH_EMAIL_SUCCESS_TITLE)
               .AddBodyText(BitBuddiesConsts.ATTACH_EMAIL_SUCCESS_MESSAGE);
    }

    private void OnAttachEmailFailed()
    {
        PopUpUI.Show(BitBuddiesConsts.ATTACH_EMAIL_FAILURE_TITLE)
               .AddBodyText(BitBuddiesConsts.ATTACH_EMAIL_FAILURE_MESSAGE);
    }

    private void OnLogout()
    {
        PopUpUI.Show(BitBuddiesConsts.ARE_YOU_SURE_LOGOUT_TITLE, false)
               .AddBodyText(BitBuddiesConsts.ARE_YOU_SURE_LOGOUT_MESSAGE)
               .AddButton("Close", PopUpUI.ButtonColor.Blue, null)
               .AddButton("Confirm", PopUpUI.ButtonColor.Green, LogoutConfirm);
    }

    private void LogoutConfirm()
    {
        BrainCloudManager.Wrapper.Logout
        (
            true,
            BrainCloudManager.HandleSuccess("Logout Successful", OnLogoutSuccess)
        );
    }

    private void OnLogoutSuccess()
    {
        PlayerPrefs.DeleteAll();
        GameManager.Instance.ClearDataForLogout();
        BrainCloudManager.Instance.ClearDataForLogout();
        StateManager.Instance.GoToLogin();
        Destroy(gameObject);
    }

    private void OnClose()
    {
        PlayerPrefs.SetFloat(BitBuddiesConsts.VOLUME_SLIDER_KEY, _volumeSlider.value);
    }
}
