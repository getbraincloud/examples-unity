using System.Collections.Generic;
using System.IO;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Debug = UnityEngine.Debug;

public class BrainCloudManager : MonoBehaviour
{
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;

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
    }

#region Start Up - Logging In

    //Called from Unity Button, attempting to login
    public void Login()
    {
        string username = GameManager.Instance.UsernameInputField.text;
        string password = GameManager.Instance.PasswordInputField.text;
        if (username.IsNullOrEmpty())
        {
            GameManager.Instance.ErrorMessage.SetUpPopUpMessage($"Please provide a username");
            StateManager.Instance.AbortToSignIn();
            return;
        }
        else if (password.IsNullOrEmpty())
        {
            GameManager.Instance.ErrorMessage.SetUpPopUpMessage($"Please provide a password");
            StateManager.Instance.AbortToSignIn();
            return;
        }
        InitializeBC();
        // Authenticate with brainCloud
        m_bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, LoggingInError, "Login Failed");
    }
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;

        GameManager.Instance.CurrentUserInfo = new UserInfo();
        GameManager.Instance.CurrentUserInfo.ID = data["profileId"] as string;
        
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            SubmitName(GameManager.Instance.CurrentUserInfo.Username);
        }
        else
        {
            SubmitName(data["playerName"] as string);
            OnLoggedIn(jsonResponse, cbObject);
        }
    }

    
    private void InitializeBC()
    {
        string url = BrainCloud.Plugin.Interface.DispatcherURL;
        string appId = BrainCloud.Plugin.Interface.AppId;
        string appSecret = BrainCloud.Plugin.Interface.AppSecret;
        
        m_bcWrapper.Init(url, appSecret, appId, "1.0");

        m_bcWrapper.Client.EnableLogging(true);
    }
    
    // User fully logged in. Enable RTT and listen for chat messages
    void OnLoggedIn(string jsonResponse, object cbObject)
    {
        
        Debug.Log("Logged in");
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
        GameManager.Instance.ErrorMessage.SetUpPopUpMessage($"Message: {message} |||| JSON: {jsonError}");
        
        StateManager.Instance.AbortToSignIn();
        Debug.Log($"LOGGED");
        
    }
    
    // Submit user name to brainCloud to be assosiated with the current user
    void SubmitName(string username)
    {
        // Update name
        GameManager.Instance.UpdateUsername(username);
        m_bcWrapper.PlayerStateService.UpdateUserName(username, OnLoggedIn, LoggingInError, "Failed to update username to braincloud");
    }
#endregion Start Up - Logging In

    
}
