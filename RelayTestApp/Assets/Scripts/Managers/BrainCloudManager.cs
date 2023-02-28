
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BrainCloud.JsonFx.Json;
using UnityEngine;
using BrainCloud;
using BrainCloud.UnityWebSocketsForWebGL.WebSocketSharp;

public enum RelayCompressionTypes {JsonString, KeyValuePairString, DataStreamByte }

/// <summary>
/// Example of how to communicate game logic to brain cloud functions
/// </summary>

public class BrainCloudManager : MonoBehaviour
{
    private BrainCloudWrapper m_bcWrapper;
    private bool m_dead = false;
    public BrainCloudWrapper Wrapper => m_bcWrapper;
    public static BrainCloudManager Instance;
    internal RelayCompressionTypes _relayCompressionType { get; set; }
    private LogErrors _logger;
    private bool _presentWhileStarted;
    public bool PresentWhileStarted
    {
        get => _presentWhileStarted;
    }
    private void Awake()
    {
        _logger = FindObjectOfType<LogErrors>();
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

        if (reasonCode == ReasonCodes.RS_ENDMATCH_REQUESTED)
        {
            return;
        }

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
        StateManager.Instance.Protocol = protocol;
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

        //
        m_bcWrapper.LobbyService.UpdateReady(StateManager.Instance.CurrentLobby.LobbyID, true, extra);
    }

    public void EndMatch()
    {
        GameManager.Instance.UpdateLobbyState();
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["cxId"] = m_bcWrapper.Client.RTTConnectionID;
        json["lobbyId"] = StateManager.Instance.CurrentLobby.LobbyID;
        json["op"] = "END_MATCH";
        m_bcWrapper.RelayService.EndMatch(json);
    }

    public void ReconnectUser()
    {
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        //Continue doing reconnection stuff.....
        m_bcWrapper.RTTService.EnableRTT(RTTConnectionType.WEBSOCKET, RTTReconnect, OnRTTDisconnected);
        m_bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
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
        jsonData["y"] = -pos.y;
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
        
        SendWithSpecificCompression
        (
            json,
            true,
            false,
            Settings.GetChannel()
        );
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
                m_bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
            case RelayCompressionTypes.KeyValuePairString:
                jsonData = SerializeDict(in_dict, in_joinChar, in_splitChar); 
                jsonBytes = Encoding.ASCII.GetBytes(jsonData);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                m_bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
            case RelayCompressionTypes.DataStreamByte:
                jsonData = JsonWriter.Serialize(in_dict);
                jsonBytes = SerializeDict(in_dict);
                _logger?.WriteGameplayInput(jsonData, jsonBytes);
                m_bcWrapper.RelayService.Send(jsonBytes, BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
                break;
        }
    }

#endregion Input update

#region RTT functions

    //Getting input from other members
    public void OnRelayMessage(short netId, byte[] jsonResponse)
    {
        var memberProfileId = m_bcWrapper.RelayService.GetProfileIdForNetId(netId);
        
        var json = DeserializeString(jsonResponse);
        Lobby lobby = StateManager.Instance.CurrentLobby;
        foreach (var member in lobby.Members)
        {
            switch (_relayCompressionType)
            {
                case RelayCompressionTypes.JsonString:
                    if (member.ID == memberProfileId)
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
                            member.MousePosition.x = Convert.ToSingle(data["x"]);
                            member.MousePosition.y = -Convert.ToSingle(data["y"]); // + _mouseYOffset;
                        }
                        else if (op == "shockwave")
                        {
                            Vector2 position; 
                            position.x = Convert.ToSingle(data["x"]);
                            position.y = -Convert.ToSingle(data["y"]);
                            member.ShockwavePositions.Add(position);
                        }
                    }
                    break;
                case RelayCompressionTypes.DataStreamByte:
                case RelayCompressionTypes.KeyValuePairString:
                    if (member.ID == memberProfileId)
                    {
                        var op = json["op"] as string;
                        if (op == "move")
                        {
                            member.IsAlive = true;
                            member.MousePosition.x = Convert.ToSingle(json["x"]);
                            member.MousePosition.y = -Convert.ToSingle(json["y"]); // + _mouseYOffset;
                        }
                        else if (op == "shockwave")
                        {
                            Vector2 position; 
                            position.x = Convert.ToSingle(json["x"]);
                            position.y = -Convert.ToSingle(json["y"]);
                            member.ShockwavePositions.Add(position);
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
                    if (_presentWhileStarted)
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
        m_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        m_bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        m_bcWrapper.RelayService.RegisterRelayCallback(OnRelayMessage);
        m_bcWrapper.RelayService.RegisterSystemCallback(OnRelaySystemMessage);

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
        m_bcWrapper.RelayService.Connect
        (
            StateManager.Instance.Protocol,
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
                Lobby lobby = StateManager.Instance.CurrentLobby;
                profileId = lobby.FormatCxIdToProfileId(profileId);
                foreach (var member in lobby.Members)
                {
                    if (member.ID == profileId)
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
            //Check if user connected is new, if so update name to not have "In Lobby" 
            GameManager.Instance.UpdateMatchState();
        }
        else if (json["op"] as string == "END_MATCH")
        {
            StateManager.Instance.isReady = false;
            GameManager.Instance.UpdateMatchAndLobbyState();
            StateManager.Instance.ChangeState(GameStates.Lobby);
        }
        else if (json["op"] as string == "MIGRATE_OWNER")
        {
            StateManager.Instance.CurrentLobby.ReassignOwnerID(m_bcWrapper.RelayService.OwnerCxId);
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

        //
        m_bcWrapper.LobbyService.FindOrCreateLobby
        (
            "CursorPartyV2Backfill",//"CursorPartyV2", // lobby type
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
}