using System;
using System.Collections;
using System.Text.RegularExpressions;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    TMP_Text LoadingIndicator;

    private bool _isLoading;
    
    public bool IsLoading
    {
        get => _isLoading;
        set => _isLoading = value;
    }

    [SerializeField]
    string m_LobbySceneName = "InvadersLobby";

    [SerializeField] private GameObject LoginInputFields;
    [SerializeField] private GameObject MainMenuButtons;

    [SerializeField] private TMP_InputField UsernameInputField;
    [SerializeField] private TMP_InputField PasswordInputField;
    [SerializeField] private TMP_Text AuthenticatedUserLabel;

    private string _loadingIndicatorMessage = "Looking for a server";
    private string _dotsForLoadingIndicator;
    private int _numberOfDots;
    
    public Toggle RememberMeToggle;
    public static MenuControl Singleton { get; private set; }

    private void Start()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
        LoadingIndicator.gameObject.SetActive(false);
        if(BrainCloudManager.Singleton.BCWrapper.Client.Authenticated) 
        {
            LoginInputFields.gameObject.SetActive(false);
            MainMenuButtons.gameObject.SetActive(true);
            DisplayUsername(BrainCloudManager.Singleton.LocalUserInfo.Username);
        }
        else
        {
            LoginInputFields.gameObject.SetActive(true);
            AuthenticatedUserLabel.gameObject.SetActive(false);
            MainMenuButtons.gameObject.SetActive(false);            
        }
    }
    
    public void Logout()
    {
        BrainCloudManager.Singleton.Logout();
        BackToLogin();  
    }
    
    public void JoinGame()
    {
        BrainCloudManager.Singleton.FindOrCreateLobby();

        LoadingIndicator.gameObject.SetActive(true);
        _isLoading = true;
        _numberOfDots = 0;
        LoadingIndicator.text = _loadingIndicatorMessage;
        StartCoroutine(WaitingForLobby());
    }
    
    IEnumerator WaitingForLobby()
    {
        while(_isLoading)
        {
            if(_numberOfDots < 3)
            {
                LoadingIndicator.text += ".";
                _numberOfDots++;
            }
            else
            {
                _numberOfDots = 0;
                LoadingIndicator.text = _loadingIndicatorMessage;
            }

            yield return new WaitForSeconds(0.5f);
        }
        
        SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
    }
    
    public void Login()
    {
        if(UsernameInputField.text.IsNullOrEmpty() || PasswordInputField.text.IsNullOrEmpty())
        {
            Debug.LogError("Need to fill both fields in order to login.");
            return;
        }
        BrainCloudManager.Singleton.AuthenticateWithBrainCloud(UsernameInputField.text, PasswordInputField.text);
    }

    public void DisplayUsername(string username)
    {
        AuthenticatedUserLabel.gameObject.SetActive(true);
        AuthenticatedUserLabel.text = username;
    }

    public void SwitchMenuButtons()
    {
        LoginInputFields.SetActive(false);
        MainMenuButtons.SetActive(true);
    }
    
    private void BackToLogin()
    {
        AuthenticatedUserLabel.text = "";
        UsernameInputField.text = "";
        PasswordInputField.text = "";
        RememberMeToggle.isOn = true;
        AuthenticatedUserLabel.gameObject.SetActive(false);
        LoginInputFields.SetActive(true);
        MainMenuButtons.SetActive(false);
    }
}
