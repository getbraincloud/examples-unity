using System.Collections;
using System.Collections.Generic;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using BrainCloud.JsonFx.Json;
using UnityEngine;

public class BrainCloudManager : MonoBehaviour
{
    
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;
    public bool LeavingGame;
    public BrainCloudWrapper Wrapper => m_bcWrapper;
    public static BrainCloudManager Instance;
    
    private void Awake()
    {
        m_bcWrapper = GetComponent<BrainCloudWrapper>();
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }
    
    private void InitializeBC()
    {
        m_bcWrapper.Init();

        m_bcWrapper.Client.EnableLogging(true);
    }
    
    // Uninitialize brainCloud
    void UninitializeBC()
    {
        if (m_bcWrapper != null)
        {
            m_bcWrapper.Client.ShutDown();
        }
    }

    //Called from Unity Button, attempting to login
    public void Login()
    {
        string username = MenuManager.Instance.UsernameInputField.text;
        string password = MenuManager.Instance.PasswordInputField.text;
        if (username.IsNullOrEmpty())
        {   
            MenuManager.Instance.AbortToSignIn($"Please provide a username");
            return;
        }
        if (password.IsNullOrEmpty())
        {
            MenuManager.Instance.AbortToSignIn($"Please provide a password");
            return;
        }
        
        GameManager.Instance.CurrentUserInfo.Username = username;
        InitializeBC();
        // Authenticate with brainCloud
        m_bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, LoggingInError, "Login Failed");
    }
    
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var tempUsername = GameManager.Instance.CurrentUserInfo.Username;
        var userInfo = GameManager.Instance.CurrentUserInfo;
        
        userInfo = new UserInfo
        {
            ID = data["profileId"] as string
        };
        
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            m_bcWrapper.PlayerStateService.UpdateName(tempUsername, OnLoggedIn, LoggingInError,
                "Failed to update username to braincloud");
        }
        else
        {
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
            }
            m_bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnLoggedIn, LoggingInError,
                "Failed to update username to braincloud");
        }
        GameManager.Instance.CurrentUserInfo = userInfo;
    }
    
    // Go back to login screen, with an error message
    void LoggingInError(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (m_dead) return;

        m_dead = true;

        m_bcWrapper.RelayService.DeregisterRelayCallback();
        m_bcWrapper.RelayService.DeregisterSystemCallback();
        m_bcWrapper.RelayService.Disconnect();
        m_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        m_bcWrapper.RTTService.DisableRTT();

        string message = cbObject as string;

        MenuManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");

    }
    
    // User fully logged in. 
    void OnLoggedIn(string jsonResponse, object cbObject)
    {
        MenuManager.Instance.UpdateMainMenu();
        PlayerPrefs.SetString(Settings.PasswordKey, MenuManager.Instance.PasswordInputField.text);
        MenuManager.Instance.IsLoading = false;
    }
}
