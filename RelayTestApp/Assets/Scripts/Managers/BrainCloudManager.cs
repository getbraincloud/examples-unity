
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;
using TMPro;

public enum RelayCompressionTypes {JsonString, KeyValuePairString, DataStreamByte }

//Team codes for Free for all = all and team specific is alpha and beta
public enum TeamCodes {all, alpha, beta}
/// <summary>
/// Example of how to communicate game logic to brain cloud functions
/// </summary>

public class BrainCloudManager : MonoBehaviour
{
    private BrainCloudWrapper _bcWrapper;
    private bool _dead = false;
    public BrainCloudWrapper Wrapper => _bcWrapper;
    public static BrainCloudManager Instance;
    public TMP_Dropdown FreeForAllDropdown;
    public TMP_Dropdown TeamDropdown;
    internal RelayCompressionTypes _relayCompressionType { get; set; }
    private LogErrors _logger;
    private bool _presentWhileStarted;
    private bool _isReconnecting;
    public TeamCodes TeamCode { get; set; } = TeamCodes.all;

    private List<string> _ffaLobbyTypesList = new List<string>();
    private List<string> _teamLobbyTypesList = new List<string>();

    private string _currentFFALobby;
    private string _currentTeamLobby;
    
    private void Awake()
    {
        _logger = FindObjectOfType<LogErrors>();
        _bcWrapper = GetComponent<BrainCloudWrapper>();
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeBC();
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
        // Authenticate with brainCloud
        _bcWrapper.AuthenticateUniversal(username, password, true, HandlePlayerState, LogErrorThenPopUpWindow, "Login Failed");
    }
    
    public void AuthenticateReconnect()
    {
        _bcWrapper.Reconnect(HandlePlayerState, LogErrorThenPopUpWindow);
    }

    private void FixedUpdate()
    {
        if (_dead)
        {
            _dead = false;
            UninitializeBC();
        }
    }

    private void OnApplicationQuit()
    {
        if(_bcWrapper.Client.Authenticated)
        {
            _bcWrapper.LogoutOnApplicationQuit(false);
        }
    }

    public void InitializeBC()
    {
        _bcWrapper.Init();
    }
    // Uninitialize brainCloud
    void UninitializeBC()
    {
        if (_bcWrapper != null)
        {
            _bcWrapper.Client.ShutDown();
        }
    }

#region BC Callbacks

    // User fully logged in.
    void OnLoggedIn()
    {
        GameManager.Instance.UpdateMainMenuText();
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
        userInfo.ProfileID = data["profileId"] as string;
        // If no username is set for this user, ask for it
        if (!data.ContainsKey("playerName"))
        {
            // Update name for display
            _bcWrapper.PlayerStateService.UpdateName(tempUsername, OnUpdateName, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        else
        {
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
            }
            _bcWrapper.PlayerStateService.UpdateName(userInfo.Username, OnUpdateName, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        GameManager.Instance.CurrentUserInfo = userInfo;
        
        if(!GameManager.Instance.RememberMeToggle.isOn)
        {
            var profileID = _bcWrapper.GetStoredProfileId();
            _bcWrapper.ResetStoredProfileId();
            _bcWrapper.Client.AuthenticationService.ProfileId = profileID;
        }
    }
    
    private void OnUpdateName(string jsonResponse, object cbObject)
    {
        _bcWrapper.GlobalAppService.ReadProperties(OnReadProperties, LogErrorThenPopUpWindow);
    }
    
    private void OnReadProperties(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        if(data == null)
        {   
            Debug.LogWarning("Need to set up lobby types as a global properties in brainCloud portal. " +
                             "Refer to the README.md for an example under Relay Test App.");
            OnLoggedIn();
            return;
        }
        Dictionary<string, object> objectContainer = new Dictionary<string, object>();
        Dictionary<string, object> lobby = new Dictionary<string, object>();
        for (int i = 0; i < data.Count; i++)
        {
            var item = data.ElementAt(i);
            objectContainer[item.Key] = ((Dictionary<string, object>) item.Value)["value"];
            var lobbyData = JsonReader.Deserialize<Dictionary<string, object>>((string) objectContainer["AllLobbyTypes"]);
            for (int j = 0; j < lobbyData.Count; j++)
            {
                lobby = lobbyData[j.ToString()] as Dictionary<string, object>;
                string lobbyType = lobby["lobby"].ToString();
                if(lobbyType.Contains("Team"))
                {
                    _teamLobbyTypesList.Add(lobbyType);
                }
                else
                {
                    _ffaLobbyTypesList.Add(lobbyType);
                }
            }
        }
        GameManager.Instance.UpdateLobbyDropdowns(_ffaLobbyTypesList, _teamLobbyTypesList);
        OnLoggedIn();
    }

    // Go back to login screen, with an error message
    void LogErrorThenPopUpWindow(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (_dead) return;

        if (reasonCode == ReasonCodes.RS_ENDMATCH_REQUESTED)
        {
            return;
        }
        _isReconnecting = false;
        _dead = true;
        StateManager.Instance.SessionPlayers.Clear();
        _bcWrapper.RTTService.DeregisterRTTLobbyCallback();
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        _bcWrapper.RTTService.DisableRTT();
        _bcWrapper.Client.ResetCommunication();
        string message = cbObject as string;
        Debug.Log($"JSON ERROR: {jsonError}");
        Debug.Log($"MESSAGE: {message}");
        StateManager.Instance.AbortToSignIn($"Message: {message} |||| JSON: {jsonError}");

    }
#endregion BC Callbacks

#region GameFlow

    public void FindLobby(RelayConnectionType protocol)
    {
        StateManager.Instance.SessionPlayers.Clear();
        StateManager.Instance.Protocol = protocol;
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        _isReconnecting = false;
        // Enable RTT
        _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        _bcWrapper.RTTService.EnableRTT(OnRTTConnected, OnRTTDisconnected);
    }

    // Cleanly close the game. Go back to main menu but don't log
    public void CloseGame(bool changeState = false)
    {
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RelayService.Disconnect();

        _bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        _bcWrapper.RTTService.DisableRTT();

        if (changeState)
        {
            StateManager.Instance.LeaveMatchBackToMenu();
            GameManager.Instance.ClearMatchEntries();
        }
    }

    // Ready up and signals RTT service we can start the game
    public void StartGame()
    {
        StateManager.Instance.isReady = true;

        //Setting up a update to send to brain cloud about local users color
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;
        extra["presentSinceStart"] = GameManager.Instance.CurrentUserInfo.PresentSinceStart;

        //
        _bcWrapper.LobbyService.UpdateReady(StateManager.Instance.CurrentLobby.LobbyID, true, extra);
    }

    public void EndMatch()
    {
        GameManager.Instance.UpdateLobbyState();
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["cxId"] = _bcWrapper.Client.RTTConnectionID;
        json["lobbyId"] = StateManager.Instance.CurrentLobby.LobbyID;
        json["op"] = "END_MATCH";
        _bcWrapper.RelayService.EndMatch(json);
    }

    public void ReconnectUserToLobby()
    {
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        _isReconnecting = true;
        //Continue doing reconnection stuff.....
        _bcWrapper.RTTService.EnableRTT(RTTReconnect, OnRTTDisconnected);
        _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
    }

    public void JoinMatch()
    {
        StateManager.Instance.ButtonPressed_ChangeState(GameStates.Lobby);
        GameManager.Instance.JoinInProgressButton.gameObject.SetActive(false);
        ConnectRelay();
    }

    private void RTTReconnect(string jsonResponse, object cbObject)
    {
        //Sending what users current color is
        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;

        _bcWrapper.LobbyService.JoinLobby
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
            if (GameManager.Instance.CurrentUserInfo.ProfileID == user.ProfileID)
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
        jsonData["y"] = pos.y;
        //Set up JSON to send
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["op"] = "move";
        json["data"] = jsonData;

        SendWithSpecificCompression
        (
            json,
            Settings.GetPlayerPrefBool(Settings.ReliableKey),
            Settings.GetPlayerPrefBool(Settings.OrderedKey),
            Settings.GetChannel()
        );
    }

    // Local User summoned a splatter in the play area
    public void LocalSplatter(Vector2 pos)
    {
        SendWithSpecificCompression
        (
            CreateSplatterJson(pos, TeamCodes.all),
            true,
            false,
            Settings.GetChannel()
        );
    }

    public void SendSplatterToAll(Vector2 pos)
    {
        SendToSpecificTeamWithCompression
        (
            CreateSplatterJson(pos, TeamCodes.all),
            TeamCodes.all,
            true,
            false,
            Settings.GetChannel()
        );
    }

    public void SendSplatterToTeam(Vector2 pos)
    {
        TeamCodes teamToSend = GameManager.Instance.CurrentUserInfo.Team;
        SendToSpecificTeamWithCompression
        (
            CreateSplatterJson(pos, teamToSend),
            teamToSend,
            true,
            false,
            Settings.GetChannel()
        );
    }

    public void SendSplatterToOpponents(Vector2 pos)
    {
        TeamCodes TeamToSend = GameManager.Instance.CurrentUserInfo.Team == TeamCodes.alpha
            ? TeamCodes.beta
            : TeamCodes.alpha;
        SendToSpecificTeamWithCompression
        (
            CreateSplatterJson(pos, TeamToSend),
            TeamToSend,
            true,
            false,
            Settings.GetChannel()
        );
    }

    private Dictionary<string, object> CreateSplatterJson(Vector2 pos, TeamCodes intendedTeam)
    {
        // Send to other players
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["x"] = pos.x;
        jsonData["y"] = pos.y;
        jsonData["teamCode"] = (int)intendedTeam;
        jsonData["instigator"] = (int)GameManager.Instance.CurrentUserInfo.Team;

        Dictionary<string, object> json = new Dictionary<string, object>();
        json["op"] = "shockwave";
        json["data"] = jsonData;

        return json;
    }

    private void SendWithSpecificCompression(Dictionary<string, object> in_dict, bool in_reliable = true, bool in_ordered = true, int in_channel = 0, char in_joinChar = '=', char in_splitChar = ';')
    {
        string jsonData;
        byte[] jsonBytes = {0x0};
        switch (_relayCompressionType)
        {
            case RelayCompressionTypes.JsonString:
                jsonData = JsonWriter.Serialize(in_dict);
                jsonBytes = Encoding.ASCII.GetBytes(jsonData);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
            case RelayCompressionTypes.KeyValuePairString:
                jsonData = SerializeDict(in_dict, in_joinChar, in_splitChar);
                jsonBytes = Encoding.ASCII.GetBytes(jsonData);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
            case RelayCompressionTypes.DataStreamByte:
                jsonData = JsonWriter.Serialize(in_dict);
                jsonBytes = SerializeDict(in_dict);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
        }
    }

    private void SendToSpecificTeamWithCompression(Dictionary<string, object> in_dict,TeamCodes teamToSend, bool in_reliable = true,
        bool in_ordered = true, int in_channel = 0, char in_joinChar = '=', char in_splitChar = ';')
    {
        string jsonData;
        byte[] jsonBytes = {0x0};
        List<int> netIDsToSend = new List<int>();

        if (teamToSend != TeamCodes.all)
        {
            foreach (UserInfo member in StateManager.Instance.CurrentLobby.Members)
            {
                if (member.Team == teamToSend)
                {
                    int netID = _bcWrapper.RelayService.GetNetIdForCxId(member.cxId);
                    netIDsToSend.Add(netID);
                }
            }
        }
        switch (_relayCompressionType)
        {
            case RelayCompressionTypes.JsonString:
                jsonData = JsonWriter.Serialize(in_dict);
                jsonBytes = Encoding.ASCII.GetBytes(jsonData);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                if (teamToSend == TeamCodes.all)
                {
                    _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                }
                else
                {
                    for (int i = 0; i < netIDsToSend.Count; ++i)
                    {
                        _bcWrapper.RelayService.Send(jsonBytes, (ulong)netIDsToSend[i], in_reliable, in_ordered, in_channel);
                    }
                }
                break;
            case RelayCompressionTypes.KeyValuePairString:
                jsonData = SerializeDict(in_dict, in_joinChar, in_splitChar);
                jsonBytes = Encoding.ASCII.GetBytes(jsonData);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                if (teamToSend == TeamCodes.all)
                {
                    _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                }
                else
                {
                    for (int i = 0; i < netIDsToSend.Count; ++i)
                    {
                        _bcWrapper.RelayService.Send(jsonBytes, (ulong)netIDsToSend[i], in_reliable, in_ordered, in_channel);
                    }
                }
                break;
            case RelayCompressionTypes.DataStreamByte:
                jsonData = JsonWriter.Serialize(in_dict);
                jsonBytes = SerializeDict(in_dict);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                if (teamToSend == TeamCodes.all)
                {
                    _bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                }
                else
                {
                    for (int i = 0; i < netIDsToSend.Count; ++i)
                    {
                        _bcWrapper.RelayService.Send(jsonBytes, (ulong)netIDsToSend[i], in_reliable, in_ordered, in_channel);
                    }
                }
                break;
        }
    }

    public void SwitchTeams()
    {
        if (GameManager.Instance.CurrentUserInfo.Team == TeamCodes.alpha)
        {
            GameManager.Instance.CurrentUserInfo.Team = TeamCodes.beta;
        }
        else
        {
            GameManager.Instance.CurrentUserInfo.Team = TeamCodes.alpha;
        }
        //On success is null because we will get an update from RTT about the switch
        _bcWrapper.LobbyService.SwitchTeam
        (
            StateManager.Instance.CurrentLobby.LobbyID,
            GameManager.Instance.CurrentUserInfo.Team.ToString(),
            null,
            LogErrorThenPopUpWindow
        );
    }


#endregion Input update

#region RTT functions

    //Getting input from other members
    public void OnRelayMessage(short netId, byte[] jsonResponse)
    {
        var memberProfileId = _bcWrapper.RelayService.GetProfileIdForNetId(netId);

        var json = DeserializeString(jsonResponse);
        Lobby lobby = StateManager.Instance.CurrentLobby;
        foreach (var member in lobby.Members)
        {
            switch (_relayCompressionType)
            {
                case RelayCompressionTypes.JsonString:
                    if (member.ProfileID == memberProfileId)
                    {
                        var data = json["data"] as Dictionary<string, object>;
                        if (data == null)
                        {
                            Debug.LogWarning("On Relay Message is null !");
                            break;
                        }
                        var op = json["op"] as string;
                        if (op == "move")
                        {
                            member.IsAlive = true;
                            float mousePosX = (float)Convert.ToDouble(data["x"]);
                            float mousePosY = (float)Convert.ToDouble(data["y"]);

                            member.MousePosition.y = mousePosY;
                            member.MousePosition.x = mousePosX;
                        }
                        else if (op == "shockwave")
                        {
                            Vector2 position;
                            position.x = (float)Convert.ToDouble(data["x"]);
                            position.y = (float)Convert.ToDouble(data["y"]);
                            member.SplatterPositions.Add(position);
                            if(data.ContainsKey("teamCode"))
                            {
                                TeamCodes splatterCode = (TeamCodes) data["teamCode"];
                                member.SplatterTeamCodes.Add(splatterCode);

                                TeamCodes instigatorCode = (TeamCodes) data["instigator"];
                                member.InstigatorTeamCodes.Add(instigatorCode);   
                            }
                        }
                    }
                    break;
                case RelayCompressionTypes.DataStreamByte:
                case RelayCompressionTypes.KeyValuePairString:
                    if (member.ProfileID == memberProfileId)
                    {
                        var op = json["op"] as string;
                        if (op == "move")
                        {
                            member.IsAlive = true;
                            member.MousePosition.x = (float)Convert.ToDouble(json["x"]);
                            member.MousePosition.y = (float)-Convert.ToDouble(json["y"]);
                        }
                        else if (op == "shockwave")
                        {
                            Vector2 position;
                            position.x = (float)Convert.ToDouble(json["x"]);
                            position.y = (float)-Convert.ToDouble(json["y"]);
                            member.SplatterPositions.Add(position);

                            TeamCodes splatterCode = (TeamCodes) json["teamCode"];
                            member.SplatterTeamCodes.Add(splatterCode);

                            TeamCodes instigatorCode = (TeamCodes) json["instigator"];
                            member.InstigatorTeamCodes.Add(instigatorCode);
                        }
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
                StateManager.Instance.isLoading = false;
            }
            GameManager.Instance.UpdateMatchAndLobbyState();
        }

        //Using the key "operation" to determine what state the lobby is in
        if (response.ContainsKey("operation"))
        {
            var operation = response["operation"] as string;
            switch (operation)
            {
                case "MEMBER_JOIN":
                    var lobby = jsonData["lobby"] as Dictionary<string, object>;
                    var lobbyTypeDef = lobby["lobbyTypeDef"] as Dictionary<string, object>;
                    if (lobbyTypeDef == null || !lobbyTypeDef.ContainsKey("roomConfig"))
                    {
                        StateManager.Instance.UpdateDisconnectButtons(false);
                        return;
                    }
                    var roomConfig = lobbyTypeDef["roomConfig"] as Dictionary<string, object>;
                    
                    //These buttons are for testing a disconnect from internet scenario.
                    //One button will disconnect everything and then the other button is
                    //to re-initialize and re-authenticate and then join back to the same room
                    //the User was disconnected from. To set this up for your app, go to your
                    //lobby settings(Design->Multiplayer->Lobbies) and add 
                    //{"enableDisconnectButton":true} to the Custom Config to your lobby.
                    if(roomConfig != null && roomConfig.ContainsKey("enableDisconnectButton"))
                    {
                        bool buttonStatus = (bool)roomConfig["enableDisconnectButton"];
                        StateManager.Instance.UpdateDisconnectButtons(buttonStatus);                        
                    }
                    else
                    {
                        StateManager.Instance.UpdateDisconnectButtons(false);
                    }
                    break;
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
                    _presentWhileStarted = true;
                    GameManager.Instance.UpdatePresentSinceStart();
                    Settings.SetPlayerPrefColor(GameManager.Instance.CurrentUserInfo.UserGameColor);
                    if (!GameManager.Instance.IsLocalUserHost())
                    {
                        StateManager.Instance.ButtonPressed_ChangeState(GameStates.Lobby);
                    }
                    break;
                case "ROOM_READY":
                    StateManager.Instance.CurrentServer = new Server(jsonData);
                    GameManager.Instance.UpdateMatchAndLobbyState();
                    GameManager.Instance.UpdateCursorList();
                    //Check to see if a user joined the lobby before the match started or after.
                    //If a user joins while match is in progress, you will only receive MEMBER_JOIN & ROOM_READY RTT updates.
                    if (_presentWhileStarted || _isReconnecting)
                    {
                        ConnectRelay();
                    }
                    else
                    {
                        GameManager.Instance.JoinInProgressButton.gameObject.SetActive(true);
                    }
                    break;
            }
        }
    }

    // Connect to the Relay server and start the game
    public void ConnectRelay()
    {
        _presentWhileStarted = false;
        _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        _bcWrapper.RelayService.RegisterRelayCallback(OnRelayMessage);
        _bcWrapper.RelayService.RegisterSystemCallback(OnRelaySystemMessage);

        int port = 0;
        switch (StateManager.Instance.Protocol)
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
        _bcWrapper.RelayService.Connect
        (
            StateManager.Instance.Protocol,
            new RelayConnectOptions(false, server.Host, port, server.Passcode, server.LobbyId),
            null,
            LogErrorThenPopUpWindow,
            "Failed to connect to server"
        );
    }
    
    public void DisconnectFromEverything()
    {
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RelayService.Disconnect();
        _bcWrapper.RTTService.DisableRTT();
        _bcWrapper.Client.ResetCommunication();
    }
    
    public void DisconnectFromRelay()
    {
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RelayService.Disconnect();
    }

    public void ReauthenticateAndReconnectToRelay()
    {
        string username = GameManager.Instance.UsernameInputField.text;
        string password = GameManager.Instance.PasswordInputField.text;
        
        _bcWrapper.AuthenticateUniversal(username, password, true, OnReAuthenticateSuccess, LogErrorThenPopUpWindow, "Login Failed");
    }
    
    public void ReconnectToRelay()
    {
        ConnectRelay();
    }
    
    private void OnReAuthenticateSuccess(string response, object cbObject)
    {
        _bcWrapper.RTTService.EnableRTT(OnReEnableRTT, LogErrorThenPopUpWindow);
    }
    
    private void OnReEnableRTT(string response, object cbObject)
    {
        ConnectRelay();
    }
    
    public void Logout()
    {
        _bcWrapper.Logout(true);
        GameManager.Instance.UsernameInputField.text = "";
        GameManager.Instance.PasswordInputField.text = "";
        PlayerPrefs.DeleteAll();
        StateManager.Instance.ChangeState(GameStates.SignIn);
    }

    void OnRelaySystemMessage(string jsonResponse)
    {
        var json = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        if (json["op"] as string == "DISCONNECT")
        {
            if (json.ContainsKey("cxId"))
            {
                var profileId = json["cxId"] as string;
                Lobby lobby = StateManager.Instance.CurrentLobby;
                profileId = lobby.FormatCxIdToProfileId(profileId);
                foreach (var member in lobby.Members)
                {
                    if (member.ProfileID == profileId)
                    {
                        member.IsAlive = false;
                        GameManager.Instance.UpdateMatchAndLobbyState();
                        break;
                    }
                }
            }
        }
        else if (json["op"] as string == "CONNECT")
        {
            StateManager.Instance.isLoading = false;
            var cxId = json["cxId"] as string;
            StateManager.Instance.CheckPlayerReconnecting(cxId);
            //Check if user connected is new, if so update name to not have "In Lobby"
            GameManager.Instance.UpdateMatchState();
        }
        else if (json["op"] as string == "END_MATCH")
        {
            StateManager.Instance.isReady = false;
            GameManager.Instance.CurrentUserInfo.PresentSinceStart = false;
            GameManager.Instance.UpdateMatchAndLobbyState();
            StateManager.Instance.ChangeState(GameStates.Lobby);
        }
        else if (json["op"] as string == "MIGRATE_OWNER")
        {
            StateManager.Instance.CurrentLobby.ReassignOwnerID(_bcWrapper.RelayService.OwnerCxId);
            GameManager.Instance.UpdateMatchAndLobbyState();
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

        string teamCode = GameManager.Instance.GameMode == GameMode.FreeForAll ? "all" : "";
        string lobbyType = "";
        if (GameManager.Instance.GameMode == GameMode.FreeForAll)
        {
            lobbyType = _currentFFALobby;
        }
        else
        {
            lobbyType = _currentTeamLobby;
        }

        //
        _bcWrapper.LobbyService.FindOrCreateLobby
        (
            lobbyType,
            0, // rating
            1, // max steps
            algo, // algorithm
            filters, // filters
            0, // Timeout
            false, // ready
            extra, // extra
            teamCode, // team code
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

    private Dictionary<string, object> DeserializeString(byte[] in_data, char in_joinChar = '=', char in_splitChar = ';')
    {
        Dictionary<string, object> toDict = new Dictionary<string, object>();
        string jsonMessage = Encoding.ASCII.GetString(in_data);
        if (jsonMessage == "") return toDict;

        switch (_relayCompressionType)
        {
            case RelayCompressionTypes.JsonString:
                try
                {
                    toDict = (Dictionary<string, object>)JsonReader.Deserialize(jsonMessage);
                }
                catch (Exception)
                {
                    Debug.LogWarning("COULD NOT SERIALIZE " + jsonMessage);
                }
                break;
            case RelayCompressionTypes.DataStreamByte:
                RelayInfo info = ByteArrayToStructure<RelayInfo>(in_data);
                toDict.Add("op", info.Operation);
                toDict.Add("x", info.PositionX);
                toDict.Add("y", info.PositionY);
                break;
            case RelayCompressionTypes.KeyValuePairString:
                string[] splitItems = jsonMessage.Split(in_splitChar);
                int indexOf = -1;
                foreach (string item in splitItems)
                {
                    indexOf = item.IndexOf(in_joinChar);
                    if (indexOf >= 0)
                    {
                        toDict[item.Substring(0, indexOf)] = item.Substring(indexOf + 1);
                    }
                }
                break;
        }
        return toDict;
    }

    private string SerializeDict(Dictionary<string, object> in_dict, char in_joinChar = '=', char in_splitChar = ';')
    {
        string toString = "";
        string toSubString = "";
        foreach (string key in in_dict.Keys)
        {
            if (in_dict[key] != null)
            {
                Dictionary<string, object> data = in_dict[key] as Dictionary<string, object>;
                if (data != null)
                {
                    foreach (string dataKey in data.Keys)
                    {
                        toSubString += dataKey + in_joinChar + data[dataKey] + in_splitChar;
                    }
                }
                else
                {
                    toString += key + in_joinChar + in_dict[key] + in_splitChar;
                }
            }
        }
        return toString + toSubString;
    }

    private static byte[] EMPTY_ARRAY = new byte[0];

    [StructLayout(LayoutKind.Sequential)]
    struct RelayInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public string Operation;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public float PositionX;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
        public float PositionY;
    }

    private byte[] SerializeDict(Dictionary<string, object> in_dict)
    {
        RelayInfo relayInfo;
        relayInfo.Operation = in_dict["op"] as string;
        Dictionary<string, object> data = in_dict["data"] as Dictionary<string, object>;
        relayInfo.PositionX = (float) data["x"];
        relayInfo.PositionY = (float) data["y"];
        try
        {
            byte[] toReturn = StructureToByteArray(relayInfo);
            return toReturn;
        }
        catch (Exception)
        {
            return EMPTY_ARRAY;
        }
    }

    private byte[] StructureToByteArray<T>(T str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        GCHandle h = default(GCHandle);
        try
        {
            h = GCHandle.Alloc(arr, GCHandleType.Pinned);
            Marshal.StructureToPtr<T>(str, h.AddrOfPinnedObject(), false);
        }
        finally
        {
            if (h.IsAllocated)
            {
                h.Free();
            }
        }

        return arr;
    }

    public static T ByteArrayToStructure<T>(byte[] arr) where T : struct
    {
        T str = default(T);
        if (arr.Length != Marshal.SizeOf(str))
        {
            throw new InvalidOperationException("WRONG SIZE STRUCTURE COPY");
        }
        GCHandle h = default(GCHandle);
        try
        {
            h = GCHandle.Alloc(arr, GCHandleType.Pinned);
            str = Marshal.PtrToStructure<T>(h.AddrOfPinnedObject());
        }
        finally
        {
            if (h.IsAllocated)
            {
                h.Free();
            }
        }
        return str;
    }

    public void SetLobbyType(GameMode in_gameMode, int index)
    {
        if(in_gameMode == GameMode.Team)
        {
            _currentTeamLobby = _teamLobbyTypesList[index];            
        }
        else
        {
            _currentFFALobby = _ffaLobbyTypesList[index];            
        }
    }
}
