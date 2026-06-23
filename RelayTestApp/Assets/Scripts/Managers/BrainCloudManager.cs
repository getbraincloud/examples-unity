
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
using Object = System.Object;

public enum RelayCompressionTypes { JsonString, KeyValuePairString, DataStreamByte }

//Team codes for Free for all = all and team specific is alpha and beta
public enum TeamCodes { all, alpha, beta }
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

    private string currentEntryId;

    private static List<Color> colours = new List<Color>();
    private bool _noServerSelected;

    private Dictionary<string, int> _pingData = new Dictionary<string, int>();

    // Lobby / server join timers — drive the loading-screen sub-message each FixedUpdate
    private long _lobbySearchStartTime = 0;   // set when FindOrCreateLobby is called
    private long _lobbyStatusStartTime = 0;   // set on STARTING lobby event
    private string _progressMessage = "";  // latest roomProgressUpdate text

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

        // Update the loading-screen timer while the connecting overlay is visible.
        // we may also need to user _lobbyStatusStartTime
        if ((_lobbySearchStartTime > 0 || _lobbyStatusStartTime > 0) && StateManager.Instance != null && StateManager.Instance.isLoading)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long elapsed = now - (_presentWhileStarted ? _lobbyStatusStartTime : _lobbySearchStartTime);
            double secs = elapsed / 1000.0;

            string timerStr = $"{secs:0} s";
            string msg = _progressMessage.Length > 0
                ? $"{_progressMessage} {timerStr}"
                : timerStr;
            StateManager.Instance.LoadingGameState.UpdateSubMessage(msg);
        }

    }

    private void OnApplicationQuit()
    {
        if (_bcWrapper.Client.Authenticated)
        {
            _bcWrapper.LogoutOnApplicationQuit(false);
        }
    }

    public void InitializeBC()
    {
        string tag = "";
#if UNITY_EDITOR
    var tags = Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags();
    if (tags != null && tags.Length > 0)
        tag = tags[0];
#endif
        _bcWrapper.WrapperName = "RelayTestApp_" + tag;
        UnityEngine.Debug.Log("WrapperName: " + _bcWrapper.WrapperName);
        _bcWrapper.Init();
    }

    public void OnEnable()
    {
        Invoke("AutoSignIn", 0.15f);
    }
    private void AutoSignIn()
    {
        StateManager.Instance.AutoSignIn();
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

    // 40-colour palette aligned with C#/Java/JS/C++ RelayTestApp implementations.
    // Row 0 (0-9): vivid  |  Row 1 (10-19): vivid-medium  |  Row 2 (20-29): pastel  |  Row 3 (30-39): muted
    private static readonly Color[] s_palette = new Color[]
    {
        // Row 0 — vivid
        new Color(0xFF/255f, 0x33/255f, 0x33/255f), // 0  vivid red
        new Color(0xFF/255f, 0x88/255f, 0x00/255f), // 1  vivid orange
        new Color(0xFF/255f, 0xD7/255f, 0x00/255f), // 2  gold
        new Color(0x88/255f, 0xFF/255f, 0x00/255f), // 3  vivid lime
        new Color(0x00/255f, 0xEE/255f, 0x44/255f), // 4  vivid green
        new Color(0x00/255f, 0xDD/255f, 0xDD/255f), // 5  vivid cyan
        new Color(0x00/255f, 0xAA/255f, 0xFF/255f), // 6  vivid sky blue
        new Color(0x33/255f, 0x55/255f, 0xFF/255f), // 7  vivid blue (default)
        new Color(0xAA/255f, 0x00/255f, 0xFF/255f), // 8  vivid purple
        new Color(0xFF/255f, 0x00/255f, 0xBB/255f), // 9  vivid magenta

        // Row 1 — vivid-medium
        new Color(0xFF/255f, 0x55/255f, 0x66/255f), // 10 coral
        new Color(0xFF/255f, 0xAA/255f, 0x00/255f), // 11 amber
        new Color(0xAA/255f, 0xDD/255f, 0x00/255f), // 12 yellow-green
        new Color(0x00/255f, 0xFF/255f, 0x88/255f), // 13 spring green
        new Color(0x00/255f, 0xFF/255f, 0xCC/255f), // 14 aqua
        new Color(0x00/255f, 0x88/255f, 0xFF/255f), // 15 azure
        new Color(0x88/255f, 0x33/255f, 0xFF/255f), // 16 violet
        new Color(0xFF/255f, 0x44/255f, 0xAA/255f), // 17 hot pink
        new Color(0x77/255f, 0xFF/255f, 0x33/255f), // 18 chartreuse
        new Color(0xFF/255f, 0x66/255f, 0x88/255f), // 19 rose

        // Row 2 — pastel
        new Color(0xFF/255f, 0x99/255f, 0x99/255f), // 20 light red
        new Color(0xFF/255f, 0xCC/255f, 0x88/255f), // 21 peach
        new Color(0xFF/255f, 0xFF/255f, 0x88/255f), // 22 pale yellow
        new Color(0xAA/255f, 0xFF/255f, 0xAA/255f), // 23 pale green
        new Color(0x88/255f, 0xFF/255f, 0xEE/255f), // 24 pale cyan
        new Color(0xAA/255f, 0xBB/255f, 0xFF/255f), // 25 periwinkle
        new Color(0xDD/255f, 0xBB/255f, 0xFF/255f), // 26 lavender
        new Color(0xFF/255f, 0xBB/255f, 0xDD/255f), // 27 light pink
        new Color(0xCC/255f, 0xFF/255f, 0xDD/255f), // 28 mint
        new Color(0xFF/255f, 0xEE/255f, 0xCC/255f), // 29 cream

        // Row 3 — muted
        new Color(0xCC/255f, 0x11/255f, 0x33/255f), // 30 crimson
        new Color(0xCC/255f, 0x55/255f, 0x00/255f), // 31 burnt orange
        new Color(0x88/255f, 0xAA/255f, 0x00/255f), // 32 olive
        new Color(0x22/255f, 0x88/255f, 0x55/255f), // 33 forest green
        new Color(0x00/255f, 0x99/255f, 0x99/255f), // 34 deep teal
        new Color(0x33/255f, 0x66/255f, 0xAA/255f), // 35 steel blue
        new Color(0x77/255f, 0x44/255f, 0xCC/255f), // 36 medium purple
        new Color(0xAA/255f, 0x33/255f, 0x66/255f), // 37 dark rose
        new Color(0xAA/255f, 0x66/255f, 0x33/255f), // 38 brown
        new Color(0x77/255f, 0x88/255f, 0xAA/255f), // 39 slate
    };

    private void PopulateHardcodedColours()
    {
        colours.Clear();
        colours.AddRange(s_palette);
        GameManager.Instance.UpdateColorList(colours);
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
            _bcWrapper.PlayerStateService.UpdateUserName(tempUsername, null, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        else
        {
            userInfo.Username = data["playerName"] as string;
            if (userInfo.Username.IsNullOrEmpty())
            {
                userInfo.Username = tempUsername;
            }
            _bcWrapper.PlayerStateService.UpdateUserName(userInfo.Username, null, LogErrorThenPopUpWindow,
                "Failed to update username to braincloud");
        }
        GameManager.Instance.CurrentUserInfo = userInfo;

        if (!GameManager.Instance.RememberMeToggle.isOn)
        {
            var profileID = _bcWrapper.GetStoredProfileId();
            _bcWrapper.ResetStoredProfileId();
            _bcWrapper.Client.AuthenticationService.ProfileId = profileID;
        }

        if (colours.Count == 0)
        {
            // Hardcoded palette — identical across all RelayTestApp clients
            PopulateHardcodedColours();
            _bcWrapper.GlobalAppService.ReadProperties(OnReadProperties, LogErrorThenPopUpWindow);
        }
        else
        {
            // Enable RTT
            _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
            _bcWrapper.RTTService.RegisterRTTEventCallback(OnEventCallback);
            _bcWrapper.RTTService.EnableRTT(OnEnableRTT, OnRTTDisconnected);
        }
    }

    private void OnReadProperties(string jsonResponse, object cbObject)
    {
        var response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        if (data == null)
        {
            Debug.LogWarning("Need to set up lobby types as a global properties in brainCloud portal. " +
                             "Refer to the README.md for an example under Relay Test App.");

            // Enable RTT
            _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
            _bcWrapper.RTTService.RegisterRTTEventCallback(OnEventCallback);
            _bcWrapper.RTTService.EnableRTT(OnEnableRTT, OnRTTDisconnected);
            return;
        }
        var value = new Dictionary<string, object>();
        for (int i = 0; i < data.Count; i++)
        {
            var item = data.ElementAt(i);
            value[item.Key] = ((Dictionary<string, object>)item.Value)["value"];
        }

        Dictionary<string, object> lobby = new Dictionary<string, object>();
        var lobbyData = JsonReader.Deserialize<Dictionary<string, object>>((string)value["AllLobbyTypes"]);
        _teamLobbyTypesList.Clear();
        _ffaLobbyTypesList.Clear();
        for (int j = 0; j < lobbyData.Count; j++)
        {
            lobby = lobbyData[j.ToString()] as Dictionary<string, object>;
            string lobbyType = lobby["lobby"].ToString();
            if (lobbyType.Contains("Team"))
            {
                _teamLobbyTypesList.Add(lobbyType);
            }
            else
            {
                _ffaLobbyTypesList.Add(lobbyType);
            }
        }

        _noServerSelected = false;
        GameManager.Instance.UpdateLobbyDropdowns(_ffaLobbyTypesList, _teamLobbyTypesList);

        if (value.ContainsKey("Colors"))
        {
            try
            {
                var hexArray = JsonReader.Deserialize<string[]>((string)value["Colors"]);
                if (hexArray != null && hexArray.Length > 0)
                {
                    var newColors = new List<Color>();
                    foreach (var hex in hexArray)
                    {
                        if (ColorUtility.TryParseHtmlString(hex, out Color c))
                            newColors.Add(c);
                    }
                    if (newColors.Count > 0)
                    {
                        colours.Clear();
                        colours.AddRange(newColors);
                        GameManager.Instance.UpdateColorList(colours);
                    }
                }
            }
            catch { }
        }

        // Enable RTT
        _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        _bcWrapper.RTTService.RegisterRTTEventCallback(OnEventCallback);
        _bcWrapper.RTTService.EnableRTT(OnEnableRTT, OnRTTDisconnected);
    }

    private void OnEnableRTT(string jsonResponse, object cbObject)
    {
        OnLoggedIn();
    }

    // Called when UpdateReady fails — if the lobby is already gone (e.g. disbanded on match start),
    // just return to the main menu instead of showing a hard error.
    public void OnUpdateReadyFailure(int status, int reasonCode, string jsonError, object cbObject)
    {
        if (reasonCode == ReasonCodes.LOBBY_NOT_FOUND)
        {
            StateManager.Instance.PopupMessageToMainMenu("The lobby has ended. Returning to main menu.");
            return;
        }
        LogErrorThenPopUpWindow(status, reasonCode, jsonError, cbObject);
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

    void OnRoomLaunchFailure()
    {
        if (_dead) return;

        _dead = true;
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        StateManager.Instance.PopupMessageToMainMenu("Something went wrong with launching the server. Please try again.");
    }
    #endregion BC Callbacks

    #region GameFlow

    public void FindLobby(RelayConnectionType protocol)
    {
        StateManager.Instance.SessionPlayers.Clear();
        StateManager.Instance.Protocol = protocol;
        GameManager.Instance.CurrentUserInfo.UserGameColor = Settings.GetPlayerPrefColor();
        _isReconnecting = false;
        OnRTTConnected("", null);
    }

    // Cleanly close the game. Go back to main menu but don't log
    private void BroadcastRelayPing()
    {
        if (!_bcWrapper.RelayService.IsConnected()) return;
        int ping = (int)(_bcWrapper.RelayService.LastPing * 0.0001f);

        string myCxId = _bcWrapper.Client.RTTConnectionID;
        foreach (var member in StateManager.Instance.CurrentLobby.Members)
        {
            if (member.cxId == myCxId) { member.activePing = ping; break; }
        }

        var msg = new Dictionary<string, object>
        {
            ["op"] = "relay_ping",
            ["data"] = new Dictionary<string, object> { ["ping"] = ping }
        };
        byte[] bytes = Encoding.ASCII.GetBytes(JsonWriter.Serialize(msg));
        _bcWrapper.RelayService.Send(bytes, BrainCloudRelay.TO_ALL_PLAYERS,
            false, false, BrainCloudRelay.CHANNEL_HIGH_PRIORITY_1);

        GameManager.Instance.RefreshMatchEntryPings();
    }

    public void CloseGame(bool changeState = false)
    {
        CancelInvoke(nameof(BroadcastRelayPing));
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RelayService.Disconnect();

        //_bcWrapper.RTTService.DeregisterAllRTTCallbacks();
        //_bcWrapper.RTTService.DisableRTT();

        if (changeState)
        {
            StateManager.Instance.LeaveMatchBackToMenu();
            GameManager.Instance.ClearMatchEntries();
        }
    }
    public void LeaveLobby()
    {
        _bcWrapper.LobbyService.LeaveLobby(StateManager.Instance.CurrentLobby.LobbyID, null, null);
    }

    // Ready up and signals RTT service we can start the game
    public void StartGame()
    {
        StateManager.Instance.isReady = true;

        if (_noServerSelected && GameManager.Instance.IsLocalUserHost())
        {
            //   "members": [
            //     {
            //       "cxId": "23649:05b379b4-d366-4748-9424-750d77bbc428:nodufh0g6c45qbunfri8raiov1",
            //       "passcode": "12345"
            //     }
            List<string> memberScriptData = new List<string>();
            var listOfMembers = StateManager.Instance.CurrentLobby.Members;
            for (int i = 0; i < listOfMembers.Count; i++)
            {
                memberScriptData.Add(listOfMembers[i].cxId);
            }

            Dictionary<string, object> scriptData = new Dictionary<string, object>();

            scriptData.Add("members", memberScriptData);
            scriptData.Add("ownerCxId", StateManager.Instance.CurrentLobby.OwnerCxID);

            _bcWrapper.ScriptService.RunScript("ConnectPlayer", JsonWriter.Serialize(scriptData));
        }
        else if (!StateManager.Instance.CurrentLobby.LobbyID.IsNullOrEmpty())
        {
            //Setting up a update to send to brain cloud about local users color
            var extra = new Dictionary<string, object>();
            extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;
            extra["presentSinceStart"] = GameManager.Instance.CurrentUserInfo.PresentSinceStart;

            //
            _bcWrapper.LobbyService.UpdateReady(StateManager.Instance.CurrentLobby.LobbyID, true, extra, null, OnUpdateReadyFailure);
        }
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
        _bcWrapper.RTTService.RegisterRTTEventCallback(OnEventCallback);
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
        byte[] jsonBytes = { 0x0 };
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

    private void SendToSpecificTeamWithCompression(Dictionary<string, object> in_dict, TeamCodes teamToSend, bool in_reliable = true,
        bool in_ordered = true, int in_channel = 0, char in_joinChar = '=', char in_splitChar = ';')
    {
        string jsonData;
        byte[] jsonBytes = { 0x0 };
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
        // Always attempt JSON parse first for special non-player ops (sent as JSON regardless
        // of the configured relay compression type).
        try
        {
            string rawStr = Encoding.ASCII.GetString(jsonResponse);
            var earlyParse = (Dictionary<string, object>)JsonReader.Deserialize(rawStr);
            var earlyOp = earlyParse?.ContainsKey("op") == true ? earlyParse["op"] as string : null;

            if (earlyOp == "splotch_sync")
            {
                HandleSplotchSync(earlyParse);
                return;
            }
            if (earlyOp == "clear_splotches")
            {
                StateManager.Instance.PendingClearSplatters = true;
                return;
            }
            if (earlyOp == "relay_ping")
            {
                var pingData = earlyParse["data"] as Dictionary<string, object>;
                if (pingData != null && pingData.ContainsKey("ping"))
                {
                    int ping = Convert.ToInt32(pingData["ping"]);
                    string senderCxId = _bcWrapper.RelayService.GetCxIdForNetId(netId);
                    foreach (var member in StateManager.Instance.CurrentLobby.Members)
                    {
                        if (member.cxId == senderCxId) { member.activePing = ping; break; }
                    }
                    GameManager.Instance.RefreshMatchEntryPings();
                }
                return;
            }
        }
        catch { /* Binary DataStreamByte packets will throw — fall through to normal path */ }

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
                            if (data.ContainsKey("teamCode"))
                            {
                                TeamCodes splatterCode = (TeamCodes)data["teamCode"];
                                member.SplatterTeamCodes.Add(splatterCode);

                                TeamCodes instigatorCode = (TeamCodes)data["instigator"];
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

                            TeamCodes splatterCode = (TeamCodes)json["teamCode"];
                            member.SplatterTeamCodes.Add(splatterCode);

                            TeamCodes instigatorCode = (TeamCodes)json["instigator"];
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
            if (jsonData.ContainsKey("lobbyId"))
            {
                StateManager.Instance.CurrentLobby = new Lobby(jsonData["lobby"] as Dictionary<string, object>,
                    jsonData["lobbyId"] as string);
            }
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
                    if (roomConfig != null && roomConfig.ContainsKey("enableDisconnectButton"))
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
                        // var reason = jsonData["reason"] as Dictionary<string, object>;
                        // if ((int) reason["code"] != ReasonCodes.RTT_ROOM_READY)
                        // {
                        //     // Disbanded for any other reason than ROOM_READY, means we failed to launch the game.
                        //     CloseGame(true);
                        // }
                        // else
                        // {
                        //OnRoomLaunchFailure();
                        // }

                        break;
                    }
                case "STARTING":
                    // Save our picked color index
                    _presentWhileStarted = true;
                    _lobbyStatusStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    _progressMessage = "Server starting...";
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

    private string serverId;
    private void OnEventCallback(string jsonResponse)
    {
        Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(jsonResponse);

        //Using the key "operation" to determine what state the lobby is in
        if (response.ContainsKey("operation") && response["data"] is Dictionary<string, object> jsonData)
        {
            var operation = response["operation"] as string;
            if (operation == "GET_EVENTS")
            {
                string incomingEventType = jsonData["eventType"] as string;
                Dictionary<string, object> eventData = jsonData["eventData"] as Dictionary<string, object>;
                switch (incomingEventType)
                {
                    case "launchStart":
                        // Save our picked color index
                        _presentWhileStarted = true;
                        GameManager.Instance.UpdatePresentSinceStart();
                        Settings.SetPlayerPrefColor(GameManager.Instance.CurrentUserInfo.UserGameColor);
                        //Set up loading screen
                        if (!GameManager.Instance.IsLocalUserHost())
                        {
                            StateManager.Instance.ButtonPressed_ChangeState(GameStates.Lobby);
                        }
                        //Save server info
                        if (eventData != null)
                        {
                            serverId = eventData["serverId"] as string;
                        }
                        break;
                    case "roomProgressUpdate":
                        if (eventData != null)
                        {
                            string progressUpdate = eventData["description"] as string;
                            if (!string.IsNullOrEmpty(progressUpdate))
                                _progressMessage = progressUpdate;
                        }
                        break;
                    case "roomAssigned":
                        StateManager.Instance.CurrentServer = new Server(eventData, true);
                        break;
                    case "roomReady":
                        GameManager.Instance.UpdateMatchAndLobbyState();
                        GameManager.Instance.UpdateCursorList();
                        //Check to see if a user joined the lobby before the match started or after.
                        //If a user joins while match is in progress, you will only receive MEMBER_JOIN & ROOM_READY RTT updates.
                        ConnectRelay();
                        break;
                }
            }
        }
    }

    // Connect to the Relay server and start the game
    public void ConnectRelay()
    {
        _presentWhileStarted = false;
        _lobbySearchStartTime = 0;
        _lobbyStatusStartTime = 0;
        _progressMessage = "";
        _bcWrapper.RTTService.RegisterRTTLobbyCallback(OnLobbyEvent);
        _bcWrapper.RTTService.RegisterRTTEventCallback(OnEventCallback);
        _bcWrapper.RelayService.RegisterRelayCallback(OnRelayMessage);
        _bcWrapper.RelayService.RegisterSystemCallback(OnRelaySystemMessage);
        InvokeRepeating(nameof(BroadcastRelayPing), 2f, 2f);

        int port = 0;
        Server server = StateManager.Instance.CurrentServer;

        // GameLift and i3D only expose a single WebSocket port — force WEBSOCKET for both.
        RelayConnectionType connectionType = StateManager.Instance.Protocol;
        if (server.GameliftPort != -1)
        {
            port = server.GameliftPort;
            connectionType = RelayConnectionType.WEBSOCKET;
        }
        else if (server.i3dPort != -1)
        {
            port = server.i3dPort;
            connectionType = RelayConnectionType.WEBSOCKET;
        }
        else
        {
            switch (connectionType)
            {
                case RelayConnectionType.WEBSOCKET:
                    port = server.WsPort;
                    break;
                case RelayConnectionType.TCP:
                    port = server.TcpPort;
                    break;
                case RelayConnectionType.UDP:
                    port = server.UdpPort;
                    break;
            }
        }

        if (_noServerSelected)
        {
            _bcWrapper.RelayService.Connect
            (
                connectionType,
                new RelayConnectOptions(false, server.Host, port, server.Passcode, serverId),
                null,
                (FailureCallback)OnConnectFailed + LogErrorThenPopUpWindow,
                "Failed to connect to server"
            );
        }
        else
        {
            _bcWrapper.RelayService.Connect
            (
                connectionType,
                new RelayConnectOptions(false, server.Host, port, server.Passcode, server.LobbyId),
                null,
                (FailureCallback)OnConnectFailed + LogErrorThenPopUpWindow,
                "Failed to connect to server"
            );
        }
        Debug.LogWarning("Relay Connect Called");
    }

    private void OnConnectFailed(int status, int reasonCode, string jsonError, object cbObject)
    {
        Debug.LogError("Connect Error: " + jsonError);
        Debug.LogError($"Reason Code: {reasonCode}, Status: {status}");
    }

    public void DisconnectFromEverything()
    {
        CancelInvoke(nameof(BroadcastRelayPing));
        _bcWrapper.RelayService.DeregisterRelayCallback();
        _bcWrapper.RelayService.DeregisterSystemCallback();
        _bcWrapper.RelayService.Disconnect();
        _bcWrapper.RTTService.DisableRTT();
        _bcWrapper.Client.ResetCommunication();
    }

    public void DisconnectFromRelay()
    {
        CancelInvoke(nameof(BroadcastRelayPing));
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
        UnityEngine.Debug.Log("Re-authentication successful. " + _bcWrapper.WrapperName);
        _bcWrapper.RTTService.EnableRTT(OnReEnableRTT, LogErrorThenPopUpWindow);
    }

    private void OnReEnableRTT(string response, object cbObject)
    {
        ConnectRelay();
    }

    public void Logout()
    {
        _bcWrapper.RTTService.DisableRTT();
        _bcWrapper.RTTService.DeregisterAllRTTCallbacks();

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

            // Host: send full splotch canvas to the joining player
            if (GameManager.Instance.IsLocalUserHost() &&
                StateManager.Instance.CurrentGameState == GameStates.Match)
            {
                int newNetId = _bcWrapper.RelayService.GetNetIdForCxId(cxId);
                if (newNetId >= 0)
                {
                    ulong playerMask = (ulong)newNetId;
                    SendSplotchSync(playerMask);
                }
            }
        }
        else if (json["op"] as string == "END_MATCH")
        {
            StateManager.Instance.isReady = false;
            GameManager.Instance.CurrentUserInfo.PresentSinceStart = false;

            StateManager.Instance.ResetData();
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
        _lobbySearchStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _lobbyStatusStartTime = 0;
        _progressMessage = "";

        var algo = new Dictionary<string, object>();
        algo["strategy"] = "ranged-absolute";
        algo["alignment"] = "center";
        List<int> ranges = new List<int>();
        ranges.Add(1000);
        algo["ranges"] = ranges;

        var extra = new Dictionary<string, object>();
        extra["colorIndex"] = (int)GameManager.Instance.CurrentUserInfo.UserGameColor;

        var filters = new Dictionary<string, object>();
        var settings = new Dictionary<string, object>();
        string teamCode = GameManager.Instance.GameMode == GameMode.FreeForAll ? "all" : "";

        if (Settings.GetUsePingData())
        {
            _bcWrapper.LobbyService.GetRegionsForLobbies(
                new string[] { GetLobbyType() },
                (regionsJson, cbObj) =>
                {
                    _bcWrapper.LobbyService.PingRegions(
                        (pingJson, cbObj2) =>
                        {
                            _pingData.Clear();
                            if (_bcWrapper.LobbyService.PingData != null)
                            {
                                foreach (var kv in _bcWrapper.LobbyService.PingData)
                                    _pingData[kv.Key] = (int)kv.Value;
                            }
                            GameManager.Instance.UpdatePingRegionQuality();
                            _bcWrapper.LobbyService.FindOrCreateLobbyWithPingData(
                                GetLobbyType(), 0, 1, algo, filters, false, extra, teamCode, settings, null,
                                FindLobbyCallback, LogErrorThenPopUpWindow, "Failed to find lobby"
                            );
                        },
                        LogErrorThenPopUpWindow
                    );
                },
                LogErrorThenPopUpWindow
            );
        }
        else
        {
            _pingData.Clear();
            GameManager.Instance.UpdatePingRegionQuality();
            _bcWrapper.LobbyService.FindOrCreateLobby(
                GetLobbyType(), 0, 1, algo, filters, false, extra, teamCode, settings, null,
                FindLobbyCallback, LogErrorThenPopUpWindow, "Failed to find lobby"
            );
        }
    }

    public Dictionary<string, int> PingData => _pingData;

    private string GetLobbyType()
    {
        string lobbyType = "";
        if (GameManager.Instance.GameMode == GameMode.FreeForAll)
        {
            lobbyType = _currentFFALobby;
        }
        else
        {
            lobbyType = _currentTeamLobby;
        }

        return lobbyType;
    }



    private void FindLobbyCallback(string in_response, object cbObject)
    {
        Dictionary<string, object> response = JsonReader.Deserialize<Dictionary<string, object>>(in_response);
        Dictionary<string, object> data = response["data"] as Dictionary<string, object>;
        currentEntryId = data["entryId"] as string;
    }

    public void CancelFindRequest()
    {
        _lobbySearchStartTime = 0;
        _lobbyStatusStartTime = 0;
        _progressMessage = "";
        _bcWrapper.LobbyService.CancelFindRequest(GetLobbyType(), currentEntryId);
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
        relayInfo.PositionX = (float)data["x"];
        relayInfo.PositionY = (float)data["y"];
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

    // ── JIP / canvas-sync helpers ─────────────────────────────────────────────

    /// <summary>
    /// Parse an incoming splotch_sync JSON packet and queue records for GameArea
    /// to rebuild on the next Update frame.
    /// </summary>
    private void HandleSplotchSync(Dictionary<string, object> json)
    {
        var data = json.ContainsKey("data") ? json["data"] as Dictionary<string, object> : null;
        if (data == null) return;

        bool isFirst = data.ContainsKey("first") && data["first"] is bool b && b;
        if (isFirst) StateManager.Instance.PendingSyncIsFirst = true;

        if (!data.ContainsKey("splotches")) return;
        var arr = data["splotches"] as object[];
        if (arr == null) return;

        foreach (var entry in arr)
        {
            var sd = entry as Dictionary<string, object>;
            if (sd == null) continue;
            StateManager.Instance.PendingSyncSplotches.Add(new SplotchRecord
            {
                Position = new Vector2((float)Convert.ToDouble(sd["x"]), (float)Convert.ToDouble(sd["y"])),
                ColorIndex = Convert.ToInt32(sd["c"]),
                TeamCode = TeamCodes.all,
                InstigatorCode = TeamCodes.all,
                StartTimeMs = Convert.ToInt64(sd["t"])
            });
        }
    }

    /// <summary>
    /// Host-only: send the full splotch canvas to a specific player (by relay netId mask).
    /// Chunked so every packet stays under the relay max packet size (~900 bytes of payload).
    /// The first chunk carries "first":true so the receiver clears before rebuilding.
    /// </summary>
    private void SendSplotchSync(ulong toPlayerMask)
    {
        const int maxChunkBytes = 900;
        var splotches = StateManager.Instance.AllSplotches;
        bool isFirst = true;
        int i = 0;

        // Always send at least one packet (even empty) so the receiver clears its canvas.
        do
        {
            var batch = new List<Dictionary<string, object>>();
            byte[] packet = null;

            while (i < splotches.Count)
            {
                var s = splotches[i];
                batch.Add(new Dictionary<string, object>
                {
                    ["x"] = s.Position.x,
                    ["y"] = s.Position.y,
                    ["c"] = s.ColorIndex,
                    ["t"] = s.StartTimeMs
                });

                byte[] candidate = BuildSplotchSyncPacket(isFirst, batch);
                if (candidate.Length > maxChunkBytes && batch.Count > 1)
                {
                    // This entry pushed the packet over the limit — back it out and flush.
                    batch.RemoveAt(batch.Count - 1);
                    break;
                }
                packet = candidate;
                i++;
            }

            packet = packet ?? BuildSplotchSyncPacket(isFirst, batch);

            _bcWrapper.RelayService.Send(packet, toPlayerMask, true, true, 0);
            isFirst = false;

        } while (i < splotches.Count);
    }

    private byte[] BuildSplotchSyncPacket(bool isFirst, List<Dictionary<string, object>> batch)
    {
        var json = new Dictionary<string, object>
        {
            ["op"] = "splotch_sync",
            ["data"] = new Dictionary<string, object>
            {
                ["first"] = isFirst,
                ["splotches"] = batch.ToArray()
            }
        };
        return Encoding.ASCII.GetBytes(JsonWriter.Serialize(json));
    }

    /// <summary>
    /// Host-only: clear the splotch canvas on all players and locally.
    /// Wire a UI button to this method in the Unity editor.
    /// </summary>
    public void ClearSplatterCanvas()
    {
        // Clear locally (sender doesn't receive its own relay messages)
        StateManager.Instance.PendingClearSplatters = true;

        var json = new Dictionary<string, object> { ["op"] = "clear_splotches" };
        byte[] bytes = Encoding.ASCII.GetBytes(JsonWriter.Serialize(json));
        _bcWrapper.RelayService.Send(bytes, BrainCloudRelay.TO_ALL_PLAYERS, true, true, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────

    public void SetLobbyType(GameMode in_gameMode, int index)
    {
        if (in_gameMode == GameMode.Team)
        {
            _currentTeamLobby = _teamLobbyTypesList[index];
            _noServerSelected = _currentTeamLobby.Contains("NoRoomServer");
        }
        else
        {
            _currentFFALobby = _ffaLobbyTypesList[index];
            _noServerSelected = _currentFFALobby.Contains("NoRoomServer");
        }
    }
}
