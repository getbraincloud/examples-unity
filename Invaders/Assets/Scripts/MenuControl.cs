using System;
using System.Collections;
using System.Text.RegularExpressions;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

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

    private string _loadingIndicatorMessage = "Looking for a server";
    private string _dotsForLoadingIndicator;
    private int _numberOfDots;
    
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
        LoginInputFields.gameObject.SetActive(true);
        MainMenuButtons.gameObject.SetActive(false);
#if UNITY_SERVER && !UNITY_EDITOR
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            //Debug.Log("Server Started Successfully !");
        };

        NetworkManager.Singleton.OnClientConnectedCallback += clientID =>
        {
            SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
            SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
        };
        StartServer();
#endif
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
        
        //Set Connection here?
        //StartClient();
        //SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
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
    
    public void SwitchMenuButtons()
    {
        LoginInputFields.SetActive(false);
        MainMenuButtons.SetActive(true);
    }
    
    public void StartClient()
    {
        /*var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            //ToDo: Need to replace the set connection data to IP address from brainCloud
            utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
        }
        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
        }*/

    }
    
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
    
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }
    
    static string Sanitize(string dirtyString)
    {
        // sanitize the input for the ip address
        return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
    }
}
