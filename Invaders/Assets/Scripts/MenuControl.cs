using System;
using System.Text.RegularExpressions;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    TMP_Text m_IPAddressText;

    [SerializeField]
    string m_LobbySceneName = "InvadersLobby";

    [SerializeField] private GameObject LoginInputFields;
    [SerializeField] private GameObject MainMenuButtons;

    [SerializeField] private TMP_InputField UsernameInputField;
    [SerializeField] private TMP_InputField PasswordInputField;
    
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

#if UNITY_SERVER
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

    // Logic for hosting a game
    // public void StartGame()
    // {
    //      var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
    //      if (utpTransport)
    //      {
    //          utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
    //      }
    //      if (NetworkManager.Singleton.StartHost())
    //      {
    //          SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
    //          SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
    //      }
    //      else
    //      {
    //          Debug.LogError("Failed to start host.");
    //      }
    // }

    public void JoinGame()
    {
        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
        }
        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
        }
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
        NetworkManager.Singleton.StartClient();
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
