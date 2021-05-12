using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using Debug = UnityEngine.Debug;

public class BrainCloudManager : MonoBehaviour
{
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;
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
    }

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
        if (password.IsNullOrEmpty())
        {
            GameManager.Instance.ErrorMessage.SetUpPopUpMessage($"Please provide a password");
            StateManager.Instance.AbortToSignIn();
            return;
        }
        
        InitializeBC();
        // Authenticate with brainCloud
        m_bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, LoggingInError, "Login Failed");
    }

    private void FixedUpdate()
    {
        if (m_bcWrapper != null)
        {
            m_bcWrapper.Update();
            if (StateManager.Instance.CurrentGameState == GameStates.Match)
            {
                //Update shockwaves
                //Validate shockwaves
                
            }    
        }
        

        if (m_dead)
        {
            m_dead = false;
            
            // We differ destroying BC because we cannot destroy it within a callback
            UninitializeBC();
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
    // Uninitialize brainCloud
    void UninitializeBC()
    {
        if (m_bcWrapper != null)
        {
            m_bcWrapper.Client.ShutDown();
            m_bcWrapper = null;
        }
    }

#region Input update

    // User moved mouse in the play area
        public void MouseMoved(Vector2 pos)
        {
            GameManager.Instance.CurrentUserInfo.IsAlive = true;
            GameManager.Instance.CurrentUserInfo.MousePosition = pos;
            UserInfo myUser = null;
            Lobby lobby = StateManager.Instance.CurrentLobby;
            foreach (var user in lobby.Members)
            {
                if (GameManager.Instance.CurrentUserInfo.ID == user.ID)
                {
                    //Save it for later !
                    user.IsAlive = true;
                    user.MousePosition = pos;
                    myUser = user;
                    break;
                }
            }

            // Send to other players
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData["x"] = pos.x;
            jsonData["y"] = pos.y;

            Dictionary<string, object> json = new Dictionary<string, object>();
            json["op"] = "move";
            json["data"] = jsonData;

            byte[] data = Encoding.ASCII.GetBytes(JsonWriter.Serialize(json));
            m_bcWrapper.RelayService.Send
                (
                    data, 
                    BrainCloudRelay.TO_ALL_PLAYERS, 
                    Settings.GetPlayerPrefBool(Settings.ReliableKey), 
                    Settings.GetPlayerPrefBool(Settings.OrderedKey),
                    Settings.SendChannel()
                );
        }

        // User clicked mouse in the play area
        public void Shockwave(Vector2 pos)
        {
            // Send to other players
            Dictionary<string, object> jsonData = new Dictionary<string, object>();
            jsonData["x"] = pos.x;
            jsonData["y"] = pos.y;

            Dictionary<string, object> json = new Dictionary<string, object>();
            json["op"] = "shockwave";
            json["data"] = jsonData;

            byte[] data = Encoding.ASCII.GetBytes(JsonWriter.Serialize(json));
            m_bcWrapper.RelayService.Send(data, BrainCloudRelay.TO_ALL_PLAYERS, 
                true, // Reliable
                false, // Unordered
                Settings.SendChannel());

       }

#endregion Input update

#region BC Callbacks

    // User fully logged in. Enable RTT and listen for chat messages
    void OnLoggedIn(string jsonResponse, object cbObject)
    {

        Debug.Log("Logged in");
    }
    
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var userInfo = GameManager.Instance.CurrentUserInfo;
        userInfo = new UserInfo();
        userInfo.ID = data["profileId"] as string;
        GameManager.Instance.CurrentUserInfo = userInfo;
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            GameManager.Instance.UpdateUsername(userInfo.Username);
            m_bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnLoggedIn, LoggingInError,
                "Failed to update username to braincloud");
        }
        else
        {
            var username = data["playerName"] as string;
            GameManager.Instance.UpdateUsername(username);
            m_bcWrapper.PlayerStateService.UpdateName(username, OnLoggedIn, LoggingInError,
                "Failed to update username to braincloud");
            //OnLoggedIn(jsonResponse, cbObject);
        }
        PlayerPrefs.SetString(Settings.PasswordKey,GameManager.Instance.PasswordInputField.text);
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

    }
#endregion BC Callbacks
    
#region GameFlow

    public void FindLobby(RelayConnectionType protocol)
    {
        StateManager.Instance.protocol = protocol;
        
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();

        // Enable RTT
        m_bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        m_bcWrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, OnRTTConnected, OnRTTDisconnected);
    }
    // Cleanly close the game. Go back to main menu but don't log 
    private void CloseGame()
    {
        m_bcWrapper.RelayService.DeregisterRelayCallback();
        m_bcWrapper.RelayService.DeregisterSystemCallback();
        m_bcWrapper.RelayService.Disconnect();
        m_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        m_bcWrapper.RTTService.DisableRTT();

        // Reset state but keep the user around
        StateManager.Instance.LeaveMatchBackToMenu();
    }
    
    // Ready up and signals RTT service we can start the game
    public void StartGame()
    {
        StateManager.Instance.isReady = true;
        
        //
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;

        //
        m_bcWrapper.LobbyService.UpdateReady(StateManager.Instance.CurrentLobby.LobbyID, StateManager.Instance.isReady, extra);
    }

#endregion GameFlow

#region RTT functions

    // We received a lobby event through RTT
    void OnLobbyEvent(string jsonResponse)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var jsonData = response["data"] as Dictionary<string, object>;

        // If there is a lobby object present in the message, update our lobby
        // state with it.
        if (jsonData.ContainsKey("lobby"))
        {
            StateManager.Instance.CurrentLobby = new Lobby(jsonData["lobby"] as Dictionary<string, object>,
                jsonData["lobbyId"] as string);
            if (StateManager.Instance.CurrentGameState == GameStates.Lobby)
            {
                StateManager.Instance.isLoading = false; 
                GameManager.Instance.UpdateLobbyList();    
            }
        }
        
        if (response.ContainsKey("operation"))
        {
            var operation = response["operation"] as string;
            Debug.Log($"OPERTATION: {operation}");
            switch (operation)
            {
                case "DISBANDED":
                {
                    var reason = jsonData["reason"] as Dictionary<string, object>;
                    if ((int) reason["code"] == ReasonCodes.RTT_ROOM_READY)
                    {
                        ConnectRelay();
                    }
                    else
                    {
                        // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                        CloseGame();
                    }

                    break;
                }
                case "STARTING":
                    // Save our picked color index
                    Settings.SetPlayerPrefColor(GameManager.Instance.CurrentUserInfo.UserGameColor);
                    
                    break;
                case "ROOM_READY":
                    StateManager.Instance.CurrentServer = new Server(jsonData);
                    GameManager.Instance.UpdateMatchList();
                    GameManager.Instance.UpdateCursorList();
                    StateManager.Instance.isLoading = false;
                    break;
            }
        }
    }
    
    // Connect to the Relay server and start the game
    void ConnectRelay()
    {
        m_bcWrapper.RelayService.RegisterRelayCallback(OnRelayMessage);
        m_bcWrapper.RelayService.RegisterSystemCallback(OnRelaySystemMessage);

        int port = 0;
        switch (StateManager.Instance.protocol)
        {
            case RelayConnectionType.WEBSOCKET:
                port = StateManager.Instance.CurrentServer.wsPort;
                break;
            case RelayConnectionType.TCP:
                port = StateManager.Instance.CurrentServer.tcpPort;
                break;
            case RelayConnectionType.UDP:
                port = StateManager.Instance.CurrentServer.udpPort;
                break;
        }

        Server server = StateManager.Instance.CurrentServer;
        m_bcWrapper.RelayService.Connect
        (
            StateManager.Instance.protocol,
            new RelayConnectOptions(false, server.host, port, server.passcode, server.lobbyId),
            OnRelayConnectSuccess, 
            LoggingInError, "Failed to connect to server"
        );
    }
    void OnRelayConnectSuccess(string jsonResponse, object cbObject)
    {
        //StateManager.Instance.ChangeState(GameStates.Match);
        Debug.Log("Relay Connection Success");
        GameManager.Instance.UpdateLobbyList();
        //State.form.UpdateGameViewport();
    }

    void OnRelayMessage(short netId, byte[] jsonResponse)
    {
        var memberProfileId = m_bcWrapper.RelayService.GetProfileIdForNetId(netId);
        string jsonMessage = Encoding.ASCII.GetString(jsonResponse);
        var json = JsonReader.Deserialize<Dictionary<string, object>>(jsonMessage);
        Lobby lobby = StateManager.Instance.CurrentLobby;
        foreach (var member in lobby.Members)
        {
            if (member.ID == memberProfileId)
            {
                var op = json["op"] as string;
                if (op == "move")
                {
                    var data = json["data"] as Dictionary<string, object>;

                    member.IsAlive = true;
                    member.MousePosition.x = (int)data["x"];
                    member.MousePosition.y = (int)data["y"];
                    Debug.Log("User Moved");
                }
                else if (op == "shockwave")
                {
                   
                    var data = json["data"] as Dictionary<string, object>;
                    Vector2 position = new Vector2((int) data["x"], (int) data["y"]);
                    GameManager.Instance.GameArea.SpawnShockwave(position);
                    Debug.Log("Shockwave summoned");
                }
                break;
            }
        }
    }

    void OnRelaySystemMessage(string jsonResponse)
    {
        var json = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        if (json["op"] as string == "DISCONNECT")
        {
            var profileId = json["profileId"] as string;
            Lobby lobby = StateManager.Instance.CurrentLobby;
            foreach (var member in lobby.Members)
            {
                if (member.ID == profileId)
                {
                    member.IsAlive = false;
                    GameManager.Instance.MemberLeft();
                    break;
                }
            }
        }
    }

    // RTT connected. Try to create or join a lobby
    void OnRTTConnected(string jsonResponse, object cbObject)
    {
        // Find lobby
        var algo = new Dictionary<string, object>();
        algo["strategy"] = "ranged-absolute";
        algo["alignment"] = "center";
        List<int> ranges = new List<int>();
        ranges.Add(1000);
        algo["ranges"] = ranges;

        //
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;

        //
        var filters = new Dictionary<string, object>();

        //
        var settings = new Dictionary<string, object>();

        //
        m_bcWrapper.LobbyService.FindOrCreateLobby
        (
            "CursorPartyV2", // lobby type
            0, // rating
            1, // max steps
            algo, // algorithm
            filters, // filters
            0, // Timeout
            false, // ready
            extra, // extra
            "all", // team code
            settings, // settings
            null, // other users
            null, // Success of lobby found will be in the event onLobbyEvent
            LoggingInError, "Failed to find lobby"
        );
    }

    void OnRTTDisconnected(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (jsonError == "DisableRTT Called") return; // Ignore
        LoggingInError(status, reasonCode, jsonError, cbObject);
    }

#endregion RTT Functions
}

