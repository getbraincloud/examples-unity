
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;

/// <summary>
/// Example of how to communicate game logic to brain cloud functions
/// </summary>

public class BrainCloudManager : MonoBehaviour
{
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;
    public bool LeavingGame;
    public BrainCloudWrapper Wrapper => m_bcWrapper;
    public static BrainCloudManager Instance;
    //Offset for the different mouse coordinates from Unity space to Nodejs space
    private float _mouseYOffset = 321;
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
            StateManager.Instance.AbortToSignIn($"Please provide a username");
            return;
        }
        if (password.IsNullOrEmpty())
        {
            StateManager.Instance.AbortToSignIn($"Please provide a password");
            return;
        }
        
        GameManager.Instance.CurrentUserInfo.Username = username;
        InitializeBC();
        // Authenticate with brainCloud
        m_bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, LogErrorThenPopUpWindow, "Login Failed");
    }

    private void FixedUpdate()
    {

        if (m_dead)
        {
            m_dead = false;
            
            UninitializeBC();
        }
    }

    public void InitializeBC()
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

    
#region BC Callbacks

    // User fully logged in. 
    void OnLoggedIn(string jsonResponse, object cbObject)
    {
        GameManager.Instance.UpdateMainMenuText();
        PlayerPrefs.SetString(Settings.PasswordKey,GameManager.Instance.PasswordInputField.text);
        StateManager.Instance.isLoading = false;
    }
    
    // User authenticated, handle the result
    void HandlePlayerState(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        var data = response["data"] as Dictionary<string, object>;
        var tempUsername = GameManager.Instance.CurrentUserInfo.Username;
        var userInfo = GameManager.Instance.CurrentUserInfo;
        userInfo = new UserInfo();
        userInfo.ID = data["profileId"] as string;
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            m_bcWrapper.PlayerStateService.UpdateName(tempUsername, OnLoggedIn, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        else
        {
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
            }
            m_bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnLoggedIn, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        GameManager.Instance.CurrentUserInfo = userInfo;
    }
    
    // Go back to login screen, with an error message
    void LogErrorThenPopUpWindow(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (m_dead) return;

        m_dead = true;
        m_bcWrapper.RTTService.DeregisterRTTLobbyCallback();
        m_bcWrapper.RelayService.DeregisterRelayCallback();
        m_bcWrapper.RelayService.DeregisterSystemCallback();
        m_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        m_bcWrapper.RTTService.DisableRTT();
        m_bcWrapper.Client.ResetCommunication();
        string message = cbObject as string;
        Debug.Log($"JSON ERROR: {jsonError}");
        Debug.Log($"MESSAGE: {message}");
        StateManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");

    }
#endregion BC Callbacks
    
#region GameFlow

    public void FindLobby(RelayConnectionType protocol)
    {
        StateManager.Instance.PROTOCOL = protocol;
        
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        
        // Enable RTT
        m_bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        m_bcWrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, OnRTTConnected, OnRTTDisconnected);
    }
    
    // Cleanly close the game. Go back to main menu but don't log 
    public void CloseGame(bool changeState = false)
    {
        m_bcWrapper.RelayService.DeregisterRelayCallback();
        m_bcWrapper.RelayService.DeregisterSystemCallback();
        m_bcWrapper.RelayService.Disconnect();
        
        m_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        m_bcWrapper.RTTService.DisableRTT();

        if (changeState)
        {
            StateManager.Instance.LeaveMatchBackToMenu();    
        }
    }
    
    // Ready up and signals RTT service we can start the game
    public void StartGame()
    {
        StateManager.Instance.isReady = true;
        
        //Setting up a update to send to brain cloud about local users color
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;

        //
        m_bcWrapper.LobbyService.UpdateReady(StateManager.Instance.CurrentLobby.LobbyID, StateManager.Instance.isReady, extra);
    }
    

    public void ReconnectUser()
    {
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        //Continue doing reconnection stuff.....
        m_bcWrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, RTTReconnect, OnRTTDisconnected);
        m_bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
    }

    private void RTTReconnect(string jsonResponse, object cbObject)
    {
        //Sending what users current color is
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;
        
        m_bcWrapper.LobbyService.JoinLobby
        (
            StateManager.Instance.CurrentLobby.LobbyID,
            true,
            extra,
            "all",
            null,
            null,
            LogErrorThenPopUpWindow
        );
    }

#endregion GameFlow

#region Input update

    // Local User moved mouse in the play area
    public void LocalMouseMoved(Vector2 pos)
    {
        GameManager.Instance.CurrentUserInfo.IsAlive = true;
        GameManager.Instance.CurrentUserInfo.MousePosition = pos;
        Lobby lobby = StateManager.Instance.CurrentLobby;
        foreach (var user in lobby.Members)
        {
            if (GameManager.Instance.CurrentUserInfo.ID == user.ID)
            {
                //Save it for later !
                user.IsAlive = true;
                user.MousePosition = pos;
                break;
            }
        }
        // Send to other players
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["x"] = pos.x;
        jsonData["y"] = -pos.y;// + _mouseYOffset;
        //Set up JSON to send
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
            Settings.GetChannel()
        );
    }

    // Local User summoned a shockwave in the play area
    public void LocalShockwave(Vector2 pos)
    {
        // Send to other players
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["x"] = pos.x;
        jsonData["y"] = -pos.y;

        Dictionary<string, object> json = new Dictionary<string, object>();
        json["op"] = "shockwave";
        json["data"] = jsonData;

        byte[] data = Encoding.ASCII.GetBytes(JsonWriter.Serialize(json));
        m_bcWrapper.RelayService.Send
        (
            data, 
            BrainCloudRelay.TO_ALL_PLAYERS, 
            true, // Reliable
            false, // Unordered
            Settings.GetChannel()
        );
   }

#endregion Input update

#region RTT functions

    //Getting input from other members
    public void OnRelayMessage(short netId, byte[] jsonResponse)
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
                    member.MousePosition.x = Convert.ToSingle(data["x"]);
                    member.MousePosition.y = -Convert.ToSingle(data["y"]); // + _mouseYOffset;
                }
                else if (op == "shockwave")
                {
                    var data = json["data"] as Dictionary<string, object>;
                    Vector2 position; 
                    position.x = Convert.ToSingle(data["x"]);
                    position.y = -Convert.ToSingle(data["y"]);
                    member.ShockwavePositions.Add(position);
                }
                break;
            }
        }
    }

    // We received a lobby event through RTT
    void OnLobbyEvent(string jsonResponse)
    {
        Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> jsonData = response["data"] as Dictionary<string, object>;

        // If there is a lobby object present in the message, update our lobby
        // state with it.
        if (jsonData.ContainsKey("lobby"))
        {
            StateManager.Instance.CurrentLobby = new Lobby(jsonData["lobby"] as Dictionary<string, object>,
                jsonData["lobbyId"] as string);
            //If we're still in lobby, then update the list of users
            if (StateManager.Instance.CurrentGameState == GameStates.Lobby)
            {
                GameManager.Instance.UpdateLobbyState();
                StateManager.Instance.isLoading = false;
            }
        }
        
        //Using the key "operation" to determine what state the lobby is in
        if (response.ContainsKey("operation"))
        {
            var operation = response["operation"] as string;
            switch (operation)
            {
                case "DISBANDED":
                {
                    var reason = jsonData["reason"] as Dictionary<string, object>;
                    if ((int) reason["code"] != ReasonCodes.RTT_ROOM_READY)
                    {
                        // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                        CloseGame(true);
                    }
                    break;
                }
                case "STARTING":
                    // Save our picked color index
                    Settings.SetPlayerPrefColor(GameManager.Instance.CurrentUserInfo.UserGameColor);
                    if (!GameManager.Instance.IsLocalUserHost())
                    {
                        StateManager.Instance.ButtonPressed_ChangeState(GameStates.Lobby);
                    }
                    break;
                case "ROOM_READY":
                    StateManager.Instance.CurrentServer = new Server(jsonData);
                    GameManager.Instance.UpdateMatchState();
                    GameManager.Instance.UpdateCursorList();
                    ConnectRelay();
                    StateManager.Instance.isLoading = false;
                    break;
            }
        }
    }
    
    // Connect to the Relay server and start the game
    public void ConnectRelay()
    {
        m_bcWrapper.RelayService.RegisterRelayCallback(OnRelayMessage);
        m_bcWrapper.RelayService.RegisterSystemCallback(OnRelaySystemMessage);

        int port = 0;
        switch (StateManager.Instance.PROTOCOL)
        {
            case RelayConnectionType.WEBSOCKET:
                port = StateManager.Instance.CurrentServer.WsPort;
                break;
            case RelayConnectionType.TCP:
                port = StateManager.Instance.CurrentServer.TcpPort;
                break;
            case RelayConnectionType.UDP:
                port = StateManager.Instance.CurrentServer.UdpPort;
                break;
        }

        Server server = StateManager.Instance.CurrentServer;
        m_bcWrapper.RelayService.Connect
        (
            StateManager.Instance.PROTOCOL,
            new RelayConnectOptions(false, server.Host, port, server.Passcode, server.LobbyId),
            null, 
            LogErrorThenPopUpWindow, 
            "Failed to connect to server"
        );
    }

    void OnRelaySystemMessage(string jsonResponse)
    {
        var json = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        if (json["op"] as string == "DISCONNECT")
        {
            if (json.ContainsKey("cxId"))
            {
                var profileId = json["cxId"] as string;
                profileId = profileId.Substring(6);
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
            LogErrorThenPopUpWindow, "Failed to find lobby"
        );
    }

    void OnRTTDisconnected(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (jsonError == "DisableRTT Called") return; // Ignore
        LogErrorThenPopUpWindow(status, reasonCode, jsonError, cbObject);
    }

#endregion RTT Functions
}

