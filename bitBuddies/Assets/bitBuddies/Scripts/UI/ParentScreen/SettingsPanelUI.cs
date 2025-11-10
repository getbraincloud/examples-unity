using System;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Gameframework;
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

    private string _tempUsername;
    private void Awake()
    {
        _attachEmailButton.onClick.AddListener(OnAttachEmail);
        _logoutButton.onClick.AddListener(OnLogout);
        _closeButton.onClick.AddListener(OnClose);

        _attachEmailButton.interactable = !BrainCloudManager.Instance.IsEmailAuthenticated;
        _attachEmailInputField.interactable = !BrainCloudManager.Instance.IsEmailAuthenticated;
        _passwordInputField.interactable = !BrainCloudManager.Instance.IsEmailAuthenticated;

        float value = PlayerPrefs.GetFloat(BitBuddiesConsts.VOLUME_SLIDER_KEY, -1.0f);
        if(value == -1)
        {
            value = 0.5f;
        }
        
        //ToDo: Hook up this value to the main audio source component if we get one..
        _volumeSlider.value = value;
    }
    
    private void OnAttachEmail()
    {
        if(_attachEmailInputField.text.IsNullOrEmpty())
        {
            StateManager.Instance.OpenInfoPopUp("Not enough information", "Please fill out the email field");
            return;
        }
        
        if(_passwordInputField.text.IsNullOrEmpty())
        {
            StateManager.Instance.OpenInfoPopUp("Not enough information", "Please fill out the password field");
            return;
        }
        int atSymbol = _attachEmailInputField.text.IndexOf('@');
        _tempUsername =  _attachEmailInputField.text.Substring(0, atSymbol);
        
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
        BrainCloudManager.Instance.UserInfo.UpdateEmail(_attachEmailInputField.text);
        BrainCloudManager.Instance.UserInfo.UpdateUsername(_tempUsername);
        BrainCloudManager.Wrapper.PlayerStateService.UpdateName(_tempUsername);
        StateManager.Instance.RefreshScreen();
        StateManager.Instance.OpenInfoPopUp(BitBuddiesConsts.ATTACH_EMAIL_SUCCESS_TITLE, BitBuddiesConsts.ATTACH_EMAIL_SUCCESS_MESSAGE);
    }
    
    private void OnAttachEmailFailed()
    {
        StateManager.Instance.OpenInfoPopUp(BitBuddiesConsts.ATTACH_EMAIL_FAILURE_TITLE, BitBuddiesConsts.ATTACH_EMAIL_FAILURE_MESSAGE);
    }
    
    private void OnLogout()
    {
        StateManager.Instance.OpenConfirmPopUp(BitBuddiesConsts.ARE_YOU_SURE_LOGOUT_TITLE, BitBuddiesConsts.ARE_YOU_SURE_LOGOUT_MESSAGE, LogoutConfirm);
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
    }
    
    private void OnClose()
    {
        PlayerPrefs.SetFloat(BitBuddiesConsts.VOLUME_SLIDER_KEY, _volumeSlider.value);
    }
}
